namespace AgyTui.Tests.Unit.Infrastructure.Integrations.AgyClient;

[Collection("Sequential")]
public class AgyVaultTests
{
    [Fact]
    public void Vault_CanProtectAndUnprotectString()
    {
        var vault = new AgyVault();
        var original = "test_secret_payload_123";
        var encrypted = vault.Protect(original);
        var decrypted = vault.Unprotect(encrypted);
        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Vault_BackupAndRestoreActiveToken_PersistsLoginAcrossSwitches()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "agy_vault_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var originalUserProfile = Environment.GetEnvironmentVariable("USERPROFILE");
        var db = new AgyTui.Infrastructure.Persistence.DbContext.SqliteDatabase();
        var repo = new AgyTui.Infrastructure.Persistence.Repositories.SqliteAgyAccountRepository(db);

        try
        {
            Environment.SetEnvironmentVariable("USERPROFILE", tempRoot);
            var primaryDir = Path.Combine(tempRoot, ".gemini");
            var primaryCli = Path.Combine(primaryDir, "antigravity-cli");
            Directory.CreateDirectory(primaryCli);

            // Simulate user logged in with custom email under 'acc1'
            var userToken = "ya29.sample_oauth_token_payload_xyz";
            File.WriteAllText(Path.Combine(primaryCli, "antigravity-oauth-token"), userToken);
            File.WriteAllText(Path.Combine(primaryDir, "google_accounts.json"), "{\"accounts\":[{\"email\":\"john.doe@example.com\"}],\"activeAccount\":\"john.doe@example.com\"}");

            var store = new AgyAccountStore(repo);
            var vault = new AgyVault(store, repo);

            // Backup active account 'acc1'
            vault.BackupActiveToken("acc1");

            // Verify acc1 directory received the token and google_accounts.json
            var acc1Dir = store.GetAccountDirectory("acc1");
            Assert.True(Directory.Exists(acc1Dir));
            var savedToken = Path.Combine(acc1Dir, "antigravity-cli", "antigravity-oauth-token");
            Assert.True(File.Exists(savedToken));
            Assert.Equal(userToken, File.ReadAllText(savedToken));

            // Verify db saved the real discovered email
            var creds = repo.GetAccountCredentials("acc1");
            Assert.NotNull(creds);
            Assert.Equal("john.doe@example.com", creds.Email);

            // Now switch to 'acc2' (which is empty)
            vault.RestoreActiveToken("acc2");

            // Now switch back to 'acc1'
            vault.RestoreActiveToken("acc1");

            // Verify primary directory has restored token and login
            Assert.True(File.Exists(Path.Combine(primaryCli, "antigravity-oauth-token")));
            Assert.Equal(userToken, File.ReadAllText(Path.Combine(primaryCli, "antigravity-oauth-token")));
        }
        finally
        {
            Environment.SetEnvironmentVariable("USERPROFILE", originalUserProfile);
            try { repo.DeleteAccount("acc1"); } catch { }
            try { repo.DeleteAccount("acc2"); } catch { }
            try { Directory.Delete(tempRoot, true); } catch { }
        }
    }
}
