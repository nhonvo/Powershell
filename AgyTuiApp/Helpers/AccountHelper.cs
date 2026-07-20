using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public sealed class AccountMetadata
{
    [JsonPropertyName("LastUsed")]
    public string LastUsed
    {
        get;
        set;

    }
    = "Never";

    [JsonPropertyName("UsageCount")]
    public int UsageCount
    {
        get;
        set;

    }
    [JsonPropertyName("QuotaStatus")]
    public string QuotaStatus
    {
        get;
        set;

    }
    = "OK";

    [JsonPropertyName("RequestHistory")]
    public List<string> RequestHistory
    {
        get;
        set;

    }
    = [];

}

public sealed record QuotaMetrics(double RemainingWeekly, double Remaining5H, string TimeWeekly, string Time5H, int CountWeekly, int Count5H);

public sealed record AccountStats(string LastUsed, int UsageCount, string PrivateSize, string JunctionStatus, int SkillsCount, int ConversationsCount, string TokenStatus, string QuotaStatus, double GeminiWeekly, double GeminiFiveHour);

public static class AgyKeyringHelper
{
    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CredRead(string target, uint type, uint reserved, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CredWrite(ref CREDENTIAL credential, uint flags);

    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CredDelete(string target, uint type, uint flags);

    [DllImport("advapi32.dll", EntryPoint = "CredFree", SetLastError = true)] private static extern void CredFree(IntPtr buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        public string? TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string? UserName;

    }

    public static string? ReadToken(string target)
    {
        if (!CredRead(target, 1, 0, out var credPtr)) return null;

        try
        {
            var cred = Marshal.PtrToStructure<CREDENTIAL>(credPtr);
            if (cred.CredentialBlobSize > 0 && cred.CredentialBlob != IntPtr.Zero)
            {
                var blob = new byte[cred.CredentialBlobSize];
                Marshal.Copy(cred.CredentialBlob, blob, 0, (int)cred.CredentialBlobSize);
                return Encoding.UTF8.GetString(blob);
            }
        }
        finally
        {
            CredFree(credPtr);
        }
        return null;

    }

    public static bool WriteToken(string target, string username, string token)
    {
        var cred = new CREDENTIAL
        {
            Type = 1,
            TargetName = target,
            UserName = username,
            Persist = 2
        }
        ;
        var blob = Encoding.UTF8.GetBytes(token);
        cred.CredentialBlobSize = (uint)blob.Length;
        var blobPtr = Marshal.AllocHGlobal(blob.Length);

        try
        {
            Marshal.Copy(blob, 0, blobPtr, blob.Length);
            cred.CredentialBlob = blobPtr;
            return CredWrite(ref cred, 0);
        }
        finally
        {
            Marshal.FreeHGlobal(blobPtr);
        }

    }

    public static bool DeleteToken(string target) => CredDelete(target, 1, 0);

}
public static class AgySecretVault
{
    public static string GetSecretsFilePath()
    {
        var dir = @"C:\Users\Public\.gemini";
        Directory.CreateDirectory(dir);
        return System.IO.Path.Combine(dir, "secrets.json");

    }

    public static Dictionary<string, string> LoadSecrets()
    {
        var file = GetSecretsFilePath();
        if (!File.Exists(file)) return new();

        try
        {
            var raw = File.ReadAllText(file);
            if (string.IsNullOrWhiteSpace(raw)) return new();
            return JsonSerializer.Deserialize<Dictionary<string, string>>(raw) ?? new();
        }
        catch
        {
            return new();
        }

    }

    public static void SaveSecrets(Dictionary<string, string> secrets)
    {
        var file = GetSecretsFilePath();

        try
        {
            File.WriteAllText(file, JsonSerializer.Serialize(secrets));
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to save secrets: {ex.Message}");
        }

    }

    public static void SetSecret(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
        {
            SpectrePanel.Error("Key and Value cannot be empty.");
            return;
        }
        var secrets = LoadSecrets();

        try
        {
            var bytes = Encoding.Unicode.GetBytes(value);
            var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            secrets[key] = Convert.ToHexString(protectedBytes).ToLowerInvariant();
            SaveSecrets(secrets);
            AnsiConsole.MarkupLine($"[green]Secret '{key.EscapeMarkup()}' saved and encrypted successfully.[/]");
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to encrypt/save secret: {ex.Message}");
        }

    }

    public static string GetSecret(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "";
        var secrets = LoadSecrets();
        if (!secrets.TryGetValue(key, out var encrypted))
        {
            SpectrePanel.Warning($"Secret '{key}' not found.");
            return "";
        }
        try
        {
            var bytes = Convert.FromHexString(encrypted);
            var plain = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.Unicode.GetString(plain);
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to decrypt secret '{key}': {ex.Message}");
            return "";
        }

    }

    public static void RemoveSecret(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        var secrets = LoadSecrets();
        if (secrets.Remove(key))
        {
            SaveSecrets(secrets);
            AnsiConsole.MarkupLine($"[green]Secret '{key.EscapeMarkup()}' removed successfully.[/]");
        }
        else
        {
            SpectrePanel.Warning($"Secret '{key}' not found.");
        }

    }

    public static void ListSecrets()
    {
        var secrets = LoadSecrets();
        if (secrets.Count == 0)
        {
            SpectrePanel.Warning("No secrets stored.");
            return;
        }
        AnsiConsole.MarkupLine("[cyan]Stored Secret Keys:[/]");
        foreach (var key in secrets.Keys) AnsiConsole.MarkupLine($" * {key.EscapeMarkup()}");

    }

}
public static class AgyAccountCore
{
    public static TimeProvider Clock
    {
        get;
        set;

    }
    = TimeProvider.System;
    public static readonly string AgySourceHome = @"C:\Users\Public\.gemini";
    public static readonly string AgyAccountPrefix = @"C:\Users\Public\.gemini_";

