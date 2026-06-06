using OptionsEdge.API.Domain.Entities;

namespace OptionsEdge.API.Infrastructure.Data;

public static class DevDataSeeder
{
    public static readonly Guid DevUserId = new("00000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!db.Users.Any(u => u.Id == DevUserId))
        {
            db.Users.Add(new User
            {
                Id                = DevUserId,
                Email             = "dev@optionsedge.local",
                PasswordHash      = "$2a$11$devplaceholdernotused",
                DisplayName       = "Dev User",
                SubscriptionPlan  = "pro",
                WalletBalance     = 1000m,
                AiCallsToday      = 0,
                AiCallsResetAt    = DateTimeOffset.UtcNow,
                IsActive          = true,
                CreatedAt         = DateTimeOffset.UtcNow,
                UpdatedAt         = DateTimeOffset.UtcNow,
            });

            await db.SaveChangesAsync();
            logger.LogInformation("Dev user seeded (id={DevUserId})", DevUserId);
        }
    }
}
