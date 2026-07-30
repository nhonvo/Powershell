using Microsoft.Data.Sqlite;

namespace AgyTui.Infrastructure.Persistence.DbContext;

public class SqliteDatabase : ISqliteDatabase
{
    public virtual string DbPath
    {
        get
        {
            var env = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            var dbFileName = string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase) ? "agytui.dev.db" : "agytui.db";
            return Path.Combine(AppPaths.DataDir, dbFileName);
        }
    }

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
        new SqliteMigrationEngine(this).ApplyMigrations();
    }
}
