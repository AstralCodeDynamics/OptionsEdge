namespace OptionsEdge.API.Infrastructure.Groww;

public record GrowwOptionLeg(
    string TradingSymbol,
    decimal Ltp,
    long OpenInterest,
    decimal OiChange,
    long Volume,
    decimal ImpliedVolatility,
    decimal Delta,
    decimal Gamma,
    decimal Theta,
    decimal Vega);

public record GrowwOptionChainRow(int Strike, GrowwOptionLeg? Call, GrowwOptionLeg? Put);

public record GrowwOrderRequest(
    string TradingSymbol,
    int Quantity,
    decimal Price,
    string Validity,
    string Exchange,
    string Segment,
    string Product,
    string OrderType,
    string TransactionType,
    string OrderReferenceId);

public record GrowwOrderResult(string OrderId, string Status, string? RejectReason);

public record GrowwPosition(string TradingSymbol, int Quantity, decimal AvgPrice, decimal Ltp, decimal Pnl);
