namespace OptionsEdge.API.Infrastructure.Background;

public static class MarketHoursHelper
{
    private static readonly TimeZoneInfo IstZone = GetIstZone();
    private static readonly TimeOnly MarketOpen = new(9, 15);
    private static readonly TimeOnly MarketClose = new(15, 30);

    public static bool IsMarketOpen() => IsMarketOpen(DateTime.UtcNow);

    public static bool IsMarketOpen(DateTime utcNow)
    {
        var ist = TimeZoneInfo.ConvertTimeFromUtc(utcNow, IstZone);
        if (ist.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) return false;
        var time = TimeOnly.FromDateTime(ist);
        return time >= MarketOpen && time <= MarketClose;
    }

    public static DateTimeOffset NextMarketOpen()
    {
        var ist = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstZone);
        var candidate = ist.Date;

        // If today's open is still in the future, use today
        if (ist.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)
            && TimeOnly.FromDateTime(ist) < MarketOpen)
        {
            return ToUtc(candidate.Add(MarketOpen.ToTimeSpan()));
        }

        // Otherwise find the next weekday
        candidate = candidate.AddDays(1);
        while (candidate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            candidate = candidate.AddDays(1);

        return ToUtc(candidate.Add(MarketOpen.ToTimeSpan()));
    }

    public static DateTimeOffset TodayMarketClose()
    {
        var ist = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstZone);
        return ToUtc(ist.Date.Add(MarketClose.ToTimeSpan()));
    }

    public static string GetNextEventDescription()
    {
        if (IsMarketOpen())
        {
            var close = TodayMarketClose();
            var diff = close - DateTimeOffset.UtcNow;
            return $"Closes in {FormatDuration(diff)}";
        }
        else
        {
            var open = NextMarketOpen();
            var diff = open - DateTimeOffset.UtcNow;
            return $"Opens in {FormatDuration(diff)}";
        }
    }

    private static DateTimeOffset ToUtc(DateTime istDateTime)
    {
        var offset = IstZone.GetUtcOffset(istDateTime);
        return new DateTimeOffset(istDateTime, offset).ToUniversalTime();
    }

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalDays >= 1) return $"{(int)ts.TotalDays}d {ts.Hours}h";
        if (ts.TotalHours >= 1) return $"{ts.Hours}h {ts.Minutes}m";
        return $"{ts.Minutes}m";
    }

    private static TimeZoneInfo GetIstZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
    }
}
