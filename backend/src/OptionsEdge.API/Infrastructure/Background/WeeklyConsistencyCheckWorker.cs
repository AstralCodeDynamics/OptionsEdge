using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OptionsEdge.API.Common.Constants;
using OptionsEdge.API.Domain.Entities;
using OptionsEdge.API.Infrastructure.Data;
using OptionsEdge.API.Infrastructure.Email;

namespace OptionsEdge.API.Infrastructure.Background;

public class WeeklyConsistencyCheckWorker(
    IServiceScopeFactory scopeFactory,
    IWebHostEnvironment env,
    IConfiguration config,
    ILogger<WeeklyConsistencyCheckWorker> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromDays(7);
    private const int StalenessThresholdDays = 90;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("WeeklyConsistencyCheckWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = await GetDelayUntilNextRunAsync(stoppingToken);
            if (delay > TimeSpan.Zero)
            {
                logger.LogInformation("Next consistency check in {Delay}", delay);
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            try
            {
                await RunChecksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WeeklyConsistencyCheckWorker run failed");
                try { await Task.Delay(TimeSpan.FromHours(1), stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            }
        }
    }

    private async Task<TimeSpan> GetDelayUntilNextRunAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var lastRun = await db.ConsistencyCheckRuns
            .OrderByDescending(r => r.RunAtUtc)
            .Select(r => r.RunAtUtc)
            .FirstOrDefaultAsync(ct);

        if (lastRun == default)
            return TimeSpan.Zero;

        var nextRun = lastRun + CheckInterval;
        var delay = nextRun - DateTimeOffset.UtcNow;
        return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
    }

    private async Task RunChecksAsync(CancellationToken ct)
    {
        logger.LogInformation("Running weekly consistency checks");

        var findings = new List<ConsistencyFinding>();
        findings.Add(await CheckGrowwSymbolsAsync(ct));
        findings.Add(CheckLotSizeStaleness());
        findings.Add(CheckExpiryRuleStaleness());
        findings.Add(CheckIndicatorThresholdDrift());

        var run = new ConsistencyCheckRun
        {
            Id = Guid.NewGuid(),
            RunAtUtc = DateTimeOffset.UtcNow,
            TotalChecks = findings.Count,
            NeedsReviewCount = findings.Count(f => f.Status == "NEEDS_REVIEW"),
            CheckFailedCount = findings.Count(f => f.Status == "CHECK_FAILED"),
            EmailSent = false,
        };

        foreach (var f in findings)
        {
            f.Id = Guid.NewGuid();
            f.ConsistencyCheckRunId = run.Id;
            f.Run = run;
        }
        run.Findings = findings;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.ConsistencyCheckRuns.Add(run);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Consistency check persisted. Run {RunId}: {Total} checks, {Review} need review, {Failed} failed",
            run.Id, run.TotalChecks, run.NeedsReviewCount, run.CheckFailedCount);

        var alertEmail = config["Ops:AlertEmail"];
        if (string.IsNullOrWhiteSpace(alertEmail))
        {
            logger.LogWarning("Ops:AlertEmail not configured — skipping consistency report email");
            return;
        }

        string? reportPath = null;
        try
        {
            reportPath = await ConsistencyReportMarkdownBuilder.BuildAndWriteTempFileAsync(run, ct);
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            await emailService.SendWeeklyConsistencyReportAsync(alertEmail, run.Findings, reportPath, ct);

            run.EmailSent = true;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Consistency report email failed — run data is persisted (RunId={RunId})", run.Id);
        }
        finally
        {
            if (reportPath is not null && File.Exists(reportPath))
                File.Delete(reportPath);
        }
    }

    private async Task<ConsistencyFinding> CheckGrowwSymbolsAsync(CancellationToken ct)
    {
        const string checkName = "Groww Symbol Resolution";
        const string csvUrl = "https://growwapi-assets.groww.in/instruments/instrument.csv";
        var required = new[] { "INDIAVIX", "NIFTY", "BANKNIFTY" };

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            string csv = await http.GetStringAsync(csvUrl, ct);

            var missing = required.Where(s => !csv.Contains(s, StringComparison.OrdinalIgnoreCase)).ToList();
            if (missing.Count == 0)
            {
                return Ok(checkName, "All required Groww trading symbols (INDIAVIX, NIFTY, BANKNIFTY) found in instruments CSV.");
            }

            return NeedsReview(checkName,
                $"Symbol(s) not found in Groww instruments CSV: {string.Join(", ", missing)}.",
                $"Check the Groww instruments CSV at {csvUrl} and update the trading_symbol values in GrowwUserApiClient.GetVixAsync / GetSpotSnapshotAsync.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Groww symbol check could not fetch instruments CSV");
            return Failed(checkName,
                $"Could not fetch Groww instruments CSV: {ex.Message}",
                $"Verify network access to {csvUrl} and retry.");
        }
    }

    private ConsistencyFinding CheckLotSizeStaleness()
    {
        const string checkName = "Lot Size Config Review";
        var raw = config["LotSizes:LastReviewedUtc"];

        return CheckStaleness(checkName, raw,
            "NSE/SEBI lot sizes (LotSizes:LastReviewedUtc in appsettings) were last reviewed {days} days ago — over the 90-day threshold.",
            "Verify current NIFTY and BANKNIFTY lot sizes on NSE website. If unchanged, update LotSizes:LastReviewedUtc in appsettings.Development.json and production secrets to today's date.",
            "NSE/SEBI lot sizes (LotSizes:LastReviewedUtc) have not been reviewed. Set LotSizes:LastReviewedUtc in appsettings after your first review.",
            $"Lot sizes last reviewed {GetDaysAgo(raw)} days ago — within the 90-day window.");
    }

    private ConsistencyFinding CheckExpiryRuleStaleness()
    {
        const string checkName = "Expiry Day Rule Review";
        var raw = config["ExpiryRules:LastReviewedUtc"];

        return CheckStaleness(checkName, raw,
            "NSE expiry day rules (ExpiryRules:LastReviewedUtc) were last reviewed {days} days ago — over the 90-day threshold.",
            "Verify NIFTY weekly (Tuesday) and BANKNIFTY monthly (last Tuesday) expiry rules on NSE circulars. If unchanged, update ExpiryRules:LastReviewedUtc to today's date.",
            "Expiry day rules (ExpiryRules:LastReviewedUtc) have not been reviewed. Set this key after your first review.",
            $"Expiry rules last reviewed {GetDaysAgo(raw)} days ago — within the 90-day window.");
    }

    private static ConsistencyFinding CheckStaleness(
        string checkName,
        string? rawDate,
        string staleDetailTemplate,
        string suggestedAction,
        string missingDetail,
        string okDetail)
    {
        if (string.IsNullOrWhiteSpace(rawDate) || !DateTimeOffset.TryParse(rawDate, out var lastReviewed))
            return NeedsReview(checkName, missingDetail, suggestedAction);

        int days = (int)(DateTimeOffset.UtcNow - lastReviewed).TotalDays;
        if (days > StalenessThresholdDays)
            return NeedsReview(checkName, staleDetailTemplate.Replace("{days}", days.ToString()), suggestedAction);

        return Ok(checkName, okDetail);
    }

    private ConsistencyFinding CheckIndicatorThresholdDrift()
    {
        const string checkName = "Indicator Threshold Doc/Code Drift";
        var jsonPath = Path.Combine(env.ContentRootPath, "Common", "Constants", "DocumentedThresholds.json");

        if (!File.Exists(jsonPath))
            return Failed(checkName,
                $"DocumentedThresholds.json not found at {jsonPath}.",
                "Restore the file from git or create it at Common/Constants/DocumentedThresholds.json.");

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
        }
        catch (Exception ex)
        {
            return Failed(checkName, $"DocumentedThresholds.json parse error: {ex.Message}", "Fix the JSON syntax in Common/Constants/DocumentedThresholds.json.");
        }

        var drifts = new List<string>();

        Check(doc, drifts, "RsiPeriod",                 AppConstants.IndicatorThresholds.RsiPeriod);
        Check(doc, drifts, "RsiOverbought",              AppConstants.IndicatorThresholds.RsiOverbought);
        Check(doc, drifts, "RsiOversold",                AppConstants.IndicatorThresholds.RsiOversold);
        Check(doc, drifts, "MacdFastPeriod",             AppConstants.IndicatorThresholds.MacdFastPeriod);
        Check(doc, drifts, "MacdSlowPeriod",             AppConstants.IndicatorThresholds.MacdSlowPeriod);
        Check(doc, drifts, "MacdSignalPeriod",           AppConstants.IndicatorThresholds.MacdSignalPeriod);
        Check(doc, drifts, "BollingerPeriod",            AppConstants.IndicatorThresholds.BollingerPeriod);
        CheckDouble(doc, drifts, "BollingerStdDev",      AppConstants.IndicatorThresholds.BollingerStdDev);
        CheckDouble(doc, drifts, "BollingerSqueezeBandwidth", AppConstants.IndicatorThresholds.BollingerSqueezeBandwidth);
        Check(doc, drifts, "AdxPeriod",                  AppConstants.IndicatorThresholds.AdxPeriod);
        Check(doc, drifts, "AdxWeakThreshold",           AppConstants.IndicatorThresholds.AdxWeakThreshold);
        Check(doc, drifts, "AdxStrongThreshold",         AppConstants.IndicatorThresholds.AdxStrongThreshold);
        Check(doc, drifts, "Ema9Period",                 AppConstants.IndicatorThresholds.Ema9Period);
        Check(doc, drifts, "Ema20Period",                AppConstants.IndicatorThresholds.Ema20Period);
        Check(doc, drifts, "Ema50Period",                AppConstants.IndicatorThresholds.Ema50Period);
        Check(doc, drifts, "Ema200Period",               AppConstants.IndicatorThresholds.Ema200Period);
        Check(doc, drifts, "SupertrendPeriod",           AppConstants.IndicatorThresholds.SupertrendPeriod);
        CheckDouble(doc, drifts, "SupertrendMultiplier", AppConstants.IndicatorThresholds.SupertrendMultiplier);

        doc.Dispose();

        if (drifts.Count == 0)
            return Ok(checkName, "All indicator thresholds match DocumentedThresholds.json.");

        return NeedsReview(checkName,
            $"Threshold drift detected: {string.Join("; ", drifts)}",
            "Update DocumentedThresholds.json to match AppConstants.IndicatorThresholds, or revert AppConstants.IndicatorThresholds to the documented values.");
    }

    private static void Check(JsonDocument doc, List<string> drifts, string key, int codeValue)
    {
        if (doc.RootElement.TryGetProperty(key, out var prop) && prop.TryGetInt32(out int jsonValue))
        {
            if (jsonValue != codeValue)
                drifts.Add($"{key}: doc={jsonValue} code={codeValue}");
        }
        else
        {
            drifts.Add($"{key}: missing or non-integer in DocumentedThresholds.json");
        }
    }

    private static void CheckDouble(JsonDocument doc, List<string> drifts, string key, double codeValue)
    {
        if (doc.RootElement.TryGetProperty(key, out var prop) && prop.TryGetDouble(out double jsonValue))
        {
            if (Math.Abs(jsonValue - codeValue) > 1e-9)
                drifts.Add($"{key}: doc={jsonValue} code={codeValue}");
        }
        else
        {
            drifts.Add($"{key}: missing or non-numeric in DocumentedThresholds.json");
        }
    }

    private static int GetDaysAgo(string? rawDate)
    {
        if (string.IsNullOrWhiteSpace(rawDate) || !DateTimeOffset.TryParse(rawDate, out var d))
            return -1;
        return (int)(DateTimeOffset.UtcNow - d).TotalDays;
    }

    private static ConsistencyFinding Ok(string checkName, string detail) =>
        new() { CheckName = checkName, Status = "OK", Detail = detail };

    private static ConsistencyFinding NeedsReview(string checkName, string detail, string? suggestedAction = null) =>
        new() { CheckName = checkName, Status = "NEEDS_REVIEW", Detail = detail, SuggestedAction = suggestedAction };

    private static ConsistencyFinding Failed(string checkName, string detail, string? suggestedAction = null) =>
        new() { CheckName = checkName, Status = "CHECK_FAILED", Detail = detail, SuggestedAction = suggestedAction };
}
