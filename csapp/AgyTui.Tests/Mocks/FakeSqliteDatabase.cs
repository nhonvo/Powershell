using Microsoft.Data.Sqlite;
using AgyTui.Infrastructure.Persistence.Interfaces;

namespace AgyTui.Tests.Mocks;

public class FakeSqliteDatabase : ISqliteDatabase
{
    private readonly SqliteConnection _keepAliveConnection;

    public string DbPath => "Data Source=:memory:";

    public FakeSqliteDatabase()
    {
        _keepAliveConnection = new SqliteConnection("Data Source=:memory:");
        _keepAliveConnection.Open();
        InitializeDatabase();
    }

    public SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        return conn;
    }

    public void InitializeDatabase()
    {
        using var cmd = _keepAliveConnection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS accounts (
                account_name TEXT PRIMARY KEY,
                email TEXT,
                is_active INTEGER DEFAULT 0,
                quota_status TEXT DEFAULT 'OK',
                last_used TEXT,
                usage_count INTEGER DEFAULT 0,
                request_history_json TEXT,
                metadata_json TEXT,
                created_at TEXT DEFAULT CURRENT_TIMESTAMP,
                updated_at TEXT DEFAULT CURRENT_TIMESTAMP
            );
            """;
        cmd.ExecuteNonQuery();
    }
}
