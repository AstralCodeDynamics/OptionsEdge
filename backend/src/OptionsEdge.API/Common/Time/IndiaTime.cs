namespace OptionsEdge.API.Common.Time;

public static class IndiaTime
{
    public const string TimeZoneId = "Asia/Kolkata";

    private static readonly Lazy<TimeZoneInfo> IstZone = new(() =>
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone(TimeZoneId, TimeSpan.FromHours(5.5), "India Standard Time", "India Standard Time");
        }
    });

    public static TimeZoneInfo Zone => IstZone.Value;

    public static DateTimeOffset ToIst(DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, Zone);
}
