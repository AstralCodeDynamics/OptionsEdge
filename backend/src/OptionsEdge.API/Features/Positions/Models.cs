namespace OptionsEdge.API.Features.Positions;

public record CreatePositionRequest(
    string Symbol,
    int Strike,
    string OptionType,
    string Expiry,
    decimal EntryPrice,
    int Quantity,
    decimal StopLoss,
    decimal Target1,
    decimal? Target2,
    Guid? SignalId);

public record UpdatePositionRequest(
    decimal? StopLoss,
    decimal? Target1,
    decimal? Target2,
    string? Status,
    decimal? ExitPrice,
    string? ExitReason);

public record PositionResponse(
    Guid Id,
    string Symbol,
    int Strike,
    string OptionType,
    string Expiry,
    decimal EntryPrice,
    int Quantity,
    decimal StopLoss,
    decimal Target1,
    decimal? Target2,
    Guid? SignalId,
    string Status,
    decimal? ExitPrice,
    string? ExitReason,
    string? ClosedAt,
    string CreatedAt,
    decimal? CurrentLtp,
    decimal? PnL,
    decimal? PnLPct,
    decimal? DistanceToSLPct,
    decimal? DistanceToTarget1Pct);

public record PnLResponse(
    Guid PositionId,
    decimal EntryPrice,
    decimal CurrentLtp,
    decimal PnL,
    decimal PnLPct,
    decimal DistanceToSLRs,
    decimal DistanceToSLPct,
    decimal DistanceToTarget1Rs,
    decimal DistanceToTarget1Pct,
    decimal? DistanceToTarget2Rs,
    decimal? DistanceToTarget2Pct,
    decimal ThetaDecayPct);

public record AlertResponse(
    Guid Id,
    Guid PositionId,
    string Severity,
    string AlertType,
    string Message,
    bool IsRead,
    string CreatedAt);

public record AlertTrigger(string Severity, string AlertType, string Message);
