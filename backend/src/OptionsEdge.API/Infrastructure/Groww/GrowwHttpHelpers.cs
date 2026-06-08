using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace OptionsEdge.API.Infrastructure.Groww;

// Shared low-level helpers for talking to the Groww broker REST API (https://api.groww.in),
// used by GrowwUserApiClient — the sole client, since every Groww call (market data, orders,
// positions) authenticates with an individual user's own credentials.
internal static class GrowwHttpHelpers
{
    private static readonly TimeZoneInfo IstZone = GetIstZone();

    // Sends an authenticated request with standard Groww headers, retrying up to 3 times
    // with a 100ms delay when rate-limited (HTTP 429).
    public static async Task<JsonElement> SendAuthenticatedAsync(
        HttpClient http, ILogger logger, string token, HttpMethod method, string path, object? body, CancellationToken ct)
    {
        for (int attempt = 0; attempt <= 3; attempt++)
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("X-API-VERSION", "1.0");

            if (body is not null)
                request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await http.SendAsync(request, ct);

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
    public static JsonElement Unwrap(JsonElement root)
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

    public static string? GetString(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    public static decimal? GetDecimal(JsonElement element, string property)
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
    public static DateTimeOffset NextSixAmIst()
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
