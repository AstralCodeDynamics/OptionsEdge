using Microsoft.EntityFrameworkCore;
using OptionsEdge.API.Common.Extensions;
using OptionsEdge.API.Infrastructure.Data;

namespace OptionsEdge.API.Features.Signals;

public static class SignalEndpoints
{
    public static void MapSignalEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/signals")
            .RequireAuthorization();

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

        // GET /api/v1/signals/history?page=1&pageSize=20
        group.MapGet("/history", async (
            AppDbContext db,
            IConfiguration config,
            HttpContext ctx,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var userId = ctx.GetUserId(config);
            var query = db.Signals
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt);

            var total = await query.CountAsync(ct);
            var signals = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var items = signals.Select(s => new SignalHistoryItem(
                s.Id,
                s.Symbol,
                s.SignalType,
                s.Strike,
                s.OptionType,
                s.Expiry.ToString("yyyy-MM-dd"),
                s.Confidence,
                s.EntryLow,
                s.EntryHigh,
                s.Target1,
                s.Target2,
                s.StopLoss,
                s.RiskReward,
                s.ModelUsed,
                s.CostUsd,
                s.CreatedAt.ToString("O"),
                s.ValidUntil.ToString("O"))).ToList();

            var totalPages = (int)Math.Ceiling((double)total / pageSize);
            return Results.Ok(new SignalHistoryResponse(items, page, pageSize, total, totalPages));
        }).WithName("GetSignalHistory");

        // GET /api/v1/signals/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            AppDbContext db,
            IConfiguration config,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId(config);
            var s = await db.Signals.FirstOrDefaultAsync(signal => signal.Id == id && signal.UserId == userId, ct);
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

        // GET /api/v1/signals/preferences — current user's NIFTY/BANKNIFTY auto-signal schedule
        group.MapGet("/preferences", async (
            UserSignalPreferenceService prefService,
            IConfiguration config,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId(config);
            var pref = await prefService.GetOrCreateAsync(userId, ct);

            return Results.Ok(new SignalPreferenceResponse(
                pref.NiftyAutoSignalEnabled,
                pref.NiftyAutoSignalTimes,
                pref.BankNiftyAutoSignalEnabled,
                pref.BankNiftyAutoSignalTimes));
        }).WithName("GetSignalPreferences");

        // PUT /api/v1/signals/preferences — save the current user's auto-signal schedule
        group.MapPut("/preferences", async (
            SignalPreferenceRequest req,
            UserSignalPreferenceService prefService,
            IConfiguration config,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId(config);
            await prefService.SaveAsync(
                userId,
                req.NiftyAutoSignalEnabled,
                req.NiftyAutoSignalTimes,
                req.BankNiftyAutoSignalEnabled,
                req.BankNiftyAutoSignalTimes,
                ct);

            return Results.Ok(new { message = "Signal preferences saved." });
        }).WithName("SaveSignalPreferences");
    }

    public static void AddSignalServices(this IServiceCollection services)
    {
        services.AddHttpClient("claude");
        services.AddSingleton<Infrastructure.Claude.ClaudeApiClient>();
        services.AddSingleton<SignalCacheService>();
        services.AddScoped<AISignalService>();
        services.AddScoped<UserSignalPreferenceService>();
    }
}
