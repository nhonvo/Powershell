using System.Text;
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
using AgyTui.Helpers;

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
            var selectedLine = entries[idx];
            var spaceIdx = selectedLine.IndexOf(' ');
            var selected = spaceIdx >= 0 ? selectedLine[(spaceIdx + 1)..].Trim() : selectedLine;
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
        if (ext == ".cs")
        {
            // Simple syntax highlight heuristics
            var keywords = new[] { "public", "private", "protected", "internal", "class", "void", "string", "int", "var", "return", "if", "else", "foreach", "using", "namespace", "static", "new" };
            foreach (var kw in keywords)
            {
                escaped = Regex.Replace(escaped, $@"\b{kw}\b", $"[blue]{kw}[/]");
            }
            escaped = Regex.Replace(escaped, @"//.*$", m => $"[green]{m.Value}[/]");
        }
        else if (ext == ".ps1")
        {
            var keywords = new[] { "function", "param", "if", "else", "foreach", "return", "process" };
            foreach (var kw in keywords)
            {
                escaped = Regex.Replace(escaped, $@"\b{kw}\b", $"[blue]{kw}[/]");
            }
            escaped = Regex.Replace(escaped, @"#.*$", m => $"[green]{m.Value}[/]");
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

    public static void BrowseWorkspaceSymbols(string dirPath)
    {
        var query = AnsiConsole.Ask<string>("[cyan]Filter workspace symbols/files (or Enter for all):[/]", "").Trim();
        var files = Directory.GetFiles(dirPath, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".cs") || f.EndsWith(".ps1") || f.EndsWith(".ts") || f.EndsWith(".js") || f.EndsWith(".py"))
            .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("\\.git\\"))
            .ToArray();
        if (files.Length == 0)
        {
            SpectrePanel.Info("No code files found in workspace.");
            return;
        }

        var scored = files.Select(f => {
            var relPath = Path.GetRelativePath(dirPath, f);
            var score = string.IsNullOrEmpty(query) ? 100 : TerminalIde.FuzzyScore(query, relPath);
            return (File: f, RelPath: relPath, Score: score);
        })
        .Where(x => x.Score >= 0)
        .OrderByDescending(x => x.Score)
        .Take(50)
        .ToArray();

        if (scored.Length == 0)
        {
            SpectrePanel.Info($"No files matching '{query}'.");
            return;
        }

        var selectedIdx = SpectreMenu.Show("Select File to Browse Symbols", scored.Select(x => x.RelPath).ToArray(), 0, true);
        if (selectedIdx >= 0)
        {
            BrowseSymbols(scored[selectedIdx].File);
        }
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
        UpdateAgyContext(root);
        ShowIdeLayout(root);
    }

    public static void ShowIdeLayout(string rootPath, string? openFilePath = null)
    {
        var currentFile = openFilePath;
        var files = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj") && !f.Contains(".git"))
            .Select(f => Path.GetRelativePath(rootPath, f))
            .ToList();

        if (currentFile == null && files.Count > 0)
        {
            currentFile = Path.Combine(rootPath, files[0]);
        }

        while (true)
        {
            var activeTab = currentFile != null ? Path.GetFileName(currentFile) : "No file open";

            var layout = new Layout("Root")
                .SplitRows(
                    new Layout("Header").Size(3),
                    new Layout("Main").SplitColumns(
                        new Layout("Sidebar").Size(30),
                        new Layout("Editor")
                    ),
                    new Layout("Status").Size(3)
                );

            var sidebarLines = new List<string>();
            foreach (var f in files.Take(15))
            {
                var icon = AgyTui.Icons.GetFileIcon(Path.GetExtension(f));
                var prefix = currentFile != null && f == Path.GetRelativePath(rootPath, currentFile) ? "[green]▶ [/]" : "  ";
                sidebarLines.Add($"{prefix}{icon} [cyan]{f}[/]");
            }
            if (files.Count > 15) sidebarLines.Add("  ...");

            var sidebarPanel = new Panel(string.Join("\n", sidebarLines))
            {
                Header = new PanelHeader("[bold yellow]Explorer[/]"),
                Border = BoxBorder.Rounded
            };
            layout["Sidebar"].Update(sidebarPanel);

            var breadcrumbs = currentFile != null
                ? $"[bold white]📁 {Path.GetFileName(rootPath)}[/] › [green]{Path.GetRelativePath(rootPath, currentFile).Replace(Path.DirectorySeparatorChar, '›')}[/]"
                : $"[bold white]📁 {Path.GetFileName(rootPath)}[/]";
            var headerPanel = new Panel(new Align(new Markup(breadcrumbs), HorizontalAlignment.Left, VerticalAlignment.Middle))
            {
                Border = BoxBorder.None
            };
            layout["Header"].Update(headerPanel);

            string editorText = "";
            if (currentFile != null && File.Exists(currentFile))
            {
                var fileLines = File.ReadAllLines(currentFile).Take(40).ToList();
                editorText = string.Join("\n", fileLines.Select((l, i) => $"[dim]{i + 1:D3} |[/] {l.EscapeMarkup()}"));
                if (File.ReadAllLines(currentFile).Length > 40) editorText += "\n[dim]... (truncated) ...[/]";
            }
            else
            {
                editorText = "[dim]No file loaded. Select a file from the sidebar to inspect.[/]";
            }
            var editorPanel = new Panel(editorText)
            {
                Header = new PanelHeader($"[bold green] {activeTab} [/]"),
                Border = BoxBorder.Rounded
            };
            layout["Editor"].Update(editorPanel);

            var branch = Helpers.ProcessRunner.RunCapture("git", "branch --show-current").Trim();
            if (string.IsNullOrEmpty(branch)) branch = "main";
            var statusText = $"[green]⚙ {activeTab.EscapeMarkup()}[/] | Git: [yellow]{branch.EscapeMarkup()}[/] | [dim][[Ctrl+B]] Sidebar | [[Ctrl+P]] Quick Open | [[Ctrl+K]] Ask AI[/]";
            var statusPanel = new Panel(new Align(new Markup(statusText), HorizontalAlignment.Left, VerticalAlignment.Middle))
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Grey)
            };
            layout["Status"].Update(statusPanel);

            ScreenChrome.RenderFrame(() =>
            {
                AnsiConsole.Write(layout);
            });

            AnsiConsole.WriteLine();
            var key = Console.ReadKey(intercept: true);
            if ((key.Key == ConsoleKey.R && key.Modifiers.HasFlag(ConsoleModifiers.Control)) || key.KeyChar == 'e')
            {
                if (currentFile != null)
                {
                    ProcessRunner.Run(EditorResolver.Resolve(), $"\"{currentFile}\"");
                }
                else
                {
                    SpectrePanel.Warning("No active file open to edit.");
                    Thread.Sleep(1000);
                }
            }
            else if ((key.Key == ConsoleKey.P && key.Modifiers.HasFlag(ConsoleModifiers.Control)) || key.KeyChar == 'p')
            {
                var query = AnsiConsole.Ask<string>("[cyan]Quick Open (Fuzzy Search):[/]").Trim();
                var matches = files
                    .Select(f => new { File = f, Score = FuzzyScore(query, f) })
                    .Where(x => x.Score >= 0)
                    .OrderByDescending(x => x.Score)
                    .Select(x => x.File)
                    .ToArray();
                if (matches.Length > 0)
                {
                    var fileIdx = SpectreMenu.Show("Matches", matches, 0, true);
                    if (fileIdx >= 0)
                    {
                        currentFile = Path.Combine(rootPath, matches[fileIdx]);
                        UpdateAgyContext(rootPath, currentFile);
                    }
                }
                else
                {
                    SpectrePanel.Warning("No matching files.");
                    Thread.Sleep(1000);
                }
            }
            else if ((key.Key == ConsoleKey.K && key.Modifiers.HasFlag(ConsoleModifiers.Control)) || key.KeyChar == 'k')
            {
                if (currentFile != null && File.Exists(currentFile))
                {
                    AnsiConsole.MarkupLine("[cyan]Sending file content to AI for review/explanation...[/]");
                    string content = File.ReadAllText(currentFile);
                    if (content.Length > 8000) content = content[..8000] + "\n...(truncated)";
                    AgyAiCore.AskAi($"Regarding the file '{currentFile}', explain this file:\n\nFile Content:\n{content}");
                }
                else
                {
                    SpectrePanel.Warning("Please open a file first.");
                    Thread.Sleep(1000);
                }
            }
            else if ((key.Key == ConsoleKey.B && key.Modifiers.HasFlag(ConsoleModifiers.Control)) || key.KeyChar == 'b')
            {
                var fileIdx = SpectreMenu.Show("Select File to Open", files.ToArray(), 0, true);
                if (fileIdx >= 0)
                {
                    currentFile = Path.Combine(rootPath, files[fileIdx]);
                    UpdateAgyContext(rootPath, currentFile);
                }
            }
            else if (key.KeyChar == '/')
            {
                AnsiConsole.Markup("[yellow]/[/]");
                var commandLine = Console.ReadLine()?.Trim();
                var skills = SkillLoader.Discover(rootPath).ToList();
                if (string.IsNullOrEmpty(commandLine))
                {
                    var list = new List<string>();
                    foreach (var c in IdeCommandRegistry.All)
                    {
                        list.Add($"/{c.Name,-10} {c.ArgHint,-12} [dim]{c.Description}[/]");
                    }
                    foreach (var s in skills)
                    {
                        list.Add($"🧩 /{s.Trigger,-8} {s.Name,-12} [dim]{s.Description}[/]");
                    }
                    list.Add("← Back to Editor");
                    var menuIdx = SpectreMenu.Show("IDE Slash Commands", list.ToArray(), 0, true);
                    if (menuIdx >= 0 && menuIdx < list.Count - 1)
                    {
                        if (menuIdx < IdeCommandRegistry.All.Length)
                        {
                            var c = IdeCommandRegistry.All[menuIdx];
                            var context = new IdeContext(rootPath, currentFile);
                            c.Run(context, []);
                            currentFile = context.CurrentFile;
                        }
                        else
                        {
                            var s = skills[menuIdx - IdeCommandRegistry.All.Length];
                            AnsiConsole.MarkupLine($"[cyan]Running Skill: {s.Name}...[/]");
                            var context = new IdeContext(rootPath, currentFile);
                            foreach (var step in s.Steps)
                            {
                                var primitiveCommand = IdeCommandRegistry.All.FirstOrDefault(c => c.Name.Equals(step.Primitive, StringComparison.OrdinalIgnoreCase));
                                if (primitiveCommand != null)
                                {
                                    primitiveCommand.Run(context, string.IsNullOrEmpty(step.Arg) ? [] : [step.Arg]);
                                }
                            }
                            currentFile = context.CurrentFile;
                            AnsiConsole.MarkupLine("[green]Skill complete. Press any key...[/]");
                            Console.ReadKey(true);
                        }
                    }
                }
                else
                {
                    var parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        var cmdName = parts[0].ToLowerInvariant();
                        var cmdArgs = parts.Skip(1).ToArray();
                        var command = IdeCommandRegistry.All.FirstOrDefault(c => c.Name.Equals(cmdName, StringComparison.OrdinalIgnoreCase));
                        if (command != null)
                        {
                            var context = new IdeContext(rootPath, currentFile);
                            command.Run(context, cmdArgs);
                            currentFile = context.CurrentFile;
                        }
                        else
                        {
                            var skill = skills.FirstOrDefault(s => s.Trigger.Equals(cmdName, StringComparison.OrdinalIgnoreCase) || s.Name.Equals(cmdName, StringComparison.OrdinalIgnoreCase));
                            if (skill != null)
                            {
                                AnsiConsole.MarkupLine($"[cyan]Running Skill: {skill.Name}...[/]");
                                var context = new IdeContext(rootPath, currentFile);
                                foreach (var step in skill.Steps)
                                {
                                    var primitiveCommand = IdeCommandRegistry.All.FirstOrDefault(c => c.Name.Equals(step.Primitive, StringComparison.OrdinalIgnoreCase));
                                    if (primitiveCommand != null)
                                    {
                                        primitiveCommand.Run(context, string.IsNullOrEmpty(step.Arg) ? [] : [step.Arg]);
                                    }
                                }
                                currentFile = context.CurrentFile;
                            }
                            else
                            {
                                SpectrePanel.Warning($"Unknown command or skill trigger: {cmdName}");
                                Thread.Sleep(1000);
                            }
                        }
                    }
                }
            }
            else if (key.Key == ConsoleKey.Escape || key.KeyChar == 'q')
            {
                break;
            }
        }
    }

    public static void UpdateAgyContext(string rootPath, string? touchedFile = null)
    {
        try
        {
            var contextFile = Path.Combine(rootPath, ".agy-context.md");
            var touchedList = new List<string>();
            if (File.Exists(contextFile))
            {
                var lines = File.ReadAllLines(contextFile);
                var isTouchedSection = false;
                foreach (var line in lines)
                {
                    if (line.StartsWith("## Recently Touched Files"))
                    {
                        isTouchedSection = true;
                        continue;
                    }
                    if (line.StartsWith("##"))
                    {
                        isTouchedSection = false;
                    }
                    if (isTouchedSection && line.StartsWith("- "))
                    {
                        touchedList.Add(line[2..].Trim());
                    }
                }
            }

            if (!string.IsNullOrEmpty(touchedFile))
            {
                var relPath = Path.GetRelativePath(rootPath, touchedFile);
                touchedList.Remove(relPath);
                touchedList.Insert(0, relPath);
            }

            var todoList = new List<string>();
            foreach (var file in Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj") && !f.Contains(".git"))
                .Take(200))
            {
                try
                {
                    var fileLines = File.ReadAllLines(file);
                    for (int i = 0; i < fileLines.Length; i++)
                    {
                        var match = Regex.Match(fileLines[i], @"\bTODO\b:(.*)", RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            todoList.Add($"- {Path.GetRelativePath(rootPath, file)}:L{i + 1}:{match.Groups[1].Value.Trim()}");
                        }
                    }
                }
                catch { }
            }

            var sb = new StringBuilder();
            sb.AppendLine("# Workspace Context Handoff (.agy-context.md)");
            sb.AppendLine();
            sb.AppendLine("## Recently Touched Files");
            foreach (var f in touchedList.Take(5))
            {
                sb.AppendLine($"- {f}");
            }
            sb.AppendLine();
            sb.AppendLine("## Active TODOs");
            foreach (var todo in todoList.Take(10))
            {
                sb.AppendLine(todo);
            }

            File.WriteAllText(contextFile, sb.ToString(), Encoding.UTF8);
        }
        catch { }
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
                "View file", "Symbol search", "View diff (this file)", $"Edit ({EditorResolver.Resolve()})", "← Back"
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
        try
        {
            Helpers.ProcessRunner.Run(EditorResolver.Resolve(), $"\"{filePath}\"");
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Editor launch failed: {ex.Message}");
        }
    }

    public static int FuzzyScore(string query, string target)
    {
        if (string.IsNullOrEmpty(query)) return 0;
        if (string.IsNullOrEmpty(target)) return -1;
        
        int queryIdx = 0;
        int score = 0;
        int lastMatchIdx = -2;
        int firstMatchIdx = -1;
        
        for (int i = 0; i < target.Length; i++)
        {
            if (char.ToLowerInvariant(target[i]) == char.ToLowerInvariant(query[queryIdx]))
            {
                if (firstMatchIdx == -1) firstMatchIdx = i;
                
                if (i == lastMatchIdx + 1)
                {
                    score += 5;
                }
                
                lastMatchIdx = i;
                queryIdx++;
                
                if (queryIdx == query.Length)
                {
                    score += Math.Max(0, 100 - firstMatchIdx);
                    return score;
                }
            }
        }
        
        return -1;
    }
}
