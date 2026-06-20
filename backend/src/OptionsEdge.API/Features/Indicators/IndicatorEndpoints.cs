using OptionsEdge.API.Common.Extensions;
using OptionsEdge.API.Features.Groww;
using OptionsEdge.API.Features.Market;

namespace OptionsEdge.API.Features.Indicators;

public static class IndicatorEndpoints
{
    public static void MapIndicatorEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/indicators/{symbol}", async (
            string symbol, HttpContext ctx, IConfiguration config,
            IndicatorService svc, GrowwCredentialService credentialSvc,
            CancellationToken ct) =>
        {
            if (symbol.ToUpper() is not ("NIFTY" or "BANKNIFTY"))
                return Results.NotFound();

            if (config.GetValue<bool>("Groww:Enabled"))
            {
                var userId = ctx.GetUserId(config);
                if (!await credentialSvc.HasCredentialsAsync(userId, ct))
                    return Results.Ok(new GrowwGatedResponse<IndicatorsResponse>(false, null));
            }

            return Results.Ok(new GrowwGatedResponse<IndicatorsResponse>(true, svc.GetIndicators(symbol)));
        }).WithName("GetIndicators")
          .RequireAuthorization();
    }

    public static void AddIndicatorServices(this IServiceCollection services)
    {
        services.AddSingleton<IndicatorService>();
    }
}
