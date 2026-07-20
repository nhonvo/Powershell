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

public sealed class FlatTreeRenderer : IMenuRenderer
{
    private readonly HashSet<string> _expandedCategories = new();
    private readonly HashSet<string> _expandedGroups = new();
    private readonly HashSet<string> _expandedWidgets = new();

    public void Run(MenuNode root)
    {
        var selectionIndex = 0;
        var detailsActive = false;
        var detailsMode = "";
        var detailsSel = 0;
        var searching = false;
        var searchBuffer = "";

        try { Console.CursorVisible = false; } catch { }

        while (true)
        {
            var categories = GetActiveChildren(root);
            var visibleRows = new List<VisibleRow>();

            if (!string.IsNullOrEmpty(searchBuffer))
            {
                // Filter every leaf across every category
                var q = searchBuffer.ToLowerInvariant();
                foreach (var cat in categories)
                {
                    if (cat.Kind == MenuNodeKind.Separator || cat.Kind == MenuNodeKind.Exit) continue;

                    var catMatches = new List<MenuNode>();
                    foreach (var child in GetActiveChildren(cat))
                    {
                        if (child.Kind == MenuNodeKind.Group)
                        {
                            var groupMatches = GetActiveChildren(child)
                                .Where(sub => sub.SearchKey.Contains(q) ||
                                              (sub.Command != null && sub.Command.Alias.ToLowerInvariant().Contains(q)))
                                .ToList();
                            if (groupMatches.Count > 0)
                            {
                                catMatches.Add(child with { Children = groupMatches.ToArray() });
                            }
                        }
                        else if (child.Kind == MenuNodeKind.Command && child.Command != null)
                        {
                            if (child.SearchKey.Contains(q) || child.Command.Alias.ToLowerInvariant().Contains(q))
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

            if (selectionIndex >= visibleRows.Count) selectionIndex = Math.Max(0, visibleRows.Count - 1);

            ScreenChrome.RenderBanner();

            if (detailsActive)
            {
                RenderSubPageSelection(detailsMode, detailsSel);
            }
            else
            {
                RenderTree(visibleRows, selectionIndex, searching, searchBuffer);
            }

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
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (searchBuffer.Length > 0) searchBuffer = searchBuffer[..^1];
                }
                else if (key.KeyChar >= 32 && key.KeyChar <= 126)
                {
                    searchBuffer += key.KeyChar;
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
                    itemsCount = GetThemeNames().Length;
                }
                else if (detailsMode == "learn" || detailsMode == "session" || detailsMode == "weak")
                {
                    itemsCount = 6;
                }
                else if (detailsMode == "proj")
                {
                    itemsCount = WorkspaceRegistry.GetWorkspaces().Length;
                }

                if (itemsCount == 0)
                {
                    detailsActive = false;
                    continue;
                }

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.K:
                        detailsSel = (detailsSel - 1 + itemsCount) % itemsCount;
                        break;
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.J:
                        detailsSel = (detailsSel + 1) % itemsCount;
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
                                var selectedThemeFile = Path.Combine(AgyAccountCore.AgySourceHome, "selected_theme.txt");
                                File.WriteAllText(selectedThemeFile, themePath);
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
                                var selectedProj = workspaces[detailsSel].WorkspacePath;
                                var selectedProjFile = Path.Combine(AgyAccountCore.AgySourceHome, "selected_project.txt");
                                File.WriteAllText(selectedProjFile, selectedProj);
                                AnsiConsole.MarkupLine($"[green][[Workspace]] Selected workspace '{workspaces[detailsSel].Name}'. Switch will apply on exit.[/]");
                                Thread.Sleep(1000);
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
                        detailsActive = false;
                        break;
                }
                continue;
            }

            // Normal mode keys
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    selectionIndex = Math.Max(0, selectionIndex - 1);
                    break;
                case ConsoleKey.DownArrow:
                    selectionIndex = Math.Min(visibleRows.Count - 1, selectionIndex + 1);
                    break;
                case ConsoleKey.Divide:
                case ConsoleKey.Oem2:
                    searching = true;
                    break;
                case ConsoleKey.Enter:
                case ConsoleKey.RightArrow:
                    if (selectionIndex < visibleRows.Count)
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
                    if (selectionIndex < visibleRows.Count)
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
                    return;
            }
        }
    }

    private static MenuNode[] GetActiveChildren(MenuNode parent)
    {
        var enableAi = AgyAiCore.IsAiOllamaEnabled();
        var enableAgy = AgyAiCore.IsAgyEnabled();

        var list = new List<MenuNode>();
        foreach (var child in parent.Children)
        {
            if (child.Kind == MenuNodeKind.Category)
            {
                if (child.Label.Contains("AI Agent & Ollama") && !enableAi) continue;
                if (child.Label.Contains("AGY Account Switch") && !enableAgy) continue;
            }
            else if (child.Kind == MenuNodeKind.Command && child.Command != null)
            {
                if (child.Command.RequiresAiOllama && !enableAi) continue;
                if (child.Command.RequiresAgy && !enableAgy) continue;
            }

            if (child.Id == "agy-cli" && !enableAgy)
            {
                var originalCmd = child.Command!;
                var rewrittenCmd = originalCmd with { DisplayName = "Launch Claude Code CLI (claude)" };
                list.Add(child with { Label = "Launch Claude Code CLI (claude)", Command = rewrittenCmd });
                continue;
            }

            list.Add(child);
        }
        return list.ToArray();
    }

