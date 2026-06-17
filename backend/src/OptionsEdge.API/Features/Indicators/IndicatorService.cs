using OptionsEdge.API.Common.Constants;
using Skender.Stock.Indicators;
using OptionsEdge.API.Infrastructure.MockData;

namespace OptionsEdge.API.Features.Indicators;

public class IndicatorService(IMarketDataService marketData)
{
    private static readonly TimeZoneInfo IstZone = GetIstZone();

    public IndicatorsResponse GetIndicators(string symbol)
    {
        var key = symbol.ToUpper();
        var candles = marketData.GetCandles(key);
        var snapshot = marketData.GetSnapshot(key);
        decimal price = snapshot.Ltp;

        var quotes = candles
            .OrderBy(c => c.Time)
            .Select(c => new Quote
            {
                Date = c.Time.UtcDateTime,
                Open = c.Open,
                High = c.High,
                Low = c.Low,
                Close = c.Close,
                Volume = c.Volume
            })
            .ToList();

        return new IndicatorsResponse(
            Symbol: key,
            Rsi: ComputeRsi(quotes),
            Macd: ComputeMacd(quotes),
            BollingerBands: ComputeBollingerBands(quotes, price),
            Adx: ComputeAdx(quotes),
            Ema: ComputeEma(quotes, price),
            Supertrend: ComputeSupertrend(quotes, price),
            Pivots: ComputePivots(candles),
            Timestamp: DateTimeOffset.UtcNow);
    }

    // ------------------------------------------------------------------
    private static RsiResponse ComputeRsi(List<Quote> quotes)
    {
        var result = quotes.GetRsi(AppConstants.IndicatorThresholds.RsiPeriod).LastOrDefault(r => r.Rsi.HasValue);
        double value = result?.Rsi ?? 50;
        string signal = value >= AppConstants.IndicatorThresholds.RsiOverbought ? "Overbought"
            : value <= AppConstants.IndicatorThresholds.RsiOversold ? "Oversold" : "Neutral";
        return new RsiResponse(Math.Round(value, 2), signal);
    }

    private static MacdResponse ComputeMacd(List<Quote> quotes)
    {
        var results = quotes.GetMacd(
            AppConstants.IndicatorThresholds.MacdFastPeriod,
            AppConstants.IndicatorThresholds.MacdSlowPeriod,
            AppConstants.IndicatorThresholds.MacdSignalPeriod).ToList();
        var cur  = results.LastOrDefault(r => r.Macd.HasValue);
        var prev = results.Where(r => r.Macd.HasValue).TakeLast(2).FirstOrDefault();

        double macd      = cur?.Macd      ?? 0;
        double signal    = cur?.Signal    ?? 0;
        double histogram = cur?.Histogram ?? 0;

        double prevMacd   = prev?.Macd   ?? 0;
        double prevSignal = prev?.Signal ?? 0;
        bool isBullishCross = macd > signal && prevMacd <= prevSignal;

        return new MacdResponse(
            Math.Round(macd, 4),
            Math.Round(signal, 4),
            Math.Round(histogram, 4),
            isBullishCross);
    }

    private static BollingerBandsResponse ComputeBollingerBands(List<Quote> quotes, decimal price)
    {
        var result = quotes.GetBollingerBands(
            AppConstants.IndicatorThresholds.BollingerPeriod,
            AppConstants.IndicatorThresholds.BollingerStdDev).LastOrDefault(r => r.UpperBand.HasValue);
        double upper  = result?.UpperBand ?? (double)price * 1.02;
        double lower  = result?.LowerBand ?? (double)price * 0.98;
        double middle = result?.Sma       ?? (double)price;
        double width  = middle > 0 ? (upper - lower) / middle : 0;
        bool squeeze  = width < AppConstants.IndicatorThresholds.BollingerSqueezeBandwidth;

        return new BollingerBandsResponse(
            Math.Round(upper, 2), Math.Round(middle, 2), Math.Round(lower, 2), squeeze);
    }

    private static AdxResponse ComputeAdx(List<Quote> quotes)
    {
        var result = quotes.GetAdx(AppConstants.IndicatorThresholds.AdxPeriod).LastOrDefault(r => r.Adx.HasValue);
        double value    = result?.Adx ?? 20;
        string strength = value < AppConstants.IndicatorThresholds.AdxWeakThreshold ? "Weak"
            : value <= AppConstants.IndicatorThresholds.AdxStrongThreshold ? "Moderate" : "Strong";
        return new AdxResponse(Math.Round(value, 2), strength);
    }

    private static EmaResponse ComputeEma(List<Quote> quotes, decimal price)
    {
        double Get(int period)
        {
            var r = quotes.GetEma(period).LastOrDefault(e => e.Ema.HasValue);
            return r?.Ema ?? (double)price;
        }

        double ema9   = Math.Round(Get(AppConstants.IndicatorThresholds.Ema9Period),   2);
        double ema20  = Math.Round(Get(AppConstants.IndicatorThresholds.Ema20Period),  2);
        double ema50  = Math.Round(Get(AppConstants.IndicatorThresholds.Ema50Period),  2);
        double ema200 = Math.Round(Get(AppConstants.IndicatorThresholds.Ema200Period), 2);
        double p = (double)price;

        return new EmaResponse(ema9, ema20, ema50, ema200, p > ema20, p > ema50);
    }

    private static SupertrendResponse ComputeSupertrend(List<Quote> quotes, decimal price)
    {
        var result = quotes.GetSuperTrend(
            AppConstants.IndicatorThresholds.SupertrendPeriod,
            AppConstants.IndicatorThresholds.SupertrendMultiplier).LastOrDefault(r => r.SuperTrend.HasValue);
        double value    = (double)(result?.SuperTrend ?? price);
        bool isBullish  = result?.LowerBand.HasValue ?? price > (decimal)value;
        return new SupertrendResponse(Math.Round(value, 2), isBullish);
    }

    private static PivotLevelsResponse ComputePivots(IReadOnlyList<CandleData> candles)
    {
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstZone).Date;

        // Find the most recent complete trading day
        var prevDayCandles = candles
            .Where(c => TimeZoneInfo.ConvertTimeFromUtc(c.Time.UtcDateTime, IstZone).Date < today)
            .ToList();

        if (prevDayCandles.Count == 0) prevDayCandles = [.. candles];

        var lastDate = TimeZoneInfo.ConvertTimeFromUtc(
            prevDayCandles[^1].Time.UtcDateTime, IstZone).Date;

        var dayCandles = prevDayCandles
            .Where(c => TimeZoneInfo.ConvertTimeFromUtc(c.Time.UtcDateTime, IstZone).Date == lastDate)
            .ToList();

        decimal h = dayCandles.Max(c => c.High);
        decimal l = dayCandles.Min(c => c.Low);
        decimal c = dayCandles[^1].Close;

        decimal pivot = Round2((h + l + c) / 3m);
        decimal r1    = Round2(2m * pivot - l);
        decimal r2    = Round2(pivot + (h - l));
        decimal r3    = Round2(h + 2m * (pivot - l));
        decimal s1    = Round2(2m * pivot - h);
        decimal s2    = Round2(pivot - (h - l));
        decimal s3    = Round2(l - 2m * (h - pivot));

        return new PivotLevelsResponse(
            (double)r3, (double)r2, (double)r1, (double)pivot,
            (double)s1, (double)s2, (double)s3);
    }

    private static decimal Round2(decimal v) => Math.Round(v, 2);

    private static TimeZoneInfo GetIstZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
    }
}
