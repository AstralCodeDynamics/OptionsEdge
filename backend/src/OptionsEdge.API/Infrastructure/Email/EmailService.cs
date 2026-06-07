using MailKit.Net.Smtp;
using MimeKit;

namespace OptionsEdge.API.Infrastructure.Email;

public class EmailService(IConfiguration config, ILogger<EmailService> logger) : IEmailService
{
    public Task SendEmailConfirmationAsync(string toEmail, string displayName, string confirmationLink, CancellationToken ct = default) =>
        SendAsync(toEmail, displayName, "Confirm your OptionsEdge email",
            $"<p>Hi {displayName},</p><p>Please confirm your email by clicking the link below:</p><p><a href=\"{confirmationLink}\">Confirm email</a></p>", ct);

    public Task SendPasswordResetAsync(string toEmail, string displayName, string resetLink, CancellationToken ct = default) =>
        SendAsync(toEmail, displayName, "Reset your OptionsEdge password",
            $"<p>Hi {displayName},</p><p>Reset your password using the link below:</p><p><a href=\"{resetLink}\">Reset password</a></p>", ct);

    public Task SendTwoFactorCodeAsync(string toEmail, string displayName, string code, CancellationToken ct = default) =>
        SendAsync(toEmail, displayName, "Your OptionsEdge verification code",
            $"<p>Hi {displayName},</p><p>Your verification code is: <strong>{code}</strong></p>", ct);

    private async Task SendAsync(string toEmail, string displayName, string subject, string htmlBody, CancellationToken ct)
    {
        var section = config.GetSection("Email");
        var smtpHost = section["SmtpHost"] ?? throw new InvalidOperationException("Email:SmtpHost is not configured.");
        var smtpUsername = section["SmtpUsername"] ?? string.Empty;
        var smtpPassword = section["SmtpPassword"] ?? string.Empty;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(section["FromName"], section["FromAddress"] ?? "noreply@optionsedge.local"));
        message.To.Add(new MailboxAddress(displayName, toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(smtpHost, section.GetValue("SmtpPort", 587), MailKit.Security.SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(smtpUsername, smtpPassword, ct);
            await client.SendAsync(message, ct);
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }
        logger.LogInformation("Sent email {Subject} to {ToEmail}", subject, toEmail);
    }
}
