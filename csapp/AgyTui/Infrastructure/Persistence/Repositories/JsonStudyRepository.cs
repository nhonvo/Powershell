using System.Text.Json;

namespace AgyTui.Infrastructure.Persistence.Repositories;

public class JsonStudyRepository : JsonFileRepositoryBase<object>, IStudyRepository
{
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
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions);
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
            var json = JsonSerializer.Serialize(obj, JsonOptions);
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

    public FlashcardDeck LoadDeck(string topic)
    {
        var path = Path.Combine(LearnDataPaths.DecksDir, $"{topic}_deck.json");
        var loaded = LoadJson<FlashcardDeck>(path);
        return loaded ?? new FlashcardDeck(topic);
    }

    public bool SaveDeck(FlashcardDeck deck)
    {
        var path = Path.Combine(LearnDataPaths.DecksDir, $"{deck.Topic}_deck.json");
        return SaveJson(path, deck);
    }
}
