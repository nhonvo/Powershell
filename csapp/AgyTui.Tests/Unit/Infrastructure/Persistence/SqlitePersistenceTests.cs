using AgyTui.Infrastructure.Persistence.DbContext;
using AgyTui.Infrastructure.Persistence.Repositories;

namespace AgyTui.Tests.Unit.Infrastructure.Persistence;

public class SqlitePersistenceTests
{
    [Fact]
    public void SqliteConfigRepository_SaveAndLoad_RoundtripsSuccessfully()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "agy_sqlite_cfg_test_" + Path.GetRandomFileName() + ".db");
        try
        {
            var db = new TestSqliteDatabase(dbPath);
            var repo = new SqliteConfigRepository(db);

            var config = new ConfigData();
            config.Ui.Mode = "three-pane";
            config.Ui.Density = "compact";
            config.Ai.ProviderMode = "ollama";

            repo.SaveConfig(config);
            var loaded = repo.LoadConfig();

            Assert.Equal("three-pane", loaded.Ui.Mode);
            Assert.Equal("compact", loaded.Ui.Density);
            Assert.Equal("ollama", loaded.Ai.ProviderMode);
        }
        finally
        {
            if (File.Exists(dbPath)) try { File.Delete(dbPath); } catch { }
        }
    }

    [Fact]
    public void SqliteAgyAccountRepository_SetActiveAndGet_RoundtripsSuccessfully()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "agy_sqlite_acc_test_" + Path.GetRandomFileName() + ".db");
        try
        {
            var db = new TestSqliteDatabase(dbPath);
            var repo = new SqliteAgyAccountRepository(db);

            repo.AddAccount("testuser@gmail.com", "testuser@gmail.com");
            repo.SetActiveAccount("testuser@gmail.com");

            var active = repo.GetActiveAccount();
            Assert.Equal("testuser@gmail.com", active);

            var accounts = repo.GetAccounts();
            Assert.Contains("testuser@gmail.com", accounts);
        }
        finally
        {
            if (File.Exists(dbPath)) try { File.Delete(dbPath); } catch { }
        }
    }

    private sealed class TestSqliteDatabase : SqliteDatabase
    {
        private readonly string _path;
        public TestSqliteDatabase(string path) => _path = path;
        public override string DbPath => _path;
    }
}
