using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AgyTui.Infrastructure.Integrations.AgyClient;

public class AgyVault : IAgyVault
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("AgyTui_Secure_Entropy_v1");
    private readonly IAgyAccountStore _accountStore;
    private readonly IAgyAccountRepository _accountRepo;

    public AgyVault(IAgyAccountStore accountStore, IAgyAccountRepository accountRepo)
    {
        _accountStore = accountStore;
        _accountRepo = accountRepo;
    }

    public AgyVault(IAgyAccountStore accountStore) : this(accountStore, new SqliteAgyAccountRepository(new SqliteDatabase())) { }

    public AgyVault() : this(new AgyAccountStore(), new SqliteAgyAccountRepository(new SqliteDatabase())) { }

    private string AgySourceHome => _accountStore.AgySourceHome;

    public string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;
        try
        {
            var data = Encoding.UTF8.GetBytes(plainText);
            var encrypted = ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }
        catch
        {
            return string.Empty;
        }
    }

    public string Unprotect(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return string.Empty;
        try
        {
            var data = Convert.FromBase64String(cipherText);
            var decrypted = ProtectedData.Unprotect(data, Entropy, DataProtectionScope.CurrentUser);
            return AgyKeyringHelper.DecodeTokenBytes(decrypted);
        }
        catch
        {
            return string.Empty;
        }
    }

    public EncryptedToken CreateEncryptedToken(string accountName, string plainText)
    {
        var cipherText = Protect(plainText);
        return new EncryptedToken(accountName, cipherText, DateTime.UtcNow);
    }

    public void BackupActiveToken(string accountName)
    {
        try
        {
            var accDir = _accountStore.GetAccountDirectory(accountName);
            if (!Directory.Exists(accDir)) Directory.CreateDirectory(accDir);

            var primaryDir = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? "", ".gemini");
            if (!Directory.Exists(primaryDir)) return;

            var expectedEmail = _accountStore.GetCanonicalEmail(accountName);
            var primaryGJson = Path.Combine(primaryDir, "google_accounts.json");

            bool primaryBelongsToAccount = false;
            if (File.Exists(primaryGJson))
            {
                try
                {
                    var jsonStr = File.ReadAllText(primaryGJson);
                    using var doc = JsonDocument.Parse(jsonStr);
                    if (doc.RootElement.TryGetProperty("activeAccount", out var accProp) && accProp.ValueKind == JsonValueKind.String)
                    {
                        var activeEmail = accProp.GetString()?.Trim() ?? "";
                        if (!string.IsNullOrEmpty(activeEmail) && !string.IsNullOrEmpty(expectedEmail) && string.Equals(activeEmail, expectedEmail, StringComparison.OrdinalIgnoreCase))
                        {
                            primaryBelongsToAccount = true;
                        }
                    }
                }
                catch { }
            }
            else if (string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase))
            {
                primaryBelongsToAccount = true;
            }

            if (!primaryBelongsToAccount) return;

            var token = AgyKeyringHelper.ReadToken("gemini:antigravity");
            string? encryptedToken = null;
            if (!string.IsNullOrEmpty(token))
            {
                encryptedToken = Protect(token);
                File.WriteAllText(Path.Combine(accDir, "keyring_token.txt"), encryptedToken, Utf8NoBom);
            }

            if (!string.Equals(accDir, primaryDir, StringComparison.OrdinalIgnoreCase))
            {
                var filesToSync = new[]
                {
                    "google_accounts.json", "oauth_creds.json", "state.json", "installation_id", "keyring_token.txt",
                    Path.Combine("antigravity-cli", "settings.json"),
                    Path.Combine("antigravity-cli", "installation_id"),
                    Path.Combine("antigravity-cli", "keyring_token.txt")
                };
                foreach (var f in filesToSync)
                {
                    var src = Path.Combine(primaryDir, f);
                    var dst = Path.Combine(accDir, f);
                    if (File.Exists(src))
                    {
                        try
                        {
                            var parent = Path.GetDirectoryName(dst);
                            if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
                            File.Copy(src, dst, overwrite: true);
                        }
                        catch { }
                    }
                }
            }

            string? googleAcc = File.Exists(Path.Combine(accDir, "google_accounts.json")) ? File.ReadAllText(Path.Combine(accDir, "google_accounts.json")) : null;
            string? oauthCreds = File.Exists(Path.Combine(accDir, "oauth_creds.json")) ? File.ReadAllText(Path.Combine(accDir, "oauth_creds.json")) : null;
            string? stateJson = File.Exists(Path.Combine(accDir, "state.json")) ? File.ReadAllText(Path.Combine(accDir, "state.json")) : null;

            var creds = new AccountCredentials(accountName, encryptedToken, googleAcc, oauthCreds, stateJson, expectedEmail);
            _accountRepo.SaveAccountCredentials(creds);
        }
        catch { }
    }

    public void RestoreActiveToken(string accountName)
    {
        try
        {
            _accountStore.SanitizeAccountDirectory(accountName);

            var accDir = _accountStore.GetAccountDirectory(accountName);
            if (!Directory.Exists(accDir)) Directory.CreateDirectory(accDir);

            var primaryDir = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? "", ".gemini");
            Directory.CreateDirectory(primaryDir);
            var filesToSync = new[]
            {
                "google_accounts.json", "oauth_creds.json", "state.json", "installation_id", "keyring_token.txt",
                Path.Combine("antigravity-cli", "settings.json"),
                Path.Combine("antigravity-cli", "installation_id"),
                Path.Combine("antigravity-cli", "keyring_token.txt")
            };

            var diskTokenFile = Path.Combine(accDir, "keyring_token.txt");
            string? token = null;
            if (File.Exists(diskTokenFile))
            {
                var encrypted = File.ReadAllText(diskTokenFile).Trim();
                if (!string.IsNullOrEmpty(encrypted))
                {
                    token = Unprotect(encrypted);
                }
            }

            var dbCreds = _accountRepo.GetAccountCredentials(accountName);
            if (string.IsNullOrEmpty(token) && dbCreds != null && !string.IsNullOrEmpty(dbCreds.KeyringToken))
            {
                token = Unprotect(dbCreds.KeyringToken);
                File.WriteAllText(diskTokenFile, dbCreds.KeyringToken, Utf8NoBom);

                if (!File.Exists(Path.Combine(accDir, "google_accounts.json")) && !string.IsNullOrEmpty(dbCreds.GoogleAccountsJson))
                    File.WriteAllText(Path.Combine(accDir, "google_accounts.json"), dbCreds.GoogleAccountsJson, Utf8NoBom);

                if (!File.Exists(Path.Combine(accDir, "oauth_creds.json")) && !string.IsNullOrEmpty(dbCreds.OAuthCredsJson))
                    File.WriteAllText(Path.Combine(accDir, "oauth_creds.json"), dbCreds.OAuthCredsJson, Utf8NoBom);

                if (!File.Exists(Path.Combine(accDir, "state.json")) && !string.IsNullOrEmpty(dbCreds.StateJson))
                    File.WriteAllText(Path.Combine(accDir, "state.json"), dbCreds.StateJson, Utf8NoBom);
            }

            if (!string.Equals(accDir, primaryDir, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var f in filesToSync)
                {
                    var src = Path.Combine(accDir, f);
                    var dst = Path.Combine(primaryDir, f);
                    if (File.Exists(src))
                    {
                        try
                        {
                            var parent = Path.GetDirectoryName(dst);
                            if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
                            if (File.Exists(dst)) { try { File.SetAttributes(dst, FileAttributes.Normal); } catch { } }
                            File.Copy(src, dst, overwrite: true);
                        }
                        catch
                        {
                            try
                            {
                                Thread.Sleep(50);
                                if (File.Exists(dst)) { try { File.SetAttributes(dst, FileAttributes.Normal); } catch { } }
                                File.Copy(src, dst, overwrite: true);
                            }
                            catch { }
                        }
                    }
                    else if (File.Exists(dst))
                    {
                        try { File.Delete(dst); } catch { }
                    }
                }
            }

            if (!string.IsNullOrEmpty(token))
            {
                AgyKeyringHelper.WriteToken("gemini:antigravity", "antigravity", token);
            }
            else
            {
                AgyKeyringHelper.DeleteToken("gemini:antigravity");
            }
        }
        catch { }
    }

    public void SyncActiveAccountWithKeyring(bool silent)
    {
        try
        {
            string activeAcc = _accountStore.GetActiveAccount();
            string? keyringToken = AgyKeyringHelper.ReadToken("gemini:antigravity");
            var dbCreds = _accountRepo.GetAccountCredentials(activeAcc);

            if (string.IsNullOrEmpty(keyringToken))
            {
                if (dbCreds != null && !string.IsNullOrEmpty(dbCreds.KeyringToken))
                {
                    var token = Unprotect(dbCreds.KeyringToken);
                    if (!string.IsNullOrEmpty(token))
                    {
                        AgyKeyringHelper.WriteToken("gemini:antigravity", "antigravity", token);
                    }
                }
            }
            else
            {
                if (dbCreds == null || string.IsNullOrEmpty(dbCreds.KeyringToken))
                {
                    BackupActiveToken(activeAcc);
                }
            }
        }
        catch { }
    }

    public void SetSecret(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        var encrypted = Protect(value);
        AgyKeyringHelper.WriteToken($"agy:secret:{key}", "secret", encrypted);
        SpectrePanel.Success($"Secret '{key}' stored securely via DPAPI.");
    }

    public string? GetSecret(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        var encrypted = AgyKeyringHelper.ReadToken($"agy:secret:{key}");
        if (string.IsNullOrEmpty(encrypted)) return null;
        var val = Unprotect(encrypted);
        return string.IsNullOrWhiteSpace(val) ? null : val;
    }

    public void ListSecrets()
    {
        var secrets = AgyKeyringHelper.ListTokens("agy:secret:");
        if (secrets.Length == 0)
        {
            SpectrePanel.Info("No secrets stored in vault.");
            return;
        }
        AnsiConsole.MarkupLine("[bold cyan]Vault Secrets:[/]");
        foreach (var s in secrets)
        {
            var key = s.Replace("agy:secret:", "");
            AnsiConsole.MarkupLine($"  [dim]•[/] [bold]{key.EscapeMarkup()}[/]");
        }
    }

    public void RemoveSecret(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        AgyKeyringHelper.DeleteToken($"agy:secret:{key}");
        SpectrePanel.Success($"Secret '{key}' removed from vault.");
    }
}

