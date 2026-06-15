namespace OptionsEdge.API.Features.Signals;

public record GenerateSignalRequest(string Symbol);

public record SignalResponse(
    Guid Id,
    string Symbol,
    string SignalType,
    string OptionType,
    int Strike,
    string Expiry,
    decimal EntryLow,
    decimal EntryHigh,
    decimal StopLoss,
    decimal Target1,
    decimal? Target2,
    int Confidence,
    decimal RiskReward,
    string[] Rationale,
    string ModelUsed,
    int InputTokens,
    int OutputTokens,
    decimal CostUsd,
    string ValidUntil,
    string CreatedAt,
    bool FromCache = false);

public record RiskCheckResponse(
    string Severity,
    string AlertType,
    string Message,
    string RecommendedAction);

public record SignalPreferenceResponse(
    bool NiftyAutoSignalEnabled,
    string NiftyAutoSignalTimes,
    bool BankNiftyAutoSignalEnabled,
    string BankNiftyAutoSignalTimes);

public record SignalPreferenceRequest(
    bool NiftyAutoSignalEnabled,
    string NiftyAutoSignalTimes,
    bool BankNiftyAutoSignalEnabled,
    string BankNiftyAutoSignalTimes);

// Raw AI output before DB save (parsed from Claude JSON)
internal record SignalAiOutput(
    string SignalType,
    string Symbol,
    int Strike,
    string OptionType,
    string Expiry,
    decimal EntryLow,
    decimal EntryHigh,
    decimal StopLoss,
    decimal Target1,
    decimal? Target2,
    int Confidence,
    decimal RiskReward,
    string[] Rationale,
    string ValidUntil);
