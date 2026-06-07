namespace OptionsEdge.API.Features.Backtest;

public record BacktestRunRequest(
    string Symbol,
    string Strategy,
    string EntryCondition,
    string ExitCondition,
    int PeriodDays,
    int Lots);

public record BacktestTradeLogEntry(
    string EntryDate,
    string ExitDate,
    string Contract,
    decimal EntryPrice,
    decimal ExitPrice,
    decimal PnL,
    string ExitReason);

public record BacktestResultResponse(
    Guid Id,
    string Symbol,
    string Strategy,
    string EntryCondition,
    string ExitCondition,
    int PeriodDays,
    int Lots,
    decimal WinRate,
    int TotalTrades,
    decimal NetPnl,
    decimal MaxDrawdown,
    decimal SharpeRatio,
    decimal ProfitFactor,
    decimal AvgWin,
    decimal AvgLoss,
    IReadOnlyList<BacktestTradeLogEntry> TradeLog,
    string CreatedAt);
