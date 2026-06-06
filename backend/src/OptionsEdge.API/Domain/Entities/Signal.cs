using System.Text.Json;

namespace OptionsEdge.API.Domain.Entities;

public class Signal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string SignalType { get; set; } = string.Empty;
    public string OptionType { get; set; } = string.Empty;
    public int Strike { get; set; }
    public DateOnly Expiry { get; set; }
    public decimal EntryLow { get; set; }
    public decimal EntryHigh { get; set; }
    public decimal StopLoss { get; set; }
    public decimal Target1 { get; set; }
    public decimal? Target2 { get; set; }
    public int Confidence { get; set; }
    public decimal RiskReward { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public JsonDocument? MarketSnapshot { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal CostUsd { get; set; }
    public DateTimeOffset ValidUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
