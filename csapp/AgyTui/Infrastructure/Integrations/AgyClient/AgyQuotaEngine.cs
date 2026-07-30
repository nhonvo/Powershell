
using System.Text.Json;

namespace AgyTui.Infrastructure.Integrations.AgyClient;

public class AgyQuotaEngine : IAgyQuotaEngine
{
    private static readonly TtlCache<string, long> _sizeCache = new(TimeSpan.FromSeconds(15));
    private static readonly TtlCache<string, AccountStats> _statsCache = new(TimeSpan.FromSeconds(3));
    private readonly IAgyAccountStore _accountStore;

    public AgyQuotaEngine(IAgyAccountStore accountStore)
    {
        _accountStore = accountStore;
    }

    public AgyQuotaEngine() : this(new AgyAccountStore()) { }

    private string AgySourceHome => _accountStore.AgySourceHome;

    public void ClearStatsCache() => _statsCache.InvalidateAll();

    public QuotaMetrics CalculateRollingQuotas(string accountName)
    {
        var history = _accountStore.GetAccountMetadata(accountName).RequestHistory;
        var dts = history.Select(ts => DateTime.TryParse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt) ? dt : (DateTime?)null).Where(dt => dt.HasValue).Select(dt => dt!.Value).ToList();
        var (qCount5H, qUsage5H) = CalculateWindowUsage(dts, 5, 50);
        var (qCountWeekly, qUsageWeekly) = CalculateWindowUsage(dts, 168, 1000);

