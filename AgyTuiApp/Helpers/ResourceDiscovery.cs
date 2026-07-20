using System;
using System.Collections.Generic;
using System.Collections.Frozen;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public sealed record ExtractedItem(string SourceId, string SourcePath, string Format, string Topic, string SubTopic, string Language, string ItemType, string Front, string Back, string? Hint, string? Mnemonic, string? ExampleSentence, string? CodeSnippetText, string[] Tags, int Difficulty);

public sealed record ResourceEntry(string Id, string Path, string Format, string Title, string[] Tags, string[] Topics, string Language, string SourceType, string? Checksum, long SizeBytes, string AddedAt, string? LastExtractedAt, string Status, string? ErrorMessage, int ExtractedItemCount, string[] LearnFiles, bool AutoDiscovered, bool Enabled);

public sealed record ExtractionConfig(string LearnPath, string VaultPath, string ResourcesIndexPath, bool DryRun, bool ForceReExtract);

public sealed record ResourceIndex(int Version, string UpdatedAt, ResourceEntry[] Resources);

public static class ResourceRegistry
{
    public static ResourceEntry[] LoadAll()
    {
        var idx = LearnDataPaths.LoadJson<ResourceIndex>(LearnDataPaths.ResourcesIndex);
        return idx?.Resources ?? [];

    }

    public static void Save(ResourceEntry[] entries)
    {
        var idx = new ResourceIndex(1, DateTimeOffset.Now.ToString("o"), entries);
        LearnDataPaths.SaveJson(LearnDataPaths.ResourcesIndex, idx);

    }

    public static string AddResource(string path, string[] tags)
    {
        var entries = LoadAll().ToList();
        var id = $"res_{entries.Count + 1:000}";
        var format = DetectFormat(path);
        var checksum = File.Exists(path) ? ComputeChecksum(path) : null;
        var size = File.Exists(path) ? new FileInfo(path).Length : 0;
        entries.Add(new ResourceEntry(id, path, format, System.IO.Path.GetFileNameWithoutExtension(path), tags, [], "auto", "local_file", checksum, size, DateTimeOffset.Now.ToString("o"), null, "pending", null, 0, [], false, true));
        Save([.. entries]);
        return id;

    }

    public static void UpdateStatus(string id, string status, string? error = null)
    {
        var entries = LoadAll().ToList();
        var idx = entries.FindIndex(e => e.Id == id);
        if (idx < 0) return;
        entries[idx] = entries[idx] with
        {
            Status = status,
            ErrorMessage = error
        }
        ;
        Save([.. entries]);

    }

    public static string ComputeChecksum(string filePath)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();

        using var stream = File.OpenRead(filePath);
        return "sha256:" + Convert.ToHexString(sha.ComputeHash(stream));

    }

    private static string DetectFormat(string path)
    {
        if (path.StartsWith("http")) return "url";
        return System.IO.Path.GetExtension(path).ToLower() switch
        {
            ".md" or ".txt" => "md",
            ".pdf" => "pdf",
            ".docx" => "docx",
            ".csv" => "csv",
            ".epub" => "epub",
            ".cs" or ".py" or ".ts" or ".js" or ".go" => "code",
            ".png" or ".jpg" or ".jpeg" => "image",
            _ => "md"
        }
        ;

    }

}
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
            for (int i = 1;
            i < rows.Length;
            i++)
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
    public static ExtractedItem[] Extract(string path, ResourceEntry entry)
    {
        if (!File.Exists(path)) return [];
        var lines = File.ReadAllLines(path);
        if (lines.Length < 2) return [];
        var headers = lines[0].Split(',').Select(h => h.Trim().Trim('"').ToLower()).ToArray();
        int frontIdx = Array.FindIndex(headers, h => h is "word" or "front" or "term" or "question");
        int backIdx = Array.FindIndex(headers, h => h is "definition" or "back" or "meaning" or "answer");
        if (frontIdx < 0) frontIdx = 0;
        if (backIdx < 0) backIdx = 1;
        var items = new List<ExtractedItem>();
        for (int i = 1;
        i < lines.Length;
        i++)
        {
            var cells = lines[i].Split(',').Select(c => c.Trim().Trim('"')).ToArray();
            if (cells.Length <= Math.Max(frontIdx, backIdx)) continue;
            items.Add(new ExtractedItem(entry.Id, path, "csv", entry.Topics.FirstOrDefault() ?? "general", "csv-row", entry.Language, "flashcard", cells[frontIdx], cells[backIdx], null, null, null, null, entry.Tags, 3));
        }
        return [.. items];

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
                "code" => ExtractCode(entry),
                "url" => ExtractUrl(entry),
                _ => []
            }
            ;
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
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            }
            ;
            var html = client.GetStringAsync(entry.Path).GetAwaiter().GetResult();
            var text = Regex.Replace(html, @"<[^>]+>", " ");
            text = Regex.Replace(text, @"\s+", " ").Trim();
            var tempEntry = entry with
            {
                Format = "md"
            }
            ;
            var fakeEntry = new ResourceEntry(entry.Id, entry.Path, "md", entry.Title, entry.Tags, entry.Topics, entry.Language, entry.SourceType, null, 0, entry.AddedAt, null, "pending", null, 0, [], false, true);
            var tempFile = System.IO.Path.GetTempFileName() + ".md";
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
public static class ResourceScanner
{
    public static string[] FindNotesByTag(string vaultPath, string[] tags)
    {
        if (!Directory.Exists(vaultPath)) return [];
        return Directory.GetFiles(vaultPath, "*.md", SearchOption.AllDirectories).Where(f =>
        {
            var fm = ObsidianBridge.ParseFrontmatter(f);
            return fm != null && fm.Tags.Any(t => tags.Any(needle => t.Contains(needle, StringComparison.OrdinalIgnoreCase)));
        }
        ).ToArray();

    }

