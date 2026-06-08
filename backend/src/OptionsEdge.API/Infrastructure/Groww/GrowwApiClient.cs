using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using OptionsEdge.API.Infrastructure.MockData;
using OtpNet;
using static OptionsEdge.API.Infrastructure.Groww.GrowwHttpHelpers;

namespace OptionsEdge.API.Infrastructure.Groww;

// Shared, read-only market-data wrapper around the Groww broker REST API (https://api.groww.in).
// Powers the app's live NIFTY/BANKNIFTY snapshots, option chains, and candles for ALL users —
// these aren't user-specific, so a single "system" account (Groww:ApiKey / Groww:ApiSecret in
// configuration) is used. It authenticates automatically: that ApiKey/ApiSecret pair (the
// permanent "TOTP Token" and "TOTP Secret" from the Groww API dashboard) generates a 6-digit
// TOTP code, exchanged for a daily access token cached until 6 AM IST.
//
// Per-user operations (placing orders, cancelling orders, importing positions) use each
// user's own credentials via GrowwUserApiClient instead.
public class GrowwApiClient(
    IHttpClientFactory factory,
    IConfiguration config,
    IMemoryCache cache,
    ILogger<GrowwApiClient> logger)
{
    private const string TokenCacheKey = "groww:system_access_token";
    private static readonly SemaphoreSlim AuthLock = new(1, 1);

    private readonly HttpClient _http = factory.CreateClient("groww");
    private string ApiKey => config["Groww:SystemApiKey"] ?? "";
    private string ApiSecret => config["Groww:SystemApiSecret"] ?? "";

    // The cached token's absolute expiry is always "the next 6 AM IST" — while the token
    // remains valid, recomputing it now yields the same instant the cache entry was set with.
    public static DateTimeOffset NextTokenExpiry() => NextSixAmIst();

    // Returns the cached daily access token for the shared system account, transparently
    // generating a fresh TOTP from ApiKey/ApiSecret and re-authenticating once the cache has
    // expired (after 6 AM IST). Concurrent callers share one in-flight authentication via AuthLock.
    public async Task<string> GetOrRefreshTokenAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(TokenCacheKey, out string? cached) && !string.IsNullOrEmpty(cached))
            return cached;

        await AuthLock.WaitAsync(ct);
        try
        {
            if (cache.TryGetValue(TokenCacheKey, out cached) && !string.IsNullOrEmpty(cached))
                return cached;

            var apiKey = ApiKey;
            var apiSecret = ApiSecret;
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
                throw new InvalidOperationException("Groww:SystemApiKey and Groww:SystemApiSecret must be configured");

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
                logger.LogError("Groww system TOTP authentication failed ({Status}): {Body}", response.StatusCode, raw);
                throw new InvalidOperationException($"Groww authentication failed ({(int)response.StatusCode}): {raw}");
            }

            using var doc = JsonDocument.Parse(raw);
            var root = Unwrap(doc.RootElement);
            var token = GetString(root, "token") ?? GetString(root, "access_token");

            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException("Groww authentication response did not include a token");

            var expiry = NextSixAmIst();
            cache.Set(TokenCacheKey, token, new MemoryCacheEntryOptions { AbsoluteExpiration = expiry });
            logger.LogInformation("Groww system access token refreshed automatically, expires {Expiry} IST", expiry);
            return token;
        }
        finally
        {
            AuthLock.Release();
        }
    }

    public async Task<MarketSnapshotData> GetSpotSnapshotAsync(string symbol, CancellationToken ct = default)
    {
        var root = Unwrap(await SendAsync(HttpMethod.Get,
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
            // Groww's quote API doesn't expose VIX/PCR/FII/DII flow — those need a separate data source
            Vix: 0,
            Pcr: 0,
            FiiFlow: 0,
            DiiFlow: 0,
            Timestamp: DateTimeOffset.UtcNow);
    }

    public async Task<decimal> GetOptionLtpAsync(string tradingSymbol, CancellationToken ct = default)
    {
        var root = Unwrap(await SendAsync(HttpMethod.Get,
            $"/v1/live-data/quote?exchange=NSE&segment=FNO&trading_symbol={Uri.EscapeDataString(tradingSymbol)}",
            null, ct));

        return GetDecimal(root, "last_price") ?? 0;
    }

    public async Task<IReadOnlyList<GrowwOptionChainRow>> GetOptionChainAsync(string underlying, string expiryDate, CancellationToken ct = default)
    {
        var root = Unwrap(await SendAsync(HttpMethod.Get,
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
        string tradingSymbol, string segment = "CASH", string resolution = "D", int lookbackDays = 130, CancellationToken ct = default)
    {
        var to = DateTimeOffset.UtcNow;
        var from = to.AddDays(-lookbackDays);

        var root = Unwrap(await SendAsync(HttpMethod.Get,
            $"/v1/historical-data/candles/exchange/NSE/trading-symbol/{Uri.EscapeDataString(tradingSymbol)}" +
            $"?segment={segment}&resolution={resolution}&from={from.ToUnixTimeSeconds()}&to={to.ToUnixTimeSeconds()}",
            null, ct));

        var candles = new List<CandleData>();
        if (!root.TryGetProperty("candles", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return candles;

        // Each candle is [timestamp, open, high, low, close, volume]
        foreach (var c in arr.EnumerateArray())
        {
            if (c.ValueKind != JsonValueKind.Array) continue;
            var items = c.EnumerateArray().ToList();
            if (items.Count < 6) continue;

            candles.Add(new CandleData(
                Time: DateTimeOffset.FromUnixTimeSeconds(items[0].GetInt64()),
                Open: items[1].GetDecimal(),
                High: items[2].GetDecimal(),
                Low: items[3].GetDecimal(),
                Close: items[4].GetDecimal(),
                Volume: items[5].GetInt64()));
        }

        return candles;
    }

    private async Task<JsonElement> SendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var token = await GetOrRefreshTokenAsync(ct);
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
