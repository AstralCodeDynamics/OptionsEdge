using OptionsEdge.API.Common.Extensions;
using OptionsEdge.API.Features.Groww;

namespace OptionsEdge.API.Features.Options;

public static class OptionsEndpoints
{
    public static void MapOptionsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/options")
            .RequireAuthorization();

        group.MapGet("/chain/{symbol}", async (
            string symbol, string? expiry,
            HttpContext ctx, IConfiguration config, IServiceProvider sp,
            OptionsService svc, ILogger<OptionsService> logger, CancellationToken ct) =>
        {
            var expiries = svc.GetExpiries(symbol);
            var selectedExpiry = expiry ?? expiries.FirstOrDefault() ?? "";

            if (config.GetValue<bool>("Groww:Enabled"))
            {
                try
                {
                    var growwUserApi = sp.GetRequiredService<GrowwUserApiClient>();
                    var userId = ctx.GetUserId(config);
                    var chain = await growwUserApi.GetOptionChainAsync(userId, symbol, selectedExpiry, ct);
                    svc.CacheGrowwChain(symbol, selectedExpiry, chain);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Groww option chain refresh failed for {Symbol}, using synthetic", symbol);
                }
            }

            return Results.Ok(svc.GetChain(symbol, selectedExpiry));
        }).WithName("GetOptionsChain");

        group.MapGet("/expiries/{symbol}", (string symbol, OptionsService svc) =>
            Results.Ok(svc.GetExpiries(symbol)))
            .WithName("GetExpiries");

        group.MapGet("/maxpain/{symbol}", (string symbol, string? expiry, OptionsService svc) =>
        {
            var expiries = svc.GetExpiries(symbol);
            var selectedExpiry = expiry ?? expiries.FirstOrDefault() ?? "";
            return Results.Ok(svc.GetMaxPain(symbol, selectedExpiry));
        }).WithName("GetMaxPain");

        group.MapPost("/payoff", (PayoffRunRequest req, OptionsService svc) =>
        {
            try
            {
                return Results.Ok(svc.ComputePayoff(req.Legs));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).WithName("ComputePayoff");
    }

    public static void AddOptionsServices(this IServiceCollection services)
    {
        services.AddSingleton<OptionsService>();
    }
}
