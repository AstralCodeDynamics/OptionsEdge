namespace OptionsEdge.API.Infrastructure.MockData;

// Shared read surface for market data, satisfied by both the simulated MockMarketDataService
// (used in dev/when Groww is disabled) and GrowwMarketDataService (live data when enabled).
// Tick() is intentionally excluded — it drives the mock simulation only and is consumed
// directly by MarketDataWorker via the concrete MockMarketDataService.
public interface IMarketDataService
{
    IReadOnlyList<MarketSnapshotData> GetSnapshots();
    MarketSnapshotData GetSnapshot(string symbol);
    IReadOnlyList<CandleData> GetCandles(string symbol);
}
