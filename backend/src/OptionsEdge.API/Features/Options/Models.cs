namespace OptionsEdge.API.Features.Options;

public record OptionsChainResponse(
    string Symbol,
    string Expiry,
    decimal Spot,
    decimal Pcr,
    decimal MaxPain,
    IReadOnlyList<ChainRowResponse> Rows);

public record ChainRowResponse(
    int Strike,
    bool IsAtm,
    OptionLegResponse Ce,
    OptionLegResponse Pe);

public record OptionLegResponse(
    decimal Ltp,
    long Oi,
    long OiChange,
    long Volume,
    double Iv,
    double Delta,
    double Gamma,
    double Theta,
    double Vega);

public record MaxPainResponse(decimal MaxPain, decimal Spot, string Expiry);

public record PayoffLegRequest(
    string Symbol,
    int Strike,
    string OptionType,
    string Action,
    int Lots,
    decimal Premium);

public record PayoffRunRequest(IReadOnlyList<PayoffLegRequest> Legs);

public record PayoffPoint(decimal Price, decimal Pnl);

public record PayoffResponse(
    IReadOnlyList<PayoffPoint> PayoffCurve,
    decimal? MaxProfit,
    bool MaxProfitUnlimited,
    decimal? MaxLoss,
    bool MaxLossUnlimited,
    IReadOnlyList<decimal> Breakevens);
