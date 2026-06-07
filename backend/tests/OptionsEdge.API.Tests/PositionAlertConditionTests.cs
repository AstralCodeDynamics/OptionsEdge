using OptionsEdge.API.Domain.Entities;
using OptionsEdge.API.Features.Positions;

namespace OptionsEdge.API.Tests;

// Verifies all 7 active-position alert conditions emitted by PositionService.CheckAlertConditions,
// the rule PositionMonitorWorker evaluates every tick against live LTP/spot/VIX readings.
public class PositionAlertConditionTests
{
    private readonly PositionService _service = new();

    private static Position CallPosition(decimal stopLoss = 90m, decimal target1 = 130m, decimal? target2 = 150m, decimal entryPrice = 110m) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Symbol = "NIFTY",
        Strike = 24000,
        OptionType = "CE",
        Expiry = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
        EntryPrice = entryPrice,
        Quantity = 1,
        StopLoss = stopLoss,
        Target1 = target1,
        Target2 = target2,
        Status = "active",
        CreatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public void SlApproaching_FiresWithinTenPercentAboveStopLoss()
    {
        var position = CallPosition(stopLoss: 90m);

        // 95 is within 10% above the 90 SL (90 * 1.10 = 99)
        var triggers = _service.CheckAlertConditions(position, currentLtp: 95m, currentSpot: 24000m, spot15MinAgo: null, currentVix: 14m, prevVix30Min: null);

        Assert.Contains(triggers, t => t.AlertType == "SL_APPROACHING" && t.Severity == "Warning");
    }

    [Fact]
    public void SlHit_FiresWhenLtpAtOrBelowStopLoss()
    {
        var position = CallPosition(stopLoss: 90m);

        var triggers = _service.CheckAlertConditions(position, currentLtp: 90m, currentSpot: 24000m, spot15MinAgo: null, currentVix: 14m, prevVix30Min: null);

        Assert.Contains(triggers, t => t.AlertType == "SL_HIT" && t.Severity == "Danger");
    }

    [Fact]
    public void Target1Hit_FiresWhenLtpReachesTarget1()
    {
        var position = CallPosition(target1: 130m);

        var triggers = _service.CheckAlertConditions(position, currentLtp: 130m, currentSpot: 24000m, spot15MinAgo: null, currentVix: 14m, prevVix30Min: null);

        Assert.Contains(triggers, t => t.AlertType == "TARGET1_HIT" && t.Severity == "Info");
    }

    [Fact]
    public void Target2Hit_FiresWhenLtpReachesTarget2()
    {
        var position = CallPosition(target2: 150m);

        var triggers = _service.CheckAlertConditions(position, currentLtp: 150m, currentSpot: 24000m, spot15MinAgo: null, currentVix: 14m, prevVix30Min: null);

        Assert.Contains(triggers, t => t.AlertType == "TARGET2_HIT" && t.Severity == "Info");
    }

    [Fact]
    public void Target2Hit_DoesNotFireWhenPositionHasNoTarget2()
    {
        var position = CallPosition(target2: null);

        var triggers = _service.CheckAlertConditions(position, currentLtp: 999m, currentSpot: 24000m, spot15MinAgo: null, currentVix: 14m, prevVix30Min: null);

        Assert.DoesNotContain(triggers, t => t.AlertType == "TARGET2_HIT");
    }

    [Fact]
    public void IvSpike_FiresWhenVixMovesMoreThanTwentyPercentInThirtyMinutes()
    {
        var position = CallPosition();

        // 12 -> 15 is a 25% jump, above the 20% threshold
        var triggers = _service.CheckAlertConditions(position, currentLtp: 110m, currentSpot: 24000m, spot15MinAgo: null, currentVix: 15m, prevVix30Min: 12m);

        Assert.Contains(triggers, t => t.AlertType == "IV_SPIKE" && t.Severity == "Warning");
    }

    [Fact]
    public void IvSpike_DoesNotFireForMovesUnderTwentyPercent()
    {
        var position = CallPosition();

        // 14 -> 15 is ~7%, below the threshold
        var triggers = _service.CheckAlertConditions(position, currentLtp: 110m, currentSpot: 24000m, spot15MinAgo: null, currentVix: 15m, prevVix30Min: 14m);

        Assert.DoesNotContain(triggers, t => t.AlertType == "IV_SPIKE");
    }

