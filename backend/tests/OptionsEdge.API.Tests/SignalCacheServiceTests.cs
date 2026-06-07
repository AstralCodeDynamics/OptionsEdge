using Microsoft.Extensions.Caching.Memory;
using OptionsEdge.API.Features.Signals;

namespace OptionsEdge.API.Tests;

// Verifies the per-market-snapshot cache key bucketing and the cache-hit/miss
// round trip that lets two rapid identical signal requests collapse into a
// single AI call (AISignalService checks TryGet before invoking Claude).
public class SignalCacheServiceTests
{
    private static SignalCacheService NewService() => new(new MemoryCache(new MemoryCacheOptions()));

    private static SignalResponse SampleSignal(string symbol) => new(
        Id: Guid.NewGuid(),
        Symbol: symbol,
        SignalType: "BUY_CE",
        OptionType: "CE",
        Strike: 24000,
        Expiry: "2026-06-25",
        EntryLow: 100m,
        EntryHigh: 110m,
        StopLoss: 90m,
        Target1: 130m,
        Target2: 150m,
        Confidence: 70,
        RiskReward: 2.0m,
        Rationale: ["RSI oversold", "Bullish MACD crossover"],
        ModelUsed: "claude-sonnet-4-6",
        InputTokens: 500,
        OutputTokens: 200,
        CostUsd: 0.015m,
        ValidUntil: "2026-06-08T10:00:00Z",
        CreatedAt: "2026-06-08T09:30:00Z");

    [Fact]
    public void BuildKey_IsStableForIdenticalInputs()
    {
        var service = NewService();

        var key1 = service.BuildKey("NIFTY", rsi: 58.3, macdSignal: "bullish", pcr: 1.04, spot: 24012m);
        var key2 = service.BuildKey("NIFTY", rsi: 58.3, macdSignal: "bullish", pcr: 1.04, spot: 24012m);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void BuildKey_BucketsNearbyMarketStatesToTheSameKey()
    {
        var service = NewService();

        // Pick two slightly different raw readings whose RSI (nearest 5), PCR (1 decimal)
        // and spot (nearest 50) buckets all land on the same value, so they collapse to one key.
        var key1 = service.BuildKey("NIFTY", rsi: 56.0, macdSignal: "bullish", pcr: 1.04, spot: 24010m);
        var key2 = service.BuildKey("NIFTY", rsi: 57.0, macdSignal: "bullish", pcr: 1.04, spot: 24020m);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void BuildKey_DiffersWhenBucketedValuesDiffer()
    {
        var service = NewService();

        var key1 = service.BuildKey("NIFTY", rsi: 30.0, macdSignal: "bearish", pcr: 0.8, spot: 23900m);
        var key2 = service.BuildKey("NIFTY", rsi: 70.0, macdSignal: "bullish", pcr: 1.3, spot: 24200m);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void TryGet_MissesUntilSet_ThenHitsWithTheStoredSignal()
    {
        var service = NewService();
        var key = service.BuildKey("BANKNIFTY", rsi: 50.0, macdSignal: "neutral", pcr: 1.0, spot: 51000m);

        // First lookup for a fresh market snapshot: cache miss (this is what triggers the AI call)
        var firstHit = service.TryGet(key, out var firstSignal);
        Assert.False(firstHit);
        Assert.Null(firstSignal);

        var stored = SampleSignal("BANKNIFTY");
        service.Set(key, stored);

        // A second rapid request with the same snapshot hash hits the cache instead of calling the AI again
        var secondHit = service.TryGet(key, out var secondSignal);
        Assert.True(secondHit);
        Assert.Equal(stored.Id, secondSignal!.Id);
        Assert.Equal(stored.Symbol, secondSignal.Symbol);
    }
}
