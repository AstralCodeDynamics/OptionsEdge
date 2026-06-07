namespace OptionsEdge.API.Infrastructure.Email;

public class DevEmailService(ILogger<DevEmailService> logger) : IEmailService
{
    public Task SendEmailConfirmationAsync(string toEmail, string displayName, string confirmationLink, CancellationToken ct = default)
    {
        logger.LogInformation("[DevEmail] Email confirmation for {ToEmail} ({DisplayName}): {Link}", toEmail, displayName, confirmationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string toEmail, string displayName, string resetLink, CancellationToken ct = default)
    {
        logger.LogInformation("[DevEmail] Password reset for {ToEmail} ({DisplayName}): {Link}", toEmail, displayName, resetLink);
        return Task.CompletedTask;
    }

    public Task SendTwoFactorCodeAsync(string toEmail, string displayName, string code, CancellationToken ct = default)
    {
        logger.LogInformation("[DevEmail] Two-factor code for {ToEmail} ({DisplayName}): {Code}", toEmail, displayName, code);
        return Task.CompletedTask;
    }
}
