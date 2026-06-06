using System.Text;
using System.Text.Json;

namespace OptionsEdge.API.Infrastructure.Claude;

public record ClaudeResponse(string Text, int InputTokens, int OutputTokens);

public class ClaudeApiClient(
    IHttpClientFactory factory,
    IConfiguration config,
    ILogger<ClaudeApiClient> logger)
{
    private readonly HttpClient _http = factory.CreateClient("claude");
    private readonly string _apiKey = config["Claude:ApiKey"] ?? "";

    public async Task<ClaudeResponse> CompleteAsync(
        string model,
        string systemPrompt,
        string userMessage,
        int maxTokens,
        CancellationToken ct = default)
    {
        var requestBody = JsonSerializer.Serialize(new
        {
            model,
            max_tokens = maxTokens,
            system = systemPrompt,
            messages = new[] { new { role = "user", content = userMessage } }
        });

        for (int attempt = 0; attempt <= 2; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request, ct);
            var rawBody = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(rawBody);
                var root = doc.RootElement;

                var text = root
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString() ?? "";

                var usage = root.GetProperty("usage");
                int inputTokens = usage.GetProperty("input_tokens").GetInt32();
                int outputTokens = usage.GetProperty("output_tokens").GetInt32();

                return new ClaudeResponse(text, inputTokens, outputTokens);
            }

            if ((int)response.StatusCode >= 500 && attempt < 2)
            {
                logger.LogWarning("Claude API {Status} on attempt {Attempt}, retrying", response.StatusCode, attempt + 1);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
                continue;
            }

            throw new HttpRequestException($"Claude API error {(int)response.StatusCode}: {rawBody}");
        }

        throw new InvalidOperationException("All Claude API retries exhausted");
    }

    // Cost in USD: Haiku $0.80/$4 per 1M tokens; Sonnet 4.6 $3/$15 per 1M tokens
    public static decimal CalculateCost(string model, int inputTokens, int outputTokens) =>
        model.Contains("haiku", StringComparison.OrdinalIgnoreCase)
            ? (decimal)(inputTokens * 0.0000008 + outputTokens * 0.000004)
            : (decimal)(inputTokens * 0.000003 + outputTokens * 0.000015);
}
