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
