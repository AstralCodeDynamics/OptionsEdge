using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Skender.Stock.Indicators;
using OptionsEdge.API.Common.Constants;
using OptionsEdge.API.Infrastructure.Data;
using OptionsEdge.API.Infrastructure.MockData;

namespace OptionsEdge.API.Features.Backtest;

// Simulates options strategies over the mock 90-day OHLCV history, evaluating one
// entry signal and one exit signal per run (plus always-on SL/Target risk management,
// since a strategy without a stop would never close within the test window).
// Option premiums are estimated with Black-Scholes using a baseline IV + moneyness skew —
// there is no real historical option chain to replay against.
public class BacktestService(IMarketDataService marketData, AppDbContext db, ILogger<BacktestService> logger)
{
    private const double RiskFreeRate = 0.065;
    private const double BaselineIv = 0.14;
    private const int MinWarmupDays = 35;

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
         "SupertrendBullish", "SupertrendBearish", "PriceBreakoutAboveR1", "PriceBreakdownBelowS1"];

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

        int lotSize = symbol == "BANKNIFTY" ? AppConstants.LotSizes.BankNifty : AppConstants.LotSizes.Nifty;
        int step = StrikeStep[symbol];

        var daily = ToDailyCandles(marketData.GetCandles(symbol));
        if (daily.Count < MinWarmupDays + 5)
            throw new InvalidOperationException("Not enough historical data to run a backtest");

        var trades = Simulate(daily, req, legs, symbol, step, lotSize);

        var parameters = JsonSerializer.SerializeToDocument(new
        {
            symbol,
            strategy = req.Strategy,
            entryCondition = req.EntryCondition,
            exitCondition = req.ExitCondition,
            periodDays = req.PeriodDays,
            lots = req.Lots,
        });
        var stats = ComputeStats(trades);

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

    public async Task<IReadOnlyList<BacktestResultResponse>> GetHistoryAsync(Guid userId, int limit = 20, CancellationToken ct = default)
    {
        var rows = await db.BacktestResults
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Take(Math.Min(limit, 100))
            .ToListAsync(ct);

        return rows.Select(ToResponse).ToList();
    }

    // ------------------------------------------------------------------
    // Simulation
    // ------------------------------------------------------------------

    private List<BacktestTradeLogEntry> Simulate(
        IReadOnlyList<CandleData> daily, BacktestRunRequest req, StrategyLeg[] legs, string symbol, int step, int lotSize)
    {
        var quotes = daily.Select(ToQuote).ToList();
        var rsi = quotes.GetRsi(14).ToList();
        var macd = quotes.GetMacd(12, 26, 9).ToList();
        var supertrend = quotes.GetSuperTrend(10, 3).ToList();

        int testWindow = Math.Min(req.PeriodDays, daily.Count - MinWarmupDays);
        int startIndex = Math.Max(MinWarmupDays, daily.Count - testWindow);

        var trades = new List<BacktestTradeLogEntry>();
        OpenTrade? open = null;

        for (int i = startIndex; i < daily.Count; i++)
        {
            var candle = daily[i];
            var prev = daily[i - 1];
            var (_, r1, s1) = ComputeDailyPivots(prev);

            if (open is null)
            {
                if (EntrySignalFires(req.EntryCondition, i, candle, prev, rsi, macd, supertrend, r1, s1))
                    open = OpenPosition(symbol, req.Strategy, legs, candle, step, lotSize);
            }
            else
            {
                var (netValue, intrinsic, daysToExpiry) = ValuePosition(open, candle.Close, candle.Time);
                decimal pnl = (netValue - open.EntryNetPremium) * req.Lots * lotSize;
                decimal extrinsic = netValue - intrinsic;

                var exitReason = CheckExitConditions(req.ExitCondition, open, pnl, extrinsic, daysToExpiry);
                if (exitReason is not null)
                {
                    trades.Add(ToTradeLogEntry(open, candle.Time, netValue, pnl, exitReason));
                    open = null;
                }
            }
        }

        if (open is not null)
        {
            var last = daily[^1];
            var (netValue, _, _) = ValuePosition(open, last.Close, last.Time);
            decimal pnl = (netValue - open.EntryNetPremium) * req.Lots * lotSize;
            trades.Add(ToTradeLogEntry(open, last.Time, netValue, pnl, "EndOfPeriod"));
        }

        return trades;
    }

    private static BacktestTradeLogEntry ToTradeLogEntry(OpenTrade trade, DateTimeOffset exitDate, decimal exitValue, decimal pnl, string reason) =>
        new(
            EntryDate: trade.EntryDate.ToString("yyyy-MM-dd"),
            ExitDate: exitDate.ToString("yyyy-MM-dd"),
            Contract: trade.Contract,
            EntryPrice: Math.Round(trade.EntryNetPremium, 2),
            ExitPrice: Math.Round(exitValue, 2),
            PnL: Math.Round(pnl, 2),
            ExitReason: reason);

    private static bool EntrySignalFires(
        string condition, int i, CandleData candle, CandleData prev,
        IReadOnlyList<RsiResult> rsi, IReadOnlyList<MacdResult> macd, IReadOnlyList<SuperTrendResult> supertrend,
        decimal r1, decimal s1) => condition switch
    {
        "RSI_Oversold"         => (rsi[i].Rsi ?? 50) < 30,
        "RSI_Overbought"       => (rsi[i].Rsi ?? 50) > 70,
        "MACD_Bullish_Cross"   => (macd[i].Macd ?? 0) > (macd[i].Signal ?? 0) && (macd[i - 1].Macd ?? 0) <= (macd[i - 1].Signal ?? 0),
        "MACD_Bearish_Cross"   => (macd[i].Macd ?? 0) < (macd[i].Signal ?? 0) && (macd[i - 1].Macd ?? 0) >= (macd[i - 1].Signal ?? 0),
        "SupertrendBullish"    => IsSupertrendBullish(supertrend[i]) && !IsSupertrendBullish(supertrend[i - 1]),
        "SupertrendBearish"    => !IsSupertrendBullish(supertrend[i]) && IsSupertrendBullish(supertrend[i - 1]),
        "PriceBreakoutAboveR1" => candle.Close > r1 && prev.Close <= r1,
        "PriceBreakdownBelowS1" => candle.Close < s1 && prev.Close >= s1,
        _ => false,
    };

    private static bool IsSupertrendBullish(SuperTrendResult r) => r.LowerBand.HasValue;

    // SL/Target are always-on risk management sized as a percentage of entry risk capital —
    // without them a strategy could ride out the whole test window without ever closing.
    // The user-selected exit condition is checked as an additional early-exit trigger.
    private static string? CheckExitConditions(string selectedExit, OpenTrade trade, decimal pnl, decimal extrinsic, double daysToExpiry)
    {
        if (pnl <= -0.5m * trade.RiskCapital) return "SLHit";
        if (pnl >= 1.5m * trade.RiskCapital) return "Target2Hit";
        if (pnl >= 1.0m * trade.RiskCapital) return "Target1Hit";
        if (daysToExpiry <= 0) return "ExpiryMinus1Day";

        return selectedExit switch
        {
            "ThetaDecay50Pct" when trade.EntryExtrinsic > 0 && extrinsic <= 0.5m * trade.EntryExtrinsic => "ThetaDecay50Pct",
            "ExpiryMinus1Day" when daysToExpiry <= 1 => "ExpiryMinus1Day",
            _ => null,
        };
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

    private static (decimal Pivot, decimal R1, decimal S1) ComputeDailyPivots(CandleData prevDay)
    {
        decimal pivot = (prevDay.High + prevDay.Low + prevDay.Close) / 3m;
        decimal r1 = 2m * pivot - prevDay.Low;
        decimal s1 = 2m * pivot - prevDay.High;
        return (pivot, r1, s1);
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
        decimal sharpe = stdDev > 0 ? Math.Round((decimal)(mean / stdDev * Math.Sqrt(trades.Count)), 2) : 0;

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

        var tradeLog = r.TradeLog is not null
            ? JsonSerializer.Deserialize<List<BacktestTradeLogEntry>>(r.TradeLog.RootElement.GetRawText()) ?? []
            : [];

        // AvgWin/AvgLoss aren't persisted columns — rederive them from the trade log
        var stats = ComputeStats(tradeLog);

        return new BacktestResultResponse(
            r.Id, symbol, r.Strategy, entryCondition, exitCondition, periodDays, lots,
            r.WinRate, r.TotalTrades, r.NetPnl, r.MaxDrawdown, r.SharpeRatio, r.ProfitFactor,
            stats.AvgWin, stats.AvgLoss, tradeLog, r.CreatedAt.ToString("O"));
    }

    private static string TryGetString(JsonElement? element, string property) =>
        element?.TryGetProperty(property, out var v) == true ? v.GetString() ?? "" : "";

    private static int TryGetInt(JsonElement? element, string property) =>
        element?.TryGetProperty(property, out var v) == true ? v.GetInt32() : 0;

    private static TimeZoneInfo GetIstZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
    }

    private record StrategyLeg(int StrikeOffset, string OptionType, string Action);
    private record TradeLeg(int Strike, string OptionType, string Action, decimal EntryPremium);

    private record OpenTrade(
        DateTimeOffset EntryDate,
        DateTimeOffset ExpiryDate,
        string Contract,
        IReadOnlyList<TradeLeg> Legs,
        decimal EntryNetPremium,
        decimal EntryExtrinsic,
        decimal RiskCapital);
}
