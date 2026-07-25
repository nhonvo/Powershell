namespace AgyTui.UI.Screens.Ide;

public static class GitDiffViewer
{
    public static string BuildDiffArgs(string? filePath)
    {
        return filePath != null ? $"diff \"{filePath.Trim('\"')}\"" : "diff";
    }

    public static void ShowDiff(string workspacePath, string? filePath = null)
    {
        var args = BuildDiffArgs(filePath);
        var output = RunGit(workspacePath, args);
        if (string.IsNullOrWhiteSpace(output))
        {
            SpectrePanel.Info("No changes to show.");
            return;
        }
        var lines = ColorizeHunk(output.Split('\n'));
        SpectrePager.Show($"Diff: {Path.GetFileName(workspacePath)}", lines);
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
