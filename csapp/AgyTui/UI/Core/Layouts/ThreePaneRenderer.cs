using Spectre.Console.Rendering;

namespace AgyTui.UI.Core.Layouts;

public sealed class ThreePaneRenderer : MenuRendererBase
{
    private readonly HashSet<string> _expandedGroups = new();

    public override void Run(MenuNode root)
    {
        var leftSel = 0;
        var midSel = 0;
        var midActive = false;
        var searchBuffer = "";

        try { Console.CursorVisible = false; } catch { }

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

            if (!string.IsNullOrEmpty(searchBuffer))
            {
                visibleItems = visibleItems.Where(item =>
                    SystemHelper.IsFuzzyMatch(item.Label, searchBuffer) ||
                    (item.Command != null && SystemHelper.IsFuzzyMatch(item.Command.Alias, searchBuffer))
                ).ToList();
            }

            if (midSel < 0) midSel = 0;
            if (visibleItems.Count > 0 && midSel >= visibleItems.Count) midSel = visibleItems.Count - 1;

            ScreenChrome.RenderFrame(() =>
            {
                RenderPanes(categories, leftSel, visibleItems, midSel, midActive, searchBuffer);
            });

            ScreenChrome.EnableMouseTracking();
            var (key, isScrollUp, isScrollDown) = ScreenChrome.ReadKeyWithMouse();

            if (isScrollUp)
            {
                if (midActive && visibleItems.Count > 0) midSel = Math.Max(0, midSel - 3);
                else if (categories.Length > 0) leftSel = Math.Max(0, leftSel - 1);
                continue;
            }
            if (isScrollDown)
            {
                if (midActive && visibleItems.Count > 0) midSel = Math.Min(visibleItems.Count - 1, midSel + 3);
                else if (categories.Length > 0) leftSel = Math.Min(categories.Length - 1, leftSel + 1);
                continue;
            }

            // Handle search buffer keys when searching
            if (!string.IsNullOrEmpty(searchBuffer))
            {
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
                    midSel = 0;
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    searchBuffer = "";
                    midSel = 0;
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    if (midSel >= 0 && midSel < visibleItems.Count)
                    {
                        var item = visibleItems[midSel];
                        if (item.Command != null)
                        {
                            var alias = item.Command.Alias;
                            if (string.Equals(alias, "agyswitch", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(alias, "theme", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(alias, "learn", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(alias, "session", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(alias, "weak", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(alias, "proj", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(alias, "cnav", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(alias, "p", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(alias, "prj", StringComparison.OrdinalIgnoreCase))
                            {
                                var targetNav = (alias is "cnav" or "p" or "prj") ? "proj" : alias;
                                SubPageNavigator.Run(targetNav);
                                if (File.Exists(Path.Combine(AppPaths.GeminiHome, "selected_project.txt")) || File.Exists(Path.Combine(AppPaths.GeminiHome, "selected_theme.txt")))
                                {
                                    return;
                                }
                            }
                            else if (StatusWidgetRegistry.GetByAlias(alias) != null)
                            {
                                // Widgets are rendered directly on the right pane, no direct execution needed on Enter
                            }
                            else
                            {
                                Console.CursorVisible = true;
                                Program.RunCommand(alias);
                                if (File.Exists(Path.Combine(AppPaths.GeminiHome, "selected_project.txt")) || File.Exists(Path.Combine(AppPaths.GeminiHome, "selected_theme.txt")))
                                {
                                    return;
                                }
                                if (string.Equals(alias, "deck-start", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(alias, "desk-start", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(alias, "deck-online", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(alias, "desk-online", StringComparison.OrdinalIgnoreCase))
                                {
                                    return;
                                }
                                AnsiConsole.WriteLine();
                                AnsiConsole.MarkupLine("[dim]Press any key to return to Control Center...[/]");
                                Console.ReadKey(true);
                                Console.CursorVisible = false;
                            }
                        }
                    }
                }
                else if (key.Key == ConsoleKey.UpArrow || (key.Key == ConsoleKey.K && key.Modifiers == 0))
                {
                    if (visibleItems.Count > 0)
                    {
                        midSel = (midSel - 1 + visibleItems.Count) % visibleItems.Count;
                    }
                }
                else if (key.Key == ConsoleKey.DownArrow || (key.Key == ConsoleKey.J && key.Modifiers == 0))
                {
                    if (visibleItems.Count > 0)
                    {
                        midSel = (midSel + 1) % visibleItems.Count;
                    }
                }
                else if (key.KeyChar >= 32 && key.KeyChar <= 126 && key.Key != ConsoleKey.Enter)
                {
                    searchBuffer += key.KeyChar;
                    midSel = 0;
                }
                continue;
            }

            // If search is empty, check for search key
            if (key.KeyChar == '/' || key.Key == ConsoleKey.Divide || key.Key == ConsoleKey.Oem2)
            {
                searchBuffer = "";
                midActive = true;
                midSel = 0;
                continue;
            }

            // Normal mode
            if (!midActive)
            {
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.K:
                        {
                            var next = leftSel;
                            do
                            {
                                next = (next - 1 + categories.Length) % categories.Length;
                            }
                            while (next != leftSel && IsSep(categories, next));
                            if (!IsSep(categories, next))
                            {
                                leftSel = next;
                                midSel = 0;
                            }
                            break;
                        }
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.J:
                        {
                            var next = leftSel;
                            do
                            {
                                next = (next + 1) % categories.Length;
                            }
                            while (next != leftSel && IsSep(categories, next));
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
                    default:
                        if (key.KeyChar >= 32 && key.KeyChar <= 126 && key.Key != ConsoleKey.Enter)
                        {
                            searchBuffer = key.KeyChar.ToString();
                            midActive = true;
                            midSel = 0;
                        }
                        break;
                }
            }
            else
            {
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.K:
                        if (visibleItems.Count > 0)
                        {
                            midSel = (midSel - 1 + visibleItems.Count) % visibleItems.Count;
                        }
                        break;
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.J:
                        if (visibleItems.Count > 0)
                        {
                            midSel = (midSel + 1) % visibleItems.Count;
                        }
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
                                if (string.Equals(alias, "agyswitch", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(alias, "theme", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(alias, "learn", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(alias, "session", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(alias, "weak", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(alias, "proj", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(alias, "cnav", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(alias, "p", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(alias, "prj", StringComparison.OrdinalIgnoreCase))
                                {
                                    var targetNav = (alias is "cnav" or "p" or "prj") ? "proj" : alias;
                                    SubPageNavigator.Run(targetNav);
                                    if (File.Exists(Path.Combine(AppPaths.GeminiHome, "selected_project.txt")) || File.Exists(Path.Combine(AppPaths.GeminiHome, "selected_theme.txt")))
                                    {
                                        return;
                                    }
                                }
                                else if (StatusWidgetRegistry.GetByAlias(alias) != null)
                                {
                                    // Widgets are rendered directly on the right pane, no direct execution needed on Enter
                                }
                                else
                                {
                                    Console.CursorVisible = true;
                                    Program.RunCommand(alias);
                                    if (string.Equals(alias, "deck-start", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(alias, "desk-start", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(alias, "deck-online", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(alias, "desk-online", StringComparison.OrdinalIgnoreCase))
                                    {
                                        return;
                                    }
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
                    default:
                        if (key.KeyChar >= 32 && key.KeyChar <= 126 && key.Key != ConsoleKey.Enter)
                        {
                            searchBuffer = key.KeyChar.ToString();
                            midActive = true;
                            midSel = 0;
                        }
                        break;
                }
            }
        }
    }

    private static bool IsSep(MenuNode[] categories, int idx) => categories[idx].Label.Length > 0 && categories[idx].Label[0] == '─';

    private void RenderPanes(
        MenuNode[] categories,
        int leftSel,
        List<MenuNode> visibleItems,
        int midSel,
        bool midActive,
        string searchBuffer)
    {
        var isCompact = Config.IsMobileContext();
        var leftSb = new StringBuilder();
        for (var i = 0; i < categories.Length; i++)
        {
            var s = categories[i];
            if (s.Kind == MenuNodeKind.Separator)
            {
                leftSb.AppendLine("[dim]────────────────────────────[/]");
                continue;
            }
            var icon = Icons.GetCategoryIcon(s.Label);
            var hk = Icons.GetCategoryHotkey(s.Label);
            var hkSuffix = string.IsNullOrEmpty(hk) ? "" : $" [dim]({hk})[/]";
            var labelWithIcon = $"{icon} {s.Label.EscapeMarkup()}{hkSuffix}";
            if (i == leftSel) leftSb.AppendLine(midActive ? $"[{AgyThemeColors.Accent} bold]> {labelWithIcon}[/]" : $"[{AgyThemeColors.Selected} bold]> {labelWithIcon}[/]");
            else leftSb.AppendLine($"  {labelWithIcon}");
        }

        var category = categories[leftSel];
        var midSb = new StringBuilder();

        if (!string.IsNullOrEmpty(searchBuffer))
        {
            midSb.AppendLine($"[{AgyThemeColors.Secondary}]Search:[/] [white]{searchBuffer.EscapeMarkup()}[/]_");
            midSb.AppendLine();
        }

        for (var i = 0; i < visibleItems.Count; i++)
        {
            var item = visibleItems[i];
            var display = item.Label;

            if (!string.IsNullOrEmpty(searchBuffer))
            {
                display = SystemHelper.BoldFuzzyMatch(display, searchBuffer);
            }
            else
            {
                display = display.EscapeMarkup();
            }

            if (item.Kind == MenuNodeKind.Group)
            {
                var isExpanded = _expandedGroups.Contains(item.Id);
                var arrow = isExpanded ? "▼" : "▶";
                display = $"[bold {AgyThemeColors.Accent}]{arrow} {display}[/]";
            }
            else if (item.Command == null)
            {
                display = $"  {display}";
            }

            midSb.AppendLine(midActive && i == midSel ? $"[{AgyThemeColors.Selected} bold]> {display}[/]" : $"  {display}");
        }

        if (visibleItems.Count == 0)
        {
            if (!string.IsNullOrEmpty(searchBuffer))
            {
                midSb.AppendLine($"[dim]  No items matching '{searchBuffer.EscapeMarkup()}'[/]");
            }
            else
            {
                midSb.AppendLine("[dim]  (press Enter to select)[/]");
            }
        }

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
            else
            {
                var rightSb = new StringBuilder();
                rightSb.AppendLine($"[bold white]{display.EscapeMarkup()}[/]");
                rightSb.AppendLine($"[dim]alias:[/] [{AgyThemeColors.Secondary}]{alias.EscapeMarkup()}[/]");

                var cmd = CommandPalette.Commands.FirstOrDefault(c => string.Equals(c.Alias, alias, StringComparison.OrdinalIgnoreCase));
                if (cmd != null && !isCompact)
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
            rightSb.AppendLine($"[bold {AgyThemeColors.Accent}]{sectionTitle.EscapeMarkup()}[/]");
            rightSb.AppendLine();
            if (category.Kind == MenuNodeKind.Category && leftSel < categories.Length)
            {
                rightSb.AppendLine("[dim]Select an option to view details or execute command.[/]");
            }
            rightSb.AppendLine();
            rightSb.AppendLine("[dim]Press → or Enter to browse options[/]");
            detailsContent = new Markup(rightSb.ToString());
        }

        var leftPanel = new Panel(leftSb.ToString())
        {
            Header = new PanelHeader($"[bold {AgyThemeColors.Accent}]Menu[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(!midActive ? AgyThemeColors.GetAccentColor() : AgyThemeColors.GetBorderColor())
        };
        var midPanel = new Panel(midSb.ToString())
        {
            Header = new PanelHeader($"[bold {AgyThemeColors.Accent}]Options[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(midActive ? AgyThemeColors.GetAccentColor() : AgyThemeColors.GetBorderColor())
        };
        var rightPanel = new Panel(detailsContent)
        {
            Header = new PanelHeader($"[bold {AgyThemeColors.Accent}]Details[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(AgyThemeColors.GetBorderColor())
        };

        int winWidth = 100;
        try { winWidth = Console.WindowWidth; } catch { }
        int menuWidth = isCompact ? Math.Max(22, (int)(winWidth * 0.28)) : Math.Max(32, (int)(winWidth * 0.30));
        int optionsWidth = isCompact ? Math.Max(25, (int)(winWidth * 0.32)) : Math.Max(35, (int)(winWidth * 0.35));

        var table = new Table().NoBorder().HideHeaders().Expand();
        table.AddColumn(new TableColumn("").Width(menuWidth).NoWrap());
        table.AddColumn(new TableColumn("").Width(optionsWidth).NoWrap());
        table.AddColumn(new TableColumn("").NoWrap());
        table.AddRow(leftPanel, midPanel, rightPanel);
        ScreenChrome.WriteSmooth(table);
    }
}
