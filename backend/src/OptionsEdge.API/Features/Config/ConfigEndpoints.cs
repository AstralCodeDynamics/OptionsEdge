using Microsoft.Extensions.Options;
using OptionsEdge.API.Common.Options;

namespace OptionsEdge.API.Features.Config;

public static class ConfigEndpoints
{
    public static void MapConfigEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/config");

        group.MapGet("/lot-sizes", (IOptionsMonitor<LotSizeOptions> lotSizeOptions) =>
        {
            var current = lotSizeOptions.CurrentValue;
            return Results.Ok(new Dictionary<string, int>
            {
                ["NIFTY"] = current.NIFTY,
                ["BANKNIFTY"] = current.BANKNIFTY,
            });
        }).WithName("GetLotSizes");
    }
}