public static class AgySecretVault
{
    private static IAgyVault? _instance;
    public static IAgyVault Instance
    {
        get => _instance ??= new AgyVault(new AgyAccountStore(new SqliteAgyAccountRepository(new SqliteDatabase()), new AppPathManager()), new SqliteAgyAccountRepository(new SqliteDatabase()));
        set => _instance = value;
    }

    public static void SetSecret(string key, string value) => Instance.SetSecret(key, value);
    public static string? GetSecret(string key) => Instance.GetSecret(key);
    public static void ListSecrets() => Instance.ListSecrets();
    public static void RemoveSecret(string key) => Instance.RemoveSecret(key);
}

public static class TokenVault
{
    private static IAgyVault? _instance;
    public static IAgyVault Instance
    {
        get => _instance ??= new AgyVault(new AgyAccountStore(new SqliteAgyAccountRepository(new SqliteDatabase()), new AppPathManager()), new SqliteAgyAccountRepository(new SqliteDatabase()));
        set => _instance = value;
    }

    public static string Protect(string plainText) => Instance.Protect(plainText);
    public static string Unprotect(string cipherText) => Instance.Unprotect(cipherText);
}

internal static class AgyKeyringHelper
{
    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credential);

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWrite([In] ref CREDENTIAL userCredential, uint flags);

    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDelete(string target, int type, int reservedFlag);

    [DllImport("advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
    private static extern void CredFree([In] IntPtr cred);

    [DllImport("advapi32.dll", EntryPoint = "CredEnumerateW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredEnumerate(string filter, int flags, out int count, out IntPtr credentials);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public int flags;
        public int type;
        public string targetName;
        public string comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME lastWritten;
        public int credentialBlobSize;
        public IntPtr credentialBlob;
        public int persist;
        public int attributeCount;
        public IntPtr attributes;
        public string targetAlias;
        public string userName;
    }

    public static string DecodeTokenBytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0) return string.Empty;

        if (bytes.Length >= 2 && bytes.Length % 2 == 0)
        {
            bool isUnicode = true;
            for (int i = 1; i < bytes.Length; i += 2)
            {
                if (bytes[i] != 0) { isUnicode = false; break; }
            }
            if (isUnicode)
            {
                return Encoding.Unicode.GetString(bytes);
            }
        }

        var utf8Str = Encoding.UTF8.GetString(bytes);
        if (utf8Str.Contains("\"token\"") || utf8Str.Contains("\"access_token\""))
        {
            return utf8Str;
        }

        var unicodeStr = Encoding.Unicode.GetString(bytes);
        if (unicodeStr.Contains("\"token\"") || unicodeStr.Contains("\"access_token\""))
        {
            return unicodeStr;
        }

        return utf8Str;
    }

    public static string? ReadToken(string target)
    {
        if (CredRead(target, 1, 0, out var credPtr))
        {
            try
            {
                var cred = Marshal.PtrToStructure<CREDENTIAL>(credPtr);
                if (cred.credentialBlob != IntPtr.Zero && cred.credentialBlobSize > 0)
                {
                    var bytes = new byte[cred.credentialBlobSize];
                    Marshal.Copy(cred.credentialBlob, bytes, 0, cred.credentialBlobSize);
                    return DecodeTokenBytes(bytes);
                }
            }
            finally
            {
                CredFree(credPtr);
            }
        }
        return null;
    }

    public static bool WriteToken(string target, string username, string token)
    {
        if (string.IsNullOrEmpty(token)) return false;
        var bytes = Encoding.UTF8.GetBytes(token);
        var blobPtr = Marshal.AllocHGlobal(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, blobPtr, bytes.Length);
            var cred = new CREDENTIAL
            {
                type = 1,
                targetName = target,
                userName = username,
                credentialBlob = blobPtr,
                credentialBlobSize = bytes.Length,
                persist = 2
            };
            CredDelete(target, 1, 0);
            return CredWrite(ref cred, 0);
        }
        finally
        {
            Marshal.FreeHGlobal(blobPtr);
        }
    }

    public static bool DeleteToken(string target)
    {
        bool d1 = CredDelete(target, 1, 0);
        bool d2 = CredDelete(target, 2, 0);
        bool d3 = CredDelete("LegacyGeneric:target=" + target, 1, 0);
        bool d4 = CredDelete("LegacyGeneric:target=" + target, 2, 0);
        try
        {
            using var proc1 = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmdkey",
                    Arguments = $"/delete:{target}",
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };
            proc1.Start();
            proc1.WaitForExit(1000);
        }
        catch { }

        try
        {
            using var proc2 = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmdkey",
                    Arguments = $"/delete:LegacyGeneric:target={target}",
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };
            proc2.Start();
            proc2.WaitForExit(1000);
        }
        catch { }

        return d1 || d2 || d3 || d4;
    }

    public static string[] ListTokens(string prefix)
    {
        var list = new List<string>();
        if (CredEnumerate(prefix + "*", 0, out var count, out var credsPtr))
        {
            try
            {
                for (int i = 0; i < count; i++)
                {
                    var ptr = Marshal.ReadIntPtr(credsPtr, i * IntPtr.Size);
                    var cred = Marshal.PtrToStructure<CREDENTIAL>(ptr);
                    if (!string.IsNullOrEmpty(cred.targetName))
                    {
                        list.Add(cred.targetName);
                    }
                }
            }
            finally
            {
                CredFree(credsPtr);
            }
        }
        return [.. list];
    }
}
