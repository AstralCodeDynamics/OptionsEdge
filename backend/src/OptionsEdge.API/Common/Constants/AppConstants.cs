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

    public static class IndicatorThresholds
    {
        public const int RsiPeriod = 14;
        public const int RsiOverbought = 70;
        public const int RsiOversold = 30;

        public const int MacdFastPeriod = 12;
        public const int MacdSlowPeriod = 26;
        public const int MacdSignalPeriod = 9;

        public const int BollingerPeriod = 20;
        public const double BollingerStdDev = 2.0;
        public const double BollingerSqueezeBandwidth = 0.04;

        public const int AdxPeriod = 14;
        public const int AdxWeakThreshold = 20;
        public const int AdxStrongThreshold = 40;

        public const int Ema9Period = 9;
        public const int Ema20Period = 20;
        public const int Ema50Period = 50;
        public const int Ema200Period = 200;

        public const int SupertrendPeriod = 10;
        public const double SupertrendMultiplier = 3.0;
    }
}
