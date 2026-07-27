namespace AgyTui.Infrastructure.Integrations.AgyClient;

using AgyTui.Infrastructure.Integrations.AgyClient.Interfaces;
using AgyTui.Infrastructure.Persistence;
using AgyTui.Infrastructure.Persistence.Interfaces;
using System.Text;

public class AgyAccountStore : IAgyAccountStore
{
    private readonly IAgyAccountRepository _accountRepo;
    private string AgySourceHome => AgyAccountCore.AgySourceHome;

    private IEnumerable<string> GetActiveAccountFileCandidates()
    {
        var list = new List<string>
        {
            Path.Combine(AgySourceHome, "active_account.txt"),
            Path.Combine(AgySourceHome, "active_account")
        };

        var projectGemini = AppPaths.GeminiHome;
        list.Add(Path.Combine(projectGemini, "active_account.txt"));
        list.Add(Path.Combine(projectGemini, "active_account"));

        return list.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    public AgyAccountStore(IAgyAccountRepository accountRepo)
    {
        _accountRepo = accountRepo;
    }

    public AgyAccountStore() : this(new SqliteAgyAccountRepository(new SqliteDatabase())) { }

    public string GetActiveAccount()
    {
        var dbActive = _accountRepo.GetActiveAccount();
        if (!string.IsNullOrEmpty(dbActive)) return dbActive;

        foreach (var file in GetActiveAccountFileCandidates())
        {
            if (File.Exists(file))
            {
                try
                {
                    var acc = File.ReadAllText(file).Trim();
                    if (!string.IsNullOrEmpty(acc)) return acc;
                }
                catch { }
            }
        }
        return "default";
    }

    public void SetActiveAccount(string accountName) => SetActiveAccount(accountName, false);

    public void SetActiveAccount(string accountName, bool temporary = false)
    {
        if (!string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase))
        {
            var targetDir = AgyAccountCore.GetAccountDirectory(accountName);
            if (!Directory.Exists(targetDir))
            {
                throw new ArgumentException($"Account '{accountName}' does not exist.");
            }
        }

        AgyAccountCore.ClearStatsCache();
        AgyAccountCore.UpdateAccountMetadata(accountName);
        AgyAccountCore.BackupActiveToken(AgyAccountCore.GetActiveAccount());
        if (!temporary) _accountRepo.SetActiveAccount(accountName);

        if (string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase))
        {
            Environment.SetEnvironmentVariable("GEMINI_HOME", AgySourceHome);
            if (!temporary)
            {
                try
                {
                    Directory.CreateDirectory(AgySourceHome);
                    File.WriteAllText(AgyAccountCore.AgyActiveAccountFile, "default", Encoding.UTF8);
                }
                catch { }
            }
            AgyAccountCore.RestoreActiveToken("default");
            SpectrePanel.Success("Switched to default Antigravity account (Primary).");
            return;
        }

        var targetDirLoc = AgyAccountCore.GetAccountDirectory(accountName);
        var defaultIdFile = Path.Combine(AgySourceHome, "installation_id");
        var targetIdFile = Path.Combine(targetDirLoc, "installation_id");
        string defaultId = "";
        if (File.Exists(defaultIdFile)) defaultId = File.ReadAllText(defaultIdFile).Trim();
        string targetId = "";
        if (File.Exists(targetIdFile)) targetId = File.ReadAllText(targetIdFile).Trim();

        if (string.IsNullOrWhiteSpace(targetId) || targetId == defaultId)
        {
            try
            {
                Directory.CreateDirectory(targetDirLoc);
                var newId = Guid.NewGuid().ToString();
                File.WriteAllText(targetIdFile, newId);
                SpectrePanel.Warning($"Re-generated unique installation ID for '{accountName}' to separate credentials.");
            }
            catch { }
        }

        Environment.SetEnvironmentVariable("GEMINI_HOME", targetDirLoc);
        AgyAccountCore.RestoreActiveToken(accountName);

        if (!temporary)
        {
            try
            {
                foreach (var file in GetActiveAccountFileCandidates())
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(file);
                        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                        File.WriteAllText(file, accountName);
                    }
                    catch { }
                }
                SpectrePanel.Success($"Switched to account '{accountName}' (Persistent).");
            }
            catch
            {
                SpectrePanel.Error("Failed to update active account file.");
            }
        }
        else
        {
            SpectrePanel.Warning($"Switched to account '{accountName}' (Temporary - current session only).");
        }
    }

    public string[] GetAccounts() => AgyAccountCore.GetAccounts();

    public void AddAccount(string accountName)
    {
        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("Account name cannot be empty.");

        if (string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Cannot create an account named 'default'.");

        var destDir = AgyAccountCore.GetAccountDirectory(accountName);
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
        AgyAccountCore.ClearStatsCache();
    }

    public void DeleteAccount(string accountName)
    {
        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("Account name cannot be empty.");

        var targetDir = AgyAccountCore.GetAccountDirectory(accountName);
        if (!Directory.Exists(targetDir))
            throw new DirectoryNotFoundException($"Account '{accountName}' does not exist.");

        bool wasActive = string.Equals(GetActiveAccount(), accountName, StringComparison.OrdinalIgnoreCase);

        if (wasActive)
        {
            AgyKeyringHelper.DeleteToken("gemini:antigravity");
            try
            {
                Directory.CreateDirectory(AgySourceHome);
                File.WriteAllText(AgyAccountCore.AgyActiveAccountFile, "default", Encoding.UTF8);
            }
            catch { }
        }

        Directory.Delete(targetDir, true);

        if (wasActive)
        {
            SetActiveAccount("default", false);
        }
        AgyAccountCore.ClearStatsCache();
    }

    public void LogoutAccount(string accountName)
    {
        if (string.Equals(GetActiveAccount(), accountName, StringComparison.OrdinalIgnoreCase))
        {
            AgyKeyringHelper.DeleteToken("gemini:antigravity");
        }
        var dir = AgyAccountCore.GetAccountDirectory(accountName);
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
        var activeMeta = AgyAccountCore.GetAccountMetadata(active);
        if (!string.Equals(activeMeta.QuotaStatus, "Exceeded", StringComparison.OrdinalIgnoreCase)) return null;
        foreach (var acc in GetAccounts())
        {
            if (string.Equals(acc, active, StringComparison.OrdinalIgnoreCase)) continue;
            var tokenFile = Path.Combine(AgyAccountCore.GetAccountDirectory(acc), "keyring_token.txt");
            if (!File.Exists(tokenFile)) continue;
            var quota = AgyAccountCore.GetAccountMetadata(acc).QuotaStatus ?? "OK";
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
