namespace OptionsEdge.API.Features.Indicators;

public static class IndicatorEndpoints
{
    public static void MapIndicatorEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/indicators/{symbol}", (string symbol, IndicatorService svc) =>
        {
            if (symbol.ToUpper() is not ("NIFTY" or "BANKNIFTY"))
                return Results.NotFound();
            return Results.Ok(svc.GetIndicators(symbol));
        }).WithName("GetIndicators")
          .RequireAuthorization();
    }

    public static void AddIndicatorServices(this IServiceCollection services)
    {
        services.AddSingleton<IndicatorService>();
    }
}
