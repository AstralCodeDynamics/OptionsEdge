using OptionsEdge.API.Infrastructure.Data;

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
            CancellationToken ct) =>
        {
            var userId = DevUserId(config);
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
            CancellationToken ct) =>
        {
            var userId = DevUserId(config);
            var history = await backtestSvc.GetHistoryAsync(userId, ct: ct);
            return Results.Ok(history);
        }).WithName("GetBacktestHistory");
    }

    public static void AddBacktestServices(this IServiceCollection services)
    {
        services.AddScoped<BacktestService>();
    }

    // Phase 3 dev user; replaced by JWT claim in Phase 8
    private static Guid DevUserId(IConfiguration config) =>
        Guid.TryParse(config["Dev:UserId"], out var id) ? id : DevDataSeeder.DevUserId;
}
