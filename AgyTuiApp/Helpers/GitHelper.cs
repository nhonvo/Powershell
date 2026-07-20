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
        var output = RunGit("status --short");
        if (string.IsNullOrWhiteSpace(output))
        {
            SpectrePanel.Info("Working tree clean.");
            return;
        }
        AnsiConsole.Write(new Rule("[bold cyan]Git Status[/]").RuleStyle("grey"));
        foreach (var line in output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var status = line.Length >= 2 ? line[..2] : "??";
            var color = status.Trim() switch
            {
                "M" => "yellow",
                "A" => "green",
                "D" => "red",
                "??" => "dim",
                "R" => "cyan",
                _ => "white"
            };
            AnsiConsole.MarkupLine($"[{color}]{line.EscapeMarkup()}[/]");
        }
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
        var draftWithClaude = AnsiConsole.Confirm("Would you like Claude to draft the description from staged diff?");
        if (draftWithClaude)
        {
            var diff = RunGit("diff --cached").Trim();
            if (string.IsNullOrEmpty(diff))
            {
                SpectrePanel.Warning("No staged diff found. Please stage changes first.");
            }
            else
            {
                AnsiConsole.MarkupLine("[cyan]Sending staged diff to Claude to draft description...[/]");
                var tempFile = Path.Combine(Path.GetTempPath(), "staged_diff.diff");
                File.WriteAllText(tempFile, diff);
                AgyAiCore.InvokeClaude(["--prompt", $"Read the diff in {tempFile} and output ONLY a short (under 72 chars), clear description of the changes (no prefix/boilerplate) suitable for a git commit message."]);
                try { File.Delete(tempFile); } catch { }
            }
        }

        while (true)
        {
            description = AnsiConsole.Ask<string>("[cyan]Short description[/]:").Trim();
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

    private static string RunGit(string args) => Helpers.ProcessRunner.RunCapture("git", args);

    private static int RunGitDirect(string args)
    {
        return Helpers.ProcessRunner.Run("git", args);
    }
}
