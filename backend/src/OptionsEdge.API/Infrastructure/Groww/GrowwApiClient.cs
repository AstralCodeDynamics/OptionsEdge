using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using OptionsEdge.API.Infrastructure.MockData;

namespace OptionsEdge.API.Infrastructure.Groww;

// Thin wrapper around the Groww broker REST API (https://api.groww.in).
// Handles TOTP authentication, daily token caching (expires 6 AM IST), standard headers,
// and retry-with-delay on HTTP 429 (rate limited).
public class GrowwApiClient(
    IHttpClientFactory factory,
    IConfiguration config,
    IMemoryCache cache,
    ILogger<GrowwApiClient> logger)
{
    private const string TokenCacheKey = "groww:access_token";
    private static readonly TimeZoneInfo IstZone = GetIstZone();

    private readonly HttpClient _http = factory.CreateClient("groww");
    private string ApiKey => config["Groww:ApiKey"] ?? "";

    public bool IsConnected => cache.TryGetValue(TokenCacheKey, out string? token) && !string.IsNullOrEmpty(token);

    // The cached token's absolute expiry is always "the next 6 AM IST" — while the token
    // remains valid, recomputing it now yields the same instant the cache entry was set with.
    public static DateTimeOffset NextTokenExpiry() => NextSixAmIst();

    public async Task<string> AuthenticateAsync(string totp, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/token/api/access");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("X-API-VERSION", "1.0");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { key_type = "totp", totp }), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Groww authentication failed ({(int)response.StatusCode}): {raw}");

        using var doc = JsonDocument.Parse(raw);
        var root = Unwrap(doc.RootElement);
        var token = GetString(root, "token") ?? GetString(root, "access_token");

        if (string.IsNullOrEmpty(token))
            throw new InvalidOperationException("Groww authentication response did not include a token");

        var expiry = NextSixAmIst();
        cache.Set(TokenCacheKey, token, new MemoryCacheEntryOptions { AbsoluteExpiration = expiry });
        logger.LogInformation("Groww access token cached, expires {Expiry} IST", expiry);
        return token;
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

    public async Task<GrowwOrderResult> PlaceOrderAsync(GrowwOrderRequest order, CancellationToken ct = default)
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

        var root = Unwrap(await SendAsync(HttpMethod.Post, "/v1/order/create", body, ct));

        return new GrowwOrderResult(
            OrderId: GetString(root, "order_id") ?? "",
            Status: GetString(root, "order_status") ?? GetString(root, "status") ?? "",
            RejectReason: GetString(root, "remark"));
    }

    public async Task<GrowwOrderResult> CancelOrderAsync(string orderId, string segment, CancellationToken ct = default)
    {
        var root = Unwrap(await SendAsync(HttpMethod.Post, "/v1/order/cancel",
            new { order_id = orderId, segment }, ct));

        return new GrowwOrderResult(
            OrderId: orderId,
            Status: GetString(root, "order_status") ?? GetString(root, "status") ?? "CANCELLED",
            RejectReason: GetString(root, "remark"));
    }

    public async Task<IReadOnlyList<GrowwPosition>> GetPositionsAsync(CancellationToken ct = default)
    {
        var root = Unwrap(await SendAsync(HttpMethod.Get, "/v1/portfolio/positions?segment=FNO", null, ct));

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

    // Sends an authenticated request with standard Groww headers, retrying up to 3 times
    // with a 100ms delay when rate-limited (HTTP 429).
    private async Task<JsonElement> SendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        if (!cache.TryGetValue(TokenCacheKey, out string? token) || string.IsNullOrEmpty(token))
            throw new InvalidOperationException("Groww is not connected. Submit your TOTP via /api/v1/groww/connect first.");

        for (int attempt = 0; attempt <= 3; attempt++)
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("X-API-VERSION", "1.0");

            if (body is not null)
                request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests && attempt < 3)
            {
                logger.LogWarning("Groww API rate limited on {Path}, retrying in 100ms (attempt {Attempt})", path, attempt + 1);
                await Task.Delay(TimeSpan.FromMilliseconds(100), ct);
                continue;
            }

            var raw = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Groww API error {(int)response.StatusCode} on {path}: {raw}");

            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
            return doc.RootElement.Clone();
        }

        throw new HttpRequestException($"Groww API rate limit retries exhausted for {path}");
    }

    // Many broker APIs wrap the real payload in an envelope, e.g. {"status":"SUCCESS","payload":{...}}
    private static JsonElement Unwrap(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("payload", out var payload) && payload.ValueKind == JsonValueKind.Object)
                return Unwrap(payload);
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                return Unwrap(data);
        }
        return root;
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

    private static string? GetString(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    private static decimal? GetDecimal(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var el))
            return null;

        return el.ValueKind switch
        {
            JsonValueKind.Number => el.GetDecimal(),
            JsonValueKind.String when decimal.TryParse(el.GetString(), out var d) => d,
            _ => null,
        };
    }

    // Groww access tokens expire daily at 6:00 AM IST
    private static DateTimeOffset NextSixAmIst()
    {
        var ist = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstZone);
        var sixAm = ist.Date.AddHours(6);
        if (ist >= sixAm)
            sixAm = sixAm.AddDays(1);

        var offset = IstZone.GetUtcOffset(sixAm);
        return new DateTimeOffset(sixAm, offset).ToUniversalTime();
    }

    private static TimeZoneInfo GetIstZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
    }
}
