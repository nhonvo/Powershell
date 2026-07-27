namespace AgyTui.Infrastructure.Integrations.AgyClient;

using System.Text.Json;
using System.Text.RegularExpressions;

public interface IAgyQuotaCalculator
{
    QuotaMetrics CalculateRollingQuotas(string accountName);
    QuotaMetrics CalculateRollingQuotasForAgent(string agentName);
    List<(DateTime Time, int ReqsReleased, double QuotaGained)> GetQuotaReleaseForecast(string accountName);
    void TriggerLowQuotaWebhook(string accountName, double remaining5H);
    bool CheckQuotaAfterRun(string accountName);
}

public class AgyQuotaCalculator : IAgyQuotaCalculator
{
    private string AgySourceHome => AgyAccountCore.AgySourceHome;

    public QuotaMetrics CalculateRollingQuotas(string accountName)
    {
        var history = AgyAccountCore.GetAccountMetadata(accountName).RequestHistory;
        var dts = history.Select(ts => DateTime.TryParse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt) ? dt : (DateTime?)null).Where(dt => dt.HasValue).Select(dt => dt!.Value).ToList();
        var (qCount5H, qUsage5H) = QuotaTracker.CalculateWindowUsage(dts, 5, 50);
        var (qCountWeekly, qUsageWeekly) = QuotaTracker.CalculateWindowUsage(dts, 168, 1000);

        var now = AgyAccountCore.Clock.GetUtcNow().UtcDateTime;
        var fiveHoursAgo = now.AddHours(-5);
        var sevenDaysAgo = now.AddDays(-7);
        int reqs5H = qCount5H, reqsWeekly = qCountWeekly;
        var oldest5H = now;
        var oldestWeekly = now;
        foreach (var dt in dts)
        {
            if (dt >= fiveHoursAgo && dt < oldest5H) oldest5H = dt;
            if (dt >= sevenDaysAgo && dt < oldestWeekly) oldestWeekly = dt;
        }
        const int limit5H = 50, limitWeekly = 1000;
        var remaining5H = Math.Max(0.0, 100.0 - qUsage5H);
        var remainingWeekly = Math.Max(0.0, 100.0 - qUsageWeekly);
        var secs5H = Math.Max(0, (int)Math.Round((oldest5H.AddHours(5) - now).TotalSeconds));
        var secsWeekly = Math.Max(0, (int)Math.Round((oldestWeekly.AddDays(7) - now).TotalSeconds));

        var oneHourAgo = now.AddHours(-1);
        double reqsLastHour = dts.Count(dt => dt >= oneHourAgo);
        string exhaustion5H = "Never";
        if (reqsLastHour > 0 && reqs5H > 0)
        {
            double remainingReqs = limit5H - reqs5H;
            double hoursToExhaustion = remainingReqs / reqsLastHour;
            if (hoursToExhaustion <= 0) exhaustion5H = "Now";
            else if (hoursToExhaustion > 24) exhaustion5H = $"{Math.Round(hoursToExhaustion / 24, 1)} days";
            else exhaustion5H = $"{Math.Round(hoursToExhaustion, 1)} hours";
        }

        var oneDayAgo = now.AddDays(-1);
        double reqsLastDay = dts.Count(dt => dt >= oneDayAgo);
        string exhaustionWeekly = "Never";
        if (reqsLastDay > 0 && reqsWeekly > 0)
        {
            double remainingReqs = limitWeekly - reqsWeekly;
            double daysToExhaustion = remainingReqs / reqsLastDay;
            if (daysToExhaustion <= 0) exhaustionWeekly = "Now";
            else exhaustionWeekly = $"{Math.Round(daysToExhaustion, 1)} days";
        }

