using Microsoft.EntityFrameworkCore;
using OptionsEdge.API.Domain.Entities;
using OptionsEdge.API.Infrastructure.Data;
using OptionsEdge.API.Infrastructure.Security;

namespace OptionsEdge.API.Features.AI;

// Stores and retrieves each user's Anthropic API key, AES-encrypted at rest.
public class UserAICredentialService(AppDbContext db, IEncryptionService enc, ILogger<UserAICredentialService> logger)
{
    public async Task SaveAsync(Guid userId, string apiKey, CancellationToken ct = default)
    {
        if (!apiKey.StartsWith("sk-ant-"))
            throw new ArgumentException("Invalid Anthropic API key. Key must start with sk-ant-");

        var existing = await db.UserAICredentials.FirstOrDefaultAsync(x => x.UserId == userId, ct);

        if (existing is null)
        {
            db.UserAICredentials.Add(new UserAICredential
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ApiKeyEncrypted = enc.Encrypt(apiKey),
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            existing.ApiKeyEncrypted = enc.Encrypt(apiKey);
            existing.IsActive = true;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("AI credentials saved for user {UserId}", userId);
    }

    public async Task<string?> GetApiKeyAsync(Guid userId, CancellationToken ct = default)
    {
        var cred = await db.UserAICredentials.FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive, ct);
        return cred is null ? null : enc.Decrypt(cred.ApiKeyEncrypted);
    }

    public Task<bool> HasKeyAsync(Guid userId, CancellationToken ct = default) =>
        db.UserAICredentials.AnyAsync(x => x.UserId == userId && x.IsActive, ct);

    public async Task RemoveAsync(Guid userId, CancellationToken ct = default)
    {
        var cred = await db.UserAICredentials.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (cred is null) return;

        cred.IsActive = false;
        cred.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
