using OptionsEdge.API.Infrastructure.Background;
using OptionsEdge.API.Infrastructure.MockData;

namespace OptionsEdge.API.Features.Market;

public class MarketService(MockMarketDataService mockData)
{
    public IReadOnlyList<MarketSnapshotResponse> GetSnapshots() =>
        mockData.GetSnapshots().Select(ToResponse).ToList();

    public MarketSnapshotResponse? GetSnapshot(string symbol)
    {
        var key = symbol.ToUpper();
        if (key is not ("NIFTY" or "BANKNIFTY")) return null;
        return ToResponse(mockData.GetSnapshot(key));
    }

    public IReadOnlyList<CandleResponse> GetCandles(string symbol)
    {
        var candles = mockData.GetCandles(symbol.ToUpper());
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

    private static MarketSnapshotResponse ToResponse(MarketSnapshotData d) =>
        new(d.Symbol, d.Ltp, d.Open, d.High, d.Low, d.PreviousClose,
            d.Change, d.ChangePct, d.Vix, d.Pcr, d.FiiFlow, d.DiiFlow, d.Timestamp);
}
