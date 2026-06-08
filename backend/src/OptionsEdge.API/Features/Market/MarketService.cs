using Microsoft.Extensions.Caching.Memory;
using OptionsEdge.API.Infrastructure.Background;
using OptionsEdge.API.Infrastructure.MockData;

namespace OptionsEdge.API.Features.Market;

public class MarketService(IMarketDataService marketData, IMemoryCache cache)
{
    public IReadOnlyList<MarketSnapshotResponse> GetSnapshots() =>
        marketData.GetSnapshots().Select(ToResponse).ToList();

    public MarketSnapshotResponse? GetSnapshot(string symbol)
    {
        var key = symbol.ToUpper();
        if (key is not ("NIFTY" or "BANKNIFTY")) return null;
        return ToResponse(marketData.GetSnapshot(key));
    }

    public IReadOnlyList<CandleResponse> GetCandles(string symbol)
    {
        var candles = marketData.GetCandles(symbol.ToUpper());
        return candles.Select(c => new CandleResponse(
            Time:   c.Time.ToUnixTimeSeconds(),
            Open:   c.Open,
            High:   c.High,
            Low:    c.Low,
            Close:  c.Close,
            Volume: c.Volume)).ToList();
    }

    public MarketStatusResponse GetStatus()
    {
        bool isOpen = MarketHoursHelper.IsMarketOpen();
        string message = isOpen ? "Market is Open" : "Market is Closed";
        string nextEvent = MarketHoursHelper.GetNextEventDescription();
        return new MarketStatusResponse(isOpen, message, nextEvent);
    }

    private MarketSnapshotResponse ToResponse(MarketSnapshotData d) =>
        new(d.Symbol, d.Ltp, d.Open, d.High, d.Low, d.PreviousClose,
            d.Change, d.ChangePct, d.Vix, d.Pcr, d.FiiFlow, d.DiiFlow, d.Timestamp,
            DataSource: HasLiveSnapshot(d.Symbol) ? "groww_live" : "mock");

    // GrowwMarketDataService caches the last live snapshot it fetched on a user's behalf
    // under this key — its presence is what distinguishes real Groww data from the
    // simulated fallback, regardless of which IMarketDataService implementation is active.
    private bool HasLiveSnapshot(string symbol) =>
        cache.TryGetValue($"groww_snapshot:{symbol.ToUpperInvariant()}", out MarketSnapshotData? cached) && cached is not null;
}
