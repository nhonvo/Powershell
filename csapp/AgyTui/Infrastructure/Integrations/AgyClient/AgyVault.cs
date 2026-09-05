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
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var data = Encoding.UTF8.GetBytes(plainText);
                var encrypted = ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encrypted);
            }
            catch { }
        }
        return AesProtect(plainText);
    }

    public string Unprotect(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return string.Empty;
        var trimmed = cipherText.Trim();

        if (trimmed.StartsWith("ya29") || trimmed.StartsWith("AIza") || trimmed.StartsWith("{") || trimmed.StartsWith("ey"))
        {
            return trimmed;
        }

        if (OperatingSystem.IsWindows())
        {
            try
            {
                var data = Convert.FromBase64String(trimmed);
                var decrypted = ProtectedData.Unprotect(data, Entropy, DataProtectionScope.CurrentUser);
                var result = AgyKeyringHelper.DecodeTokenBytes(decrypted);
                if (!string.IsNullOrEmpty(result)) return result;
            }
            catch { }
        }

        var aesDecrypted = AesUnprotect(trimmed);
        if (!string.IsNullOrEmpty(aesDecrypted)) return aesDecrypted;

        if (trimmed.Length >= 8 && !trimmed.Contains("\n"))
        {
            return trimmed;
        }

        return string.Empty;
    }

    private static string GetUserProfileDir()
    {
        return AppPaths.UserProfileDir;
    }

    private static byte[] GetPlatformKey()
    {
        var userProfile = GetUserProfileDir();

        var seed = $"{Environment.UserName}@{Environment.MachineName}:{userProfile}";
        using var kdf = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(seed), Entropy, 10000, HashAlgorithmName.SHA256);
        return kdf.GetBytes(32);
    }

    private static string AesProtect(string plainText)
    {
        try
        {
            var key = GetPlatformKey();
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            var iv = aes.IV;

            using var ms = new MemoryStream();
            ms.Write(iv, 0, iv.Length);
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                cs.Write(plainBytes, 0, plainBytes.Length);
                cs.FlushFinalBlock();
            }
            return Convert.ToBase64String(ms.ToArray());
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string AesUnprotect(string cipherText)
    {
        try
        {
            var key = GetPlatformKey();
            var fullBytes = Convert.FromBase64String(cipherText);
            if (fullBytes.Length <= 16) return string.Empty;

            using var aes = Aes.Create();
            aes.Key = key;
            var iv = new byte[16];
            Buffer.BlockCopy(fullBytes, 0, iv, 0, 16);
            aes.IV = iv;

            using var ms = new MemoryStream(fullBytes, 16, fullBytes.Length - 16);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var reader = new StreamReader(cs, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetPrimaryGeminiDir()
    {
        var userProfile = GetUserProfileDir();
        return Path.Combine(userProfile, ".gemini");
    }

    public EncryptedToken CreateEncryptedToken(string accountName, string plainText)
    {
        var cipherText = Protect(plainText);
        return new EncryptedToken(accountName, cipherText, DateTime.UtcNow);
    }

    private static void MirrorDirectory(string srcDir, string dstDir)
    {
        if (!Directory.Exists(srcDir)) return;
        Directory.CreateDirectory(dstDir);

        var srcFiles = Directory.GetFiles(srcDir, "*", SearchOption.AllDirectories);
        var srcFileSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var srcFile in srcFiles)
        {
            var relPath = Path.GetRelativePath(srcDir, srcFile);
            srcFileSet.Add(relPath);

            if (relPath.StartsWith(".keyring", StringComparison.OrdinalIgnoreCase) ||
                relPath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) ||
                relPath.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var dstFile = Path.Combine(dstDir, relPath);
            var parent = Path.GetDirectoryName(dstFile);
            if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);

            try
            {
                if (File.Exists(dstFile)) File.SetAttributes(dstFile, FileAttributes.Normal);
                File.Copy(srcFile, dstFile, overwrite: true);
            }
            catch { }
        }

        if (Directory.Exists(dstDir))
        {
            var dstFiles = Directory.GetFiles(dstDir, "*", SearchOption.AllDirectories);
            foreach (var dstFile in dstFiles)
            {
                var relPath = Path.GetRelativePath(dstDir, dstFile);
                if (relPath.StartsWith(".keyring", StringComparison.OrdinalIgnoreCase) ||
                    relPath.Equals("active_account.txt", StringComparison.OrdinalIgnoreCase) ||
                    relPath.EndsWith(".db", StringComparison.OrdinalIgnoreCase) ||
                    relPath.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!srcFileSet.Contains(relPath))
                {
                    try
                    {
                        File.SetAttributes(dstFile, FileAttributes.Normal);
                        File.Delete(dstFile);
                    }
                    catch { }
                }
            }
        }
    }

    public void BackupActiveToken(string accountName)
    {
        try
        {
            var accDir = _accountStore.GetAccountDirectory(accountName);
            if (!Directory.Exists(accDir)) Directory.CreateDirectory(accDir);

            var primaryDir = GetPrimaryGeminiDir();
            if (!Directory.Exists(primaryDir)) return;

            var primaryGJson = Path.Combine(primaryDir, "google_accounts.json");
            string? discoveredEmail = null;

            if (File.Exists(primaryGJson))
            {
                try
                {
                    var jsonStr = File.ReadAllText(primaryGJson);
                    using var doc = JsonDocument.Parse(jsonStr);
                    if (doc.RootElement.TryGetProperty("activeAccount", out var accProp) && accProp.ValueKind == JsonValueKind.String)
                    {
                        var em = accProp.GetString()?.Trim();
                        if (!string.IsNullOrEmpty(em) && em.Contains("@"))
                        {
                            discoveredEmail = em;
                        }
                    }
                }
                catch { }
            }

            var expectedEmail = !string.IsNullOrEmpty(discoveredEmail)
                ? discoveredEmail
                : _accountStore.GetCanonicalEmail(accountName);

            string? token = null;
            var pTok1 = Path.Combine(primaryDir, "antigravity-cli", "antigravity-oauth-token");
            var pTok2 = Path.Combine(primaryDir, "antigravity-oauth-token");
            if (File.Exists(pTok1)) token = File.ReadAllText(pTok1).Trim();
            else if (File.Exists(pTok2)) token = File.ReadAllText(pTok2).Trim();

            if (string.IsNullOrEmpty(token))
            {
                token = AgyKeyringHelper.ReadToken("gemini:antigravity");
            }
            if (string.IsNullOrEmpty(token))
            {
                var localTok = Path.Combine(accDir, "antigravity-cli", "antigravity-oauth-token");
                if (File.Exists(localTok)) token = File.ReadAllText(localTok).Trim();
            }
            if (string.IsNullOrEmpty(token))
            {
                var localTok2 = Path.Combine(accDir, "antigravity-oauth-token");
                if (File.Exists(localTok2)) token = File.ReadAllText(localTok2).Trim();
            }

            string? encryptedToken = null;
            if (!string.IsNullOrEmpty(token))
            {
                encryptedToken = Protect(token);
                File.WriteAllText(Path.Combine(accDir, "keyring_token.txt"), encryptedToken, Utf8NoBom);
                File.WriteAllText(Path.Combine(primaryDir, "keyring_token.txt"), encryptedToken, Utf8NoBom);

                var t1 = Path.Combine(accDir, "antigravity-cli", "antigravity-oauth-token");
                var t2 = Path.Combine(accDir, "antigravity-oauth-token");
                var t3 = Path.Combine(primaryDir, "antigravity-cli", "antigravity-oauth-token");
                var t4 = Path.Combine(primaryDir, "antigravity-oauth-token");
                Directory.CreateDirectory(Path.GetDirectoryName(t1)!);
                Directory.CreateDirectory(Path.GetDirectoryName(t3)!);
                File.WriteAllText(t1, token, Utf8NoBom);
                File.WriteAllText(t2, token, Utf8NoBom);
                File.WriteAllText(t3, token, Utf8NoBom);
                File.WriteAllText(t4, token, Utf8NoBom);

                var kHash = "7407b4ddbbd1bfbf2dce30edc9115b02dd294ffb233a1e05d28b98df241bc386.key";
                var kDir1 = Path.Combine(accDir, ".keyring");
                var kDir2 = Path.Combine(primaryDir, ".keyring");
                Directory.CreateDirectory(kDir1);
                Directory.CreateDirectory(kDir2);
                var keyContent = $"gemini:antigravity\n{token}";
                File.WriteAllText(Path.Combine(kDir1, kHash), keyContent, Utf8NoBom);
                File.WriteAllText(Path.Combine(kDir2, kHash), keyContent, Utf8NoBom);
            }

            if (!string.Equals(accDir, primaryDir, StringComparison.OrdinalIgnoreCase))
            {
                MirrorDirectory(primaryDir, accDir);
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

            var primaryDir = GetPrimaryGeminiDir();
            Directory.CreateDirectory(primaryDir);

            var diskTokenFile = Path.Combine(accDir, "keyring_token.txt");
            string? token = null;
            if (File.Exists(diskTokenFile))
            {
                var raw = File.ReadAllText(diskTokenFile).Trim();
                if (!string.IsNullOrEmpty(raw))
                {
                    token = Unprotect(raw);
                    if (string.IsNullOrEmpty(token)) token = raw;
                }
            }

            if (string.IsNullOrEmpty(token))
            {
                var linuxTok1 = Path.Combine(accDir, "antigravity-cli", "antigravity-oauth-token");
                var linuxTok2 = Path.Combine(accDir, "antigravity-oauth-token");
                if (File.Exists(linuxTok1))
                {
                    var raw = File.ReadAllText(linuxTok1).Trim();
                    if (!string.IsNullOrEmpty(raw)) token = raw;
                }
                else if (File.Exists(linuxTok2))
                {
                    var raw = File.ReadAllText(linuxTok2).Trim();
                    if (!string.IsNullOrEmpty(raw)) token = raw;
                }
            }

            var dbCreds = _accountRepo.GetAccountCredentials(accountName);
            if (string.IsNullOrEmpty(token) && dbCreds != null && !string.IsNullOrEmpty(dbCreds.KeyringToken))
            {
                token = Unprotect(dbCreds.KeyringToken);
                if (string.IsNullOrEmpty(token)) token = dbCreds.KeyringToken;
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
                MirrorDirectory(accDir, primaryDir);
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

    private static string GetUserProfileDir() => AppPaths.UserProfileDir;

    private static string GetKeyringDir()
    {
        var userProfile = GetUserProfileDir();

        var dir = Path.Combine(userProfile, ".gemini", ".keyring");
        if (!Directory.Exists(dir))
        {
            try { Directory.CreateDirectory(dir); } catch { }
        }
        return dir;
    }

    private static string GetTargetFilePath(string target)
    {
        var safeFileName = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(target))) + ".key";
        return Path.Combine(GetKeyringDir(), safeFileName);
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
        if (OperatingSystem.IsWindows())
        {
            try
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
                            var decoded = DecodeTokenBytes(bytes);
                            if (!string.IsNullOrEmpty(decoded)) return decoded;
                        }
                    }
                    finally
                    {
                        CredFree(credPtr);
                    }
                }
            }
            catch { }
        }

        // File-based keyring fallback / non-Windows storage
        try
        {
            var filePath = GetTargetFilePath(target);
            if (File.Exists(filePath))
            {
                var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                if (lines.Length > 1)
                {
                    return string.Join("\n", lines.Skip(1));
                }
            }
        }
        catch { }

        // If target is gemini:antigravity, probe standard Linux/macOS token file locations
        if (string.Equals(target, "gemini:antigravity", StringComparison.OrdinalIgnoreCase))
        {
            var userProfile = GetUserProfileDir();

            var candidates = new[]
            {
                Path.Combine(userProfile, ".gemini", "antigravity-cli", "antigravity-oauth-token"),
                Path.Combine(userProfile, ".gemini", "antigravity-oauth-token"),
                Path.Combine(userProfile, ".gemini", "keyring_token.txt"),
                Path.Combine(AppPaths.GeminiHome, "antigravity-cli", "antigravity-oauth-token"),
                Path.Combine(AppPaths.GeminiHome, "antigravity-oauth-token"),
                Path.Combine(AppPaths.GeminiHome, "keyring_token.txt")
            };

            foreach (var candidate in candidates)
            {
                try
                {
                    if (File.Exists(candidate) && new FileInfo(candidate).Length > 0)
                    {
                        var text = File.ReadAllText(candidate, Encoding.UTF8).Trim();
                        if (!string.IsNullOrEmpty(text))
                        {
                            return text;
                        }
                    }
                }
                catch { }
            }
        }

        return null;
    }

    public static bool WriteToken(string target, string username, string token)
    {
        if (string.IsNullOrEmpty(token)) return false;
        bool windowsSuccess = false;

        if (OperatingSystem.IsWindows())
        {
            try
            {
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
                    windowsSuccess = CredWrite(ref cred, 0);
                }
                finally
                {
                    Marshal.FreeHGlobal(blobPtr);
                }
            }
            catch { }
        }

        // Always save to file-based vault (for non-Windows or fallback)
        try
        {
            var filePath = GetTargetFilePath(target);
            var content = target + "\n" + token;
            File.WriteAllText(filePath, content, Encoding.UTF8);

            if (string.Equals(target, "gemini:antigravity", StringComparison.OrdinalIgnoreCase))
            {
                var userProfile = GetUserProfileDir();

                var primaryDir = Path.Combine(userProfile, ".gemini");
                var cliDir = Path.Combine(primaryDir, "antigravity-cli");
                Directory.CreateDirectory(cliDir);

                File.WriteAllText(Path.Combine(cliDir, "antigravity-oauth-token"), token, Encoding.UTF8);
                File.WriteAllText(Path.Combine(primaryDir, "antigravity-oauth-token"), token, Encoding.UTF8);

                var envGemini = Environment.GetEnvironmentVariable("GEMINI_HOME");
                if (!string.IsNullOrEmpty(envGemini) && Directory.Exists(envGemini) && !string.Equals(envGemini, primaryDir, StringComparison.OrdinalIgnoreCase))
                {
                    var targetCli = Path.Combine(envGemini, "antigravity-cli");
                    Directory.CreateDirectory(targetCli);
                    File.WriteAllText(Path.Combine(targetCli, "antigravity-oauth-token"), token, Encoding.UTF8);
                    File.WriteAllText(Path.Combine(envGemini, "antigravity-oauth-token"), token, Encoding.UTF8);
                }
            }

            return true;
        }
        catch { }

        return windowsSuccess;
    }

    public static bool DeleteToken(string target)
    {
        bool deleted = false;
        if (OperatingSystem.IsWindows())
        {
            try
            {
                bool d1 = CredDelete(target, 1, 0);
                bool d2 = CredDelete(target, 2, 0);
                bool d3 = CredDelete("LegacyGeneric:target=" + target, 1, 0);
                bool d4 = CredDelete("LegacyGeneric:target=" + target, 2, 0);
                deleted = d1 || d2 || d3 || d4;

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
            }
            catch { }
        }

        try
        {
            var filePath = GetTargetFilePath(target);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                deleted = true;
            }

            if (string.Equals(target, "gemini:antigravity", StringComparison.OrdinalIgnoreCase))
            {
                var userProfile = GetUserProfileDir();

                var primaryDir = Path.Combine(userProfile, ".gemini");
                var f1 = Path.Combine(primaryDir, "antigravity-cli", "antigravity-oauth-token");
                var f2 = Path.Combine(primaryDir, "antigravity-oauth-token");
                if (File.Exists(f1)) { try { File.Delete(f1); deleted = true; } catch { } }
                if (File.Exists(f2)) { try { File.Delete(f2); deleted = true; } catch { } }

                var envGemini = Environment.GetEnvironmentVariable("GEMINI_HOME");
                if (!string.IsNullOrEmpty(envGemini) && Directory.Exists(envGemini) && !string.Equals(envGemini, primaryDir, StringComparison.OrdinalIgnoreCase))
                {
                    var gf1 = Path.Combine(envGemini, "antigravity-cli", "antigravity-oauth-token");
                    var gf2 = Path.Combine(envGemini, "antigravity-oauth-token");
                    if (File.Exists(gf1)) { try { File.Delete(gf1); deleted = true; } catch { } }
                    if (File.Exists(gf2)) { try { File.Delete(gf2); deleted = true; } catch { } }
                }
            }
        }
        catch { }

        return deleted;
    }

    public static string[] ListTokens(string prefix)
    {
        var list = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (OperatingSystem.IsWindows())
        {
            try
            {
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
            }
            catch { }
        }

        try
        {
            var dir = GetKeyringDir();
            if (Directory.Exists(dir))
            {
                foreach (var file in Directory.GetFiles(dir, "*.key"))
                {
                    try
                    {
                        using var reader = new StreamReader(file, Encoding.UTF8);
                        var targetName = reader.ReadLine()?.Trim();
                        if (!string.IsNullOrEmpty(targetName) && targetName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            list.Add(targetName);
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

        return [.. list];
    }
}
