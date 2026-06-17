using System.Text;
using OptionsEdge.API.Domain.Entities;

namespace OptionsEdge.API.Infrastructure.Background;

public static class ConsistencyReportMarkdownBuilder
{
    private static readonly TimeZoneInfo IstZone = GetIstZone();

    public static async Task<string> BuildAndWriteTempFileAsync(ConsistencyCheckRun run, CancellationToken ct)
    {
        var content = Build(run);
        var path = Path.Combine(Path.GetTempPath(), $"optionsedge-consistency-{run.Id:N}.md");
        await File.WriteAllTextAsync(path, content, ct);
        return path;
    }

    public static string Build(ConsistencyCheckRun run)
    {
        var ist = TimeZoneInfo.ConvertTimeFromUtc(run.RunAtUtc.UtcDateTime, IstZone);
        var sb = new StringBuilder();

        sb.AppendLine("# OptionsEdge Weekly Consistency Check");
        sb.AppendLine($"Run: {ist:yyyy-MM-dd HH:mm:ss} IST");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine($"- Total checks: {run.TotalChecks}");
        sb.AppendLine($"- Needs review: {run.NeedsReviewCount}");
        sb.AppendLine($"- Check failed: {run.CheckFailedCount}");
        sb.AppendLine();
        sb.AppendLine("## Findings");
        sb.AppendLine();

        // NEEDS_REVIEW and CHECK_FAILED first
        var priority = run.Findings
            .Where(f => f.Status is "NEEDS_REVIEW" or "CHECK_FAILED")
            .OrderBy(f => f.Status);

        var ok = run.Findings
            .Where(f => f.Status == "OK")
            .OrderBy(f => f.CheckName);

        foreach (var f in priority.Concat(ok))
        {
            sb.AppendLine($"### [{f.Status}] {f.CheckName}");
            sb.AppendLine($"Detail: {f.Detail}");
            if (!string.IsNullOrEmpty(f.SuggestedAction))
                sb.AppendLine($"Suggested action: {f.SuggestedAction}");
            sb.AppendLine();
        }

        sb.AppendLine("## If You Need to Fix Something");
        sb.AppendLine("Paste the relevant finding above directly to Claude Code or Codex along with:");
        sb.AppendLine();
        sb.AppendLine("> \"Read CLAUDE.md and docs/AI_HANDOFF.md first. [paste finding here]. Fix based on the Suggested Action above.\"");

        return sb.ToString();
    }

    private static TimeZoneInfo GetIstZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
    }
}
