using System.Buffers;
using System.Text.RegularExpressions;
using AgyTui.Infrastructure.Integrations.Ai.Services;
using AgyTui.Infrastructure.Di;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.UI.Screens.Ide;

public static class TerminalIde
{
    private struct ExplorerNode
    {
        public string Path;
        public string Name;
        public bool IsDirectory;
        public int Level;
    }

    public static void Open(string? path = null)
    {
        var root = path ?? Directory.GetCurrentDirectory();
        UpdateAgyContext(root);
        ShowIdeLayout(root);
    }

    private static List<ExplorerNode> GetVisibleNodes(string rootPath, string dir, int level, HashSet<string> expandedFolders)
    {
        var nodes = new List<ExplorerNode>();
        try
        {
            var dirs = Directory.GetDirectories(dir)
                .Where(d =>
                {
                    var name = Path.GetFileName(d);
                    return !name.Equals("bin") && !name.Equals("obj") && !name.Equals(".git") && !name.StartsWith(".");
                })
                .OrderBy(d => d)
                .ToList();

            foreach (var d in dirs)
            {
                nodes.Add(new ExplorerNode
                {
                    Path = d,
                    Name = Path.GetFileName(d),
                    IsDirectory = true,
                    Level = level
                });

                if (expandedFolders.Contains(d))
                {
                    nodes.AddRange(GetVisibleNodes(rootPath, d, level + 1, expandedFolders));
                }
            }

            var files = Directory.GetFiles(dir)
                .Where(f =>
                {
                    var name = Path.GetFileName(f);
                    return !name.Equals("bin") && !name.Equals("obj") && !name.Equals(".git") && !name.StartsWith(".");
                })
                .OrderBy(f => f)
                .ToList();

            foreach (var f in files)
            {
                nodes.Add(new ExplorerNode
                {
                    Path = f,
                    Name = Path.GetFileName(f),
                    IsDirectory = false,
                    Level = level
                });
            }
        }
        catch { }
        return nodes;
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
        var sidebarFocused = true;
        var sidebarSel = 0;
        var editorScrollOffset = 0;
        var expandedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            var visibleNodes = GetVisibleNodes(rootPath, rootPath, 0, expandedFolders);

            if (sidebarSel < 0) sidebarSel = 0;
            if (visibleNodes.Count > 0 && sidebarSel >= visibleNodes.Count) sidebarSel = visibleNodes.Count - 1;

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
                int termH = 30;
                try { termH = Console.WindowHeight; } catch { }
                int maxSidebarRows = Math.Max(5, termH - 9);
                var (sTop, sEnd) = ScrollableListView.ComputeViewport(visibleNodes.Count, sidebarSel, maxSidebarRows);

                for (int i = sTop; i < sEnd; i++)
                {
                    var node = visibleNodes[i];
                    var isSelected = (i == sidebarSel);

                    var indent = new string(' ', node.Level * 2);
                    var icon = node.IsDirectory
                        ? (expandedFolders.Contains(node.Path) ? "📂" : "📁")
                        : Icons.GetFileIcon(Path.GetExtension(node.Name));

                    var isCurrent = !node.IsDirectory && currentFile != null && string.Equals(node.Path, currentFile, StringComparison.OrdinalIgnoreCase);
                    var prefix = isCurrent ? "[green]▶ [/]" : "  ";
                    var line = $"{prefix}{indent}{icon} {node.Name}";

                    if (isSelected)
                    {
                        if (sidebarFocused)
                        {
                            line = $"[bold black on yellow]❯ {indent}{icon} {node.Name}[/]";
                        }
                        else
                        {
                            line = $"[bold yellow]❯ {indent}{icon} {node.Name}[/]";
                        }
                    }
                    else if (isCurrent)
                    {
                        line = $"[bold green]  {indent}{icon} {node.Name}[/]";
                    }
                    else
                    {
                        line = node.IsDirectory ? $"  {indent}[bold cyan]{icon} {node.Name}[/]" : $"  {indent}[cyan]{icon} {node.Name}[/]";
                    }
                    sidebarLines.Add(line);
                }

                if (visibleNodes.Count > maxSidebarRows && sEnd < visibleNodes.Count)
                {
                    sidebarLines.Add($"  [dim]... {visibleNodes.Count - sEnd} more files[/]");
                }

                var sidebarTitle = sidebarFocused ? "[bold yellow]EXPLORER (Focused)[/]" : "[dim]EXPLORER[/]";
                var sidebarPanel = new Panel(string.Join("\n", sidebarLines))
                {
                    Header = new PanelHeader(sidebarTitle),
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(sidebarFocused ? Color.Yellow : Color.Grey)
                };
                layout["Sidebar"].Update(sidebarPanel);
            }

