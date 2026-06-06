namespace OptionsEdge.API.Features.Indicators;

public record IndicatorsResponse(
    string Symbol,
    RsiResponse Rsi,
    MacdResponse Macd,
    BollingerBandsResponse BollingerBands,
    AdxResponse Adx,
    EmaResponse Ema,
    SupertrendResponse Supertrend,
    PivotLevelsResponse Pivots,
    DateTimeOffset Timestamp);

public record RsiResponse(double Value, string Signal);

public record MacdResponse(double Value, double Signal, double Histogram, bool IsBullishCross);

public record BollingerBandsResponse(double Upper, double Middle, double Lower, bool IsSqueeze);

public record AdxResponse(double Value, string Strength);

public record EmaResponse(
    double Ema9, double Ema20, double Ema50, double Ema200,
    bool PriceAboveEma20, bool PriceAboveEma50);

public record SupertrendResponse(double Value, bool IsBullish);

public record PivotLevelsResponse(
    double R3, double R2, double R1, double Pivot,
    double S1, double S2, double S3);

// SignalR broadcast payload
public record IndicatorUpdateEvent(
    string Symbol,
    RsiResponse Rsi,
    MacdResponse Macd,
    string SupertrendSignal,
    DateTimeOffset Timestamp);
