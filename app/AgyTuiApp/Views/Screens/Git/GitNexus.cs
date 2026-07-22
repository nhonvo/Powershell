using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public sealed record RepoStatus(string Name, string RepoPath, string Branch, int AheadBy, int BehindBy, int DirtyFiles, string LastCommit);
public sealed record RepoNode(string Name, string NodePath, string Kind, string[] DependsOn);

public static class GitNexus
{
    public static RepoStatus[] FetchAllStatuses()
    {
        var workspaces = WorkspaceRegistry.GetWorkspaces();
        return workspaces.AsParallel().Select(w => FetchStatus(w.Name, w.WorkspacePath)).OfType<RepoStatus>().ToArray();
    }

    public static void ShowLiveDashboard()
    {
        var cols = new[] { "Repo", "Branch", "↑↓", "Dirty", "Last Commit" };
        AnsiConsole.Write(new Rule("[bold cyan]AGY — Git Nexus[/]").RuleStyle("grey"));
        AnsiConsole.MarkupLine("[dim] Auto-refreshes · Press any key to exit[/]");
        var t = new Table { Border = TableBorder.Rounded };
        foreach (var col in cols) t.AddColumn(new TableColumn($"[bold]{col.EscapeMarkup()}[]"));
        AnsiConsole.Live(t).Start(ctx =>
        {
            int ticks = 0;
            while (!Console.KeyAvailable && ticks < 20)
            {
                t.Rows.Clear();
                var statuses = FetchAllStatuses();
                foreach (var s in statuses)
                {
                    var sync = s.AheadBy > 0 ? $"[yellow]↑{s.AheadBy}[/]" : s.BehindBy > 0 ? $"[cyan]↓{s.BehindBy}[/]" : "[green]sync[/]";
                    t.AddRow(s.Name.EscapeMarkup(), s.Branch.EscapeMarkup(), sync, s.DirtyFiles > 0 ? $"[yellow]{s.DirtyFiles}[/]" : "[dim]0[/]", s.LastCommit.EscapeMarkup());
                }
                ctx.Refresh();
                Thread.Sleep(30_000);
                ticks++;
            }
            if (Console.KeyAvailable) Console.ReadKey(true);
        });
    }

    public static void ShowCommitGraph(string workspacePath, int count = 20)
    {
        var output = Git(workspacePath, $"log --graph --oneline --decorate -n {count}");
        if (string.IsNullOrWhiteSpace(output))
        {
            SpectrePanel.Info("No commits found.");
            return;
        }
        SpectrePager.Show($"Commit Graph: {Path.GetFileName(workspacePath)}", output.Split('\n'));
    }

    private static RepoStatus? FetchStatus(string name, string path)
    {
        if (!Directory.Exists(path)) return null;

        try
        {
            var branch = Git(path, "rev-parse --abbrev-ref HEAD").Trim();
            var dirty = Git(path, "status --short").Split('\n').Count(l => !string.IsNullOrWhiteSpace(l));
            var ahead = 0;
            var behind = 0;
            var ab = Git(path, "rev-list --left-right --count HEAD...@{upstream}").Trim().Split('\t');
            if (ab.Length == 2)
            {
                int.TryParse(ab[0], out ahead);
                int.TryParse(ab[1], out behind);
            }
            var last = Git(path, "log --oneline -1").Trim();
            return new RepoStatus(name, path, branch, ahead, behind, dirty, last);
        }
        catch
        {
            return null;
        }
    }

    private static string Git(string workingDir, string args)
    {
        return Helpers.ProcessRunner.RunCapture("git", args, workingDir);
    }
}

public static class RepoGraph
{
    public static RepoNode[] Build()
    {
        var workspaces = WorkspaceRegistry.GetWorkspaces();
        return workspaces.SelectMany(w => ParseWorkspace(w.Name, w.WorkspacePath)).ToArray();
    }

    public static void Show(RepoNode[] graph)
    {
        AnsiConsole.Write(new Rule("[bold cyan]Repo Dependency Graph[/]").RuleStyle("grey"));
        var tree = new Tree("[bold cyan]Workspaces[/]");
        foreach (var node in graph)
        {
            var n = tree.AddNode($"[bold]{node.Name.EscapeMarkup()}[/] [dim]({node.Kind})[/]");
            foreach (var dep in node.DependsOn) n.AddNode($"→ {dep.EscapeMarkup()}");
            if (node.DependsOn.Length == 0) n.AddNode("[dim](no dependencies)[/]");
        }
        AnsiConsole.Write(tree);
        AnsiConsole.MarkupLine("[dim] Press any key...[/]");
        Console.ReadKey(true);
    }

