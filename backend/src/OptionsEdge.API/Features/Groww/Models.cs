namespace OptionsEdge.API.Features.Groww;

public record ConnectGrowwRequest(string Totp);
public record ConnectGrowwResponse(bool Connected, DateTimeOffset ExpiresAt, int ImportedPositions);
public record GrowwStatusResponse(bool Enabled, bool Connected, DateTimeOffset? ExpiresAt, bool OrderPlacementEnabled);

public record PlaceOrderRequest(
    Guid? PositionId,
    string Symbol,
    int Strike,
    string OptionType,
    string Expiry,
    int Quantity,
    decimal Price,
    string OrderType,
    string TransactionType);

public record PlaceOrderResponse(string OrderId, string Status, string TradingSymbol, int Quantity);
