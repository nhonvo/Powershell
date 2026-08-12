namespace AgyTui.Infrastructure.Persistence.Seeding;

public class ThemeSeeder : ISeeder
{
    private readonly ISqliteDatabase _db;

    public int Order => 4;

    public ThemeSeeder(ISqliteDatabase db)
    {
        _db = db;
    }

    public void Seed()
    {
        try
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM themes;";
            var countObj = cmd.ExecuteScalar();
            long count = countObj != null && countObj != DBNull.Value ? Convert.ToInt64(countObj) : 0;
            if (count > 0) return;

            var now = DateTime.UtcNow.ToString("o");
            var defaultThemes = new (string Name, string DisplayName, string Accent, string ColorsJson, bool IsActive)[]
            {
                ("neko", "Neko Cat Theme", "#A3E635", "{\"accent\":\"#A3E635\",\"bg\":\"#0F172A\",\"fg\":\"#F8FAFC\"}", true),
                ("cyberpunk", "Cyberpunk Neon", "#EC4899", "{\"accent\":\"#EC4899\",\"bg\":\"#09090B\",\"fg\":\"#38BDF8\"}", false),
                ("nord", "Nordic Frost", "#88C0D0", "{\"accent\":\"#88C0D0\",\"bg\":\"#2E3440\",\"fg\":\"#ECEFF4\"}", false),
                ("dracula", "Dracula Vampire", "#BD93F9", "{\"accent\":\"#BD93F9\",\"bg\":\"#282A36\",\"fg\":\"#F8F8F2\"}", false)
            };

            foreach (var t in defaultThemes)
            {
                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = """
                    INSERT INTO themes (theme_name, display_name, accent_color, colors_json, is_active, updated_at)
                    VALUES (@name, @disp, @accent, @json, @active, @now)
                    ON CONFLICT(theme_name) DO UPDATE SET display_name = @disp, colors_json = @json, updated_at = @now;
                    """;
                insertCmd.Parameters.AddWithValue("@name", t.Name);
                insertCmd.Parameters.AddWithValue("@disp", t.DisplayName);
                insertCmd.Parameters.AddWithValue("@accent", t.Accent);
                insertCmd.Parameters.AddWithValue("@json", t.ColorsJson);
                insertCmd.Parameters.AddWithValue("@active", t.IsActive ? 1 : 0);
                insertCmd.Parameters.AddWithValue("@now", now);
                insertCmd.ExecuteNonQuery();
            }
        }
        catch { }
    }
}
