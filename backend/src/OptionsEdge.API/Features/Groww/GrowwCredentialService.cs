using Microsoft.EntityFrameworkCore;
using OptionsEdge.API.Domain.Entities;
using OptionsEdge.API.Infrastructure.Data;
using OptionsEdge.API.Infrastructure.Security;

namespace OptionsEdge.API.Features.Groww;

// Stores and retrieves each user's Groww API credentials, AES-encrypted at rest.
public class GrowwCredentialService(AppDbContext db, IEncryptionService encryption)
{
    public async Task SaveCredentialsAsync(Guid userId, string apiKey, string apiSecret, CancellationToken ct = default)
    {
        var existing = await db.GrowwCredentials.FirstOrDefaultAsync(g => g.UserId == userId, ct);

        if (existing is null)
        {
            db.GrowwCredentials.Add(new GrowwCredential
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ApiKeyEncrypted = encryption.Encrypt(apiKey),
                ApiSecretEncrypted = encryption.Encrypt(apiSecret),
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            existing.ApiKeyEncrypted = encryption.Encrypt(apiKey);
            existing.ApiSecretEncrypted = encryption.Encrypt(apiSecret);
            existing.IsActive = true;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<(string ApiKey, string ApiSecret)?> GetCredentialsAsync(Guid userId, CancellationToken ct = default)
    {
        var cred = await db.GrowwCredentials.FirstOrDefaultAsync(g => g.UserId == userId && g.IsActive, ct);
        if (cred is null) return null;

        return (encryption.Decrypt(cred.ApiKeyEncrypted), encryption.Decrypt(cred.ApiSecretEncrypted));
    }

    public Task<bool> HasCredentialsAsync(Guid userId, CancellationToken ct = default) =>
        db.GrowwCredentials.AnyAsync(g => g.UserId == userId && g.IsActive, ct);

    public async Task RemoveCredentialsAsync(Guid userId, CancellationToken ct = default)
    {
        var cred = await db.GrowwCredentials.FirstOrDefaultAsync(g => g.UserId == userId, ct);
        if (cred is null) return;

        cred.IsActive = false;
        cred.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
