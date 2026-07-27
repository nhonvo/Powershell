namespace AgyTui.Infrastructure.Integrations.AgyClient;

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AgyTui.Core.Interfaces;
using AgyTui.Core.Models;
using AgyTui.Infrastructure.Common;
using AgyTui.Infrastructure.Di;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

public sealed record QuotaMetrics(double RemainingWeekly, double Remaining5H, string TimeWeekly, string Time5H, int CountWeekly, int Count5H, string ExhaustionWeekly, string Exhaustion5H);

public sealed record AccountStats(string LastUsed, int UsageCount, string PrivateSize, string JunctionStatus, int SkillsCount, int ConversationsCount, string TokenStatus, string QuotaStatus, double GeminiWeekly, double GeminiFiveHour);

public static class AgyAccountCore
{
    public static TimeProvider Clock { get; set; } = TimeProvider.System;

    public static IAgyClient AgyClientInstance => Bootstrapper.ServiceProvider.GetRequiredService<IAgyClient>();

    public static string AgySourceHome
    {
        get
        {
            var cfgHome = Config.Current.AgySourceHome;
            if (!string.IsNullOrEmpty(cfgHome)) return cfgHome;
            return AppPaths.GeminiHome;
        }
    }

    public static string AgyAccountPrefix
    {
        get
        {
            if (AgySourceHome.StartsWith(@"C:\Users\Public", StringComparison.OrdinalIgnoreCase))
            {
                return @"C:\Users\Public\.gemini_";
            }
            var accountsDir = Path.Combine(AppPaths.DataDir, ".gemini_");
            Directory.CreateDirectory(Path.GetDirectoryName(accountsDir)!);
            return accountsDir;
        }
    }

    public static string ActiveAccountFile => Path.Combine(AgySourceHome, "active_account.txt");
    public static string AgyActiveAccountFile => ActiveAccountFile;

    private static readonly TtlCache<string, bool> _networkCache = new(TimeSpan.FromSeconds(10));

    public static bool CheckNetworkStatus()
    {
        return _networkCache.GetOrCompute("status", () =>
        {
            try
            {
                var res = HttpClientProvider.Client.GetAsync("https://www.google.com").GetAwaiter().GetResult();
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        });
    }

    public static string GetAccountDirectory(string accountName)
    {
        if (string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase))
            return AgySourceHome;

        return $"{AgyAccountPrefix}{accountName}";
    }

    public static string[] GetAccounts()
    {
        var accounts = new List<string> { "default" };
        var scanPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var userProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? "";
        if (Directory.Exists(userProfile)) scanPaths.Add(userProfile);
        var publicDir = @"C:\Users\Public";
        if (Directory.Exists(publicDir)) scanPaths.Add(publicDir);
        var prefixParent = Path.GetDirectoryName(AgyAccountPrefix);
        if (prefixParent != null && Directory.Exists(prefixParent)) scanPaths.Add(prefixParent);

        foreach (var scanPath in scanPaths)
        {
            foreach (var dir in Directory.GetDirectories(scanPath, ".gemini_*"))
            {
                var m = Regex.Match(Path.GetFileName(dir), @"^\.gemini_(.+)$");
                if (!m.Success) continue;
                var name = m.Groups[1].Value;
                if (!Regex.IsMatch(name, @"^(backup|copy|temp)([_-]|$)", RegexOptions.IgnoreCase) && !accounts.Contains(name, StringComparer.OrdinalIgnoreCase)) accounts.Add(name);
            }
        }
        return [.. accounts];
    }

    public static string GetActiveAccount() => AgyClientInstance.GetActiveAccount();

