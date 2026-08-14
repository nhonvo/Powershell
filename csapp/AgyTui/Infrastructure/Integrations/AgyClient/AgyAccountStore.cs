using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AgyTui.Infrastructure.Integrations.AgyClient;

public class AgyAccountStore : IAgyAccountStore
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
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

    public string GetCanonicalEmail(string accountName)
    {
        if (string.IsNullOrWhiteSpace(accountName)) return "default@gmail.com";
        var name = accountName.Trim();
        if (name.Contains("@")) return name.ToLowerInvariant();

        try
        {
            var dbCreds = _accountRepo.GetAccountCredentials(name);
            if (dbCreds != null && !string.IsNullOrEmpty(dbCreds.Email) && dbCreds.Email.Contains("@"))
            {
                var dbEmail = dbCreds.Email.Trim().ToLowerInvariant();
                if (dbEmail.StartsWith(name.ToLowerInvariant()) || dbEmail.Contains(name.ToLowerInvariant()))
                {
                    return dbEmail;
                }
            }
        }
        catch { }

        if (!string.Equals(name, "default", StringComparison.OrdinalIgnoreCase))
        {
            return $"{name.ToLowerInvariant()}@gmail.com";
        }

        return "default@gmail.com";
    }

    public void SanitizeAccountDirectory(string accountName)
    {
        if (string.IsNullOrWhiteSpace(accountName)) return;
        var dir = GetAccountDirectory(accountName);
        if (!Directory.Exists(dir)) return;

        var expectedEmail = GetCanonicalEmail(accountName);
        var googleAccountsFile = Path.Combine(dir, "google_accounts.json");
        bool needsSanitization = false;

        if (File.Exists(googleAccountsFile))
        {
            try
            {
                var json = File.ReadAllText(googleAccountsFile);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("activeAccount", out var acc) && acc.ValueKind == JsonValueKind.String)
                {
                    var activeEmail = acc.GetString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(activeEmail) && !string.Equals(activeEmail, expectedEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        needsSanitization = true;
                    }
                }
            }
            catch
            {
                needsSanitization = true;
            }
        }
        else
        {
            needsSanitization = true;
        }

        if (needsSanitization)
        {
            var filesToDelete = new[] { "google_accounts.json", "oauth_creds.json", "state.json", "keyring_token.txt" };
            foreach (var f in filesToDelete)
            {
                var p = Path.Combine(dir, f);
                if (File.Exists(p)) { try { File.Delete(p); } catch { } }
            }

            var subKeyring = Path.Combine(dir, "antigravity-cli", "keyring_token.txt");
            if (File.Exists(subKeyring)) { try { File.Delete(subKeyring); } catch { } }

            var gObj = new
            {
                accounts = new[] { new { email = expectedEmail } },
                activeAccount = expectedEmail
            };
            var gJson = JsonSerializer.Serialize(gObj, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(googleAccountsFile, gJson, Utf8NoBom);

            var sObj = new
            {
                accountName = accountName,
                userEmail = expectedEmail
            };
            var sJson = JsonSerializer.Serialize(sObj, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(Path.Combine(dir, "antigravity-cli"));
            File.WriteAllText(Path.Combine(dir, "antigravity-cli", "settings.json"), sJson, Utf8NoBom);

            try
            {
                _accountRepo.SaveAccountCredentials(new AccountCredentials(accountName, null, gJson, null, null, expectedEmail));
            }
            catch { }
        }
    }

    public string? GetAccountEmail(string accountName)
    {
        if (string.IsNullOrWhiteSpace(accountName)) return null;
        SanitizeAccountDirectory(accountName);
        return GetCanonicalEmail(accountName);
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
            File.WriteAllText(Path.Combine(AgySourceHome, "no_auto_commit_enabled.txt"), next ? "True" : "False", Utf8NoBom);
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
            var dbActive = _accountRepo.GetActiveAccount();
            if (!string.IsNullOrEmpty(dbActive) && !string.Equals(dbActive, "default", StringComparison.OrdinalIgnoreCase))
            {
                return dbActive;
            }
        }
        catch { }

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

        var fallbackActive = _accountRepo.GetActiveAccount();
        if (!string.IsNullOrEmpty(fallbackActive)) return fallbackActive;

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
                Directory.CreateDirectory(targetDir);
                SanitizeAccountDirectory(accountName);
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
                    File.WriteAllText(Path.Combine(rootGemini, "active_account.txt"), accountName, Utf8NoBom);
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
        {
            DeleteDirectoryWithRetry(destDir);
        }

        Directory.CreateDirectory(destDir);

        var email = accountName.Contains("@") ? accountName : $"{accountName}@gmail.com";

        var gObj = new
        {
            accounts = new[] { new { email } },
            activeAccount = email
        };
        var gJson = JsonSerializer.Serialize(gObj, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(destDir, "google_accounts.json"), gJson, Utf8NoBom);

        var installationIdFile = Path.Combine(destDir, "installation_id");
        File.WriteAllText(installationIdFile, Guid.NewGuid().ToString(), Utf8NoBom);

        var subDirs = new[] { "antigravity", "antigravity-cli", "config", "history", "antigravity-ide", "wf", "learn" };
        foreach (var sub in subDirs)
        {
            Directory.CreateDirectory(Path.Combine(destDir, sub));
        }

        var sObj = new
        {
            accountName = accountName,
            userEmail = email
        };
        var sJson = JsonSerializer.Serialize(sObj, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(destDir, "antigravity-cli", "settings.json"), sJson, Utf8NoBom);

        try
        {
            _accountRepo.AddAccount(accountName, email);
            _accountRepo.SaveAccountCredentials(new AccountCredentials(accountName, null, gJson, null, null, email));
        }
        catch { }

        (new AgyQuotaEngine(this)).ClearStatsCache();
    }

    private static void DeleteDirectoryWithRetry(string path, int maxRetries = 5, int delayMs = 150)
    {
        if (!Directory.Exists(path)) return;

        try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); } catch { }
        GC.Collect();
        GC.WaitForPendingFinalizers();

        Exception? lastEx = null;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var dirInfo = new DirectoryInfo(path);
                foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    try { file.Attributes = FileAttributes.Normal; } catch { }
                }
                foreach (var subDir in dirInfo.GetDirectories("*", SearchOption.AllDirectories))
                {
                    try { subDir.Attributes = FileAttributes.Normal; } catch { }
                }

                dirInfo.Delete(true);
                return;
            }
            catch (Exception ex)
            {
                lastEx = ex;
                try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); } catch { }
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Thread.Sleep(delayMs);
            }
        }

        if (lastEx != null)
        {
            throw new InvalidOperationException($"Could not delete account directory '{path}': {lastEx.Message}", lastEx);
        }
    }

    public void DeleteAccount(string accountName)
    {
        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("Account name cannot be empty.");

        var targetDir = GetAccountDirectory(accountName);
        bool wasActive = string.Equals(GetActiveAccount(), accountName, StringComparison.OrdinalIgnoreCase);

        if (wasActive)
        {
            AgyKeyringHelper.DeleteToken("gemini:antigravity");
        }

        if (Directory.Exists(targetDir))
        {
            DeleteDirectoryWithRetry(targetDir);
        }

        try
        {
            _accountRepo.DeleteAccount(accountName);
        }
        catch { }

        if (wasActive)
        {
            SetActiveAccount("default", false);
        }
        (new AgyQuotaEngine(this)).ClearStatsCache();
    }

    public void LogoutAccount(string accountName)
    {
        var expectedEmail = GetCanonicalEmail(accountName);
        bool isActive = string.Equals(GetActiveAccount(), accountName, StringComparison.OrdinalIgnoreCase);

        if (isActive)
        {
            AgyKeyringHelper.DeleteToken("gemini:antigravity");
            var primaryDir = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? "", ".gemini");
            if (Directory.Exists(primaryDir))
            {
                var primaryAuthFiles = new[] { "oauth_creds.json", "state.json", "keyring_token.txt" };
                foreach (var f in primaryAuthFiles)
                {
                    var p = Path.Combine(primaryDir, f);
                    if (File.Exists(p)) { try { File.Delete(p); } catch { } }
                }
            }
        }

        var dir = GetAccountDirectory(accountName);
        if (Directory.Exists(dir))
        {
            var authFiles = new[] { "oauth_creds.json", "state.json", "keyring_token.txt" };
            foreach (var f in authFiles)
            {
                var p = Path.Combine(dir, f);
                if (File.Exists(p)) { try { File.Delete(p); } catch { } }
            }

            var subKeyring = Path.Combine(dir, "antigravity-cli", "keyring_token.txt");
            if (File.Exists(subKeyring)) { try { File.Delete(subKeyring); } catch { } }

            var gObj = new
            {
                accounts = new[] { new { email = expectedEmail } },
                activeAccount = expectedEmail
            };
            var gJson = JsonSerializer.Serialize(gObj, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(dir, "google_accounts.json"), gJson, Utf8NoBom);
        }

        try
        {
            _accountRepo.SaveAccountCredentials(new AccountCredentials(accountName, null, null, null, null, expectedEmail));
        }
        catch { }

        (new AgyQuotaEngine(this)).ClearStatsCache();
    }

    public void AuthenticateAccount(string accountName)
    {
        var email = GetAccountEmail(accountName) ?? (accountName.Contains("@") ? accountName : $"{accountName}@gmail.com");
        var targetDir = GetAccountDirectory(accountName);

        if (!Directory.Exists(targetDir))
        {
            AddAccount(accountName);
        }

        LogoutAccount(accountName);
        AgyKeyringHelper.DeleteToken("gemini:antigravity");

        var gObj = new
        {
            accounts = new[] { new { email } },
            activeAccount = email
        };
        var gJson = JsonSerializer.Serialize(gObj, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(targetDir, "google_accounts.json"), gJson, Utf8NoBom);

        var sObj = new
        {
            accountName = accountName,
            userEmail = email
        };
        var sJson = JsonSerializer.Serialize(sObj, new JsonSerializerOptions { WriteIndented = true });
        Directory.CreateDirectory(Path.Combine(targetDir, "antigravity-cli"));
        File.WriteAllText(Path.Combine(targetDir, "antigravity-cli", "settings.json"), sJson, Utf8NoBom);

        SetActiveAccount(accountName, false);

        var envDict = new Dictionary<string, string?>
        {
            ["GEMINI_HOME"] = targetDir,
            ["GEMINI_CLI_IDE_AUTH_TOKEN"] = null,
            ["GEMINI_CLI_IDE_SERVER_PORT"] = null
        };

        var agyExe = Helpers.ProcessRunner.Instance.FindOnPath("agy") ?? Helpers.ProcessRunner.Instance.FindOnPath("antigravity");
        if (!string.IsNullOrEmpty(agyExe))
        {
            SpectrePanel.Info($"Launching OAuth login for '{accountName}' ({email}) via '{agyExe}'...");
            Helpers.ProcessRunner.Instance.RunInteractive(agyExe, ["auth", "login"], envDict, targetDir);
        }
        else
        {
            SpectrePanel.Info($"Launching OAuth login for '{accountName}' ({email})...");
            Helpers.ProcessRunner.Instance.RunInteractive("pwsh", ["-NoProfile", "-Command", $"Remove-Item Env:\\GEMINI_CLI_IDE_AUTH_TOKEN -ErrorAction SilentlyContinue; Remove-Item Env:\\GEMINI_CLI_IDE_SERVER_PORT -ErrorAction SilentlyContinue; $env:GEMINI_HOME='{targetDir}'; agy"], null, targetDir);
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
                    try { DeleteDirectoryWithRetry(dir); } catch { }
                }
            }
            catch { }
        }

        try
        {
            var dbAccs = _accountRepo.GetAccounts();
            foreach (var dbAcc in dbAccs)
            {
                if (!string.Equals(dbAcc, "default", StringComparison.OrdinalIgnoreCase))
                {
                    _accountRepo.DeleteAccount(dbAcc);
                }
            }
        }
        catch { }

        LogoutAccount("default");
        SetActiveAccount("default", false);
        (new AgyQuotaEngine(this)).ClearStatsCache();
    }

    public bool IsAutoSwitchEnabled()
    {
        try
        {
            var configRepo = new Persistence.Repositories.SqliteConfigRepository(new Persistence.DbContext.SqliteDatabase());
            var val = configRepo.GetState("auto_switch_enabled");
            if (!string.IsNullOrEmpty(val))
            {
                return val.Trim() != "False";
            }
        }
        catch { }

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
        var newStateStr = current ? "False" : "True";
        try
        {
            var configRepo = new Persistence.Repositories.SqliteConfigRepository(new Persistence.DbContext.SqliteDatabase());
            configRepo.SetState("auto_switch_enabled", newStateStr);
        }
        catch { }

        try
        {
            Directory.CreateDirectory(AgySourceHome);
            File.WriteAllText(Path.Combine(AgySourceHome, "auto_switch_enabled.txt"), newStateStr, Utf8NoBom);
        }
        catch { }

        SpectrePanel.Info($"Auto-Switch is now: {(current ? "Disabled" : "Enabled")}");
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

