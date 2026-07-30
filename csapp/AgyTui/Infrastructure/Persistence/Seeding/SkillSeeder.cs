using AgyTui.Infrastructure.Persistence.Interfaces;

namespace AgyTui.Infrastructure.Persistence.Seeding;

public class SkillSeeder : ISeeder
{
    private readonly ISqliteDatabase _db;

    public int Order => 6;

    public SkillSeeder(ISqliteDatabase db)
    {
        _db = db;
    }

    public void Seed()
    {
        try
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM skills;";
            var countObj = cmd.ExecuteScalar();
            long count = countObj != null && countObj != DBNull.Value ? Convert.ToInt64(countObj) : 0;
            if (count > 0) return;

            var skillsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "csapp", "skills");
            if (!Directory.Exists(skillsDir))
            {
                skillsDir = Path.Combine(Directory.GetCurrentDirectory(), "csapp", "skills");
            }
            if (!Directory.Exists(skillsDir)) return;

            var files = Directory.GetFiles(skillsDir, "*.md");
            var now = DateTime.UtcNow.ToString("o");

            foreach (var file in files)
            {
                var skillName = Path.GetFileNameWithoutExtension(file);
                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = """
                    INSERT INTO skills (skill_name, display_name, skill_path, is_builtin, updated_at)
                    VALUES (@name, @disp, @path, 1, @now)
                    ON CONFLICT(skill_name) DO UPDATE SET skill_path = @path, updated_at = @now;
                    """;
                insertCmd.Parameters.AddWithValue("@name", skillName);
                insertCmd.Parameters.AddWithValue("@disp", skillName.Replace('-', ' '));
                insertCmd.Parameters.AddWithValue("@path", file);
                insertCmd.Parameters.AddWithValue("@now", now);
                insertCmd.ExecuteNonQuery();
            }
        }
        catch { }
    }
}
