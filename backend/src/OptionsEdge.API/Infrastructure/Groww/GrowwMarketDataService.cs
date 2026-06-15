using Microsoft.Extensions.Caching.Memory;
using OptionsEdge.API.Features.Groww;
using OptionsEdge.API.Infrastructure.Background;
using OptionsEdge.API.Infrastructure.MockData;

namespace OptionsEdge.API.Infrastructure.Groww;

// Live-data-backed implementation of IMarketDataService, swapped in via "Groww:Enabled".
//
// There's no platform-wide Groww account — every live fetch needs an authenticated user's own
// credentials (GrowwUserApiClient). So this service doesn't fetch on its own: RefreshForUserAsync
// is called from market endpoints whenever an authenticated user requests a snapshot, and the
// result is cached as the "last known" data under a shared key. GetSnapshot/GetCandles (the
// synchronous IMarketDataService surface consumed by background workers and other features with
// no user context) simply read that cache, falling back to simulated data when it's empty.
public class GrowwMarketDataService(
    IServiceScopeFactory scopeFactory,
    MockMarketDataService mockData,
    IMemoryCache cache,
    ILogger<GrowwMarketDataService> logger) : IMarketDataService
{
    private static readonly string[] Symbols = ["NIFTY", "BANKNIFTY"];
    private static readonly TimeSpan CandleTtl = TimeSpan.FromMinutes(30);

    private static string SnapshotCacheKey(string symbol) => $"groww_snapshot:{symbol}";
    private static string CandleCacheKey(string symbol) => $"groww_candles:{symbol}";

    public IReadOnlyList<MarketSnapshotData> GetSnapshots() =>
        Symbols.Select(GetSnapshot).ToList();

    public MarketSnapshotData GetSnapshot(string symbol)
    {
        var key = symbol.ToUpperInvariant();
        return cache.TryGetValue(SnapshotCacheKey(key), out MarketSnapshotData? cached) && cached is not null
            ? cached
            : mockData.GetSnapshot(key);
    }

    public IReadOnlyList<CandleData> GetCandles(string symbol)
    {
        var key = symbol.ToUpperInvariant();
        return cache.TryGetValue(CandleCacheKey(key), out IReadOnlyList<CandleData>? cached) && cached is not null
            ? cached
            : mockData.GetCandles(key);
    }

    // Fetches fresh live data using the requesting user's own Groww credentials and stores it
    // as the shared "last known" snapshot/candles for everyone. GrowwUserApiClient is scoped
    // (it depends on AppDbContext via GrowwCredentialService), so — since this service is a
    // singleton — it's resolved from a fresh DI scope rather than injected directly.
    public async Task RefreshForUserAsync(Guid userId, string symbol, CancellationToken ct = default)
    {
        var key = symbol.ToUpperInvariant();
        if (key is not ("NIFTY" or "BANKNIFTY"))
            return;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var groww = scope.ServiceProvider.GetRequiredService<GrowwUserApiClient>();

            var credSvc = scope.ServiceProvider.GetRequiredService<GrowwCredentialService>();
            if (!await credSvc.HasCredentialsAsync(userId, ct))
            {
                logger.LogDebug(
                    "Groww credentials not configured for user {UserId} — using cached/mock data", userId);
                return;
            }

            var snapshot = await groww.GetSpotSnapshotAsync(userId, key, ct);

            // India VIX is fetched once for NIFTY and reused for BANKNIFTY from the cached
            // NIFTY snapshot, avoiding a second Groww call per refresh cycle.
            decimal vix = key == "NIFTY"
                ? await groww.GetVixAsync(userId, ct)
                : cache.TryGetValue(SnapshotCacheKey("NIFTY"), out MarketSnapshotData? niftySnapshot) && niftySnapshot is not null
                    ? niftySnapshot.Vix
                    : 0m;
            snapshot = snapshot with { Vix = vix };

            // Outside market hours prices don't move, so the last known value can stay cached
            // far longer than the brief during-hours TTL — it just needs to survive until the
            // next user-triggered refresh instead of falling back to mock data.
            var snapshotTtl = MarketHoursHelper.IsMarketOpen()
                ? TimeSpan.FromSeconds(30)
                : TimeSpan.FromMinutes(5);
            cache.Set(SnapshotCacheKey(key), snapshot, snapshotTtl);

            logger.LogInformation(
                "Groww live snapshot cached for {Symbol} via user {UserId}: LTP={Ltp}",
                key, userId, snapshot.Ltp);

            if (!cache.TryGetValue(CandleCacheKey(key), out IReadOnlyList<CandleData>? cachedCandles)
                || cachedCandles is null
                || cachedCandles.Count == 0)
            {
                var candles = await groww.GetHistoricalCandlesAsync(
                    userId, key, segment: "CASH", intervalMinutes: 15, lookbackDays: 90, ct: ct);
                if (candles.Count > 0)
                    cache.Set(CandleCacheKey(key), candles, CandleTtl);
                else
                    cache.Remove(CandleCacheKey(key));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to refresh Groww market data for {Symbol} via user {UserId}", key, userId);
        }
    }

    public async Task<IReadOnlyList<CandleData>> RefreshCandlesForBacktestAsync(
        Guid userId,
        string symbol,
        CancellationToken ct = default)
    {
        var key = symbol.ToUpperInvariant();
        if (key is not ("NIFTY" or "BANKNIFTY"))
            throw new ArgumentException("Symbol must be NIFTY or BANKNIFTY");

        using var scope = scopeFactory.CreateScope();
        var groww = scope.ServiceProvider.GetRequiredService<GrowwUserApiClient>();

        var candles = await groww.GetHistoricalCandlesAsync(
            userId, key, segment: "CASH", intervalMinutes: 15, lookbackDays: 90, ct: ct);

        if (candles.Count == 0)
        {
            cache.Remove(CandleCacheKey(key));
            throw new InvalidOperationException($"Groww returned no historical candles for {key}.");
        }

        cache.Set(CandleCacheKey(key), candles, CandleTtl);
        logger.LogInformation(
            "Groww historical candles cached for backtest: {Symbol}, {Count} candles",
            key, candles.Count);

        return candles;
    }
}
