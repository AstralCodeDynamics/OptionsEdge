using Microsoft.Extensions.Options;
using OptionsEdge.API.Common.Options;

namespace OptionsEdge.API.Infrastructure.Logging;

public class LogFileCleanupWorker(
    LogFileMaintenanceService maintenanceService,
    IOptionsMonitor<LogFileOptions> logFileOptions,
    ILogger<LogFileCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = logFileOptions.CurrentValue;
        logger.LogInformation(
            "LogFileCleanupWorker started. Directory {LogDirectory}. RetentionDays {RetentionDays}. CleanupTimeLocal {CleanupTimeLocal}",
            maintenanceService.GetLogDirectoryPath(),
            options.RetentionDays,
            options.CleanupTimeLocal);

        await RunCleanupAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = maintenanceService.GetDelayUntilNextRun(DateTimeOffset.UtcNow);
                logger.LogInformation("Next log cleanup scheduled in {Delay}", delay);
                await Task.Delay(delay, stoppingToken);
                await RunCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Log cleanup scheduler failed");
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private Task RunCleanupAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var deletedFiles = maintenanceService.DeleteExpiredFiles(DateTimeOffset.UtcNow);
        logger.LogInformation(
            "Log cleanup completed. Deleted {DeletedCount} expired file(s) from {LogDirectory}",
            deletedFiles.Count,
            maintenanceService.GetLogDirectoryPath());

        return Task.CompletedTask;
    }
}
