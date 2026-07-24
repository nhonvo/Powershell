using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace AgyTui;

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
        catch
        {
            return null;
        }
    }

    public void SaveJson<T>(string path, T obj)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(obj, _js), Encoding.UTF8);
        }
        catch
        {
        }
    }
}
