namespace OptionsEdge.API.Features.Market;

public record MarketSnapshotResponse(
    string Symbol,
    decimal Ltp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal PreviousClose,
    decimal Change,
    decimal ChangePct,
    decimal Vix,
    decimal Pcr,
    decimal FiiFlow,
    decimal DiiFlow,
    DateTimeOffset Timestamp);

public record CandleResponse(
    long Time,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume);

public record MarketStatusResponse(
    bool IsOpen,
    string Message,
    string NextEvent);

// SignalR broadcast payloads
public record PriceUpdateEvent(
    string Symbol,
    decimal Ltp,
    decimal Change,
    decimal ChangePct,
    DateTimeOffset Timestamp);

public record MarketStatusEvent(
    bool IsOpen,
    string Message,
    string NextEvent);
