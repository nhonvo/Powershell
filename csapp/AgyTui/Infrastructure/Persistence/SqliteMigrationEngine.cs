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
                """),

            (3, "V3__DomainDbStorage", """
                CREATE TABLE IF NOT EXISTS workspaces (
                    name TEXT PRIMARY KEY NOT NULL,
                    workspace_path TEXT NOT NULL,
                    associated_account TEXT DEFAULT 'default',
                    tags_csv TEXT,
                    alias TEXT,
                    updated_at TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS flashcard_decks (
                    topic TEXT PRIMARY KEY NOT NULL,
                    cards_count INTEGER DEFAULT 0,
                    average_ease_factor REAL DEFAULT 2.5,
                    last_reviewed_utc TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS flashcards (
                    id TEXT PRIMARY KEY NOT NULL,
                    topic TEXT NOT NULL,
                    front TEXT NOT NULL,
                    back TEXT NOT NULL,
                    ease_factor REAL DEFAULT 2.5,
                    interval_days INTEGER DEFAULT 0,
                    repetitions INTEGER DEFAULT 0,
                    next_review TEXT,
                    status TEXT DEFAULT 'new',
                    FOREIGN KEY(topic) REFERENCES flashcard_decks(topic) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS idx_workspaces_account ON workspaces(associated_account);
                CREATE INDEX IF NOT EXISTS idx_flashcards_topic ON flashcards(topic);
                CREATE INDEX IF NOT EXISTS idx_flashcards_next_review ON flashcards(next_review);
                """),

            (4, "V4__DomainExtendedStorage", """
                CREATE TABLE IF NOT EXISTS themes (
                    theme_name TEXT PRIMARY KEY NOT NULL,
                    display_name TEXT NOT NULL,
                    accent_color TEXT,
                    colors_json TEXT,
                    is_active INTEGER NOT NULL DEFAULT 0,
                    updated_at TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS ai_invocation_logs (
                    id TEXT PRIMARY KEY NOT NULL,
                    alias TEXT NOT NULL,
                    timestamp_utc TEXT NOT NULL,
                    duration_ms INTEGER NOT NULL,
                    success INTEGER NOT NULL,
                    active_account TEXT DEFAULT 'default',
                    provider_mode TEXT DEFAULT 'auto',
                    created_at TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_themes_active ON themes(is_active);
                CREATE INDEX IF NOT EXISTS idx_ai_logs_account ON ai_invocation_logs(active_account);
                CREATE INDEX IF NOT EXISTS idx_ai_logs_timestamp ON ai_invocation_logs(timestamp_utc);
                """),

            (5, "V5__SystemStateAndResources", """
                CREATE TABLE IF NOT EXISTS system_state (
                    state_key TEXT PRIMARY KEY NOT NULL,
                    state_value TEXT,
                    updated_at TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS resources (
                    id TEXT PRIMARY KEY NOT NULL,
                    title TEXT NOT NULL,
                    topic TEXT NOT NULL,
                    file_path TEXT NOT NULL,
                    content_hash TEXT,
                    tags_csv TEXT,
                    updated_at TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS skills (
                    skill_name TEXT PRIMARY KEY NOT NULL,
                    display_name TEXT NOT NULL,
                    skill_path TEXT NOT NULL,
                    is_builtin INTEGER NOT NULL DEFAULT 0,
                    updated_at TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_resources_topic ON resources(topic);
                CREATE INDEX IF NOT EXISTS idx_skills_builtin ON skills(is_builtin);
                """),

            (6, "V6__ComprehensiveLearningSeeding", """
                CREATE TABLE IF NOT EXISTS quiz_questions (
                    id TEXT PRIMARY KEY NOT NULL,
                    category TEXT NOT NULL,
                    type TEXT,
                    difficulty TEXT,
                    question TEXT NOT NULL,
                    format TEXT,
                    hints_json TEXT,
                    companies_json TEXT,
                    tags_json TEXT,
                    updated_at TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_quiz_questions_category ON quiz_questions(category);
                """)
        };
    }
}
