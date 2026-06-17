using OptionsEdge.API.Domain.Entities;

namespace OptionsEdge.API.Infrastructure.Email;

public interface IEmailService
{
    Task SendEmailConfirmationAsync(string toEmail, string displayName, string confirmationLink, CancellationToken ct = default);
    Task SendPasswordResetAsync(string toEmail, string displayName, string resetLink, CancellationToken ct = default);
    Task SendTwoFactorCodeAsync(string toEmail, string displayName, string code, CancellationToken ct = default);
    Task SendWeeklyConsistencyReportAsync(string toEmail, IReadOnlyList<ConsistencyFinding> findings, string markdownReportPath, CancellationToken ct = default);
}