            // Header Bar Rendering
            var rootName = Path.GetFileName(rootPath);
            var fileRel = currentFile != null ? Path.GetRelativePath(rootPath, currentFile) : "No file open";
            var fileExt = currentFile != null ? Path.GetExtension(currentFile) : "";
            var fileIcon = Icons.GetFileIcon(fileExt);

            var gitBranch = ProcessRunner.Instance.RunCapture("git", "branch --show-current").Trim();
            if (string.IsNullOrEmpty(gitBranch)) gitBranch = "main";

            var fileInfoStr = "";
            if (currentFile != null && File.Exists(currentFile))
            {
                try
                {
                    var fi = new FileInfo(currentFile);
                    var sizeKb = fi.Length / 1024.0;
                    var lineCount = File.ReadLines(currentFile).Count();
                    fileInfoStr = $"[dim]({lineCount} lines · {sizeKb:F1} KB)[/]";
                }
                catch { }
            }

            var headerMarkup = $" [bold cyan]IDE[/] [dim]│[/] [bold white]📂 {rootName.EscapeMarkup()}[/] › [bold green]{fileIcon} {fileRel.EscapeMarkup()}[/] {fileInfoStr} [dim]│[/] 🌿 [yellow]{gitBranch.EscapeMarkup()}[/]";
            var headerPanel = new Panel(new Align(new Markup(headerMarkup), HorizontalAlignment.Left, VerticalAlignment.Middle))
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Cyan1)
            };
            layout["Header"].Update(headerPanel);

            string editorText = "";
            var editorTitle = "";
            if (currentFile != null && File.Exists(currentFile))
            {
                var allLines = File.ReadAllLines(currentFile);
                int termH = 30;
                try { termH = Console.WindowHeight; } catch { }
                int maxEditorRows = Math.Max(5, termH - 9);

                int maxScroll = Math.Max(0, allLines.Length - maxEditorRows);
                if (editorScrollOffset < 0) editorScrollOffset = 0;
                if (editorScrollOffset > maxScroll) editorScrollOffset = maxScroll;

                var displayLines = allLines.Skip(editorScrollOffset).Take(maxEditorRows).ToList();
                var sb = new StringBuilder();
                for (int i = 0; i < displayLines.Count; i++)
                {
                    int lineNum = editorScrollOffset + i + 1;
                    sb.AppendLine($"[dim]{lineNum:D3} │[/] {displayLines[i].EscapeMarkup()}");
                }

                if (allLines.Length > editorScrollOffset + maxEditorRows)
                {
                    sb.AppendLine($"[dim]... (truncated, showing lines {editorScrollOffset + 1}-{editorScrollOffset + displayLines.Count} of {allLines.Length}) ...[/]");
                }
                editorText = sb.ToString();
                editorTitle = $" [bold green] {activeTab} [/] ({allLines.Length} lines) ";
            }
            else
            {
                editorText = "[dim]No file loaded. Select a file from the sidebar to inspect.[/]";
                editorTitle = " [bold green] Editor [/] ";
            }

            var editorPanel = new Panel(editorText)
            {
                Header = new PanelHeader(editorTitle),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(!sidebarFocused ? Color.Yellow : Color.Grey)
            };
            layout["Editor"].Update(editorPanel);

            var modeTag = sidebarFocused ? "[bold black on yellow] EXPLORER [/]" : "[bold black on green] EDITOR [/]";
            var statusText = $"{modeTag} | [cyan]{fileIcon} {activeTab.EscapeMarkup()}[/] | 🌿 [yellow]{gitBranch.EscapeMarkup()}[/] | [dim][[Tab]] Focus | [[/]] Search | [[e]] Edit | [[g]] Git | [[k]] AI | [[b]] Sidebar | [[q]] Exit[/]";
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

            // Tab navigation
            if (key.Key == ConsoleKey.Tab)
            {
                if (showSidebar)
                {
                    sidebarFocused = !sidebarFocused;
                }
                else
                {
                    sidebarFocused = false;
                }
            }
            // Toggle sidebar visibility
            else if ((key.Key == ConsoleKey.B && key.Modifiers.HasFlag(ConsoleModifiers.Control)) || key.KeyChar == 'b')
            {
                showSidebar = !showSidebar;
                if (!showSidebar) sidebarFocused = false;
            }
            // Open file search
            else if ((key.Key == ConsoleKey.P && key.Modifiers.HasFlag(ConsoleModifiers.Control)) || key.KeyChar == 'p' || key.KeyChar == '/')
            {
                OpenFileSearch(rootPath, files, ref currentFile);
                if (currentFile != null)
                {
                    editorScrollOffset = 0;
                    sidebarFocused = false;
                }
            }
            // Ask AI
            else if ((key.Key == ConsoleKey.K && key.Modifiers.HasFlag(ConsoleModifiers.Control)) || key.KeyChar == 'k')
            {
                if (currentFile != null && File.Exists(currentFile))
                {
                    AnsiConsole.MarkupLine("[cyan]Sending file content to AI for review/explanation...[/]");
                    string content = File.ReadAllText(currentFile);
                    if (content.Length > 8000) content = content[..8000] + "\n...(truncated)";
                    AiDashboardView.AskAi($"Regarding the file '{currentFile}', explain this file:\n\nFile Content:\n{content}");
                }
                else
                {
                    SpectrePanel.Warning("Please open a file first.");
                    Thread.Sleep(1000);
                }
            }
            // In-IDE Git Actions Menu
            else if ((key.Key == ConsoleKey.G && key.Modifiers.HasFlag(ConsoleModifiers.Control)) || key.KeyChar == 'g')
            {
                ShowInIdeGitMenu(rootPath, currentFile);
            }
            // Edit file
            else if ((key.Key == ConsoleKey.R && key.Modifiers.HasFlag(ConsoleModifiers.Control)) || key.KeyChar == 'e')
            {
                if (currentFile != null)
                {
                    ProcessRunner.Instance.Run(Bootstrapper.ServiceProvider.GetRequiredService<IEditorResolver>().Resolve(), $"\"{currentFile}\"");
                }
                else
                {
                    SpectrePanel.Warning("No active file open to edit.");
                    Thread.Sleep(1000);
                }
            }
            // Up Arrow (Navigate sidebar or scroll editor)
            else if (key.Key == ConsoleKey.UpArrow || (key.Key == ConsoleKey.K && key.Modifiers == 0))
            {
                if (sidebarFocused)
                {
                    if (visibleNodes.Count > 0)
                    {
                        sidebarSel = (sidebarSel - 1 + visibleNodes.Count) % visibleNodes.Count;
                    }
                }
                else
                {
                    editorScrollOffset = Math.Max(0, editorScrollOffset - 1);
                }
            }
            // Down Arrow (Navigate sidebar or scroll editor)
            else if (key.Key == ConsoleKey.DownArrow || (key.Key == ConsoleKey.J && key.Modifiers == 0))
            {
                if (sidebarFocused)
                {
                    if (visibleNodes.Count > 0)
                    {
                        sidebarSel = (sidebarSel + 1) % visibleNodes.Count;
                    }
                }
                else
                {
                    editorScrollOffset++;
                }
            }
            // PageUp
            else if (key.Key == ConsoleKey.PageUp)
            {
                if (sidebarFocused)
                {
                    sidebarSel = Math.Max(0, sidebarSel - 10);
                }
                else
                {
                    editorScrollOffset = Math.Max(0, editorScrollOffset - 20);
                }
            }
            // PageDown
            else if (key.Key == ConsoleKey.PageDown)
            {
                if (sidebarFocused)
                {
                    sidebarSel = Math.Min(visibleNodes.Count - 1, sidebarSel + 10);
                }
                else
                {
                    editorScrollOffset += 20;
                }
            }
            // Enter
            else if (key.Key == ConsoleKey.Enter)
            {
                if (sidebarFocused)
                {
                    if (sidebarSel >= 0 && sidebarSel < visibleNodes.Count)
                    {
                        var node = visibleNodes[sidebarSel];
                        if (node.IsDirectory)
                        {
                            if (expandedFolders.Contains(node.Path))
                                expandedFolders.Remove(node.Path);
                            else
                                expandedFolders.Add(node.Path);
                        }
                        else
                        {
                            currentFile = node.Path;
                            UpdateAgyContext(rootPath, currentFile);
                            editorScrollOffset = 0;
                            sidebarFocused = false;
                        }
                    }
                }
            }
            // Left/Right Arrow focus switching or collapse/expand
            else if (key.Key == ConsoleKey.LeftArrow || (key.Key == ConsoleKey.H && key.Modifiers == 0))
            {
                if (sidebarFocused && sidebarSel >= 0 && sidebarSel < visibleNodes.Count && visibleNodes[sidebarSel].IsDirectory)
                {
                    expandedFolders.Remove(visibleNodes[sidebarSel].Path);
                }
                else if (showSidebar)
                {
                    sidebarFocused = true;
                }
            }
            else if (key.Key == ConsoleKey.RightArrow || (key.Key == ConsoleKey.L && key.Modifiers == 0))
            {
                if (sidebarFocused && sidebarSel >= 0 && sidebarSel < visibleNodes.Count && visibleNodes[sidebarSel].IsDirectory)
                {
                    expandedFolders.Add(visibleNodes[sidebarSel].Path);
                }
                else
                {
                    sidebarFocused = false;
                }
            }
            // Escape / Quit
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

    private static void ShowInIdeGitMenu(string rootPath, string? currentFile)
    {
        var gitClient = Bootstrapper.ServiceProvider.GetRequiredService<Infrastructure.Integrations.Git.IGitClient>();
        var actions = new[]
        {
            "🌿 Git Status & Diff",
            "🌿 Git Branch Manager (/gbr)",
            "💬 Conventional Commit Wizard (/gcmt)",
            "🔀 Conflict Resolution Helper (/gconflict)",
            "📦 Git Stash Manager (/gstash)",
            "🔄 Git Rebase Wizard (/grebase)",
            "↩ Back to Editor"
        };

        var choice = SpectreMenu.Show("In-IDE Git Actions", actions, 0);
        switch (choice)
        {
            case 0:
                if (currentFile != null && File.Exists(currentFile))
                    GitDiffViewer.ShowDiff(rootPath, currentFile);
                else
                    gitClient.ShowStatus();
                break;
            case 1:
                gitClient.ShowBranches();
                break;
            case 2:
                gitClient.ConventionalCommitWizard();
                break;
            case 3:
                gitClient.ShowConflictResolver();
                break;
            case 4:
                gitClient.ShowStashManager();
                break;
            case 5:
                gitClient.ShowRebaseWizard();
                break;
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
                "View file", "Symbol search", "View diff (this file)", $"Edit ({Bootstrapper.ServiceProvider.GetRequiredService<IEditorResolver>().Resolve()})", "← Back"
            };
            var sel = SpectreMenu.Show($"File: {Path.GetFileName(filePath)}", actions, 0, false);
            switch (sel)
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
            AnsiConsole.MarkupLine("[yellow]No matches found.[/]");
            Thread.Sleep(1000);
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
            AnsiConsole.MarkupLine("[yellow]No matches found.[/]");
            Thread.Sleep(1000);
            return;
        }
        SpectrePager.Show($"Search results: {pattern}", [.. results]);
    }

    private static void LaunchEditor(string filePath)
    {
        try
        {
            ProcessRunner.Instance.Run(Bootstrapper.ServiceProvider.GetRequiredService<IEditorResolver>().Resolve(), $"\"{filePath}\"");
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
                matches = files.Where(f => SystemHelper.Instance.IsFuzzyMatch(f, searchBuffer)).ToList();
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
                        var boldF = string.IsNullOrEmpty(searchBuffer) ? f.EscapeMarkup() : SystemHelper.Instance.BoldFuzzyMatch(f, searchBuffer);
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
