using System.Text.RegularExpressions;

namespace AgyTui.UI.Screens.Ide;

public class IdeFileSearchService
{
    public IEnumerable<(int lineNumber, string lineContent)> SearchInFile(string filePath, string pattern)
    {
        if (!File.Exists(filePath) || string.IsNullOrWhiteSpace(pattern))
            return Enumerable.Empty<(int, string)>();

        var lines = File.ReadAllLines(filePath);
        return lines
            .Select((l, i) => (lineNumber: i + 1, lineContent: l))
            .Where(x => Regex.IsMatch(x.lineContent, pattern, RegexOptions.IgnoreCase));
    }

    public IEnumerable<string> SearchAcrossFiles(string rootPath, string pattern, int maxResults = 100)
    {
        if (!Directory.Exists(rootPath) || string.IsNullOrWhiteSpace(pattern))
            return Enumerable.Empty<string>();

        var results = new List<string>();
        foreach (var f in Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj") && !f.Contains(".git")))
        {
            try
            {
                var lines = File.ReadAllLines(f);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (Regex.IsMatch(lines[i], pattern, RegexOptions.IgnoreCase))
                    {
                        results.Add($"{Path.GetRelativePath(rootPath, f)}:{i + 1}: {lines[i].Trim()}");
                    }
                }
            }
            catch { }
            if (results.Count >= maxResults) break;
        }
        return results;
    }
}
