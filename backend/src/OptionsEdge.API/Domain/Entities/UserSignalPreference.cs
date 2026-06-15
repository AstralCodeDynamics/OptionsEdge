namespace OptionsEdge.API.Domain.Entities;

// Per-user schedule for automatic AI signal generation, configured separately for
// NIFTY and BANKNIFTY. Times are comma-separated IST "HH:mm" values.
public class UserSignalPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public bool NiftyAutoSignalEnabled { get; set; }
    public string NiftyAutoSignalTimes { get; set; } = "09:30,12:00,14:00";

    public bool BankNiftyAutoSignalEnabled { get; set; }
    public string BankNiftyAutoSignalTimes { get; set; } = "09:30,12:00,14:00";

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
