using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OptionsEdge.API.Common.Constants;
using OptionsEdge.API.Domain.Entities;
using OptionsEdge.API.Features.Indicators;
using OptionsEdge.API.Features.Options;
using OptionsEdge.API.Infrastructure.Background;
using OptionsEdge.API.Infrastructure.Claude;
using OptionsEdge.API.Infrastructure.Data;
using OptionsEdge.API.Infrastructure.MockData;
using OptionsEdge.API.Infrastructure.SignalR;

namespace OptionsEdge.API.Features.Signals;

public class AISignalService(
    ClaudeApiClient claude,
    SignalCacheService cache,
    IndicatorService indicatorService,
    OptionsService optionsService,
    IMarketDataService marketData,
    IHubContext<MarketHub> hub,
    AppDbContext db,
    IConfiguration config,
    ILogger<AISignalService> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<(SignalResponse Signal, string? Error)> GenerateEntrySignalAsync(
        string symbol,
        Guid userId,
        CancellationToken ct = default)
    {
        var key = symbol.ToUpper();

        bool bypass = config.GetValue<bool>("Claude:BypassMarketHours");
        if (!bypass && !MarketHoursHelper.IsMarketOpen())
            return (null!, "Market is closed. Signals can only be generated during market hours (9:15–15:30 IST).");

        // Rate limit check
        var user = await db.Users.FindAsync([userId], ct);
        if (user is not null)
        {
            if (DateTimeOffset.UtcNow - user.AiCallsResetAt < TimeSpan.FromHours(1)
                && user.AiCallsToday >= AppConstants.RateLimits.AiCallsPerUserPerHour)
            {
                return (null!, $"Rate limit reached. You can make {AppConstants.RateLimits.AiCallsPerUserPerHour} AI calls per hour.");
            }

            if (DateTimeOffset.UtcNow - user.AiCallsResetAt >= TimeSpan.FromHours(1))
            {
                user.AiCallsToday = 0;
                user.AiCallsResetAt = DateTimeOffset.UtcNow;
            }
        }

        // Gather market context
        var indicators = indicatorService.GetIndicators(key);
        var snapshot   = marketData.GetSnapshot(key);
        var expiries   = optionsService.GetExpiries(key);

        // Cache check
        var cacheKey = cache.BuildKey(
            key,
            indicators.Rsi.Value,
            indicators.Macd.Value > indicators.Macd.Signal ? "bullish" : "bearish",
            (double)snapshot.Pcr,
            snapshot.Ltp);

        if (cache.TryGet(cacheKey, out var cached) && cached is not null)
        {
            logger.LogInformation("Signal cache hit for {Symbol}", key);
            return (cached with { FromCache = true }, null);
        }

        // Build prompt
        var model  = config["Claude:SonnetModel"] ?? AppConstants.Models.Sonnet;
        var prompt = BuildSignalPrompt(key, indicators, snapshot, expiries);

        logger.LogInformation("Calling Claude Sonnet for {Symbol} signal", key);
        var claudeResp = await claude.CompleteAsync(
            model,
            SignalSystemPrompt,
            prompt,
            AppConstants.AiTokenLimits.SonnetMaxTokens,
            ct);

        SignalAiOutput aiOutput;
        try
        {
            var json = ExtractJson(claudeResp.Text);
            aiOutput = JsonSerializer.Deserialize<SignalAiOutput>(json, JsonOpts)
                ?? throw new InvalidOperationException("Null deserialization result");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse Claude signal response: {Text}", claudeResp.Text);
            return (null!, "AI returned an invalid response. Please try again.");
        }

        decimal cost = ClaudeApiClient.CalculateCost(model, claudeResp.InputTokens, claudeResp.OutputTokens);
        var now = DateTimeOffset.UtcNow;

        // Persist signal to DB
        Guid signalId = Guid.NewGuid();
        if (user is not null)
        {
            try
            {
                var snapshotDoc = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    ltp        = snapshot.Ltp,
                    vix        = snapshot.Vix,
                    pcr        = snapshot.Pcr,
                    fiiFlow    = snapshot.FiiFlow,
                    diiFlow    = snapshot.DiiFlow,
                    rsi        = indicators.Rsi.Value,
                    macd       = indicators.Macd.Value,
                    adxStrength= indicators.Adx.Strength,
                    supertrend = indicators.Supertrend.IsBullish ? "bullish" : "bearish",
                    capturedAt = now
                }));

                var signal = new Signal
                {
                    Id             = signalId,
                    UserId         = userId,
                    Symbol         = aiOutput.Symbol,
                    SignalType     = aiOutput.SignalType,
                    OptionType     = aiOutput.OptionType,
                    Strike         = aiOutput.Strike,
                    Expiry         = DateOnly.TryParse(aiOutput.Expiry, out var exp) ? exp : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                    EntryLow       = aiOutput.EntryLow,
                    EntryHigh      = aiOutput.EntryHigh,
                    StopLoss       = aiOutput.StopLoss,
                    Target1        = aiOutput.Target1,
                    Target2        = aiOutput.Target2,
                    Confidence     = aiOutput.Confidence,
                    RiskReward     = aiOutput.RiskReward,
                    Rationale      = aiOutput.Rationale,
                    MarketSnapshot = snapshotDoc,
                    ModelUsed      = model,
                    InputTokens    = claudeResp.InputTokens,
                    OutputTokens   = claudeResp.OutputTokens,
                    CostUsd        = cost,
                    ValidUntil     = DateTimeOffset.TryParse(aiOutput.ValidUntil, out var vu) ? vu : now.AddHours(4),
                    CreatedAt      = now,
                };
                db.Signals.Add(signal);

                db.AIUsageLogs.Add(new AIUsageLog
                {
                    Id           = Guid.NewGuid(),
                    UserId       = userId,
                    Feature      = "signal",
                    ModelUsed    = model,
                    InputTokens  = claudeResp.InputTokens,
                    OutputTokens = claudeResp.OutputTokens,
                    CostUsd      = cost,
                    CreatedAt    = now,
                });

                user.AiCallsToday++;
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to persist signal to DB, continuing");
            }
        }

        var result = new SignalResponse(
            Id:           signalId,
            Symbol:       aiOutput.Symbol,
            SignalType:   aiOutput.SignalType,
            OptionType:   aiOutput.OptionType,
            Strike:       aiOutput.Strike,
            Expiry:       aiOutput.Expiry,
            EntryLow:     aiOutput.EntryLow,
            EntryHigh:    aiOutput.EntryHigh,
            StopLoss:     aiOutput.StopLoss,
            Target1:      aiOutput.Target1,
            Target2:      aiOutput.Target2,
            Confidence:   aiOutput.Confidence,
            RiskReward:   aiOutput.RiskReward,
            Rationale:    aiOutput.Rationale,
            ModelUsed:    model,
            InputTokens:  claudeResp.InputTokens,
            OutputTokens: claudeResp.OutputTokens,
            CostUsd:      cost,
            ValidUntil:   aiOutput.ValidUntil,
            CreatedAt:    now.ToString("O"));

        cache.Set(cacheKey, result);

        // Broadcast via SignalR
        await hub.Clients.Group(key).SendAsync("NewSignal", result, ct);

        return (result, null);
    }

    public async Task<RiskCheckResponse> RunPositionRiskCheckAsync(
        Domain.Entities.Position position,
        CancellationToken ct = default)
    {
        var snapshot = marketData.GetSnapshot(position.Symbol);
        var model    = config["Claude:HaikuModel"] ?? AppConstants.Models.Haiku;

        var prompt = BuildRiskCheckPrompt(position, snapshot);
        var claudeResp = await claude.CompleteAsync(
            model,
            RiskCheckSystemPrompt,
            prompt,
            AppConstants.AiTokenLimits.HaikuMaxTokens,
            ct);

        try
        {
            var json = ExtractJson(claudeResp.Text);
            return JsonSerializer.Deserialize<RiskCheckResponse>(json, JsonOpts)
                ?? new RiskCheckResponse("INFO", "UNKNOWN", "Unable to assess risk", "Monitor position");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse Claude risk check response");
            return new RiskCheckResponse("INFO", "UNKNOWN", "Unable to assess risk", "Monitor position");
        }
    }

    // ------------------------------------------------------------------
    private static string BuildSignalPrompt(
        string symbol,
        Indicators.IndicatorsResponse ind,
        MarketSnapshotData snap,
        IReadOnlyList<string> expiries)
    {
        var atm = symbol == "NIFTY"
            ? (int)Math.Round((double)snap.Ltp / 50) * 50
            : (int)Math.Round((double)snap.Ltp / 100) * 100;

        return $"""
            Symbol: {symbol}
            Spot: {snap.Ltp:F2} | Change: {snap.ChangePct:F2}%
            VIX: {snap.Vix:F2} | PCR: {snap.Pcr:F2}
            FII Flow: {snap.FiiFlow:F0} Cr | DII Flow: {snap.DiiFlow:F0} Cr
            ATM Strike: {atm}

            Technical Indicators:
            - RSI(14): {ind.Rsi.Value:F1} [{ind.Rsi.Signal}]
            - MACD: {ind.Macd.Value:F2} | Signal: {ind.Macd.Signal:F2} | Histogram: {ind.Macd.Histogram:F2} | Bullish Cross: {ind.Macd.IsBullishCross}
            - Bollinger Bands: Upper {ind.BollingerBands.Upper:F0} / Middle {ind.BollingerBands.Middle:F0} / Lower {ind.BollingerBands.Lower:F0} | Squeeze: {ind.BollingerBands.IsSqueeze}
            - ADX(14): {ind.Adx.Value:F1} [{ind.Adx.Strength}]
            - EMA20: {ind.Ema.Ema20:F0} | EMA50: {ind.Ema.Ema50:F0} | Price Above EMA20: {ind.Ema.PriceAboveEma20} | Above EMA50: {ind.Ema.PriceAboveEma50}
            - SuperTrend: {ind.Supertrend.Value:F0} | Bullish: {ind.Supertrend.IsBullish}
            - Pivot R1: {ind.Pivots.R1:F0} | Pivot: {ind.Pivots.Pivot:F0} | S1: {ind.Pivots.S1:F0}

            Available Expiries: {string.Join(", ", expiries.Take(3))}

            Generate an options trading signal for {symbol}.
            """;
    }

    private static string BuildRiskCheckPrompt(
        Domain.Entities.Position position,
        MarketSnapshotData snapshot) =>
        $"""
        Position: {position.Symbol} {position.Strike} {position.OptionType} Expiry {position.Expiry}
        Entry Price: {position.EntryPrice} | Quantity: {position.Quantity} lots
        Stop Loss: {position.StopLoss} | Target1: {position.Target1} | Target2: {position.Target2}
        Current Spot: {snapshot.Ltp} | VIX: {snapshot.Vix} | PCR: {snapshot.Pcr}

        Assess the risk of this position now.
        """;

    private const string SignalSystemPrompt = """
        You are a professional NIFTY/BANKNIFTY options trader with deep expertise in technical analysis and options pricing.

        Analyze the market data and generate a precise trading signal. You MUST respond with ONLY a valid JSON object — no markdown, no code fences, no explanation, just the JSON.

        JSON schema (use exactly these field names):
        {"signalType":"ENTRY","symbol":"NIFTY","strike":24200,"optionType":"CE","expiry":"2026-06-12","entryLow":180,"entryHigh":200,"stopLoss":120,"target1":300,"target2":420,"confidence":72,"riskReward":1.85,"rationale":["RSI recovering from oversold","MACD bullish crossover forming","PCR supportive for CE buying"],"validUntil":"2026-06-06T15:30:00+05:30"}

        Rules:
        - signalType: ENTRY (new trade), HOLD (stay in), WATCH (not yet), EXIT (close)
        - Strike: ATM or 1 OTM preferred
        - Stop loss must be 30-40% below entry
        - Target1 must be R:R >= 1.5 from stop loss
        - confidence: realistic 55-85 range
        - rationale: 3-4 specific technical reasons
        - validUntil: end of current trading session (15:30 IST)
        """;

    private const string RiskCheckSystemPrompt = """
        You are a risk manager for an options trading system. Analyze the position and respond with ONLY a valid JSON object, no markdown.

        JSON schema: {"severity":"WARNING","alertType":"SL_APPROACHING","message":"brief message","recommendedAction":"specific action"}

        severity options: INFO, WARNING, DANGER
        alertType options: SL_APPROACHING, SL_HIT, TARGET_HIT, IV_SPIKE, ADVERSE_MOVE, THETA_DECAY, ALL_CLEAR
        """;

    private static string ExtractJson(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```"))
        {
            var nlIdx = trimmed.IndexOf('\n');
            if (nlIdx >= 0)
            {
                var endIdx = trimmed.LastIndexOf("```");
                if (endIdx > nlIdx)
                    trimmed = trimmed[(nlIdx + 1)..endIdx].Trim();
            }
        }
        // Find first '{' to last '}' in case there's preamble text
        var start = trimmed.IndexOf('{');
        var end   = trimmed.LastIndexOf('}');
        return start >= 0 && end > start ? trimmed[start..(end + 1)] : trimmed;
    }
}
