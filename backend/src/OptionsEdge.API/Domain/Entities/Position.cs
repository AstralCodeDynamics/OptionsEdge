namespace OptionsEdge.API.Domain.Entities;

public class Position
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public int Strike { get; set; }
    public string OptionType { get; set; } = string.Empty;
    public DateOnly Expiry { get; set; }
    public decimal EntryPrice { get; set; }
    public int Quantity { get; set; }
    public decimal StopLoss { get; set; }
    public decimal Target1 { get; set; }
    public decimal? Target2 { get; set; }
    public Guid? SignalId { get; set; }
    public string Status { get; set; } = "active";
    public decimal? ExitPrice { get; set; }
    public string? ExitReason { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Alert> Alerts { get; set; } = [];
}
