using AgyTui.Infrastructure.Persistence;
using AgyTui.Tests.Mocks;
using Xunit;

namespace AgyTui.Tests.Integration;

public class SqlitePersistenceTests
{
    [Fact]
    public void ApplyMigrations_AppliesAllVersions_AndCreatesTables()
    {
        var db = new FakeSqliteDatabase();
        var engine = new SqliteMigrationEngine(db);

        engine.ApplyMigrations();

        using var conn = db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM schema_migrations;";
        var count = Convert.ToInt32(cmd.ExecuteScalar());

        Assert.True(count >= 2);
    }

    [Fact]
    public void EnvironmentIsolation_SelectsCorrectDbFileName()
    {
        try
        {
            Environment.SetEnvironmentVariable("ENVIRONMENT", "Development");
            var devDb = new AgyTui.Infrastructure.Persistence.DbContext.SqliteDatabase();
            Assert.EndsWith("agytui.dev.db", devDb.DbPath);

            Environment.SetEnvironmentVariable("ENVIRONMENT", "Production");
            var prodDb = new AgyTui.Infrastructure.Persistence.DbContext.SqliteDatabase();
            Assert.EndsWith("agytui.db", prodDb.DbPath);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ENVIRONMENT", null);
        }
    }
}
