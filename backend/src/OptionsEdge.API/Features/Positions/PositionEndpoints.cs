using Microsoft.EntityFrameworkCore;
using OptionsEdge.API.Common.Extensions;
using OptionsEdge.API.Domain.Entities;
using OptionsEdge.API.Features.Options;
using OptionsEdge.API.Infrastructure.Background;
using OptionsEdge.API.Infrastructure.Data;

namespace OptionsEdge.API.Features.Positions;

public static class PositionEndpoints
{
    public static void MapPositionEndpoints(this WebApplication app)
    {
        var positions = app.MapGroup("/api/v1/positions")
            .RequireAuthorization();
        var alerts = app.MapGroup("/api/v1/alerts")
            .RequireAuthorization();

        // GET /api/v1/positions
        positions.MapGet("/", async (
            AppDbContext db,
            IConfiguration config,
            HttpContext ctx,
            PositionService positionSvc,
            OptionsService optionsSvc,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId(config);
            var list   = await db.Positions
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);

            return Results.Ok(list.Select(p =>
            {
                decimal ltp = optionsSvc.GetOptionLtp(p.Symbol, p.Strike, p.OptionType, p.Expiry.ToString("yyyy-MM-dd"));
                decimal pnl    = positionSvc.CalculatePnL(p, ltp);
                decimal pnlPct = positionSvc.CalculatePnLPct(p, pnl);
                var (_, slPct) = positionSvc.CalculateDistanceToSL(ltp, p.StopLoss);
                var (_, t1Pct) = positionSvc.CalculateDistanceToTarget(ltp, p.Target1);
                return ToResponse(p, ltp, pnl, pnlPct, slPct, t1Pct);
            }));
        }).WithName("GetPositions");

        // POST /api/v1/positions
        positions.MapPost("/", async (
            CreatePositionRequest req,
            AppDbContext db,
            IConfiguration config,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (!ValidateCreate(req, out var err))
                return Results.BadRequest(new { error = err });

            var userId   = ctx.GetUserId(config);
            var position = new Position
            {
                Id                = Guid.NewGuid(),
                UserId            = userId,
                Symbol            = req.Symbol.ToUpper(),
                Strike            = req.Strike,
                OptionType        = req.OptionType.ToUpper(),
                Expiry            = DateOnly.TryParse(req.Expiry, out var exp) ? exp : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                EntryPrice        = req.EntryPrice,
                Quantity          = req.Quantity,
                StopLoss          = req.StopLoss,
                Target1           = req.Target1,
                Target2           = req.Target2,
                SignalId          = req.SignalId,
                Status            = "active",
                IsAfterHoursEntry = !MarketHoursHelper.IsMarketOpen(),
                CreatedAt         = DateTimeOffset.UtcNow,
            };

            db.Positions.Add(position);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/positions/{position.Id}", ToResponse(position));
        }).WithName("CreatePosition");