        static string Fmt(int s) => $"{s / 3600}h {(s % 3600) / 60}m";
        return new QuotaMetrics(remainingWeekly, remaining5H, Fmt(secsWeekly), Fmt(secs5H), reqsWeekly, reqs5H, exhaustionWeekly, exhaustion5H);
    }

    public QuotaMetrics CalculateRollingQuotasForAgent(string agentName)
    {
        var logPath = Path.Combine(AgySourceHome, "ai_activity_log.jsonl");
        var dts = new List<DateTime>();
        if (File.Exists(logPath))
        {
            try
            {
                foreach (var line in File.ReadLines(logPath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    using var doc = JsonDocument.Parse(line);
                    if (doc.RootElement.TryGetProperty("Agent", out var agentProp) &&
                        string.Equals(agentProp.GetString(), agentName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (doc.RootElement.TryGetProperty("Timestamp", out var tsProp) &&
                            DateTime.TryParse(tsProp.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                        {
                            dts.Add(dt);
                        }
                    }
                }
            }
            catch { }
        }

        var (qCount5H, qUsage5H) = QuotaTracker.CalculateWindowUsage(dts, 5, 50);
        var (qCountWeekly, qUsageWeekly) = QuotaTracker.CalculateWindowUsage(dts, 168, 1000);

        var now = AgyAccountCore.Clock.GetUtcNow().UtcDateTime;
        var fiveHoursAgo = now.AddHours(-5);
        var sevenDaysAgo = now.AddDays(-7);
        int reqs5H = qCount5H, reqsWeekly = qCountWeekly;
        var oldest5H = now;
        var oldestWeekly = now;
        foreach (var dt in dts)
        {
            if (dt >= fiveHoursAgo && dt < oldest5H) oldest5H = dt;
            if (dt >= sevenDaysAgo && dt < oldestWeekly) oldestWeekly = dt;
        }
        var remaining5H = Math.Max(0.0, 100.0 - qUsage5H);
        var remainingWeekly = Math.Max(0.0, 100.0 - qUsageWeekly);
        var secs5H = Math.Max(0, (int)Math.Round((oldest5H.AddHours(5) - now).TotalSeconds));
        var secsWeekly = Math.Max(0, (int)Math.Round((oldestWeekly.AddDays(7) - now).TotalSeconds));

        static string Fmt(int s) => $"{s / 3600}h {(s % 3600) / 60}m";
        return new QuotaMetrics(remainingWeekly, remaining5H, Fmt(secsWeekly), Fmt(secs5H), reqsWeekly, reqs5H, "Never", "Never");
    }

    public List<(DateTime Time, int ReqsReleased, double QuotaGained)> GetQuotaReleaseForecast(string accountName)
    {
        var history = AgyAccountCore.GetAccountMetadata(accountName).RequestHistory;
        var dts = history.Select(ts => DateTime.TryParse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt) ? dt : (DateTime?)null).Where(dt => dt.HasValue).Select(dt => dt!.Value).ToList();
        var forecast = QuotaTracker.ForecastQuotaRelease(dts, 5, 50);
        return forecast.Select(f => (f.TimeSlot, f.ReqsReleased, f.RestoredPct)).ToList();
    }

    public void TriggerLowQuotaWebhook(string accountName, double remaining5H)
    {
        var webhookFile = Path.Combine(AgySourceHome, "quota_webhook.txt");
        _ = QuotaTracker.TriggerLowQuotaWebhookAsync(accountName, remaining5H, webhookFile);
    }

    public bool CheckQuotaAfterRun(string accountName)
    {
        try
        {
            var accDir = AgyAccountCore.GetAccountDirectory(accountName);
            var brainDir = Path.Combine(accDir, "antigravity", "brain");
            if (!Directory.Exists(brainDir)) brainDir = Path.Combine(AgySourceHome, "antigravity", "brain");
            if (!Directory.Exists(brainDir)) return false;
            var latest = new DirectoryInfo(brainDir).EnumerateFiles("transcript.jsonl", SearchOption.AllDirectories).OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
            if (latest == null) return false;
            if ((DateTime.Now - latest.LastWriteTime).TotalSeconds > 60) return false;
            var tail = File.ReadLines(latest.FullName).TakeLast(15);
            var quotaErr = tail.Any(line => Regex.IsMatch(line, @"RESOURCE_EXHAUSTED|quota exceeded|quotaExceeded|ResourceExhausted|quota limit") && Regex.IsMatch(line, @"""status""\s*:\s*""ERROR""|""code""\s*:\s*429"));
            AgyAccountCore.SetAccountQuotaExceeded(accountName, quotaErr);
            return quotaErr;
        }
        catch
        {
            return false;
        }
    }
}