    public static string[] FindNotesByTopic(string vaultPath, string topic)
    {
        if (!Directory.Exists(vaultPath)) return [];
        return Directory.GetFiles(vaultPath, "*.md", SearchOption.AllDirectories).Where(f =>
        {
            var fm = ObsidianBridge.ParseFrontmatter(f);
            return fm != null && fm.Topic.Equals(topic, StringComparison.OrdinalIgnoreCase);
        }
        ).ToArray();

    }

    public static string[] ListAllTags(string vaultPath)
    {
        if (!Directory.Exists(vaultPath)) return [];
        return Directory.GetFiles(vaultPath, "*.md", SearchOption.AllDirectories).SelectMany(f => ObsidianBridge.ParseFrontmatter(f)?.Tags ?? []).Distinct().OrderBy(t => t).ToArray();

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
            var path = System.IO.Path.Combine(LearnDataPaths.DecksDir, $"{g.Key}.json");
            var existing = LearnDataPaths.LoadJson<DeckFile>(path);
            var existingCards = existing?.Cards.ToList() ?? [];
            var newCards = g.Where(i => !existingCards.Any(c => c.Front.Equals(i.Front, StringComparison.OrdinalIgnoreCase))).Select((i, idx) => new FlashCard($"card_{existingCards.Count + idx + 1:000}", i.Front, i.Back, i.Hint, i.Mnemonic, i.ExampleSentence, i.Tags, i.Difficulty, SpacedRepetitionEngine.NewCard())).ToList();
            existingCards.AddRange(newCards);
            var meta = existing?.Meta ?? new DeckMeta(g.Key, g.Key, "mixed", g.Key, "intermediate", [], DateTimeOffset.Now.ToString("o"), 1);
            LearnDataPaths.SaveJson(path, new DeckFile(meta with
            {
                Version = meta.Version + 1
            }
            , [.. existingCards]));
        }

    }

    private static void GenerateVocabFile(ExtractedItem[] items)
    {
        var byTopic = items.GroupBy(i => i.SubTopic.Contains("beginner") ? "beginner" : i.SubTopic.Contains("advanced") ? "advanced" : "intermediate");
        foreach (var g in byTopic)
        {
            var path = System.IO.Path.Combine(LearnDataPaths.VocabDir, $"{g.Key}.json");
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
            var path = System.IO.Path.Combine(LearnDataPaths.SnippetsDir, $"{g.Key}.json");
            var existing = LearnDataPaths.LoadJson<SnippetsFile>(path);
            var snippets = existing?.Snippets.ToList() ?? [];
            foreach (var item in g.Where(i => i.CodeSnippetText != null)) snippets.Add(new CodeSnippet($"cs_{snippets.Count + 1:000}", item.Front, "general", item.CodeSnippetText!, item.Back, "", item.Tags, item.Difficulty));
            LearnDataPaths.SaveJson(path, new SnippetsFile(g.Key, [.. snippets]));
        }

    }

}
public static class LearnRouter
{
    public static void StartLearning(string topic)
    {
        LearnDataPaths.EnsureDirectories();
        RefreshData(topic);
        if (string.IsNullOrWhiteSpace(topic) || topic.Equals("all", StringComparison.OrdinalIgnoreCase) || topic.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            LaunchMasterHub();
        }
        else
        {
            LaunchTool(topic, "auto");
        }
    }

    public static void LaunchMasterHub()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[bold cyan]🎓 Antigravity Master Learning Suite[/]").RuleStyle("grey"));

            var options = new[]
            {
                "🎌 Japanese Language Suite (Kana, Kanji, JLPT)",
                "📖 English & Vocabulary (Vocab Drill, Word of Day, Flashcards)",
                "💻 C# & .NET Masterclass (Quiz, Snippets, Cheat Sheets)",
                "🧩 DSA & System Architecture (Algo Visualizer, Big-O, Tracker)",
                "💼 Career & Technical Interview (Questions, STAR Builder, Mock)",
                "📊 Progress & Spaced Repetition Queue",
                "← Exit Learning Suite"
            };

