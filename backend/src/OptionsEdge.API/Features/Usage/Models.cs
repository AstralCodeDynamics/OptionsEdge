namespace OptionsEdge.API.Features.Usage;

public record UsageStatsResponse(
    int CallsToday,
    int CallsLimit,
    decimal CostToday,
    decimal WalletBalance);
