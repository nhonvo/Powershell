using System.Text.Json;
using AgyTui.Infrastructure.Persistence.Interfaces;

namespace AgyTui.Infrastructure.Persistence.Seeding;

public class ResourceSeeder : ISeeder
{
    private readonly ISqliteDatabase _db;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public int Order => 5;

    public ResourceSeeder(ISqliteDatabase db)
    {
        _db = db;
    }

    public void Seed()
    {
        try
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM resources;";
            var countObj = cmd.ExecuteScalar();
            long count = countObj != null && countObj != DBNull.Value ? Convert.ToInt64(countObj) : 0;
            if (count > 0) return;

            var indexPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "csapp", "resources", "index.json");
            if (!File.Exists(indexPath))
            {
                indexPath = Path.Combine(Directory.GetCurrentDirectory(), "csapp", "resources", "index.json");
            }
            if (!File.Exists(indexPath)) return;

            var json = File.ReadAllText(indexPath);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return;

            var now = DateTime.UtcNow.ToString("o");
            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                var id = elem.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();
                var title = elem.TryGetProperty("title", out var tProp) ? tProp.GetString() ?? "Untitled" : "Untitled";
                var topic = elem.TryGetProperty("topic", out var topProp) ? topProp.GetString() ?? "general" : "general";
                var filePath = elem.TryGetProperty("filePath", out var fpProp) ? fpProp.GetString() ?? "" : "";
                var contentHash = elem.TryGetProperty("contentHash", out var hashProp) ? hashProp.GetString() : null;

                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = """
                    INSERT INTO resources (id, title, topic, file_path, content_hash, tags_csv, updated_at)
                    VALUES (@id, @title, @topic, @filePath, @hash, @tags, @now)
                    ON CONFLICT(id) DO UPDATE SET title = @title, updated_at = @now;
                    """;
                insertCmd.Parameters.AddWithValue("@id", id);
                insertCmd.Parameters.AddWithValue("@title", title);
                insertCmd.Parameters.AddWithValue("@topic", topic);
                insertCmd.Parameters.AddWithValue("@filePath", filePath);
                insertCmd.Parameters.AddWithValue("@hash", (object?)contentHash ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@tags", DBNull.Value);
                insertCmd.Parameters.AddWithValue("@now", now);
                insertCmd.ExecuteNonQuery();
            }
        }
        catch { }
    }
}
