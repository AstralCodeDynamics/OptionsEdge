namespace OptionsEdge.API.Infrastructure.Auth;

public class AuthSettings
{
    public bool RequireEmailConfirmation { get; set; } = true;
    public bool RequireTwoFactor { get; set; } = false;
    public bool EnableLockout { get; set; } = true;
    public bool SendRealEmails { get; set; } = true;
}
