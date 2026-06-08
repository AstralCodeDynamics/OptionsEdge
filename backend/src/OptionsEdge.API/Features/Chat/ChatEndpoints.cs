using System.Text.Json;
using OptionsEdge.API.Common.Extensions;

namespace OptionsEdge.API.Features.Chat;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/chat")
            .RequireAuthorization();

        // POST /api/v1/chat/message — streams the assistant reply as Server-Sent Events
        group.MapPost("/message", async Task (
            ChatMessageRequest req,
            ChatService svc,
            IConfiguration config,
            HttpContext ctx,
            HttpResponse response,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Message))
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                await response.WriteAsJsonAsync(new { error = "Message cannot be empty" }, ct);
                return;
            }

            var userId = ctx.GetUserId(config);
            var error  = await svc.ValidateAsync(userId, ct);
            if (error is not null)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                await response.WriteAsJsonAsync(new { error }, ct);
                return;
            }

            response.ContentType = "text/event-stream";
            response.Headers.CacheControl = "no-cache";
            response.Headers["X-Accel-Buffering"] = "no";

            await foreach (var chunk in svc.StreamMessageAsync(userId, req.SessionId, req.Message, ct))
            {
                await response.WriteAsync($"event: {chunk.Type}\n", ct);
                await response.WriteAsync($"data: {JsonSerializer.Serialize(chunk)}\n\n", ct);
                await response.Body.FlushAsync(ct);
            }
        }).WithName("SendChatMessage");

        // GET /api/v1/chat/history?sessionId=...
        group.MapGet("/history", async (
            Guid sessionId,
            ChatService svc,
            IConfiguration config,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId  = ctx.GetUserId(config);
            var history = await svc.GetHistoryAsync(userId, sessionId, ct);
            return Results.Ok(history);
        }).WithName("GetChatHistory");

        // POST /api/v1/chat/new-session
        group.MapPost("/new-session", () =>
            Results.Ok(new NewSessionResponse(Guid.NewGuid()))
        ).WithName("NewChatSession");
    }

    public static void AddChatServices(this IServiceCollection services)
    {
        services.AddScoped<ChatService>();
    }
}
