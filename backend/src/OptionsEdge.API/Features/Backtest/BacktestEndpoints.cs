using OptionsEdge.API.Common.Extensions;
using OptionsEdge.API.Infrastructure.Groww;

namespace OptionsEdge.API.Features.Backtest;

public static class BacktestEndpoints
{
    public static void MapBacktestEndpoints(this WebApplication app)
    {
        var backtest = app.MapGroup("/api/v1/backtest");

        // POST /api/v1/backtest/run
        backtest.MapPost("/run", async (
            BacktestRunRequest req,
            BacktestService backtestSvc,
            IConfiguration config,
            IServiceProvider sp,
            HttpContext ctx,
            ILogger<BacktestService> logger,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId(config);

            // Live data needs the user's own Groww credentials (no platform-wide account), so
            // refresh the candle cache on their behalf before backtesting against it. Failure
            // here shouldn't block the run — it just falls back to whatever's already cached.
            if (config.GetValue<bool>("Groww:Enabled"))
            {
                try
                {
                    var growwMarketData = sp.GetRequiredService<GrowwMarketDataService>();
                    await growwMarketData.RefreshForUserAsync(userId, req.Symbol, ct);
                    logger.LogInformation("Groww candles refreshed for backtest: {Symbol}", req.Symbol);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "Groww candle refresh failed before backtest for {Symbol} — using cached/mock data",
                        req.Symbol);
                }
            }

            try
            {
                var result = await backtestSvc.RunAsync(userId, req, ct);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).WithName("RunBacktest");

        // GET /api/v1/backtest/history
        backtest.MapGet("/history", async (
            BacktestService backtestSvc,
            IConfiguration config,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId(config);
            var history = await backtestSvc.GetHistoryAsync(userId, ct: ct);
            return Results.Ok(history);
        }).WithName("GetBacktestHistory");
    }

    public static void AddBacktestServices(this IServiceCollection services)
    {
        services.AddScoped<BacktestService>();
    }
}
