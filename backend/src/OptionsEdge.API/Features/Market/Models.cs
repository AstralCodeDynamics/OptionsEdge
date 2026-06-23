namespace OptionsEdge.API.Features.Market;

// Wrapper returned by every Groww-backed data endpoint. Frontend checks IsGrowwConnected
// first; when false, Data is null and the user must connect their Groww account.
public record GrowwGatedResponse<T>(bool IsGrowwConnected, bool IsDataFresh, T? Data);

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
    // FiiFlow/DiiFlow are 0 when DataSource is "groww_live" — Groww's API doesn't expose
    // FII/DII cash-market flow data. Non-zero only for mock data.
    decimal FiiFlow,
    decimal DiiFlow,
    DateTimeOffset Timestamp,
    string DataSource);

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