    private static string[] GetThemeNames()
    {
        var themesPath = Environment.GetEnvironmentVariable("POSH_THEMES_PATH");
        if (string.IsNullOrEmpty(themesPath) || !Directory.Exists(themesPath))
        {
            themesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asset", "powershell-themes");
            if (!Directory.Exists(themesPath))
            {
                themesPath = Path.Combine(Directory.GetCurrentDirectory(), "asset", "powershell-themes");
            }
        }
        if (!Directory.Exists(themesPath)) return Array.Empty<string>();
        return Directory.GetFiles(themesPath, "*.omp.json").Select(f => Path.GetFileName(f).Replace(".omp.json", "")).OrderBy(f => f).ToArray();
    }

    private void RenderTree(List<VisibleRow> rows, int selIdx, bool searching, string searchBuffer)
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn().NoWrap());

        var isCompact = Config.Current.Density == "compact";

        for (int i = 0; i < rows.Count; i++)
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
                var sign = isExpanded ? "[-]" : "[+]";
                var safeText = $"{sign} {row.Node.Label}".EscapeMarkup();
                var label = isSelected ? $"[green bold]{safeText}[/]" : $"[bold cyan]{safeText}[/]";
                grid.AddRow(new Markup($"{prefix}{label}"));
            }
            else if (row.Type == VisibleRowType.Group)
            {
                var isExpanded = _expandedGroups.Contains(row.Node.Id) || !string.IsNullOrEmpty(searchBuffer);
                var sign = isExpanded ? "[-]" : "[+]";
                var safeText = $"{treePrefix}{sign} {row.Node.Label.Trim()}".EscapeMarkup();
                var label = isSelected ? $"[green bold]{safeText}[/]" : $"[bold yellow]{safeText}[/]";
                grid.AddRow(new Markup($"{prefix}{label}"));
            }
            else if (row.Type == VisibleRowType.Command)
            {
                var cmd = row.Node.Command!;
                var icon = Icons.GetFileIcon(cmd.Alias == "proj" ? ".cs" : cmd.Alias == "db-tui" ? ".sql" : ".txt");
                if (cmd.Category == "[AI Agent & Ollama]") icon = Icons.GetProviderIcon(cmd.Alias.Split('-')[0]);
                if (cmd.Category == "[Learn & Study]") icon = Icons.GetSubjectIcon(cmd.Alias);

                var displayLabel = $"/{cmd.Alias} — {cmd.DisplayName}".EscapeMarkup();
                var desc = isCompact && !isSelected ? "" : $" [dim]· {cmd.Description.EscapeMarkup()}[/]";

                var label = $"{treePrefix}{icon} {displayLabel}{desc}";
                label = isSelected ? $"[green bold]{label}[/]" : $"  {label}";
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

        var headerText = searching ? $"Search (Esc to clear): [green]{searchBuffer.EscapeMarkup()}[/]_" : "Type [[/]] to search ...";
        IRenderable content = grid;

        if (isCompact && selIdx >= 0 && selIdx < rows.Count)
        {
            var highlighted = rows[selIdx];
            if (highlighted.Type == VisibleRowType.Command && highlighted.Node.Command != null)
            {
                var footer = new Markup($"[dim] {highlighted.Node.Command.Description.EscapeMarkup()} [/]");
                content = new Rows(grid, new Markup("\n"), footer);
            }
        }

        var outerPanel = new Panel(content)
        {
            Header = new PanelHeader($"[bold cyan] {headerText} [/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1)
        };

        AnsiConsole.Write(outerPanel);
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
            grid.AddRow(new Markup("[cyan bold]Select Oh My Posh Theme:[/]\n"));
            var themeNames = GetThemeNames();
            var currentTheme = Environment.GetEnvironmentVariable("THEME");
            for (var i = 0; i < themeNames.Length; i++)
            {
                var isSelected = (i == selIdx);
                var isActive = (themeNames[i] == currentTheme);
                var prefix = isSelected ? "[green bold]> [/]" : "  ";
                var suffix = isActive ? " [green](Active)[/]" : "";
                grid.AddRow(new Markup($"{prefix}{themeNames[i].EscapeMarkup()}{suffix}"));
            }
            grid.AddRow(new Markup("\n[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc Cancel[/]"));
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
            grid.AddRow(new Markup("[cyan bold]Select Workspace directory to switch to on exit:[/]\n"));
            var workspaces = WorkspaceRegistry.GetWorkspaces();
            for (var i = 0; i < workspaces.Length; i++)
            {
                var isSelected = (i == selIdx);
                var prefix = isSelected ? "[green bold]> [/]" : "  ";
                grid.AddRow(new Markup($"{prefix}{workspaces[i].Name.EscapeMarkup()} [dim]({workspaces[i].WorkspacePath.EscapeMarkup()})[/]"));
            }
            grid.AddRow(new Markup("\n[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc Cancel[/]"));
        }

        var panel = new Panel(grid)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1)
        };
        AnsiConsole.Write(panel);
    }
}
