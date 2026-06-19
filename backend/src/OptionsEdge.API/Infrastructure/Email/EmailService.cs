using MailKit.Net.Smtp;
using MimeKit;
using OptionsEdge.API.Domain.Entities;

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

    public async Task SendWeeklyConsistencyReportAsync(
        IReadOnlyList<string> toEmails,
        IReadOnlyList<ConsistencyFinding> findings,
        string markdownReportPath,
        CancellationToken ct = default)
    {
        int needsReview = findings.Count(f => f.Status is "NEEDS_REVIEW" or "CHECK_FAILED");
        string subject = needsReview > 0
            ? $"OptionsEdge Weekly Consistency Check — {needsReview} item(s) need review"
            : "OptionsEdge Weekly Consistency Check — All clear";

        var reviewItems = findings
            .Where(f => f.Status is "NEEDS_REVIEW" or "CHECK_FAILED")
            .Select(f => $"<li><strong>[{f.Status}] {f.CheckName}</strong>: {f.Detail}</li>");

        string htmlBody = needsReview > 0
            ? $"<p>{needsReview} item(s) need attention this week:</p><ul>{string.Join("", reviewItems)}</ul><p>Full details in the attached Markdown report.</p>"
            : "<p>All consistency checks passed this week. Full report attached.</p>";

        var section = config.GetSection("Email");
        var smtpHost = section["SmtpHost"] ?? throw new InvalidOperationException("Email:SmtpHost is not configured.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(section["FromName"], section["FromAddress"] ?? "noreply@optionsedge.local"));
        foreach (var toEmail in toEmails)
        {
            message.To.Add(new MailboxAddress("Manu", toEmail));
        }
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        await bodyBuilder.Attachments.AddAsync(markdownReportPath, ct);
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(smtpHost, section.GetValue("SmtpPort", 587), MailKit.Security.SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(section["SmtpUsername"] ?? string.Empty, section["SmtpPassword"] ?? string.Empty, ct);
            await client.SendAsync(message, ct);
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }
        logger.LogInformation("Sent consistency report email to {ToEmail}: {Subject}", string.Join(", ", toEmails), subject);
    }

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
