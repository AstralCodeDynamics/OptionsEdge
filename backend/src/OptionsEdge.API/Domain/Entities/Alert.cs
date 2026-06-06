namespace OptionsEdge.API.Domain.Entities;

public class Alert
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PositionId { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Position Position { get; set; } = null!;
}
