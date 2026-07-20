using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public sealed record ObsidianConfig(string VaultPath);
public sealed record NoteMatch(string Title, string RelativePath, string FullPath);
public sealed record NoteNode(string Title, string NodePath, string[] OutLinks);
public sealed record NoteFrontmatter(string[] Tags, string Topic, string Level, string Type, string Source, string Difficulty);

public static class ObsidianBridge
{
    public static void Configure()
    {
        var vaultPath = AnsiConsole.Ask<string>("[cyan]Obsidian vault path:[/]").Trim();
        if (!Directory.Exists(vaultPath))
        {
            SpectrePanel.Error($"Directory not found: {vaultPath}");
            return;
        }
        LearnDataPaths.SaveJson(LearnDataPaths.ObsidianCfgFile, new ObsidianConfig(vaultPath));
        SpectrePanel.Success($"Vault configured: {vaultPath}");
    }

    public static ObsidianConfig? LoadConfig()
    {
        var cfg = LearnDataPaths.LoadJson<ObsidianConfig>(LearnDataPaths.ObsidianCfgFile);
        if (cfg != null && Directory.Exists(cfg.VaultPath)) return cfg;

        string localLearnVault = System.IO.Path.Combine(LearnDataPaths.BaseDirectory, "learn");
        string defaultVault = Directory.Exists(localLearnVault)
            ? localLearnVault
            : System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "project", "learning");

