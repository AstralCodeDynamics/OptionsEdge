using Microsoft.AspNetCore.Identity;

namespace OptionsEdge.API.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public string SubscriptionPlan { get; set; } = "free";
    public decimal WalletBalance { get; set; }
    public int AiCallsToday { get; set; }
    public DateTimeOffset AiCallsResetAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Position> Positions { get; set; } = [];
    public ICollection<Signal> Signals { get; set; } = [];
    public ICollection<Alert> Alerts { get; set; } = [];
    public ICollection<ChatMessage> ChatMessages { get; set; } = [];
    public ICollection<AIUsageLog> AIUsageLogs { get; set; } = [];
    public ICollection<BacktestResult> BacktestResults { get; set; } = [];
}
