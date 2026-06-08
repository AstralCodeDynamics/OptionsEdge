using OptionsEdge.API.Common.Extensions;
using OptionsEdge.API.Infrastructure.Groww;

namespace OptionsEdge.API.Features.Backtest;

public static class BacktestEndpoints
{
    public static void MapBacktestEndpoints(this WebApplication app)
    {
        var backtest = app.MapGroup("/api/v1/backtest")
            .RequireAuthorization();

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
            // fetch fresh historical candles on their behalf before backtesting. When Groww is
            // enabled, backtests should use Groww data rather than quietly falling back to mock.
            if (config.GetValue<bool>("Groww:Enabled"))
            {
                try
                {
                    var growwMarketData = sp.GetRequiredService<GrowwMarketDataService>();
                    var candles = await growwMarketData.RefreshCandlesForBacktestAsync(userId, req.Symbol, ct);
                    logger.LogInformation("Groww candles refreshed for backtest: {Symbol}, {Count} candles", req.Symbol, candles.Count);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "Groww candle refresh failed before backtest for {Symbol}",
                        req.Symbol);
                    return Results.BadRequest(new
                    {
                        error = $"Could not fetch Groww historical candles for {req.Symbol}. Check Groww connection and try again."
                    });
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
            int? page,
            int? pageSize,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId(config);
            var history = await backtestSvc.GetHistoryAsync(userId, page ?? 1, pageSize ?? 8, ct);
            return Results.Ok(history);
        }).WithName("GetBacktestHistory");
    }

    public static void AddBacktestServices(this IServiceCollection services)
    {
        services.AddScoped<BacktestService>();
    }
}
