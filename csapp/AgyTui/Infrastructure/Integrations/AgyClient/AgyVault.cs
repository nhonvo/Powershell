namespace AgyTui.Infrastructure.Integrations.AgyClient;

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Spectre.Console;

using AgyTui.Infrastructure.Integrations.AgyClient.Interfaces;

public class AgyVault : IAgyVault
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("AgyTui_Secure_Entropy_v1");
    private string AgySourceHome => AgyAccountCore.AgySourceHome;

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
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return string.Empty;
        }
    }

    public void BackupActiveToken(string accountName)
    {
        try
        {
            var accDir = AgyAccountCore.GetAccountDirectory(accountName);
            if (!Directory.Exists(accDir)) return;
            var token = AgyKeyringHelper.ReadToken("gemini:antigravity");
            if (!string.IsNullOrEmpty(token))
            {
                var tokenFile = Path.Combine(accDir, "keyring_token.txt");
                string encrypted;
                try
                {
                    encrypted = Protect(token);
                }
                catch (CryptographicException ex)
                {
                    SpectrePanel.Error($"DPAPI token encryption failed: {ex.Message}");
                    return;
                }
                File.WriteAllText(tokenFile, encrypted, Encoding.UTF8);
            }
        }
        catch (CryptographicException ex)
        {
            SpectrePanel.Error($"DPAPI token encryption failed: {ex.Message}");
        }
        catch { }
    }

    public void RestoreActiveToken(string accountName)
    {
        try
        {
            var accDir = AgyAccountCore.GetAccountDirectory(accountName);
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
            else
            {
                AgyKeyringHelper.DeleteToken("gemini:antigravity");
            }

            var targetHomeDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                AgySourceHome,
                AppPaths.GeminiHome
            };

            var credFiles = new[] { "google_accounts.json", "oauth_creds.json", "state.json", "keyring_token.txt" };
            foreach (var homeDir in targetHomeDirs)
            {
                if (string.Equals(homeDir, accDir, StringComparison.OrdinalIgnoreCase)) continue;
                Directory.CreateDirectory(homeDir);
                foreach (var f in credFiles)
                {
                    var src = Path.Combine(accDir, f);
                    var dest = Path.Combine(homeDir, f);
                    if (File.Exists(src))
                    {
                        try { File.Copy(src, dest, true); } catch { }
                    }
                    else if (File.Exists(dest))
                    {
                        try { File.Delete(dest); } catch { }
                    }
                }
            }
        }
        catch { }
    }

    public void SyncActiveAccountWithKeyring(bool silent)
    {
        if (!AgyAccountCore.IsAutoSwitchEnabled()) return;
        try
        {
            string savedAcc = AgyAccountCore.GetActiveAccount();
            string? keyringToken = AgyKeyringHelper.ReadToken("gemini:antigravity");
            if (string.IsNullOrEmpty(keyringToken)) return;

            string? matchedAcc = null;
            var availableAccounts = AgyAccountCore.GetAccounts();
            foreach (var acc in availableAccounts)
            {
                var accDir = AgyAccountCore.GetAccountDirectory(acc);
                var tokenFile = Path.Combine(accDir, "keyring_token.txt");
                if (File.Exists(tokenFile))
                {
                    try
                    {
                        var encrypted = File.ReadAllText(tokenFile).Trim();
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
                    if (AgyAccountCore.CheckNetworkStatus())
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
                    File.WriteAllText(AgyAccountCore.AgyActiveAccountFile, savedAcc, Encoding.UTF8);
                    Environment.SetEnvironmentVariable("GEMINI_HOME", AgyAccountCore.GetAccountDirectory(savedAcc));
                }
                var accDir = AgyAccountCore.GetAccountDirectory(savedAcc);
                Directory.CreateDirectory(accDir);
                var tokenFile = Path.Combine(accDir, "keyring_token.txt");
                var encryptedToken = Protect(keyringToken);
                File.WriteAllText(tokenFile, encryptedToken, Encoding.UTF8);
            }
            else
            {
                var accDir = AgyAccountCore.GetAccountDirectory(savedAcc);
                Directory.CreateDirectory(accDir);
                var tokenFile = Path.Combine(accDir, "keyring_token.txt");
                var encryptedToken = Protect(keyringToken);
                File.WriteAllText(tokenFile, encryptedToken, Encoding.UTF8);
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
    private static readonly IAgyVault _vault = new AgyVault();

    public static void SetSecret(string key, string value) => _vault.SetSecret(key, value);
    public static string? GetSecret(string key) => _vault.GetSecret(key);
    public static void ListSecrets() => _vault.ListSecrets();
    public static void RemoveSecret(string key) => _vault.RemoveSecret(key);
}

public static class TokenVault
{
    private static readonly IAgyVault _vault = new AgyVault();

    public static string Protect(string plainText) => _vault.Protect(plainText);
    public static string Unprotect(string cipherText) => _vault.Unprotect(cipherText);
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

    public static string? ReadToken(string target)
    {
        if (CredRead(target, 1, 0, out var credPtr))
        {
            try
            {
                var cred = Marshal.PtrToStructure<CREDENTIAL>(credPtr);
                if (cred.credentialBlob != IntPtr.Zero && cred.credentialBlobSize > 0)
                {
                    return Marshal.PtrToStringUni(cred.credentialBlob, cred.credentialBlobSize / 2);
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
        var bytes = Encoding.Unicode.GetBytes(token);
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
