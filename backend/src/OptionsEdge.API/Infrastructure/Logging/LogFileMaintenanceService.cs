using Microsoft.Extensions.Options;
using OptionsEdge.API.Common.Options;
using OptionsEdge.API.Common.Time;

namespace OptionsEdge.API.Infrastructure.Logging;

public class LogFileMaintenanceService(
    IHostEnvironment hostEnvironment,
    IOptionsMonitor<LogFileOptions> logFileOptions)
{
    public string GetLogDirectoryPath()
    {
        var options = logFileOptions.CurrentValue;
        return LogFilePathResolver.Resolve(hostEnvironment.ContentRootPath, options.Directory);
    }

    public TimeSpan GetDelayUntilNextRun(DateTimeOffset nowUtc)
    {
        var nowIst = IndiaTime.ToIst(nowUtc);
        var cleanupTime = logFileOptions.CurrentValue.GetCleanupTimeLocal();
        var nextRun = new DateTimeOffset(
            nowIst.Year,
            nowIst.Month,
            nowIst.Day,
            0,
            0,
            0,
            nowIst.Offset).Add(cleanupTime);

        if (nextRun <= nowIst)
            nextRun = nextRun.AddDays(1);

        return nextRun.ToUniversalTime() - nowUtc.ToUniversalTime();
    }

    public IReadOnlyList<string> DeleteExpiredFiles(DateTimeOffset nowUtc)
    {
        var options = logFileOptions.CurrentValue;
        var logDirectory = GetLogDirectoryPath();

        if (!Directory.Exists(logDirectory))
            return [];

        var deletedFiles = new List<string>();
        var cutoffUtc = nowUtc.UtcDateTime.AddDays(-Math.Max(1, options.RetentionDays));
        var searchPattern = $"{options.FileNamePrefix}*.log*";

        foreach (var filePath in Directory.EnumerateFiles(logDirectory, searchPattern, SearchOption.TopDirectoryOnly))
        {
            var lastWriteUtc = File.GetLastWriteTimeUtc(filePath);
            if (lastWriteUtc >= cutoffUtc)
                continue;

            File.Delete(filePath);
            deletedFiles.Add(filePath);
        }

        return deletedFiles;
    }
}
