using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public static class GitHelper
{
    private static readonly string[] CommitTypes = ["feat", "fix", "docs", "style", "refactor", "test", "chore", "ci"];

    public static void ShowStatus()
    {
        var branch = RunGit("branch --show-current").Trim();
        if (string.IsNullOrEmpty(branch)) branch = "main";

        AnsiConsole.Write(new Rule($"[bold cyan]Git Status ({branch.EscapeMarkup()})[/]").RuleStyle("grey"));
        var output = RunGit("status --short");
        if (string.IsNullOrWhiteSpace(output))
        {
            SpectrePanel.Success("Working tree clean. No changes detected.");
            return;
        }

        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
        table.AddColumn("State");
        table.AddColumn("File Path");

        foreach (var line in output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var trimmed = line.TrimEnd('\r');
            if (trimmed.Length < 3) continue;
            var code = trimmed[..2];
            var file = trimmed[3..].Trim();

            var (label, color) = code switch
            {
                "??" => ("Untracked", "dim"),
                " M" or "M " or "MM" => ("Modified", "yellow"),
                " A" or "A " => ("Staged", "green"),
                " D" or "D " => ("Deleted", "red"),
                " R" or "R " => ("Renamed", "cyan"),
                _ => ("Changed", "white")
            };

            table.AddRow($"[{color}]{label} ({code.Trim()})[/]", $"[bold white]{file.EscapeMarkup()}[/]");
        }

        AnsiConsole.Write(table);
    }

    public static void ConventionalCommitWizard()
    {
        AnsiConsole.Write(new Rule("[bold cyan]Conventional Commit Wizard[/]").RuleStyle("grey"));
        var typeIdx = SpectreMenu.Show("Commit Type", CommitTypes, 0, false);
        if (typeIdx < 0) return;
        var commitType = CommitTypes[typeIdx];
        var scope = AnsiConsole.Ask<string>("[dim]Scope[/] (optional, press Enter to skip):", string.Empty).Trim();
        var scopePart = string.IsNullOrWhiteSpace(scope) ? string.Empty : $"({scope})";

        string description = "";
        var draftWithAI = AnsiConsole.Confirm("Would you like local AI to draft the description from staged diff?");
        string draft = "";
        if (draftWithAI)
        {
            var diff = RunGit("diff --cached").Trim();
            if (string.IsNullOrEmpty(diff))
            {
                SpectrePanel.Warning("No staged diff found. Please stage changes first.");
            }
            else
            {
                AnsiConsole.MarkupLine("[cyan]Querying local AI to draft description...[/]");
                draft = AgyAiCore.GenerateDraftDescription(diff);
                if (!string.IsNullOrEmpty(draft))
                {
                    AnsiConsole.MarkupLine($"[green]Suggested draft:[/] {draft}");
                }
            }
        }

        while (true)
        {
            description = string.IsNullOrEmpty(draft)
                ? AnsiConsole.Ask<string>("[cyan]Short description[/]:").Trim()
                : AnsiConsole.Ask<string>("[cyan]Short description[/]:", draft).Trim();
            if (description.Length is >= 5 and <= 72) break;
            SpectrePanel.Warning("Description must be 5–72 characters.");
        }

        var breaking = AnsiConsole.Ask<string>("[dim]Breaking changes[/] (optional):", string.Empty).Trim();
        var issues = AnsiConsole.Ask<string>("[dim]Issues closed[/] (e.g. #42, optional):", string.Empty).Trim();
        var sb = new StringBuilder($"{commitType}{scopePart}: {description}");
        if (!string.IsNullOrWhiteSpace(breaking)) sb.Append($"\n\nBREAKING CHANGE: {breaking}");
        if (!string.IsNullOrWhiteSpace(issues)) sb.Append($"\n\nCloses {issues}");
        var message = sb.ToString();
        AnsiConsole.Write(new Panel(message.EscapeMarkup())
        {
            Header = new PanelHeader("[bold]Commit Message Preview[/]"),
            Border = BoxBorder.Rounded
        });
        if (!AnsiConsole.Confirm("Commit now?")) return;
        var exitCode = RunGitDirect($"commit -m \"{message.Replace("\"", "\\\"")}\"");
        if (exitCode == 0) SpectrePanel.Success("Committed successfully.");
        else SpectrePanel.Error($"git commit failed (exit {exitCode}).");
    }

    public static void InvokeGitUndo()
    {
        var lastLog = RunGit("log --oneline -1").Trim();
        AnsiConsole.MarkupLine($"[yellow]Last commit:[/] {lastLog.EscapeMarkup()}");
        if (!AnsiConsole.Confirm("Soft-reset (keep changes staged)?")) return;
        var exit = RunGitDirect("reset HEAD~1 --soft");
        if (exit == 0) SpectrePanel.Success("Last commit undone. Changes kept in working directory.");
        else SpectrePanel.Error($"git reset failed (exit {exit}).");
    }

    public static void ShowBranches()
    {
        var output = RunGit("branch -a --sort=-committerdate");
        if (string.IsNullOrWhiteSpace(output))
        {
            SpectrePanel.Warning("Not a git repository or no branches found.");
            return;
        }
        var branches = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        AnsiConsole.Write(new Rule("[bold cyan]Git Branches (Sorted by recent activity)[/]").RuleStyle("grey"));
        var selectedIdx = SpectreMenu.Show("Select Branch to Checkout", branches, 0, false);
        if (selectedIdx < 0) return;

        var targetBranch = branches[selectedIdx].TrimStart('*', ' ').Trim();
        if (targetBranch.StartsWith("remotes/"))
        {
            var parts = targetBranch.Split('/');
            if (parts.Length >= 3) targetBranch = parts[^1];
        }

        AnsiConsole.MarkupLine($"[cyan]Checking out branch:[/] [bold green]{targetBranch.EscapeMarkup()}[/]");
        var exitCode = RunGitDirect($"checkout \"{targetBranch}\"");
        if (exitCode == 0) SpectrePanel.Success($"Checked out '{targetBranch}'.");
        else SpectrePanel.Error($"git checkout failed (exit {exitCode}).");
    }

    public static void ShowLog()
    {
        var output = RunGit("log --oneline --graph --decorate -n 50");
        if (string.IsNullOrWhiteSpace(output))
        {
            SpectrePanel.Info("No commit history found.");
            return;
        }
        SpectrePager.Show("Git Commit Log (Last 50)", output);
    }

    public static void Pull()
    {
        AnsiConsole.MarkupLine("[cyan]Pulling latest changes from remote...[/]");
        var exitCode = RunGitDirect("pull");
        if (exitCode == 0) SpectrePanel.Success("Git pull completed successfully.");
        else SpectrePanel.Error($"git pull failed (exit {exitCode}).");
    }

    public static void Push()
    {
        AnsiConsole.MarkupLine("[cyan]Pushing local commits to remote...[/]");
        var exitCode = RunGitDirect("push");
        if (exitCode == 0) SpectrePanel.Success("Git push completed successfully.");
        else SpectrePanel.Error($"git push failed (exit {exitCode}).");
    }

    public static void AddAll()
    {
        AnsiConsole.MarkupLine("[cyan]Staging all modified and new files...[/]");
        var exitCode = RunGitDirect("add .");
        if (exitCode == 0) SpectrePanel.Success("Staged all workspace changes.");
        else SpectrePanel.Error($"git add failed (exit {exitCode}).");
    }

    public static void Fetch()
    {
        AnsiConsole.MarkupLine("[cyan]Fetching remote references...[/]");
        var exitCode = RunGitDirect("fetch");
        if (exitCode == 0) SpectrePanel.Success("Fetched latest remote references.");
        else SpectrePanel.Error($"git fetch failed (exit {exitCode}).");
    }

    private static string RunGit(string args) => Helpers.ProcessRunner.RunCapture("git", args);

    private static int RunGitDirect(string args)
    {
        return Helpers.ProcessRunner.Run("git", args);
    }
}
