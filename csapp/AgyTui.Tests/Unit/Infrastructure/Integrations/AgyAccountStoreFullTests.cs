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
}
