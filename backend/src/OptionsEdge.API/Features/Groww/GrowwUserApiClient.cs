using System.Net.Http.Headers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using OptionsEdge.API.Infrastructure.Groww;
using OptionsEdge.API.Infrastructure.MockData;
using OtpNet;
using static OptionsEdge.API.Infrastructure.Groww.GrowwHttpHelpers;

namespace OptionsEdge.API.Features.Groww;

// Per-user wrapper around the entire Groww broker REST API. There is no platform-wide "system"
// Groww account — every call (market data, orders, positions) authenticates with that user's
// own ApiKey/ApiSecret pair (saved via GrowwCredentialService, AES-encrypted at rest),
// generating a TOTP via Otp.NET and exchanging it for a daily access token cached per user
// until 6 AM IST.
//
// Market data (snapshots/option chains/candles) isn't user-specific in meaning, but Groww
// still requires an authenticated user to fetch it — GrowwMarketDataService caches whichever
// active user's results come back as the "last known" data for everyone.
public class GrowwUserApiClient(
    IHttpClientFactory factory,
    GrowwCredentialService credentials,
    IMemoryCache cache,
    ILogger<GrowwUserApiClient> logger)
{
    private static readonly SemaphoreSlim AuthLock = new(1, 1);
    private static readonly TimeSpan IstOffset = TimeSpan.FromHours(5.5);

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
        var root = Unwrap(await SendAsync(userId, HttpMethod.Get, "/v1/positions/user?segment=FNO", null, ct));

        var positions = new List<GrowwPosition>();
        if (!root.TryGetProperty("positions", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return positions;

        foreach (var p in arr.EnumerateArray())
        {
            positions.Add(new GrowwPosition(
                TradingSymbol: GetString(p, "trading_symbol") ?? "",
                Quantity: (int)(GetDecimal(p, "quantity") ?? GetDecimal(p, "net_quantity") ?? 0),
                AvgPrice: GetDecimal(p, "average_price") ?? GetDecimal(p, "buy_avg_price") ?? GetDecimal(p, "net_price") ?? GetDecimal(p, "net_carry_forward_price") ?? 0,
                Ltp: GetDecimal(p, "ltp") ?? GetDecimal(p, "last_price") ?? 0,
                Pnl: GetDecimal(p, "pnl") ?? GetDecimal(p, "realised_pnl") ?? 0));
        }

        return positions;
    }

    public async Task<MarketSnapshotData> GetSpotSnapshotAsync(Guid userId, string symbol, CancellationToken ct = default)
    {
        var root = Unwrap(await SendAsync(userId, HttpMethod.Get,
            $"/v1/live-data/quote?exchange=NSE&segment=CASH&trading_symbol={Uri.EscapeDataString(symbol)}",
            null, ct));

        decimal ltp = GetDecimal(root, "last_price") ?? 0;
        decimal open = GetDecimal(root, "open") ?? GetDecimal(root, "day_open") ?? ltp;
        decimal high = GetDecimal(root, "high") ?? GetDecimal(root, "day_high") ?? ltp;
        decimal low = GetDecimal(root, "low") ?? GetDecimal(root, "day_low") ?? ltp;
        decimal prevClose = GetDecimal(root, "close") ?? GetDecimal(root, "prev_close") ?? GetDecimal(root, "previous_close") ?? ltp;
        decimal change = Math.Round(ltp - prevClose, 2);
        decimal changePct = prevClose != 0 ? Math.Round(change / prevClose * 100, 2) : 0;

        return new MarketSnapshotData(
            Symbol: symbol,
            Ltp: ltp,
            Open: open,
            High: high,
            Low: low,
            PreviousClose: prevClose,
            Change: change,
            ChangePct: changePct,
            // Vix is fetched separately (GetVixAsync) and patched in by GrowwMarketDataService.
            // PCR/FII/DII flow aren't available via Groww at all.
            Vix: 0,
            Pcr: 0,
            FiiFlow: 0,
            DiiFlow: 0,
            Timestamp: DateTimeOffset.UtcNow);
    }

    // India VIX is symbol-agnostic, so callers fetch it once (for NIFTY) and reuse the value
    // for BANKNIFTY rather than calling Groww again. Failure is non-critical — VIX-dependent
    // UI/IV-smile calculations fall back to 0.
    public async Task<decimal> GetVixAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var root = Unwrap(await SendAsync(userId, HttpMethod.Get,
                "/v1/live-data/quote?exchange=NSE&segment=CASH&trading_symbol=INDIA%20VIX",
                null, ct));
            return GetDecimal(root, "last_price") ?? 0m;
        }
        catch
        {
            return 0m;
        }
    }

    public async Task<decimal> GetOptionLtpAsync(Guid userId, string tradingSymbol, CancellationToken ct = default)
    {
        var root = Unwrap(await SendAsync(userId, HttpMethod.Get,
            $"/v1/live-data/quote?exchange=NSE&segment=FNO&trading_symbol={Uri.EscapeDataString(tradingSymbol)}",
            null, ct));

        return GetDecimal(root, "last_price") ?? 0;
    }

    public async Task<IReadOnlyList<GrowwOptionChainRow>> GetOptionChainAsync(Guid userId, string underlying, string expiryDate, CancellationToken ct = default)
    {
        var root = Unwrap(await SendAsync(userId, HttpMethod.Get,
            $"/v1/option-chain/exchange/NSE/underlying/{Uri.EscapeDataString(underlying)}?expiry_date={Uri.EscapeDataString(expiryDate)}",
            null, ct));

        var rows = new List<GrowwOptionChainRow>();
        if (!root.TryGetProperty("strikes", out var strikes) || strikes.ValueKind != JsonValueKind.Object)
            return rows;

        // "strikes" is an object keyed by strike price, e.g. {"24200": {"CE": {...}, "PE": {...}}}
        foreach (var strikeEntry in strikes.EnumerateObject())
        {
            if (!int.TryParse(strikeEntry.Name, out var strike))
                continue;

            var leg = strikeEntry.Value;
            var call = ParseLeg(leg, "CE") ?? ParseLeg(leg, "call_options");
            var put = ParseLeg(leg, "PE") ?? ParseLeg(leg, "put_options");
            rows.Add(new GrowwOptionChainRow(strike, call, put));
        }

        return rows;
    }

    public async Task<IReadOnlyList<CandleData>> GetHistoricalCandlesAsync(
        Guid userId, string tradingSymbol, string segment = "CASH", int intervalMinutes = 15, int lookbackDays = 90, CancellationToken ct = default)
    {
        var to = DateTime.UtcNow.Add(IstOffset);
        var from = to.AddDays(-lookbackDays);
        var maxDuration = TimeSpan.FromDays(GetMaxCandleRequestDays(intervalMinutes));
        var candlesByTime = new SortedDictionary<DateTimeOffset, CandleData>();

        for (var chunkStart = from; chunkStart < to; chunkStart = chunkStart.Add(maxDuration))
        {
            ct.ThrowIfCancellationRequested();

            var chunkEnd = chunkStart.Add(maxDuration);
            if (chunkEnd > to)
                chunkEnd = to;

            foreach (var candle in await GetHistoricalCandleChunkAsync(
                userId, tradingSymbol, segment, intervalMinutes, chunkStart, chunkEnd, ct))
            {
                candlesByTime[candle.Time] = candle;
            }
        }

        var candles = candlesByTime.Values.ToList();

        logger.LogInformation(
            "Groww candles fetched for {Symbol}: {Count} candles at {Interval}min interval",
            tradingSymbol, candles.Count, intervalMinutes);

        return candles;
    }

    private async Task<IReadOnlyList<CandleData>> GetHistoricalCandleChunkAsync(
        Guid userId,
        string tradingSymbol,
        string segment,
        int intervalMinutes,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        // Groww expects "yyyy-MM-dd HH:mm:ss" in IST, not unix timestamps.
        var startTime = from.ToString("yyyy-MM-dd HH:mm:ss");
        var endTime = to.ToString("yyyy-MM-dd HH:mm:ss");

        var path = "/v1/historical/candle/range" +
            $"?exchange=NSE" +
            $"&segment={Uri.EscapeDataString(segment)}" +
            $"&trading_symbol={Uri.EscapeDataString(tradingSymbol)}" +
            $"&start_time={Uri.EscapeDataString(startTime)}" +
            $"&end_time={Uri.EscapeDataString(endTime)}" +
            $"&interval_in_minutes={intervalMinutes}";

        var root = Unwrap(await SendAsync(userId, HttpMethod.Get, path, null, ct));
        var candles = new List<CandleData>();

        // Response: { "payload": { "candles": [[ts,o,h,l,c,vol], ...] } } — Unwrap already
        // extracts payload, so root is the payload object directly.
        if (!root.TryGetProperty("candles", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return candles;

        // Each candle is [timestamp, open, high, low, close, volume].
        foreach (var c in arr.EnumerateArray())
        {
            if (c.ValueKind != JsonValueKind.Array)
                continue;

            var items = c.EnumerateArray().ToList();
            if (items.Count < 6)
                continue;

            if (!TryReadInt64(items[0], out var timestamp)
                || !TryReadDecimal(items[1], out var open)
                || !TryReadDecimal(items[2], out var high)
                || !TryReadDecimal(items[3], out var low)
                || !TryReadDecimal(items[4], out var close))
            {
                continue;
            }

            var volume = TryReadInt64(items[5], out var parsedVolume) ? parsedVolume : 0;

            candles.Add(new CandleData(
                Time: DateTimeOffset.FromUnixTimeSeconds(timestamp),
                Open: open,
                High: high,
                Low: low,
                Close: close,
                Volume: volume));
        }

        return candles;
    }

    private static bool TryReadDecimal(JsonElement element, out decimal value)
    {
        value = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetDecimal(out value),
            JsonValueKind.String => decimal.TryParse(element.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out value),
            _ => false,
        };
    }

    private static bool TryReadInt64(JsonElement element, out long value)
    {
        value = 0;
        if (element.ValueKind == JsonValueKind.Number)
        {
            if (element.TryGetInt64(out value))
                return true;

            if (element.TryGetDecimal(out var decimalValue))
            {
                value = decimal.ToInt64(decimal.Truncate(decimalValue));
                return true;
            }
        }

        return element.ValueKind == JsonValueKind.String
            && long.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static int GetMaxCandleRequestDays(int intervalMinutes) => intervalMinutes switch
    {
        <= 1 => 7,
        <= 5 => 15,
        <= 30 => 30,
        <= 60 => 150,
        <= 240 => 365,
        <= 1440 => 1080,
        _ => 1080,
    };

    private async Task<JsonElement> SendAsync(Guid userId, HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var token = await GetOrRefreshTokenAsync(userId, ct);
        return await SendAuthenticatedAsync(_http, logger, token, method, path, body, ct);
    }

    private static GrowwOptionLeg? ParseLeg(JsonElement parent, string property)
    {
        if (!parent.TryGetProperty(property, out var leg) || leg.ValueKind != JsonValueKind.Object)
            return null;

        return new GrowwOptionLeg(
            TradingSymbol: GetString(leg, "trading_symbol") ?? "",
            Ltp: GetDecimal(leg, "ltp") ?? GetDecimal(leg, "last_price") ?? 0,
            OpenInterest: (long)(GetDecimal(leg, "open_interest") ?? 0),
            Volume: (long)(GetDecimal(leg, "volume") ?? 0),
            ImpliedVolatility: GetDecimal(leg, "implied_volatility") ?? 0,
            Delta: GetDecimal(leg, "delta") ?? 0,
            Gamma: GetDecimal(leg, "gamma") ?? 0,
            Theta: GetDecimal(leg, "theta") ?? 0,
            Vega: GetDecimal(leg, "vega") ?? 0);
    }
}
