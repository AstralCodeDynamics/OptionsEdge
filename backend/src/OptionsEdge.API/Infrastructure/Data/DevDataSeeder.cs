using Microsoft.AspNetCore.Identity;
using OptionsEdge.API.Domain.Entities;

namespace OptionsEdge.API.Infrastructure.Data;

public static class DevDataSeeder
{
    public static readonly Guid DevUserId = new("00000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (await userManager.FindByIdAsync(DevUserId.ToString()) is not null)
            return;

        var user = new ApplicationUser
        {
            Id                = DevUserId,
            UserName          = "dev@optionsedge.local",
            Email             = "dev@optionsedge.local",
            EmailConfirmed    = true,
            TwoFactorEnabled  = false,
            DisplayName       = "Dev User",
            SubscriptionPlan  = "pro",
            WalletBalance     = 1000m,
            AiCallsToday      = 0,
            AiCallsResetAt    = DateTimeOffset.UtcNow,
            IsActive          = true,
            CreatedAt         = DateTimeOffset.UtcNow,
            UpdatedAt         = DateTimeOffset.UtcNow,
        };

        var result = await userManager.CreateAsync(user, "DevPass123!");
        if (result.Succeeded)
            logger.LogInformation("Dev user seeded (id={DevUserId})", DevUserId);
        else
            logger.LogWarning("Failed to seed dev user: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