            var idx = SpectreMenu.Show("Select Learning Domain", options, 0);
            if (idx == 0) LaunchTool("jp", "auto");
            else if (idx == 1) LaunchTool("en", "auto");
            else if (idx == 2) LaunchTool("cs", "auto");
            else if (idx == 3) LaunchTool("dsa", "auto");
            else if (idx == 4) LaunchTool("interview", "auto");
            else if (idx == 5) ProgressDashboard.Show();
            else break;
        }
    }

    public static void RefreshData(string topic)
    {
        var cfg = ObsidianBridge.LoadConfig();
        if (cfg == null || !Directory.Exists(cfg.VaultPath))
        {
            return;
        }
        var tagMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["jp"] = ["japanese", "jp", "jlpt"],
            ["japanese"] = ["japanese", "jp", "jlpt"],
            ["en"] = ["english", "vocab", "idiom", "grammar"],
            ["english"] = ["english", "vocab", "idiom"],
            ["cs"] = ["csharp", "dotnet", "cs", "snippet"],
            ["csharp"] = ["csharp", "dotnet", "cs"],
            ["dsa"] = ["dsa", "algorithm", "leetcode", "problem"],
            ["interview"] = ["interview", "behavioral", "system-design"],
        };
        var tags = tagMap.GetValueOrDefault(topic, [topic]);
        var notes = ResourceScanner.FindNotesByTag(cfg.VaultPath, tags);
        if (notes.Length == 0)
        {
            return;
        }
        var items = new List<ExtractedItem>();
        SpectreProgress.BulkProgress($"Extracting {notes.Length} notes", notes, (_, note) =>
        {
            var fakeEntry = new ResourceEntry("tmp", note, "md", System.IO.Path.GetFileNameWithoutExtension(note), tags, [topic], "auto", "obsidian_note", null, 0, DateTimeOffset.Now.ToString("o"), null, "pending", null, 0, [], true, true);
            items.AddRange(MdExtractor.Extract(note, fakeEntry));
        });
        TemplateGenerator.RouteItemsToFiles([.. items]);
        SpectrePanel.Success($"Generated {items.Count} items from {notes.Length} notes → learn/");
    }

    public static void LaunchTool(string topic, string level)
    {
        LearnDataPaths.EnsureDirectories();
        switch (topic.ToLower())
        {
            case "jp" or "japanese":
                var jpTools = new[]
                {
                    "🎌 JLPT Vocabulary Drill (N5)",
                    "🌸 Hiragana & Katakana Kana Quiz",
                    "⛩️ Kanji Radical & Stroke Lookup"
                };
                var jpChoice = SpectreMenu.Show("Japanese Learning Suite", jpTools, 0);
                if (jpChoice == 0) JlptVocabDrill.Run("N5");
                else if (jpChoice == 1) KanaQuiz.Run("hiragana");
                else if (jpChoice == 2) KanjiLookup.Run();
                break;
            case "en" or "english":
                var enTools = new[]
                {
                    "📖 English Vocab Drill",
                    "🌟 Word of the Day",
                    "🎴 Flashcard Decks"
                };
                var enChoice = SpectreMenu.Show("English & Vocabulary Suite", enTools, 0);
                if (enChoice == 0) VocabDrill.Run("Intermediate");
                else if (enChoice == 1)
                {
                    var word = WordOfDay.Pick();
                    if (word != null) WordOfDay.Render(word);
                    else SpectrePanel.Warning("No word of the day available.");
                }
                else if (enChoice == 2) FlashcardEngine.PickAndRun(LearnDataPaths.DecksDir);
                break;
            case "cs" or "csharp":
                var csTools = new[]
                {
                    "💻 C# & .NET Interactive Quiz",
                    "⚡ Code Snippet Library",
                    "📄 Developer Cheat Sheets"
                };
                var csChoice = SpectreMenu.Show("C# & Dev Masterclass Suite", csTools, 0);
                if (csChoice == 0) CsharpQuiz.Run();
                else if (csChoice == 1) SnippetLibrary.Run();
                else if (csChoice == 2) CheatSheetBrowser.Run();
                break;
            case "dsa":
                var dsaTools = new[]
                {
                    "🧩 Algorithm Step Visualizer",
                    "📊 Big-O Complexity Sheet",
                    "🎯 Coding Problem Tracker"
                };
                var dsaChoice = SpectreMenu.Show("DSA & System Architecture Suite", dsaTools, 0);
                if (dsaChoice == 0) AlgoVisualizer.PickAndRun();
                else if (dsaChoice == 1) ComplexitySheet.Run();
                else if (dsaChoice == 2) ProblemTracker.Run();
                break;
            case "interview":
                var intTools = new[]
                {
                    "💼 Technical & Behavioral Question Bank",
                    "⭐ STAR Answer Builder",
                    "⏱️ Mock Interview Session Timer"
                };
                var intChoice = SpectreMenu.Show("Career & Interview Suite", intTools, 0);
                if (intChoice == 0) InterviewBank.Run();
                else if (intChoice == 1) StarBuilder.Run();
                else if (intChoice == 2) MockInterviewTimer.Run(300);
                break;
            default:
                FlashcardEngine.PickAndRun(LearnDataPaths.DecksDir);
                break;
        }
    }
}
