using Microsoft.EntityFrameworkCore;
using OptionsEdge.API.Common.Extensions;
using OptionsEdge.API.Infrastructure.Data;

namespace OptionsEdge.API.Features.Signals;

public static class SignalEndpoints
{
    public static void MapSignalEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/signals");

        // POST /api/v1/signals/generate
        group.MapPost("/generate", async (
            GenerateSignalRequest req,
            AISignalService svc,
            IConfiguration config,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Symbol) ||
                req.Symbol.ToUpper() is not ("NIFTY" or "BANKNIFTY"))
            {
                return Results.BadRequest(new { error = "Symbol must be NIFTY or BANKNIFTY" });
            }

            var userId = ctx.GetUserId(config);
            var (signal, error) = await svc.GenerateEntrySignalAsync(req.Symbol, userId, ct);

            if (error is not null)
                return Results.BadRequest(new { error });

            return Results.Ok(signal);
        }).WithName("GenerateSignal");

        // GET /api/v1/signals/history?symbol=NIFTY&limit=20
        group.MapGet("/history", async (
            AppDbContext db,
            IConfiguration config,
            HttpContext ctx,
            string? symbol,
            int limit = 20,
            CancellationToken ct = default) =>
        {
            var userId = ctx.GetUserId(config);
            var query  = db.Signals
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrEmpty(symbol))
                query = query.Where(s => s.Symbol == symbol.ToUpper());

            var signals = await query.Take(Math.Min(limit, 50)).ToListAsync(ct);

            return Results.Ok(signals.Select(s => new SignalResponse(
                s.Id,
                s.Symbol,
                s.SignalType,
                s.OptionType,
                s.Strike,
                s.Expiry.ToString("yyyy-MM-dd"),
                s.EntryLow,
                s.EntryHigh,
                s.StopLoss,
                s.Target1,
                s.Target2,
                s.Confidence,
                s.RiskReward,
                s.Rationale,
                s.ModelUsed,
                s.InputTokens,
                s.OutputTokens,
                s.CostUsd,
                s.ValidUntil.ToString("O"),
                s.CreatedAt.ToString("O"))));
        }).WithName("GetSignalHistory");

        // GET /api/v1/signals/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var s = await db.Signals.FindAsync([id], ct);
            if (s is null) return Results.NotFound();

            return Results.Ok(new SignalResponse(
                s.Id,
                s.Symbol,
                s.SignalType,
                s.OptionType,
                s.Strike,
                s.Expiry.ToString("yyyy-MM-dd"),
                s.EntryLow,
                s.EntryHigh,
                s.StopLoss,
                s.Target1,
                s.Target2,
                s.Confidence,
                s.RiskReward,
                s.Rationale,
                s.ModelUsed,
                s.InputTokens,
                s.OutputTokens,
                s.CostUsd,
                s.ValidUntil.ToString("O"),
                s.CreatedAt.ToString("O")));
        }).WithName("GetSignalById");
    }

    public static void AddSignalServices(this IServiceCollection services)
    {
        services.AddHttpClient("claude");
        services.AddSingleton<Infrastructure.Claude.ClaudeApiClient>();
        services.AddSingleton<SignalCacheService>();
        services.AddScoped<AISignalService>();
    }
}
