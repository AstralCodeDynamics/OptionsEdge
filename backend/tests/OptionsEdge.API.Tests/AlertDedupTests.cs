using OptionsEdge.API.Domain.Entities;

namespace OptionsEdge.API.Tests;

// Proves that the DB-backed alert dedup predicate used in PositionMonitorWorker
// correctly detects duplicates that would survive a process restart (i.e., after
// the in-memory cache is wiped). The predicate is evaluated here as LINQ-to-objects
// using the exact same expression that EF Core translates to SQL in production.
public class AlertDedupTests
{
    // The same predicate PositionMonitorWorker evaluates via EF Core's AnyAsync.
    private static bool HasRecentDuplicate(IEnumerable<Alert> alerts, Guid positionId, string alertType)
        => alerts.Any(a =>
               a.PositionId == positionId &&
               a.AlertType  == alertType  &&
               a.CreatedAt  > DateTimeOffset.UtcNow.AddMinutes(-15));

    // Simulate: alert fires and is saved to DB. Process restarts (cache wiped).
    // Same condition re-evaluated 5 min later — DB check must find the alert and
    // block the duplicate.
    [Fact]
    public void DedupPredicate_ReturnsDuplicate_WhenAlertSavedWithin15Minutes()
    {
        var positionId = Guid.NewGuid();
        const string alertType = "ADVERSE_MOVE";

        var alerts = new[]
        {
            new Alert
            {
                Id         = Guid.NewGuid(),
                UserId     = Guid.NewGuid(),
                PositionId = positionId,
                AlertType  = alertType,
                Severity   = "Danger",
                Message    = "Spot moved -0.8% in 15 min",
                IsRead     = false,
                CreatedAt  = DateTimeOffset.UtcNow.AddMinutes(-5), // 5 min ago — within window
            },
        };

        Assert.True(HasRecentDuplicate(alerts, positionId, alertType),
            "DB dedup must detect an alert saved within the 15-minute window, " +
            "blocking the duplicate even after an in-memory cache wipe.");
    }

    // Alert saved 20 min ago — outside window — must allow re-fire.
    [Fact]
    public void DedupPredicate_AllowsRefiring_WhenExistingAlertIsOlderThanWindow()
    {
        var positionId = Guid.NewGuid();
        const string alertType = "ADVERSE_MOVE";

        var alerts = new[]
        {
            new Alert
            {
                Id         = Guid.NewGuid(),
                UserId     = Guid.NewGuid(),
                PositionId = positionId,
                AlertType  = alertType,
                Severity   = "Danger",
                Message    = "old alert",
                IsRead     = false,
                CreatedAt  = DateTimeOffset.UtcNow.AddMinutes(-20), // outside window
            },
        };

        Assert.False(HasRecentDuplicate(alerts, positionId, alertType),
            "Alert outside the 15-minute window should not block a new fire.");
    }

    // No prior alert — must allow first fire.
    [Fact]
    public void DedupPredicate_AllowsFiring_WhenNoAlertExists()
    {
        var positionId = Guid.NewGuid();

        Assert.False(HasRecentDuplicate(Array.Empty<Alert>(), positionId, "SL_HIT"),
            "No existing alert should not block the first fire.");
    }

    // Different AlertType for the same position must not suppress unrelated alerts.
    [Fact]
    public void DedupPredicate_DoesNotSuppressUnrelatedAlertType()
    {
        var positionId = Guid.NewGuid();

        var alerts = new[]
        {
            new Alert
            {
                Id         = Guid.NewGuid(),
                UserId     = Guid.NewGuid(),
                PositionId = positionId,
                AlertType  = "ADVERSE_MOVE",
                Severity   = "Danger",
                Message    = "existing alert for different type",
                IsRead     = false,
                CreatedAt  = DateTimeOffset.UtcNow.AddMinutes(-2),
            },
        };

        Assert.False(HasRecentDuplicate(alerts, positionId, "SL_HIT"),
            "An ADVERSE_MOVE alert must not suppress an unrelated SL_HIT alert type.");
    }
}