    public static string AgyActiveAccountFile => Path.Combine(AgySourceHome, "active_account.txt");
    private static bool? _networkOnline;

    private static readonly Lock _networkLock = new();

    public static bool CheckNetworkStatus()
    {
        lock (_networkLock)
        {
            if (_networkOnline.HasValue) return _networkOnline.Value;

            try
            {
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    _networkOnline = false;
                    return false;
                }
                using var ping = new Ping();
                _networkOnline = ping.Send("8.8.8.8", 200).Status == IPStatus.Success;
            }
            catch
            {
                _networkOnline = false;
            }
            return _networkOnline.Value;
        }

    }

    public static string[] GetAccounts()
    {
        var accounts = new List<string>
        {
            "default"
        }
        ;
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
                if (!Regex.IsMatch(name, "backup|copy|temp", RegexOptions.IgnoreCase) && !accounts.Contains(name, StringComparer.OrdinalIgnoreCase)) accounts.Add(name);
            }
        }
        return [.. accounts];

    }

    public static string GetActiveAccount()
    {
        try
        {
            if (File.Exists(AgyActiveAccountFile))
            {
                var content = File.ReadAllText(AgyActiveAccountFile).Trim();
                if (!string.IsNullOrEmpty(content)) return content;
            }
        }
        catch { }
        var home = Environment.GetEnvironmentVariable("GEMINI_HOME") ?? "";
        var m = Regex.Match(home, @"\.gemini_(.+)$");
        return m.Success ? m.Groups[1].Value : "default";

    }

    public static string? GetAccountEmail(string accountName)
    {
        var dir = GetAccountDirectory(accountName);
        var googleAccountsFile = Path.Combine(dir, "google_accounts.json");
        if (File.Exists(googleAccountsFile))
        {
            try
            {
                var content = File.ReadAllText(googleAccountsFile);
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("email", out var emailProp)) return emailProp.GetString();
                    if (doc.RootElement.TryGetProperty("account", out var accProp)) return accProp.GetString();
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                {
                    var first = doc.RootElement[0];
                    if (first.TryGetProperty("email", out var emailProp)) return emailProp.GetString();
                    if (first.TryGetProperty("account", out var accProp)) return accProp.GetString();
                }
            }
            catch { }
        }

        var tokenFile = Path.Combine(dir, "keyring_token.txt");
        if (File.Exists(tokenFile))
        {
            try
            {
                var encrypted = File.ReadAllText(tokenFile).Trim();
                var decrypted = DecryptToken(encrypted);
                if (!string.IsNullOrEmpty(decrypted))
                {
                    using var doc = JsonDocument.Parse(decrypted);
                    if (doc.RootElement.TryGetProperty("email", out var emailProp)) return emailProp.GetString();
                }
            }
            catch { }
        }
        return null;
    }

    public static string GetAccountDirectory(string accountName)
    {
        if (string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase)) return AgySourceHome;
        var target = AgyAccountPrefix + accountName;
        if (!Directory.Exists(target))
        {
            var alt = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? "", ".gemini_" + accountName);
            if (Directory.Exists(alt)) return alt;
        }
        return target;

    }

    public static AccountMetadata GetAccountMetadata(string accountName)
    {
        var file = Path.Combine(GetAccountDirectory(accountName), "account_metadata.json");
        if (File.Exists(file))
        {
            try
            {
                var raw = File.ReadAllText(file);
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    var meta = JsonSerializer.Deserialize<AccountMetadata>(raw);
                    if (meta != null) return meta;
                }
            }
            catch
            {
            }
        }
        return new AccountMetadata();

    }

    public static void UpdateAccountMetadata(string accountName)
    {
        var dir = GetAccountDirectory(accountName);

        try
        {
            Directory.CreateDirectory(dir);
        }
        catch
        {
            return;
        }
        var meta = GetAccountMetadata(accountName);
        meta.LastUsed = Clock.GetLocalNow().ToString("yyyy-MM-ddTHH:mm:sszzz");
        meta.UsageCount++;
        var now = Clock.GetUtcNow().UtcDateTime;
        var cutoff = now.AddDays(-7);
        meta.RequestHistory.Add(now.ToString("o"));
        meta.RequestHistory = meta.RequestHistory.Where(ts => DateTime.TryParse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt) && dt >= cutoff).ToList();

        var rolling = CalculateRollingQuotas(accountName);
        if (rolling.Remaining5H <= 10.0)
        {
            TriggerLowQuotaWebhook(accountName, rolling.Remaining5H);
        }

        try
        {
            var json = JsonSerializer.Serialize(meta, new JsonSerializerOptions
            {
                WriteIndented = true
            }
            );
            File.WriteAllText(Path.Combine(dir, "account_metadata.json"), json, Encoding.UTF8);
        }
        catch
        {
        }

    }

    public static void SetAccountQuotaExceeded(string accountName, bool exceeded)
    {
        var dir = GetAccountDirectory(accountName);
        if (!Directory.Exists(dir)) return;
        var meta = GetAccountMetadata(accountName);
        var newStatus = exceeded ? "Exceeded" : "OK";
        if (meta.QuotaStatus == newStatus) return;
        meta.QuotaStatus = newStatus;

        try
        {
            var json = JsonSerializer.Serialize(meta, new JsonSerializerOptions
            {
                WriteIndented = true
            }
            );
            File.WriteAllText(Path.Combine(dir, "account_metadata.json"), json, Encoding.UTF8);
        }
        catch
        {
        }

    }

    public static List<(DateTime Time, int ReqsReleased, double QuotaGained)> GetQuotaReleaseForecast(string accountName)
    {
        var history = GetAccountMetadata(accountName).RequestHistory;
        var now = Clock.GetUtcNow().UtcDateTime;
        var fiveHoursAgo = now.AddHours(-5);

        var activeRequests = new List<DateTime>();
        foreach (var ts in history)
        {
            if (DateTime.TryParse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
            {
                if (dt >= fiveHoursAgo) activeRequests.Add(dt);
            }
        }

        activeRequests.Sort();

        var forecast = new List<(DateTime Time, int ReqsReleased, double QuotaGained)>();
        const int limit5H = 50;

        var releases = activeRequests.Select(dt => dt.AddHours(5)).GroupBy(t =>
        {
            var min = (t.Minute / 15) * 15;
            return new DateTime(t.Year, t.Month, t.Day, t.Hour, min, 0, DateTimeKind.Utc);
        }).OrderBy(g => g.Key);

        foreach (var group in releases)
        {
            int count = group.Count();
            double gain = Math.Round((count / (double)limit5H) * 100.0, 2);
            forecast.Add((group.Key, count, gain));
        }

        return forecast;
    }

    public static void TriggerLowQuotaWebhook(string accountName, double remaining5H)
    {
        var webhookFile = Path.Combine(AgySourceHome, "quota_webhook.txt");
        if (!File.Exists(webhookFile)) return;

        try
        {
            var url = File.ReadAllText(webhookFile).Trim();
            if (string.IsNullOrEmpty(url)) return;

            var client = new System.Net.Http.HttpClient();
            var payload = new
            {
                text = $"[Agy Alert] Low quota warning for account {accountName}: Only {remaining5H}% of the 5-hour quota remaining."
            };
            var json = JsonSerializer.Serialize(payload);
            var reqContent = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");

            _ = client.PostAsync(url, reqContent);
        }
        catch { }
    }

    public static QuotaMetrics CalculateRollingQuotas(string accountName)
    {
        var history = GetAccountMetadata(accountName).RequestHistory;
        var now = Clock.GetUtcNow().UtcDateTime;
        var fiveHoursAgo = now.AddHours(-5);
        var sevenDaysAgo = now.AddDays(-7);
        int reqs5H = 0, reqsWeekly = 0;
        var oldest5H = now;
        var oldestWeekly = now;
        foreach (var ts in history)
        {
            if (!DateTime.TryParse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)) continue;
            if (dt >= fiveHoursAgo)
            {
                reqs5H++;
                if (dt < oldest5H) oldest5H = dt;
            }
            if (dt >= sevenDaysAgo)
            {
                reqsWeekly++;
                if (dt < oldestWeekly) oldestWeekly = dt;
            }
        }
        const int limit5H = 50, limitWeekly = 1000;
        var remaining5H = Math.Max(0.0, 100.0 - Math.Round((reqs5H / (double)limit5H) * 100.0, 2));
        var remainingWeekly = Math.Max(0.0, 100.0 - Math.Round((reqsWeekly / (double)limitWeekly) * 100.0, 2));
        var secs5H = Math.Max(0, (int)Math.Round((oldest5H.AddHours(5) - now).TotalSeconds));
        var secsWeekly = Math.Max(0, (int)Math.Round((oldestWeekly.AddDays(7) - now).TotalSeconds));

        static string Fmt(int s) => $"{s / 3600}h {(s % 3600) / 60}m";
        return new QuotaMetrics(remainingWeekly, remaining5H, Fmt(secsWeekly), Fmt(secs5H), reqsWeekly, reqs5H);

    }

    public static bool IsAutoSwitchEnabled()
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

    public static string GlobalBinDir => @"C:\ProgramData\agy\bin";
    public static string AgyBinaryPath => Path.Combine(GlobalBinDir, "agy.exe");

    public static void BackupActiveToken(string accountName)
    {
        try
        {
            var token = AgyKeyringHelper.ReadToken("gemini:antigravity");
            if (!string.IsNullOrEmpty(token))
            {
                var accDir = GetAccountDirectory(accountName);
                Directory.CreateDirectory(accDir);
                var tokenFile = Path.Combine(accDir, "keyring_token.txt");
                var encrypted = EncryptToken(token);
                File.WriteAllText(tokenFile, encrypted, Encoding.UTF8);
            }
        }
        catch { }
    }

    public static void RestoreActiveToken(string accountName)
    {
        try
        {
            var accDir = GetAccountDirectory(accountName);
            var tokenFile = Path.Combine(accDir, "keyring_token.txt");
            if (File.Exists(tokenFile))
            {
                var encrypted = File.ReadAllText(tokenFile).Trim();
                if (!string.IsNullOrEmpty(encrypted))
                {
                    var token = DecryptToken(encrypted);
                    if (!string.IsNullOrEmpty(token))
                    {
                        AgyKeyringHelper.WriteToken("gemini:antigravity", "antigravity", token);
                    }
                }
            }
            else
            {
                AgyKeyringHelper.DeleteToken("gemini:antigravity");
            }
        }
        catch { }
    }

    private static string EncryptToken(string token)
    {
        var data = Encoding.UTF8.GetBytes(token);
        var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    private static string? DecryptToken(string encrypted)
    {
        try
        {
            var data = Convert.FromBase64String(encrypted);
            var decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return null;
        }
    }

    public static void SetActiveAccount(string accountName, bool temporary)
    {
        if (!string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase))
        {
            var targetDir = GetAccountDirectory(accountName);
            if (!Directory.Exists(targetDir))
            {
                throw new ArgumentException($"Account '{accountName}' does not exist.");
            }
        }

        ClearStatsCache();
        UpdateAccountMetadata(accountName);
        BackupActiveToken(GetActiveAccount());

        if (string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase))
        {
            Environment.SetEnvironmentVariable("GEMINI_HOME", AgySourceHome);
            if (!temporary)
            {
                try
                {
                    Directory.CreateDirectory(AgySourceHome);
                    File.WriteAllText(AgyActiveAccountFile, "default", Encoding.UTF8);
                }
                catch { }
            }
            RestoreActiveToken("default");
            SpectrePanel.Success("Switched to default Antigravity account (Primary).");
            return;
        }

        var targetDirLoc = GetAccountDirectory(accountName);
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
        RestoreActiveToken(accountName);

        if (!temporary)
        {
            try
            {
                Directory.CreateDirectory(AgySourceHome);
                File.WriteAllText(AgyActiveAccountFile, accountName, Encoding.UTF8);
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

        try
        {
            foreach (var name in new[] { "agy", "antigravity-hub", "openclaw" })
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    try { p.Kill(true); } catch { }
                }
            }
        }
        catch { }
    }

    public static void AddAccount(string accountName)
    {
        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("Account name cannot be empty.");

        if (string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Cannot create an account named 'default'.");

        var destDir = GetAccountDirectory(accountName);
        if (Directory.Exists(destDir))
            throw new InvalidOperationException($"Account '{accountName}' already exists.");

        Directory.CreateDirectory(destDir);

        // Copy root files
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

        // Generate installation ID
        var installationIdFile = Path.Combine(destDir, "installation_id");
        File.WriteAllText(installationIdFile, Guid.NewGuid().ToString());

        // Create independent subdirectories (no symlinks or junctions, to keep accounts isolated!)
        var subDirs = new[] { "antigravity", "antigravity-cli", "config", "history", "antigravity-ide", "wf", "learn" };
        foreach (var sub in subDirs)
        {
            Directory.CreateDirectory(Path.Combine(destDir, sub));
        }
        ClearStatsCache();
    }

    public static void DeleteAccount(string accountName)
    {
        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("Account name cannot be empty.");

        var targetDir = GetAccountDirectory(accountName);
        if (!Directory.Exists(targetDir))
            throw new DirectoryNotFoundException($"Account '{accountName}' does not exist.");

        // Recursively delete the directory
        Directory.Delete(targetDir, true);

        // If the active account was deleted, switch back to default
        if (string.Equals(GetActiveAccount(), accountName, StringComparison.OrdinalIgnoreCase))
        {
            SetActiveAccount("default", false);
        }
        ClearStatsCache();
    }

    public static void LogoutAccount(string accountName)
    {
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

    public static void SyncActiveAccountWithKeyring(bool silent)
    {
        if (!IsAutoSwitchEnabled()) return;
        try
        {
            string savedAcc = GetActiveAccount();
            string? keyringToken = AgyKeyringHelper.ReadToken("gemini:antigravity");
            if (string.IsNullOrEmpty(keyringToken)) return;

            string? matchedAcc = null;
            var availableAccounts = GetAccounts();
            foreach (var acc in availableAccounts)
            {
                var accDir = GetAccountDirectory(acc);
                var tokenFile = Path.Combine(accDir, "keyring_token.txt");
                if (File.Exists(tokenFile))
                {
                    try
                    {
                        var encrypted = File.ReadAllText(tokenFile).Trim();
                        if (!string.IsNullOrEmpty(encrypted))
                        {
                            var savedToken = DecryptToken(encrypted);
                            if (savedToken == keyringToken)
                            {
                                matchedAcc = acc;
                                break;
                            }
                            else
                            {
                                try
                                {
                                    using var savedJson = JsonDocument.Parse(savedToken);
                                    using var currentJson = JsonDocument.Parse(keyringToken);
                                    if (savedJson.RootElement.TryGetProperty("token", out var sToken) &&
                                        currentJson.RootElement.TryGetProperty("token", out var cToken) &&
                                        sToken.TryGetProperty("refresh_token", out var sRefresh) &&
                                        cToken.TryGetProperty("refresh_token", out var cRefresh) &&
                                        sRefresh.GetString() == cRefresh.GetString())
                                    {
                                        matchedAcc = acc;
                                        break;
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                }
            }

            if (matchedAcc == null)
            {
                try
                {
                    if (CheckNetworkStatus())
                    {
                        using var json = JsonDocument.Parse(keyringToken);
                        if (json.RootElement.TryGetProperty("token", out var tokenObj) && tokenObj.TryGetProperty("access_token", out var accessTok))
                        {
                            var accessToken = accessTok.GetString();
                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                using var client = new HttpClient();
                                client.Timeout = TimeSpan.FromSeconds(3);
                                var response = client.GetStringAsync($"https://oauth2.googleapis.com/tokeninfo?access_token={accessToken}").Result;
                                using var info = JsonDocument.Parse(response);
                                if (info.RootElement.TryGetProperty("email", out var emailProp))
                                {
                                    var email = emailProp.GetString()?.Trim().ToLower();
                                    if (email != null)
                                    {
                                        foreach (var acc in availableAccounts)
                                        {
                                            if (string.Equals(acc.Trim(), email, StringComparison.OrdinalIgnoreCase))
                                            {
                                                matchedAcc = acc;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            if (matchedAcc != null)
            {
                if (!string.Equals(matchedAcc, savedAcc, StringComparison.OrdinalIgnoreCase))
                {
                    if (!silent)
                    {
                        SpectrePanel.Warning($"Keyring matches account '{matchedAcc}'. Auto-switching active account.");
                    }
                    savedAcc = matchedAcc;
                    Directory.CreateDirectory(AgySourceHome);
                    File.WriteAllText(AgyActiveAccountFile, savedAcc, Encoding.UTF8);
                    Environment.SetEnvironmentVariable("GEMINI_HOME", GetAccountDirectory(savedAcc));
                }
                var accDir = GetAccountDirectory(savedAcc);
                Directory.CreateDirectory(accDir);
                var tokenFile = Path.Combine(accDir, "keyring_token.txt");
                var encryptedToken = EncryptToken(keyringToken);
                File.WriteAllText(tokenFile, encryptedToken, Encoding.UTF8);
            }
            else
            {
                var accDir = GetAccountDirectory(savedAcc);
                Directory.CreateDirectory(accDir);
                var tokenFile = Path.Combine(accDir, "keyring_token.txt");
                var encryptedToken = EncryptToken(keyringToken);
                File.WriteAllText(tokenFile, encryptedToken, Encoding.UTF8);
            }
        }
        catch { }
    }

    public static void AutoSwitchOnQuotaExceeded()
    {
        if (!IsAutoSwitchEnabled()) return;
        var active = GetActiveAccount();
        var activeMeta = GetAccountMetadata(active);
        if (string.Equals(activeMeta.QuotaStatus, "Exceeded", StringComparison.OrdinalIgnoreCase))
        {
            string? candidate = null;
            foreach (var acc in GetAccounts())
            {
                if (string.Equals(acc, active, StringComparison.OrdinalIgnoreCase)) continue;
                var accDir = GetAccountDirectory(acc);
                if (!File.Exists(Path.Combine(accDir, "keyring_token.txt"))) continue;
                var meta = GetAccountMetadata(acc);
                if (string.Equals(meta.QuotaStatus, "OK", StringComparison.OrdinalIgnoreCase))
                {
                    candidate = acc;
                    break;
                }
            }

            if (candidate != null)
            {
                SpectrePanel.Warning($"Active account '{active}' exceeded quota. Auto-switching to candidate account '{candidate}' with available quota.");
                SetActiveAccount(candidate, false);
            }
        }
    }

    public static void ToggleAutoSwitch()
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

    public static string? FindAutoSwitchCandidate()
    {
        if (!IsAutoSwitchEnabled()) return null;
        var active = GetActiveAccount();
        if (GetAccountMetadata(active).QuotaStatus != "Exceeded") return null;
        foreach (var acc in GetAccounts())
        {
            if (string.Equals(acc, active, StringComparison.OrdinalIgnoreCase)) continue;
            var tokenFile = Path.Combine(GetAccountDirectory(acc), "keyring_token.txt");
            if (!File.Exists(tokenFile)) continue;
            var quota = GetAccountMetadata(acc).QuotaStatus ?? "OK";
            if (quota == "OK") return acc;
        }
        return null;

    }

    public static bool CheckQuotaAfterRun(string accountName)
    {
        try
        {
            var brainDir = Path.Combine(AgySourceHome, "antigravity", "brain");
            if (!Directory.Exists(brainDir)) return false;
            var latest = new DirectoryInfo(brainDir).EnumerateFiles("transcript.jsonl", SearchOption.AllDirectories).OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
            if (latest == null) return false;
            if ((DateTime.Now - latest.LastWriteTime).TotalSeconds > 60) return false;
            var tail = File.ReadLines(latest.FullName).TakeLast(15);
            var quotaErr = tail.Any(line => Regex.IsMatch(line, @"RESOURCE_EXHAUSTED|quota exceeded|quotaExceeded|ResourceExhausted|quota limit") && Regex.IsMatch(line, @"""status""\s*:\s*""ERROR""|""code""\s*:\s*429"));
            SetAccountQuotaExceeded(accountName, quotaErr);
            return quotaErr;
        }
        catch
        {
            return false;
        }

    }

    private static readonly TtlCache<string, long> _sizeCache = new(TimeSpan.FromSeconds(15));

    public static long GetPrivateDirectorySize(string path)
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

    public static string GetJunctionStatus(string accountName)
    {
        if (string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase)) return "Healthy (Primary)";
        var destDir = GetAccountDirectory(accountName);
        if (!Directory.Exists(destDir)) return "Uninitialized";
        var shared = new[]
        {
            "antigravity","antigravity-cli","config","history","antigravity-ide","wf"
        }
        ;
        foreach (var sub in shared)
        {
            var subPath = Path.Combine(destDir, sub);
            if (!Directory.Exists(subPath)) return "Needs Repair";
            if (new DirectoryInfo(subPath).LinkTarget == null) return "Needs Repair";
        }
        return "Healthy";

    }

    private static readonly TtlCache<string, AccountStats> _statsCache = new(TimeSpan.FromSeconds(3));

    public static void ClearStatsCache()
    {
        _statsCache.InvalidateAll();
    }

    public static AccountStats GetAccountStats(string accountName)
    {
        return _statsCache.GetOrCompute(accountName, () =>
        {
            var meta = GetAccountMetadata(accountName);
            var dir = GetAccountDirectory(accountName);
            var privateSize = GetPrivateDirectorySize(dir);
            var junctionStatus = GetJunctionStatus(accountName);
            int skillsCount = 0, convCount = 0;
            var skillsPath = Path.Combine(AgySourceHome, "config", "skills");
            if (Directory.Exists(skillsPath)) skillsCount = Directory.GetDirectories(skillsPath).Length;
            var convPath = Path.Combine(AgySourceHome, "antigravity", "brain");
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

    public static string GetProgressBar(double percentage)
    {
        const int total = 50;
        int filled = Math.Min(total, Math.Max(0, (int)Math.Round((percentage / 100.0) * total)));
        var bar = new string('█', filled) + new string('░', total - filled);
        return $" [{bar}] {Math.Round(percentage, 2):F2}%";

    }

    public static string[] GetUsageLines(string accountName)
    {
        var meta = GetAccountMetadata(accountName);
        var quota = CalculateRollingQuotas(accountName);
        double geminiWeekly = quota.RemainingWeekly;
        double geminiFiveHour = quota.Remaining5H;
        double claudeWeekly = 100.0;
        double claudeFiveHour = 100.0;
        var bar = new string('─', 140);
        var lines = new List<string>
        {
            bar,">", bar,"└ Models & Quota","",$" Account: {accountName}","","GEMINI MODELS"," Models within this group: Gemini Flash, Gemini Pro",""," Weekly Limit", GetProgressBar(geminiWeekly),$" {(int)Math.Round(geminiWeekly)}% remaining · Refreshes in {quota.TimeWeekly}",""," Five Hour Limit", GetProgressBar(geminiFiveHour), geminiFiveHour>=100.0?" Quota available":$" {(int)Math.Round(geminiFiveHour)}% remaining · Refreshes in {quota.Time5H}","","","CLAUDE AND GPT MODELS"," Models within this group: Claude Opus, Claude Sonnet, GPT-OSS",""," Weekly Limit", GetProgressBar(claudeWeekly), claudeWeekly>=100.0?" Quota available":$" {(int)Math.Round(claudeWeekly)}% remaining",""," Five Hour Limit", GetProgressBar(claudeFiveHour), claudeFiveHour>=100.0?" Quota available":$" {(int)Math.Round(claudeFiveHour)}% remaining","",""," │ Within each group, models share a weekly limit and a 5-hour limit. Quota is"," │ consumed proportionally to the cost of the tokens. The 5-hour limit smooths"," │ out aggregate demand to fairly distribute global capacity across all users.",""," Weekly Request Distribution (Last 7 Days)"," ==========================================="
        }
        ;
        var forecast = GetQuotaReleaseForecast(accountName);
        if (forecast.Count > 0)
        {
            lines.Add("");
            lines.Add(" Quota Release Forecast (Next 5 Hours)");
            lines.Add(" -------------------------------------");
            foreach (var item in forecast.Take(10))
            {
                var localTime = item.Time.ToLocalTime();
                lines.Add($"   * [{localTime:HH:mm}] +{item.QuotaGained}% (+{item.ReqsReleased} reqs)");
            }
        }

        var now = Clock.GetLocalNow().DateTime;
        var dayData = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0;
        i < 7;
        i++) dayData[now.Date.AddDays(-i).ToString("ddd")] = 0;
        foreach (var ts in meta.RequestHistory)
        {
            if (!DateTime.TryParse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)) continue;
            var key = dt.ToLocalTime().ToString("ddd");
            if (dayData.ContainsKey(key)) dayData[key]++;
        }
        for (int i = 6;
        i >= 0;
        i--)
        {
            var date = now.Date.AddDays(-i);
            var day = date.ToString("ddd");
            var count = dayData.GetValueOrDefault(day);
            var bar2 = new string('█', Math.Min(10, count)) + new string('░', Math.Max(0, 10 - count));
            lines.Add($" {day} [{bar2}] {count} requests");
        }
        lines.Add(" -------------------------------------------");
        lines.Add($" Total Weekly Requests: {quota.CountWeekly} / 1000 limit");
        return [.. lines];

    }

    public static void ShowAccounts()
    {
        var accounts = GetAccounts();
        var active = GetActiveAccount();
        var rows = accounts.Select(a => new[]
        {
            a==active?"[green bold]*[/]":" ", a.EscapeMarkup(), GetAccountDirectory(a).EscapeMarkup(), File.Exists(Path.Combine(GetAccountDirectory(a),"keyring_token.txt"))?"[green]Logged In[/]":"[dim]Not Logged In[/]"
        }
        ).ToArray();
        SpectreTable.Render(["", "Account", "Directory", "Status"], rows, markup: true);

    }

    public static void ShowAllAccountsSummary()
    {
        var accounts = GetAccounts();
        var active = GetActiveAccount();
        var rows = accounts.Select(acc =>
        {
            var stats = GetAccountStats(acc);
            var nameCell = (acc == active ? "[green bold]* " : " ") + acc.EscapeMarkup() + (acc == active ? "[/]" : "");
            var quotaStr = stats.TokenStatus == "Logged In" ? (stats.QuotaStatus == "Exceeded" ? "[red]Exceeded[/]" : $"[cyan]{(int)Math.Round(stats.GeminiWeekly)}%[/] / [cyan]{(int)Math.Round(stats.GeminiFiveHour)}%[/]") : "[dim]--[/]";
            var lastUsed = stats.LastUsed.Length >= 10 && stats.LastUsed != "Never" ? stats.LastUsed[..10] : "Never";
            return new[]
            {
                nameCell, stats.TokenStatus=="Logged In"?"[green]Logged In[/]":"[dim]Not Logged In[/]", quotaStr, stats.UsageCount.ToString(), lastUsed, stats.PrivateSize
            }
            ;
        }
        ).ToArray();
        SpectreTable.Render(["Account", "Status", "Quota W / 5h", "Uses", "Last Used", "Size"], rows, markup: true);

    }

}
public static class AgyAccountMenu
{
    public enum MainMenuChoice
    {
        Exit, ManageAccount, AddAccount, ToggleAutoSwitch, ShowStats

    }

    public sealed record MainMenuResult(MainMenuChoice Choice, string? AccountName);

    public static MainMenuResult ShowManageMenu()
    {
        var accounts = AgyAccountCore.GetAccounts();
        var active = AgyAccountCore.GetActiveAccount();
        var menuItems = new List<string>();
        var defaultIdx = 0;
        for (var i = 0;
        i < accounts.Length;
        i++)
        {
            var status = File.Exists(System.IO.Path.Combine(AgyAccountCore.GetAccountDirectory(accounts[i]), "keyring_token.txt")) ? "Logged In" : "Not Logged In";
            if (accounts[i] == active)
            {
                menuItems.Add($"* {accounts[i]} (Active, {status})");
                defaultIdx = i;
            }
            else menuItems.Add($" {accounts[i]} ({status})");
        }
        menuItems.Add("[+] Add New Account");
        menuItems.Add($"[Settings] Toggle Auto-Switch (Currently: {(AgyAccountCore.IsAutoSwitchEnabled() ? "Enabled" : "Disabled")})");
        menuItems.Add("[Stats] Show All Accounts Summary");
        menuItems.Add("[x] Exit Dashboard");
        var selected = SpectreMenu.ShowRobust(["Antigravity Multi-Account Manager"], menuItems.ToArray(), defaultIdx, false, true);
        if (selected < 0 || selected == menuItems.Count - 1) return new(MainMenuChoice.Exit, null);
        if (selected < accounts.Length) return new(MainMenuChoice.ManageAccount, accounts[selected]);
        if (selected == accounts.Length) return new(MainMenuChoice.AddAccount, null);
        if (selected == accounts.Length + 1) return new(MainMenuChoice.ToggleAutoSwitch, null);
        return new(MainMenuChoice.ShowStats, null);

    }
    public enum AccountAction
    {
        Back, SetActivePersistent, SetActiveTemporary, ShowUsage, Login, Logout, Delete

    }

    public static void ShowAccountStatsCard(string accountName)
    {
        var stats = AgyAccountCore.GetAccountStats(accountName);
        AnsiConsole.MarkupLine("[cyan]=============================================[/]");
        AnsiConsole.MarkupLine($"[cyan] ACCOUNT STATS: {accountName.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine("[cyan]=============================================[/]");
        AnsiConsole.MarkupLine($" * Status: {stats.TokenStatus.EscapeMarkup()}");
        AnsiConsole.MarkupLine($" * Quota Status: {stats.QuotaStatus.EscapeMarkup()}");
        AnsiConsole.MarkupLine($" * Last Used: {stats.LastUsed.EscapeMarkup()}");
        AnsiConsole.MarkupLine($" * Usage Count: {stats.UsageCount} sessions/calls");
        AnsiConsole.MarkupLine($" * Private Size: {stats.PrivateSize.EscapeMarkup()} (excluding shared)");
        AnsiConsole.MarkupLine($" * Sync Health: {stats.JunctionStatus.EscapeMarkup()}");
        AnsiConsole.MarkupLine($" * Shared Skills: {stats.SkillsCount} skills");
        AnsiConsole.MarkupLine($" * Shared History: {stats.ConversationsCount} conversations");
        AnsiConsole.MarkupLine("[cyan]=============================================[/]");
        AnsiConsole.WriteLine();

    }

    public static AccountAction ShowAccountSubMenu(string accountName)
    {
        AnsiConsole.Clear();
        ShowAccountStatsCard(accountName);
        var status = File.Exists(System.IO.Path.Combine(AgyAccountCore.GetAccountDirectory(accountName), "keyring_token.txt")) ? "Logged In" : "Not Logged In";
        var subItems = new List<string>
        {
            "[Switch] Set as Active (Persistent)","[Switch] Set as Active (Temporary)","[Usage] Models & Quota","[Login] Sign In / Re-authenticate","[Logout] Sign Out / Reset Credentials"
        }
        ;
        if (!string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase)) subItems.Add("[Delete] Remove Account");
        subItems.Add("[Back] Return to Main Menu");
        var subSel = SpectreMenu.ShowRobust([$"Manage Account: {accountName} ({status})"], subItems.ToArray(), 0, false, true);
        if (subSel < 0) return AccountAction.Back;
        return subItems[subSel] switch
        {
            "[Switch] Set as Active (Persistent)" => AccountAction.SetActivePersistent,
            "[Switch] Set as Active (Temporary)" => AccountAction.SetActiveTemporary,
            "[Usage] Models & Quota" => AccountAction.ShowUsage,
            "[Login] Sign In / Re-authenticate" => AccountAction.Login,
            "[Logout] Sign Out / Reset Credentials" => AccountAction.Logout,
            "[Delete] Remove Account" => AccountAction.Delete,
            _ => AccountAction.Back
        }
        ;

    }
    public enum SelectChoice
    {
        Cancel, Selected, AddAccount, DeleteAccount

    }

    public sealed record SelectResult(SelectChoice Choice, string? AccountName);

    public static SelectResult ShowSelectAccountMenu()
    {
        var accounts = AgyAccountCore.GetAccounts();
        var active = AgyAccountCore.GetActiveAccount();
        var menuItems = new List<string>();
        var defaultIdx = 0;
        for (var i = 0;
        i < accounts.Length;
        i++)
        {
            if (accounts[i] == active)
            {
                menuItems.Add($"{accounts[i]} (Active)");
                defaultIdx = i;
            }
            else menuItems.Add(accounts[i]);
        }
        menuItems.Add("[+] Add New Account");
        menuItems.Add("[x] Delete Account");
        menuItems.Add("[exit] Cancel / Exit");
        var selected = SpectreMenu.ShowRobust(["Select Antigravity Account"], menuItems.ToArray(), defaultIdx, false, true);
        if (selected < 0) return new(SelectChoice.Cancel, null);
        if (selected < accounts.Length) return new(SelectChoice.Selected, accounts[selected]);
        if (selected == accounts.Length) return new(SelectChoice.AddAccount, null);
        return new(SelectChoice.DeleteAccount, null);

    }

    public static string? ShowDeleteAccountMenu()
    {
        var deletable = AgyAccountCore.GetAccounts().Where(a => !string.Equals(a, "default", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (deletable.Length == 0)
        {
            SpectrePanel.Warning("No secondary accounts available to delete.");
            return null;
        }
        var idx = SpectreMenu.ShowRobust(["Delete Antigravity Account"], deletable, 0, false, true);
        return idx >= 0 ? deletable[idx] : null;

    }

}
public static class Projects
{
    public static readonly string AgBaseDir = !string.IsNullOrEmpty(Config.Current.ProjectsBaseDir) ? Config.Current.ProjectsBaseDir : (Directory.Exists(@"C:\Users\sshuser\project") ? @"C:\Users\sshuser\project" : System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "project"));

    public static string? StartManager()
    {
        var projectDir = System.IO.Path.Combine(AgBaseDir, "AntigravityManager");
        if (!Directory.Exists(projectDir))
        {
            SpectrePanel.Error($"Project not found: {projectDir}");
            return null;
        }
        RunNpmSetupAndStart(projectDir, "Antigravity Manager", null);
        return projectDir;

    }

    public static string? StartProxy()
    {
        var projectDir = System.IO.Path.Combine(AgBaseDir, "antigravity-claude-proxy");
        if (!Directory.Exists(projectDir))
        {
            SpectrePanel.Error($"Project not found: {projectDir}");
            return null;
        }
        AnsiConsole.MarkupLine("[cyan]🛸 Proxy env set (BASE_URL=localhost:8080)[/]");
        var env = new Dictionary<string, string?>
        {
            ["ANTHROPIC_BASE_URL"] = "http://localhost:8080",
            ["ANTHROPIC_AUTH_TOKEN"] = "test"
        }
        ;
        RunNpmSetupAndStart(projectDir, "Antigravity Proxy", env);
        return projectDir;

    }

    private static void RunNpmSetupAndStart(string projectDir, string label, IDictionary<string, string?>? env)
    {
        AnsiConsole.MarkupLine("[cyan][[1/2]] 📦 Checking dependencies...[/]");
        if (!Directory.Exists(System.IO.Path.Combine(projectDir, "node_modules")))
        {
            AnsiConsole.MarkupLine("[yellow] -> Installing (npm install)...[/]");
            RunNpm("install", projectDir, env);
        }
        else
        {
            AnsiConsole.MarkupLine("[green] -> node_modules OK.[/]");
        }
        AnsiConsole.MarkupLine($"[green][[2/2]] 🚀 Launching {label.EscapeMarkup()}...[/]");
        RunNpm("start", projectDir, env);

    }

    private static void RunNpm(string args, string workingDir, IDictionary<string, string?>? env)
    {
        Helpers.ProcessRunner.Run("npm.cmd", args, workingDir);
    }

}
public static class AgyAccountDisplay
{
    public static void ShowQuotaChart(string accountName)
    {
        var quota = AgyAccountCore.CalculateRollingQuotas(accountName);
        AnsiConsole.Write(new Rule($"[bold cyan]Quota: {accountName.EscapeMarkup()}[/]").RuleStyle("grey"));
        var chart = new BarChart().Width(60).Label($"[bold]Remaining Quota % — {accountName.EscapeMarkup()}[/]").CenterLabel().AddItem("Gemini Weekly", quota.RemainingWeekly, Color.Cyan1).AddItem("Gemini 5-Hour", quota.Remaining5H, Color.Yellow).AddItem("Claude Weekly", 100.0, Color.Green).AddItem("Claude 5-Hour", 100.0, Color.Blue);
        AnsiConsole.Write(chart);
        AnsiConsole.MarkupLine($"[dim] Weekly : {quota.CountWeekly,4} / 1000 requests · Refreshes in {quota.TimeWeekly}[/]");
        AnsiConsole.MarkupLine($"[dim] 5-Hour : {quota.Count5H,4} / 50 requests · Refreshes in {quota.Time5H}[/]");

    }

    public static void ShowAccountTree()
    {
        var accounts = AgyAccountCore.GetAccounts();
        var active = AgyAccountCore.GetActiveAccount();
        var tree = new Tree("[bold cyan]AGY Accounts[/]");
        foreach (var acc in accounts)
        {
            var stats = AgyAccountCore.GetAccountStats(acc);
            var label = acc == active ? $"[green bold]★ {acc.EscapeMarkup()} (Active)[/]" : acc.EscapeMarkup();
            var node = tree.AddNode(label);
            node.AddNode($"[dim]Login:[/] {(stats.TokenStatus == "Logged In" ? "[green]Logged In[/]" : "[red]Not Logged In[/]")}");
            node.AddNode($"[dim]Convos:[/] {stats.ConversationsCount} [dim]Skills:[/] {stats.SkillsCount}");
            node.AddNode($"[dim]Weekly:[/] {(int)Math.Round(stats.GeminiWeekly)}% [dim]5h:[/] {(int)Math.Round(stats.GeminiFiveHour)}%");
            node.AddNode($"[dim]Size:[/] {stats.PrivateSize} [dim]Junctions:[/] {stats.JunctionStatus.EscapeMarkup()}");
        }
        AnsiConsole.Write(tree);

    }

    public static string[] MultiSelectAccounts(string prompt = "Select accounts:")
    {
        var accounts = AgyAccountCore.GetAccounts();
        if (accounts.Length == 0)
        {
            SpectrePanel.Warning("No accounts found.");
            return [];
        }
        try
        {
            return [.. AnsiConsole.Prompt(new MultiSelectionPrompt<string>().Title(prompt).PageSize(12).HighlightStyle(new Style(Color.Green)).InstructionsText("[grey](Space to select · Enter to confirm)[/]").AddChoices(accounts))];
        }
        catch
        {
            return [];
        }

    }

    public static void BulkAccountOperation(string label, string selectPrompt, Action<string> action)
    {
        var selected = MultiSelectAccounts(selectPrompt);
        if (selected.Length == 0)
        {
            SpectrePanel.Info("Nothing selected.");
            return;
        }
        SpectreProgress.BulkProgress(label, selected, (_, acc) => action(acc));

    }

}
