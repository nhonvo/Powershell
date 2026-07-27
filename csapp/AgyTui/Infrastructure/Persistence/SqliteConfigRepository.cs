namespace AgyTui.Infrastructure.Persistence;

using AgyTui.Core.Models;
using AgyTui.Infrastructure.Common;
using AgyTui.Infrastructure.Persistence.Interfaces;
using System.Text.Json;

public class SqliteConfigRepository : IConfigRepository
{
    private readonly ISqliteDatabase _db;

    public SqliteConfigRepository(ISqliteDatabase db)
    {
        _db = db;
        _db.InitializeDatabase();
    }

    public ConfigData LoadConfig()
    {
        try
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT json_data FROM app_config WHERE section_name = 'main';";
            var result = cmd.ExecuteScalar();
            if (result != null && result != DBNull.Value)
            {
                var json = result.ToString()!;
                var options = new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    PropertyNameCaseInsensitive = true
                };
                var data = JsonSerializer.Deserialize<ConfigData>(json, options);
                if (data != null) return data;
            }
        }
        catch { }

        // Fallback sync with profile.config.json if sqlite record doesn't exist yet
        var fallbackPath = AppPaths.ConfigFile;
        if (File.Exists(fallbackPath))
        {
            try
            {
                var json = File.ReadAllText(fallbackPath);
                var data = JsonSerializer.Deserialize<ConfigData>(json, new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    PropertyNameCaseInsensitive = true
                });
                if (data != null)
                {
                    SaveConfig(data);
                    return data;
                }
            }
            catch { }
        }

        var defaultConfig = new ConfigData();
        SaveConfig(defaultConfig);
        return defaultConfig;
    }

    public void SaveConfig(ConfigData config)
    {
        var now = DateTime.UtcNow.ToString("o");
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        try
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO app_config (section_name, json_data, updated_at)
                VALUES ('main', @json, @now)
                ON CONFLICT(section_name) DO UPDATE SET json_data = @json, updated_at = @now;
                """;
            cmd.Parameters.AddWithValue("@json", json);
            cmd.Parameters.AddWithValue("@now", now);
            cmd.ExecuteNonQuery();
        }
        catch { }

        // Dual-write to profile.config.json for external tool compatibility
        try
        {
            var fallbackPath = AppPaths.ConfigFile;
            var dir = Path.GetDirectoryName(fallbackPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(fallbackPath, json);
        }
        catch { }
    }

    public string? GetState(string key)
    {
        try
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT state_value FROM system_state WHERE state_key = @key;";
            cmd.Parameters.AddWithValue("@key", key);
            var result = cmd.ExecuteScalar();
            return result?.ToString();
        }
        catch { return null; }
    }

    public void SetState(string key, string? value)
    {
        var now = DateTime.UtcNow.ToString("o");
        try
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO system_state (state_key, state_value, updated_at)
                VALUES (@key, @val, @now)
                ON CONFLICT(state_key) DO UPDATE SET state_value = @val, updated_at = @now;
                """;
            cmd.Parameters.AddWithValue("@key", key);
            cmd.Parameters.AddWithValue("@val", (object?)value ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@now", now);
            cmd.ExecuteNonQuery();
        }
        catch { }
    }
}
