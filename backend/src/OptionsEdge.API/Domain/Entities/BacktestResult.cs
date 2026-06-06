using System.Text.Json;

namespace OptionsEdge.API.Domain.Entities;

public class BacktestResult
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Strategy { get; set; } = string.Empty;
    public JsonDocument? Parameters { get; set; }
    public decimal WinRate { get; set; }
    public int TotalTrades { get; set; }
    public decimal NetPnl { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal ProfitFactor { get; set; }
    public JsonDocument? TradeLog { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
