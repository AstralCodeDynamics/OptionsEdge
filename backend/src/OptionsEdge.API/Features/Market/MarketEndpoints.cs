using OptionsEdge.API.Common.Extensions;
using OptionsEdge.API.Infrastructure.Groww;

namespace OptionsEdge.API.Features.Market;

public static class MarketEndpoints
{
    public static void MapMarketEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/market");

        group.MapGet("/snapshot", async (HttpContext ctx, IConfiguration config, MarketService svc, IServiceProvider sp, CancellationToken ct) =>
        {
            await RefreshLiveDataAsync(ctx, config, sp, null, ct);
            return Results.Ok(svc.GetSnapshots());
        }).WithName("GetMarketSnapshots")
          .RequireAuthorization();

        group.MapGet("/snapshot/{symbol}", async (string symbol, HttpContext ctx, IConfiguration config, MarketService svc, IServiceProvider sp, CancellationToken ct) =>
        {
            await RefreshLiveDataAsync(ctx, config, sp, symbol, ct);
            var snap = svc.GetSnapshot(symbol);
            return snap is null ? Results.NotFound() : Results.Ok(snap);
        }).WithName("GetMarketSnapshot")
          .RequireAuthorization();

        group.MapGet("/candles/{symbol}", async (string symbol, HttpContext ctx, IConfiguration config, IServiceProvider sp, MarketService svc, CancellationToken ct) =>
        {
            await RefreshLiveDataAsync(ctx, config, sp, symbol, ct);
            return Results.Ok(svc.GetCandles(symbol));
        }).WithName("GetMarketCandles")
          .RequireAuthorization();

        group.MapGet("/status", (MarketService svc) =>
            Results.Ok(svc.GetStatus()))
            .WithName("GetMarketStatus");
    }

    public static void AddMarketServices(this IServiceCollection services)
    {
        services.AddSingleton<OptionsEdge.API.Infrastructure.MockData.MockMarketDataService>();
        services.AddScoped<MarketService>();
    }

    // Live Groww data requires an authenticated user's own credentials (no platform-wide
    // account exists). When Groww is enabled and the caller is signed in, this refreshes the
    // shared "last known" snapshot/candles cache using their credentials before responding;
    // otherwise the response simply falls back to whatever's cached, or simulated data.
    //
    // GrowwMarketDataService is only resolved here — lazily, via IServiceProvider — so that
    // when Groww is disabled, the endpoint never has to materialize it (and its dependencies)
    // at all.
    private static async Task RefreshLiveDataAsync(
        HttpContext ctx, IConfiguration config, IServiceProvider sp, string? symbol, CancellationToken ct)
    {
        if (!config.GetValue<bool>("Groww:Enabled")) return;
        if (ctx.User.Identity?.IsAuthenticated != true) return;

        var growwMarketData = sp.GetRequiredService<GrowwMarketDataService>();
        var userId = ctx.GetUserId(config);
        var symbols = symbol is null ? ["NIFTY", "BANKNIFTY"] : new[] { symbol };

        foreach (var s in symbols)
            await growwMarketData.RefreshForUserAsync(userId, s, ct);
    }
}
