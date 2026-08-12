using System.Text.Json;
using System.Text.RegularExpressions;

namespace AgyTui.Infrastructure.Integrations.AgyClient;

public class AgyAccountStore : IAgyAccountStore
{
    private readonly IAgyAccountRepository _accountRepo;
    private readonly IAppPathManager _pathManager;

    public string AgySourceHome => _pathManager.GeminiHome;
    public string AgyAccountPrefix => _pathManager.AccountPrefix;

    public AgyAccountStore(
        IAgyAccountRepository accountRepo,
        IAppPathManager pathManager)
    {
        _accountRepo = accountRepo;
        _pathManager = pathManager;
    }

    public AgyAccountStore(IAgyAccountRepository accountRepo)
        : this(accountRepo, new Services.AppPathManager()) { }

    public AgyAccountStore()
        : this(new Persistence.Repositories.SqliteAgyAccountRepository(new Persistence.DbContext.SqliteDatabase()), new Services.AppPathManager()) { }

    public string GetAccountDirectory(string accountName) => _pathManager.GetAccountDirectory(accountName);

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
                    var emailStr = acc.GetString();
                    if (!string.IsNullOrEmpty(emailStr)) return emailStr;
                }
            }
            catch { }
        }

        try
        {
            var dbCreds = _accountRepo.GetAccountCredentials(accountName);
            if (dbCreds != null && !string.IsNullOrEmpty(dbCreds.Email))
            {
                return dbCreds.Email;
            }
        }
        catch { }

        return null;
    }

    public string GetShortCredentialSignature(string accountName)
    {
        try
        {
            var creds = _accountRepo.GetAccountCredentials(accountName);
            var rawToken = creds?.KeyringToken;

            if (string.IsNullOrEmpty(rawToken))
            {
                var tokenFile = Path.Combine(GetAccountDirectory(accountName), "keyring_token.txt");
                if (File.Exists(tokenFile))
                {
                    rawToken = File.ReadAllText(tokenFile).Trim();
                }
            }

            if (string.IsNullOrEmpty(rawToken) && string.Equals(accountName, GetActiveAccount(), StringComparison.OrdinalIgnoreCase))
            {
                rawToken = AgyKeyringHelper.ReadToken("gemini:antigravity");
            }

            if (string.IsNullOrEmpty(rawToken)) return "None";

            var clean = rawToken.Trim();
            string plainToken = clean;

            try
            {
                var vault = new AgyVault(this);
                var decrypted = vault.Unprotect(clean);
                if (!string.IsNullOrWhiteSpace(decrypted))
                {
                    plainToken = decrypted.Trim();
                }
            }
            catch { }

            if (plainToken.StartsWith("{") && plainToken.EndsWith("}"))
            {
                try
                {
                    using var doc = JsonDocument.Parse(plainToken);
                    if (doc.RootElement.TryGetProperty("access_token", out var at) && at.ValueKind == JsonValueKind.String)
                    {
                        plainToken = at.GetString() ?? plainToken;
                    }
                    else if (doc.RootElement.TryGetProperty("token", out var tk) && tk.ValueKind == JsonValueKind.String)
                    {
                        plainToken = tk.GetString() ?? plainToken;
                    }
                }
                catch { }
            }

            if (plainToken.Length <= 6) return plainToken;

            var headLength = plainToken.StartsWith("ya29") ? 4 : (plainToken.StartsWith("AIza") ? 4 : 3);
            if (headLength >= plainToken.Length - 3) headLength = 2;

            var head = plainToken[..headLength];
            var tail = plainToken[^3..];
            return $"{head}..{tail}";
        }
        catch
        {
            return "None";
        }
    }

    public AccountMetadata GetAccountMetadata(string accountName)
    {
        return _accountRepo.GetAccountMetadata(accountName);
    }

    public AccountAggregate GetAccountAggregate(string accountName)
    {
        var meta = GetAccountMetadata(accountName);
        var email = GetAccountEmail(accountName);
        var active = string.Equals(GetActiveAccount(), accountName, StringComparison.OrdinalIgnoreCase);
        return AccountAggregate.FromMetadata(accountName, meta, email, active);
    }

    public void SaveAccountAggregate(AccountAggregate aggregate)
    {
        _accountRepo.SaveAccountMetadata(aggregate.AccountName, aggregate.ToMetadata());
    }

    public void UpdateAccountMetadata(string accountName)
    {
        try
        {
            var agg = GetAccountAggregate(accountName);
            agg.RecordUsage(DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"));
            SaveAccountAggregate(agg);
        }
        catch { }
    }

    public void SetAccountQuotaExceeded(string accountName, bool exceeded)
    {
        try
        {
            var agg = GetAccountAggregate(accountName);
            agg.SetQuotaExceeded(exceeded);
            SaveAccountAggregate(agg);
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
        var accounts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "default" };

        try
        {
            var dbAccs = _accountRepo.GetAccounts();
            foreach (var dbAcc in dbAccs)
            {
                if (!string.IsNullOrWhiteSpace(dbAcc))
                    accounts.Add(dbAcc);
            }
        }
        catch { }

        var scanPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var userProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? "";
        if (Directory.Exists(userProfile)) scanPaths.Add(userProfile);
        var prefixParent = Path.GetDirectoryName(AgyAccountPrefix);
        if (prefixParent != null && Directory.Exists(prefixParent)) scanPaths.Add(prefixParent);

        foreach (var scanPath in scanPaths)
        {
            foreach (var dir in Directory.GetDirectories(scanPath, ".gemini_*"))
            {
                var m = Regex.Match(Path.GetFileName(dir), @"^\.gemini_(.+)$");
                if (!m.Success) continue;
                var name = m.Groups[1].Value;
                if (Regex.IsMatch(name, @"^(backup|copy|temp|test|testacc)([_-]|$)", RegexOptions.IgnoreCase)) continue;

                accounts.Add(name);
            }
        }

        return accounts.OrderBy(a => a, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public string GetActiveAccount()
    {
        var envGemini = Environment.GetEnvironmentVariable("GEMINI_HOME");
        if (string.IsNullOrEmpty(envGemini))
        {
            try
            {
                envGemini = Environment.GetEnvironmentVariable("GEMINI_HOME", EnvironmentVariableTarget.User);
            }
            catch { }
        }

        if (!string.IsNullOrEmpty(envGemini) && Directory.Exists(envGemini))
        {
            var folderName = Path.GetFileName(envGemini.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (folderName.StartsWith(".gemini_", StringComparison.OrdinalIgnoreCase))
            {
                return folderName[8..];
            }
            if (string.Equals(folderName, ".gemini", StringComparison.OrdinalIgnoreCase))
            {
                return "default";
            }
        }

        try
        {
            var userProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? "";
            if (!string.IsNullOrEmpty(userProfile))
            {
                var activeFile = Path.Combine(userProfile, ".gemini", "active_account.txt");
                if (File.Exists(activeFile))
                {
                    var name = File.ReadAllText(activeFile).Trim();
                    if (!string.IsNullOrEmpty(name))
                    {
                        return name;
                    }
                }
            }
        }
        catch { }

        var dbActive = _accountRepo.GetActiveAccount();
        if (!string.IsNullOrEmpty(dbActive)) return dbActive;

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
                throw new Domain.Exceptions.AccountNotFoundException(accountName);
            }
        }

        var oldActiveAcc = GetActiveAccount();

        (new AgyQuotaEngine(this)).ClearStatsCache();
        UpdateAccountMetadata(accountName);
        (new AgyVault(this, _accountRepo)).BackupActiveToken(oldActiveAcc);

        var targetDirLoc = GetAccountDirectory(accountName);
        Environment.SetEnvironmentVariable("GEMINI_HOME", targetDirLoc);
        try
        {
            Environment.SetEnvironmentVariable("GEMINI_HOME", targetDirLoc, EnvironmentVariableTarget.User);
        }
        catch { }

        if (!temporary)
        {
            try
            {
                var userProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? "";
                if (!string.IsNullOrEmpty(userProfile))
                {
                    var rootGemini = Path.Combine(userProfile, ".gemini");
                    Directory.CreateDirectory(rootGemini);
                    File.WriteAllText(Path.Combine(rootGemini, "active_account.txt"), accountName, Encoding.UTF8);
                }
            }
            catch { }

            _accountRepo.SetActiveAccount(accountName);
            var activeAgg = GetAccountAggregate(accountName);
            activeAgg.MarkActive();
            SaveAccountAggregate(activeAgg);
        }

        (new AgyVault(this, _accountRepo)).RestoreActiveToken(accountName);

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
        (new AgyQuotaEngine(this)).ClearStatsCache();
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
        (new AgyQuotaEngine(this)).ClearStatsCache();
    }

    public void LogoutAccount(string accountName)
    {
        if (string.Equals(GetActiveAccount(), accountName, StringComparison.OrdinalIgnoreCase))
        {
            AgyKeyringHelper.DeleteToken("gemini:antigravity");
        }
        var dir = GetAccountDirectory(accountName);
        if (Directory.Exists(dir))
        {
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
        _accountRepo.SaveAccountCredentials(new AccountCredentials(accountName, null, null, null, null, null));
        (new AgyQuotaEngine(this)).ClearStatsCache();
    }

    public void AuthenticateAccount(string accountName)
    {
        SetActiveAccount(accountName, false);
        var targetDir = GetAccountDirectory(accountName);
        Environment.SetEnvironmentVariable("GEMINI_HOME", targetDir);

        var agyExe = Helpers.ProcessRunner.Instance.FindOnPath("agy") ?? Helpers.ProcessRunner.Instance.FindOnPath("antigravity");
        if (!string.IsNullOrEmpty(agyExe))
        {
            SpectrePanel.Info($"Launching OAuth login for '{accountName}' via '{agyExe}'...");
            Helpers.ProcessRunner.Instance.RunInteractive(agyExe, ["auth", "login"], new Dictionary<string, string?> { ["GEMINI_HOME"] = targetDir }, targetDir);
        }
        else
        {
            SpectrePanel.Info($"Launching OAuth login for '{accountName}'...");
            Helpers.ProcessRunner.Instance.RunInteractive("pwsh", ["-NoProfile", "-Command", $"$env:GEMINI_HOME='{targetDir}'; agy auth login"], null, targetDir);
        }
        (new AgyVault(this, _accountRepo)).BackupActiveToken(accountName);
        (new AgyQuotaEngine(this)).ClearStatsCache();
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
        (new AgyQuotaEngine(this)).ClearStatsCache();
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

