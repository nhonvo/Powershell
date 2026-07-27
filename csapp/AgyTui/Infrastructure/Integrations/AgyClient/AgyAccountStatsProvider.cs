namespace AgyTui.Infrastructure.Integrations.AgyClient;

using AgyTui.Infrastructure.Common;

public interface IAgyAccountStatsProvider
{
    long GetPrivateDirectorySize(string path);
    string GetJunctionStatus(string accountName);
    AccountStats GetAccountStats(string accountName);
    void ClearStatsCache();
}

public class AgyAccountStatsProvider : IAgyAccountStatsProvider
{
    private static readonly TtlCache<string, long> _sizeCache = new(TimeSpan.FromSeconds(15));
    private static readonly TtlCache<string, AccountStats> _statsCache = new(TimeSpan.FromSeconds(3));

    public void ClearStatsCache() => _statsCache.InvalidateAll();

    public long GetPrivateDirectorySize(string path)
    {
        return _sizeCache.GetOrCompute(path, () =>
        {
            if (!Directory.Exists(path)) return 0;
            long total = 0;
            try
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    bool inJunction = false;
                    var parent = Path.GetDirectoryName(file);
                    while (parent != null && parent.Length >= path.Length)
                    {
                        var di = new DirectoryInfo(parent);
                        if (di.Exists && di.LinkTarget != null)
                        {
                            inJunction = true;
                            break;
                        }
                        parent = Path.GetDirectoryName(parent);
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
        var destDir = AgyAccountCore.GetAccountDirectory(accountName);
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
            var meta = AgyAccountCore.GetAccountMetadata(accountName);
            var dir = AgyAccountCore.GetAccountDirectory(accountName);
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
            var quota = AgyAccountCore.CalculateRollingQuotas(accountName);
            return new AccountStats(meta.LastUsed, meta.UsageCount, sizeStr, junctionStatus, skillsCount, convCount, tokenStatus, meta.QuotaStatus, quota.RemainingWeekly, quota.Remaining5H);
        });
    }
}
