using System.Text.RegularExpressions;

namespace AgyTui.Infrastructure.Persistence.Learning;

public sealed record ExtractedItem(string SourceId, string SourcePath, string Format, string Topic, string SubTopic, string Language, string ItemType, string Front, string Back, string? Hint, string? Mnemonic, string? ExampleSentence, string? CodeSnippetText, string[] Tags, int Difficulty);

public static class MdExtractor
{
    public static ExtractedItem[] Extract(string path, ResourceEntry entry)
    {
        if (!File.Exists(path)) return [];
        var text = File.ReadAllText(path);
        var items = new List<ExtractedItem>();
        items.AddRange(ExtractTables(text, entry));
        items.AddRange(ExtractBoldColon(text, entry));
        items.AddRange(ExtractCodeBlocks(text, entry));
        return [.. items];
    }

    private static ExtractedItem[] ExtractTables(string text, ResourceEntry entry)
    {
        var items = new List<ExtractedItem>();
        var tablePattern = new Regex(@"^\|.+\|$", RegexOptions.Multiline);
        var tableBlocks = Regex.Split(text, @"\n\n+");
        foreach (var block in tableBlocks)
        {
            var rows = block.Split('\n').Where(l => l.TrimStart().StartsWith("|") && !l.Contains("---")).ToArray();
            if (rows.Length < 2) continue;
            var headers = rows[0].Split('|').Select(h => h.Trim()).Where(h => h.Length > 0).ToArray();
            for (int i = 1; i < rows.Length; i++)
            {
                var cells = rows[i].Split('|').Select(c => c.Trim()).Where(c => c.Length > 0).ToArray();
                if (cells.Length < 2) continue;
                items.Add(new ExtractedItem(entry.Id, entry.Path, "md", entry.Topics.FirstOrDefault() ?? "general", "table", entry.Language, "flashcard", cells[0], cells.Length > 1 ? cells[1] : "", null, null, cells.Length > 2 ? cells[2] : null, null, entry.Tags, 3));
            }
        }
        return [.. items];
    }

    private static ExtractedItem[] ExtractBoldColon(string text, ResourceEntry entry)
    {
        var pattern = new Regex(@"\*\*([^*]+)\*\*\s*:\s*(.+)");
        return pattern.Matches(text).Select(m => new ExtractedItem(entry.Id, entry.Path, "md", entry.Topics.FirstOrDefault() ?? "general", "bold-colon", entry.Language, "flashcard", m.Groups[1].Value.Trim(), m.Groups[2].Value.Trim(), null, null, null, null, entry.Tags, 3)).ToArray();
    }

    private static ExtractedItem[] ExtractCodeBlocks(string text, ResourceEntry entry)
    {
        var pattern = new Regex(@"```(\w+)?\n([\s\S]*?)```");
        return pattern.Matches(text).Select(m => new ExtractedItem(entry.Id, entry.Path, "md", entry.Topics.FirstOrDefault() ?? "csharp", "snippet", "code", "snippet", $"Snippet", m.Groups[2].Value.Trim(), null, null, null, m.Groups[2].Value.Trim(), entry.Tags, 3)).ToArray();
    }
}

public static class CsvExtractor
{
    public static string[] ParseCsvLine(string line, char delimiter = ',')
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == delimiter && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result.Select(s => s.Trim().Trim('"')).ToArray();
    }

    public static ExtractedItem[] Extract(string path, ResourceEntry entry, char delimiter = ',')
    {
        if (!File.Exists(path)) return [];
        var lines = File.ReadAllLines(path);
        if (lines.Length < 2) return [];
        var headers = ParseCsvLine(lines[0], delimiter).Select(h => h.ToLower()).ToArray();
        int frontIdx = Array.FindIndex(headers, h => h is "word" or "front" or "term" or "question");
        int backIdx = Array.FindIndex(headers, h => h is "definition" or "back" or "meaning" or "answer");
        if (frontIdx < 0) frontIdx = 0;
        if (backIdx < 0) backIdx = 1;
        var items = new List<ExtractedItem>();
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var cells = ParseCsvLine(lines[i], delimiter);
            if (cells.Length <= Math.Max(frontIdx, backIdx)) continue;
            items.Add(new ExtractedItem(entry.Id, path, delimiter == '\t' ? "tsv" : "csv", entry.Topics.FirstOrDefault() ?? "general", "csv-row", entry.Language, "flashcard", cells[frontIdx], cells[backIdx], null, null, null, null, entry.Tags, 3));
        }
        return [.. items];
    }
}

public static class TsvExtractor
{
    public static ExtractedItem[] Extract(string path, ResourceEntry entry) => CsvExtractor.Extract(path, entry, '\t');

