using AgyTui.Infrastructure.Persistence.DbContext;
using AgyTui.Infrastructure.Persistence.Interfaces;
using AgyTui.Infrastructure.Persistence.Repositories;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Persistence;

public class RepositoriesFullTests
{
    [Fact]
    public void SqliteConfigRepository_GetState_NonExistentKey_ReturnsNull()
    {
        ISqliteDatabase db = new SqliteDatabase();
        IConfigRepository repo = new SqliteConfigRepository(db);

        var val = repo.GetState("non_existent_key_xyz");
        Assert.Null(val);
    }

    [Fact]
    public void SqliteConfigRepository_SetStateAndGetState_RoundtripsSuccessfully()
    {
        ISqliteDatabase db = new SqliteDatabase();
        IConfigRepository repo = new SqliteConfigRepository(db);

        repo.SetState("test_key", "test_val");
        var val = repo.GetState("test_key");
        Assert.Equal("test_val", val);
    }

    [Fact]
    public void SqliteWorkspaceRepository_GetAllWorkspaces_ReturnsList()
    {
        ISqliteDatabase db = new SqliteDatabase();
        IWorkspaceRepository repo = new SqliteWorkspaceRepository(db);

        var workspaces = repo.GetAllWorkspaces();
        Assert.NotNull(workspaces);
    }

    [Fact]
    public void SqliteDatabase_CreateConnection_ReturnsConnection()
    {
        ISqliteDatabase db = new SqliteDatabase();
        using var conn = db.CreateConnection();
        Assert.NotNull(conn);
    }
}
