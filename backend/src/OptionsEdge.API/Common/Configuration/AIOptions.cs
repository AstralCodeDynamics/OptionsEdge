using OptionsEdge.API.Common.Constants;

namespace OptionsEdge.API.Common.Configuration;

public class AIOptions
{
    public AIModelOptions Models { get; init; } = new();
    public Dictionary<string, AIFeatureOptions> Features { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public AIBudgetOptions Budget { get; init; } = new();
    public AIDisclaimerOptions Disclaimers { get; init; } = new();
}

public class AIModelOptions
{
    // Fallback defaults reference AppConstants so model strings are never duplicated.
    public string Quick { get; init; } = AppConstants.Models.Haiku;
    public string Deep { get; init; } = AppConstants.Models.Sonnet;
    public string Default { get; init; } = AppConstants.Models.Haiku;
}

public class AIFeatureOptions
{
    /// <summary>Tier key: "Quick", "Deep", or "Default". Resolved by ResolveModel helper.</summary>
    public string ModelTier { get; init; } = "Default";
}

public class AIBudgetOptions
{
    public int DefaultMaxOutputTokens { get; init; } = 1000;
    public Dictionary<string, AIBudgetFeatureOptions> Features { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, AIRateLimitOptions> RateLimits { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public class AIBudgetFeatureOptions
{
    public int MaxOutputTokens { get; init; } = 1000;
}

public class AIRateLimitOptions
{
    public int CallsPerHour { get; init; } = 10;
}

public class AIDisclaimerOptions
{
    public string SignalDisclaimer { get; init; } = string.Empty;
    public string ChatDisclaimer { get; init; } = string.Empty;
    public List<string> InjectOnFeatures { get; init; } = [];
}
