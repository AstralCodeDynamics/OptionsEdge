using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using OptionsEdge.API.Infrastructure.Groww;
using OtpNet;
using static OptionsEdge.API.Infrastructure.Groww.GrowwHttpHelpers;

namespace OptionsEdge.API.Features.Groww;

// Per-user wrapper around the Groww broker REST API for genuinely user-specific operations:
// placing/cancelling orders and reading each user's own portfolio. Authenticates with that
// user's own ApiKey/ApiSecret pair (saved via GrowwCredentialService, AES-encrypted at rest),
// generating a TOTP via Otp.NET and exchanging it for a daily access token cached per user
// until 6 AM IST.
//
// Shared, read-only market data (snapshots/option chains/candles) is NOT user-specific and
// is served by GrowwApiClient with a single system account instead.
public class GrowwUserApiClient(
    IHttpClientFactory factory,
    GrowwCredentialService credentials,
    IMemoryCache cache,
    ILogger<GrowwUserApiClient> logger)
{
    private static readonly SemaphoreSlim AuthLock = new(1, 1);

    private readonly HttpClient _http = factory.CreateClient("groww");

    public static DateTimeOffset NextTokenExpiry() => NextSixAmIst();

    private static string TokenCacheKey(Guid userId) => $"groww:user_token:{userId}";
    private static string ImportPendingCacheKey(Guid userId) => $"groww:user_import_pending:{userId}";

    public void InvalidateToken(Guid userId)
    {
        cache.Remove(TokenCacheKey(userId));
        cache.Remove(ImportPendingCacheKey(userId));
    }

    // Validates a not-yet-saved ApiKey/ApiSecret pair by generating a TOTP and authenticating
    // with Groww. Throws if the pair doesn't authenticate; the token itself is discarded —
    // callers should persist the credentials only after this succeeds.
    public Task TestAuthenticateAsync(string apiKey, string apiSecret, CancellationToken ct = default) =>
        AuthenticateAsync(apiKey, apiSecret, ct);

    // Returns the cached daily access token for this user, transparently generating a fresh
    // TOTP from their saved ApiKey/ApiSecret and re-authenticating once the cache has expired
    // (after 6 AM IST). Throws if the user hasn't saved Groww credentials yet.
    public async Task<string> GetOrRefreshTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var tokenKey = TokenCacheKey(userId);
        if (cache.TryGetValue(tokenKey, out string? cached) && !string.IsNullOrEmpty(cached))
            return cached;

        await AuthLock.WaitAsync(ct);
        try
        {
            if (cache.TryGetValue(tokenKey, out cached) && !string.IsNullOrEmpty(cached))
                return cached;

            var creds = await credentials.GetCredentialsAsync(userId, ct)
                ?? throw new InvalidOperationException(
                    "Groww credentials not configured. Go to Settings > Groww Integration to add your API credentials.");

            var token = await AuthenticateAsync(creds.ApiKey, creds.ApiSecret, ct);

            var expiry = NextSixAmIst();
            var cacheOptions = new MemoryCacheEntryOptions { AbsoluteExpiration = expiry };
            cache.Set(tokenKey, token, cacheOptions);
            cache.Set(ImportPendingCacheKey(userId), true, cacheOptions);
            logger.LogInformation("Groww access token refreshed for user {UserId}, expires {Expiry} IST", userId, expiry);
            return token;
        }
        finally
        {
            AuthLock.Release();
        }
    }

    // Generates a TOTP from apiSecret and exchanges the (apiKey, totp) pair for a daily
    // access token. Shared by GetOrRefreshTokenAsync (caches the result) and
    // TestAuthenticateAsync (validates a credential pair before it's saved).
    private async Task<string> AuthenticateAsync(string apiKey, string apiSecret, CancellationToken ct)
    {
        var totp = new Totp(Base32Encoding.ToBytes(apiSecret));
        var totpCode = totp.ComputeTotp();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/token/api/access");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("X-API-VERSION", "1.0");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { key_type = "totp", totp = totpCode }), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Groww TOTP authentication failed ({Status}): {Body}", response.StatusCode, raw);
            throw new InvalidOperationException($"Groww authentication failed ({(int)response.StatusCode}): {raw}");
        }

        using var doc = JsonDocument.Parse(raw);
        var root = Unwrap(doc.RootElement);
        var token = GetString(root, "token") ?? GetString(root, "access_token");

        if (string.IsNullOrEmpty(token))
            throw new InvalidOperationException("Groww authentication response did not include a token");

        return token;
    }

    // Returns true exactly once per freshly-issued token, so the caller can run a one-time
    // position import right after each automatic re-authentication for this user.
    public bool TryConsumeImportFlag(Guid userId)
    {
        var key = ImportPendingCacheKey(userId);
        if (cache.TryGetValue(key, out bool pending) && pending)
        {
            cache.Remove(key);
            return true;
        }
        return false;
    }

    public async Task<GrowwOrderResult> PlaceOrderAsync(Guid userId, GrowwOrderRequest order, CancellationToken ct = default)
    {
        var body = new
        {
            trading_symbol = order.TradingSymbol,
            quantity = order.Quantity,
            price = order.Price,
            validity = order.Validity,
            exchange = order.Exchange,
            segment = order.Segment,
            product = order.Product,
            order_type = order.OrderType,
            transaction_type = order.TransactionType,
            order_reference_id = order.OrderReferenceId,
        };

        var root = Unwrap(await SendAsync(userId, HttpMethod.Post, "/v1/order/create", body, ct));

        return new GrowwOrderResult(
            OrderId: GetString(root, "order_id") ?? "",
            Status: GetString(root, "order_status") ?? GetString(root, "status") ?? "",
            RejectReason: GetString(root, "remark"));
    }

    public async Task<GrowwOrderResult> CancelOrderAsync(Guid userId, string orderId, string segment, CancellationToken ct = default)
    {
        var root = Unwrap(await SendAsync(userId, HttpMethod.Post, "/v1/order/cancel",
            new { order_id = orderId, segment }, ct));

        return new GrowwOrderResult(
            OrderId: orderId,
            Status: GetString(root, "order_status") ?? GetString(root, "status") ?? "CANCELLED",
            RejectReason: GetString(root, "remark"));
    }

    public async Task<IReadOnlyList<GrowwPosition>> GetPositionsAsync(Guid userId, CancellationToken ct = default)
    {
        var root = Unwrap(await SendAsync(userId, HttpMethod.Get, "/v1/portfolio/positions?segment=FNO", null, ct));

        var positions = new List<GrowwPosition>();
        if (!root.TryGetProperty("positions", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return positions;

        foreach (var p in arr.EnumerateArray())
        {
            positions.Add(new GrowwPosition(
                TradingSymbol: GetString(p, "trading_symbol") ?? "",
                Quantity: (int)(GetDecimal(p, "quantity") ?? GetDecimal(p, "net_quantity") ?? 0),
                AvgPrice: GetDecimal(p, "average_price") ?? GetDecimal(p, "buy_avg_price") ?? 0,
                Ltp: GetDecimal(p, "ltp") ?? GetDecimal(p, "last_price") ?? 0,
                Pnl: GetDecimal(p, "pnl") ?? 0));
        }

        return positions;
    }

    private async Task<JsonElement> SendAsync(Guid userId, HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var token = await GetOrRefreshTokenAsync(userId, ct);
        return await SendAuthenticatedAsync(_http, logger, token, method, path, body, ct);
    }
}
