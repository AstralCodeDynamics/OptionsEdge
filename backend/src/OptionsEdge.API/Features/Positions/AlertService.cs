using Microsoft.AspNetCore.SignalR;
using OptionsEdge.API.Domain.Entities;
using OptionsEdge.API.Infrastructure.Data;
using OptionsEdge.API.Infrastructure.SignalR;

namespace OptionsEdge.API.Features.Positions;

public class AlertService(
    AppDbContext db,
    IHubContext<MarketHub> hub,
    ILogger<AlertService> logger)
{
    public async Task<Alert> SaveAlertAsync(
        Guid positionId,
        Guid userId,
        string severity,
        string alertType,
        string message,
        CancellationToken ct = default)
    {
        var alert = new Alert
        {
            Id         = Guid.NewGuid(),
            UserId     = userId,
            PositionId = positionId,
            Severity   = severity,
            AlertType  = alertType,
            Message    = message,
            IsRead     = false,
            CreatedAt  = DateTimeOffset.UtcNow,
        };

        db.Alerts.Add(alert);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Alert saved: {AlertType} for position {PositionId}", alertType, positionId);
        return alert;
    }

    public async Task BroadcastAlertAsync(Alert alert, CancellationToken ct = default)
    {
        var payload = new AlertResponse(
            alert.Id,
            alert.PositionId,
            alert.Severity,
            alert.AlertType,
            alert.Message,
            alert.IsRead,
            alert.CreatedAt.ToString("O"));

        await hub.Clients
            .Group($"alerts:{alert.UserId}")
            .SendAsync("NewAlert", payload, ct);
    }
}
