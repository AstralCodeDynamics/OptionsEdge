using OptionsEdge.API.Common.Constants;
using OptionsEdge.API.Domain.Entities;

namespace OptionsEdge.API.Features.Positions;

public class PositionService
{
    public decimal CalculatePnL(Position position, decimal currentLtp)
    {
        int lotSize = GetLotSize(position.Symbol);
        return (currentLtp - position.EntryPrice) * position.Quantity * lotSize;
    }

    public decimal CalculatePnLPct(Position position, decimal pnl)
    {
        int lotSize  = GetLotSize(position.Symbol);
        decimal cost = position.EntryPrice * position.Quantity * lotSize;
        return cost == 0 ? 0 : Math.Round(pnl / cost * 100, 2);
    }

    public (decimal Rs, decimal Pct) CalculateDistanceToSL(decimal currentLtp, decimal sl)
    {
        decimal rs  = currentLtp - sl;
        decimal pct = sl == 0 ? 0 : Math.Round(rs / sl * 100, 2);
        return (Math.Round(rs, 2), pct);
    }

    public (decimal Rs, decimal Pct) CalculateDistanceToTarget(decimal currentLtp, decimal target)
    {
        decimal rs  = target - currentLtp;
        decimal pct = currentLtp == 0 ? 0 : Math.Round(rs / currentLtp * 100, 2);
        return (Math.Round(rs, 2), pct);
    }

    public decimal GetThetaDecayPercent(Position position, decimal currentLtp)
    {
        if (position.EntryPrice == 0) return 0;
        decimal decay = position.EntryPrice - currentLtp;
        return Math.Round(Math.Max(0, decay / position.EntryPrice * 100), 2);
    }

    public List<AlertTrigger> CheckAlertConditions(
        Position position,
        decimal currentLtp,
        decimal currentSpot,
        decimal? spot15MinAgo,
        decimal currentVix,
        decimal? prevVix30Min)
    {
        var triggers = new List<AlertTrigger>();
        string contract = $"{position.Symbol} {position.Strike}{position.OptionType}";

        // 1. SL Approaching: within 10% above SL
        if (currentLtp > position.StopLoss && currentLtp <= position.StopLoss * 1.10m)
        {
            decimal away = Math.Round(currentLtp - position.StopLoss, 2);
            triggers.Add(new AlertTrigger("Warning", "SL_APPROACHING",
                $"SL Approaching — ₹{away} away on {contract}"));
        }

        // 2. SL Hit
        if (currentLtp <= position.StopLoss)
        {
            triggers.Add(new AlertTrigger("Danger", "SL_HIT",
                $"EXIT NOW — Stop Loss Hit on {contract}"));
        }

        // 3. Target 1 reached
        if (currentLtp >= position.Target1)
        {
            triggers.Add(new AlertTrigger("Info", "TARGET1_HIT",
                $"Target 1 Reached — Book partial profits on {contract}"));
        }

        // 4. Target 2 reached
        if (position.Target2.HasValue && currentLtp >= position.Target2.Value)
        {
            triggers.Add(new AlertTrigger("Info", "TARGET2_HIT",
                $"Target 2 Reached — Full target hit on {contract}"));
        }

        // 5. IV Spike > 20% in last 30 min (via VIX as IV proxy)
        if (prevVix30Min.HasValue && prevVix30Min > 0)
        {
            decimal ivChange = Math.Abs(currentVix - prevVix30Min.Value) / prevVix30Min.Value * 100;
            if (ivChange > 20)
            {
                triggers.Add(new AlertTrigger("Warning", "IV_SPIKE",
                    $"IV Spike detected — Premium inflating (+{ivChange:F1}% VIX in 30 min)"));
            }
        }

        // 6. Adverse index move > 0.5% against trade direction in 15 min
        if (spot15MinAgo.HasValue && spot15MinAgo > 0)
        {
            decimal spotChgPct = (currentSpot - spot15MinAgo.Value) / spot15MinAgo.Value * 100;
            bool adverse = position.OptionType.ToUpper() == "CE"
                ? spotChgPct < -0.5m
                : spotChgPct > 0.5m;
            if (adverse)
            {
                decimal pct = Math.Abs(spotChgPct);
                triggers.Add(new AlertTrigger("Danger", "ADVERSE_MOVE",
                    $"Sharp adverse move — {position.Symbol} moved {pct:F2}% against your {position.OptionType} in 15 min"));
            }
        }

        // 7. Theta decay > 50%
        decimal thetaDecay = GetThetaDecayPercent(position, currentLtp);
        if (thetaDecay > 50)
        {
            triggers.Add(new AlertTrigger("Warning", "THETA_DECAY",
                $"50% premium eroded by time decay on {contract}"));
        }

        return triggers;
    }

    private static int GetLotSize(string symbol) =>
        symbol.ToUpper() == "BANKNIFTY" ? AppConstants.LotSizes.BankNifty : AppConstants.LotSizes.Nifty;
}
