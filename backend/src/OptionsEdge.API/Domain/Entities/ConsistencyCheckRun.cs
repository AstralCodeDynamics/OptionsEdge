namespace OptionsEdge.API.Domain.Entities;

public class ConsistencyCheckRun
{
    public Guid Id { get; set; }
    public DateTimeOffset RunAtUtc { get; set; }
    public int TotalChecks { get; set; }
    public int NeedsReviewCount { get; set; }
    public int CheckFailedCount { get; set; }
    public bool EmailSent { get; set; }

    public List<ConsistencyFinding> Findings { get; set; } = [];
}
