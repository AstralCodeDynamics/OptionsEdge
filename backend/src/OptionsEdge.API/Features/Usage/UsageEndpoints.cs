using OptionsEdge.API.Common.Extensions;

namespace OptionsEdge.API.Features.Usage;

public static class UsageEndpoints
{
    public static void MapUsageEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/usage");

        // GET /api/v1/usage/stats
        group.MapGet("/stats", async (
            UsageService usageSvc,
            IConfiguration config,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId(config);
            var stats  = await usageSvc.GetStatsAsync(userId, ct);
            if (stats is null) return Results.NotFound();

            return Results.Ok(stats);
        }).WithName("GetUsageStats");
    }

    public static void AddUsageServices(this IServiceCollection services)
    {
        services.AddScoped<UsageService>();
    }
}
