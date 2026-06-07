namespace OptionsEdge.API.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ModelUsed { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public decimal? CostUsd { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