        // PUT /api/v1/positions/{id}
        positions.MapPut("/{id:guid}", async (
            Guid id,
            UpdatePositionRequest req,
            AppDbContext db,
            IConfiguration config,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId   = ctx.GetUserId(config);
            var position = await db.Positions.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, ct);
            if (position is null) return Results.NotFound();

            if (req.StopLoss.HasValue) position.StopLoss = req.StopLoss.Value;
            if (req.Target1.HasValue)  position.Target1  = req.Target1.Value;
            if (req.Target2.HasValue)  position.Target2  = req.Target2.Value;

            if (req.Status is "closed" or "expired")
            {
                position.Status    = req.Status;
                position.ExitPrice  = req.ExitPrice;
                position.ExitReason = req.ExitReason;
                position.ClosedAt   = DateTimeOffset.UtcNow;
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(position));
        }).WithName("UpdatePosition");

        // DELETE /api/v1/positions/{id}
        positions.MapDelete("/{id:guid}", async (
            Guid id,
            AppDbContext db,
            IConfiguration config,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId   = ctx.GetUserId(config);
            var position = await db.Positions.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, ct);
            if (position is null) return Results.NotFound();

            position.Status     = "closed";
            position.ClosedAt   = DateTimeOffset.UtcNow;
            position.ExitReason = "manual";
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).WithName("ClosePosition");

        // GET /api/v1/positions/{id}/pnl
        positions.MapGet("/{id:guid}/pnl", async (
            Guid id,
            AppDbContext db,
            IConfiguration config,
            HttpContext ctx,
            PositionService positionSvc,
            OptionsService optionsSvc,
            CancellationToken ct) =>
        {
            var userId   = ctx.GetUserId(config);
            var position = await db.Positions.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, ct);
            if (position is null) return Results.NotFound();

            decimal ltp    = optionsSvc.GetOptionLtp(position.Symbol, position.Strike, position.OptionType, position.Expiry.ToString("yyyy-MM-dd"));
            decimal pnl    = positionSvc.CalculatePnL(position, ltp);
            decimal pnlPct = positionSvc.CalculatePnLPct(position, pnl);
            var (slRs, slPct)   = positionSvc.CalculateDistanceToSL(ltp, position.StopLoss);
            var (t1Rs, t1Pct)   = positionSvc.CalculateDistanceToTarget(ltp, position.Target1);
            decimal? t2Rs       = position.Target2.HasValue ? position.Target2.Value - ltp : null;
            decimal? t2Pct      = position.Target2.HasValue && ltp > 0 ? Math.Round((position.Target2.Value - ltp) / ltp * 100, 2) : null;
            decimal thetaDecay  = positionSvc.GetThetaDecayPercent(position, ltp);

            return Results.Ok(new PnLResponse(
                position.Id, position.EntryPrice, ltp, pnl, pnlPct,
                slRs, slPct, t1Rs, t1Pct, t2Rs, t2Pct, thetaDecay));
        }).WithName("GetPositionPnL");

        // GET /api/v1/alerts
        alerts.MapGet("/", async (
            AppDbContext db,
            IConfiguration config,
            HttpContext ctx,
            bool? unread = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default) =>
        {
            var userId = ctx.GetUserId(config);
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = db.Alerts
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .AsQueryable();

            if (unread == true)
                query = query.Where(a => !a.IsRead);

            var total = await query.CountAsync(ct);
            var list = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return Results.Ok(new
            {
                Items = list.Select(ToAlertResponse),
                Page = page,
                PageSize = pageSize,
                Total = total,
            });
        }).WithName("GetAlerts");

        // PUT /api/v1/alerts/{id}/read
        alerts.MapPut("/{id:guid}/read", async (
            Guid id,
            AppDbContext db,
            IConfiguration config,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId(config);
            var alert  = await db.Alerts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);
            if (alert is null) return Results.NotFound();

            alert.IsRead = true;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToAlertResponse(alert));
        }).WithName("MarkAlertRead");

        // PUT /api/v1/alerts/read-all
        alerts.MapPut("/read-all", async (
            AppDbContext db,
            IConfiguration config,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId(config);
            await db.Alerts
                .Where(a => a.UserId == userId && !a.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsRead, true), ct);
            return Results.NoContent();
        }).WithName("MarkAllAlertsRead");
    }

    public static void AddPositionServices(this IServiceCollection services)
    {
        services.AddSingleton<PositionService>();
        services.AddScoped<AlertService>();
    }

    private static bool ValidateCreate(CreatePositionRequest req, out string error)
    {
        error = string.Empty;
        if (req.Symbol.ToUpper() is not ("NIFTY" or "BANKNIFTY"))
        { error = "Symbol must be NIFTY or BANKNIFTY"; return false; }
        if (req.OptionType.ToUpper() is not ("CE" or "PE"))
        { error = "OptionType must be CE or PE"; return false; }
        if (req.EntryPrice <= 0)
        { error = "EntryPrice must be positive"; return false; }
        if (req.Quantity <= 0)
        { error = "Quantity must be positive"; return false; }
        if (req.StopLoss >= req.EntryPrice)
        { error = "StopLoss must be less than EntryPrice"; return false; }
        if (req.Target1 <= req.EntryPrice)
        { error = "Target1 must be greater than EntryPrice"; return false; }
        return true;
    }

    private static PositionResponse ToResponse(
        Position p,
        decimal? ltp      = null,
        decimal? pnl      = null,
        decimal? pnlPct   = null,
        decimal? slPct    = null,
        decimal? t1Pct    = null) =>
        new(p.Id,
            p.Symbol,
            p.Strike,
            p.OptionType,
            p.Expiry.ToString("yyyy-MM-dd"),
            p.EntryPrice,
            p.Quantity,
            p.StopLoss,
            p.Target1,
            p.Target2,
            p.SignalId,
            p.Status,
            p.IsAfterHoursEntry,
            p.ExitPrice,
            p.ExitReason,
            p.ClosedAt?.ToString("O"),
            p.CreatedAt.ToString("O"),
            ltp,
            pnl,
            pnlPct,
            slPct,
            t1Pct);

    private static AlertResponse ToAlertResponse(Domain.Entities.Alert a) =>
        new(a.Id, a.PositionId, a.Severity, a.AlertType, a.Message, a.IsRead, a.CreatedAt.ToString("O"));
}
