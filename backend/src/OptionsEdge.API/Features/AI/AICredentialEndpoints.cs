using System.Net;
using OptionsEdge.API.Common.Extensions;

namespace OptionsEdge.API.Features.AI;

public static class AICredentialEndpoints
{
    public static void MapAICredentialEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/ai")
            .RequireAuthorization();

        // POST /api/v1/ai/credentials — save (or replace) the current user's Anthropic API
        // key. Validates it with a 1-token test call before persisting anything.
        group.MapPost("/credentials", async (
            SaveAIKeyRequest req,
            UserAICredentialService aiCredentialService,
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var apiKey = req.ApiKey?.Trim() ?? "";
            if (!apiKey.StartsWith("sk-ant-"))
                return Results.BadRequest(new { error = "Invalid key format. Must start with sk-ant-" });

            using var http = httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Add("x-api-key", apiKey);
            http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var testBody = JsonContent.Create(new
            {
                model = config["Claude:HaikuModel"] ?? "claude-haiku-4-5-20251001",
                max_tokens = 1,
                messages = new[] { new { role = "user", content = "hi" } },
            });

            var testRes = await http.PostAsync("https://api.anthropic.com/v1/messages", testBody, ct);

            if (testRes.StatusCode == HttpStatusCode.Unauthorized)
                return Results.UnprocessableEntity(new { error = "Invalid API key. Please check your key at console.anthropic.com" });

            var userId = ctx.GetUserId(config);
            await aiCredentialService.SaveAsync(userId, apiKey, ct);

            return Results.Ok(new { message = "AI key saved and verified." });
        }).WithName("SaveAICredentials");

        // DELETE /api/v1/ai/credentials — removes the current user's Anthropic API key
        group.MapDelete("/credentials", async (
            UserAICredentialService aiCredentialService,
            IConfiguration config,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId(config);
            await aiCredentialService.RemoveAsync(userId, ct);
            return Results.NoContent();
        }).WithName("RemoveAICredentials");

        // GET /api/v1/ai/credentials/status — whether the current user has an active key
        group.MapGet("/credentials/status", async (
            UserAICredentialService aiCredentialService,
            IConfiguration config,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId(config);
            bool hasKey = await aiCredentialService.HasKeyAsync(userId, ct);

            return Results.Ok(new
            {
                hasKey,
                message = hasKey
                    ? "Your Anthropic API key is configured."
                    : "No API key set. Get yours from console.anthropic.com",
            });
        }).WithName("GetAICredentialStatus");
    }

    public static void AddAIServices(this IServiceCollection services)
    {
        services.AddScoped<UserAICredentialService>();
    }
}
