using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OptionsEdge.API.Common.Configuration;
using OptionsEdge.API.Common.Constants;
using OptionsEdge.API.Domain.Entities;
using OptionsEdge.API.Features.AI;
using OptionsEdge.API.Features.Groww;
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
    IOptions<AIOptions> aiOptions,
    UserAICredentialService aiCredentials,
    GrowwCredentialService growwCredentials,
    ILogger<AISignalService> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly TimeZoneInfo IstZone = GetIstZone();

    private static TimeZoneInfo GetIstZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
    }

    public async Task<(SignalResponse Signal, string? Error)> GenerateEntrySignalAsync(
        string symbol,
        Guid userId,
        CancellationToken ct = default)
    {
        var key = symbol.ToUpper();

        bool bypass = config.GetValue<bool>("Claude:BypassMarketHours");
        if (!bypass && !MarketHoursHelper.IsMarketOpen())
            return (null!, "Market is closed. Signals can only be generated during market hours (9:15–15:30 IST).");

        if (config.GetValue<bool>("Groww:Enabled") && !await growwCredentials.HasCredentialsAsync(userId, ct))
            return (null!, "No Groww account connected. Connect your Groww account in Settings to generate live signals.");

        // Rate limit check (plan-based)
        var user = await db.Users.FindAsync([userId], ct);
        if (user is not null)
        {
            var callLimit = AppConstants.RateLimits.GetCallLimitForPlan(user.SubscriptionPlan);
            if (DateTimeOffset.UtcNow - user.AiCallsResetAt < TimeSpan.FromHours(1)
                && user.AiCallsToday >= callLimit)
            {
                return (null!, $"Rate limit reached. You can make {callLimit} AI calls per hour.");
            }

            if (DateTimeOffset.UtcNow - user.AiCallsResetAt >= TimeSpan.FromHours(1))
            {
                user.AiCallsToday = 0;
                user.AiCallsResetAt = DateTimeOffset.UtcNow;
            }
        }

        // Gather market context
        var snapshot   = marketData.GetSnapshot(key);
        if (snapshot is null)
            return (null!, "Live market data temporarily unavailable. Try again in a moment.");

        var indicators = indicatorService.GetIndicators(key);
        if (indicators is null)
            return (null!, "Live indicators temporarily unavailable. Try again in a moment.");

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

        var apiKey = await aiCredentials.GetApiKeyAsync(userId, ct);
        if (string.IsNullOrEmpty(apiKey))
            return (null!, "No AI API key configured. Go to Settings → AI Connection to add your Anthropic key from console.anthropic.com");

        // Fetch nearby-strikes premium table (ATM ±5) to anchor the AI's entry price to real premiums.
        // Degrades to empty string on any failure — prompt falls back to ATM-only context.
        string nearbyStrikesTable = string.Empty;
        try
        {
            if (expiries.Count > 0)
            {
                var chain  = optionsService.GetChain(key, expiries[0]);
                int sStep  = key == "BANKNIFTY" ? 100 : 50;
                int sAtm   = (int)Math.Round((double)snapshot.Ltp / sStep) * sStep;
                if (chain is not null)
                    nearbyStrikesTable = BuildNearbyStrikesTable(chain.Rows, sAtm);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch options chain for signal prompt — falling back to ATM-only context");
        }

        // Build prompt
        var model  = ResolveModel(aiOptions.Value, "Signal");
        var istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstZone);
        var prompt = BuildSignalPrompt(key, indicators, snapshot, expiries, istNow, nearbyStrikesTable);

        logger.LogInformation("Calling Claude Sonnet for {Symbol} signal", key);
        var claudeResp = await claude.CompleteAsync(
            apiKey,
            model,
            BuildSignalSystemPrompt(istNow),
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

        // Strike sanity check: reject AI picks more than 3 steps from ATM.
        // Root cause of Anju's 18/06/26 report: AI picked 23600 CE while ATM ~24100 (10 steps off),
        // then gave entry 110-130 which only fits a near-ATM strike — inconsistent and unactionable.
        // Threshold (3 steps) is a judgment call — confirm with Manu if legitimate strategy calls get blocked.
        if (!IsStrikeWithinBounds(aiOutput.Strike, snapshot.Ltp, key))
        {
            int sStep    = key == "BANKNIFTY" ? 100 : 50;
            int sAtm     = (int)Math.Round((double)snapshot.Ltp / sStep) * sStep;
            int sFromAtm = Math.Abs(aiOutput.Strike - sAtm) / sStep;
            logger.LogWarning(
                "AI chose strike {Strike} ({N} steps from ATM {Atm} for {Symbol}) — exceeds the 3-step sanity bound. Rejecting signal.",
                aiOutput.Strike, sFromAtm, sAtm, key);
            return (null!, "The AI's strike selection failed a sanity check. Please try generating again.");
        }

        decimal cost = ClaudeApiClient.CalculateCost(model, claudeResp.InputTokens, claudeResp.OutputTokens);
        var now = DateTimeOffset.UtcNow;

        // Normalize ValidUntil to UTC and apply safety net: a signal must never be born expired.
        var parsedValidUntil = DateTimeOffset.TryParse(aiOutput.ValidUntil, out var vu)
            ? vu.ToUniversalTime()
            : now.AddHours(4);
        if (parsedValidUntil <= now)
        {
            logger.LogWarning(
                "AI returned ValidUntil ({ValidUntil}) not after now ({Now}) for user {UserId}, symbol {Symbol}. Falling back to now + 4 hours.",
                parsedValidUntil, now, userId, aiOutput.Symbol);
            parsedValidUntil = now.AddHours(4);
        }

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
                    ValidUntil     = parsedValidUntil,
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
                logger.LogError(ex,
                    "Failed to persist signal to DB for user {UserId}, symbol {Symbol}, " +
                    "strike {Strike}, expiry {Expiry}. Signal was shown to user but will NOT appear in history.",
                    userId, aiOutput.Symbol, aiOutput.Strike, aiOutput.Expiry);
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
            ValidUntil:   parsedValidUntil.ToString("O"),
            CreatedAt:    now.ToString("O"));

        cache.Set(cacheKey, result);

        // Signal results are stored per user, so live updates must stay user-scoped too.
        await hub.Clients.User(userId.ToString()).SendAsync("NewSignal", result, ct);

        return (result, null);
    }

    public async Task<RiskCheckResponse> RunPositionRiskCheckAsync(
        Domain.Entities.Position position,
        CancellationToken ct = default)
    {
        var apiKey = await aiCredentials.GetApiKeyAsync(position.UserId, ct);
        if (string.IsNullOrEmpty(apiKey))
            return new RiskCheckResponse(
                "INFO",
                "NO_API_KEY",
                "No AI API key configured. Go to Settings → AI Connection to add your Anthropic key from console.anthropic.com",
                "Add your Anthropic API key in Settings → AI Connection");

        var snapshot = marketData.GetSnapshot(position.Symbol);
        if (snapshot is null)
            return new RiskCheckResponse(
                "INFO",
                "MARKET_DATA_UNAVAILABLE",
                "Live market data temporarily unavailable.",
                "Skip automated risk assessment until live data refreshes.");

        var model    = ResolveModel(aiOptions.Value, "RiskCheck");

        var prompt = BuildRiskCheckPrompt(position, snapshot);
        var claudeResp = await claude.CompleteAsync(
            apiKey,
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
    private static string ResolveModel(AIOptions ai, string feature)
    {
        var tier = ai.Features.TryGetValue(feature, out var f) ? f.ModelTier : "Default";
        return tier switch
        {
            "Quick" => ai.Models.Quick,
            "Deep" => ai.Models.Deep,
            _ => ai.Models.Default
        };
    }

    private static string BuildSignalPrompt(
        string symbol,
        Indicators.IndicatorsResponse ind,
        MarketSnapshotData snap,
        IReadOnlyList<string> expiries,
        DateTime istNow,
        string nearbyStrikesTable = "")
    {
        var atm = symbol == "NIFTY"
            ? (int)Math.Round((double)snap.Ltp / 50) * 50
            : (int)Math.Round((double)snap.Ltp / 100) * 100;

        string chainContext = string.IsNullOrEmpty(nearbyStrikesTable)
            ? $"ATM Strike: {atm}"
            : $"ATM Strike: {atm}\n\n{nearbyStrikesTable}";

        return $"""
            Current date and time (IST): {istNow:yyyy-MM-dd HH:mm:ss} IST
            Symbol: {symbol}
            Spot: {snap.Ltp:F2} | Change: {snap.ChangePct:F2}%
            VIX: {snap.Vix:F2} | PCR: {snap.Pcr:F2}
            FII Flow: {snap.FiiFlow:F0} Cr | DII Flow: {snap.DiiFlow:F0} Cr
            {chainContext}

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

    // Not a const — must embed today's IST date in the example so the AI anchors to the
    // correct date when computing validUntil. A stale hardcoded date in the example causes
    // the AI to copy that past date, producing a signal that is already expired on arrival.
    private static string BuildSignalSystemPrompt(DateTime istNow) =>
        $$"""
        You are a professional NIFTY/BANKNIFTY options trader with deep expertise in technical analysis and options pricing.

        Analyze the market data and generate a precise trading signal. You MUST respond with ONLY a valid JSON object — no markdown, no code fences, no explanation, just the JSON.

        JSON schema (use exactly these field names):
        {"signalType":"ENTRY","symbol":"NIFTY","strike":24200,"optionType":"CE","expiry":"{{istNow:yyyy-MM-dd}}","entryLow":180,"entryHigh":200,"stopLoss":120,"target1":300,"target2":420,"confidence":72,"riskReward":1.85,"rationale":["RSI recovering from oversold","MACD bullish crossover forming","PCR supportive for CE buying"],"validUntil":"{{istNow:yyyy-MM-dd}}T15:30:00+05:30"}

        Rules:
        - signalType: ENTRY (new trade), HOLD (stay in), WATCH (not yet), EXIT (close)
        - Strike selection: prefer ATM or the 1-2 strikes nearest ATM. Weight your choice toward
          whichever nearby strike has the HIGHEST open interest — high-OI strikes tend to see faster
          premium movement on directional confirmation. Do NOT choose a strike significantly far from
          ATM regardless of its OI.
        - entryLow/entryHigh MUST be consistent with the live CE/PE Premium shown in the nearby-strikes
          table above. Do not estimate or guess a premium that contradicts the table data.
        - Stop loss must be 30-40% below entry
        - Target1 must be R:R >= 1.5 from stop loss
        - confidence: realistic 55-85 range
        - rationale: 3-4 specific technical reasons
        - validUntil: use TODAY's date ({{istNow:yyyy-MM-dd}}) at 15:30:00+05:30, which is the end of the current trading session. Use the "Current date and time (IST)" from the user prompt — do NOT use any other date.
        - expiry: NIFTY weekly options expire every Tuesday on NSE (effective Sep 2025).
          BANKNIFTY weekly contracts were discontinued (Nov 2024) — only the monthly
          expiry (last Tuesday of the month) is available. Pick the expiry from the
          "Available Expiries" list provided in the prompt.
        """;

    private const string RiskCheckSystemPrompt = """
        You are a risk manager for an options trading system. Analyze the position and respond with ONLY a valid JSON object, no markdown.

        JSON schema: {"severity":"WARNING","alertType":"SL_APPROACHING","message":"brief message","recommendedAction":"specific action"}

        severity options: INFO, WARNING, DANGER
        alertType options: SL_APPROACHING, SL_HIT, TARGET_HIT, IV_SPIKE, ADVERSE_MOVE, THETA_DECAY, ALL_CLEAR
        """;

    // maxSteps: how many strike intervals from ATM the AI is allowed to pick.
    // 3 steps = ±150 for NIFTY, ±300 for BANKNIFTY. Threshold chosen based on Anju's 18/06/26
    // report (23600 CE vs ATM ~24100 = 10 steps). Adjust if legitimate wider strategies get blocked.
    internal static bool IsStrikeWithinBounds(int aiStrike, decimal spot, string symbol, int maxSteps = 3)
    {
        int step = symbol.ToUpperInvariant() == "BANKNIFTY" ? 100 : 50;
        int atm  = (int)Math.Round((double)spot / step) * step;
        return Math.Abs(aiStrike - atm) / step <= maxSteps;
    }

    // Builds a compact ATM ±maxSteps strike table from chain rows for prompt injection.
    // Returns empty string if rows is empty so callers can fall back to ATM-only context.
    internal static string BuildNearbyStrikesTable(IReadOnlyList<Options.ChainRowResponse> rows, int atm, int maxSteps = 5)
    {
        if (rows.Count == 0) return string.Empty;
        int step = rows.Count > 1 ? Math.Abs(rows[1].Strike - rows[0].Strike) : 50;
        var nearby = rows
            .Where(r => Math.Abs(r.Strike - atm) <= maxSteps * step)
            .OrderBy(r => r.Strike)
            .ToList();
        if (nearby.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("Nearby strikes (current premiums from live chain):");
        sb.AppendLine("  Strike | CE Premium | CE OI  | PE Premium | PE OI");
        foreach (var row in nearby)
        {
            string atmMark = row.Strike == atm ? " <- ATM" : string.Empty;
            sb.AppendLine(
                $"  {row.Strike,6} | {row.Ce.Ltp,10:F1} | {FormatOi(row.Ce.Oi),6} | {row.Pe.Ltp,10:F1} | {FormatOi(row.Pe.Oi)}{atmMark}");
        }
        return sb.ToString().TrimEnd();
    }

    private static string FormatOi(long oi) =>
        oi >= 1_000_000 ? $"{oi / 1_000_000.0:F1}M" :
        oi >= 1_000     ? $"{oi / 1_000.0:F0}K" :
        oi.ToString();

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
