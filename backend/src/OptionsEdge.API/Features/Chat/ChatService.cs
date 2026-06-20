using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OptionsEdge.API.Common.Constants;
using OptionsEdge.API.Domain.Entities;
using OptionsEdge.API.Features.AI;
using OptionsEdge.API.Features.Groww;
using OptionsEdge.API.Features.Indicators;
using OptionsEdge.API.Infrastructure.Background;
using OptionsEdge.API.Infrastructure.Claude;
using OptionsEdge.API.Infrastructure.Data;
using OptionsEdge.API.Infrastructure.MockData;

namespace OptionsEdge.API.Features.Chat;

public class ChatService(
    ClaudeApiClient claude,
    IndicatorService indicatorService,
    IMarketDataService marketData,
    AppDbContext db,
    IConfiguration config,
    UserAICredentialService aiCredentials,
    GrowwCredentialService growwCredentials,
    ILogger<ChatService> logger)
{
    public async Task<string?> ValidateAsync(Guid userId, CancellationToken ct = default)
    {
        bool bypass = config.GetValue<bool>("Claude:BypassMarketHours");
        if (!bypass && !MarketHoursHelper.IsMarketOpen())
            return "Market is closed. Chat is available during market hours (9:15–15:30 IST).";

        var user = await db.Users.FindAsync([userId], ct);
        if (user is not null)
        {
            var callLimit = AppConstants.RateLimits.GetCallLimitForPlan(user.SubscriptionPlan);
            if (DateTimeOffset.UtcNow - user.AiCallsResetAt < TimeSpan.FromHours(1)
                && user.AiCallsToday >= callLimit)
            {
                return $"Rate limit reached. You can make {callLimit} AI calls per hour.";
            }
        }

        return null;
    }

    public async Task<ChatHistoryResponse> GetHistoryAsync(Guid userId, Guid sessionId, CancellationToken ct = default)
    {
        var messages = await db.ChatMessages
            .Where(m => m.UserId == userId && m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

        return new ChatHistoryResponse(
            sessionId,
            messages.Select(m => new ChatHistoryItem(
                m.Id,
                m.Role,
                m.Content,
                m.ModelUsed,
                m.InputTokens,
                m.OutputTokens,
                m.CostUsd,
                m.CreatedAt.ToString("O"))).ToList());
    }

    public async IAsyncEnumerable<ChatStreamChunk> StreamMessageAsync(
        Guid userId,
        Guid sessionId,
        string message,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var apiKey = await aiCredentials.GetApiKeyAsync(userId, ct);
        if (string.IsNullOrEmpty(apiKey))
        {
            yield return new ChatStreamChunk("error",
                Error: "No AI API key configured. Go to Settings → AI Connection to add your Anthropic key from console.anthropic.com");
            yield break;
        }

        var model = config["Claude:SonnetModel"] ?? AppConstants.Models.Sonnet;
        var now   = DateTimeOffset.UtcNow;

        var systemPrompt = await BuildSystemPromptAsync(userId, ct);

        // Last 10 turns (oldest first) give Claude genuine multi-turn memory of this session
        var recent = await db.ChatMessages
            .Where(m => m.UserId == userId && m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(10)
            .Select(m => new { m.Role, m.Content, m.CreatedAt })
            .ToListAsync(ct);

        IReadOnlyList<(string Role, string Content)> history = recent
            .OrderBy(m => m.CreatedAt)
            .Select(m => (m.Role, m.Content))
            .ToList();

        db.ChatMessages.Add(new ChatMessage
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            SessionId = sessionId,
            Role      = "user",
            Content   = message,
            CreatedAt = now,
        });
        await db.SaveChangesAsync(ct);

        var assistantText = new StringBuilder();
        int inputTokens   = 0;
        int outputTokens  = 0;

        logger.LogInformation("Streaming Claude Sonnet chat reply for user {UserId} session {SessionId}", userId, sessionId);

        await foreach (var chunk in claude.StreamAsync(apiKey, model, systemPrompt, message, AppConstants.AiTokenLimits.SonnetMaxTokens, history, ct))
        {
            if (chunk.TextDelta is not null)
            {
                assistantText.Append(chunk.TextDelta);
                yield return new ChatStreamChunk("delta", Text: chunk.TextDelta);
            }
            else
            {
                inputTokens  = chunk.InputTokens ?? inputTokens;
                outputTokens = chunk.OutputTokens ?? outputTokens;
            }
        }

        decimal cost   = ClaudeApiClient.CalculateCost(model, inputTokens, outputTokens);
        var completedAt = DateTimeOffset.UtcNow;

        try
        {
            db.ChatMessages.Add(new ChatMessage
            {
                Id           = Guid.NewGuid(),
                UserId       = userId,
                SessionId    = sessionId,
                Role         = "assistant",
                Content      = assistantText.ToString(),
                ModelUsed    = model,
                InputTokens  = inputTokens,
                OutputTokens = outputTokens,
                CostUsd      = cost,
                CreatedAt    = completedAt,
            });

            db.AIUsageLogs.Add(new AIUsageLog
            {
                Id           = Guid.NewGuid(),
                UserId       = userId,
                Feature      = "chat",
                ModelUsed    = model,
                InputTokens  = inputTokens,
                OutputTokens = outputTokens,
                CostUsd      = cost,
                CreatedAt    = completedAt,
            });

            var user = await db.Users.FindAsync([userId], ct);
            if (user is not null)
            {
                if (DateTimeOffset.UtcNow - user.AiCallsResetAt >= TimeSpan.FromHours(1))
                {
                    user.AiCallsToday   = 0;
                    user.AiCallsResetAt = DateTimeOffset.UtcNow;
                }
                user.AiCallsToday++;
            }

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to persist chat message/usage to DB, continuing");
        }

        yield return new ChatStreamChunk("done",
            ModelUsed:    model,
            InputTokens:  inputTokens,
            OutputTokens: outputTokens,
            CostUsd:      cost);
    }

    private async Task<string> BuildSystemPromptAsync(Guid userId, CancellationToken ct)
    {
        var positions = await db.Positions
            .Where(p => p.UserId == userId && p.Status == "active")
            .ToListAsync(ct);

        var positionLines = positions.Count == 0
            ? ["No active positions."]
            : positions.Select(p =>
                $"- {p.Symbol} {p.Strike} {p.OptionType} exp {p.Expiry:yyyy-MM-dd} | Entry ₹{p.EntryPrice} x{p.Quantity} lot(s) | " +
                $"SL ₹{p.StopLoss} | T1 ₹{p.Target1}" + (p.Target2 is { } t2 ? $" | T2 ₹{t2}" : "")).ToArray();

        var marketStatus = MarketHoursHelper.IsMarketOpen() ? "OPEN" : "CLOSED";

        // Only read from shared market cache when user has their own Groww connection.
        // Without it, using the cache would serve another user's live data in this user's AI context.
        bool hasGroww = !config.GetValue<bool>("Groww:Enabled")
                        || await growwCredentials.HasCredentialsAsync(userId, ct);

        string marketContextSection;
        if (hasGroww)
        {
            var symbols = positions
                .Select(p => p.Symbol)
                .Distinct()
                .DefaultIfEmpty("NIFTY")
                .ToList();

            var marketLines = symbols.Select(sym =>
            {
                var snap = marketData.GetSnapshot(sym);
                var ind  = indicatorService.GetIndicators(sym);
                return $"- {sym}: Spot {snap.Ltp:F2} ({snap.ChangePct:F2}%) | VIX {snap.Vix:F2} | PCR {snap.Pcr:F2} | " +
                       $"RSI {ind.Rsi.Value:F1} [{ind.Rsi.Signal}] | MACD bullish cross: {ind.Macd.IsBullishCross} | " +
                       $"SuperTrend: {(ind.Supertrend.IsBullish ? "bullish" : "bearish")}";
            });

            marketContextSection = $"""
                Live market context:
                {string.Join("\n", marketLines)}
                Market status: {marketStatus} (IST trading hours: 9:15–15:30)
                """;
        }
        else
        {
            marketContextSection = $"""
                Live market context: NOT AVAILABLE — user has not connected a Groww account.
                Acknowledge this when asked about live prices, indicators, or market conditions.
                You can still help with general options concepts, strategy discussion, and reviewing
                the user's existing positions below.
                Market status: {marketStatus} (IST trading hours: 9:15–15:30)
                """;
        }

        return $"""
            {ChatPersona}

            {marketContextSection}

            User's active positions:
            {string.Join("\n", positionLines)}

            Be proactive: reference the user's actual positions and the live market data above when relevant.
            Keep answers concise, specific, and grounded in the numbers given — avoid vague generalities.
            Remember the user trades manually on Groww — you provide analysis, signals, and risk guidance only, never execute trades.
            """;
    }

    private const string ChatPersona = """
        You are an expert NIFTY/BANKNIFTY options trading partner — a seasoned derivatives trader having
        a conversation with the user. You explain reasoning clearly, give direct opinions with confidence
        levels, and help the user think through entries, exits, position sizing, and risk management.
        """;
}