    public static DeckFile ImportTsv(string tsvPath, string deckName)
    {
        var meta = new DeckMeta(deckName, deckName, "en", "imported", "general", [], DateTime.UtcNow.ToString("o"), 1);
        if (!File.Exists(tsvPath)) return new DeckFile(meta, []);
        var cards = File.ReadLines(tsvPath)
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("#"))
            .Select(l => CsvExtractor.ParseCsvLine(l, '\t'))
            .Where(parts => parts.Length >= 2)
            .Select((parts, i) => new FlashCard(
                Id: $"{deckName}-{i + 1}",
                Front: parts[0],
                Back: parts[1],
                Hint: parts.Length > 2 ? parts[2] : null,
                Mnemonic: null,
                ExampleSentence: parts.Length > 3 ? parts[3] : null,
                Tags: parts.Length > 4 ? parts[4].Split(',').Select(t => t.Trim()).ToArray() : [deckName],
                Difficulty: 1,
                Sr: SpacedRepetitionEngine.NewCard()
            ))
            .ToArray();
        return new DeckFile(meta, cards);
    }
}

public static class ExtractorRouter
{
    public static ExtractedItem[] Route(ResourceEntry entry)
    {
        try
        {
            return entry.Format switch
            {
                "md" or "txt" => MdExtractor.Extract(entry.Path, entry),
                "csv" => CsvExtractor.Extract(entry.Path, entry),
                "tsv" => TsvExtractor.Extract(entry.Path, entry),
                "code" => ExtractCode(entry),
                "url" => ExtractUrl(entry),
                _ => []
            };
        }
        catch (Exception ex)
        {
            ResourceRegistry.UpdateStatus(entry.Id, "error", ex.Message);
            return [];
        }
    }

    private static ExtractedItem[] ExtractCode(ResourceEntry entry)
    {
        if (!File.Exists(entry.Path)) return [];
        var content = File.ReadAllText(entry.Path);
        var comments = Regex.Matches(content, @"///\s*<summary>([\s\S]*?)</summary>|/\*\*([\s\S]*?)\*/|#\s+(.+)").Select(m => (m.Groups[1].Value + m.Groups[2].Value + m.Groups[3].Value).Trim()).Where(c => c.Length > 10).Select(c => new ExtractedItem(entry.Id, entry.Path, "code", entry.Topics.FirstOrDefault() ?? "csharp", "comment", "code", "flashcard", c, "", null, null, null, null, entry.Tags, 3)).ToArray();
        return comments;
    }

    private static ExtractedItem[] ExtractUrl(ResourceEntry entry)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var html = client.GetStringAsync(entry.Path).GetAwaiter().GetResult();
            var text = Regex.Replace(html, @"<[^>]+>", " ");
            text = Regex.Replace(text, @"\s+", " ").Trim();
            var fakeEntry = new ResourceEntry(entry.Id, entry.Path, "md", entry.Title, entry.Tags, entry.Topics, entry.Language, entry.SourceType, null, 0, entry.AddedAt, null, "pending", null, 0, [], false, true);
            var tempFile = Path.GetTempFileName() + ".md";
            File.WriteAllText(tempFile, text);
            var items = MdExtractor.Extract(tempFile, fakeEntry);
            File.Delete(tempFile);
            return items;
        }
        catch
        {
            return [];
        }
    }
}

public static class ContentExtractor
{
    public static string[][] ExtractVocabTable(string notePath)
    {
        if (!File.Exists(notePath)) return [];
        var lines = File.ReadAllLines(notePath);
        var results = new List<string[]>();
        foreach (var line in lines)
        {
            if (!line.TrimStart().StartsWith("|") || line.Contains("---")) continue;
            var cells = line.Split('|').Select(c => c.Trim()).Where(c => c.Length > 0).ToArray();
            if (cells.Length >= 2) results.Add(cells);
        }
        return results.Count > 1 ? [.. results.Skip(1)] : [];
    }

    public static (string Front, string Back)[] ExtractBoldColonPairs(string notePath)
    {
        if (!File.Exists(notePath)) return [];
        var content = File.ReadAllText(notePath);
        return Regex.Matches(content, @"\*\*([^*]+)\*\*\s*:\s*(.+)").Select(m => (m.Groups[1].Value.Trim(), m.Groups[2].Value.Trim())).ToArray();
    }

    public static string[] ExtractBulletPoints(string notePath)
    {
        if (!File.Exists(notePath)) return [];
        return File.ReadAllLines(notePath).Where(l => l.TrimStart().StartsWith("- ") || l.TrimStart().StartsWith("* ")).Select(l => l.TrimStart('-', '*', ' ')).ToArray();
    }

