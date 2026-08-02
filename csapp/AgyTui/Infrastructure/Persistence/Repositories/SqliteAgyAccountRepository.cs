using System.Text.Json;
using AgyTui.Domain.AccountContext;
using AgyTui.Domain.Common;
using AgyTui.Infrastructure.Middleware;
using AgyTui.Infrastructure.Persistence.DbContext;
using AgyTui.Infrastructure.Persistence.Interfaces;

namespace AgyTui.Infrastructure.Persistence.Repositories;

public class SqliteAgyAccountRepository : SqliteRepositoryBase<AccountMetadata, string>, IAgyAccountRepository
{
    public SqliteAgyAccountRepository(ISqliteDatabase db) : base(db) { }

    public override AccountMetadata? GetById(string id) => GetAccountMetadata(id);
    public override IEnumerable<AccountMetadata> GetAll() => GetAccounts().Select(GetAccountMetadata);
    public override void Save(string id, AccountMetadata entity) => SaveAccountMetadata(id, entity);
    public override void Delete(string id) => DeleteAccount(id);

    public string GetActiveAccount()
    {
        try
        {
            using var conn = Database.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT account_name FROM accounts WHERE is_active = 1 LIMIT 1;";
            var res = cmd.ExecuteScalar();
            if (res != null && res != DBNull.Value) return res.ToString()!;
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"[SqliteAgyAccountRepository] GetActiveAccount failed: {ex.Message}", ex);
        }
        return "default";
    }

    public void SetActiveAccount(string accountName)
    {
        var now = DateTime.UtcNow.ToString("o");
        try
        {
            using var conn = Database.CreateConnection();
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
        catch (Exception ex)
        {
            ExceptionMiddleware.Handle(ex, ErrorConstants.Vault.AccountSyncFailed, "Database Sync Failed");
        }
    }

    public AccountMetadata GetAccountMetadata(string accountName)
    {
        try
        {
            using var conn = Database.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT metadata_json FROM accounts WHERE account_name = @name;";
            cmd.Parameters.AddWithValue("@name", accountName);
            var res = cmd.ExecuteScalar();
            if (res != null && res != DBNull.Value)
            {
                var meta = JsonSerializer.Deserialize<AccountMetadata>(res.ToString()!, JsonOptions);
                if (meta != null) return meta;
            }
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"[SqliteAgyAccountRepository] GetAccountMetadata failed for '{accountName}': {ex.Message}", ex);
        }
        return new AccountMetadata();
    }

    public void SaveAccountMetadata(string accountName, AccountMetadata metadata)
    {
        var now = DateTime.UtcNow.ToString("o");
        var json = JsonSerializer.Serialize(metadata, JsonOptions);
        var historyJson = JsonSerializer.Serialize(metadata.RequestHistory);
        try
        {
            using var conn = Database.CreateConnection();
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
        catch (Exception ex)
        {
            ExceptionMiddleware.Handle(ex, ErrorConstants.Vault.StorageAccessFailed, "Save Metadata Failed");
        }
    }

    public string[] GetAccounts()
    {
        var list = new List<string> { "default" };
        try
        {
            using var conn = Database.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT account_name FROM accounts ORDER BY account_name ASC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var name = reader.GetString(0);
                if (!list.Contains(name, StringComparer.OrdinalIgnoreCase)) list.Add(name);
            }
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"[SqliteAgyAccountRepository] GetAccounts failed: {ex.Message}", ex);
        }
        return [.. list];
    }

    public void AddAccount(string accountName, string? email = null)
    {
        var now = DateTime.UtcNow.ToString("o");
        try
        {
            using var conn = Database.CreateConnection();
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

    public AccountCredentials? GetAccountCredentials(string accountName)
    {
        try
        {
            using var conn = Database.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT account_name, keyring_token, google_accounts_json, oauth_creds_json, state_json, email FROM accounts WHERE account_name = @name;";
            cmd.Parameters.AddWithValue("@name", accountName);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var name = reader.GetString(0);
                var token = reader.IsDBNull(1) ? null : reader.GetString(1);
                var googleAcc = reader.IsDBNull(2) ? null : reader.GetString(2);
                var oauth = reader.IsDBNull(3) ? null : reader.GetString(3);
                var state = reader.IsDBNull(4) ? null : reader.GetString(4);
                var email = reader.IsDBNull(5) ? null : reader.GetString(5);
                return new AccountCredentials(name, token, googleAcc, oauth, state, email);
            }
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"[SqliteAgyAccountRepository] GetAccountCredentials failed for '{accountName}': {ex.Message}", ex);
        }
        return null;
    }

    public void SaveAccountCredentials(AccountCredentials credentials)
    {
        var now = DateTime.UtcNow.ToString("o");
        try
        {
            using var conn = Database.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO accounts (account_name, keyring_token, google_accounts_json, oauth_creds_json, state_json, email, updated_at)
                VALUES (@name, @token, @googleAcc, @oauth, @state, @email, @now)
                ON CONFLICT(account_name) DO UPDATE SET
                    keyring_token = COALESCE(@token, keyring_token),
                    google_accounts_json = COALESCE(@googleAcc, google_accounts_json),
                    oauth_creds_json = COALESCE(@oauth, oauth_creds_json),
                    state_json = COALESCE(@state, state_json),
                    email = COALESCE(@email, email),
                    updated_at = @now;
                """;
            cmd.Parameters.AddWithValue("@name", credentials.AccountName);
            cmd.Parameters.AddWithValue("@token", (object?)credentials.KeyringToken ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@googleAcc", (object?)credentials.GoogleAccountsJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@oauth", (object?)credentials.OAuthCredsJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@state", (object?)credentials.StateJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@email", (object?)credentials.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@now", now);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            ExceptionMiddleware.Handle(ex, ErrorConstants.Vault.StorageAccessFailed, "Save Credentials Failed");
        }
    }

    public void DeleteAccount(string accountName)
    {
        try
        {
            using var conn = Database.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM accounts WHERE account_name = @name;";
            cmd.Parameters.AddWithValue("@name", accountName);
            cmd.ExecuteNonQuery();
        }
        catch { }
    }
}
