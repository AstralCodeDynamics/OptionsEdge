using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OptionsEdge.API.Common.Options;
using OptionsEdge.API.Infrastructure.Logging;

namespace OptionsEdge.API.Tests;

public class LogFileMaintenanceServiceTests
{
    [Fact]
    public void DeleteExpiredFiles_DeletesOnlyExpiredLogFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"optionsedge-logs-{Guid.NewGuid():N}");
        var logDirectory = Path.Combine(root, "logs");
        Directory.CreateDirectory(logDirectory);

        try
        {
            var expiredLog = Path.Combine(logDirectory, "optionsedge-20260601.log");
            var freshLog = Path.Combine(logDirectory, "optionsedge-20260615.log");
            var unrelated = Path.Combine(logDirectory, "notes.txt");

            File.WriteAllText(expiredLog, "expired");
            File.WriteAllText(freshLog, "fresh");
            File.WriteAllText(unrelated, "keep");

            File.SetLastWriteTimeUtc(expiredLog, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
            File.SetLastWriteTimeUtc(freshLog, new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc));

            var service = CreateService(root, new LogFileOptions
            {
                Directory = "logs",
                FileNamePrefix = "optionsedge-",
                RetentionDays = 7,
                CleanupTimeLocal = "00:00:00",
            });

            var deletedFiles = service.DeleteExpiredFiles(new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));

            Assert.Single(deletedFiles);
            Assert.Contains(expiredLog, deletedFiles);
            Assert.False(File.Exists(expiredLog));
            Assert.True(File.Exists(freshLog));
            Assert.True(File.Exists(unrelated));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void GetDelayUntilNextRun_UsesIstMidnightFromUtcClock()
    {
        var service = CreateService("/tmp/optionsedge-tests", new LogFileOptions
        {
            Directory = "logs",
            CleanupTimeLocal = "00:00:00",
        });

        // 2026-06-16 18:30 UTC = 2026-06-17 00:00 IST, so next cleanup is 24h away.
        var delay = service.GetDelayUntilNextRun(new DateTimeOffset(2026, 6, 16, 18, 30, 0, TimeSpan.Zero));

        Assert.Equal(TimeSpan.FromHours(24), delay);
    }

    [Fact]
    public void GetDelayUntilNextRun_ReturnsRemainingTimeUntilIstMidnight()
    {
        var service = CreateService("/tmp/optionsedge-tests", new LogFileOptions
        {
            Directory = "logs",
            CleanupTimeLocal = "00:00:00",
        });

        // 2026-06-16 16:00 UTC = 21:30 IST, so 2.5h remain until IST midnight.
        var delay = service.GetDelayUntilNextRun(new DateTimeOffset(2026, 6, 16, 16, 0, 0, TimeSpan.Zero));

        Assert.Equal(TimeSpan.FromHours(2.5), delay);
    }

    private static LogFileMaintenanceService CreateService(string contentRootPath, LogFileOptions options) =>
        new(new TestHostEnvironment { ContentRootPath = contentRootPath }, new TestOptionsMonitor<LogFileOptions>(options));

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "OptionsEdge.API.Tests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
