using System.Text.Json;
using System.Text.RegularExpressions;
using AgyTui.Infrastructure.Di;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Infrastructure.Integrations.AgyClient;

public class AgyAccountStore : IAgyAccountStore
{
    public string AgySourceHome
    {
        get
        {
            var cfgHome = Config.Current.System.AgySourceHome;
            if (!string.IsNullOrEmpty(cfgHome)) return cfgHome;
            return AppPaths.GeminiHome;
        }
    }

    public string AgyAccountPrefix
    {
        get
        {
            var publicDir = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\", "Users", "Public");
            if (AgySourceHome.StartsWith(publicDir, StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(publicDir, ".gemini_");
            }

            var accountsDir = Path.Combine(AppPaths.DataDir, ".gemini_");
            Directory.CreateDirectory(Path.GetDirectoryName(accountsDir)!);
            return accountsDir;
        }
    }

    
    private readonly IAgyAccountRepository _accountRepo;

    private readonly Func<IAgyQuotaEngine> _quotaEngineFactory;
    private readonly Func<IAgyVault> _vaultFactory;

    public AgyAccountStore(
        IAgyAccountRepository accountRepo,
        Func<IAgyQuotaEngine>? quotaEngineFactory = null,
        Func<IAgyVault>? vaultFactory = null)
    {
        _accountRepo = accountRepo;
        _quotaEngineFactory = quotaEngineFactory ??
                              (() => Bootstrapper.ServiceProvider.GetRequiredService<IAgyQuotaEngine>());
        _vaultFactory = vaultFactory ?? (() => Bootstrapper.ServiceProvider.GetRequiredService<IAgyVault>());
    }

    public AgyAccountStore() : this(new SqliteAgyAccountRepository(new SqliteDatabase())) { }

    public string GetAccountDirectory(string accountName)
    {
        if (string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase))
            return AgySourceHome;

        return $"{AgyAccountPrefix}{accountName}";
    }

    public string? GetAccountEmail(string accountName)
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

    public AccountMetadata GetAccountMetadata(string accountName)
    {
        return _accountRepo.GetAccountMetadata(accountName);
    }

