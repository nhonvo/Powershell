namespace AgyTui.Infrastructure.Persistence.DbContext;

using AgyTui.Infrastructure.Common;
using AgyTui.Infrastructure.Persistence.Interfaces;
using Microsoft.Data.Sqlite;

public class SqliteDatabase : ISqliteDatabase
{
    public virtual string DbPath => Path.Combine(AppPaths.DataDir, "agytui.db");

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
        Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS app_config (
                section_name TEXT PRIMARY KEY,
                json_data TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS accounts (
                account_name TEXT PRIMARY KEY,
                email TEXT,
                is_active INTEGER NOT NULL DEFAULT 0,
                quota_status TEXT DEFAULT 'OK',
                last_used TEXT,
                usage_count INTEGER DEFAULT 0,
                request_history_json TEXT DEFAULT '[]',
                metadata_json TEXT DEFAULT '{}',
                updated_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS system_state (
                state_key TEXT PRIMARY KEY,
                state_value TEXT,
                updated_at TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }
}
