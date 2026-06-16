using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OptionsEdge.API.Common.Options;
using OptionsEdge.API.Domain.Entities;
using OptionsEdge.API.Features.Positions;

namespace OptionsEdge.API.Tests;

public class LotSizeConfigurationReloadTests
{
    [Fact]
    public async Task PositionService_UsesUpdatedLotSizeAfterAppsettingsEdit()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"optionsedge-lotsize-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var configPath = Path.Combine(tempDir, "appsettings.json");

        try
        {
            await File.WriteAllTextAsync(configPath, """
            {
              "LotSizes": {
                "NIFTY": 65,
                "BANKNIFTY": 30
              }
            }
            """);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(tempDir)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();
            services.Configure<LotSizeOptions>(configuration.GetSection("LotSizes"));
            services.AddSingleton<PositionService>();

            await using var serviceProvider = services.BuildServiceProvider();
            var service = serviceProvider.GetRequiredService<PositionService>();
            var lotSizeMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<LotSizeOptions>>();
            var position = CreatePosition();

            var initialPnL = service.CalculatePnL(position, currentLtp: 110m);

            await File.WriteAllTextAsync(configPath, """
            {
              "LotSizes": {
                "NIFTY": 75,
                "BANKNIFTY": 30
              }
            }
            """);

            var timeoutAt = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < timeoutAt && lotSizeMonitor.CurrentValue.NIFTY != 75)
                await Task.Delay(100);

            var updatedPnL = service.CalculatePnL(position, currentLtp: 110m);

            Assert.Equal(650m, initialPnL);
            Assert.Equal(75, lotSizeMonitor.CurrentValue.NIFTY);
            Assert.Equal(750m, updatedPnL);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    private static Position CreatePosition() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Symbol = "NIFTY",
        Strike = 24000,
        OptionType = "CE",
        Expiry = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
        EntryPrice = 100m,
        Quantity = 1,
        StopLoss = 80m,
        Target1 = 120m,
        Status = "active",
        CreatedAt = DateTimeOffset.UtcNow,
    };
}
