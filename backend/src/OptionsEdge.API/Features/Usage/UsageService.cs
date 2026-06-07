using Microsoft.EntityFrameworkCore;
using OptionsEdge.API.Common.Constants;
using OptionsEdge.API.Infrastructure.Data;

namespace OptionsEdge.API.Features.Usage;

public class UsageService(AppDbContext db)
{
    public async Task<UsageStatsResponse?> GetStatsAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null) return null;

        var todayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var costToday = await db.AIUsageLogs
            .Where(l => l.UserId == userId && l.CreatedAt >= todayStart)
            .SumAsync(l => (decimal?)l.CostUsd, ct) ?? 0m;

        return new UsageStatsResponse(
            user.AiCallsToday,
            AppConstants.RateLimits.GetCallLimitForPlan(user.SubscriptionPlan),
            costToday,
            user.WalletBalance);
    }
}
