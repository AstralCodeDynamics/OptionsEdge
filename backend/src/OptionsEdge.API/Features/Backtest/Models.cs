namespace OptionsEdge.API.Features.Backtest;

public record BacktestRunRequest(
    string Symbol,
    string Strategy,
    string EntryCondition,
    string ExitCondition,
    int PeriodDays,
    int Lots,
    decimal? TargetPoints = null,
    decimal? StopLossPoints = null);

public record BacktestTradeLogEntry(
    string EntryDate,
    string ExitDate,
    string Contract,
    decimal EntryPrice,
    decimal ExitPrice,
    decimal PnL,
    string ExitReason,
    decimal? StopLossPrice = null,
    decimal? Target1Price = null,
    decimal? Target2Price = null);

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
    string DataSource,
    int CandleCount,
    int TradingDays,
    decimal? TargetPoints,
    decimal? StopLossPoints,
    IReadOnlyList<BacktestTradeLogEntry> TradeLog,
    string CreatedAt);

public record BacktestHistoryResponse(
    IReadOnlyList<BacktestResultResponse> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    int RetentionDays);