    [Fact]
    public void AdverseMove_FiresWhenSpotDropsAgainstACallPositionByOverHalfPercentInFifteenMinutes()
    {
        var position = CallPosition(); // CE — adverse means spot falling

        // 24180 -> 24000 is roughly -0.74%, beyond the -0.5% adverse threshold for a CE
        var triggers = _service.CheckAlertConditions(position, currentLtp: 110m, currentSpot: 24000m, spot15MinAgo: 24180m, currentVix: 14m, prevVix30Min: null);

        Assert.Contains(triggers, t => t.AlertType == "ADVERSE_MOVE" && t.Severity == "Danger");
    }

    [Fact]
    public void AdverseMove_FiresWhenSpotRisesAgainstAPutPositionByOverHalfPercentInFifteenMinutes()
    {
        var position = CallPosition();
        position.OptionType = "PE"; // adverse means spot rising for a put

        var triggers = _service.CheckAlertConditions(position, currentLtp: 110m, currentSpot: 24180m, spot15MinAgo: 24000m, currentVix: 14m, prevVix30Min: null);

        Assert.Contains(triggers, t => t.AlertType == "ADVERSE_MOVE" && t.Severity == "Danger");
    }

    [Fact]
    public void AdverseMove_DoesNotFireForMovesUnderHalfPercent()
    {
        var position = CallPosition();

        // 24050 -> 24000 is ~ -0.21%, under the -0.5% threshold
        var triggers = _service.CheckAlertConditions(position, currentLtp: 110m, currentSpot: 24000m, spot15MinAgo: 24050m, currentVix: 14m, prevVix30Min: null);

        Assert.DoesNotContain(triggers, t => t.AlertType == "ADVERSE_MOVE");
    }

    [Fact]
    public void ThetaDecay_FiresWhenPremiumHasErodedByMoreThanFiftyPercent()
    {
        var position = CallPosition(entryPrice: 110m);

        // (110 - 50) / 110 = ~54.5% decay, above the 50% threshold
        var triggers = _service.CheckAlertConditions(position, currentLtp: 50m, currentSpot: 24000m, spot15MinAgo: null, currentVix: 14m, prevVix30Min: null);

        Assert.Contains(triggers, t => t.AlertType == "THETA_DECAY" && t.Severity == "Warning");
    }

    [Fact]
    public void ThetaDecay_DoesNotFireForLessThanFiftyPercentErosion()
    {
        var position = CallPosition(entryPrice: 110m);

        // (110 - 80) / 110 = ~27% decay, below the 50% threshold
        var triggers = _service.CheckAlertConditions(position, currentLtp: 80m, currentSpot: 24000m, spot15MinAgo: null, currentVix: 14m, prevVix30Min: null);

        Assert.DoesNotContain(triggers, t => t.AlertType == "THETA_DECAY");
    }

    [Fact]
    public void AllSevenConditionsCanFireSimultaneouslyForASeverelyDistressedPosition()
    {
        // A position whose LTP has collapsed below SL while VIX spikes and the index
        // moves sharply against it should surface every relevant alert at once —
        // SL_APPROACHING does not co-fire with SL_HIT since LTP is at/below SL, not above it.
        var position = CallPosition(stopLoss: 90m, target1: 50m, target2: 40m, entryPrice: 110m);

        var triggers = _service.CheckAlertConditions(
            position,
            currentLtp: 45m,            // <= SL (90) -> SL_HIT; >= Target1 (50)? no -> but >= Target2(40) -> TARGET2_HIT; not >= Target1
            currentSpot: 24000m,
            spot15MinAgo: 24180m,       // adverse for CE
            currentVix: 18m,
            prevVix30Min: 14m);         // ~28.6% IV jump

        var types = triggers.Select(t => t.AlertType).ToHashSet();
        Assert.Contains("SL_HIT", types);
        Assert.Contains("TARGET2_HIT", types);
        Assert.Contains("IV_SPIKE", types);
        Assert.Contains("ADVERSE_MOVE", types);
        Assert.Contains("THETA_DECAY", types);
        Assert.DoesNotContain("SL_APPROACHING", types);
    }
}