        var now = DateTime.UtcNow;
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
        if (!File.Exists(logPath)) logPath = Path.Combine(AppPaths.LogsDir, "ai_activity_log.jsonl");
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
                            DateTime.TryParse(tsProp.GetString(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                        {
                            dts.Add(dt.ToUniversalTime());
                        }
                    }
                }
            }
            catch { }
        }

        var (qCount5H, qUsage5H) = CalculateWindowUsage(dts, 5, 50);
        var (qCountWeekly, qUsageWeekly) = CalculateWindowUsage(dts, 168, 1000);

        var now = DateTime.UtcNow;
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

        var time5H = qCount5H >= 50 ? FormatTimeSpan(oldest5H.AddHours(5) - now) : "Never";
        var timeWeekly = qCountWeekly >= 1000 ? FormatTimeSpan(oldestWeekly.AddDays(7) - now) : "Never";

        return new QuotaMetrics(
            100.0 - qUsageWeekly, 100.0 - qUsage5H,
            timeWeekly, time5H,
            qCountWeekly, qCount5H,
            timeWeekly, time5H
        );
    }

    private static string FormatTimeSpan(TimeSpan ts) => ts.TotalSeconds <= 0 ? "Now" : $"{(int)ts.TotalHours}h {ts.Minutes}m";

    public List<(DateTime Time, int ReqsReleased, double QuotaGained)> GetQuotaReleaseForecast(string accountName)
    {
        var history = _accountStore.GetAccountMetadata(accountName).RequestHistory;
        var dts = history.Select(ts => DateTime.TryParse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt) ? dt : (DateTime?)null).Where(dt => dt.HasValue).Select(dt => dt!.Value).ToList();
        var forecast5H = ForecastQuotaRelease(dts, 5, 50);
        return forecast5H.Select(f => (f.TimeSlot, f.ReqsReleased, f.RestoredPct)).ToList();
    }

    public void TriggerLowQuotaWebhook(string accountName, double remaining5H)
    {
        var webhookFile = Path.Combine(AgySourceHome, "quota_webhook.txt");
        _ = TriggerLowQuotaWebhookAsync(accountName, remaining5H, webhookFile);
    }

    public long GetPrivateDirectorySize(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return 0;
        return _sizeCache.GetOrCompute(path, () =>
        {
            long total = 0;
            try
            {
                var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var dir = Path.GetDirectoryName(file);
                    bool inJunction = false;
                    while (dir != null && dir.Length >= path.Length)
                    {
                        if (new DirectoryInfo(dir).LinkTarget != null) { inJunction = true; break; }
                        dir = Path.GetDirectoryName(dir);
                    }
                    if (!inJunction) total += new FileInfo(file).Length;
                }
            }
            catch { }
            return total;
        });
    }

    public string GetJunctionStatus(string accountName)
    {
        if (string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase)) return "Healthy (Primary)";
        var destDir = _accountStore.GetAccountDirectory(accountName);
        if (!Directory.Exists(destDir)) return "Uninitialized";
        var shared = new[] { "antigravity", "antigravity-cli", "config", "history", "antigravity-ide", "wf" };
        foreach (var sub in shared)
        {
            var subPath = Path.Combine(destDir, sub);
            if (!Directory.Exists(subPath)) return "Needs Repair";
            if (new DirectoryInfo(subPath).LinkTarget == null) return "Needs Repair";
        }
        return "Healthy";
    }

    public AccountStats GetAccountStats(string accountName)
    {
        return _statsCache.GetOrCompute(accountName, () =>
        {
            var meta = _accountStore.GetAccountMetadata(accountName);
            var dir = _accountStore.GetAccountDirectory(accountName);
            var privateSize = GetPrivateDirectorySize(dir);
            var junctionStatus = GetJunctionStatus(accountName);
            int skillsCount = 0, convCount = 0;
            var skillsPath = Path.Combine(dir, "config", "skills");
            if (!Directory.Exists(skillsPath)) skillsPath = Path.Combine(dir, "skills");
            if (Directory.Exists(skillsPath)) skillsCount = Directory.GetDirectories(skillsPath).Length;

            var convPath = Path.Combine(dir, "antigravity", "brain");
            if (!Directory.Exists(convPath)) convPath = Path.Combine(dir, "brain");
            if (Directory.Exists(convPath)) convCount = Directory.GetDirectories(convPath).Length;
            var tokenStatus = File.Exists(Path.Combine(dir, "keyring_token.txt")) ? "Logged In" : "Not Logged In";
            string sizeStr;
            if (privateSize > 1_048_576) sizeStr = $"{Math.Round(privateSize / 1_048_576.0, 2)} MB";
            else if (privateSize > 1_024) sizeStr = $"{Math.Round(privateSize / 1_024.0, 2)} KB";
            else sizeStr = $"{privateSize} B";
            var quota = CalculateRollingQuotas(accountName);
            return new AccountStats(meta.LastUsed, meta.UsageCount, sizeStr, junctionStatus, skillsCount, convCount, tokenStatus, meta.QuotaStatus, quota.RemainingWeekly, quota.Remaining5H);
        });
    }

    public static (int Count, double UsagePct) CalculateWindowUsage(IEnumerable<DateTime> timestamps, double limitWindowHours, int maxLimit)
    {
        if (timestamps == null || maxLimit <= 0) return (0, 0.0);
        var now = DateTime.UtcNow;
        var cutoff = now.AddHours(-limitWindowHours);
        int count = timestamps.Count(ts => ts >= cutoff);
        double usagePct = Math.Min(100.0, (double)count / maxLimit * 100.0);
        return (count, usagePct);
    }

    public static List<(DateTime TimeSlot, int ReqsReleased, double RestoredPct)> ForecastQuotaRelease(IEnumerable<DateTime> timestamps, double limitWindowHours, int maxLimit)
    {
        var result = new List<(DateTime TimeSlot, int ReqsReleased, double RestoredPct)>();
        if (timestamps == null || maxLimit <= 0) return result;
        var now = DateTime.UtcNow;
        var cutoff = now.AddHours(-limitWindowHours);
        var inWindow = timestamps.Where(ts => ts >= cutoff).OrderBy(ts => ts).ToList();
        if (inWindow.Count == 0) return result;

        var grouped = inWindow.GroupBy(ts =>
        {
            var expiry = ts.AddHours(limitWindowHours);
            return new DateTime(expiry.Year, expiry.Month, expiry.Day, expiry.Hour, (expiry.Minute / 15) * 15, 0, DateTimeKind.Utc);
        }).OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            int reqs = group.Count();
            double restoredPct = Math.Round((double)reqs / maxLimit * 100.0, 1);
            result.Add((group.Key, reqs, restoredPct));
        }
        return result;
    }

    public static async Task TriggerLowQuotaWebhookAsync(string accountName, double remainingPct, string webhookUrlFile)
    {
        if (!File.Exists(webhookUrlFile)) return;
        try
        {
            var url = (await File.ReadAllTextAsync(webhookUrlFile)).Trim();
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;
            var payload = new
            {
                Account = accountName,
                Remaining5H = $"{Math.Round(remainingPct, 1)}%",
                Timestamp = DateTime.UtcNow.ToString("o"),
                Message = $"Warning: Account '{accountName}' has low quota ({Math.Round(remainingPct, 1)}% remaining in 5H window)."
            };
            using var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
            await HttpClientProvider.Client.PostAsync(uri, content);
        }
        catch { }
    }
}

public static class QuotaTracker
{
    public static (int Count, double UsagePct) CalculateWindowUsage(IEnumerable<DateTime> timestamps, double limitWindowHours, int maxLimit)
        => AgyQuotaEngine.CalculateWindowUsage(timestamps, limitWindowHours, maxLimit);

    public static List<(DateTime TimeSlot, int ReqsReleased, double RestoredPct)> ForecastQuotaRelease(IEnumerable<DateTime> timestamps, double limitWindowHours, int maxLimit)
        => AgyQuotaEngine.ForecastQuotaRelease(timestamps, limitWindowHours, maxLimit);

    public static Task TriggerLowQuotaWebhookAsync(string accountName, double remainingPct, string webhookUrlFile)
        => AgyQuotaEngine.TriggerLowQuotaWebhookAsync(accountName, remainingPct, webhookUrlFile);
}