    public static RepoNode[] ParseCsproj(string path)
    {
        if (!File.Exists(path)) return [];
        var content = File.ReadAllText(path);
        var refs = Regex.Matches(content, @"<ProjectReference\s+Include=""([^""]+)""").Select(m => Path.GetFileNameWithoutExtension(m.Groups[1].Value)).ToArray();
        return [new RepoNode(Path.GetFileNameWithoutExtension(path), path, "csproj", refs)];
    }

    public static RepoNode ParseNpm(string path)
    {
        if (!File.Exists(path)) return new("", path, "npm", []);

        try
        {
            var doc = JsonDocument.Parse(File.ReadAllText(path));
            var name = doc.RootElement.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
            var deps = doc.RootElement.TryGetProperty("dependencies", out var d) ? d.EnumerateObject().Select(p => p.Name).ToArray() : Array.Empty<string>();
            return new RepoNode(name, path, "npm", deps);
        }
        catch
        {
            return new("", path, "npm", []);
        }
    }

    private static RepoNode[] ParseWorkspace(string name, string path)
    {
        if (!Directory.Exists(path)) return [];
        var results = new List<RepoNode>();
        foreach (var csproj in Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories)) results.AddRange(ParseCsproj(csproj));
        foreach (var pkg in Directory.GetFiles(path, "package.json", SearchOption.AllDirectories)) results.Add(ParseNpm(pkg));
        if (results.Count == 0) results.Add(new RepoNode(name, path, "unknown", []));
        return [.. results];
    }
}

public static class GitNexusStats
{
    public static void Run()
    {
        AnsiConsole.Clear();
        var workspaces = WorkspaceRegistry.GetWorkspaces();
        var commitsByRepo = workspaces.ToDictionary(w => w.Name, w => CountCommitsSince(w.WorkspacePath, 7));
        ShowCommitBarChart(commitsByRepo);
        AnsiConsole.WriteLine();
        ShowBranchTree(workspaces);
        AnsiConsole.MarkupLine("[dim] Press any key...[/]");
        Console.ReadKey(true);
    }

    public static void ShowCommitBarChart(Dictionary<string, int> commitsByRepo)
    {
        AnsiConsole.Write(new Rule("[bold cyan]Commits this week by repo[/]").RuleStyle("grey"));
        if (commitsByRepo.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]  No commit data recorded this week.[/]");
            return;
        }
        var chart = new BarChart().Width(50).Label("[bold]Commits[/]").CenterLabel();
        foreach (var (repo, count) in commitsByRepo.OrderByDescending(x => x.Value)) chart.AddItem(repo, count, Color.Cyan1);
        AnsiConsole.Write(chart);
    }

    public static void ShowBranchTree(WorkspaceEntry[] workspaces)
    {
        AnsiConsole.Write(new Rule("[bold cyan]Branch Structure[/]").RuleStyle("grey"));
        var tree = new Tree("[bold cyan]Repos[/]");
        foreach (var ws in workspaces)
        {
            var node = tree.AddNode($"[bold]{ws.Name.EscapeMarkup()}[/]");
            foreach (var branch in GetBranches(ws.WorkspacePath)) node.AddNode(branch.StartsWith("*") ? $"[green]{branch.EscapeMarkup()}[/]" : branch.EscapeMarkup());
        }
        AnsiConsole.Write(tree);
    }

    private static int CountCommitsSince(string path, int days)
    {
        if (!Directory.Exists(path)) return 0;
        try
        {
            var output = Helpers.ProcessRunner.RunCapture("git", $"log --since={days}.days --oneline", path);
            return output.Split('\n').Count(l => !string.IsNullOrWhiteSpace(l));
        }
        catch
        {
            return 0;
        }
    }

    private static string[] GetBranches(string path)
    {
        if (!Directory.Exists(path)) return [];
        try
        {
            var output = Helpers.ProcessRunner.RunCapture("git", "branch", path);
            return output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()).ToArray();
        }
        catch
        {
            return [];
        }
    }
}
