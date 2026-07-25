using System.Text.Json;

namespace AgyTui.Infrastructure.Persistence.Learning;

public class JsonStudyRepository : IStudyRepository
{
    private static readonly JsonSerializerOptions _js = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public void EnsureDirectories()
    {
        foreach (var d in new[]
        {
            LearnDataPaths.LearnRoot, LearnDataPaths.JapaneseDir, LearnDataPaths.EnglishDir,
            LearnDataPaths.CsharpDir, LearnDataPaths.DsaDir, LearnDataPaths.CareerDir,
            LearnDataPaths.CertificationsDir, LearnDataPaths.DecksDir, LearnDataPaths.VocabDir,
            LearnDataPaths.SnippetsDir, LearnDataPaths.SheetsDir, LearnDataPaths.StatsDir, LearnDataPaths.GrammarDir
        })
        {
            Directory.CreateDirectory(d);
        }
    }

    public T? LoadJson<T>(string path) where T : class
    {
        if (!File.Exists(path)) return null;

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), _js);
        }
        catch (Exception ex)
        {
            LogHelper.Log($"[JsonStudyRepository] Failed to parse JSON file '{path}': {ex.Message}", "ERROR");
            return null;
        }
    }

    public bool SaveJson<T>(string path, T obj)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var tempPath = path + ".tmp." + Guid.NewGuid().ToString("N");
            var json = JsonSerializer.Serialize(obj, _js);
            File.WriteAllText(tempPath, json, Encoding.UTF8);
            File.Move(tempPath, path, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Log($"[JsonStudyRepository] Failed to save JSON file '{path}': {ex.Message}", "ERROR");
            return false;
        }
    }
}
