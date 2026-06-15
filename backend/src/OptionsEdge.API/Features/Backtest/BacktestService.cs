using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Skender.Stock.Indicators;
using OptionsEdge.API.Common.Constants;
using OptionsEdge.API.Infrastructure.Data;
using OptionsEdge.API.Infrastructure.MockData;

namespace OptionsEdge.API.Features.Backtest;

// Simulates options strategies over the available 15-minute OHLCV history, evaluating one
// entry signal and one exit signal per run (plus always-on SL/Target risk management,
// since a strategy without a stop would never close within the test window).
// Option premiums are estimated with Black-Scholes using a baseline IV + moneyness skew —
// there is no real historical option chain to replay against.
public class BacktestService(
    IMarketDataService marketData,
    AppDbContext db,
    IConfiguration config,
    ILogger<BacktestService> logger)
{
    private const double RiskFreeRate = 0.065;
    private const double BaselineIv = 0.14;
    private const int MinWarmupBars = 200;
    private const decimal StopLossPct = 0.35m;
    private const decimal Target1Pct = 0.70m;
    private const decimal Target2Pct = 1.20m;

    private static readonly TimeZoneInfo IstZone = GetIstZone();

    private static readonly Dictionary<string, int> StrikeStep = new()
    {
        ["NIFTY"] = 50,
        ["BANKNIFTY"] = 100,
    };

    public static readonly string[] Strategies =
        ["LongCall", "LongPut", "BullCallSpread", "BearPutSpread", "Straddle", "Strangle", "IronCondor"];

    public static readonly string[] EntryConditions =
        ["RSI_Oversold", "RSI_Overbought", "MACD_Bullish_Cross", "MACD_Bearish_Cross",
         "SupertrendBullish", "SupertrendBearish", "PriceBreakoutAboveR1", "PriceBreakdownBelowS1",
         "PivotEma20Bullish", "PivotEma20Bearish"];

    public static readonly string[] ExitConditions =
        ["SLHit", "Target1Hit", "Target2Hit", "ThetaDecay50Pct", "ExpiryMinus1Day"];

    // Strategy legs expressed as offsets (in strike-step units) from the at-the-money strike
    private static readonly Dictionary<string, StrategyLeg[]> StrategyLegs = new()
    {
        ["LongCall"]       = [new(0, "CE", "BUY")],
        ["LongPut"]        = [new(0, "PE", "BUY")],
        ["BullCallSpread"] = [new(0, "CE", "BUY"), new(2, "CE", "SELL")],
        ["BearPutSpread"]  = [new(0, "PE", "BUY"), new(-2, "PE", "SELL")],
        ["Straddle"]       = [new(0, "CE", "BUY"), new(0, "PE", "BUY")],
        ["Strangle"]       = [new(2, "CE", "BUY"), new(-2, "PE", "BUY")],
        ["IronCondor"]     = [new(2, "CE", "SELL"), new(4, "CE", "BUY"), new(-2, "PE", "SELL"), new(-4, "PE", "BUY")],
    };

    public async Task<BacktestResultResponse> RunAsync(Guid userId, BacktestRunRequest req, CancellationToken ct = default)
    {
        var symbol = req.Symbol.ToUpperInvariant();
        if (symbol is not ("NIFTY" or "BANKNIFTY"))
            throw new ArgumentException("Symbol must be NIFTY or BANKNIFTY");
        if (!StrategyLegs.TryGetValue(req.Strategy, out var legs))
            throw new ArgumentException($"Strategy must be one of: {string.Join(", ", Strategies)}");
        if (!EntryConditions.Contains(req.EntryCondition))
            throw new ArgumentException($"EntryCondition must be one of: {string.Join(", ", EntryConditions)}");
        if (!ExitConditions.Contains(req.ExitCondition))
            throw new ArgumentException($"ExitCondition must be one of: {string.Join(", ", ExitConditions)}");
        if (req.PeriodDays < 5)
            throw new ArgumentException("PeriodDays must be at least 5");
        if (req.Lots <= 0)
            throw new ArgumentException("Lots must be at least 1");
        if (req.TargetPoints is <= 0)
            throw new ArgumentException("TargetPoints must be greater than 0");
        if (req.StopLossPoints is <= 0)
            throw new ArgumentException("StopLossPoints must be greater than 0");

        int lotSize = symbol == "BANKNIFTY" ? AppConstants.LotSizes.BankNifty : AppConstants.LotSizes.Nifty;
        int step = StrikeStep[symbol];

        var intraday = marketData.GetCandles(symbol);
        var daily = ToDailyCandles(intraday);
        if (daily.Count < 2 || intraday.Count < MinWarmupBars + 5)
            throw new InvalidOperationException("Not enough historical data to run a backtest");

        var simulation = Simulate(intraday, daily, req, legs, symbol, step, lotSize);
        var trades = simulation.Trades;
        var diagnostics = simulation.Diagnostics;

        var parameters = JsonSerializer.SerializeToDocument(new
        {
            symbol,
            strategy = req.Strategy,
            entryCondition = req.EntryCondition,
            exitCondition = req.ExitCondition,
            periodDays = req.PeriodDays,
            lots = req.Lots,
            targetPoints = req.TargetPoints,
            stopLossPoints = req.StopLossPoints,
            adxFilter = "ADXFilter",
            adxFilterEnabled = req.AdxFilterEnabled,
            diagnosticSummary = new
            {
                candidateSignals = diagnostics.CandidateSignals,
                filteredOut = diagnostics.FilteredOut,
                tradesEntered = diagnostics.TradesEntered,
                targetHits = diagnostics.TargetHits,
                slHits = diagnostics.SlHits,
                expiryExits = diagnostics.ExpiryExits,
                thetaExits = diagnostics.ThetaExits,
            },
            dataSource = GetDataSource(),
            candleCount = intraday.Count,
            tradingDays = daily.Count,
        });
        var stats = ComputeStats(trades);
        await CleanupExpiredHistoryAsync(userId, ct);

        var result = new Domain.Entities.BacktestResult
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Strategy = req.Strategy,
            Parameters = parameters,
            WinRate = stats.WinRate,
            TotalTrades = trades.Count,
            NetPnl = stats.NetPnl,
            MaxDrawdown = stats.MaxDrawdown,
            SharpeRatio = stats.SharpeRatio,
            ProfitFactor = stats.ProfitFactor,
            TradeLog = JsonSerializer.SerializeToDocument(trades),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.BacktestResults.Add(result);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Backtest run for {UserId}: {Strategy} on {Symbol}, {Trades} trades, net P&L {NetPnl}",
            userId, req.Strategy, symbol, trades.Count, stats.NetPnl);

        return ToResponse(result);
    }

    public async Task<BacktestHistoryResponse> GetHistoryAsync(
        Guid userId,
        int page = 1,
        int pageSize = 8,
        CancellationToken ct = default)
    {
        await CleanupExpiredHistoryAsync(userId, ct);

        pageSize = pageSize <= 0 ? 8 : Math.Clamp(pageSize, 1, 25);
        page = Math.Max(page, 1);

        int totalItems = await db.BacktestResults
            .Where(b => b.UserId == userId)
            .CountAsync(ct);
        int totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        page = Math.Min(page, totalPages);

        var rows = await db.BacktestResults
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new BacktestHistoryResponse(
            rows.Select(ToResponse).ToList(),
            page,
            pageSize,
            totalItems,
            totalPages,
            GetHistoryRetentionDays());
    }

    // ------------------------------------------------------------------
    // Simulation
    // ------------------------------------------------------------------

    private SimulationResult Simulate(
        IReadOnlyList<CandleData> intraday,
        IReadOnlyList<CandleData> daily,
        BacktestRunRequest req,
        StrategyLeg[] legs,
        string symbol,
        int step,
        int lotSize)
    {
        var candles = intraday.OrderBy(c => c.Time).ToList();
        var quotes = candles.Select(ToQuote).ToList();
        var rsi = quotes.GetRsi(14).ToList();
        var macd = quotes.GetMacd(12, 26, 9).ToList();
        var ema20 = quotes.GetEma(20).ToList();
        var adx14 = quotes.GetAdx(14).ToList();
        var supertrend = quotes.GetSuperTrend(10, 3).ToList();
        var pivotsByDate = BuildPivotLookup(daily);
        var trendFilterByDay = BuildTrendFilterLookup(candles, adx14, ema20);

        int testWindow = Math.Min(req.PeriodDays, daily.Count - 1);
        var startDate = ToIstDate(daily[^testWindow].Time);
        int firstWindowBar = candles.FindIndex(c => ToIstDate(c.Time) >= startDate);
        int startIndex = Math.Max(MinWarmupBars, firstWindowBar < 0 ? MinWarmupBars : firstWindowBar);

        var trades = new List<BacktestTradeLogEntry>();
        var diagnostics = new BacktestDiagnosticCounters();
        OpenTrade? open = null;

        for (int i = startIndex; i < candles.Count; i++)
        {
            var candle = candles[i];
            var prev = candles[i - 1];
            if (!pivotsByDate.TryGetValue(ToIstDate(candle.Time), out var pivots))
                continue;

            if (open is null)
            {
                var entryDecision = EntrySignalFires(
                    req.Strategy,
                    req.EntryCondition,
                    i,
                    candle,
                    prev,
                    rsi,
                    macd,
                    ema20,
                    supertrend,
                    pivots,
                    trendFilterByDay,
                    req.AdxFilterEnabled);

                if (entryDecision != EntrySignalDecision.NoSignal)
                    diagnostics.CandidateSignals++;

                if (entryDecision == EntrySignalDecision.FilteredOut)
                {
                    diagnostics.FilteredOut++;
                    continue;
                }

                if (entryDecision == EntrySignalDecision.Accepted)
                {
                    open = OpenPosition(symbol, req.Strategy, legs, candle, step, lotSize);
                    diagnostics.TradesEntered++;
                }
            }
            else
            {
                var exit = CheckExitConditions(req.ExitCondition, open, candle, req, lotSize);
                if (exit is not null)
                {
                    var trade = ToTradeLogEntry(open, candle.Time, exit.ExitValue, exit.PnL, exit.Reason, req);
                    trades.Add(trade);
                    diagnostics.CountExit(trade.ExitReason);
                    open = null;
                }
            }
        }

        if (open is not null)
        {
            var last = daily[^1];
            var (netValue, _, _) = ValuePosition(open, last.Close, last.Time);
            decimal pnl = (netValue - open.EntryNetPremium) * req.Lots * lotSize;
            var trade = ToTradeLogEntry(open, last.Time, netValue, pnl, "EndOfPeriod", req);
            trades.Add(trade);
            diagnostics.CountExit(trade.ExitReason);
        }

        return new SimulationResult(trades, diagnostics.ToSummary());
    }

    private static BacktestTradeLogEntry ToTradeLogEntry(
        OpenTrade trade,
        DateTimeOffset exitDate,
        decimal exitValue,
        decimal pnl,
        string reason,
        BacktestRunRequest req)
    {
        var risk = GetRiskSettings(trade, req);
        return new(
            EntryDate: trade.EntryDate.ToString("yyyy-MM-dd HH:mm"),
            ExitDate: exitDate.ToString("yyyy-MM-dd HH:mm"),
            Contract: trade.Contract,
            EntryPrice: Math.Round(trade.EntryNetPremium, 2),
            ExitPrice: Math.Round(exitValue, 2),
            PnL: Math.Round(pnl, 2),
            ExitReason: reason,
            StopLossPrice: Math.Round(trade.EntryNetPremium - risk.StopLossPoints, 2),
            Target1Price: Math.Round(trade.EntryNetPremium + risk.Target1Points, 2),
            Target2Price: Math.Round(trade.EntryNetPremium + risk.Target2Points, 2));
    }

    private static EntrySignalDecision EntrySignalFires(
        string strategy,
        string condition,
        int i,
        CandleData candle,
        CandleData prev,
        IReadOnlyList<RsiResult> rsi,
        IReadOnlyList<MacdResult> macd,
        IReadOnlyList<EmaResult> ema20,
        IReadOnlyList<SuperTrendResult> supertrend,
        DailyPivotLevels pivots,
        IReadOnlyDictionary<DateOnly, IReadOnlyDictionary<int, TrendFilterValue>> trendFilterByDay,
        bool adxFilterEnabled)
    {
        bool signalFired = condition switch
        {
            "RSI_Oversold"          => (rsi[i].Rsi ?? 50) < 30,
            "RSI_Overbought"        => (rsi[i].Rsi ?? 50) > 70,
            "MACD_Bullish_Cross"    => (macd[i].Macd ?? 0) > (macd[i].Signal ?? 0) && (macd[i - 1].Macd ?? 0) <= (macd[i - 1].Signal ?? 0),
            "MACD_Bearish_Cross"    => (macd[i].Macd ?? 0) < (macd[i].Signal ?? 0) && (macd[i - 1].Macd ?? 0) >= (macd[i - 1].Signal ?? 0),
            "SupertrendBullish"     => IsSupertrendBullish(supertrend[i]),
            "SupertrendBearish"     => !IsSupertrendBullish(supertrend[i]),
            "PriceBreakoutAboveR1"  => candle.Close > pivots.R1 && prev.Close <= pivots.R1,
            "PriceBreakdownBelowS1" => candle.Close < pivots.S1 && prev.Close >= pivots.S1,
            "PivotEma20Bullish"     => IsPivotEma20Bullish(i, candle, prev, ema20, pivots),
            "PivotEma20Bearish"     => IsPivotEma20Bearish(i, candle, prev, ema20, pivots),
            _ => false,
        };

        if (!signalFired)
            return EntrySignalDecision.NoSignal;

        if (adxFilterEnabled && !PassesAdxFilter(strategy, condition, i, candle, trendFilterByDay))
            return EntrySignalDecision.FilteredOut;

        return EntrySignalDecision.Accepted;
    }

    private static bool IsSupertrendBullish(SuperTrendResult r) => r.LowerBand.HasValue;

    private static bool PassesAdxFilter(
        string strategy,
        string condition,
        int index,
        CandleData candle,
        IReadOnlyDictionary<DateOnly, IReadOnlyDictionary<int, TrendFilterValue>> trendFilterByDay)
    {
        var day = ToIstDate(candle.Time);
        if (!trendFilterByDay.TryGetValue(day, out var trendFilterByIndex)
            || !trendFilterByIndex.TryGetValue(index, out var trendFilter))
        {
            return false;
        }

        if (trendFilter.Adx < 20m || trendFilter.Ema20 <= 0m)
            return false;

        return ResolveEntryBias(strategy, condition) switch
        {
            EntryBias.Bullish => candle.Close > trendFilter.Ema20,
            EntryBias.Bearish => candle.Close < trendFilter.Ema20,
            _ => true,
        };
    }

    private static EntryBias ResolveEntryBias(string strategy, string condition)
    {
        if (strategy is "LongCall" or "BullCallSpread")
            return EntryBias.Bullish;

        if (strategy is "LongPut" or "BearPutSpread")
            return EntryBias.Bearish;

        return condition switch
        {
            "RSI_Oversold" or "MACD_Bullish_Cross" or "SupertrendBullish"
                or "PriceBreakoutAboveR1" or "PivotEma20Bullish" => EntryBias.Bullish,
            "RSI_Overbought" or "MACD_Bearish_Cross" or "SupertrendBearish"
                or "PriceBreakdownBelowS1" or "PivotEma20Bearish" => EntryBias.Bearish,
            _ => EntryBias.Neutral,
        };
    }

    private static Dictionary<DateOnly, IReadOnlyDictionary<int, TrendFilterValue>> BuildTrendFilterLookup(
        IReadOnlyList<CandleData> candles,
        IReadOnlyList<AdxResult> adx14,
        IReadOnlyList<EmaResult> ema20)
    {
        var lookup = new Dictionary<DateOnly, Dictionary<int, TrendFilterValue>>();
        for (int i = 0; i < candles.Count; i++)
        {
            var adx = adx14[i].Adx;
            var ema = ema20[i].Ema;
            if (!adx.HasValue || !ema.HasValue)
                continue;

            var day = ToIstDate(candles[i].Time);
            if (!lookup.TryGetValue(day, out var dayCache))
            {
                dayCache = [];
                lookup[day] = dayCache;
            }

            dayCache[i] = new TrendFilterValue((decimal)adx.Value, (decimal)ema.Value);
        }

        return lookup.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyDictionary<int, TrendFilterValue>)kvp.Value);
    }

    private static bool IsPivotEma20Bullish(
        int i,
        CandleData candle,
        CandleData prev,
        IReadOnlyList<EmaResult> ema20,
        DailyPivotLevels pivots)
    {
        var ema = ema20[i].Ema;
        var prevEma = ema20[i - 1].Ema;
        if (!ema.HasValue || !prevEma.HasValue)
            return false;

        decimal currentEma = (decimal)ema.Value;
        decimal previousEma = (decimal)prevEma.Value;
        bool reclaimedEma = prev.Close <= previousEma && candle.Close > currentEma;
        bool reclaimedPivot = prev.Close <= pivots.Pivot && candle.Close > pivots.Pivot;
        bool bouncedFromSupport = candle.Low <= Math.Max(currentEma, pivots.Pivot) && candle.Close > candle.Open;

        return candle.Close > currentEma
            && candle.Close > pivots.Pivot
            && candle.Close < pivots.R1
            && (reclaimedEma || reclaimedPivot || bouncedFromSupport);
    }

    private static bool IsPivotEma20Bearish(
        int i,
        CandleData candle,
        CandleData prev,
        IReadOnlyList<EmaResult> ema20,
        DailyPivotLevels pivots)
    {
        var ema = ema20[i].Ema;
        var prevEma = ema20[i - 1].Ema;
        if (!ema.HasValue || !prevEma.HasValue)
            return false;

        decimal currentEma = (decimal)ema.Value;
        decimal previousEma = (decimal)prevEma.Value;
        bool lostEma = prev.Close >= previousEma && candle.Close < currentEma;
        bool lostPivot = prev.Close >= pivots.Pivot && candle.Close < pivots.Pivot;
        bool rejectedFromResistance = candle.High >= Math.Min(currentEma, pivots.Pivot) && candle.Close < candle.Open;

        return candle.Close < currentEma
            && candle.Close < pivots.Pivot
            && candle.Close > pivots.S1
            && (lostEma || lostPivot || rejectedFromResistance);
    }

    // SL and target checks use intraday candle extremes so a stop/target touched inside
    // a 15-minute bar is captured instead of waiting for the daily close.
    private static ExitDecision? CheckExitConditions(
        string selectedExit,
        OpenTrade trade,
        CandleData candle,
        BacktestRunRequest req,
        int lotSize)
    {
        var close = ValuePosition(trade, candle.Close, candle.Time);
        decimal closePnl = ToPnl(close.NetValue, trade, req.Lots, lotSize);
        decimal highPnl = ToPnl(ValuePosition(trade, candle.High, candle.Time).NetValue, trade, req.Lots, lotSize);
        decimal lowPnl = ToPnl(ValuePosition(trade, candle.Low, candle.Time).NetValue, trade, req.Lots, lotSize);
        decimal minPnl = Math.Min(closePnl, Math.Min(highPnl, lowPnl));
        decimal maxPnl = Math.Max(closePnl, Math.Max(highPnl, lowPnl));

        var risk = GetRiskSettings(trade, req);
        decimal slPnl = -risk.StopLossPoints * req.Lots * lotSize;
        decimal target1Pnl = risk.Target1Points * req.Lots * lotSize;
        decimal target2Pnl = risk.Target2Points * req.Lots * lotSize;

        if (minPnl <= slPnl)
            return FromThreshold(trade, "SLHit", slPnl, req.Lots, lotSize);

        if (selectedExit == "Target2Hit" && maxPnl >= target2Pnl)
            return FromThreshold(trade, "Target2Hit", target2Pnl, req.Lots, lotSize);

        if (selectedExit != "Target2Hit" && maxPnl >= target1Pnl)
            return FromThreshold(trade, "Target1Hit", target1Pnl, req.Lots, lotSize);

        if (close.DaysToExpiry <= 0)
            return new ExitDecision("ExpiryMinus1Day", close.NetValue, closePnl);

        return selectedExit switch
        {
            "ThetaDecay50Pct" when trade.EntryExtrinsic > 0 && close.NetValue - close.Intrinsic <= 0.5m * trade.EntryExtrinsic
                => new ExitDecision("ThetaDecay50Pct", close.NetValue, closePnl),
            "ExpiryMinus1Day" when close.DaysToExpiry <= 1
                => new ExitDecision("ExpiryMinus1Day", close.NetValue, closePnl),
            _ => null,
        };
    }

    private static decimal ToPnl(decimal netValue, OpenTrade trade, int lots, int lotSize) =>
        (netValue - trade.EntryNetPremium) * lots * lotSize;

    private static RiskSettings GetRiskSettings(OpenTrade trade, BacktestRunRequest req)
    {
        decimal stopLossPoints = req.StopLossPoints ?? StopLossPct * trade.RiskCapital;
        decimal target1Points = req.TargetPoints ?? Target1Pct * trade.RiskCapital;
        decimal target2Points = req.TargetPoints.HasValue
            ? req.TargetPoints.Value * 2m
            : Target2Pct * trade.RiskCapital;

        return new RiskSettings(stopLossPoints, target1Points, target2Points);
    }

    private static ExitDecision FromThreshold(OpenTrade trade, string reason, decimal pnl, int lots, int lotSize)
    {
        decimal exitValue = trade.EntryNetPremium + pnl / (lots * lotSize);
        return new ExitDecision(reason, exitValue, pnl);
    }

    private OpenTrade OpenPosition(string symbol, string strategy, StrategyLeg[] legs, CandleData entryCandle, int step, int lotSize)
    {
        decimal spot = entryCandle.Close;
        int atm = (int)Math.Round((double)spot / step) * step;
        var expiry = entryCandle.Time.AddDays(7);
        double T = Math.Max((expiry - entryCandle.Time).TotalDays, 1) / 365.0;

        var tradeLegs = new List<TradeLeg>();
        decimal netPremium = 0m;
        decimal netIntrinsic = 0m;

        foreach (var leg in legs)
        {
            int strike = atm + leg.StrikeOffset * step;
            bool isCall = leg.OptionType == "CE";
            decimal premium = (decimal)PriceOption((double)spot, strike, T, isCall);
            decimal intrinsic = isCall ? Math.Max(spot - strike, 0) : Math.Max(strike - spot, 0);
            decimal sign = leg.Action == "BUY" ? 1m : -1m;

            netPremium += sign * premium;
            netIntrinsic += sign * intrinsic;
            tradeLegs.Add(new TradeLeg(strike, leg.OptionType, leg.Action, premium));
        }

        decimal extrinsic = netPremium - netIntrinsic;
        decimal riskCapital = Math.Max(Math.Abs(netPremium), 1m);
        string contract = $"{symbol} {strategy} ({string.Join(" / ", tradeLegs.Select(l => $"{l.Action} {l.Strike}{l.OptionType}"))})";

        return new OpenTrade(entryCandle.Time, expiry, contract, tradeLegs, netPremium, extrinsic, riskCapital);
    }

    private static (decimal NetValue, decimal Intrinsic, double DaysToExpiry) ValuePosition(OpenTrade trade, decimal spot, DateTimeOffset now)
    {
        double daysToExpiry = Math.Max((trade.ExpiryDate - now).TotalDays, 0);
        double T = Math.Max(daysToExpiry, 0.01) / 365.0;

        decimal netValue = 0m;
        decimal netIntrinsic = 0m;

        foreach (var leg in trade.Legs)
        {
            bool isCall = leg.OptionType == "CE";
            decimal premium = (decimal)PriceOption((double)spot, leg.Strike, T, isCall);
            decimal intrinsic = isCall ? Math.Max(spot - leg.Strike, 0) : Math.Max(leg.Strike - spot, 0);
            decimal sign = leg.Action == "BUY" ? 1m : -1m;
            netValue += sign * premium;
            netIntrinsic += sign * intrinsic;
        }

        return (netValue, netIntrinsic, daysToExpiry);
    }

    // ------------------------------------------------------------------
    // Option pricing — Black-Scholes with a baseline IV adjusted for moneyness,
    // since there is no real historical option chain to derive IV from.
    // ------------------------------------------------------------------

    private static double EstimateIv(double spot, double strike, double T)
    {
        double moneyness = Math.Abs(spot - strike) / spot;
        double iv = BaselineIv + moneyness * 0.15 + (T < 0.03 ? 0.05 : 0);
        return Math.Max(0.05, iv);
    }

    private static double PriceOption(double spot, double strike, double T, bool isCall)
    {
        if (T <= 0)
            return isCall ? Math.Max(spot - strike, 0) : Math.Max(strike - spot, 0);

        double iv = EstimateIv(spot, strike, T);
        double sqrtT = Math.Sqrt(T);
        double d1 = (Math.Log(spot / strike) + (RiskFreeRate + 0.5 * iv * iv) * T) / (iv * sqrtT);
        double d2 = d1 - iv * sqrtT;
        double disc = Math.Exp(-RiskFreeRate * T);

        double price = isCall
            ? spot * NormCdf(d1) - strike * disc * NormCdf(d2)
            : strike * disc * NormCdf(-d2) - spot * NormCdf(-d1);

        return Math.Max(price, 0);
    }

    private static double NormCdf(double x)
    {
        const double a1 =  0.254829592;
        const double a2 = -0.284496736;
        const double a3 =  1.421413741;
        const double a4 = -1.453152027;
        const double a5 =  1.061405429;
        const double p  =  0.3275911;
        int sign = x < 0 ? -1 : 1;
        x = Math.Abs(x) / Math.Sqrt(2);
        double t = 1.0 / (1.0 + p * x);
        double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);
        return 0.5 * (1.0 + sign * y);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static DailyPivotLevels ComputeDailyPivots(CandleData prevDay)
    {
        decimal pivot = (prevDay.High + prevDay.Low + prevDay.Close) / 3m;
        decimal r1 = 2m * pivot - prevDay.Low;
        decimal s1 = 2m * pivot - prevDay.High;
        return new DailyPivotLevels(pivot, r1, s1);
    }

    private static Dictionary<DateOnly, DailyPivotLevels> BuildPivotLookup(IReadOnlyList<CandleData> daily)
    {
        var lookup = new Dictionary<DateOnly, DailyPivotLevels>();
        for (int i = 1; i < daily.Count; i++)
            lookup[ToIstDate(daily[i].Time)] = ComputeDailyPivots(daily[i - 1]);

        return lookup;
    }

    // Resamples the 15-minute mock candles into daily OHLCV bars suitable for
    // multi-day swing-style options strategies.
    private static List<CandleData> ToDailyCandles(IReadOnlyList<CandleData> intraday)
    {
        return intraday
            .OrderBy(c => c.Time)
            .GroupBy(c => TimeZoneInfo.ConvertTimeFromUtc(c.Time.UtcDateTime, IstZone).Date)
            .Select(g =>
            {
                var ordered = g.OrderBy(c => c.Time).ToList();
                return new CandleData(
                    Time: ordered[0].Time,
                    Open: ordered[0].Open,
                    High: ordered.Max(c => c.High),
                    Low: ordered.Min(c => c.Low),
                    Close: ordered[^1].Close,
                    Volume: ordered.Sum(c => c.Volume));
            })
            .OrderBy(c => c.Time)
            .ToList();
    }

    private static DateOnly ToIstDate(DateTimeOffset time) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(time.UtcDateTime, IstZone));

    private static Quote ToQuote(CandleData c) => new()
    {
        Date = c.Time.UtcDateTime,
        Open = c.Open,
        High = c.High,
        Low = c.Low,
        Close = c.Close,
        Volume = c.Volume,
    };

    private record BacktestStats(
        decimal WinRate, decimal NetPnl, decimal MaxDrawdown,
        decimal SharpeRatio, decimal ProfitFactor, decimal AvgWin, decimal AvgLoss);

    private static BacktestStats ComputeStats(IReadOnlyList<BacktestTradeLogEntry> trades)
    {
        if (trades.Count == 0)
            return new BacktestStats(0, 0, 0, 0, 0, 0, 0);

        var wins = trades.Where(t => t.PnL > 0).ToList();
        var losses = trades.Where(t => t.PnL < 0).ToList();

        decimal winRate = Math.Round((decimal)wins.Count / trades.Count * 100, 2);
        decimal netPnl = trades.Sum(t => t.PnL);
        decimal grossProfit = wins.Sum(t => t.PnL);
        decimal grossLoss = Math.Abs(losses.Sum(t => t.PnL));
        decimal profitFactor = grossLoss > 0 ? Math.Round(grossProfit / grossLoss, 2) : (grossProfit > 0 ? 99.99m : 0m);
        decimal avgWin = wins.Count > 0 ? Math.Round(grossProfit / wins.Count, 2) : 0;
        decimal avgLoss = losses.Count > 0 ? Math.Round(grossLoss / losses.Count, 2) * -1 : 0;

        decimal cumulative = 0, peak = 0, maxDrawdown = 0;
        foreach (var t in trades)
        {
            cumulative += t.PnL;
            if (cumulative > peak) peak = cumulative;
            decimal drawdown = peak - cumulative;
            if (drawdown > maxDrawdown) maxDrawdown = drawdown;
        }

        double mean = (double)trades.Average(t => t.PnL);
        double variance = trades.Count > 1
            ? trades.Sum(t => Math.Pow((double)t.PnL - mean, 2)) / (trades.Count - 1)
            : 0;
        double stdDev = Math.Sqrt(variance);
        // Per-trade Sharpe (not annualised) — valid for comparing
        // strategies run over the same period. Positive > 0 is good,
        // > 1.0 is excellent for options strategies.
        decimal sharpe = stdDev > 0 ? Math.Round((decimal)(mean / stdDev), 2) : 0;

        return new BacktestStats(winRate, Math.Round(netPnl, 2), Math.Round(maxDrawdown, 2), sharpe, profitFactor, avgWin, avgLoss);
    }

    private static BacktestResultResponse ToResponse(Domain.Entities.BacktestResult r)
    {
        var p = r.Parameters?.RootElement;
        string symbol         = TryGetString(p, "symbol");
        string entryCondition = TryGetString(p, "entryCondition");
        string exitCondition  = TryGetString(p, "exitCondition");
        int periodDays        = TryGetInt(p, "periodDays");
        int lots              = TryGetInt(p, "lots");
        string dataSource     = TryGetString(p, "dataSource");
        int candleCount       = TryGetInt(p, "candleCount");
        int tradingDays       = TryGetInt(p, "tradingDays");
        decimal? targetPoints = TryGetNullableDecimal(p, "targetPoints");
        decimal? stopLossPoints = TryGetNullableDecimal(p, "stopLossPoints");
        var diagnosticSummary = TryGetDiagnosticSummary(p, "diagnosticSummary");

        var tradeLog = r.TradeLog is not null
            ? JsonSerializer.Deserialize<List<BacktestTradeLogEntry>>(r.TradeLog.RootElement.GetRawText()) ?? []
            : [];

        // AvgWin/AvgLoss aren't persisted columns — rederive them from the trade log
        var stats = ComputeStats(tradeLog);

        return new BacktestResultResponse(
            r.Id, symbol, r.Strategy, entryCondition, exitCondition, periodDays, lots,
            r.WinRate, r.TotalTrades, r.NetPnl, r.MaxDrawdown, r.SharpeRatio, r.ProfitFactor,
            stats.AvgWin, stats.AvgLoss, string.IsNullOrEmpty(dataSource) ? "unknown" : dataSource,
            candleCount, tradingDays, targetPoints, stopLossPoints, diagnosticSummary, tradeLog, r.CreatedAt.ToString("O"));
    }

    private string GetDataSource() =>
        config.GetValue<bool>("Groww:Enabled") ? "groww" : "mock";

    private async Task<int> CleanupExpiredHistoryAsync(Guid userId, CancellationToken ct)
    {
        int retentionDays = GetHistoryRetentionDays();
        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);
        int deleted = await db.BacktestResults
            .Where(b => b.UserId == userId && b.CreatedAt < cutoff)
            .ExecuteDeleteAsync(ct);

        if (deleted > 0)
        {
            logger.LogInformation(
                "Deleted {Count} expired backtest runs for {UserId}; retention {RetentionDays} days",
                deleted,
                userId,
                retentionDays);
        }

        return deleted;
    }

    private int GetHistoryRetentionDays() =>
        Math.Clamp(config.GetValue("Backtest:HistoryRetentionDays", 30), 1, 365);

    private static string TryGetString(JsonElement? element, string property) =>
        element?.TryGetProperty(property, out var v) == true ? v.GetString() ?? "" : "";

    private static int TryGetInt(JsonElement? element, string property) =>
        element?.TryGetProperty(property, out var v) == true ? v.GetInt32() : 0;

    private static decimal? TryGetNullableDecimal(JsonElement? element, string property) =>
        element?.TryGetProperty(property, out var v) == true && v.ValueKind == JsonValueKind.Number
            ? v.GetDecimal()
            : null;

    private static BacktestDiagnosticSummary? TryGetDiagnosticSummary(JsonElement? element, string property)
    {
        if (element?.TryGetProperty(property, out var v) != true || v.ValueKind != JsonValueKind.Object)
            return null;

        return new BacktestDiagnosticSummary(
            CandidateSignals: TryGetInt(v, "candidateSignals"),
            FilteredOut: TryGetInt(v, "filteredOut"),
            TradesEntered: TryGetInt(v, "tradesEntered"),
            TargetHits: TryGetInt(v, "targetHits"),
            SlHits: TryGetInt(v, "slHits"),
            ExpiryExits: TryGetInt(v, "expiryExits"),
            ThetaExits: TryGetInt(v, "thetaExits"));
    }

    private static int TryGetInt(JsonElement element, string property) =>
        element.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

    private static TimeZoneInfo GetIstZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
    }

    private record StrategyLeg(int StrikeOffset, string OptionType, string Action);
    private record TradeLeg(int Strike, string OptionType, string Action, decimal EntryPremium);
    private record DailyPivotLevels(decimal Pivot, decimal R1, decimal S1);
    private record ExitDecision(string Reason, decimal ExitValue, decimal PnL);
    private record RiskSettings(decimal StopLossPoints, decimal Target1Points, decimal Target2Points);
    private record TrendFilterValue(decimal Adx, decimal Ema20);
    private record SimulationResult(IReadOnlyList<BacktestTradeLogEntry> Trades, BacktestDiagnosticSummary Diagnostics);

    private enum EntrySignalDecision { NoSignal, FilteredOut, Accepted }
    private enum EntryBias { Neutral, Bullish, Bearish }

    private sealed class BacktestDiagnosticCounters
    {
        public int CandidateSignals { get; set; }
        public int FilteredOut { get; set; }
        public int TradesEntered { get; set; }
        public int TargetHits { get; set; }
        public int SlHits { get; set; }
        public int ExpiryExits { get; set; }
        public int ThetaExits { get; set; }

        public void CountExit(string reason)
        {
            if (reason is "Target1Hit" or "Target2Hit")
                TargetHits++;
            else if (reason == "SLHit")
                SlHits++;
            else if (reason is "ExpiryMinus1Day" or "EndOfPeriod")
                ExpiryExits++;
            else if (reason == "ThetaDecay50Pct")
                ThetaExits++;
        }

        public BacktestDiagnosticSummary ToSummary() => new(
            CandidateSignals,
            FilteredOut,
            TradesEntered,
            TargetHits,
            SlHits,
            ExpiryExits,
            ThetaExits);
    }

    private record OpenTrade(
        DateTimeOffset EntryDate,
        DateTimeOffset ExpiryDate,
        string Contract,
        IReadOnlyList<TradeLeg> Legs,
        decimal EntryNetPremium,
        decimal EntryExtrinsic,
        decimal RiskCapital);
}
