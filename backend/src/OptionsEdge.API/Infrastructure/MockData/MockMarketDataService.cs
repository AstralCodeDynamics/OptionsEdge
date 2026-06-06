using OptionsEdge.API.Infrastructure.Background;

namespace OptionsEdge.API.Infrastructure.MockData;

public record CandleData(DateTimeOffset Time, decimal Open, decimal High, decimal Low, decimal Close, long Volume);

public record MarketSnapshotData(
    string Symbol,
    decimal Ltp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal PreviousClose,
    decimal Change,
    decimal ChangePct,
    decimal Vix,
    decimal Pcr,
    decimal FiiFlow,
    decimal DiiFlow,
    DateTimeOffset Timestamp);

public class MockMarketDataService
{
    private static readonly TimeZoneInfo IstZone = GetIstZone();

    // Per-symbol config: (basePrice, minPrice, maxPrice, per15minVol)
    private static readonly Dictionary<string, (decimal Base, decimal Min, decimal Max, double Vol15)> Config = new()
    {
        ["NIFTY"]     = (24000m, 23000m, 25000m, 0.0019),
        ["BANKNIFTY"] = (51500m, 50000m, 53000m, 0.0025),
    };

    private readonly Dictionary<string, List<CandleData>> _candles = [];

    // Live-tick state — updated every 30s during market hours
    private readonly Dictionary<string, decimal> _livePrice = [];
    private readonly Dictionary<string, decimal> _dayOpen = [];
    private readonly Dictionary<string, decimal> _dayHigh = [];
    private readonly Dictionary<string, decimal> _dayLow = [];
    private decimal _vix = 14.2m;
    private decimal _pcr = 1.05m;
    private decimal _fiiFlow = 620m;
    private decimal _diiFlow = -380m;

    private readonly Random _rng = new(Environment.TickCount);
    private readonly object _lock = new();

    public MockMarketDataService()
    {
        foreach (var symbol in Config.Keys)
        {
            var candles = GenerateHistoricalCandles(symbol);
            _candles[symbol] = candles;

            var last = candles[^1];
            _livePrice[symbol] = last.Close;
            _dayOpen[symbol]   = candles.Where(c => c.Time.Date == last.Time.Date).First().Open;
            _dayHigh[symbol]   = candles.Where(c => c.Time.Date == last.Time.Date).Max(c => c.High);
            _dayLow[symbol]    = candles.Where(c => c.Time.Date == last.Time.Date).Min(c => c.Low);
        }
    }

    public IReadOnlyList<CandleData> GetCandles(string symbol)
    {
        var key = symbol.ToUpper();
        return Config.ContainsKey(key) ? _candles[key] : [];
    }

    public IReadOnlyList<MarketSnapshotData> GetSnapshots()
    {
        lock (_lock)
        {
            return Config.Keys.Select(BuildSnapshot).ToList();
        }
    }

    public MarketSnapshotData GetSnapshot(string symbol)
    {
        lock (_lock)
        {
            return BuildSnapshot(symbol.ToUpper());
        }
    }

    // Called by MarketDataWorker every 30s to advance the simulation
    public void Tick()
    {
        lock (_lock)
        {
            foreach (var (symbol, cfg) in Config)
            {
                // Per-30s vol ≈ per-15min vol / sqrt(30) (30 ticks per 15 min at 30s interval)
                double tickVol = cfg.Vol15 / Math.Sqrt(30);
                double drift = 0.0001 * (double)(cfg.Base - _livePrice[symbol]) / (double)cfg.Base;
                double ret = drift + NextGaussian() * tickVol;

                decimal newPrice = _livePrice[symbol] * (1m + (decimal)ret);
                newPrice = Math.Clamp(newPrice, cfg.Min, cfg.Max);
                newPrice = Math.Round(newPrice, symbol == "NIFTY" ? 2 : 2);
                _livePrice[symbol] = newPrice;

                if (newPrice > _dayHigh[symbol]) _dayHigh[symbol] = newPrice;
                if (newPrice < _dayLow[symbol])  _dayLow[symbol]  = newPrice;
            }

            // Update market-wide indicators
            _vix     = Clamp(_vix     + (decimal)(NextGaussian() * 0.05), 11m, 18m);
            _pcr     = Clamp(_pcr     + (decimal)(NextGaussian() * 0.008), 0.8m, 1.6m);
            _fiiFlow = Clamp(_fiiFlow + (decimal)(NextGaussian() * 30), -2000m, 2000m);
            _diiFlow = Clamp(_diiFlow + (decimal)(NextGaussian() * 20), -1500m, 1500m);
        }
    }

