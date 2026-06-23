using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OptionsEdge.API.Common.Options;
using OptionsEdge.API.Features.Options;
using OptionsEdge.API.Infrastructure.Background;
using OptionsEdge.API.Infrastructure.Groww;
using OptionsEdge.API.Infrastructure.MockData;

namespace OptionsEdge.API.Tests;

public class GrowwCacheMissTests
{
    [Fact]
    public void GrowwMarketDataService_GetSnapshot_ReturnsNullWhenCacheEmpty()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var provider = new ServiceCollection().BuildServiceProvider();
        var service = new GrowwMarketDataService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            cache,
            NullLogger<GrowwMarketDataService>.Instance);

        Assert.Null(service.GetSnapshot("NIFTY"));
        Assert.Empty(service.GetCandles("NIFTY"));
    }

    [Fact]
    public void OptionsService_GetChain_ReturnsNullWhenSnapshotUnavailable()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new OptionsService(
            new EmptyMarketDataService(),
            cache,
            new TestOptionsMonitor<LotSizeOptions>(new LotSizeOptions
            {
                NIFTY = 65,
                BANKNIFTY = 30,
            }));

        Assert.Null(service.GetChain("NIFTY", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)).ToString("yyyy-MM-dd")));
    }

    [Fact]
    public void PositionMonitorWorker_SkipsAlertEvaluationWhenMarketDataMissing()
    {
        Assert.False(PositionMonitorWorker.ShouldEvaluateAlerts(snapshot: null, currentLtp: 100m));

        var snapshot = new MarketSnapshotData(
            Symbol: "NIFTY",
            Ltp: 24100m,
            Open: 24000m,
            High: 24200m,
            Low: 23950m,
            PreviousClose: 24050m,
            Change: 50m,
            ChangePct: 0.21m,
            Vix: 14m,
            Pcr: 1.05m,
            FiiFlow: 0m,
            DiiFlow: 0m,
            Timestamp: DateTimeOffset.UtcNow);

        Assert.False(PositionMonitorWorker.ShouldEvaluateAlerts(snapshot, currentLtp: null));
        Assert.True(PositionMonitorWorker.ShouldEvaluateAlerts(snapshot, currentLtp: 100m));
    }

    private sealed class EmptyMarketDataService : IMarketDataService
    {
        public IReadOnlyList<MarketSnapshotData> GetSnapshots() => [];

        public MarketSnapshotData? GetSnapshot(string symbol) => null;

        public IReadOnlyList<CandleData> GetCandles(string symbol) => [];
    }
}
