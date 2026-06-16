using Microsoft.Extensions.Caching.Memory;
using OptionsEdge.API.Common.Constants;
using OptionsEdge.API.Infrastructure.Background;
using OptionsEdge.API.Infrastructure.Groww;
using OptionsEdge.API.Infrastructure.MockData;

namespace OptionsEdge.API.Features.Options;

public class OptionsService(IMarketDataService marketData, IMemoryCache cache)
{
    private static readonly TimeZoneInfo IstZone = GetIstZone();
    private const double RiskFreeRate = 0.065; // 6.5% India risk-free rate

    // Strike intervals per symbol
    private static readonly Dictionary<string, int> StrikeStep = new()
    {
        ["NIFTY"]     = 50,
        ["BANKNIFTY"] = 100,
    };

    private static string GrowwChainCacheKey(string symbol, string expiry) =>
        $"groww_chain:{symbol.ToUpperInvariant()}:{expiry}";

    // Called by the /chain/{symbol} endpoint after a live Groww option-chain fetch, so GetChain
    // can overlay real OI/IV/LTP onto the Black-Scholes-simulated chain below.
    public void CacheGrowwChain(string symbol, string expiry, IReadOnlyList<GrowwOptionChainRow> chain)
    {
        var ttl = MarketHoursHelper.IsMarketOpen() ? TimeSpan.FromSeconds(30) : TimeSpan.FromMinutes(5);
        cache.Set(GrowwChainCacheKey(symbol, expiry), chain, ttl);
    }

