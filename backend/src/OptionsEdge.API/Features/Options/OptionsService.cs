using OptionsEdge.API.Infrastructure.MockData;

namespace OptionsEdge.API.Features.Options;

public class OptionsService(MockMarketDataService mockData)
{
    private static readonly TimeZoneInfo IstZone = GetIstZone();
    private const double RiskFreeRate = 0.065; // 6.5% India risk-free rate

    // Strike intervals per symbol
    private static readonly Dictionary<string, int> StrikeStep = new()
    {
        ["NIFTY"]     = 50,
        ["BANKNIFTY"] = 100,
    };

    public IReadOnlyList<string> GetExpiries(string symbol)
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstZone);
        var expiries = new List<DateOnly>();

        // Next 4 weekly Thursdays
        var date = DateOnly.FromDateTime(now.Date);
        int added = 0;
        for (int i = 1; i <= 40 && added < 4; i++)
        {
            var d = date.AddDays(i);
            if (d.DayOfWeek == DayOfWeek.Thursday)
            {
                expiries.Add(d);
                added++;
            }
        }

        // Next 2 monthly (last Thursday of next 2 months)
        for (int m = 1; m <= 3 && expiries.Count < 6; m++)
        {
            var monthDate = date.AddMonths(m);
            var lastThursday = LastThursdayOfMonth(monthDate.Year, monthDate.Month);
            if (!expiries.Contains(lastThursday))
                expiries.Add(lastThursday);
        }

        return expiries.Order().Select(d => d.ToString("yyyy-MM-dd")).ToList();
    }

    public OptionsChainResponse GetChain(string symbol, string expiry)
    {
        var key = symbol.ToUpper();
        var snapshot = mockData.GetSnapshot(key);
        decimal spot = snapshot.Ltp;

        if (!DateOnly.TryParse(expiry, out var expiryDate))
            expiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

        int step = StrikeStep.TryGetValue(key, out var s) ? s : 50;
        int atm  = (int)Math.Round((double)spot / step) * step;

        var rows = new List<ChainRowResponse>();
        long totalCeOi = 0, totalPeOi = 0;

        for (int i = -5; i <= 5; i++)
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

            long ceOi = GenerateOi(key, strike, atm, step, isAtm, true);
            long peOi = GenerateOi(key, strike, atm, step, isAtm, false);
            totalCeOi += ceOi;
            totalPeOi += peOi;

            rows.Add(new ChainRowResponse(
                Strike: strike,
                IsAtm:  isAtm,
                Ce: new OptionLegResponse(
                    Ltp:      Math.Round((decimal)ceLtp, 2),
                    Oi:       ceOi,
                    OiChange: (long)(ceOi * (new Random(strike).NextDouble() * 0.1 - 0.05)),
                    Volume:   (long)(ceOi * 0.3 * (0.5 + new Random(strike + 1).NextDouble())),
                    Iv:       Math.Round(iv * 100, 2),
                    Delta:    Math.Round(ceGreeks.delta, 4),
                    Gamma:    Math.Round(ceGreeks.gamma, 6),
                    Theta:    Math.Round(ceGreeks.theta, 2),
                    Vega:     Math.Round(ceGreeks.vega, 2)),
                Pe: new OptionLegResponse(
                    Ltp:      Math.Round((decimal)peLtp, 2),
                    Oi:       peOi,
                    OiChange: (long)(peOi * (new Random(strike + 2).NextDouble() * 0.1 - 0.05)),
                    Volume:   (long)(peOi * 0.3 * (0.5 + new Random(strike + 3).NextDouble())),
                    Iv:       Math.Round(iv * 100, 2),
                    Delta:    Math.Round(peGreeks.delta, 4),
                    Gamma:    Math.Round(peGreeks.gamma, 6),
                    Theta:    Math.Round(peGreeks.theta, 2),
                    Vega:     Math.Round(peGreeks.vega, 2))));
        }

        decimal pcr  = totalCeOi > 0 ? Math.Round((decimal)totalPeOi / totalCeOi, 2) : 1m;
        decimal maxPain = ComputeMaxPain(rows);

        return new OptionsChainResponse(key, expiry, spot, pcr, maxPain, rows);
    }

    public MaxPainResponse GetMaxPain(string symbol, string expiry)
    {
        var chain = GetChain(symbol, expiry);
        return new MaxPainResponse(chain.MaxPain, chain.Spot, expiry);
    }

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

    private static double DaysToExpiry(DateOnly expiry)
    {
        DateTime istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstZone);
        var expiryDt = new DateTime(expiry.Year, expiry.Month, expiry.Day, 15, 30, 0);
        double days = (expiryDt - istNow).TotalDays;
        return Math.Max(days, 0.001);
    }

    private static DateOnly LastThursdayOfMonth(int year, int month)
    {
        var lastDay = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        int daysBack = ((int)lastDay.DayOfWeek - (int)DayOfWeek.Thursday + 7) % 7;
        return lastDay.AddDays(-daysBack);
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
