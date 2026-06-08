using Microsoft.AspNetCore.SignalR;
using OptionsEdge.API.Features.Indicators;
using OptionsEdge.API.Features.Market;
using OptionsEdge.API.Infrastructure.Groww;
using OptionsEdge.API.Infrastructure.MockData;
using OptionsEdge.API.Infrastructure.SignalR;

namespace OptionsEdge.API.Infrastructure.Background;

public class MarketDataWorker(
    IHubContext<MarketHub> hubContext,
    MockMarketDataService mockData,
    GrowwMarketDataService growwMarketData,
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
            bool growwEnabled = config.GetValue<bool>("Groww:Enabled");
            if (!growwEnabled)
                mockData.Tick();

            // No platform-wide Groww account exists, so the worker (no user context) can't
            // call Groww directly — it broadcasts whatever GrowwMarketDataService has cached
            // from recent authenticated user requests, falling back to simulated data when
            // that cache is empty (e.g. no user has been active yet).
            var snapshots = growwEnabled ? growwMarketData.GetSnapshots() : mockData.GetSnapshots();

            foreach (var snapshot in snapshots)
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
