using OptionsEdge.API.Common.Extensions;
using OptionsEdge.API.Features.Groww;
using OptionsEdge.API.Features.Market;
using OptionsEdge.API.Infrastructure.Groww;

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
            OptionsService svc, GrowwCredentialService credentialSvc,
            ILogger<OptionsService> logger, CancellationToken ct) =>
        {
            if (config.GetValue<bool>("Groww:Enabled"))
            {
                var userId = ctx.GetUserId(config);
                if (!await credentialSvc.HasCredentialsAsync(userId, ct))
                    return Results.Ok(new GrowwGatedResponse<OptionsChainResponse>(false, false, null));

                var expiries = svc.GetExpiries(symbol);
                var selectedExpiry = expiry ?? expiries.FirstOrDefault() ?? "";
                var growwMarketData = sp.GetRequiredService<GrowwMarketDataService>();
                await growwMarketData.RefreshForUserAsync(userId, symbol, ct);
                try
                {
                    var growwUserApi = sp.GetRequiredService<GrowwUserApiClient>();
                    var growwChain = await growwUserApi.GetOptionChainAsync(userId, symbol, selectedExpiry, ct);
                    svc.CacheGrowwChain(symbol, selectedExpiry, growwChain);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Groww option chain refresh failed for {Symbol}", symbol);
                }
                var chain = svc.GetChain(symbol, selectedExpiry);
                return Results.Ok(new GrowwGatedResponse<OptionsChainResponse>(true, chain is not null, chain));
            }

            var expiriesFallback = svc.GetExpiries(symbol);
            var selectedExpiryFallback = expiry ?? expiriesFallback.FirstOrDefault() ?? "";
            var fallbackChain = svc.GetChain(symbol, selectedExpiryFallback);
            return Results.Ok(new GrowwGatedResponse<OptionsChainResponse>(true, fallbackChain is not null, fallbackChain));
        }).WithName("GetOptionsChain");

        group.MapGet("/expiries/{symbol}", (string symbol, OptionsService svc) =>
            Results.Ok(svc.GetExpiries(symbol)))
            .WithName("GetExpiries");

        group.MapGet("/maxpain/{symbol}", async (
            string symbol, string? expiry,
            HttpContext ctx, IConfiguration config, IServiceProvider sp,
            OptionsService svc, GrowwCredentialService credentialSvc,
            CancellationToken ct) =>
        {
            if (config.GetValue<bool>("Groww:Enabled"))
            {
                var userId = ctx.GetUserId(config);
                if (!await credentialSvc.HasCredentialsAsync(userId, ct))
                    return Results.Ok(new GrowwGatedResponse<MaxPainResponse>(false, false, null));

                var growwMarketData = sp.GetRequiredService<GrowwMarketDataService>();
                await growwMarketData.RefreshForUserAsync(userId, symbol, ct);
            }

            var expiries = svc.GetExpiries(symbol);
            var selectedExpiry = expiry ?? expiries.FirstOrDefault() ?? "";
            var maxPain = svc.GetMaxPain(symbol, selectedExpiry);
            return Results.Ok(new GrowwGatedResponse<MaxPainResponse>(true, maxPain is not null, maxPain));
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
