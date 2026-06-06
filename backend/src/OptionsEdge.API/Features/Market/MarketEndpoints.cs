namespace OptionsEdge.API.Features.Market;

public static class MarketEndpoints
{
    public static void MapMarketEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/market");

        group.MapGet("/snapshot", (MarketService svc) =>
            Results.Ok(svc.GetSnapshots()))
            .WithName("GetMarketSnapshots");

        group.MapGet("/snapshot/{symbol}", (string symbol, MarketService svc) =>
        {
            var snap = svc.GetSnapshot(symbol);
            return snap is null ? Results.NotFound() : Results.Ok(snap);
        }).WithName("GetMarketSnapshot");

        group.MapGet("/candles/{symbol}", (string symbol, MarketService svc) =>
            Results.Ok(svc.GetCandles(symbol)))
            .WithName("GetMarketCandles");

        group.MapGet("/status", (MarketService svc) =>
            Results.Ok(svc.GetStatus()))
            .WithName("GetMarketStatus");
    }

    public static void AddMarketServices(this IServiceCollection services)
    {
        services.AddSingleton<OptionsEdge.API.Infrastructure.MockData.MockMarketDataService>();
        services.AddScoped<MarketService>();
    }
}
