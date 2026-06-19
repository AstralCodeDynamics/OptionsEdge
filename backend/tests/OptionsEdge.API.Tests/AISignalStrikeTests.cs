using OptionsEdge.API.Features.Options;
using OptionsEdge.API.Features.Signals;

namespace OptionsEdge.API.Tests;

// Validates AISignalService's two new strike-quality guardrails introduced after
// Anju's 18/06/26 functional report: AI picked 23600 CE (10 steps from ATM ~24100)
// and gave entry 110-130 which only fits a near-ATM strike — a nonsensical combination.
public class AISignalStrikeTests
{
    // ---------------------------------------------------------------------------
    // IsStrikeWithinBounds — sanity-check guardrail (Fix A)
    // ---------------------------------------------------------------------------

    [Fact]
    public void StrikeSanityCheck_AllowsAtmExactly()
    {
        Assert.True(AISignalService.IsStrikeWithinBounds(24100, 24100m, "NIFTY"));
    }

    [Fact]
    public void StrikeSanityCheck_AllowsStrikeExactly3StepsFromAtm()
    {
        // NIFTY step = 50. ATM = 24100. 3 steps = 150. 24250 is exactly on the boundary.
        Assert.True(AISignalService.IsStrikeWithinBounds(24250, 24100m, "NIFTY"));
        Assert.True(AISignalService.IsStrikeWithinBounds(23950, 24100m, "NIFTY"));
    }

    [Fact]
    public void StrikeSanityCheck_RejectsStrikeMoreThan3StepsFromAtm()
    {
        // Mirrors Anju's 18/06/26 scenario: spot ~24100, AI picked 23600 (10 steps away).
        Assert.False(AISignalService.IsStrikeWithinBounds(23600, 24100m, "NIFTY"),
            "Strike 10 steps from ATM must be rejected.");
        // 4 steps: just over boundary
        Assert.False(AISignalService.IsStrikeWithinBounds(24300, 24100m, "NIFTY"));
    }

    [Fact]
    public void StrikeSanityCheck_BankNiftyUsesCorrectStep()
    {
        // BANKNIFTY step = 100. ATM = 51500. 3 steps = ±300.
        Assert.True(AISignalService.IsStrikeWithinBounds(51800, 51500m, "BANKNIFTY"));
        Assert.True(AISignalService.IsStrikeWithinBounds(51200, 51500m, "BANKNIFTY"));
        // 4 steps = 400 off — reject
        Assert.False(AISignalService.IsStrikeWithinBounds(51100, 51500m, "BANKNIFTY"));
        Assert.False(AISignalService.IsStrikeWithinBounds(51900, 51500m, "BANKNIFTY"));
    }

    // ---------------------------------------------------------------------------
    // BuildNearbyStrikesTable — prompt chain table (Fix B)
    // ---------------------------------------------------------------------------

    private static OptionLegResponse Leg(decimal ltp, long oi) =>
        new(Ltp: ltp, Oi: oi, OiChange: 0, Volume: 0, Iv: 0, Delta: 0, Gamma: 0, Theta: 0, Vega: 0);

    private static IReadOnlyList<ChainRowResponse> BuildChain(int atm, int step, int windowSteps = 10)
    {
        var rows = new List<ChainRowResponse>();
        for (int i = -windowSteps; i <= windowSteps; i++)
        {
            int strike = atm + i * step;
            rows.Add(new ChainRowResponse(
                Strike: strike,
                IsAtm:  i == 0,
                Ce: Leg(ltp: Math.Max(1m, 100m - Math.Abs(i) * 8m), oi: 400_000 + i * 10_000L),
                Pe: Leg(ltp: Math.Max(1m, 60m  + Math.Abs(i) * 8m), oi: 350_000 - i * 10_000L)));
        }
        return rows;
    }

    [Fact]
    public void NearbyStrikesTable_IncludesAtmMarkAndLimitedToWindow()
    {
        var rows  = BuildChain(atm: 24100, step: 50);
        string table = AISignalService.BuildNearbyStrikesTable(rows, atm: 24100, maxSteps: 5);

        Assert.False(string.IsNullOrWhiteSpace(table));
        Assert.Contains("24100", table);
        Assert.Contains("<- ATM", table);
        // ATM -5 steps = 23850 — included
        Assert.Contains("23850", table);
        // ATM -6 steps = 23800 — excluded
        Assert.DoesNotContain("23800", table);
    }

    [Fact]
    public void NearbyStrikesTable_ReturnsEmptyStringForEmptyChain()
    {
        string table = AISignalService.BuildNearbyStrikesTable(Array.Empty<ChainRowResponse>(), atm: 24100);
        Assert.Equal(string.Empty, table);
    }

    [Fact]
    public void NearbyStrikesTable_FormatsKAndM()
    {
        var rows = new List<ChainRowResponse>
        {
            new(24100, IsAtm: true,
                Ce: Leg(ltp: 95.5m, oi: 453_000L),
                Pe: Leg(ltp: 62.0m, oi: 1_200_000L)),
        };
        string table = AISignalService.BuildNearbyStrikesTable(rows, atm: 24100);
        Assert.Contains("453K", table);
        Assert.Contains("1.2M", table);
    }

    [Fact]
    public void NearbyStrikesTable_FallsBackGracefullyWhenChainUnavailable()
    {
        // Caller passes empty rows when chain fetch fails — empty string triggers ATM-only prompt fallback.
        string table = AISignalService.BuildNearbyStrikesTable(Array.Empty<ChainRowResponse>(), atm: 24100);
        Assert.Equal(string.Empty, table); // empty rows → empty string → prompt falls back to ATM-only context
    }
}
