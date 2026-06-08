namespace OptionsEdge.API.Domain.Entities;

// Per-user Groww broker credentials (the permanent "TOTP Token" / "TOTP Secret" pair from
// the Groww API dashboard), stored AES-encrypted. These auto-generate a daily access token —
// see GrowwUserApiClient.
public class GrowwCredential
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ApiKeyEncrypted { get; set; } = string.Empty;
    public string ApiSecretEncrypted { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
