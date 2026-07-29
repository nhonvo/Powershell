namespace AgyTui.Infrastructure.Persistence.Repositories;

using AgyTui.Infrastructure.Integrations.AgyClient;
using AgyTui.Infrastructure.Persistence.Interfaces;
using System.Text.Json;

public class SqliteAgyAccountRepository : IAgyAccountRepository
{
    private readonly ISqliteDatabase _db;

    public SqliteAgyAccountRepository(ISqliteDatabase db)
    {
        _db = db;
        _db.InitializeDatabase();
    }

    public string GetActiveAccount()
    {
        try
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT account_name FROM accounts WHERE is_active = 1 LIMIT 1;";
            var res = cmd.ExecuteScalar();
            if (res != null && res != DBNull.Value) return res.ToString()!;
        }
        catch { }
        return "default";
    }

    public void SetActiveAccount(string accountName)
    {
        var now = DateTime.UtcNow.ToString("o");
        try
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                UPDATE accounts SET is_active = 0;
                INSERT INTO accounts (account_name, is_active, updated_at)
                VALUES (@name, 1, @now)
                ON CONFLICT(account_name) DO UPDATE SET is_active = 1, updated_at = @now;
                """;
            cmd.Parameters.AddWithValue("@name", accountName);
            cmd.Parameters.AddWithValue("@now", now);
            cmd.ExecuteNonQuery();
        }
        catch { }
    }

    public AccountMetadata GetAccountMetadata(string accountName)
    {
        try
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT metadata_json FROM accounts WHERE account_name = @name;";
            cmd.Parameters.AddWithValue("@name", accountName);
            var res = cmd.ExecuteScalar();
            if (res != null && res != DBNull.Value)
            {
                var meta = JsonSerializer.Deserialize<AccountMetadata>(res.ToString()!);
                if (meta != null) return meta;
            }
        }
        catch { }
        return new AccountMetadata();
    }

    public void SaveAccountMetadata(string accountName, AccountMetadata metadata)
    {
        var now = DateTime.UtcNow.ToString("o");
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        var historyJson = JsonSerializer.Serialize(metadata.RequestHistory);
        try
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO accounts (account_name, quota_status, last_used, usage_count, request_history_json, metadata_json, updated_at)
                VALUES (@name, @quota, @lastUsed, @usage, @history, @meta, @now)
                ON CONFLICT(account_name) DO UPDATE SET
                    quota_status = @quota,
                    last_used = @lastUsed,
                    usage_count = @usage,
                    request_history_json = @history,
                    metadata_json = @meta,
                    updated_at = @now;
                """;
            cmd.Parameters.AddWithValue("@name", accountName);
            cmd.Parameters.AddWithValue("@quota", metadata.QuotaStatus);
            cmd.Parameters.AddWithValue("@lastUsed", metadata.LastUsed);
            cmd.Parameters.AddWithValue("@usage", metadata.UsageCount);
            cmd.Parameters.AddWithValue("@history", historyJson);
            cmd.Parameters.AddWithValue("@meta", json);
            cmd.Parameters.AddWithValue("@now", now);
            cmd.ExecuteNonQuery();
        }
        catch { }
    }

    public string[] GetAccounts()
    {
        var list = new List<string> { "default" };
        try
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT account_name FROM accounts ORDER BY account_name ASC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var name = reader.GetString(0);
                if (!list.Contains(name, StringComparer.OrdinalIgnoreCase)) list.Add(name);
            }
        }
        catch { }
        return [.. list];
    }

    public void AddAccount(string accountName, string? email = null)
    {
        var now = DateTime.UtcNow.ToString("o");
        try
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO accounts (account_name, email, updated_at)
                VALUES (@name, @email, @now)
                ON CONFLICT(account_name) DO UPDATE SET email = COALESCE(@email, email), updated_at = @now;
                """;
            cmd.Parameters.AddWithValue("@name", accountName);
            cmd.Parameters.AddWithValue("@email", (object?)email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@now", now);
            cmd.ExecuteNonQuery();
        }
        catch { }
    }

    public void DeleteAccount(string accountName)
    {
        try
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM accounts WHERE account_name = @name;";
            cmd.Parameters.AddWithValue("@name", accountName);
            cmd.ExecuteNonQuery();
        }
        catch { }
    }
}
