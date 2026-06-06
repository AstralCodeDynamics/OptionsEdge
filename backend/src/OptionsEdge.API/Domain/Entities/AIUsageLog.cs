namespace OptionsEdge.API.Domain.Entities;

public class AIUsageLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Feature { get; set; } = string.Empty;
    public string ModelUsed { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal CostUsd { get; set; }
    public decimal? WalletBefore { get; set; }
    public decimal? WalletAfter { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
