using Microsoft.EntityFrameworkCore;
using OptionsEdge.API.Domain.Entities;
using OptionsEdge.API.Infrastructure.Background;
using OptionsEdge.API.Infrastructure.Data;

namespace OptionsEdge.API.Features.Signals;

// Stores each user's NIFTY/BANKNIFTY auto-signal schedule and tells AutoSignalWorker
// which (user, symbol) pairs are due at the current IST minute.
public class UserSignalPreferenceService(AppDbContext db)
{
    private static readonly TimeZoneInfo IstZone = GetIstZone();

    public async Task<UserSignalPreference> GetOrCreateAsync(Guid userId, CancellationToken ct = default)
    {
        var pref = await db.UserSignalPreferences.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (pref is not null) return pref;

        pref = new UserSignalPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        db.UserSignalPreferences.Add(pref);
        await db.SaveChangesAsync(ct);
        return pref;
    }

    public async Task SaveAsync(
        Guid userId,
        bool niftyEnabled,
        string niftyTimes,
        bool bankNiftyEnabled,
        string bankNiftyTimes,
        CancellationToken ct = default)
    {
        var pref = await GetOrCreateAsync(userId, ct);
        pref.NiftyAutoSignalEnabled = niftyEnabled;
        pref.NiftyAutoSignalTimes = ValidateTimes(niftyTimes);
        pref.BankNiftyAutoSignalEnabled = bankNiftyEnabled;
        pref.BankNiftyAutoSignalTimes = ValidateTimes(bankNiftyTimes);
        pref.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    // Called by AutoSignalWorker every tick. Returns (userId, symbol) pairs whose
    // schedule matches the current IST minute, during market hours only.
    public async Task<IReadOnlyList<(Guid UserId, string Symbol)>> GetDueSignalsAsync(CancellationToken ct = default)
    {
        if (!MarketHoursHelper.IsMarketOpen())
            return [];

        var istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstZone);
        var currentTime = $"{istNow.Hour:D2}:{istNow.Minute:D2}";

        var candidates = await db.UserSignalPreferences
            .Where(x => x.NiftyAutoSignalEnabled || x.BankNiftyAutoSignalEnabled)
            .ToListAsync(ct);

        var due = new List<(Guid UserId, string Symbol)>();
        foreach (var p in candidates)
        {
            if (p.NiftyAutoSignalEnabled && p.NiftyAutoSignalTimes.Split(',').Contains(currentTime))
                due.Add((p.UserId, "NIFTY"));

            if (p.BankNiftyAutoSignalEnabled && p.BankNiftyAutoSignalTimes.Split(',').Contains(currentTime))
                due.Add((p.UserId, "BANKNIFTY"));
        }
        return due;
    }

    // Keeps only valid times within market hours (09:15-15:25), normalizes to zero-padded
    // "HH:mm" (so GetDueSignalsAsync's string comparison matches), deduplicates, sorts,
    // and caps at 5. Falls back to a single default time if nothing valid remains.
    private static string ValidateTimes(string times)
    {
        var valid = times.Split(',')
            .Select(t => t.Trim())
            .Select(t => TimeOnly.TryParse(t, out var to) ? (TimeOnly?)to : null)
            .Where(to => to.HasValue && to.Value >= new TimeOnly(9, 15) && to.Value <= new TimeOnly(15, 25))
            .Select(to => to!.Value.ToString("HH:mm"))
            .Distinct()
            .OrderBy(t => t)
            .Take(5)
            .ToList();

        return valid.Count > 0 ? string.Join(",", valid) : "09:30";
    }

    private static TimeZoneInfo GetIstZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
    }
}
