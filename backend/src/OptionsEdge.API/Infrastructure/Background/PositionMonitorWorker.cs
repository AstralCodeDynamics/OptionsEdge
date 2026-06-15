using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OptionsEdge.API.Features.Groww;
using OptionsEdge.API.Features.Options;
using OptionsEdge.API.Features.Positions;
using OptionsEdge.API.Infrastructure.Data;
using OptionsEdge.API.Infrastructure.Groww;
using OptionsEdge.API.Infrastructure.MockData;

namespace OptionsEdge.API.Infrastructure.Background;

public class PositionMonitorWorker(
    IServiceScopeFactory scopeFactory,
    IMarketDataService marketData,
    OptionsService optionsService,
    PositionService positionService,
    IMemoryCache cache,
    IConfiguration config,
    ILogger<PositionMonitorWorker> logger) : BackgroundService
{
    private static readonly TimeSpan TickInterval           = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan AlertDeduplicationTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan VixLookback           = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan SpotLookback          = TimeSpan.FromMinutes(15);
    private const int MaxHistoryLength = 35;

    // Groww allows ~10 req/sec; pause briefly every 10 calls so a large position
    // list doesn't burst past the per-user rate limit in one tick.
    private const int GrowwRateLimitBatchSize = 10;
    private static readonly TimeSpan GrowwRateLimitPause = TimeSpan.FromMilliseconds(1100);

    private readonly Dictionary<string, Queue<(DateTimeOffset Time, decimal Value)>> _spotHistory = new();
    private readonly Dictionary<string, Queue<(DateTimeOffset Time, decimal Value)>> _vixHistory  = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PositionMonitorWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in PositionMonitorWorker tick");
            }

            await Task.Delay(TickInterval, stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        bool bypass = config.GetValue<bool>("Claude:BypassMarketHours");
        if (!bypass && !MarketHoursHelper.IsMarketOpen())
            return;

        foreach (var symbol in new[] { "NIFTY", "BANKNIFTY" })
        {
            var snapshot = marketData.GetSnapshot(symbol);
            PushHistory(_spotHistory, symbol, snapshot.Ltp);
            PushHistory(_vixHistory,  symbol, snapshot.Vix);
        }

        using var scope        = scopeFactory.CreateScope();
        var db                 = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var alertService       = scope.ServiceProvider.GetRequiredService<AlertService>();
        var activePositions    = await db.Positions.Where(p => p.Status == "active").ToListAsync(ct);

        int growwCallCount = 0;
        foreach (var position in activePositions)
        {
            try
            {
                await ProcessPositionAsync(position, alertService, scope, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error processing position {PositionId}", position.Id);
            }

            if (config.GetValue<bool>("Groww:Enabled") && ++growwCallCount % GrowwRateLimitBatchSize == 0)
                await Task.Delay(GrowwRateLimitPause, ct);
        }
    }

    private async Task ProcessPositionAsync(
        Domain.Entities.Position position,
        AlertService alertService,
        IServiceScope scope,
        CancellationToken ct)
    {
        var symbol   = position.Symbol.ToUpper();
        var snapshot = marketData.GetSnapshot(symbol);

        decimal currentLtp = await GetCurrentLtpAsync(position, symbol, scope, ct);

        decimal? spot15MinAgo = GetHistoryValue(_spotHistory, symbol, SpotLookback);
        decimal? prevVix30Min = GetHistoryValue(_vixHistory,  symbol, VixLookback);

        var triggers = positionService.CheckAlertConditions(
            position, currentLtp, snapshot.Ltp, spot15MinAgo, snapshot.Vix, prevVix30Min);

        foreach (var trigger in triggers)
        {
            string dedupKey = $"alert:{position.Id}:{trigger.AlertType}";
            if (cache.TryGetValue(dedupKey, out _))
                continue;

            cache.Set(dedupKey, true, AlertDeduplicationTtl);

            var alert = await alertService.SaveAlertAsync(
                position.Id, position.UserId, trigger.Severity, trigger.AlertType, trigger.Message, ct);
            await alertService.BroadcastAlertAsync(alert, ct);
        }
    }

    // Prefers a direct per-user Groww quote (real NSE LTP) over the shared chain
    // cache / Black-Scholes estimate from OptionsService. Falls back on any error,
    // missing credentials, or a non-positive Groww quote.
    private async Task<decimal> GetCurrentLtpAsync(
        Domain.Entities.Position position,
        string symbol,
        IServiceScope scope,
        CancellationToken ct)
    {
        decimal FallbackLtp() => optionsService.GetOptionLtp(
            symbol, position.Strike, position.OptionType, position.Expiry.ToString("yyyy-MM-dd"));

        if (!config.GetValue<bool>("Groww:Enabled"))
            return FallbackLtp();

        try
        {
            var growwUserApi = scope.ServiceProvider.GetRequiredService<GrowwUserApiClient>();
            var credSvc      = scope.ServiceProvider.GetRequiredService<GrowwCredentialService>();

            if (!await credSvc.HasCredentialsAsync(position.UserId, ct))
                return FallbackLtp();

            var tradingSymbol = GrowwSymbolHelper.FormatOptionSymbol(
                symbol, position.Expiry, position.Strike, position.OptionType);

            decimal ltp = await growwUserApi.GetOptionLtpAsync(position.UserId, tradingSymbol, ct);
            if (ltp <= 0)
                return FallbackLtp();

            logger.LogInformation("Position {Id} live LTP from Groww: {Ltp}", position.Id, ltp);
            return ltp;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Groww LTP fetch failed for position {Id}, falling back to chain cache/BS", position.Id);
            return FallbackLtp();
        }
    }

    private static void PushHistory(
        Dictionary<string, Queue<(DateTimeOffset Time, decimal Value)>> dict,
        string key,
        decimal value)
    {
        if (!dict.TryGetValue(key, out var q))
        {
            q = new Queue<(DateTimeOffset, decimal)>();
            dict[key] = q;
        }
        q.Enqueue((DateTimeOffset.UtcNow, value));
        while (q.Count > MaxHistoryLength) q.Dequeue();
    }

    private static decimal? GetHistoryValue(
        Dictionary<string, Queue<(DateTimeOffset Time, decimal Value)>> dict,
        string key,
        TimeSpan lookback)
    {
        if (!dict.TryGetValue(key, out var q)) return null;
        var cutoff = DateTimeOffset.UtcNow - lookback;
        var best   = default((DateTimeOffset Time, decimal Value));
        foreach (var item in q)
        {
            if (item.Time <= cutoff && item.Time > best.Time)
                best = item;
        }
        return best.Time == default ? null : best.Value;
    }
}
