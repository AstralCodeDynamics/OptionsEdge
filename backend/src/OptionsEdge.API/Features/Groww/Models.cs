namespace OptionsEdge.API.Features.Groww;

public record GrowwStatusResponse(
    bool Enabled,
    bool HasCredentials,
    bool Connected,
    DateTimeOffset? ExpiresAt,
    bool OrderPlacementEnabled,
    string? Error);

public record SaveGrowwCredentialsRequest(string ApiKey, string ApiSecret);
public record GrowwCredentialsResponse(bool Success, string Message);

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