    public IReadOnlyList<string> GetExpiries(string symbol)
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstZone);
        var date = DateOnly.FromDateTime(now.Date);
        var expiries = new List<DateOnly>();

        // NIFTY: next 4 weekly Tuesdays (NSE moved weekly expiry Thursday -> Tuesday, Sep 2025).
        // BANKNIFTY: weekly contracts discontinued Nov 2024 — monthly only, so skip this block.
        if (symbol.ToUpperInvariant() == "NIFTY")
        {
            int added = 0;
            for (int i = 0; i <= 40 && added < 4; i++)
            {
                var d = date.AddDays(i);
                if (d.DayOfWeek != DayOfWeek.Tuesday)
                    continue;
                // Include today only if market hasn't closed (before 15:30 IST)
                if (i == 0 && now.TimeOfDay >= new TimeSpan(15, 30, 0))
                    continue;
                expiries.Add(d);
                added++;
            }
        }

        // Monthly expiries (last Tuesday of month), starting this month — covers BANKNIFTY's
        // only contract series and NIFTY's monthly (which coincides with its last weekly).
        for (int m = 0; m <= 3 && expiries.Count < 6; m++)
        {
            var monthDate = date.AddMonths(m);
            var lastTuesday = GrowwSymbolHelper.LastTuesdayOfMonth(monthDate.Year, monthDate.Month);
            if (lastTuesday >= date && !expiries.Contains(lastTuesday))
                expiries.Add(lastTuesday);
        }

        return expiries.Order().Select(d => d.ToString("yyyy-MM-dd")).ToList();
    }

    public OptionsChainResponse GetChain(string symbol, string expiry)
    {
        var key = symbol.ToUpper();
        var snapshot = marketData.GetSnapshot(key);
        decimal spot = snapshot.Ltp;

        if (!DateOnly.TryParse(expiry, out var expiryDate))
            expiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

        int step = StrikeStep.TryGetValue(key, out var s) ? s : 50;
        int atm  = (int)Math.Round((double)spot / step) * step;

        cache.TryGetValue(GrowwChainCacheKey(key, expiry), out IReadOnlyList<GrowwOptionChainRow>? growwChain);

        // PCR/MaxPain from the FULL Groww chain — not limited to the displayed strike window.
        long fullTotalCeOi = 0, fullTotalPeOi = 0;
        if (growwChain is { Count: > 0 })
        {
            foreach (var r in growwChain)
            {
                fullTotalCeOi += r.Call?.OpenInterest ?? 0;
                fullTotalPeOi += r.Put?.OpenInterest ?? 0;
            }
        }

        var rows = new List<ChainRowResponse>();
        long totalCeOi = 0, totalPeOi = 0;

        for (int i = -10; i <= 10; i++)
        {
            int strike = atm + i * step;
            bool isAtm = i == 0;
            double T = DaysToExpiry(expiryDate) / 365.0;

            // IV: ATM base + smile effect (OTM/ITM higher IV)
            double baseIv = (double)snapshot.Vix / 100.0;
            double moneyness = Math.Abs((double)(spot - strike) / (double)spot);
            double iv = baseIv + moneyness * 0.15 + (T < 0.03 ? 0.05 : 0);
            iv = Math.Max(0.05, iv);

            var (ceGreeks, ceLtp) = BlackScholes((double)spot, strike, RiskFreeRate, T, iv, true);
            var (peGreeks, peLtp) = BlackScholes((double)spot, strike, RiskFreeRate, T, iv, false);

            long syntheticCeOi = GenerateOi(key, strike, atm, step, isAtm, true);
            long syntheticPeOi = GenerateOi(key, strike, atm, step, isAtm, false);

            // Overlay real OI/IV/LTP from the latest Groww option-chain fetch when available;
            // strikes Groww didn't return (or when Groww is disabled/unreachable) keep the
            // Black-Scholes-simulated values.
            var growwRow = growwChain?.FirstOrDefault(r => r.Strike == strike);

            long ceOi = growwRow?.Call?.OpenInterest ?? syntheticCeOi;
            long peOi = growwRow?.Put?.OpenInterest ?? syntheticPeOi;
            totalCeOi += ceOi;
            totalPeOi += peOi;

            decimal ceLtpFinal = growwRow?.Call?.Ltp ?? Math.Round((decimal)ceLtp, 2);
            decimal peLtpFinal = growwRow?.Put?.Ltp ?? Math.Round((decimal)peLtp, 2);

            // Groww reports implied volatility as a percentage, same scale as iv * 100 below.
            double ceIv = growwRow?.Call is { } ceLeg ? (double)ceLeg.ImpliedVolatility : Math.Round(iv * 100, 2);
            double peIv = growwRow?.Put is { } peLeg ? (double)peLeg.ImpliedVolatility : Math.Round(iv * 100, 2);

            // OI change / volume: use Groww's real values when non-zero, else the synthetic estimate.
            long ceOiChange = growwRow?.Call?.OiChange is { } ceOiChangeVal && ceOiChangeVal != 0
                ? (long)ceOiChangeVal
                : (long)(ceOi * (new Random(strike).NextDouble() * 0.1 - 0.05));
            long peOiChange = growwRow?.Put?.OiChange is { } peOiChangeVal && peOiChangeVal != 0
                ? (long)peOiChangeVal
                : (long)(peOi * (new Random(strike + 2).NextDouble() * 0.1 - 0.05));

            long ceVolume = growwRow?.Call?.Volume is > 0
                ? growwRow.Call.Volume
                : (long)(ceOi * 0.3 * (0.5 + new Random(strike + 1).NextDouble()));
            long peVolume = growwRow?.Put?.Volume is > 0
                ? growwRow.Put.Volume
                : (long)(peOi * 0.3 * (0.5 + new Random(strike + 3).NextDouble()));

            rows.Add(new ChainRowResponse(
                Strike: strike,
                IsAtm:  isAtm,
                Ce: new OptionLegResponse(
                    Ltp:      ceLtpFinal,
                    Oi:       ceOi,
                    OiChange: ceOiChange,
                    Volume:   ceVolume,
                    Iv:       ceIv,
                    Delta:    Math.Round(ceGreeks.delta, 4),
                    Gamma:    Math.Round(ceGreeks.gamma, 6),
                    Theta:    Math.Round(ceGreeks.theta, 2),
                    Vega:     Math.Round(ceGreeks.vega, 2)),
                Pe: new OptionLegResponse(
                    Ltp:      peLtpFinal,
                    Oi:       peOi,
                    OiChange: peOiChange,
                    Volume:   peVolume,
                    Iv:       peIv,
                    Delta:    Math.Round(peGreeks.delta, 4),
                    Gamma:    Math.Round(peGreeks.gamma, 6),
                    Theta:    Math.Round(peGreeks.theta, 2),
                    Vega:     Math.Round(peGreeks.vega, 2))));
        }

        decimal pcr = fullTotalCeOi > 0
            ? Math.Round((decimal)fullTotalPeOi / fullTotalCeOi, 2)
            : (totalCeOi > 0 ? Math.Round((decimal)totalPeOi / totalCeOi, 2) : 1m);
        decimal maxPain = growwChain is { Count: > 0 }
            ? ComputeMaxPainFromGrowwChain(growwChain)
            : ComputeMaxPain(rows);

        return new OptionsChainResponse(key, expiry, spot, pcr, maxPain, rows);
    }

    public MaxPainResponse GetMaxPain(string symbol, string expiry)
    {
        var chain = GetChain(symbol, expiry);
        return new MaxPainResponse(chain.MaxPain, chain.Spot, expiry);
    }

    // Used by Position P&L, SL/target alerts, and PositionMonitorWorker — prefer the latest
    // Groww chain LTP (cached by /chain/{symbol}) over the Black-Scholes estimate when available.
    public decimal GetOptionLtp(string symbol, int strike, string optionType, string expiry)
    {
        var key = symbol.ToUpper();
        bool isCall = optionType.ToUpper() == "CE";

        if (cache.TryGetValue(GrowwChainCacheKey(key, expiry), out IReadOnlyList<GrowwOptionChainRow>? growwChain)
            && growwChain is not null)
        {
            var row = growwChain.FirstOrDefault(r => r.Strike == strike);
            decimal? realLtp = isCall ? row?.Call?.Ltp : row?.Put?.Ltp;
            if (realLtp is > 0)
                return realLtp.Value;
        }

        var snapshot = marketData.GetSnapshot(key);
        double spot  = (double)snapshot.Ltp;

        if (!DateOnly.TryParse(expiry, out var expiryDate))
            expiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

        double T         = DaysToExpiry(expiryDate) / 365.0;
        double baseIv    = (double)snapshot.Vix / 100.0;
        double moneyness = Math.Abs((spot - strike) / spot);
        double iv        = baseIv + moneyness * 0.15 + (T < 0.03 ? 0.05 : 0);
        iv = Math.Max(0.05, iv);

        var (_, price) = BlackScholes(spot, strike, RiskFreeRate, T, iv, isCall);
        return Math.Round((decimal)price, 2);
    }

    // Computes the at-expiry payoff curve for a multi-leg strategy. The underlying
    // can't trade below zero, so the only potentially-unbounded direction is the
    // upside — determined by the strategy's net call exposure beyond the highest strike.
    public PayoffResponse ComputePayoff(IReadOnlyList<PayoffLegRequest> legs)
    {
        if (legs.Count == 0)
            throw new ArgumentException("At least one leg is required");
        if (legs.Count > 10)
            throw new ArgumentException("Maximum 10 legs allowed per strategy.");

        var resolved = legs.Select(ResolveLeg).ToList();

        int minStrike = resolved.Min(l => l.Strike);
        int maxStrike = resolved.Max(l => l.Strike);
        int buffer = Math.Max(Math.Max(maxStrike - minStrike, minStrike / 5), 1);
        decimal low  = Math.Max(0, minStrike - buffer);
        decimal high = maxStrike + buffer;

        // The payoff is piecewise-linear with kinks only at strike prices, so including
        // every strike as an explicit grid point makes each segment truly linear —
        // which in turn makes the breakeven interpolation below exact rather than approximate.
        const int points = 60;
        decimal step = (high - low) / points;
        var prices = new SortedSet<decimal>();
        for (int i = 0; i <= points; i++)
            prices.Add(Math.Round(low + step * i, 2));
        foreach (var strike in resolved.Select(l => (decimal)l.Strike))
        {
            if (strike >= low && strike <= high)
                prices.Add(Math.Round(strike, 2));
        }

        var curve = prices
            .Select(price => new PayoffPoint(price, Math.Round(resolved.Sum(l => l.Sign * (Intrinsic(l, price) - l.Premium) * l.Quantity), 2)))
            .ToList();

        // Net call exposure determines the slope of the payoff curve as price → ∞;
        // put legs contribute zero slope there since their intrinsic value flattens to zero.
        decimal upperSlope = resolved.Where(l => l.IsCall).Sum(l => l.Sign * l.Quantity);
        bool maxProfitUnlimited = upperSlope > 0;
        bool maxLossUnlimited   = upperSlope < 0;

        decimal curveMax = curve.Max(p => p.Pnl);
        decimal curveMin = curve.Min(p => p.Pnl);

        return new PayoffResponse(
            curve,
            maxProfitUnlimited ? null : curveMax,
            maxProfitUnlimited,
            maxLossUnlimited ? null : curveMin,
            maxLossUnlimited,
            FindBreakevens(curve));
    }

    private static ResolvedLeg ResolveLeg(PayoffLegRequest leg)
    {
        var symbol = leg.Symbol.ToUpperInvariant();
        if (symbol is not ("NIFTY" or "BANKNIFTY"))
            throw new ArgumentException("Symbol must be NIFTY or BANKNIFTY");
        var optionType = leg.OptionType.ToUpperInvariant();
        if (optionType is not ("CE" or "PE"))
            throw new ArgumentException("OptionType must be CE or PE");
        var action = leg.Action.ToUpperInvariant();
        if (action is not ("BUY" or "SELL"))
            throw new ArgumentException("Action must be BUY or SELL");
        if (leg.Lots <= 0)
            throw new ArgumentException("Lots must be positive");
        if (leg.Premium < 0)
            throw new ArgumentException("Premium cannot be negative");

        int lotSize = symbol == "BANKNIFTY" ? AppConstants.LotSizes.BankNifty : AppConstants.LotSizes.Nifty;
        return new ResolvedLeg(
            Strike:   leg.Strike,
            IsCall:   optionType == "CE",
            Sign:     action == "BUY" ? 1m : -1m,
            Quantity: leg.Lots * lotSize,
            Premium:  leg.Premium);
    }

    private static decimal Intrinsic(ResolvedLeg leg, decimal price) =>
        leg.IsCall ? Math.Max(price - leg.Strike, 0) : Math.Max(leg.Strike - price, 0);

    private static List<decimal> FindBreakevens(IReadOnlyList<PayoffPoint> curve)
    {
        var breakevens = new List<decimal>();
        for (int i = 1; i < curve.Count; i++)
        {
            var prev = curve[i - 1];
            var cur  = curve[i];
            if (prev.Pnl == 0)
            {
                breakevens.Add(prev.Price);
            }
            else if ((prev.Pnl < 0 && cur.Pnl > 0) || (prev.Pnl > 0 && cur.Pnl < 0))
            {
                decimal t = -prev.Pnl / (cur.Pnl - prev.Pnl);
                breakevens.Add(Math.Round(prev.Price + t * (cur.Price - prev.Price), 2));
            }
        }
        if (curve[^1].Pnl == 0) breakevens.Add(curve[^1].Price);
        return breakevens;
    }

    private record ResolvedLeg(int Strike, bool IsCall, decimal Sign, int Quantity, decimal Premium);

    // ------------------------------------------------------------------
    private static ((double delta, double gamma, double theta, double vega) greeks, double price)
        BlackScholes(double S, double K, double r, double T, double sigma, bool isCall)
    {
        if (T <= 0)
        {
            double intrinsic = isCall ? Math.Max(S - K, 0) : Math.Max(K - S, 0);
            double d = isCall ? (S > K ? 1 : 0) : (S < K ? -1 : 0);
            return ((d, 0, 0, 0), intrinsic);
        }

        double sqrtT = Math.Sqrt(T);
        double d1 = (Math.Log(S / K) + (r + 0.5 * sigma * sigma) * T) / (sigma * sqrtT);
        double d2 = d1 - sigma * sqrtT;

        double nd1  = NormCdf(isCall ? d1 : -d1);
        double nd2  = NormCdf(isCall ? d2 : -d2);
        double npd1 = NormPdf(d1);
        double disc = Math.Exp(-r * T);

        double delta = isCall ? NormCdf(d1) : NormCdf(d1) - 1;
        double gamma = npd1 / (S * sigma * sqrtT);
        double theta = isCall
            ? (-S * npd1 * sigma / (2 * sqrtT) - r * K * disc * NormCdf(d2)) / 365.0
            : (-S * npd1 * sigma / (2 * sqrtT) + r * K * disc * NormCdf(-d2)) / 365.0;
        double vega  = S * npd1 * sqrtT / 100.0;

        double price = isCall
            ? S * NormCdf(d1) - K * disc * NormCdf(d2)
            : K * disc * NormCdf(-d2) - S * NormCdf(-d1);

        return ((delta, gamma, theta, vega), Math.Max(price, 0));
    }

    private static long GenerateOi(string symbol, int strike, int atm, int step, bool isAtm, bool isCe)
    {
        long baseOi = symbol == "NIFTY" ? 800_000L : 200_000L;
        int dist = Math.Abs(strike - atm) / step;

        // Heavier OI at ATM, decreases with distance
        double distFactor = Math.Exp(-0.4 * dist);

        // Round-number boost: every 10th strike (500 for NIFTY, 1000 for BANKNIFTY)
        bool isRound = symbol == "NIFTY" ? strike % 500 == 0 : strike % 1000 == 0;
        double roundFactor = isRound ? 2.5 : 1.0;

        // CE typically has more OI on higher strikes, PE on lower strikes
        double skew = isCe ? (strike >= atm ? 1.1 : 0.8) : (strike <= atm ? 1.1 : 0.8);

        // Seed random per strike for deterministic but varied values
        var rng = new Random(strike ^ (isCe ? 0xABCD : 0x1234));
        double noise = 0.75 + rng.NextDouble() * 0.5;

        return (long)(baseOi * distFactor * roundFactor * skew * noise);
    }

    private static decimal ComputeMaxPain(IReadOnlyList<ChainRowResponse> rows)
    {
        decimal maxPainStrike = rows[0].Strike;
        double minLoss = double.MaxValue;

        foreach (var pivot in rows)
        {
            double totalLoss = 0;
            foreach (var row in rows)
            {
                // CE writers loss if price > strike
                if (pivot.Strike > row.Strike)
                    totalLoss += (double)(pivot.Strike - row.Strike) * row.Ce.Oi;
                // PE writers loss if price < strike
                if (pivot.Strike < row.Strike)
                    totalLoss += (double)(row.Strike - pivot.Strike) * row.Pe.Oi;
            }
            if (totalLoss < minLoss)
            {
                minLoss = totalLoss;
                maxPainStrike = pivot.Strike;
            }
        }

        return maxPainStrike;
    }

    private static decimal ComputeMaxPainFromGrowwChain(IReadOnlyList<GrowwOptionChainRow> chain)
    {
        decimal maxPainStrike = chain[0].Strike;
        double minLoss = double.MaxValue;

        foreach (var pivot in chain)
        {
            double totalLoss = 0;
            foreach (var row in chain)
            {
                if (pivot.Strike > row.Strike)
                    totalLoss += (double)(pivot.Strike - row.Strike) * (row.Call?.OpenInterest ?? 0);
                if (pivot.Strike < row.Strike)
                    totalLoss += (double)(row.Strike - pivot.Strike) * (row.Put?.OpenInterest ?? 0);
            }
            if (totalLoss < minLoss)
            {
                minLoss = totalLoss;
                maxPainStrike = pivot.Strike;
            }
        }

        return maxPainStrike;
    }

    private static double DaysToExpiry(DateOnly expiry)
    {
        DateTime istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstZone);
        var expiryDt = new DateTime(expiry.Year, expiry.Month, expiry.Day, 15, 30, 0);
        double days = (expiryDt - istNow).TotalDays;
        return Math.Max(days, 0.001);
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

    private static double NormPdf(double x) =>
        Math.Exp(-0.5 * x * x) / Math.Sqrt(2.0 * Math.PI);

    private static TimeZoneInfo GetIstZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
    }
}
