using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace OptionsEdge.API.Infrastructure.Claude;

public record ClaudeResponse(string Text, int InputTokens, int OutputTokens);

// TextDelta carries a streamed text chunk; InputTokens/OutputTokens carry the final usage chunk
public record ClaudeStreamChunk(string? TextDelta, int? InputTokens, int? OutputTokens);

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

    // Streams a completion as raw SSE (event:/data: lines), yielding text deltas as they
    // arrive and a final chunk carrying input/output token usage for cost tracking.
    public async IAsyncEnumerable<ClaudeStreamChunk> StreamAsync(
        string model,
        string systemPrompt,
        string userMessage,
        int maxTokens,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var requestBody = JsonSerializer.Serialize(new
        {
            model,
            max_tokens = maxTokens,
            system = systemPrompt,
            messages = new[] { new { role = "user", content = userMessage } },
            stream = true
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Claude API error {(int)response.StatusCode}: {errBody}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        int inputTokens = 0;
        int outputTokens = 0;
        string? eventType = null;

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (line.StartsWith("event:", StringComparison.Ordinal))
            {
                eventType = line["event:".Length..].Trim();
                continue;
            }

            if (!line.StartsWith("data:", StringComparison.Ordinal))
                continue;

            var json = line["data:".Length..].Trim();
            if (json.Length == 0)
                continue;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            switch (eventType)
            {
                case "message_start":
                    if (root.TryGetProperty("message", out var message)
                        && message.TryGetProperty("usage", out var startUsage)
                        && startUsage.TryGetProperty("input_tokens", out var inTokEl))
                    {
                        inputTokens = inTokEl.GetInt32();
                    }
                    break;

                case "content_block_delta":
                    if (root.TryGetProperty("delta", out var delta)
                        && delta.TryGetProperty("type", out var deltaType)
                        && deltaType.GetString() == "text_delta"
                        && delta.TryGetProperty("text", out var textEl))
                    {
                        var text = textEl.GetString();
                        if (!string.IsNullOrEmpty(text))
                            yield return new ClaudeStreamChunk(text, null, null);
                    }
                    break;

                case "message_delta":
                    if (root.TryGetProperty("usage", out var deltaUsage)
                        && deltaUsage.TryGetProperty("output_tokens", out var outTokEl))
                    {
                        outputTokens = outTokEl.GetInt32();
                    }
                    break;
            }
        }

        yield return new ClaudeStreamChunk(null, inputTokens, outputTokens);
    }

    // Cost in USD: Haiku $0.80/$4 per 1M tokens; Sonnet 4.6 $3/$15 per 1M tokens
    public static decimal CalculateCost(string model, int inputTokens, int outputTokens) =>
        model.Contains("haiku", StringComparison.OrdinalIgnoreCase)
            ? (decimal)(inputTokens * 0.0000008 + outputTokens * 0.000004)
            : (decimal)(inputTokens * 0.000003 + outputTokens * 0.000015);
}