        if (Directory.Exists(defaultVault))
        {
            var fallback = new ObsidianConfig(defaultVault);
            try { LearnDataPaths.SaveJson(LearnDataPaths.ObsidianCfgFile, fallback); } catch { }
            return fallback;
        }
        return cfg;
    }

    public static void Run()
    {
        var cfg = LoadConfig();
        if (cfg == null)
        {
            SpectrePanel.Warning("Obsidian vault not configured. Run: obsidian → Configure.");
        }
        var vaultPath = cfg?.VaultPath ?? "";
        while (true)
        {
            var actions = new[]
            {
                "Search notes", "Browse by tag", "Open today's daily note", "Obsidian graph", "Configure vault path", "← Back"
            };
            var idx = SpectreMenu.Show("Obsidian", actions, 0, false);
            switch (idx)
            {
                case 0:
                    if (Directory.Exists(vaultPath)) SearchNotes(vaultPath, "");
                    break;
                case 1:
                    if (Directory.Exists(vaultPath)) ListByTag(vaultPath);
                    break;
                case 2:
                    if (Directory.Exists(vaultPath)) ShowDailyNote(vaultPath);
                    break;
                case 3:
                    if (Directory.Exists(vaultPath)) ObsidianGraph.Run(vaultPath);
                    break;
                case 4:
                    Configure();
                    cfg = LoadConfig();
                    vaultPath = cfg?.VaultPath ?? "";
                    break;
                default:
                    return;
            }
            if (!Directory.Exists(vaultPath) && idx < 4) SpectrePanel.Warning("Configure vault path first.");
        }
    }

    public static void SearchNotes(string vaultPath, string query)
    {
        while (true)
        {
            query = AnsiConsole.Ask<string>("[cyan]Search notes:[/]", query).Trim();
            if (string.IsNullOrWhiteSpace(query)) return;
            var matches = SearchFiles(vaultPath, query);
            if (matches.Length == 0)
            {
                SpectrePanel.Info($"No notes matched '{query}'.");
                continue;
            }
            var items = matches.Select(m => $"{m.Title,-40} [dim]{m.RelativePath}[/]").ToArray();
            var idx = SpectreMenu.Show($"Results for \"{query}\"", items, 0, false);
            if (idx >= 0) ShowNote(matches[idx].FullPath, matches[idx].Title);
            return;
        }
    }

    public static NoteMatch[] FindByTag(string vaultPath, string tag)
    {
        if (!Directory.Exists(vaultPath)) return [];
        return Directory.GetFiles(vaultPath, "*.md", SearchOption.AllDirectories)
            .Select(f => (path: f, fm: ParseFrontmatter(f)))
            .Where(x => x.fm != null && x.fm.Tags.Any(t => t.Contains(tag, StringComparison.OrdinalIgnoreCase)))
            .Select(x => new NoteMatch(Path.GetFileNameWithoutExtension(x.path), Path.GetRelativePath(vaultPath, x.path), x.path))
            .ToArray();
    }

    public static void ShowDailyNote(string vaultPath)
    {
        var noteFile = Path.Combine(vaultPath, $"{DateTime.Today:yyyy-MM-dd}.md");
        if (!File.Exists(noteFile))
        {
            File.WriteAllText(noteFile, $"---\ntags: [daily]\ncreated: {DateTime.Today:yyyy-MM-dd}\n---\n\n# {DateTime.Today:yyyy-MM-dd}\n\n", Encoding.UTF8);
            SpectrePanel.Info($"Created daily note: {noteFile}");
        }
        ShowNote(noteFile, DateTime.Today.ToString("yyyy-MM-dd"));
    }

    public static void ListByTag(string vaultPath)
    {
        var allTags = Directory.GetFiles(vaultPath, "*.md", SearchOption.AllDirectories)
            .SelectMany(f => ParseFrontmatter(f)?.Tags ?? [])
            .Distinct()
            .OrderBy(t => t)
            .ToArray();
        if (allTags.Length == 0)
        {
            SpectrePanel.Info("No tags found.");
            return;
        }
        var idx = SpectreMenu.Show("Browse by Tag", allTags, 0, true);
        if (idx < 0) return;
        var matches = FindByTag(vaultPath, allTags[idx]);
        if (matches.Length == 0)
        {
            SpectrePanel.Info($"No notes tagged #{allTags[idx]}");
            return;
        }
        var noteIdx = SpectreMenu.Show($"#{allTags[idx]} ({matches.Length} notes)", matches.Select(m => m.Title).ToArray(), 0, true);
        if (noteIdx >= 0) ShowNote(matches[noteIdx].FullPath, matches[noteIdx].Title);
    }

    public static void AppendStudySummary(string vaultPath, string topic, string summary)
    {
        ShowDailyNote(vaultPath);
        var noteFile = Path.Combine(vaultPath, $"{DateTime.Today:yyyy-MM-dd}.md");
        var block = $"\n## AGY Study — {DateTime.Now:HH:mm}\n{summary}\n";
        File.AppendAllText(noteFile, block, Encoding.UTF8);
        SpectrePanel.Success("Summary appended to daily note.");
    }

    public static NoteFrontmatter? ParseFrontmatter(string notePath)
    {
        try
        {
            var lines = File.ReadLines(notePath).Take(20).ToArray();
            if (lines.Length == 0 || lines[0] != "---") return null;
            var fm = lines.Skip(1).TakeWhile(l => l != "---").ToArray();
            string GetField(string key) => fm.FirstOrDefault(l => l.StartsWith(key + ":"))?.Split(':', 2)[1].Trim() ?? "";
            string[] GetTags()
            {
                var tagLine = GetField("tags");
                return Regex.Matches(tagLine, @"[\w-]+").Select(m => m.Value).ToArray();
            }
            return new NoteFrontmatter(GetTags(), GetField("topic"), GetField("level"), GetField("type"), GetField("source"), GetField("difficulty"));
        }
        catch
        {
            return null;
        }
    }

    private static NoteMatch[] SearchFiles(string vaultPath, string query)
    {
        return Directory.GetFiles(vaultPath, "*.md", SearchOption.AllDirectories)
            .Where(f => Path.GetFileNameWithoutExtension(f).Contains(query, StringComparison.OrdinalIgnoreCase)
                     || File.ReadLines(f).Take(5).Any(l => l.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .Select(f => new NoteMatch(Path.GetFileNameWithoutExtension(f), Path.GetRelativePath(vaultPath, f), f))
            .ToArray();
    }

    private static void ShowNote(string path, string title)
    {
        if (!File.Exists(path))
        {
            SpectrePanel.Error($"File not found: {path}");
            return;
        }
        var lines = File.ReadAllLines(path);
        SpectrePager.Show(title, lines);
    }
}

public static class ObsidianGraph
{
    public static void Run(string vaultPath)
    {
        var graph = BuildGraph(vaultPath);
        var titles = graph.Select(n => n.Title).ToArray();
        var idx = SpectreMenu.Show("Obsidian Graph — Select root note", titles, 0, true);
        if (idx < 0) return;
        ShowFromRoot(graph, graph[idx].Title, 2);
        AnsiConsole.WriteLine();
        if (AnsiConsole.Confirm("Show orphan notes?", defaultValue: false)) ShowOrphans(graph);
    }

    public static NoteNode[] BuildGraph(string vaultPath)
    {
        if (!Directory.Exists(vaultPath)) return [];
        return Directory.GetFiles(vaultPath, "*.md", SearchOption.AllDirectories).Select(f =>
        {
            var content = File.ReadAllText(f);
            var links = Regex.Matches(content, @"\[\[([^\]|]+)(?:\|[^\]]+)?\]\]").Select(m => m.Groups[1].Value.Trim()).ToArray();
            return new NoteNode(Path.GetFileNameWithoutExtension(f), f, links);
        }).ToArray();
    }

    public static NoteNode[] FindOrphans(NoteNode[] graph)
    {
        var allTargets = new HashSet<string>(graph.SelectMany(n => n.OutLinks), StringComparer.OrdinalIgnoreCase);
        return graph.Where(n => n.OutLinks.Length == 0 && !allTargets.Contains(n.Title)).ToArray();
    }

    public static void ShowFromRoot(NoteNode[] graph, string rootTitle, int depth = 2)
    {
        AnsiConsole.Write(new Rule($"[bold cyan]Obsidian Graph — root: {rootTitle.EscapeMarkup()} depth: {depth}[/]").RuleStyle("grey"));
        var root = graph.FirstOrDefault(n => n.Title.Equals(rootTitle, StringComparison.OrdinalIgnoreCase));
        if (root == null)
        {
            SpectrePanel.Warning($"Note '{rootTitle}' not found.");
            return;
        }
        var tree = new Tree($"[bold]{root.Title.EscapeMarkup()}[/]");
        AddChildren(tree, graph, root, depth, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        AnsiConsole.Write(tree);
    }

    public static void ShowOrphans(NoteNode[] graph)
    {
        var orphans = FindOrphans(graph);
        if (orphans.Length == 0)
        {
            SpectrePanel.Success("No orphan notes found.");
            return;
        }
        AnsiConsole.Write(new Rule("[bold cyan]Orphan Notes[/]").RuleStyle("grey"));
        var rows = orphans.Select(n => new[] { n.Title, n.NodePath }).ToArray();
        SpectreTable.Render(["Note", "Path"], rows);
    }

    private static void AddChildren(IHasTreeNodes parent, NoteNode[] graph, NoteNode node, int depth, HashSet<string> visited)
    {
        if (depth <= 0 || !visited.Add(node.Title)) return;
        foreach (var link in node.OutLinks)
        {
            var child = graph.FirstOrDefault(n => n.Title.Equals(link, StringComparison.OrdinalIgnoreCase));
            var childNode = parent.AddNode(child != null ? link.EscapeMarkup() : $"[dim]{link.EscapeMarkup()} (not found)[/]");
            if (child != null) AddChildren(childNode, graph, child, depth - 1, visited);
        }
    }
}

public static class ObsidianStudySync
{
    public static void OfferSync(StudySummary summary)
    {
        var cfg = ObsidianBridge.LoadConfig();
        if (cfg == null || !Directory.Exists(cfg.VaultPath)) return;
        AnsiConsole.Write(new Panel($"[bold]Append session summary to {DateTime.Today:yyyy-MM-dd}.md?[/]")
        {
            Header = new PanelHeader("[cyan]ℹ Sync to Obsidian?[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1)
        });
        if (!AnsiConsole.Confirm("Sync?", defaultValue: false)) return;
        var block = FormatSummaryBlock(summary);
        ObsidianBridge.AppendStudySummary(cfg.VaultPath, summary.Topic, block);
    }

    public static string FormatSummaryBlock(StudySummary s) =>
        $"- **Topic:** {s.Topic}\n" +
        $"- **Score:** {s.Score} / {s.Total} ({(s.Total > 0 ? s.Score * 100 / s.Total : 0)}%)\n" +
        (s.WeakItems.Length > 0 ? $"- **Weak words:** {string.Join(", ", s.WeakItems)}\n" : "") +
        $"- **Duration:** {s.DurationMinutes} min";
}
