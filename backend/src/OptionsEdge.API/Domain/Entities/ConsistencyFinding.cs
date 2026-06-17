namespace OptionsEdge.API.Domain.Entities;

public class ConsistencyFinding
{
    public Guid Id { get; set; }
    public Guid ConsistencyCheckRunId { get; set; }
    public string CheckName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // OK / NEEDS_REVIEW / CHECK_FAILED
    public string Detail { get; set; } = string.Empty;
    public string? SuggestedAction { get; set; }

    public ConsistencyCheckRun Run { get; set; } = null!;
}
