using AgyTui.Infrastructure.Integrations.AgyClient;
using AgyTui.Infrastructure.Integrations.AgyClient.Interfaces;
using AgyTui.Infrastructure.Persistence.DbContext;
using AgyTui.Infrastructure.Persistence.Interfaces;
using AgyTui.Infrastructure.Persistence.Repositories;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

public class AgyAccountStoreFullTests
{
    [Fact]
    public void IAgyAccountRepository_GetAccountMetadata_NonExistentAccount_ReturnsDefault()
    {
        ISqliteDatabase db = new SqliteDatabase();
        IAgyAccountRepository repo = new SqliteAgyAccountRepository(db);

        var meta = repo.GetAccountMetadata("non_existent_acc_123");
        Assert.NotNull(meta);

        repo.DeleteAccount("non_existent_acc_123");
    }

    [Fact]
    public void IAgyVault_CreateEncryptedToken_ReturnsToken()
    {
        IAgyVault vault = new AgyVault();
        var token = vault.CreateEncryptedToken("default", "secret_token_data");
        Assert.NotNull(token);

        vault.BackupActiveToken("default");
        vault.RestoreActiveToken("default");
        vault.SyncActiveAccountWithKeyring(true);
        vault.ListSecrets();
    }

    [Fact]
    public void IAgyQuotaEngine_CalculateRollingQuotas_ReturnsMetrics()
    {
        var db = new SqliteDatabase();
        var repo = new SqliteAgyAccountRepository(db);
        var pathManager = new AgyTui.Infrastructure.Services.AppPathManager();
        var accountStore = new AgyAccountStore(repo, pathManager);

        IAgyQuotaEngine engine = new AgyQuotaEngine(accountStore);
        var metrics = engine.CalculateRollingQuotas("default");
        Assert.NotNull(metrics);

        var agentMetrics = engine.CalculateRollingQuotasForAgent("gemini");
        Assert.NotNull(agentMetrics);

        var forecast = engine.GetQuotaReleaseForecast("default");
        Assert.NotNull(forecast);

        var junction = engine.GetJunctionStatus("default");
        Assert.NotNull(junction);

        var stats = engine.GetAccountStats("default");
        Assert.NotNull(stats);

        engine.TriggerLowQuotaWebhook("default", 10.0);
        engine.SetAccountQuotaMetrics("default", 100.0, 50.0);
    }

    [Fact]
    public void AgyAccountStore_SanitizeAccountDirectory_FixesCorruptedFolder()
    {
        var db = new SqliteDatabase();
        var repo = new SqliteAgyAccountRepository(db);
        var pathManager = new AgyTui.Infrastructure.Services.AppPathManager();
        var accountStore = new AgyAccountStore(repo, pathManager);

        var accName = "test_unit_sanitize_tmp";
        var email = accountStore.GetCanonicalEmail(accName);
        Assert.Equal($"{accName}@gmail.com", email);

        var accDir = accountStore.GetAccountDirectory(accName);
        try
        {
            System.IO.Directory.CreateDirectory(accDir);

            // Intentionally pollute folder with wrong activeAccount
            var corruptedJson = "{\n  \"accounts\": [ { \"email\": \"fptvttnhon2020@gmail.com\" } ],\n  \"activeAccount\": \"fptvttnhon2020@gmail.com\"\n}";
            System.IO.File.WriteAllText(System.IO.Path.Combine(accDir, "google_accounts.json"), corruptedJson);

            // Sanitize should clean corruption and reset to expected email
            accountStore.SanitizeAccountDirectory(accName);

            var fixedEmail = accountStore.GetAccountEmail(accName);
            Assert.Equal($"{accName}@gmail.com", fixedEmail);
        }
        finally
        {
            if (System.IO.Directory.Exists(accDir))
            {
                try { System.IO.Directory.Delete(accDir, true); } catch {}
            }
            try { repo.DeleteAccount(accName); } catch {}
        }
    }
}
