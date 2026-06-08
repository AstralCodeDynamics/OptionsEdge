using OptionsEdge.API.Common.Extensions;
using OptionsEdge.API.Infrastructure.Groww;
using OptionsEdge.API.Infrastructure.MockData;
using OtpNet;

namespace OptionsEdge.API.Features.Groww;

public static class GrowwEndpoints
{
    public static void MapGrowwEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/groww");

        // POST /api/v1/groww/credentials — save (or replace) the current user's Groww API
        // credentials. Validates them by generating a TOTP and authenticating with Groww
        // before persisting anything; nothing is saved if the test auth fails.
        group.MapPost("/credentials", async (
            SaveGrowwCredentialsRequest req,
            GrowwCredentialService credentialService,
            GrowwUserApiClient groww,
            IConfiguration config,
            HttpContext ctx,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            if (!config.GetValue<bool>("Groww:Enabled"))
                return Results.BadRequest(new { error = "Groww integration is not enabled on this server." });

            var apiKey = req.ApiKey?.Trim() ?? "";
            var apiSecret = req.ApiSecret?.Trim() ?? "";

            if (string.IsNullOrEmpty(apiKey))
                return Results.BadRequest(new { error = "Enter your Groww TOTP Token (ApiKey)." });
            if (string.IsNullOrEmpty(apiSecret))
                return Results.BadRequest(new { error = "Enter your Groww TOTP Secret (ApiSecret)." });

            try
            {
                Base32Encoding.ToBytes(apiSecret);
            }
            catch (Exception)
            {
                return Results.BadRequest(new { error = "TOTP Secret is not valid Base32. Copy it again from your Groww API keys page." });
            }

            var userId = ctx.GetUserId(config);

            try
            {
                await groww.TestAuthenticateAsync(apiKey, apiSecret, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Groww credential validation failed for user {UserId}", userId);
                return Results.BadRequest(new { error = "Failed to connect to Groww with these credentials. Double-check the Token and Secret and try again." });
            }

            await credentialService.SaveCredentialsAsync(userId, apiKey, apiSecret, ct);
            groww.InvalidateToken(userId);

            return Results.Ok(new GrowwCredentialsResponse(true, "Groww connected. Credentials saved securely."));
        }).WithName("SaveGrowwCredentials");

        // DELETE /api/v1/groww/credentials — disconnects Groww for the current user
        group.MapDelete("/credentials", async (
            GrowwCredentialService credentialService,
            GrowwUserApiClient groww,
            IConfiguration config,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId(config);
            await credentialService.RemoveCredentialsAsync(userId, ct);
            groww.InvalidateToken(userId);
            return Results.NoContent();
        }).WithName("RemoveGrowwCredentials");

        // GET /api/v1/groww/status — whether Groww is enabled, whether the current user has
        // saved credentials, and (if so) whether those credentials currently authenticate.
        // On the first successful auth after a token refresh, also imports open positions
        // from the user's Groww portfolio.
        group.MapGet("/status", async (
            GrowwCredentialService credentialService,
            GrowwUserApiClient groww,
            GrowwOrderService orderService,
            IConfiguration config,
            HttpContext ctx,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            bool enabled = config.GetValue<bool>("Groww:Enabled");
            bool orderPlacementEnabled = config.GetValue<bool>("Groww:OrderPlacementEnabled");

            if (!enabled)
                return Results.Ok(new GrowwStatusResponse(false, false, false, null, orderPlacementEnabled, null));

            var userId = ctx.GetUserId(config);
            bool hasCredentials = await credentialService.HasCredentialsAsync(userId, ct);

            if (!hasCredentials)
                return Results.Ok(new GrowwStatusResponse(true, false, false, null, orderPlacementEnabled, null));

            try
            {
                await groww.GetOrRefreshTokenAsync(userId, ct);

                if (groww.TryConsumeImportFlag(userId))
                    await orderService.ImportPositionsFromGrowwAsync(userId, ct);

                return Results.Ok(new GrowwStatusResponse(true, true, true, GrowwUserApiClient.NextTokenExpiry(), orderPlacementEnabled, null));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Groww authentication check failed for user {UserId}", userId);
                return Results.Ok(new GrowwStatusResponse(true, true, false, null, orderPlacementEnabled, ex.Message));
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
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (!config.GetValue<bool>("Groww:Enabled"))
                return Results.BadRequest(new { error = "Groww integration is disabled." });

            try
            {
                var userId = ctx.GetUserId(config);
                var cancelled = await orderService.CancelOrderAsync(userId, orderId, ct);
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
        services.AddScoped<GrowwCredentialService>();
        services.AddScoped<GrowwUserApiClient>();
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
