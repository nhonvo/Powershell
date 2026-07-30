using Microsoft.Data.Sqlite;
using AgyTui.Infrastructure.Persistence.Interfaces;

namespace AgyTui.Infrastructure.Persistence;

public class SqliteMigrationEngine
{
    private readonly ISqliteDatabase _db;

    public SqliteMigrationEngine(ISqliteDatabase db)
    {
        _db = db;
    }

    public void ApplyMigrations()
    {
        var dir = Path.GetDirectoryName(_db.DbPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        using var conn = _db.CreateConnection();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS schema_migrations (
                    version INTEGER PRIMARY KEY,
                    applied_at_utc TEXT NOT NULL
                );
                """;
            cmd.ExecuteNonQuery();
        }

        int currentVersion = GetCurrentVersion(conn);
        var scripts = GetMigrationScripts();

        foreach (var script in scripts.Where(s => s.Version > currentVersion))
        {
            using var tx = conn.BeginTransaction();
            using var migrateCmd = conn.CreateCommand();
            migrateCmd.Transaction = tx;
            migrateCmd.CommandText = script.Sql;
            migrateCmd.ExecuteNonQuery();

            using var recordCmd = conn.CreateCommand();
            recordCmd.Transaction = tx;
            recordCmd.CommandText = "INSERT INTO schema_migrations (version, applied_at_utc) VALUES (@v, @dt);";
            recordCmd.Parameters.AddWithValue("@v", script.Version);
            recordCmd.Parameters.AddWithValue("@dt", DateTime.UtcNow.ToString("o"));
            recordCmd.ExecuteNonQuery();

            tx.Commit();
        }
    }

    public int GetCurrentVersion(SqliteConnection conn)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(MAX(version), 0) FROM schema_migrations;";
            var res = cmd.ExecuteScalar();
            return res != null && res != DBNull.Value ? Convert.ToInt32(res) : 0;
        }
        catch
        {
            return 0;
        }
    }

    public static List<(int Version, string Description, string Sql)> GetMigrationScripts()
    {
        return new List<(int, string, string)>
        {
            (1, "V1__InitialSchema", """
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
                """),

            (2, "V2__AddCommandInvocationLogs", """
                CREATE TABLE IF NOT EXISTS command_invocation_logs (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    alias TEXT NOT NULL,
                    timestamp_utc TEXT NOT NULL,
                    duration_ms REAL NOT NULL,
                    success INTEGER NOT NULL,
                    category TEXT,
                    account_name TEXT
                );
                """)
        };
    }
}