    public static string[][] ExtractQuizBlocks(string notePath)
    {
        if (!File.Exists(notePath)) return [];
        var content = File.ReadAllText(notePath);
        var results = new List<string[]>();
        var blocks = Regex.Matches(content, @"### Q: (.+?)\n((?:- \[[ x]\] .+\n)+)", RegexOptions.Singleline);
        foreach (Match b in blocks)
        {
            var question = b.Groups[1].Value.Trim();
            var options = Regex.Matches(b.Groups[2].Value, @"- \[([ x])\] (.+)").Select(m => m.Groups[2].Value.Trim()).ToArray();
            var correct = Regex.Matches(b.Groups[2].Value, @"- \[([ x])\] (.+)").Select((m, i) => (m.Groups[1].Value == "x", i)).FirstOrDefault(t => t.Item1).i.ToString();
            results.Add([question, .. options, correct]);
        }
        return [.. results];
    }
}

public static class TemplateGenerator
{
    public static void RouteItemsToFiles(ExtractedItem[] items)
    {
        LearnDataPaths.EnsureDirectories();
        foreach (var g in items.GroupBy(i => i.ItemType))
        {
            switch (g.Key)
            {
                case "vocab":
                    GenerateVocabFile(g.ToArray());
                    break;
                case "flashcard":
                    GenerateDeckFile(g.ToArray());
                    break;
                case "snippet":
                    GenerateSnippetFile(g.ToArray());
                    break;
            }
        }
    }

    private static void GenerateDeckFile(ExtractedItem[] items)
    {
        var byTopic = items.GroupBy(i => i.Topic);
        foreach (var g in byTopic)
        {
            var path = Path.Combine(LearnDataPaths.DecksDir, $"{g.Key}.json");
            var existing = LearnDataPaths.LoadJson<DeckFile>(path);
            var existingCards = existing?.Cards.ToList() ?? [];
            var newCards = g.Where(i => !existingCards.Any(c => c.Front.Equals(i.Front, StringComparison.OrdinalIgnoreCase))).Select((i, idx) => new FlashCard($"card_{existingCards.Count + idx + 1:000}", i.Front, i.Back, i.Hint, i.Mnemonic, i.ExampleSentence, i.Tags, i.Difficulty, SpacedRepetitionEngine.NewCard())).ToList();
            existingCards.AddRange(newCards);
            var meta = existing?.Meta ?? new DeckMeta(g.Key, g.Key, "mixed", g.Key, "intermediate", [], DateTimeOffset.Now.ToString("o"), 1);
            LearnDataPaths.SaveJson(path, new DeckFile(meta with { Version = meta.Version + 1 }, [.. existingCards]));
        }
    }

    private static void GenerateVocabFile(ExtractedItem[] items)
    {
        var byTopic = items.GroupBy(i => i.SubTopic.Contains("beginner") ? "beginner" : i.SubTopic.Contains("advanced") ? "advanced" : "intermediate");
        foreach (var g in byTopic)
        {
            var path = Path.Combine(LearnDataPaths.VocabDir, $"{g.Key}.json");
            var existing = LearnDataPaths.LoadJson<VocabFile>(path);
            var words = existing?.Words.ToList() ?? [];
            foreach (var item in g)
            {
                if (words.Any(w => w.Word.Equals(item.Front, StringComparison.OrdinalIgnoreCase))) continue;
                words.Add(new VocabWord($"word_{words.Count + 1:000}", item.Front, "", "noun", item.Back, item.ExampleSentence ?? "", [], [], item.Difficulty, item.Tags, SpacedRepetitionEngine.NewCard()));
            }
            LearnDataPaths.SaveJson(path, new VocabFile(g.Key, [.. words]));
        }
    }

    private static void GenerateSnippetFile(ExtractedItem[] items)
    {
        var byLang = items.GroupBy(i => i.Language);
        foreach (var g in byLang)
        {
            var path = Path.Combine(LearnDataPaths.SnippetsDir, $"{g.Key}.json");
            var existing = LearnDataPaths.LoadJson<SnippetsFile>(path);
            var snippets = existing?.Snippets.ToList() ?? [];
            foreach (var item in g.Where(i => i.CodeSnippetText != null)) snippets.Add(new CodeSnippet($"cs_{snippets.Count + 1:000}", item.Front, "general", item.CodeSnippetText!, item.Back, "", item.Tags, item.Difficulty));
            LearnDataPaths.SaveJson(path, new SnippetsFile(g.Key, [.. snippets]));
        }
    }
}
