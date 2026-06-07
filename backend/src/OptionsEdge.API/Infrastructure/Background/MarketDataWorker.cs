using Microsoft.AspNetCore.SignalR;
using OptionsEdge.API.Features.Indicators;
using OptionsEdge.API.Features.Market;
using OptionsEdge.API.Infrastructure.MockData;
using OptionsEdge.API.Infrastructure.SignalR;

namespace OptionsEdge.API.Infrastructure.Background;

public class MarketDataWorker(
    IHubContext<MarketHub> hubContext,
    MockMarketDataService mockData,
    IndicatorService indicatorService,
    IConfiguration config,
    ILogger<MarketDataWorker> logger) : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MarketDataWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in MarketDataWorker tick");
            }

            await Task.Delay(TickInterval, stoppingToken);
        }
    }

    private async Task TickAsync()
    {
        bool isOpen = MarketHoursHelper.IsMarketOpen();

        if (isOpen)
        {
            if (!config.GetValue<bool>("Groww:Enabled"))
                mockData.Tick();

            foreach (var snapshot in mockData.GetSnapshots())
            {
                var priceEvent = new PriceUpdateEvent(
                    snapshot.Symbol,
                    snapshot.Ltp,
                    snapshot.Change,
                    snapshot.ChangePct,
                    snapshot.Timestamp);

                await hubContext.Clients
                    .Group(snapshot.Symbol)
                    .SendAsync("PriceUpdate", priceEvent);

                var indicators = indicatorService.GetIndicators(snapshot.Symbol);
                var indicatorEvent = new IndicatorUpdateEvent(
                    snapshot.Symbol,
                    indicators.Rsi,
                    indicators.Macd,
                    indicators.Supertrend.IsBullish ? "BUY" : "SELL",
                    DateTimeOffset.UtcNow);

                await hubContext.Clients
                    .Group(snapshot.Symbol)
                    .SendAsync("IndicatorUpdate", indicatorEvent);
            }
        }

        var statusEvent = new MarketStatusEvent(
            isOpen,
            isOpen ? "Market is Open" : "Market is Closed",
            MarketHoursHelper.GetNextEventDescription());

        await hubContext.Clients.All.SendAsync("MarketStatus", statusEvent);
    }
}
