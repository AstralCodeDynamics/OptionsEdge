using OptionsEdge.API.Infrastructure.Background;
using OptionsEdge.API.Infrastructure.MockData;

namespace OptionsEdge.API.Features.Market;

public class MarketService(IMarketDataService marketData)
{
    public IReadOnlyList<MarketSnapshotResponse> GetSnapshots() =>
        marketData.GetSnapshots().Select(ToResponse).ToList();

    public MarketSnapshotResponse? GetSnapshot(string symbol)
    {
        var key = symbol.ToUpper();
        if (key is not ("NIFTY" or "BANKNIFTY")) return null;
        var snapshot = marketData.GetSnapshot(key);
        return snapshot is null ? null : ToResponse(snapshot);
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

    public bool HasFreshSnapshot(string symbol) =>
        marketData.GetSnapshot(symbol.ToUpperInvariant()) is not null;

    public bool HasFreshCandles(string symbol) =>
        marketData.GetCandles(symbol.ToUpperInvariant()).Count > 0;

    private MarketSnapshotResponse ToResponse(MarketSnapshotData d) =>
        new(d.Symbol, d.Ltp, d.Open, d.High, d.Low, d.PreviousClose,
            d.Change, d.ChangePct, d.Vix, d.Pcr, d.FiiFlow, d.DiiFlow, d.Timestamp,
            DataSource: HasLiveSnapshot(d.Symbol) ? "groww_live" : "mock");

    // GrowwMarketDataService caches the last live snapshot it fetched on a user's behalf;
    // mock mode is reported separately when Groww is disabled globally.
    private bool HasLiveSnapshot(string symbol) =>
        marketData is OptionsEdge.API.Infrastructure.Groww.GrowwMarketDataService groww
            ? groww.HasLiveSnapshot(symbol)
            : false;
}
