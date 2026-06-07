using OptionsEdge.API.Infrastructure.Data;
using OptionsEdge.API.Infrastructure.Groww;
using OptionsEdge.API.Infrastructure.MockData;

namespace OptionsEdge.API.Features.Groww;

public static class GrowwEndpoints
{
    public static void MapGrowwEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/groww");

        // POST /api/v1/groww/connect — submit a TOTP to authenticate with Groww and cache the access token
        group.MapPost("/connect", async (
            ConnectGrowwRequest req,
            GrowwApiClient groww,
            GrowwOrderService orderService,
            IConfiguration config,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            if (!config.GetValue<bool>("Groww:Enabled"))
                return Results.BadRequest(new { error = "Groww integration is disabled. Set Groww:Enabled to true in configuration." });

            if (string.IsNullOrWhiteSpace(req.Totp) || req.Totp.Trim().Length != 6)
                return Results.BadRequest(new { error = "Enter the 6-digit TOTP from your authenticator app." });

            try
            {
                await groww.AuthenticateAsync(req.Totp.Trim(), ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Groww connect failed");
                return Results.BadRequest(new { error = "Failed to connect to Groww. Check your TOTP and try again." });
            }

            var userId = DevUserId(config);
            var imported = await orderService.ImportPositionsFromGrowwAsync(userId, ct);

            return Results.Ok(new ConnectGrowwResponse(true, GrowwApiClient.NextTokenExpiry(), imported));
        }).WithName("ConnectGroww");

        // GET /api/v1/groww/status — whether Groww integration is enabled and currently connected
        group.MapGet("/status", (GrowwApiClient groww, IConfiguration config) =>
        {
            bool enabled = config.GetValue<bool>("Groww:Enabled");
            bool connected = enabled && groww.IsConnected;
            return Results.Ok(new GrowwStatusResponse(enabled, connected, connected ? GrowwApiClient.NextTokenExpiry() : null));
        }).WithName("GetGrowwStatus");

        // POST /api/v1/orders/place — places a live F&O order via Groww
        app.MapPost("/api/v1/orders/place", async (
            PlaceOrderRequest req,
            GrowwOrderService orderService,
            IConfiguration config,
            CancellationToken ct) =>
        {
            if (!config.GetValue<bool>("Groww:Enabled"))
                return Results.BadRequest(new { error = "Groww integration is disabled." });

            try
            {
                var userId = DevUserId(config);
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

    // Phase 5.5 dev user; replaced by JWT claim in Phase 8
    private static Guid DevUserId(IConfiguration config) =>
        Guid.TryParse(config["Dev:UserId"], out var id) ? id : DevDataSeeder.DevUserId;
}
