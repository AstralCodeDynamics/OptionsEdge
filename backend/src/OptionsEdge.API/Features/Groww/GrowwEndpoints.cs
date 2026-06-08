using OptionsEdge.API.Common.Extensions;
using OptionsEdge.API.Infrastructure.Groww;
using OptionsEdge.API.Infrastructure.MockData;

namespace OptionsEdge.API.Features.Groww;

public static class GrowwEndpoints
{
    public static void MapGrowwEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/groww");

        // GET /api/v1/groww/status — checks (and triggers, if needed) the automatic TOTP
        // authentication, reporting whether Groww is enabled, connected, and when the
        // current access token expires. On the first successful auth after a token refresh,
        // also imports open positions from the Groww portfolio for the calling user.
        group.MapGet("/status", async (
            GrowwApiClient groww,
            GrowwOrderService orderService,
            IConfiguration config,
            HttpContext ctx,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            bool enabled = config.GetValue<bool>("Groww:Enabled");
            bool orderPlacementEnabled = config.GetValue<bool>("Groww:OrderPlacementEnabled");

            if (!enabled)
                return Results.Ok(new GrowwStatusResponse(false, false, null, orderPlacementEnabled, null));

            try
            {
                await groww.GetOrRefreshTokenAsync(ct);

                if (groww.TryConsumeImportFlag())
                {
                    var userId = ctx.GetUserId(config);
                    await orderService.ImportPositionsFromGrowwAsync(userId, ct);
                }

                return Results.Ok(new GrowwStatusResponse(true, true, GrowwApiClient.NextTokenExpiry(), orderPlacementEnabled, null));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Groww authentication check failed");
                return Results.Ok(new GrowwStatusResponse(true, false, null, orderPlacementEnabled, ex.Message));
            }
        }).WithName("GetGrowwStatus");

        // POST /api/v1/orders/place — places a live F&O order via Groww
        app.MapPost("/api/v1/orders/place", async (
            PlaceOrderRequest req,
            GrowwOrderService orderService,
            IConfiguration config,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (!config.GetValue<bool>("Groww:Enabled"))
                return Results.BadRequest(new { error = "Groww integration is disabled." });

            if (!config.GetValue<bool>("Groww:OrderPlacementEnabled"))
                return Results.BadRequest(new { error = "Order placement requires a whitelisted static IP. Deploy to production server to enable live orders. See Groww API Keys dashboard to whitelist your server IP." });

            try
            {
                var userId = ctx.GetUserId(config);
                var result = await orderService.PlaceOrderAsync(userId, req, ct);
                return Results.Ok(result);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or HttpRequestException)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).WithName("PlaceOrder");

        // POST /api/v1/orders/{orderId}/cancel — cancels a live F&O order via Groww
        app.MapPost("/api/v1/orders/{orderId}/cancel", async (
            string orderId,
            GrowwOrderService orderService,
            IConfiguration config,
            CancellationToken ct) =>
        {
            if (!config.GetValue<bool>("Groww:Enabled"))
                return Results.BadRequest(new { error = "Groww integration is disabled." });

            try
            {
                var cancelled = await orderService.CancelOrderAsync(orderId, ct);
                return Results.Ok(new { orderId, cancelled });
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).WithName("CancelOrder");
    }

    public static void AddGrowwServices(this IServiceCollection services)
    {
        services.AddHttpClient("groww", (sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            client.BaseAddress = new Uri(config["Groww:BaseUrl"] ?? "https://api.groww.in");
        });

        services.AddSingleton<GrowwApiClient>();
        services.AddSingleton<GrowwMarketDataService>();
        services.AddScoped<GrowwOrderService>();

        // Factory pattern: swap the live IMarketDataService implementation via "Groww:Enabled"
        services.AddSingleton<IMarketDataService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            return config.GetValue<bool>("Groww:Enabled")
                ? sp.GetRequiredService<GrowwMarketDataService>()
                : sp.GetRequiredService<MockMarketDataService>();
        });
    }
}
