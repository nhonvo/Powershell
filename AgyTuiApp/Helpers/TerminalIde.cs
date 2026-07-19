using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public static class FileExplorer
{
    public static string? Browse(string rootPath)
    {
        var current = Directory.Exists(rootPath) ? rootPath : Directory.GetCurrentDirectory();
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]IDE: {current.EscapeMarkup()}[/]").RuleStyle("grey"));
            var entries = new List<string> { ".. (go up)" };

            try
            {
                entries.AddRange(Directory.GetDirectories(current).Select(d => $"📁 {Path.GetFileName(d)}"));
                entries.AddRange(Directory.GetFiles(current).Select(f => $"{GetFileIcon(Path.GetExtension(f))} {Path.GetFileName(f)}"));
            }
            catch (Exception ex)
            {
                SpectrePanel.Error(ex.Message);
                return null;
            }
            entries.Add("← Exit Explorer");
            var idx = SpectreMenu.Show($"Explorer: {Path.GetFileName(current)}", [.. entries], 0, true);
            if (idx < 0 || idx == entries.Count - 1) return null;
            if (idx == 0)
            {
                var parent = Path.GetDirectoryName(current);
                if (parent != null) current = parent;
                continue;
            }
            var selected = entries[idx][3..].Trim();
            var fullPath = Path.Combine(current, selected);
            if (Directory.Exists(fullPath))
            {
                current = fullPath;
                continue;
            }
            if (File.Exists(fullPath)) return fullPath;
        }
    }

    private static string GetFileIcon(string ext) => ext.ToLower() switch
    {
        ".cs" => "⚙",
        ".json" => "📋",
        ".md" => "📝",
        ".txt" => "📄",
        ".ps1" => "⚡",
        ".sh" => "⚡",
        ".yaml" or ".yml" => "🔧",
        ".csproj" or ".sln" => "🏗",
        _ => "📄"
    };
}

public static class CodeViewer
{
    private static readonly SearchValues<char> StringDelimiters = SearchValues.Create(['"', '\'', '`']);

    private record SyntaxRule(string[] Keywords, string? CommentPattern);
    private static readonly Dictionary<string, SyntaxRule> SyntaxRules = new()
    {
        { ".cs", new SyntaxRule(["public", "private", "protected", "internal", "class", "void", "string", "int", "var", "return", "if", "else", "foreach", "using", "namespace", "static", "new"], @"//.*$") },
        { ".ps1", new SyntaxRule(["function", "param", "if", "else", "foreach", "return", "process"], @"#.*$") },
        { ".js", new SyntaxRule(["const", "let", "var", "function", "return", "if", "else", "for", "while", "class", "import", "export", "default"], @"//.*$") },
        { ".ts", new SyntaxRule(["const", "let", "var", "function", "return", "if", "else", "for", "while", "class", "import", "export", "default", "interface", "type", "public", "private"], @"//.*$") },
        { ".json", new SyntaxRule(["true", "false", "null"], null) },
        { ".py", new SyntaxRule(["def", "class", "return", "if", "elif", "else", "for", "while", "import", "from", "as", "in", "not", "and", "or"], @"#.*$") },
        { ".sh", new SyntaxRule(["if", "then", "elif", "else", "fi", "for", "in", "do", "done", "echo", "exit", "return"], @"#.*$") }
    };

    public static void Show(string filePath)
    {
        if (!File.Exists(filePath))
        {
            SpectrePanel.Error($"File not found: {filePath}");
            return;
        }
        var lines = LoadWithLineNumbers(filePath);
        SpectrePager.Show(Path.GetFileName(filePath), lines);
    }

    public static void ShowWithHighlight(string filePath, int[] highlightLines)
    {
        if (!File.Exists(filePath))
        {
            SpectrePanel.Error($"File not found: {filePath}");
            return;
        }
        var ext = Path.GetExtension(filePath).ToLower();
        var rawLines = File.ReadAllLines(filePath);
        var numbered = rawLines.Select((l, i) =>
        {
            var num = $"{i + 1,4} ";
            var colored = ColorizeToken(l, ext);
            return highlightLines.Contains(i + 1) ? $"[yellow]{num}→[/] {colored}" : $"[dim]{num}[/] {colored}";
        }).ToArray();
        SpectrePager.Show(Path.GetFileName(filePath), numbered);
    }

    private static string[] LoadWithLineNumbers(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLower();
        var lines = File.ReadAllLines(filePath);
        return lines.Select((l, i) => $"[dim]{i + 1,4}[/] {ColorizeToken(l, ext)}").ToArray();
    }

    internal static string ColorizeToken(string line, string ext)
    {
        if (string.IsNullOrWhiteSpace(line)) return string.Empty;
        var escaped = line.EscapeMarkup();
        if (SyntaxRules.TryGetValue(ext, out var rule))
        {
            foreach (var kw in rule.Keywords)
            {
                escaped = Regex.Replace(escaped, $@"\b{kw}\b", $"[blue]{kw}[/]");
            }
            if (rule.CommentPattern != null)
            {
                escaped = Regex.Replace(escaped, rule.CommentPattern, m => $"[green]{m.Value}[/]");
            }
        }
        return escaped;
    }
}

