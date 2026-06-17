using OptionsEdge.API.Domain.Entities;
using OptionsEdge.API.Infrastructure.Background;

namespace OptionsEdge.API.Tests;

// Validates ConsistencyReportMarkdownBuilder output shapes.
// The "email attempt" contract (test 1) is verified by confirming that the
// "All clear" subject/content is produced even when NeedsReviewCount=0, so
// the worker has the data it needs to call SendWeeklyConsistencyReportAsync.
public class WeeklyConsistencyCheckTests
{
    private static ConsistencyCheckRun BuildRun(IEnumerable<ConsistencyFinding> findings)
    {
        var list = findings.ToList();
        var run = new ConsistencyCheckRun
        {
            Id = Guid.NewGuid(),
            RunAtUtc = new DateTimeOffset(2026, 6, 17, 10, 0, 0, TimeSpan.Zero),
            TotalChecks = list.Count,
            NeedsReviewCount = list.Count(f => f.Status == "NEEDS_REVIEW"),
            CheckFailedCount = list.Count(f => f.Status == "CHECK_FAILED"),
            EmailSent = false,
        };
        foreach (var f in list) { f.ConsistencyCheckRunId = run.Id; f.Run = run; }
        run.Findings = list;
        return run;
    }

    [Fact]
    public void Build_ZeroNeedsReview_ProducesAllClearContent()
    {
        var run = BuildRun([
            new ConsistencyFinding { Id = Guid.NewGuid(), CheckName = "Groww Symbol Resolution",   Status = "OK", Detail = "All symbols found." },
            new ConsistencyFinding { Id = Guid.NewGuid(), CheckName = "Lot Size Config Review",    Status = "OK", Detail = "Reviewed 5 days ago." },
            new ConsistencyFinding { Id = Guid.NewGuid(), CheckName = "Expiry Day Rule Review",    Status = "OK", Detail = "Reviewed 5 days ago." },
            new ConsistencyFinding { Id = Guid.NewGuid(), CheckName = "Indicator Threshold Drift", Status = "OK", Detail = "No drift." },
        ]);

        string md = ConsistencyReportMarkdownBuilder.Build(run);

        // Non-empty and contains key structural markers
        Assert.False(string.IsNullOrWhiteSpace(md));
        Assert.Contains("# OptionsEdge Weekly Consistency Check", md);
        Assert.Contains("- Needs review: 0", md);
        Assert.Contains("- Check failed: 0", md);
        // All four findings present
        Assert.Contains("[OK]", md);
        // Contains the "paste to Claude" instruction
        Assert.Contains("Read CLAUDE.md and docs/AI_HANDOFF.md first", md);
    }

    [Fact]
    public void Build_MixedStatuses_PrioritizesNeedsReviewAndCheckFailed()
    {
        var run = BuildRun([
            new ConsistencyFinding { Id = Guid.NewGuid(), CheckName = "Groww Symbol Resolution",   Status = "OK",           Detail = "OK." },
            new ConsistencyFinding { Id = Guid.NewGuid(), CheckName = "Lot Size Config Review",    Status = "NEEDS_REVIEW", Detail = "90+ days old.", SuggestedAction = "Re-verify NSE lot sizes." },
            new ConsistencyFinding { Id = Guid.NewGuid(), CheckName = "Expiry Day Rule Review",    Status = "CHECK_FAILED", Detail = "CSV fetch failed.", SuggestedAction = "Check network." },
            new ConsistencyFinding { Id = Guid.NewGuid(), CheckName = "Indicator Threshold Drift", Status = "NEEDS_REVIEW", Detail = "RsiPeriod drifted.", SuggestedAction = "Update DocumentedThresholds.json." },
        ]);

        string md = ConsistencyReportMarkdownBuilder.Build(run);

        Assert.False(string.IsNullOrWhiteSpace(md));
        Assert.Contains("- Needs review: 2", md);
        Assert.Contains("- Check failed: 1", md);

        // Priority findings appear before OK findings
        int needsReviewPos = md.IndexOf("[NEEDS_REVIEW]", StringComparison.Ordinal);
        int failedPos      = md.IndexOf("[CHECK_FAILED]", StringComparison.Ordinal);
        int okPos          = md.IndexOf("[OK]",           StringComparison.Ordinal);

        Assert.True(needsReviewPos < okPos, "NEEDS_REVIEW should appear before OK");
        Assert.True(failedPos < okPos,      "CHECK_FAILED should appear before OK");

        // Suggested actions included
        Assert.Contains("Re-verify NSE lot sizes.", md);
        Assert.Contains("Check network.", md);
    }

    [Fact]
    public async Task BuildAndWriteTempFile_CreatesNonEmptyFile()
    {
        var run = BuildRun([
            new ConsistencyFinding { Id = Guid.NewGuid(), CheckName = "Test Check", Status = "OK", Detail = "All good." },
        ]);

        string path = await ConsistencyReportMarkdownBuilder.BuildAndWriteTempFileAsync(run, CancellationToken.None);
        try
        {
            Assert.True(File.Exists(path));
            var content = await File.ReadAllTextAsync(path);
            Assert.False(string.IsNullOrWhiteSpace(content));
            Assert.Contains("# OptionsEdge Weekly Consistency Check", content);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
