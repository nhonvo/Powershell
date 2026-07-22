using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Spectre.Console;
using Spectre.Console.Rendering;
using AgyTui.Components;
using AgyTui.Registry;

namespace AgyTui;

public enum VisibleRowType
{
    Category,
    Group,
    Command,
    Widget,
    Separator,
    Exit
}

public sealed record VisibleRow(
    MenuNode Node,
    VisibleRowType Type,
    int Indent
);

public sealed class FlatTreeRenderer : MenuRendererBase
{
    private readonly HashSet<string> _expandedCategories = new();
    private readonly HashSet<string> _expandedGroups = new();
    private readonly HashSet<string> _expandedWidgets = new();
    private string _detailsSearchBuffer = "";

    public override void Run(MenuNode root)
    {
        var selectionIndex = 0;
        var detailsActive = false;
        var detailsMode = "";
        var detailsSel = 0;
        var searching = false;
        var searchBuffer = "";
        var lastDetailsActive = false;
        var lastVisibleRowsCount = 0;

        while (true)
        {
            var categories = GetActiveChildren(root);
            var visibleRows = new List<VisibleRow>();

            if (!string.IsNullOrEmpty(searchBuffer))
            {
                // Filter every category, group, and command node (trim leading slash for search flexibility)
                var rawQ = searchBuffer.TrimStart('/').Trim();
                var matchAll = string.IsNullOrEmpty(rawQ);

                bool IsNodeMatch(MenuNode n)
                {
                    if (matchAll) return true;
                    if (!string.IsNullOrEmpty(n.Label) && n.Label.Contains(rawQ, StringComparison.OrdinalIgnoreCase)) return true;
                    if (!string.IsNullOrEmpty(n.SearchKey) && n.SearchKey.Contains(rawQ, StringComparison.OrdinalIgnoreCase)) return true;
                    if (n.Command != null)
                    {
                        if (!string.IsNullOrEmpty(n.Command.Alias) && n.Command.Alias.Contains(rawQ, StringComparison.OrdinalIgnoreCase)) return true;
                        if (!string.IsNullOrEmpty(n.Command.DisplayName) && n.Command.DisplayName.Contains(rawQ, StringComparison.OrdinalIgnoreCase)) return true;
                        if (!string.IsNullOrEmpty(n.Command.Description) && n.Command.Description.Contains(rawQ, StringComparison.OrdinalIgnoreCase)) return true;
                    }
                    return false;
                }

                foreach (var cat in categories)
                {
                    if (cat.Kind == MenuNodeKind.Separator || cat.Kind == MenuNodeKind.Exit) continue;

                    var catMatches = new List<MenuNode>();
                    var catMatched = IsNodeMatch(cat);

                    foreach (var child in GetActiveChildren(cat))
                    {
                        if (child.Kind == MenuNodeKind.Group)
                        {
                            var groupMatches = GetActiveChildren(child)
                                .Where(sub => catMatched || IsNodeMatch(child) || IsNodeMatch(sub))
                                .ToList();
                            if (groupMatches.Count > 0)
                            {
                                catMatches.Add(child with { Children = groupMatches.ToArray() });
                            }
                        }
                        else if (child.Kind == MenuNodeKind.Command && child.Command != null)
                        {
                            if (catMatched || IsNodeMatch(child))
                            {
                                catMatches.Add(child);
                            }
                        }
                    }

                    if (catMatches.Count > 0)
                    {
                        visibleRows.Add(new VisibleRow(cat, VisibleRowType.Category, 0));
                        foreach (var match in catMatches)
                        {
                            if (match.Kind == MenuNodeKind.Group)
                            {
                                visibleRows.Add(new VisibleRow(match, VisibleRowType.Group, 1));
                                foreach (var sub in match.Children)
                                {
                                    visibleRows.Add(new VisibleRow(sub, VisibleRowType.Command, 2));
                                }
                            }
                            else
                            {
                                visibleRows.Add(new VisibleRow(match, VisibleRowType.Command, 1));
                            }
                        }
                    }
                }
            }
            else
            {
                // Normal tree display
                foreach (var cat in categories)
                {
                    if (cat.Kind == MenuNodeKind.Separator)
                    {
                        visibleRows.Add(new VisibleRow(cat, VisibleRowType.Separator, 0));
                        continue;
                    }
                    if (cat.Kind == MenuNodeKind.Exit)
                    {
                        visibleRows.Add(new VisibleRow(cat, VisibleRowType.Exit, 0));
                        continue;
                    }

                    visibleRows.Add(new VisibleRow(cat, VisibleRowType.Category, 0));
                    if (_expandedCategories.Contains(cat.Id))
                    {
                        foreach (var child in GetActiveChildren(cat))
                        {
                            if (child.Kind == MenuNodeKind.Group)
                            {
                                visibleRows.Add(new VisibleRow(child, VisibleRowType.Group, 1));
                                if (_expandedGroups.Contains(child.Id))
                                {
                                    foreach (var sub in GetActiveChildren(child))
                                    {
                                        visibleRows.Add(new VisibleRow(sub, VisibleRowType.Command, 2));
                                        if (sub.Command != null && _expandedWidgets.Contains(sub.Command.Alias))
                                        {
                                            visibleRows.Add(new VisibleRow(sub, VisibleRowType.Widget, 3));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                visibleRows.Add(new VisibleRow(child, VisibleRowType.Command, 1));
                                if (child.Command != null && _expandedWidgets.Contains(child.Command.Alias))
                                {
                                    visibleRows.Add(new VisibleRow(child, VisibleRowType.Widget, 2));
                                }
                            }
                        }
                    }
                }
            }

            if (visibleRows.Count == 0)
            {
                selectionIndex = 0;
            }
            else
            {
                if (selectionIndex >= visibleRows.Count) selectionIndex = visibleRows.Count - 1;
                if (selectionIndex < 0) selectionIndex = 0;
            }

            bool forceClear = (detailsActive != lastDetailsActive) || (visibleRows.Count < lastVisibleRowsCount);
            lastDetailsActive = detailsActive;
            lastVisibleRowsCount = visibleRows.Count;

            ScreenChrome.RenderFrame(() =>
            {
                ScreenChrome.RenderBanner(forceClear: false);
                if (detailsActive)
                {
                    RenderSubPageSelection(detailsMode, detailsSel);
                }
                else
                {
                    RenderTree(visibleRows, selectionIndex, searching, searchBuffer);
                }
            }, forceClear: forceClear);

            var key = Console.ReadKey(true);

            if (searching)
            {
                if (key.Key == ConsoleKey.Escape)
                {
                    searching = false;
                    searchBuffer = "";
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    searching = false;
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    searching = false;
                    selectionIndex = Math.Min(visibleRows.Count - 1, selectionIndex + 1);
                    continue;
                }
                else if (key.Key == ConsoleKey.UpArrow)
                {
                    searching = false;
                    selectionIndex = Math.Max(0, selectionIndex - 1);
                    continue;
                }
                else if (key.Key == ConsoleKey.PageDown)
                {
                    searching = false;
                    selectionIndex = Math.Min(visibleRows.Count - 1, selectionIndex + 10);
                    continue;
                }
                else if (key.Key == ConsoleKey.PageUp)
                {
                    searching = false;
                    selectionIndex = Math.Max(0, selectionIndex - 10);
                    continue;
                }
                else if (key.Key == ConsoleKey.Backspace || (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key == ConsoleKey.W))
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
                }
                else if (key.KeyChar >= 32 && key.KeyChar <= 126)
                {
                    if (key.KeyChar == '/' && (string.IsNullOrEmpty(searchBuffer) || searchBuffer.All(c => c == '/')))
                    {
                        searchBuffer = "/";
                    }
                    else
                    {
                        searchBuffer += key.KeyChar;
                    }
                }
                selectionIndex = 0;
                continue;
            }

            if (detailsActive)
            {
                int itemsCount = 0;
                if (detailsMode == "agyswitch")
                {
                    itemsCount = AgyAccountCore.GetAccounts().Length;
                }
                else if (detailsMode == "theme")
                {
                    var themes = GetThemeNames();
                    if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                    {
                        themes = themes.Where(t => t.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
                    }
                    itemsCount = themes.Length;
                }
                else if (detailsMode == "learn" || detailsMode == "session" || detailsMode == "weak")
                {
                    itemsCount = 6;
                }
                else if (detailsMode == "proj")
                {
                    itemsCount = WorkspaceRegistry.GetWorkspaces().Length;
                }

                if (itemsCount == 0 && detailsMode != "theme")
                {
                    detailsActive = false;
                    _detailsSearchBuffer = "";
                    continue;
                }

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.K:
                        if (itemsCount > 0) detailsSel = (detailsSel - 1 + itemsCount) % itemsCount;
                        break;
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.J:
                        if (itemsCount > 0) detailsSel = (detailsSel + 1) % itemsCount;
                        break;
                    case ConsoleKey.PageUp:
                        if (itemsCount > 0) detailsSel = Math.Max(0, detailsSel - 10);
                        break;
                    case ConsoleKey.PageDown:
                        if (itemsCount > 0) detailsSel = Math.Min(itemsCount - 1, detailsSel + 10);
                        break;
                    case ConsoleKey.Home:
                        detailsSel = 0;
                        break;
                    case ConsoleKey.End:
                        if (itemsCount > 0) detailsSel = Math.Max(0, itemsCount - 1);
                        break;
                    case ConsoleKey.Backspace:
                        if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                        {
                            if (key.Modifiers.HasFlag(ConsoleModifiers.Control) || key.KeyChar == '\x17' || key.KeyChar == '\x7f' || key.KeyChar == '\x08')
                            {
                                _detailsSearchBuffer = DeletePreviousWord(_detailsSearchBuffer);
                            }
                            else
                            {
                                _detailsSearchBuffer = _detailsSearchBuffer[..^1];
                            }
                            detailsSel = 0;
                        }
                        break;
                    case ConsoleKey.Enter:
                        if (detailsSel >= 0 && detailsSel < itemsCount)
                        {
                            if (detailsMode == "agyswitch")
                            {
                                var accs = AgyAccountCore.GetAccounts();
                                var targetAcc = accs[detailsSel];
                                Console.CursorVisible = true;
                                AgyAccountCore.SetActiveAccount(targetAcc, false);
                                Console.CursorVisible = false;
                            }
                            else if (detailsMode == "theme")
                            {
                                var themeNames = GetThemeNames();
                                if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                                {
                                    themeNames = themeNames.Where(t => t.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
                                }
                                var selectedTheme = themeNames[detailsSel];
                                var themesPath = Environment.GetEnvironmentVariable("POSH_THEMES_PATH");
                                if (string.IsNullOrEmpty(themesPath))
                                {
                                    themesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asset", "powershell-themes");
                                    if (!Directory.Exists(themesPath))
                                    {
                                        themesPath = Path.Combine(Directory.GetCurrentDirectory(), "asset", "powershell-themes");
                                    }
                                }
                                var configPath = Path.Combine(themesPath, "config.json");
                                try
                                {
                                    File.WriteAllText(configPath, JsonSerializer.Serialize(new { active_theme = selectedTheme, enable_mobile = selectedTheme.EndsWith("-mobile") }));
                                }
                                catch { }
                                Environment.SetEnvironmentVariable("THEME", selectedTheme);
                                var themePath = Path.Combine(themesPath, $"{selectedTheme}.omp.json");
                                var agyHome = !string.IsNullOrEmpty(AgyAccountCore.AgySourceHome) ? AgyAccountCore.AgySourceHome : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini");
                                Directory.CreateDirectory(agyHome);
                                var selectedThemeFile = Path.Combine(agyHome, "selected_theme.txt");
                                File.WriteAllText(selectedThemeFile, themePath);
                                SpectrePanel.Success($"Selected theme '{selectedTheme}'. Theme will apply on exit.");
                                Thread.Sleep(1000);
                            }
                            else if (detailsMode == "learn" || detailsMode == "session" || detailsMode == "weak")
                            {
                                var topics = new[] { "jp", "en", "cs", "dsa", "interview", "[Type Custom Topic...]" };
                                var selectedTopic = topics[detailsSel];
                                if (selectedTopic == "[Type Custom Topic...]")
                                {
                                    Console.CursorVisible = true;
                                    selectedTopic = AnsiConsole.Ask<string>("Enter custom topic name:").Trim();
                                    Console.CursorVisible = false;
                                }
                                if (!string.IsNullOrEmpty(selectedTopic))
                                {
                                    Console.CursorVisible = true;
                                    if (detailsMode == "learn") LearnRouter.StartLearning(selectedTopic);
                                    else if (detailsMode == "session") StudySession.Run(selectedTopic);
                                    else if (detailsMode == "weak") WeakItemsQueue.ShowPreSessionReview(selectedTopic);
                                    Console.CursorVisible = false;
                                }
                            }
                            else if (detailsMode == "proj")
                            {
                                var workspaces = WorkspaceRegistry.GetWorkspaces();
                                if (detailsSel >= 0 && detailsSel < workspaces.Length)
                                {
                                    var targetEntry = workspaces[detailsSel];
                                    var actions = new[]
                                    {
                                        $"📂 Change Directory to {targetEntry.Name} on exit (cd)",
                                        $"💻 Open in Terminal IDE (/ide)",
                                        $"📁 Open in Windows File Explorer",
                                        $"🔀 View Git Status & Diff (/ide-diff)"
                                    };
                                    var actionIdx = SpectreMenu.ShowWithEscape($"Workspace: {targetEntry.Name}", actions, 0);
                                    if (actionIdx == 0)
                                    {
                                        var agyHome = !string.IsNullOrEmpty(AgyAccountCore.AgySourceHome) ? AgyAccountCore.AgySourceHome : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini");
                                        Directory.CreateDirectory(agyHome);
                                        var selectedProjFile = Path.Combine(agyHome, "selected_project.txt");
                                        File.WriteAllText(selectedProjFile, targetEntry.WorkspacePath);
                                        SpectrePanel.Success($"Selected workspace '{targetEntry.Name}'. Directory switch will apply on exit.");
                                        Thread.Sleep(1000);
                                    }
                                    else if (actionIdx == 1)
                                    {
                                        TerminalIde.Open(targetEntry.WorkspacePath);
                                    }
                                    else if (actionIdx == 2)
                                    {
                                        SystemHelper.OpenExplorer(targetEntry.WorkspacePath);
                                    }
                                    else if (actionIdx == 3)
                                    {
                                        GitDiffViewer.ShowDiff(targetEntry.WorkspacePath);
                                    }
                                }
                            }
                            detailsActive = false;
                        }
                        break;
                    case ConsoleKey.A:
                        if (detailsMode == "agyswitch")
                        {
                            Console.CursorVisible = true;
                            AnsiConsole.Clear();
                            var newName = AnsiConsole.Ask<string>("Enter new account name:").Trim();
                            if (!string.IsNullOrEmpty(newName))
                            {
                                try
                                {
                                    AgyAccountCore.AddAccount(newName);
                                    SpectrePanel.Success($"Account '{newName}' created successfully!");
                                    Thread.Sleep(1500);
                                }
                                catch (Exception ex)
                                {
                                    SpectrePanel.Error($"Failed to create account: {ex.Message}");
                                    Thread.Sleep(2000);
                                }
                            }
                            Console.CursorVisible = false;
                            detailsSel = 0;
                        }
                        break;
                    case ConsoleKey.D:
                        if (detailsMode == "agyswitch")
                        {
                            var accs = AgyAccountCore.GetAccounts();
                            if (detailsSel >= 0 && detailsSel < accs.Length)
                            {
                                var targetAcc = accs[detailsSel];
                                var activeAcc = AgyAccountCore.GetActiveAccount();
                                if (string.Equals(targetAcc, activeAcc, StringComparison.OrdinalIgnoreCase))
                                {
                                    Console.CursorVisible = true;
                                    SpectrePanel.Error($"Cannot delete '{targetAcc}' because it is the current active account.");
                                    Thread.Sleep(1500);
                                    Console.CursorVisible = false;
                                    break;
                                }
                                Console.CursorVisible = true;
                                AnsiConsole.Clear();
                                var confirm = AnsiConsole.Confirm($"Are you sure you want to delete account '{targetAcc}'?");
                                if (confirm)
                                {
                                    try
                                    {
                                        AgyAccountCore.DeleteAccount(targetAcc);
                                        SpectrePanel.Success($"Account '{targetAcc}' deleted successfully!");
                                        Thread.Sleep(1500);
                                    }
                                    catch (Exception ex)
                                    {
                                        SpectrePanel.Error($"Failed to delete account: {ex.Message}");
                                        Thread.Sleep(2000);
                                    }
                                }
                                Console.CursorVisible = false;
                                detailsSel = 0;
                            }
                        }
                        break;
                    case ConsoleKey.O:
                        if (detailsMode == "agyswitch")
                        {
                            var accs = AgyAccountCore.GetAccounts();
                            if (detailsSel >= 0 && detailsSel < accs.Length)
                            {
                                var targetAcc = accs[detailsSel];
                                Console.CursorVisible = true;
                                AnsiConsole.Clear();
                                var confirm = AnsiConsole.Confirm($"Are you sure you want to log out of '{targetAcc}'?");
                                if (confirm)
                                {
                                    AgyAccountCore.LogoutAccount(targetAcc);
                                    SpectrePanel.Success($"Logged out of '{targetAcc}' successfully!");
                                    Thread.Sleep(1500);
                                }
                                Console.CursorVisible = false;
                            }
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.Escape:
                    case ConsoleKey.Q:
                        if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                        {
                            _detailsSearchBuffer = "";
                            detailsSel = 0;
                        }
                        else
                        {
                            detailsActive = false;
                            _detailsSearchBuffer = "";
                        }
                        break;
                    default:
                        if (key.KeyChar >= 32 && key.KeyChar <= 126 && key.Key != ConsoleKey.Enter)
                        {
                            if (detailsMode == "agyswitch" && (key.Key == ConsoleKey.A || key.Key == ConsoleKey.D || key.Key == ConsoleKey.O) && string.IsNullOrEmpty(_detailsSearchBuffer))
                            {
                                break;
                            }
                            _detailsSearchBuffer += key.KeyChar;
                            detailsSel = 0;
                        }
                        break;
                }
                continue;
            }

            // Normal mode keys
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.K:
                    selectionIndex = Math.Max(0, selectionIndex - 1);
                    break;
                case ConsoleKey.DownArrow:
                case ConsoleKey.J:
                    selectionIndex = Math.Min(visibleRows.Count - 1, selectionIndex + 1);
                    break;
                case ConsoleKey.PageUp:
                    selectionIndex = Math.Max(0, selectionIndex - 10);
                    break;
                case ConsoleKey.PageDown:
                    selectionIndex = Math.Min(visibleRows.Count - 1, selectionIndex + 10);
                    break;
                case ConsoleKey.Home:
                    selectionIndex = 0;
                    break;
                case ConsoleKey.End:
                    selectionIndex = Math.Max(0, visibleRows.Count - 1);
                    break;
                case ConsoleKey.Divide:
                case ConsoleKey.Oem2:
                    searching = true;
                    break;
                case ConsoleKey.Enter:
                case ConsoleKey.RightArrow:
                    if (selectionIndex >= 0 && selectionIndex < visibleRows.Count)
                    {
                        var row = visibleRows[selectionIndex];
                        if (row.Type == VisibleRowType.Exit) return;
                        if (row.Type == VisibleRowType.Category)
                        {
                            if (_expandedCategories.Contains(row.Node.Id)) _expandedCategories.Remove(row.Node.Id);
                            else _expandedCategories.Add(row.Node.Id);
                        }
                        else if (row.Type == VisibleRowType.Group)
                        {
                            if (_expandedGroups.Contains(row.Node.Id)) _expandedGroups.Remove(row.Node.Id);
                            else _expandedGroups.Add(row.Node.Id);
                        }
                        else if (row.Type == VisibleRowType.Command)
                        {
                            var alias = row.Node.Command!.Alias;
                            if (StatusWidgetRegistry.GetByAlias(alias) != null)
                            {
                                if (_expandedWidgets.Contains(alias)) _expandedWidgets.Remove(alias);
                                else _expandedWidgets.Add(alias);
                            }
                            else if (string.Equals(alias, "agyswitch", StringComparison.OrdinalIgnoreCase))
                            {
                                detailsActive = true;
                                detailsMode = "agyswitch";
                                var accs = AgyAccountCore.GetAccounts();
                                var activeAcc = AgyAccountCore.GetActiveAccount();
                                detailsSel = Array.IndexOf(accs, activeAcc);
                                if (detailsSel < 0) detailsSel = 0;
                            }
                            else if (string.Equals(alias, "theme", StringComparison.OrdinalIgnoreCase))
                            {
                                detailsActive = true;
                                detailsMode = "theme";
                                var themeFiles = GetThemeNames();
                                var currentTheme = Environment.GetEnvironmentVariable("THEME");
                                detailsSel = Array.IndexOf(themeFiles, currentTheme);
                                if (detailsSel < 0) detailsSel = 0;
                            }
                            else if (string.Equals(alias, "learn", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(alias, "session", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(alias, "weak", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(alias, "proj", StringComparison.OrdinalIgnoreCase))
                            {
                                detailsActive = true;
                                detailsMode = alias.ToLowerInvariant();
                                detailsSel = 0;
                            }
                            else
                            {
                                Console.CursorVisible = true;
                                Program.RunCommand(alias);
                                AnsiConsole.WriteLine();
                                AnsiConsole.MarkupLine("[dim]Press any key to return to Control Center...[/]");
                                Console.ReadKey(true);
                                Console.CursorVisible = false;
                            }
                        }
                    }
                    break;
                case ConsoleKey.LeftArrow:
                    if (selectionIndex >= 0 && selectionIndex < visibleRows.Count)
                    {
                        var row = visibleRows[selectionIndex];
                        if (row.Type == VisibleRowType.Category && _expandedCategories.Contains(row.Node.Id))
                        {
                            _expandedCategories.Remove(row.Node.Id);
                        }
                        else if (row.Type == VisibleRowType.Group && _expandedGroups.Contains(row.Node.Id))
                        {
                            _expandedGroups.Remove(row.Node.Id);
                        }
                        else if (row.Type == VisibleRowType.Command && _expandedWidgets.Contains(row.Node.Command!.Alias))
                        {
                            _expandedWidgets.Remove(row.Node.Command.Alias);
                        }
                    }
                    break;
                case ConsoleKey.Escape:
                case ConsoleKey.Q:
                    ScreenChrome.ShowCursor();
                    return;
                default:
                    if (key.KeyChar >= 32 && key.KeyChar <= 126 && key.Key != ConsoleKey.Enter)
                    {
                        searching = true;
                        searchBuffer = key.KeyChar == '/' ? "/" : "/" + key.KeyChar;
                        selectionIndex = 0;
                    }
                    break;
            }
        }
    }

    private void RenderTree(List<VisibleRow> rows, int selIdx, bool searching, string searchBuffer)
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn().NoWrap());

        var isCompact = Config.Current.Density == "compact";

        if (rows.Count == 0)
        {
            grid.AddRow(new Markup($"  [dim]No matching commands found for '{searchBuffer.EscapeMarkup()}'. Press Esc to clear.[/]"));
        }
        else
        {
            int winHeight = Console.WindowHeight > 0 ? Console.WindowHeight : 30;
            int bannerHeight = (winHeight < 32) ? 4 : 11;
            int maxRows = Math.Max(2, winHeight - bannerHeight - 8);

            int topRow = 0;
            if (selIdx >= maxRows)
            {
                topRow = selIdx - (maxRows / 2);
            }
            if (topRow + maxRows > rows.Count)
            {
                topRow = Math.Max(0, rows.Count - maxRows);
            }
            int endRow = Math.Min(rows.Count, topRow + maxRows);

            if (topRow > 0)
            {
                grid.AddRow(new Markup($"  [dim]▲ ... {topRow} items above ...[/]"));
            }

            for (int i = topRow; i < endRow; i++)
            {
                var row = rows[i];
                var isSelected = (i == selIdx);
                var prefix = isSelected ? "[green bold]> [/]" : "  ";

                var treePrefix = "";
                if (row.Indent > 0)
                {
                    treePrefix = new string(' ', (row.Indent - 1) * 3) + "├── ";
                }

                if (row.Type == VisibleRowType.Separator)
                {
                    grid.AddRow(new Markup("[dim]  ────────────────────────────[/]"));
                    continue;
                }

                if (row.Type == VisibleRowType.Exit)
                {
                    var label = isSelected ? $"[green bold]{row.Node.Label.EscapeMarkup()}[/]" : row.Node.Label.EscapeMarkup();
                    grid.AddRow(new Markup($"{prefix}{label}"));
                    continue;
                }

                if (row.Type == VisibleRowType.Category)
                {
                    var isExpanded = _expandedCategories.Contains(row.Node.Id) || !string.IsNullOrEmpty(searchBuffer);
                    var sign = isExpanded ? "[[-]]" : "[[+]]";
                    var catIcon = Icons.GetCategoryIcon(row.Node.Label);
                    var hk = Icons.GetCategoryHotkey(row.Node.Label);
                    var hkSuffix = string.IsNullOrEmpty(hk) ? "" : $" [dim]({hk})[/]";

                    var signMarkup = $"[bold yellow]{sign}[/]";
                    var safeText = $"{catIcon} {row.Node.Label.EscapeMarkup()}";
                    var label = isSelected ? $"[green bold]{sign} {safeText}[/]{hkSuffix}" : $"{signMarkup} [bold cyan]{safeText}[/]{hkSuffix}";
                    grid.AddRow(new Markup($"{prefix}{label}"));
                }
                else if (row.Type == VisibleRowType.Group)
                {
                    var isExpanded = _expandedGroups.Contains(row.Node.Id) || !string.IsNullOrEmpty(searchBuffer);
                    var sign = isExpanded ? "[[-]]" : "[[+]]";
                    var rawLabel = row.Node.Label.Trim();
                    var cleanLabel = System.Text.RegularExpressions.Regex.Replace(rawLabel, @"^\[/[^\]]+\]\s*", "").EscapeMarkup();

                    var signMarkup = $"[bold yellow]{sign}[/]";
                    var treeDim = $"[dim]{treePrefix.EscapeMarkup()}[/]";
                    var label = isSelected ? $"[green bold]{treePrefix}{sign} 📂 {cleanLabel}[/]" : $"{treeDim}{signMarkup} [bold yellow]📂 {cleanLabel}[/]";
                    grid.AddRow(new Markup($"{prefix}{label}"));
                }
                else if (row.Type == VisibleRowType.Command)
                {
                    var cmd = row.Node.Command!;
                    var icon = Icons.GetCommandIcon(cmd.Alias, cmd.Category);

                    var displayLabel = $"/{cmd.Alias} — {cmd.DisplayName}".EscapeMarkup();
                    var desc = isCompact && !isSelected ? "" : $" [dim]· {cmd.Description.EscapeMarkup()}[/]";

                    var treeDim = $"[dim]{treePrefix.EscapeMarkup()}[/]";
                    var label = isSelected
                        ? $"[green bold]{treePrefix}{icon} {displayLabel}{desc}[/]"
                        : $"{treeDim}{icon} [white]{displayLabel}[/]{desc}";
                    grid.AddRow(new Markup($"{prefix}{label}"));
                }
                else if (row.Type == VisibleRowType.Widget)
                {
                    var alias = row.Node.Command!.Alias;
                    var widget = StatusWidgetRegistry.GetByAlias(alias);
                    if (widget != null)
                    {
                        var renderable = widget.Render();
                        var indentPanel = new Panel(renderable)
                        {
                            Border = BoxBorder.Rounded,
                            BorderStyle = new Style(isSelected ? Color.Green : Color.Grey),
                            Header = new PanelHeader($"[bold cyan]{alias} status[/]")
                        };

                        var indentGrid = new Grid();
                        indentGrid.AddColumn(new GridColumn().Width(row.Indent * 3));
                        indentGrid.AddColumn(new GridColumn());
                        indentGrid.AddRow(new Markup(""), indentPanel);

                        grid.AddRow(indentGrid);
                    }
                }
            }

            if (endRow < rows.Count)
            {
                grid.AddRow(new Markup($"  [dim]▼ ... {rows.Count - endRow} items below ...[/]"));
            }
        }

        var winWidth = 80;
        try { winWidth = Console.WindowWidth; } catch {}
        var w = Math.Max(50, winWidth - 2);

        var headerText = searching ? $"Search (Esc to clear): [green]{searchBuffer.EscapeMarkup()}[/]_" : "Type [[/]] to search ...";
        IRenderable content = grid;

        if (selIdx >= 0 && selIdx < rows.Count)
        {
            var highlighted = rows[selIdx];
            if (highlighted.Type == VisibleRowType.Command && highlighted.Node.Command != null)
            {
                var cmd = highlighted.Node.Command;
                var maxNoteLen = Math.Max(30, winWidth - 18);
                var noteText = (cmd.HelpLines != null && cmd.HelpLines.Length > 0) 
                    ? string.Join(" ", cmd.HelpLines) 
                    : cmd.Description;
                if (noteText.Length > maxNoteLen) noteText = noteText[..maxNoteLen] + "…";
                grid.AddRow(new Markup($"\n[bold yellow]💡 Note:[/] [white]{noteText.EscapeMarkup()}[/]"));
            }
            else if (highlighted.Type == VisibleRowType.Category)
            {
                var catNote = $"Category '{highlighted.Node.Label}' — Press [Enter] or [→] to expand/collapse.";
                var maxNoteLen = Math.Max(30, winWidth - 18);
                if (catNote.Length > maxNoteLen) catNote = catNote[..maxNoteLen] + "…";
                grid.AddRow(new Markup($"\n[bold yellow]💡 Note:[/] [white]{catNote.EscapeMarkup()}[/]"));
            }
        }

        var hotkeyBar = new Markup("[dim]Hotkeys: cg (Git) · cdk (Docker) · cnav (Nav) · cai (AI) · csys (Sys) · cnet (Net) · cssh (SSH)\nCombos:  [[Ctrl+Space]] Complete · [[Ctrl+Shift+C]] CC TUI · [[Ctrl+Shift+B]] Build · [[Ctrl+Shift+T]] Test · [[F7]] History[/]");
        content = new Rows(content, new Markup("\n"), hotkeyBar);

        var outerPanel = new Panel(content)
        {
            Header = new PanelHeader($"[bold cyan] {headerText} [/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1),
            Width = w
        };

        ScreenChrome.WriteSmooth(outerPanel);
    }

    private void RenderSubPageSelection(string mode, int selIdx)
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn().NoWrap());

        if (mode == "agyswitch")
        {
            grid.AddRow(new Markup("[cyan bold]Select Account to Switch:[/]\n"));
            var accs = AgyAccountCore.GetAccounts();
            var activeAcc = AgyAccountCore.GetActiveAccount();
            for (var i = 0; i < accs.Length; i++)
            {
                var isSelected = (i == selIdx);
                var isActive = (accs[i] == activeAcc);
                var prefix = isSelected ? "[green bold]> [/]" : "  ";
                var suffix = isActive ? " [green](Active)[/]" : "";
                var displayName = accs[i];
                if (string.Equals(accs[i], "default", StringComparison.OrdinalIgnoreCase))
                {
                    var email = AgyAccountCore.GetAccountEmail("default");
                    if (!string.IsNullOrEmpty(email)) displayName = $"default ({email})";
                }
                var stats = AgyAccountCore.GetAccountStats(accs[i]);
                var loginStatus = stats.TokenStatus == "Logged In" ? "[green]✔[/]" : "[red]✘[/]";
                grid.AddRow(new Markup($"{prefix}{displayName.EscapeMarkup()} [dim]({loginStatus})[/]{suffix}"));
            }
            grid.AddRow(new Markup("\n[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc Cancel[/]"));
            grid.AddRow(new Markup("[dim]a Create Account  ·  d Delete  ·  o Log Out[/]"));
        }
        else if (mode == "theme")
        {
            var themeNames = GetThemeNames();
            var filtered = string.IsNullOrEmpty(_detailsSearchBuffer)
                ? themeNames
                : themeNames.Where(t => t.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();

            var currentTheme = Environment.GetEnvironmentVariable("THEME");

            grid.AddRow(new Markup($"[cyan bold]Select Oh My Posh Theme[/] [dim]({filtered.Length}/{themeNames.Length} themes)[/]:\n"));
            if (!string.IsNullOrEmpty(_detailsSearchBuffer))
            {
                grid.AddRow(new Markup($"[yellow]Search:[/] [white]{_detailsSearchBuffer.EscapeMarkup()}_[/]\n"));
            }
            else
            {
                grid.AddRow(new Markup("[dim]Type to filter themes (Esc to clear / cancel)[/]\n"));
            }

            if (filtered.Length == 0)
            {
                grid.AddRow(new Markup($"  [dim]No themes matching '{_detailsSearchBuffer.EscapeMarkup()}'.[/]"));
            }
            else
            {
                int maxRows = 12;
                int topRow = 0;
                if (selIdx >= maxRows)
                {
                    topRow = selIdx - maxRows + 1;
                }
                int endRow = Math.Min(filtered.Length, topRow + maxRows);

                if (topRow > 0)
                {
                    grid.AddRow(new Markup($"  [dim]▲ ... {topRow} items above ...[/]"));
                }

                for (var i = topRow; i < endRow; i++)
                {
                    var isSelected = (i == selIdx);
                    var isActive = string.Equals(filtered[i], currentTheme, StringComparison.OrdinalIgnoreCase);
                    var prefix = isSelected ? "[green bold]> [/]" : "  ";
                    var suffix = isActive ? " [bold green][[ACTIVE]][/]" : "";
                    var nameMarkup = isSelected ? $"[bold green]{filtered[i].EscapeMarkup()}[/]" : $"[white]{filtered[i].EscapeMarkup()}[/]";
                    grid.AddRow(new Markup($"{prefix}{nameMarkup}{suffix}"));
                }

                if (endRow < filtered.Length)
                {
                    grid.AddRow(new Markup($"  [dim]▼ ... {filtered.Length - endRow} items below ...[/]"));
                }
            }

            grid.AddRow(new Markup("\n[dim]↑/↓/j/k Navigate  ·  PgDn/PgUp Page  ·  Enter Select  ·  Esc Cancel[/]"));
        }
        else if (mode == "learn" || mode == "session" || mode == "weak")
        {
            grid.AddRow(new Markup($"[cyan bold]Select Topic for {mode.ToUpperInvariant()}:[/]\n"));
            var topics = new[] { "jp (Japanese / Language)", "en (English Vocabulary)", "cs (C# Quiz)", "dsa (Data Structures & Algorithms)", "interview (Question Bank & STAR)", "[Type Custom Topic...]" };
            for (var i = 0; i < topics.Length; i++)
            {
                var isSelected = (i == selIdx);
                var prefix = isSelected ? "[green bold]> [/]" : "  ";
                grid.AddRow(new Markup($"{prefix}{topics[i].EscapeMarkup()}"));
            }
            grid.AddRow(new Markup("\n[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc Cancel[/]"));
        }
        else if (mode == "proj")
        {
            var allWorkspaces = WorkspaceRegistry.GetWorkspaces();
            var workspaces = string.IsNullOrEmpty(_detailsSearchBuffer)
                ? allWorkspaces
                : allWorkspaces.Where(w => w != null && 
                    ((w.Name != null && w.Name.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)) ||
                     (w.WorkspacePath != null && w.WorkspacePath.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)))).ToArray();

            var currentDir = Directory.GetCurrentDirectory();
            var activeTheme = Environment.GetEnvironmentVariable("THEME") ?? "";
            var winWidth = 100;
            try { winWidth = Console.WindowWidth; } catch { }
            bool isMobile = winWidth < 90 || activeTheme.EndsWith("-mobile", StringComparison.OrdinalIgnoreCase);

            grid.AddRow(new Markup($"[bold green]📁 Registered Workspace Navigator (cnav)[/] [dim]({workspaces.Length}/{allWorkspaces.Length} workspaces)[/]:\n"));

            if (!string.IsNullOrEmpty(_detailsSearchBuffer))
            {
                grid.AddRow(new Markup($"[yellow]Search:[/] [white]{_detailsSearchBuffer.EscapeMarkup()}_[/]\n"));
            }
            else
            {
                grid.AddRow(new Markup("[dim]Type to filter workspaces (Esc to clear / cancel)[/]\n"));
            }

            if (workspaces.Length == 0)
            {
                grid.AddRow(new Markup($"  [dim]No workspaces matching '{_detailsSearchBuffer.EscapeMarkup()}'.[/]"));
            }
            else
            {
                int maxRows = isMobile ? 8 : 10;
                int topRow = 0;
                if (selIdx >= maxRows)
                {
                    topRow = selIdx - maxRows + 1;
                }
                int endRow = Math.Min(workspaces.Length, topRow + maxRows);

                if (topRow > 0)
                {
                    grid.AddRow(new Markup($"  [dim]▲ ... {topRow} items above ...[/]"));
                }

                if (isMobile)
                {
                    for (var i = topRow; i < endRow; i++)
                    {
                        if (workspaces[i] == null) continue;
                        var wsPath = workspaces[i].WorkspacePath ?? "";
                        var wsName = workspaces[i].Name ?? "Unnamed Workspace";
                        var isSelected = (i == selIdx);
                        var isCurrent = !string.IsNullOrEmpty(wsPath) && string.Equals(wsPath.TrimEnd('\\', '/'), currentDir.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);

                        var prefix = isSelected ? "[green bold]❯ [/]" : "  ";
                        var status = isCurrent ? "[bold green][[ACTIVE]][/] " : "";
                        var branch = WorkspaceRegistry.GetGitBranch(wsPath);
                        var branchSuffix = !string.IsNullOrEmpty(branch) ? $" [yellow]({branch})[/]" : "";
                        var nameMarkup = isSelected ? $"[bold green]{wsName.EscapeMarkup()}[/]" : $"[bold white]{wsName.EscapeMarkup()}[/]";

                        grid.AddRow(new Markup($"{prefix}{status}{nameMarkup}{branchSuffix}\n   [dim]{wsPath.EscapeMarkup()}[/]"));
                    }
                }
                else
                {
                    var table = new Table()
                        .Border(TableBorder.Rounded)
                        .BorderColor(Color.Cyan1)
                        .Expand()
                        .AddColumn(new TableColumn("[bold cyan]Sel[/]").Width(3).Centered().NoWrap())
                        .AddColumn(new TableColumn("[bold cyan]Status[/]").Width(10).Centered().NoWrap())
                        .AddColumn(new TableColumn("[bold cyan]Workspace Name[/]").Width(26).NoWrap())
                        .AddColumn(new TableColumn("[bold cyan]Git Branch[/]").Width(14).NoWrap())
                        .AddColumn(new TableColumn("[bold cyan]Directory Path[/]").NoWrap());

                    for (var i = topRow; i < endRow; i++)
                    {
                        if (workspaces[i] == null) continue;
                        var wsPath = workspaces[i].WorkspacePath ?? "";
                        var wsName = workspaces[i].Name ?? "Unnamed Workspace";
                        var isSelected = (i == selIdx);
                        var isCurrent = !string.IsNullOrEmpty(wsPath) && string.Equals(wsPath.TrimEnd('\\', '/'), currentDir.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);

                        var cursorMarkup = isSelected ? "[green bold]❯[/]" : " ";
                        var statusMarkup = isCurrent ? "[bold black on green] ACTIVE [/]" : "[dim cyan][[READY]][/]";
                        
                        var nameMarkup = isSelected 
                            ? $"[bold green]📁 {wsName.EscapeMarkup()}[/]" 
                            : $"[bold white]📁 {wsName.EscapeMarkup()}[/]";

                        var branch = WorkspaceRegistry.GetGitBranch(wsPath);
                        var branchMarkup = !string.IsNullOrEmpty(branch) 
                            ? $"[yellow]🌿 {branch.EscapeMarkup()}[/]" 
                            : "[dim]—[/]";

                        var pathMarkup = isSelected
                            ? $"[cyan]{wsPath.EscapeMarkup()}[/]"
                            : $"[dim]{wsPath.EscapeMarkup()}[/]";

                        table.AddRow(
                            new Markup(cursorMarkup),
                            new Markup(statusMarkup),
                            new Markup(nameMarkup),
                            new Markup(branchMarkup),
                            new Markup(pathMarkup)
                        );
                    }
                    grid.AddRow(table);
                }

                if (endRow < workspaces.Length)
                {
                    grid.AddRow(new Markup($"  [dim]▼ ... {workspaces.Length - endRow} items below ...[/]"));
                }
            }

            var selTarget = (selIdx >= 0 && selIdx < workspaces.Length) ? workspaces[selIdx]?.WorkspacePath : null;
            if (!string.IsNullOrEmpty(selTarget))
            {
                grid.AddRow(new Markup($"\n[dim]Selected Target:[/] [bold cyan]{selTarget.EscapeMarkup()}[/]"));
            }

            grid.AddRow(new Markup("[bold cyan][[Enter]][/] Actions ([green]cd[/] / [cyan]/ide[/] / [yellow]Explorer[/] / [magenta]Git Diff[/])  ·  [bold cyan][[Esc]][/] Cancel"));
        }

        var panel = new Panel(grid)
        {
            Header = new PanelHeader($"[bold cyan] {mode.ToUpperInvariant()} Selector [/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1),
            Expand = true
        };
        ScreenChrome.WriteSmooth(panel);
    }


}