    public static string? GetAccountEmail(string accountName)
    {
        var dir = GetAccountDirectory(accountName);
        var googleAccountsFile = Path.Combine(dir, "google_accounts.json");
        if (File.Exists(googleAccountsFile))
        {
            try
            {
                var json = File.ReadAllText(googleAccountsFile);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("activeAccount", out var acc) && acc.ValueKind == JsonValueKind.String)
                {
                    return acc.GetString();
                }
            }
            catch { }
        }
        return null;
    }

    public static AccountMetadata GetAccountMetadata(string accountName)
    {
        var dir = GetAccountDirectory(accountName);
        var metaFile = Path.Combine(dir, "account_metadata.json");
        if (File.Exists(metaFile))
        {
            try
            {
                var json = File.ReadAllText(metaFile);
                var meta = JsonSerializer.Deserialize<AccountMetadata>(json);
                if (meta != null) return meta;
            }
            catch { }
        }
        return new AccountMetadata();
    }

    public static void UpdateAccountMetadata(string accountName)
    {
        try
        {
            var dir = GetAccountDirectory(accountName);
            Directory.CreateDirectory(dir);
            var metaFile = Path.Combine(dir, "account_metadata.json");
            var meta = GetAccountMetadata(accountName);
            meta.LastUsed = Clock.GetLocalNow().ToString("yyyy-MM-ddTHH:mm:sszzz");
            meta.UsageCount++;

            var now = Clock.GetUtcNow().UtcDateTime;
            var cutoffWeekly = now.AddDays(-7);
            var history = meta.RequestHistory
                .Select(ts => DateTime.TryParse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt) ? dt : (DateTime?)null)
                .Where(dt => dt.HasValue && dt.Value >= cutoffWeekly)
                .Select(dt => dt!.Value.ToString("yyyy-MM-ddTHH:mm:sszzz"))
                .ToList();
            meta.RequestHistory = history;

            var json = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(metaFile, json);
        }
        catch { }
    }

    public static void SetAccountQuotaExceeded(string accountName, bool exceeded)
    {
        try
        {
            var dir = GetAccountDirectory(accountName);
            Directory.CreateDirectory(dir);
            var metaFile = Path.Combine(dir, "account_metadata.json");
            var meta = GetAccountMetadata(accountName);
            meta.QuotaStatus = exceeded ? "Exceeded" : "OK";
            var json = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(metaFile, json);
        }
        catch { }
    }

    public static bool IsNoAutoCommitEnabled()
    {
        var file = Path.Combine(AgySourceHome, "no_auto_commit_enabled.txt");
        if (!File.Exists(file)) return false;
        try { return File.ReadAllText(file).Trim() == "True"; }
        catch { return false; }
    }

    public static bool ToggleNoAutoCommit()
    {
        var current = IsNoAutoCommitEnabled();
        var next = !current;
        try
        {
            Directory.CreateDirectory(AgySourceHome);
            File.WriteAllText(Path.Combine(AgySourceHome, "no_auto_commit_enabled.txt"), next ? "True" : "False", Encoding.UTF8);
            SpectrePanel.Info($"No-Auto-Commit mode is now: {(next ? "Enabled" : "Disabled")}");
        }
        catch
        {
            SpectrePanel.Error("Failed to update No-Auto-Commit setting.");
        }
        return next;
    }

    // Direct delegation facade calls to DI services
    public static void BackupActiveToken(string accountName) => Bootstrapper.ServiceProvider.GetRequiredService<IAgyVault>().BackupActiveToken(accountName);
    public static void RestoreActiveToken(string accountName) => Bootstrapper.ServiceProvider.GetRequiredService<IAgyVault>().RestoreActiveToken(accountName);
    public static void SyncActiveAccountWithKeyring(bool silent = false) => Bootstrapper.ServiceProvider.GetRequiredService<IAgyVault>().SyncActiveAccountWithKeyring(silent);

    public static QuotaMetrics CalculateRollingQuotas(string accountName) => Bootstrapper.ServiceProvider.GetRequiredService<IAgyQuotaEngine>().CalculateRollingQuotas(accountName);
    public static QuotaMetrics CalculateRollingQuotasForAgent(string agentName) => Bootstrapper.ServiceProvider.GetRequiredService<IAgyQuotaEngine>().CalculateRollingQuotasForAgent(agentName);

    public static AccountStats GetAccountStats(string accountName) => Bootstrapper.ServiceProvider.GetRequiredService<IAgyQuotaEngine>().GetAccountStats(accountName);
    public static void ClearStatsCache() => Bootstrapper.ServiceProvider.GetRequiredService<IAgyQuotaEngine>().ClearStatsCache();

    public static void SetActiveAccount(string accountName, bool temporary = false) => Bootstrapper.ServiceProvider.GetRequiredService<IAgyAccountStore>().SetActiveAccount(accountName, temporary);
    public static void AddAccount(string accountName) => Bootstrapper.ServiceProvider.GetRequiredService<IAgyAccountStore>().AddAccount(accountName);
    public static void DeleteAccount(string accountName) => Bootstrapper.ServiceProvider.GetRequiredService<IAgyAccountStore>().DeleteAccount(accountName);
    public static void LogoutAccount(string accountName) => Bootstrapper.ServiceProvider.GetRequiredService<IAgyAccountStore>().LogoutAccount(accountName);
    public static bool IsAutoSwitchEnabled() => Bootstrapper.ServiceProvider.GetRequiredService<IAgyAccountStore>().IsAutoSwitchEnabled();
    public static void ToggleAutoSwitch() => Bootstrapper.ServiceProvider.GetRequiredService<IAgyAccountStore>().ToggleAutoSwitch();
    public static void AutoSwitchOnQuotaExceeded() => Bootstrapper.ServiceProvider.GetRequiredService<IAgyAccountStore>().AutoSwitchOnQuotaExceeded();
    public static void ShowAllAccountsSummary() => AgyTui.UI.Core.Navigation.AgyAccountDisplay.ShowAccountTree();
}
