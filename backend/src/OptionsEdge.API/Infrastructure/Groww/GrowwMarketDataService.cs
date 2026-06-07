using Microsoft.Extensions.Caching.Memory;
using OptionsEdge.API.Infrastructure.MockData;

namespace OptionsEdge.API.Infrastructure.Groww;

// Live-data-backed implementation of IMarketDataService, swapped in via "Groww:Enabled".
// The interface's methods are synchronous (mirroring MockMarketDataService), so the
// underlying async HTTP calls are awaited here and the results cached briefly to keep
// this within the documented Groww rate limits (10 req/sec, 300 req/min for live data).
public class GrowwMarketDataService(
    GrowwApiClient groww,
    IMemoryCache cache,
    ILogger<GrowwMarketDataService> logger) : IMarketDataService
{
    private static readonly string[] Symbols = ["NIFTY", "BANKNIFTY"];
    private static readonly TimeSpan SnapshotTtl = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan CandleTtl = TimeSpan.FromMinutes(5);

    public IReadOnlyList<MarketSnapshotData> GetSnapshots() =>
        Symbols.Select(GetSnapshot).ToList();

    public MarketSnapshotData GetSnapshot(string symbol)
    {
        var key = symbol.ToUpperInvariant();
        var cacheKey = $"groww:snapshot:{key}";

        if (cache.TryGetValue(cacheKey, out MarketSnapshotData? cached) && cached is not null)
            return cached;

        try
        {
            var snapshot = groww.GetSpotSnapshotAsync(key, CancellationToken.None).GetAwaiter().GetResult();
            cache.Set(cacheKey, snapshot, SnapshotTtl);
            return snapshot;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch live Groww snapshot for {Symbol}", key);
            return new MarketSnapshotData(key, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, DateTimeOffset.UtcNow);
        }
    }

    public IReadOnlyList<CandleData> GetCandles(string symbol)
    {
        var key = symbol.ToUpperInvariant();
        var cacheKey = $"groww:candles:{key}";

        if (cache.TryGetValue(cacheKey, out IReadOnlyList<CandleData>? cached) && cached is not null)
            return cached;

        try
        {
            var candles = groww.GetHistoricalCandlesAsync(key, ct: CancellationToken.None).GetAwaiter().GetResult();
            cache.Set(cacheKey, candles, CandleTtl);
            return candles;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch Groww candles for {Symbol}", key);
            return [];
        }
    }
}
