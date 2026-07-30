using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using AgyTui.Domain.AccountContext;
using AgyTui.Infrastructure.Di;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Infrastructure.Integrations.AgyClient;

public class AgyVault : IAgyVault
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("AgyTui_Secure_Entropy_v1");
    private readonly IAgyAccountStore _accountStore;

    public AgyVault(IAgyAccountStore accountStore)
    {
        _accountStore = accountStore;
    }

    public AgyVault() : this(Bootstrapper.ServiceProvider.GetRequiredService<IAgyAccountStore>()) { }

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
            if (!Directory.Exists(accDir)) return;
            var token = AgyKeyringHelper.ReadToken("gemini:antigravity");
            if (!string.IsNullOrEmpty(token))
            {
                var encryptedToken = Protect(token);
                File.WriteAllText(Path.Combine(accDir, "keyring_token.txt"), encryptedToken, Encoding.UTF8);
            }
        }
        catch { }
    }

    public void RestoreActiveToken(string accountName)
    {
        try
        {
            var accDir = _accountStore.GetAccountDirectory(accountName);
            if (!Directory.Exists(accDir)) return;

            var tokenFile = Path.Combine(accDir, "keyring_token.txt");
            if (File.Exists(tokenFile))
            {
                var encrypted = File.ReadAllText(tokenFile).Trim();
                if (!string.IsNullOrEmpty(encrypted))
                {
                    var token = Unprotect(encrypted);
                    if (!string.IsNullOrEmpty(token))
                    {
                        AgyKeyringHelper.WriteToken("gemini:antigravity", "antigravity", token);
                    }
                }
            }

            var defaultGeminiDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini");
            if (Directory.Exists(defaultGeminiDir) && !string.Equals(accDir, defaultGeminiDir, StringComparison.OrdinalIgnoreCase))
            {
                var filesToSync = new[] { "google_accounts.json", "oauth_creds.json", "state.json", "installation_id", "keyring_token.txt" };
                foreach (var f in filesToSync)
                {
                    var src = Path.Combine(accDir, f);
                    var dst = Path.Combine(defaultGeminiDir, f);
                    if (File.Exists(src))
                    {
                        try { File.Copy(src, dst, overwrite: true); } catch { }
                    }
                }
            }
        }
        catch { }
    }

    public void SyncActiveAccountWithKeyring(bool silent)
    {
        if (!_accountStore.IsAutoSwitchEnabled()) return;
        try
        {
            string savedAcc = _accountStore.GetActiveAccount();
            string? keyringToken = AgyKeyringHelper.ReadToken("gemini:antigravity");
            if (string.IsNullOrEmpty(keyringToken)) return;

            string? matchedAcc = null;
            var availableAccounts = _accountStore.GetAccounts();
            foreach (var acc in availableAccounts)
            {
                var curAccDir = _accountStore.GetAccountDirectory(acc);
                var curTokenFile = Path.Combine(curAccDir, "keyring_token.txt");
                if (File.Exists(curTokenFile))
                {
                    try
                    {
                        var encrypted = File.ReadAllText(curTokenFile).Trim();
                        if (!string.IsNullOrEmpty(encrypted))
                        {
                            var savedToken = Unprotect(encrypted);
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
                    _accountStore.SetActiveAccount(savedAcc, false);
                }
            }

            var accDir = _accountStore.GetAccountDirectory(savedAcc);
            Directory.CreateDirectory(accDir);
            var tokenFile = Path.Combine(accDir, "keyring_token.txt");
            var encryptedToken = Protect(keyringToken);
            File.WriteAllText(tokenFile, encryptedToken, Encoding.UTF8);
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
        return Unprotect(encrypted);
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
    private static readonly Func<IAgyVault> _vaultFactory = () => Bootstrapper.ServiceProvider.GetRequiredService<IAgyVault>();

    public static void SetSecret(string key, string value) => _vaultFactory().SetSecret(key, value);
    public static string? GetSecret(string key) => _vaultFactory().GetSecret(key);
    public static void ListSecrets() => _vaultFactory().ListSecrets();
    public static void RemoveSecret(string key) => _vaultFactory().RemoveSecret(key);
}

public static class TokenVault
{
    private static readonly Func<IAgyVault> _vaultFactory = () => Bootstrapper.ServiceProvider.GetRequiredService<IAgyVault>();

    public static string Protect(string plainText) => _vaultFactory().Protect(plainText);
    public static string Unprotect(string cipherText) => _vaultFactory().Unprotect(cipherText);
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

        try
        {
            var recoveredBytes = new byte[unicodeStr.Length * 2];
            for (int i = 0; i < unicodeStr.Length; i++)
            {
                ushort c = unicodeStr[i];
                recoveredBytes[i * 2] = (byte)(c & 0xFF);
                recoveredBytes[i * 2 + 1] = (byte)(c >> 8);
            }
            var recoveredStr = Encoding.UTF8.GetString(recoveredBytes).TrimEnd('\0');
            if (recoveredStr.Contains("\"token\"") || recoveredStr.Contains("\"access_token\""))
            {
                return recoveredStr;
            }
        }
        catch { }

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
        return CredDelete(target, 1, 0);
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
