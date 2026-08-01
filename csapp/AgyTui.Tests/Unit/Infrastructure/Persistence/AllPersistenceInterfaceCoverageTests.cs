using AgyTui.Infrastructure.Persistence.DbContext;
using AgyTui.Infrastructure.Persistence.Interfaces;
using AgyTui.Infrastructure.Persistence.Repositories;
using AgyTui.Infrastructure.Persistence.Seeding;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Persistence;

public class AllPersistenceInterfaceCoverageTests
{
    [Fact]
    public void IAgyAccountRepository_Methods_DeclaredAndCallable()
    {
        ISqliteDatabase db = new SqliteDatabase();
        IAgyAccountRepository repo = new SqliteAgyAccountRepository(db);

        var active = repo.GetActiveAccount();
        Assert.NotNull(active);

        var accounts = repo.GetAccounts();
        Assert.NotNull(accounts);
    }

    [Fact]
    public void IConfigRepository_Methods_DeclaredAndCallable()
    {
        ISqliteDatabase db = new SqliteDatabase();
        IConfigRepository repo = new SqliteConfigRepository(db);

        repo.SetState("test_cov_key", "test_cov_val");
        var val = repo.GetState("test_cov_key");
        Assert.Equal("test_cov_val", val);

        var cfg = repo.LoadConfig();
        Assert.NotNull(cfg);

        repo.SaveConfig(cfg);
    }

    [Fact]
    public void IWorkspaceRepository_Methods_DeclaredAndCallable()
    {
        ISqliteDatabase db = new SqliteDatabase();
        IWorkspaceRepository repo = new SqliteWorkspaceRepository(db);

        var workspaces = repo.GetAllWorkspaces();
        Assert.NotNull(workspaces);
    }

    [Fact]
    public void IMasterSeeder_ExecuteAllSeeders_ExecutesSuccessfully()
    {
        IMasterSeeder masterSeeder = new MasterSeeder(Array.Empty<ISeeder>());
        masterSeeder.ExecuteAllSeeders();
    }
}
