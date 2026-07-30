using AgyTui.Infrastructure.Configuration;
using AgyTui.Infrastructure.Di;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Infrastructure.Persistence.DbContext;

public class SqliteDatabase : ISqliteDatabase
{
    private readonly Func<SqliteMigrationEngine> _migrationEngineFactory;

    public SqliteDatabase(Func<SqliteMigrationEngine>? migrationEngineFactory = null)
    {
        _migrationEngineFactory = migrationEngineFactory ?? (() => new SqliteMigrationEngine(this));
    }

    public virtual string DbPath => Path.Combine(AppPaths.DataDir, EnvironmentProvider.DatabaseFileName);

    public SqliteConnection CreateConnection()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = DbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };
        var conn = new SqliteConnection(builder.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;";
        cmd.ExecuteNonQuery();
        return conn;
    }

    public void InitializeDatabase()
    {
        _migrationEngineFactory().ApplyMigrations();
    }
}

