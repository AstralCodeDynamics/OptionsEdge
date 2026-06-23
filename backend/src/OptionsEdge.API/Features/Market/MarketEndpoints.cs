using OptionsEdge.API.Common.Extensions;
using OptionsEdge.API.Features.Groww;
using OptionsEdge.API.Infrastructure.Groww;

namespace OptionsEdge.API.Features.Market;

public static class MarketEndpoints
{
    public static void MapMarketEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/market")
            .RequireAuthorization();

        group.MapGet("/snapshot", async (
            HttpContext ctx, IConfiguration config,
            MarketService svc, IServiceProvider sp,
            GrowwCredentialService credentialSvc, CancellationToken ct) =>
        {
            if (config.GetValue<bool>("Groww:Enabled"))
            {
                var userId = ctx.GetUserId(config);
                if (!await credentialSvc.HasCredentialsAsync(userId, ct))
                    return Results.Ok(new GrowwGatedResponse<IEnumerable<MarketSnapshotResponse>>(false, false, null));
            }
            await RefreshLiveDataAsync(ctx, config, sp, null, ct);
            var snapshots = svc.GetSnapshots();
            bool isDataFresh = !config.GetValue<bool>("Groww:Enabled") || snapshots.Count == 2;
            return Results.Ok(new GrowwGatedResponse<IEnumerable<MarketSnapshotResponse>>(
                true, isDataFresh, isDataFresh ? snapshots : null));
        }).WithName("GetMarketSnapshots");

        group.MapGet("/snapshot/{symbol}", async (
            string symbol, HttpContext ctx, IConfiguration config,
            MarketService svc, IServiceProvider sp,
            GrowwCredentialService credentialSvc, CancellationToken ct) =>
        {
            if (config.GetValue<bool>("Groww:Enabled"))
            {
                var userId = ctx.GetUserId(config);
                if (!await credentialSvc.HasCredentialsAsync(userId, ct))
                    return Results.Ok(new GrowwGatedResponse<MarketSnapshotResponse>(false, false, null));
            }
            await RefreshLiveDataAsync(ctx, config, sp, symbol, ct);
            var snap = svc.GetSnapshot(symbol);
            return Results.Ok(new GrowwGatedResponse<MarketSnapshotResponse>(true, snap is not null, snap));
        }).WithName("GetMarketSnapshot");

        group.MapGet("/candles/{symbol}", async (
            string symbol, HttpContext ctx, IConfiguration config,
            IServiceProvider sp, MarketService svc,
            GrowwCredentialService credentialSvc, CancellationToken ct) =>
        {
            if (config.GetValue<bool>("Groww:Enabled"))
            {
                var userId = ctx.GetUserId(config);
                if (!await credentialSvc.HasCredentialsAsync(userId, ct))
                    return Results.Ok(new GrowwGatedResponse<IEnumerable<CandleResponse>>(false, false, null));
            }
            await RefreshLiveDataAsync(ctx, config, sp, symbol, ct);
            var candles = svc.GetCandles(symbol);
            bool isDataFresh = !config.GetValue<bool>("Groww:Enabled") || candles.Count > 0;
            return Results.Ok(new GrowwGatedResponse<IEnumerable<CandleResponse>>(
                true, isDataFresh, isDataFresh ? candles : null));
        }).WithName("GetMarketCandles");

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
    // otherwise the response exposes whether cached live data is fresh via IsDataFresh.
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
