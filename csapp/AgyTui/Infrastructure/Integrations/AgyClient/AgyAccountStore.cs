namespace AgyTui.Infrastructure.Integrations.AgyClient;

public class AgyAccountStore : IAgyAccountStore
{
    private readonly IAgyAccountRepository _accountRepo;
    private string AgySourceHome => AgyAccountCore.AgySourceHome;

    public AgyAccountStore(IAgyAccountRepository accountRepo)
    {
        _accountRepo = accountRepo;
    }

    public AgyAccountStore() : this(new SqliteAgyAccountRepository(new SqliteDatabase())) { }

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
            var targetDir = AgyAccountCore.GetAccountDirectory(accountName);
            if (!Directory.Exists(targetDir))
            {
                throw new ArgumentException($"Account '{accountName}' does not exist.");
            }
        }

        AgyAccountCore.ClearStatsCache();
        AgyAccountCore.UpdateAccountMetadata(accountName);
        AgyAccountCore.BackupActiveToken(GetActiveAccount());

        if (!temporary)
        {
            _accountRepo.SetActiveAccount(accountName);
        }

        var targetDirLoc = AgyAccountCore.GetAccountDirectory(accountName);
        Environment.SetEnvironmentVariable("GEMINI_HOME", targetDirLoc);
        AgyAccountCore.RestoreActiveToken(accountName);

        if (!temporary)
        {
            SpectrePanel.Success($"Switched active account context to '{accountName}' (Persistent SQLite DB).");
        }
        else
        {
            SpectrePanel.Warning($"Switched to account '{accountName}' (Temporary session).");
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

    public void AuthenticateAccount(string accountName)
    {
        SetActiveAccount(accountName, false);
        var targetDir = AgyAccountCore.GetAccountDirectory(accountName);
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
        AgyAccountCore.ClearStatsCache();
    }

    public void PurgeAllNonDefaultAccounts()
    {
        var userProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? "";
        var publicDir = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\", "Users", "Public");
        var prefixParent = Path.GetDirectoryName(AgyAccountCore.AgyAccountPrefix);

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
        AgyAccountCore.ClearStatsCache();
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
