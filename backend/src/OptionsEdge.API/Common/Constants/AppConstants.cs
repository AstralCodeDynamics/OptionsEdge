namespace OptionsEdge.API.Common.Constants;

public static class AppConstants
{
    public static class MarketHours
    {
        public static readonly TimeOnly Open = new(9, 15);
        public static readonly TimeOnly Close = new(15, 30);
        public const string TimeZoneId = "Asia/Kolkata";
    }

    public static class RateLimits
    {
        public const int AiCallsPerUserPerHour = 10;

        private static readonly Dictionary<string, int> PlanCallLimits = new(StringComparer.OrdinalIgnoreCase)
        {
            ["free"] = AiCallsPerUserPerHour,
            ["pro"]  = 50,
        };

        public static int GetCallLimitForPlan(string plan) =>
            PlanCallLimits.TryGetValue(plan, out var limit) ? limit : AiCallsPerUserPerHour;
    }

    public static class Models
    {
        public const string Haiku = "claude-haiku-4-5-20251001";
        public const string Sonnet = "claude-sonnet-4-6";
    }

    public static class AiTokenLimits
    {
        public const int HaikuMaxTokens = 800;
        public const int SonnetMaxTokens = 1000;
    }

    public static class Cache
    {
        public const int SignalTtlMinutes = 5;
    }

    public static class LotSizes
    {
        public const int Nifty = 75;
        public const int BankNifty = 35;
    }
}
