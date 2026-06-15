namespace OptionsEdge.API.Domain.Entities;

// Per-user Anthropic API key, stored AES-encrypted. Used so each user's AI
// signal/chat usage is billed against their own Anthropic account.
public class UserAICredential
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ApiKeyEncrypted { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