    public void UpdateAccountMetadata(string accountName)
    {
        try
        {
            var meta = GetAccountMetadata(accountName);
            meta.LastUsed = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
            meta.UsageCount++;

            var now = DateTime.UtcNow;
            var cutoffWeekly = now.AddDays(-7);
            var history = meta.RequestHistory
                .Select(ts => DateTime.TryParse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt) ? dt : (DateTime?)null)
                .Where(dt => dt.HasValue && dt.Value >= cutoffWeekly)
                .Select(dt => dt!.Value.ToString("yyyy-MM-ddTHH:mm:sszzz"))
                .ToList();
            meta.RequestHistory = history;

            _accountRepo.SaveAccountMetadata(accountName, meta);
        }
        catch { }
    }

    public void SetAccountQuotaExceeded(string accountName, bool exceeded)
    {
        try
        {
            var meta = GetAccountMetadata(accountName);
            meta.QuotaStatus = exceeded ? "Exceeded" : "OK";
            _accountRepo.SaveAccountMetadata(accountName, meta);
        }
        catch { }
    }

    public bool IsNoAutoCommitEnabled()
    {
        var file = Path.Combine(AgySourceHome, "no_auto_commit_enabled.txt");
        if (!File.Exists(file)) return false;
        try { return File.ReadAllText(file).Trim() == "True"; }
        catch { return false; }
    }

    public bool ToggleNoAutoCommit()
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

    public string[] GetAccounts()
    {
        var accounts = new List<string> { "default" };
        var scanPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var userProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? "";
        if (Directory.Exists(userProfile)) scanPaths.Add(userProfile);
        var publicDir = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\", "Users", "Public");
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
                if (!Regex.IsMatch(name, @"^(backup|copy|temp|test|testacc)([_-]|$)", RegexOptions.IgnoreCase) && !accounts.Contains(name, StringComparer.OrdinalIgnoreCase)) accounts.Add(name);
            }
        }
        return [.. accounts];
    }
    public string GetActiveAccount()
    {
        var dbActive = _accountRepo.GetActiveAccount();
        if (!string.IsNullOrEmpty(dbActive)) return dbActive;

        var envGemini = Environment.GetEnvironmentVariable("GEMINI_HOME");
        if (!string.IsNullOrEmpty(envGemini) && Directory.Exists(envGemini))
        {
            var folderName = Path.GetFileName(envGemini);
            if (folderName.StartsWith(".gemini_"))
            {
                return folderName[8..];
            }
        }
        return "default";
    }

    public void SetActiveAccount(string accountName) => SetActiveAccount(accountName, false);

    public void SetActiveAccount(string accountName, bool temporary = false)
    {
        if (!string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase))
        {
            var targetDir = GetAccountDirectory(accountName);
            if (!Directory.Exists(targetDir))
            {
                throw new ArgumentException($"Account '{accountName}' does not exist.");
            }
        }

        _quotaEngineFactory().ClearStatsCache();
        UpdateAccountMetadata(accountName);
        _vaultFactory().BackupActiveToken(GetActiveAccount());

        if (!temporary)
        {
            _accountRepo.SetActiveAccount(accountName);
        }

        var targetDirLoc = GetAccountDirectory(accountName);
        Environment.SetEnvironmentVariable("GEMINI_HOME", targetDirLoc);
        _vaultFactory().RestoreActiveToken(accountName);

        if (!temporary)
        {
            SpectrePanel.Success($"Switched active account context to '{accountName}' (Persistent SQLite DB).");
        }
        else
        {
            SpectrePanel.Warning($"Switched to account '{accountName}' (Temporary session).");
        }
    }

    public void AddAccount(string accountName)
    {
        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("Account name cannot be empty.");

        if (string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Cannot create an account named 'default'.");

        var destDir = GetAccountDirectory(accountName);
        if (Directory.Exists(destDir))
            throw new InvalidOperationException($"Account '{accountName}' already exists.");

        Directory.CreateDirectory(destDir);

        var credentialsFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "google_accounts.json", "oauth_creds.json", "state.json", "installation_id", "keyring_token.txt"
        };

        if (Directory.Exists(AgySourceHome))
        {
            foreach (var file in Directory.GetFiles(AgySourceHome))
            {
                var fileName = Path.GetFileName(file);
                if (!credentialsFiles.Contains(fileName))
                {
                    try
                    {
                        File.Copy(file, Path.Combine(destDir, fileName), true);
                    }
                    catch { }
                }
            }
        }

        var installationIdFile = Path.Combine(destDir, "installation_id");
        File.WriteAllText(installationIdFile, Guid.NewGuid().ToString());

        var subDirs = new[] { "antigravity", "antigravity-cli", "config", "history", "antigravity-ide", "wf", "learn" };
        foreach (var sub in subDirs)
        {
            Directory.CreateDirectory(Path.Combine(destDir, sub));
        }
        _quotaEngineFactory().ClearStatsCache();
    }

    public void DeleteAccount(string accountName)
    {
        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("Account name cannot be empty.");

        var targetDir = GetAccountDirectory(accountName);
        if (!Directory.Exists(targetDir))
            throw new DirectoryNotFoundException($"Account '{accountName}' does not exist.");

        bool wasActive = string.Equals(GetActiveAccount(), accountName, StringComparison.OrdinalIgnoreCase);

        if (wasActive)
        {
            AgyKeyringHelper.DeleteToken("gemini:antigravity");
        }

        Directory.Delete(targetDir, true);

        if (wasActive)
        {
            SetActiveAccount("default", false);
        }
        _quotaEngineFactory().ClearStatsCache();
    }

    public void LogoutAccount(string accountName)
    {
        if (string.Equals(GetActiveAccount(), accountName, StringComparison.OrdinalIgnoreCase))
        {
            AgyKeyringHelper.DeleteToken("gemini:antigravity");
        }
        var dir = GetAccountDirectory(accountName);
        if (!Directory.Exists(dir)) return;

        var files = new[] { "google_accounts.json", "oauth_creds.json", "state.json", "keyring_token.txt" };
        foreach (var f in files)
        {
            var p = Path.Combine(dir, f);
            if (File.Exists(p))
            {
                try { File.Delete(p); } catch { }
            }
        }
    }

    public void AuthenticateAccount(string accountName)
    {
        SetActiveAccount(accountName, false);
        var targetDir = GetAccountDirectory(accountName);
        Environment.SetEnvironmentVariable("GEMINI_HOME", targetDir);

        var agyExe = Helpers.ProcessRunner.FindOnPath("agy") ?? Helpers.ProcessRunner.FindOnPath("antigravity");
        if (!string.IsNullOrEmpty(agyExe))
        {
            SpectrePanel.Info($"Launching OAuth login for '{accountName}' via '{agyExe}'...");
            Helpers.ProcessRunner.RunInteractive(agyExe, ["auth", "login"], new Dictionary<string, string?> { ["GEMINI_HOME"] = targetDir }, targetDir);
        }
        else
        {
            SpectrePanel.Info($"Launching OAuth login for '{accountName}'...");
            Helpers.ProcessRunner.RunInteractive("pwsh", ["-NoProfile", "-Command", $"$env:GEMINI_HOME='{targetDir}'; agy auth login"], null, targetDir);
        }
        _quotaEngineFactory().ClearStatsCache();
    }

    public void PurgeAllNonDefaultAccounts()
    {
        var userProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? "";
        var publicDir = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\", "Users", "Public");
        var prefixParent = Path.GetDirectoryName(AgyAccountPrefix);

        var scanPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (Directory.Exists(userProfile)) scanPaths.Add(userProfile);
        if (Directory.Exists(publicDir)) scanPaths.Add(publicDir);
        if (prefixParent != null && Directory.Exists(prefixParent)) scanPaths.Add(prefixParent);

        foreach (var path in scanPaths)
        {
            try
            {
                foreach (var dir in Directory.GetDirectories(path, ".gemini_*"))
                {
                    try { Directory.Delete(dir, true); } catch { }
                }
            }
            catch { }
        }

        LogoutAccount("default");
        SetActiveAccount("default", false);
        _quotaEngineFactory().ClearStatsCache();
    }

    public bool IsAutoSwitchEnabled()
    {
        var file = Path.Combine(AgySourceHome, "auto_switch_enabled.txt");
        if (!File.Exists(file)) return true;

        try
        {
            return File.ReadAllText(file).Trim() != "False";
        }
        catch
        {
            return true;
        }
    }

    public void ToggleAutoSwitch()
    {
        var current = IsAutoSwitchEnabled();
        try
        {
            Directory.CreateDirectory(AgySourceHome);
            File.WriteAllText(Path.Combine(AgySourceHome, "auto_switch_enabled.txt"), current ? "False" : "True", Encoding.UTF8);
            SpectrePanel.Info($"Auto-Switch is now: {(current ? "Disabled" : "Enabled")}");
        }
        catch
        {
            SpectrePanel.Error("Failed to update Auto-Switch setting.");
        }
    }

    public string? FindAutoSwitchCandidate()
    {
        if (!IsAutoSwitchEnabled()) return null;
        var active = GetActiveAccount();
        var activeMeta = GetAccountMetadata(active);
        if (!string.Equals(activeMeta.QuotaStatus, "Exceeded", StringComparison.OrdinalIgnoreCase)) return null;
        foreach (var acc in GetAccounts())
        {
            if (string.Equals(acc, active, StringComparison.OrdinalIgnoreCase)) continue;
            var tokenFile = Path.Combine(GetAccountDirectory(acc), "keyring_token.txt");
            if (!File.Exists(tokenFile)) continue;
            var quota = GetAccountMetadata(acc).QuotaStatus ?? "OK";
            if (string.Equals(quota, "OK", StringComparison.OrdinalIgnoreCase)) return acc;
        }
        return null;
    }

    public void AutoSwitchOnQuotaExceeded()
    {
        if (!IsAutoSwitchEnabled()) return;
        var active = GetActiveAccount();
        var candidate = FindAutoSwitchCandidate();
        if (candidate != null)
        {
            SpectrePanel.Warning($"Active account '{active}' exceeded quota. Auto-switching to candidate account '{candidate}' with available quota.");
            SetActiveAccount(candidate, false);
        }
    }
}
