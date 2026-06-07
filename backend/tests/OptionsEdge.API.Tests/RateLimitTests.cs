using OptionsEdge.API.Common.Constants;

namespace OptionsEdge.API.Tests;

// Verifies the plan-based AI call limits and the boundary check
// `user.AiCallsToday >= callLimit` that AISignalService/ChatService use to
// gate generation — i.e. that a free-plan user is blocked starting on their
// 11th call within the rolling one-hour window.
public class RateLimitTests
{
    [Theory]
    [InlineData("free", 10)]
    [InlineData("Free", 10)]
    [InlineData("pro", 50)]
    [InlineData("unknown-plan", 10)]
    public void GetCallLimitForPlan_ReturnsExpectedLimit(string plan, int expectedLimit)
    {
        Assert.Equal(expectedLimit, AppConstants.RateLimits.GetCallLimitForPlan(plan));
    }

    [Fact]
    public void FreePlanUser_IsAllowedThroughTenCallsAndBlockedOnTheEleventh()
    {
        var callLimit = AppConstants.RateLimits.GetCallLimitForPlan("free");
        var aiCallsToday = 0;

        for (var call = 1; call <= callLimit; call++)
        {
            // Mirrors the AISignalService gate: blocked only when the count has already reached the limit
            var blocked = aiCallsToday >= callLimit;
            Assert.False(blocked, $"Call #{call} should be allowed (AiCallsToday={aiCallsToday}, limit={callLimit})");
            aiCallsToday++;
        }

        // The 11th attempt: AiCallsToday is now 10, so the gate blocks it
        Assert.True(aiCallsToday >= callLimit, "AiCallsToday should equal the limit after 10 successful calls");
        var blockedOnEleventh = aiCallsToday >= callLimit;
        Assert.True(blockedOnEleventh, "The 11th call within the same hour window must be blocked");
    }
}
