using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace OptionsEdge.API.Features.Signals;

public class SignalCacheService(IMemoryCache cache)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public bool TryGet(string key, out SignalResponse? signal) =>
        cache.TryGetValue(key, out signal);

    public void Set(string key, SignalResponse signal) =>
        cache.Set(key, signal, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl });

    public string BuildKey(string symbol, double rsi, string macdSignal, double pcr, decimal spot)
    {
        // Bucket each value so nearby market states share the same cache entry
        int rsiBucket    = (int)(Math.Round(rsi / 5) * 5);
        double pcrBucket = Math.Round(pcr * 10.0) / 10.0;
        long spotBucket  = (long)(Math.Round((double)spot / 50.0) * 50.0);

        var raw   = $"{symbol}|{rsiBucket}|{macdSignal}|{pcrBucket:F1}|{spotBucket}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