    // ------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------

    private MarketSnapshotData BuildSnapshot(string symbol)
    {
        var ltp      = _livePrice[symbol];
        var prevClose = _candles[symbol][^1].Close;
        var change    = Math.Round(ltp - prevClose, 2);
        var changePct = Math.Round(change / prevClose * 100, 2);

        return new MarketSnapshotData(
            Symbol:       symbol,
            Ltp:          ltp,
            Open:         _dayOpen[symbol],
            High:         _dayHigh[symbol],
            Low:          _dayLow[symbol],
            PreviousClose: prevClose,
            Change:       change,
            ChangePct:    changePct,
            Vix:          Math.Round(_vix, 2),
            Pcr:          Math.Round(_pcr, 2),
            FiiFlow:      Math.Round(_fiiFlow, 0),
            DiiFlow:      Math.Round(_diiFlow, 0),
            Timestamp:    DateTimeOffset.UtcNow);
    }

    private List<CandleData> GenerateHistoricalCandles(string symbol)
    {
        var cfg = Config[symbol];
        var candles = new List<CandleData>(2000);

        // Start 130 calendar days back to get ~90 trading days
        var istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstZone);
        var startDate = istNow.Date.AddDays(-130);

        decimal price = cfg.Base + (decimal)(NextGaussian() * (double)cfg.Base * 0.005);

        for (var date = startDate; date < istNow.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;

            // Small overnight gap
            double gap = NextGaussian() * 0.0015;
            price = Math.Clamp(price * (1m + (decimal)gap), cfg.Min, cfg.Max);

            decimal dayOpen = price;
            decimal dayHigh = price;
            decimal dayLow  = price;

            // 25 bars: 9:15 to 15:15 (inclusive), each 15 min
            var barTime = date.AddHours(9).AddMinutes(15);
            for (int bar = 0; bar < 25; bar++, barTime = barTime.AddMinutes(15))
            {
                decimal open = price;

                double ret = NextGaussian() * cfg.Vol15;
                // Mild mean-reversion
                ret += 0.00005 * (double)(cfg.Base - price) / (double)cfg.Base;

                decimal close = Math.Clamp(open * (1m + (decimal)ret), cfg.Min, cfg.Max);

                decimal noise1 = (decimal)Math.Abs(NextGaussian()) * (decimal)cfg.Vol15 * 0.4m;
                decimal noise2 = (decimal)Math.Abs(NextGaussian()) * (decimal)cfg.Vol15 * 0.4m;
                decimal high   = Math.Max(open, close) * (1m + noise1);
                decimal low    = Math.Min(open, close) * (1m - noise2);

                high = Math.Clamp(Math.Round(high, 2), cfg.Min, cfg.Max);
                low  = Math.Clamp(Math.Round(low,  2), cfg.Min, cfg.Max);

                // Intraday volume pattern: higher near open (bar 0-2) and close (bar 22-24)
                double volFactor = (bar < 3 || bar > 21) ? 2.5 : 1.0;
                long baseVol = symbol == "NIFTY" ? 150_000L : 80_000L;
                long volume  = (long)(baseVol * volFactor * (0.7 + _rng.NextDouble() * 0.6));

                var istBarTime = DateTime.SpecifyKind(barTime, DateTimeKind.Unspecified);
                var utcBarTime = TimeZoneInfo.ConvertTimeToUtc(istBarTime, IstZone);
                candles.Add(new CandleData(utcBarTime, Math.Round(open, 2), high, low, Math.Round(close, 2), volume));

                if (high > dayHigh) dayHigh = high;
                if (low  < dayLow)  dayLow  = low;
                price = close;
            }
        }

        return candles;
    }

    private double NextGaussian()
    {
        double u1 = 1.0 - _rng.NextDouble();
        double u2 = 1.0 - _rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }

    private static decimal Clamp(decimal value, decimal min, decimal max) =>
        value < min ? min : value > max ? max : value;

    private static TimeZoneInfo GetIstZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
    }
}
