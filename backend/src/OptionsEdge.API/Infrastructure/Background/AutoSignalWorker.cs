using Microsoft.AspNetCore.SignalR;
using OptionsEdge.API.Features.Signals;
using OptionsEdge.API.Infrastructure.SignalR;

namespace OptionsEdge.API.Infrastructure.Background;

// Generates AI signals on each user's configured NIFTY/BANKNIFTY schedule
// (see UserSignalPreferenceService) and pushes them to that user via SignalR.
public class AutoSignalWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<AutoSignalWorker> logger) : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AutoSignalWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in AutoSignalWorker tick");
            }

            await Task.Delay(TickInterval, stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var prefService = scope.ServiceProvider.GetRequiredService<UserSignalPreferenceService>();

        var due = await prefService.GetDueSignalsAsync(ct);
        if (due.Count == 0)
            return;

        var signalService = scope.ServiceProvider.GetRequiredService<AISignalService>();
        var hub = scope.ServiceProvider.GetRequiredService<IHubContext<MarketHub>>();

        logger.LogInformation("AutoSignalWorker: generating {Count} scheduled signal(s)", due.Count);

        foreach (var (userId, symbol) in due)
        {
            try
            {
                var (signal, error) = await signalService.GenerateEntrySignalAsync(symbol, userId, ct);
                if (error is not null)
                {
                    logger.LogWarning("Auto signal skipped for user {UserId} {Symbol}: {Error}", userId, symbol, error);
                    continue;
                }

                await hub.Clients.User(userId.ToString()).SendAsync("AutoSignalGenerated", signal, ct);
                logger.LogInformation("Auto signal sent to user {UserId} for {Symbol}", userId, symbol);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Auto signal failed for user {UserId} {Symbol}", userId, symbol);
            }
        }
    }
}