public static class SymbolSearch
{
    public static void BrowseSymbols(string filePath)
    {
        if (!File.Exists(filePath))
        {
            SpectrePanel.Error($"File not found: {filePath}");
            return;
        }
        var ext = Path.GetExtension(filePath).ToLower();
        var lines = File.ReadAllLines(filePath);
        var symbols = ExtractSymbols(lines, ext);
        if (symbols.Length == 0)
        {
            SpectrePanel.Info("No symbols found.");
            return;
        }
        var idx = SpectreMenu.Show($"Symbols: {Path.GetFileName(filePath)}", symbols, 0, true);
        if (idx < 0) return;
        var lineNum = FindSymbolLine(filePath, symbols[idx].Split(' ')[0]);
        if (lineNum.HasValue) CodeViewer.ShowWithHighlight(filePath, [lineNum.Value]);
    }

    public static int? FindSymbolLine(string filePath, string symbol)
    {
        var lines = File.ReadAllLines(filePath);
        for (int i = 0; i < lines.Length; i++)
            if (lines[i].Contains(symbol, StringComparison.OrdinalIgnoreCase)) return i + 1;
        return null;
    }

    private static string[] ExtractSymbols(string[] lines, string ext)
    {
        var symbols = new List<string>();
        string[] patterns = ext switch
        {
            ".cs" => [
                @"(public|private|internal|protected).*\s(class|interface|record|enum)\s+(\w+)",
                @"(public|private|static).*\s(\w+)\s*\("
            ],
            ".ts" or ".js" => [
                @"(export\s+)?(class|function|interface|type)\s+(\w+)",
                @"const\s+(\w+)\s*="
            ],
            ".ps1" => [
                @"function\s+([\w-]+)"
            ],
            _ => [
                @"\b(\w+)\s*[({]"
            ]
        };
        for (int i = 0; i < lines.Length; i++)
        {
            foreach (var pat in patterns)
            {
                var m = Regex.Match(lines[i], pat);
                if (m.Success)
                {
                    var name = m.Groups[m.Groups.Count - 1].Value;
                    if (name.Length > 2) symbols.Add($"{name,-30} ln {i + 1,4}");
                }
            }
        }
        return [.. symbols.Distinct()];
    }
}

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
        return Helpers.ProcessRunner.RunCapture("git", args, workingDir);
    }
}

public static class TerminalIde
{
    public static void Open(string? path = null)
    {
        var root = path ?? Directory.GetCurrentDirectory();
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]AGY — IDE: {root.EscapeMarkup()}[/]").RuleStyle("grey"));
            var actions = new[] { "Browse files", "Search in files", "View git diff", "Open file by path", "← Exit IDE" };
            var idx = SpectreMenu.Show("Terminal IDE", actions, 0, false);
            switch (idx)
            {
                case 0:
                    var file = FileExplorer.Browse(root);
                    if (file != null) OpenFile(file);
                    break;
                case 1:
                    SearchAcrossFiles(root, AnsiConsole.Ask<string>("[cyan]Search pattern:[/]").Trim());
                    break;
                case 2:
                    GitDiffViewer.ShowDiff(root);
                    break;
                case 3:
                    var fp = AnsiConsole.Ask<string>("[cyan]File path:[/]").Trim();
                    if (File.Exists(fp)) OpenFile(fp);
                    else SpectrePanel.Error($"File not found: {fp}");
                    break;
                default:
                    return;
            }
        }
    }

    public static void OpenFile(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLower();
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]IDE: {Path.GetFileName(filePath).EscapeMarkup()}[/]").RuleStyle("grey"));
            var actions = new[]
            {
                "View file", "Symbol search", "View diff (this file)", $"Edit ({(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "notepad" : "nano")})", "← Back"
            };
            var idx = SpectreMenu.Show("File actions", actions, 0, false);
            switch (idx)
            {
                case 0:
                    CodeViewer.Show(filePath);
                    break;
                case 1:
                    SymbolSearch.BrowseSymbols(filePath);
                    break;
                case 2:
                    GitDiffViewer.ShowDiff(Path.GetDirectoryName(filePath) ?? ".", filePath);
                    break;
                case 3:
                    LaunchEditor(filePath);
                    break;
                default:
                    return;
            }
        }
    }

    public static void SearchInFile(string filePath)
    {
        if (!File.Exists(filePath)) return;
        var pattern = AnsiConsole.Ask<string>("[cyan]Search pattern:[/]").Trim();
        var lines = File.ReadAllLines(filePath);
        var matches = lines.Select((l, i) => (line: l, num: i + 1)).Where(x => Regex.IsMatch(x.line, pattern, RegexOptions.IgnoreCase)).ToArray();
        if (matches.Length == 0)
        {
            SpectrePanel.Info($"No matches for '{pattern}'.");
            return;
        }
        CodeViewer.ShowWithHighlight(filePath, matches.Select(m => m.num).ToArray());
    }

    public static void SearchAcrossFiles(string rootPath, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return;
        var results = new List<string>();
        foreach (var f in Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories).Where(f => !f.Contains("bin") && !f.Contains("obj") && !f.Contains(".git")))
        {
            try
            {
                var lines = File.ReadAllLines(f);
                for (int i = 0; i < lines.Length; i++)
                    if (Regex.IsMatch(lines[i], pattern, RegexOptions.IgnoreCase))
                        results.Add($"{Path.GetRelativePath(rootPath, f)}:{i + 1}: {lines[i].Trim()}");
            }
            catch
            {
            }
            if (results.Count >= 100) break;
        }
        if (results.Count == 0)
        {
            SpectrePanel.Info($"No matches for '{pattern}'.");
            return;
        }
        SpectrePager.Show($"Search results: {pattern}", [.. results]);
    }

    private static void LaunchEditor(string filePath)
    {
        var editor = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "notepad" : "nano";
        try
        {
            Helpers.ProcessRunner.Run(editor, $"\"{filePath}\"");
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Editor launch failed: {ex.Message}");
        }
    }
}
