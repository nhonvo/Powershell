namespace AgyTui.Infrastructure.Integrations.AgyClient;

using System.Text;
using System.Text.Json;
using Spectre.Console;

public interface IAgyTokenManager
{
    void BackupActiveToken(string accountName);
    void RestoreActiveToken(string accountName);
    void SyncActiveAccountWithKeyring(bool silent);
}

public class AgyTokenManager : IAgyTokenManager
{
    private string AgySourceHome => AgyAccountCore.AgySourceHome;

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
                    encrypted = TokenVault.Protect(token);
                }
                catch (System.Security.Cryptography.CryptographicException ex)
                {
                    SpectrePanel.Error($"DPAPI token encryption failed: {ex.Message}");
                    return;
                }
                File.WriteAllText(tokenFile, encrypted, Encoding.UTF8);
            }
        }
        catch (System.Security.Cryptography.CryptographicException ex)
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
                    var token = TokenVault.Unprotect(encrypted);
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
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini")
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
                            var savedToken = TokenVault.Unprotect(encrypted);
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
                var encryptedToken = TokenVault.Protect(keyringToken);
                File.WriteAllText(tokenFile, encryptedToken, Encoding.UTF8);
            }
            else
            {
                var accDir = AgyAccountCore.GetAccountDirectory(savedAcc);
                Directory.CreateDirectory(accDir);
                var tokenFile = Path.Combine(accDir, "keyring_token.txt");
                var encryptedToken = TokenVault.Protect(keyringToken);
                File.WriteAllText(tokenFile, encryptedToken, Encoding.UTF8);
            }
        }
        catch { }
    }
}
