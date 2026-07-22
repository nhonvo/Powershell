using System;
using System.IO;
using System.Linq;
using Spectre.Console;
using AgyTui.Components;
using AgyTui.Helpers;

namespace AgyTui;

public static class GitDiffViewer
{
    public static void ShowDiff(string workspacePath, string? filePath = null)
    {
        var args = filePath != null ? $"diff {filePath}" : "diff";
        var output = RunGit(workspacePath, args);
        if (string.IsNullOrWhiteSpace(output))
        {
            SpectrePanel.Info("No changes to show.");
            return;
        }
        var lines = ColorizeHunk(output.Split('\n'));
        SpectrePager.Show($"Diff: {Path.GetFileName(workspacePath)}", lines);
    }

    public static void ShowCommitDiff(string workspacePath, string commitHash)
    {
        var output = RunGit(workspacePath, $"show {commitHash}");
        if (string.IsNullOrWhiteSpace(output))
        {
            SpectrePanel.Info("No diff for that commit.");
            return;
        }
        var lines = ColorizeHunk(output.Split('\n'));
        SpectrePager.Show($"Commit: {commitHash[..Math.Min(7, commitHash.Length)]}", lines);
    }

    private static string[] ColorizeHunk(string[] diffLines) => diffLines.Select(l => l switch
    {
        _ when l.StartsWith("+") && !l.StartsWith("+++") => $"[green]{l.EscapeMarkup()}[/]",
        _ when l.StartsWith("-") && !l.StartsWith("---") => $"[red]{l.EscapeMarkup()}[/]",
        _ when l.StartsWith("@@") => $"[cyan]{l.EscapeMarkup()}[/]",
        _ when l.StartsWith("diff ") || l.StartsWith("index ") || l.StartsWith("--- ") || l.StartsWith("+++ ") => $"[dim]{l.EscapeMarkup()}[/]",
        _ => l.EscapeMarkup()
    }).ToArray();

    private static string RunGit(string workingDir, string args)
    {
        return ProcessRunner.RunCapture("git", args, workingDir);
    }
}
