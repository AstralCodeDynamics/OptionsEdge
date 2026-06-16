namespace OptionsEdge.API.Common.Options;

public class LogFileOptions
{
    public const string SectionName = "LogFiles";

    public string Directory { get; set; } = "logs";
    public string FileNamePrefix { get; set; } = "optionsedge-";
    public int RetentionDays { get; set; } = 7;
    public string CleanupTimeLocal { get; set; } = "00:00:00";

    public TimeSpan GetCleanupTimeLocal() =>
        TimeSpan.TryParse(CleanupTimeLocal, out var parsed) ? parsed : TimeSpan.Zero;
}
