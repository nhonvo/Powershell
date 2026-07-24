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

        var showSidebar = true;

        while (true)
        {
            var activeTab = currentFile != null ? Path.GetFileName(currentFile) : "No file open";

            var mainLayout = new Layout("Main");
            if (showSidebar)
            {
                mainLayout.SplitColumns(
                    new Layout("Sidebar").Size(30),
                    new Layout("Editor")
                );
            }
            else
            {
                mainLayout.SplitColumns(
                    new Layout("Editor")
                );
            }

            var layout = new Layout("Root")
                .SplitRows(
                    new Layout("Header").Size(3),
                    mainLayout,
                    new Layout("Status").Size(3)
                );

            if (showSidebar)
            {
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
            }

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
            else if ((key.Key == ConsoleKey.P && key.Modifiers.HasFlag(ConsoleModifiers.Control)) || key.KeyChar == 'p' || key.KeyChar == '/')
            {
                OpenFileSearch(rootPath, files, ref currentFile);
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
            else if ((key.Key == ConsoleKey.B && key.Modifiers.HasFlag(ConsoleModifiers.Control)) || key.Key == ConsoleKey.B)
            {
                showSidebar = !showSidebar;
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

    private static string DeletePreviousWord(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var trimmed = text.TrimEnd();
        if (string.IsNullOrEmpty(trimmed)) return "";
        int lastSpace = trimmed.LastIndexOf(' ');
        if (lastSpace < 0) return "";
        return trimmed[..lastSpace].TrimEnd();
    }

    private static void OpenFileSearch(string rootPath, List<string> files, ref string? currentFile)
    {
        var searchBuffer = "";
        var selIdx = 0;
        try { Console.CursorVisible = false; } catch { }

        while (true)
        {
            var matches = files;
            if (!string.IsNullOrEmpty(searchBuffer))
            {
                matches = files.Where(f => SystemHelper.IsFuzzyMatch(f, searchBuffer)).ToList();
            }

            if (selIdx < 0) selIdx = 0;
            if (matches.Count > 0 && selIdx >= matches.Count) selIdx = matches.Count - 1;

            ScreenChrome.RenderFrame(() =>
            {
                var grid = new Grid();
                grid.AddColumn(new GridColumn().NoWrap());
                grid.AddRow(new Markup("[bold green]📁 File Search / Quick Open[/]\n"));

                if (!string.IsNullOrEmpty(searchBuffer))
                {
                    grid.AddRow(new Markup($"[yellow]Search:[/] [white]{searchBuffer.EscapeMarkup()}[/]_\n"));
                }
                else
                {
                    grid.AddRow(new Markup("[dim]Type to filter files (Esc to cancel, Enter to open)[/]\n"));
                }

                if (matches.Count == 0)
                {
                    grid.AddRow(new Markup($"  [dim]No files matching '{searchBuffer.EscapeMarkup()}'.[/]"));
                }
                else
                {
                    int winH = 30;
                    try { winH = Console.WindowHeight; } catch { }
                    int maxRows = Math.Max(5, winH - 10);
                    var (topRow, endRow) = ScrollableListView.ComputeViewport(matches.Count, selIdx, maxRows);

                    for (int i = topRow; i < endRow; i++)
                    {
                        var isSelected = (i == selIdx);
                        var prefix = isSelected ? "[green bold]❯ [/]" : "  ";
                        var f = matches[i];
                        var icon = Icons.GetFileIcon(Path.GetExtension(f));
                        var boldF = string.IsNullOrEmpty(searchBuffer) ? f.EscapeMarkup() : SystemHelper.BoldFuzzyMatch(f, searchBuffer);
                        grid.AddRow(new Markup($"{prefix}{icon} {boldF}"));
                    }
                }

                AnsiConsole.Write(new Panel(grid) { Border = BoxBorder.Rounded });
            });

            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Backspace || (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key == ConsoleKey.W))
            {
                bool isCtrlWordDelete = (key.Modifiers.HasFlag(ConsoleModifiers.Control)) ||
                                        key.KeyChar == '\x17' || key.KeyChar == '\x7f' || key.KeyChar == '\x08';
                if (isCtrlWordDelete && key.Key == ConsoleKey.Backspace)
                {
                    searchBuffer = DeletePreviousWord(searchBuffer);
                }
                else if (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key == ConsoleKey.W)
                {
                    searchBuffer = DeletePreviousWord(searchBuffer);
                }
                else if (searchBuffer.Length > 0)
                {
                    searchBuffer = searchBuffer[..^1];
                }
                selIdx = 0;
            }
            else if (key.Key == ConsoleKey.Escape)
            {
                return;
            }
            else if (key.Key == ConsoleKey.Enter)
            {
                if (matches.Count > 0 && selIdx >= 0 && selIdx < matches.Count)
                {
                    currentFile = Path.Combine(rootPath, matches[selIdx]);
                    UpdateAgyContext(rootPath, currentFile);
                    return;
                }
            }
            else if (key.Key == ConsoleKey.UpArrow || (key.Key == ConsoleKey.K && key.Modifiers == 0))
            {
                if (matches.Count > 0)
                {
                    selIdx = (selIdx - 1 + matches.Count) % matches.Count;
                }
            }
            else if (key.Key == ConsoleKey.DownArrow || (key.Key == ConsoleKey.J && key.Modifiers == 0))
            {
                if (matches.Count > 0)
                {
                    selIdx = (selIdx + 1) % matches.Count;
                }
            }
            else if (key.KeyChar >= 32 && key.KeyChar <= 126 && key.KeyChar != '/')
            {
                searchBuffer += key.KeyChar;
                selIdx = 0;
            }
        }
    }
}
