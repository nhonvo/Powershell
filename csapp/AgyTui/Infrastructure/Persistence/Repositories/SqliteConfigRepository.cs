using System.Text.Json;
using AgyTui.Infrastructure.Configuration;
using AgyTui.Infrastructure.Persistence.DbContext;
using AgyTui.Infrastructure.Persistence.Interfaces;

namespace AgyTui.Infrastructure.Persistence.Repositories;

public class SqliteConfigRepository : SqliteRepositoryBase<ConfigData, string>, IConfigRepository
{
    public SqliteConfigRepository(ISqliteDatabase db) : base(db) { }

    public override ConfigData? GetById(string id) => LoadConfig();
    public override IEnumerable<ConfigData> GetAll() => [LoadConfig()];
    public override void Save(string id, ConfigData entity) => SaveConfig(entity);
    public override void Delete(string id) { }

    public ConfigData LoadConfig()
    {
        try
        {
            using var conn = Database.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT json_data FROM app_config WHERE section_name = 'main';";
            var res = cmd.ExecuteScalar();
            if (res != null && res != DBNull.Value)
            {
                var cfg = JsonSerializer.Deserialize<ConfigData>(res.ToString()!, JsonOptions);
                if (cfg != null)
                {
                    return cfg;
                }
            }
        }
        catch { }

        var legacyFile = Config.GetConfigFilePath();
        if (File.Exists(legacyFile))
        {
            try
            {
                var text = File.ReadAllText(legacyFile);
                var data = JsonSerializer.Deserialize<ConfigData>(text, JsonOptions);
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
        var json = JsonSerializer.Serialize(config, JsonOptions);
        try
        {
            using var conn = Database.CreateConnection();
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
            var fallbackPath = Config.GetConfigFilePath();
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
            using var conn = Database.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT state_value FROM system_state WHERE state_key = @key;";
            cmd.Parameters.AddWithValue("@key", key);
            var res = cmd.ExecuteScalar();
            if (res != null && res != DBNull.Value) return res.ToString();
        }
        catch { }
        return null;
    }

    public void SetState(string key, string? value)
    {
        var now = DateTime.UtcNow.ToString("o");
        try
        {
            using var conn = Database.CreateConnection();
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
