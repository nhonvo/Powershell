using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using Spectre.Console;
using Spectre.Console.Rendering;
using AgyTui.Components;
using AgyTui.Registry;

namespace AgyTui;

public sealed class ThreePaneRenderer : IMenuRenderer
{
    private readonly HashSet<string> _expandedGroups = new();

    public void Run(MenuNode root)
    {
        var leftSel = 0;
        var midSel = 0;
        var midActive = false;
        var detailsActive = false;
        var detailsMode = "";
        var detailsSel = 0;

        try { Console.CursorVisible = false; } catch {}

        while (true)
        {
            var categories = GetActiveChildren(root);
            if (leftSel >= categories.Length) leftSel = Math.Max(0, categories.Length - 1);
            var category = categories[leftSel];

            // Build visible items list for middle pane
            var visibleItems = new List<MenuNode>();
            foreach (var child in GetActiveChildren(category))
            {
                visibleItems.Add(child);
                if (child.Kind == MenuNodeKind.Group && _expandedGroups.Contains(child.Id))
                {
                    foreach (var subChild in child.Children)
                    {
                        visibleItems.Add(subChild);
                    }
                }
            }

            if (midSel >= visibleItems.Count) midSel = Math.Max(0, visibleItems.Count - 1);

            ScreenChrome.RenderBanner();
            RenderPanes(categories, leftSel, visibleItems, midSel, midActive, detailsActive, detailsSel, detailsMode);

            var key = Console.ReadKey(true);

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
                                catch {}
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

            if (!midActive)
            {
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                    {
                        var next = leftSel;
                        do
                        {
                            next = Math.Max(0, next - 1);
                        }
                        while (next > 0 && IsSep(categories, next));
                        if (!IsSep(categories, next))
                        {
                            leftSel = next;
                            midSel = 0;
                        }
                        break;
                    }
                    case ConsoleKey.DownArrow:
                    {
                        var next = leftSel;
                        do
                        {
                            next = Math.Min(categories.Length - 1, next + 1);
                        }
                        while (next < categories.Length - 1 && IsSep(categories, next));
                        if (!IsSep(categories, next))
                        {
                            leftSel = next;
                            midSel = 0;
                        }
                        break;
                    }
                    case ConsoleKey.Enter:
                    case ConsoleKey.RightArrow:
                    case ConsoleKey.Tab:
                        if (category.Kind == MenuNodeKind.Exit) return;
                        if (visibleItems.Count > 0) midActive = true;
                        break;
                    case ConsoleKey.Escape:
                    case ConsoleKey.Q:
                        return;
                }
            }
            else
            {
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        midSel = Math.Max(0, midSel - 1);
                        break;
                    case ConsoleKey.DownArrow:
                        midSel = Math.Min(visibleItems.Count - 1, midSel + 1);
                        break;
                    case ConsoleKey.Enter:
                    case ConsoleKey.RightArrow:
                    case ConsoleKey.Tab:
                        if (midSel < visibleItems.Count)
                        {
                            var item = visibleItems[midSel];
                            if (item.Kind == MenuNodeKind.Group)
                            {
                                if (_expandedGroups.Contains(item.Id)) _expandedGroups.Remove(item.Id);
                                else _expandedGroups.Add(item.Id);
                            }
                            else if (item.Command != null)
                            {
                                var alias = item.Command.Alias;
                                if (string.Equals(alias, "agyswitch", StringComparison.OrdinalIgnoreCase))
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
                                else if (StatusWidgetRegistry.GetByAlias(alias) != null)
                                {
                                    // Widgets are rendered directly on the right pane, no direct execution needed on Enter
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
                    case ConsoleKey.Escape:
                        midActive = false;
                        break;
                    case ConsoleKey.Q:
                        return;
                }
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

    private static bool IsSep(MenuNode[] categories, int idx) => categories[idx].Label.Length > 0 && categories[idx].Label[0] == '─';

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

    private void RenderPanes(
        MenuNode[] categories,
        int leftSel,
        List<MenuNode> visibleItems,
        int midSel,
        bool midActive,
        bool detailsActive,
        int detailsSel,
        string detailsMode)
    {
        var leftSb = new StringBuilder();
        for (var i = 0; i < categories.Length; i++)
        {
            var s = categories[i];
            if (s.Kind == MenuNodeKind.Separator)
            {
                leftSb.AppendLine("[dim]────────────────────────────[/]");
                continue;
            }
            if (i == leftSel) leftSb.AppendLine(midActive ? $"[cyan bold]> {s.Label.EscapeMarkup()}[/]" : $"[green bold]> {s.Label.EscapeMarkup()}[/]");
            else leftSb.AppendLine($"  {s.Label.EscapeMarkup()}");
        }

        var category = categories[leftSel];
        var midSb = new StringBuilder();
        for (var i = 0; i < visibleItems.Count; i++)
        {
            var item = visibleItems[i];
            var display = item.Label;
            
            // Check if group is expanded
            if (item.Kind == MenuNodeKind.Group)
            {
                var isExpanded = _expandedGroups.Contains(item.Id);
                var arrow = isExpanded ? "▼" : "▶";
                display = $"[bold cyan]{arrow} {item.Label.Trim().EscapeMarkup()}[/]";
            }
            else if (item.Command == null)
            {
                // Nested item indentation
                display = $"  {display.EscapeMarkup()}";
            }

            midSb.AppendLine(midActive && i == midSel ? $"[green bold]> {display}[/]" : $"  {display}");
        }

        if (visibleItems.Count == 0) midSb.AppendLine("[dim]  (press Enter to select)[/]");

        var sectionTitle = category.Label.TrimStart('>', ' ');
        IRenderable detailsContent;

        if (midActive && midSel < visibleItems.Count)
        {
            var item = visibleItems[midSel];
            var display = item.Label;
            var alias = item.Command?.Alias ?? item.Id;

            var widget = StatusWidgetRegistry.GetByAlias(alias);
            if (widget != null)
            {
                detailsContent = widget.Render();
            }
            else if (string.Equals(alias, "agyswitch", StringComparison.OrdinalIgnoreCase) && detailsActive && detailsMode == "agyswitch")
            {
                var rightSb = new StringBuilder();
                rightSb.AppendLine($"[bold white]{display.EscapeMarkup()}[/]");
                rightSb.AppendLine($"[dim]alias:[/] [yellow]{alias.EscapeMarkup()}[/]");
                rightSb.AppendLine();
                rightSb.AppendLine("[cyan bold]Select Account to Switch:[/]");
                rightSb.AppendLine();
                var accs = AgyAccountCore.GetAccounts();
                var activeAcc = AgyAccountCore.GetActiveAccount();
                for (var i = 0; i < accs.Length; i++)
                {
                    var isSelected = (i == detailsSel);
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
                    rightSb.AppendLine($"{prefix}{displayName.EscapeMarkup()} [dim]({loginStatus})[/]{suffix}");
                }
                rightSb.AppendLine();
                rightSb.AppendLine("[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc Cancel[/]");
                rightSb.AppendLine("[dim]a Create Account  ·  d Delete  ·  o Log Out[/]");
                detailsContent = new Markup(rightSb.ToString());
            }
            else if (string.Equals(alias, "theme", StringComparison.OrdinalIgnoreCase) && detailsActive && detailsMode == "theme")
            {
                var rightSb = new StringBuilder();
                rightSb.AppendLine($"[bold white]{display.EscapeMarkup()}[/]");
                rightSb.AppendLine($"[dim]alias:[/] [yellow]{alias.EscapeMarkup()}[/]");
                rightSb.AppendLine();
                rightSb.AppendLine("[cyan bold]Select Oh My Posh Theme (Color segment preview):[/]");
                rightSb.AppendLine();
                var themeNames = GetThemeNames();
                var currentTheme = Environment.GetEnvironmentVariable("THEME");
                for (var i = 0; i < themeNames.Length; i++)
                {
                    var isSelected = (i == detailsSel);
                    var isActive = (themeNames[i] == currentTheme);
                    var prefix = isSelected ? "[green bold]> [/]" : "  ";
                    var suffix = isActive ? " [green](Active)[/]" : "";
                    rightSb.AppendLine($"{prefix}{themeNames[i].EscapeMarkup()}{suffix}");
                }
                rightSb.AppendLine();
                rightSb.AppendLine("[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc Cancel[/]");
                detailsContent = new Markup(rightSb.ToString());
            }
            else if ((string.Equals(alias, "learn", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(alias, "session", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(alias, "weak", StringComparison.OrdinalIgnoreCase)) && detailsActive && detailsMode == alias.ToLowerInvariant())
            {
                var rightSb = new StringBuilder();
                rightSb.AppendLine($"[bold white]{display.EscapeMarkup()}[/]");
                rightSb.AppendLine($"[dim]alias:[/] [yellow]{alias.EscapeMarkup()}[/]");
                rightSb.AppendLine();
                rightSb.AppendLine($"[cyan bold]Select Topic for {alias.EscapeMarkup()}:[/]");
                rightSb.AppendLine();
                var topics = new[] { "jp (Japanese / Language)", "en (English Vocabulary)", "cs (C# Quiz)", "dsa (Data Structures & Algorithms)", "interview (Question Bank & STAR)", "[Type Custom Topic...]" };
                for (var i = 0; i < topics.Length; i++)
                {
                    var isSelected = (i == detailsSel);
                    var prefix = isSelected ? "[green bold]> [/]" : "  ";
                    rightSb.AppendLine($"{prefix}{topics[i].EscapeMarkup()}");
                }
                rightSb.AppendLine();
                rightSb.AppendLine("[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc Cancel[/]");
                detailsContent = new Markup(rightSb.ToString());
            }
            else if (string.Equals(alias, "proj", StringComparison.OrdinalIgnoreCase) && detailsActive && detailsMode == "proj")
            {
                var rightSb = new StringBuilder();
                rightSb.AppendLine($"[bold white]{display.EscapeMarkup()}[/]");
                rightSb.AppendLine($"[dim]alias:[/] [yellow]{alias.EscapeMarkup()}[/]");
                rightSb.AppendLine();
                rightSb.AppendLine("[cyan bold]Select Workspace directory to switch to on exit:[/]");
                rightSb.AppendLine();
                var workspaces = WorkspaceRegistry.GetWorkspaces();
                for (var i = 0; i < workspaces.Length; i++)
                {
                    var isSelected = (i == detailsSel);
                    var prefix = isSelected ? "[green bold]> [/]" : "  ";
                    rightSb.AppendLine($"{prefix}{workspaces[i].Name.EscapeMarkup()} [dim]({workspaces[i].WorkspacePath.EscapeMarkup()})[/]");
                }
                rightSb.AppendLine();
                rightSb.AppendLine("[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc Cancel[/]");
                detailsContent = new Markup(rightSb.ToString());
            }
            else
            {
                var rightSb = new StringBuilder();
                rightSb.AppendLine($"[bold white]{display.EscapeMarkup()}[/]");
                rightSb.AppendLine($"[dim]alias:[/] [yellow]{alias.EscapeMarkup()}[/]");
                
                var cmd = CommandPalette.Commands.FirstOrDefault(c => string.Equals(c.Alias, alias, StringComparison.OrdinalIgnoreCase));
                if (cmd != null)
                {
                    rightSb.AppendLine();
                    rightSb.AppendLine($"[dim]{cmd.Description.EscapeMarkup()}[/]");
                    rightSb.AppendLine();
                    rightSb.AppendLine($"[dim]Category: {cmd.Category.EscapeMarkup()}[/]");
                }
                detailsContent = new Markup(rightSb.ToString());
            }
        }
        else
        {
            var rightSb = new StringBuilder();
            rightSb.AppendLine($"[bold cyan]{sectionTitle.EscapeMarkup()}[/]");
            rightSb.AppendLine();
            if (category.Kind == MenuNodeKind.Category && leftSel < categories.Length)
            {
                // Provide some helpful description
                rightSb.AppendLine("[dim]Select an option to view details or execute command.[/]");
            }
            rightSb.AppendLine();
            rightSb.AppendLine("[dim]Press → or Enter to browse options[/]");
            detailsContent = new Markup(rightSb.ToString());
        }

        var leftPanel = new Panel(leftSb.ToString())
        {
            Header = new PanelHeader("[bold cyan]Menu[/]"), Border = BoxBorder.Rounded, BorderStyle = new Style(!midActive ? Color.Cyan1 : Color.Grey)
        };
        var midPanel = new Panel(midSb.ToString())
        {
            Header = new PanelHeader("[bold cyan]Options[/]"), Border = BoxBorder.Rounded, BorderStyle = new Style(midActive && !detailsActive ? Color.Cyan1 : Color.Grey)
        };
        var rightPanel = new Panel(detailsContent)
        {
            Header = new PanelHeader("[bold cyan]Details[/]"), Border = BoxBorder.Rounded, BorderStyle = new Style(detailsActive ? Color.Cyan1 : Color.Grey)
        };

        var table = new Table().NoBorder().HideHeaders();
        table.AddColumn(new TableColumn("").Width(30));
        table.AddColumn(new TableColumn("").Width(30));
        table.AddColumn(new TableColumn(""));
        table.AddRow(leftPanel, midPanel, rightPanel);
        AnsiConsole.Write(table);
    }
}
