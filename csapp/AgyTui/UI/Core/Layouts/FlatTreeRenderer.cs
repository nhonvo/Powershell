namespace AgyTui.UI.Core.Layouts;

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

    public override void Run(MenuNode root)
    {
        var selectionIndex = 0;
        var searching = false;
        var searchBuffer = "";
        var lastVisibleRowsCount = 0;
        var doubleSlashNavigated = false;

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
                    if (!string.IsNullOrEmpty(n.Label) && SystemHelper.IsFuzzyMatch(n.Label, rawQ)) return true;
                    if (!string.IsNullOrEmpty(n.SearchKey) && SystemHelper.IsFuzzyMatch(n.SearchKey, rawQ)) return true;
                    if (n.Command != null)
                    {
                        if (!string.IsNullOrEmpty(n.Command.Alias) && SystemHelper.IsFuzzyMatch(n.Command.Alias, rawQ)) return true;
                        if (!string.IsNullOrEmpty(n.Command.DisplayName) && SystemHelper.IsFuzzyMatch(n.Command.DisplayName, rawQ)) return true;
                        if (!string.IsNullOrEmpty(n.Command.Description) && SystemHelper.IsFuzzyMatch(n.Command.Description, rawQ)) return true;
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

            bool forceClear = (visibleRows.Count < lastVisibleRowsCount);
            lastVisibleRowsCount = visibleRows.Count;

            ScreenChrome.RenderFrame(() =>
            {
                RenderTree(visibleRows, selectionIndex, searching, searchBuffer, doubleSlashNavigated);
            }, forceClear: forceClear);

            ScreenChrome.EnableMouseTracking();
            var (key, isScrollUp, isScrollDown) = ScreenChrome.ReadKeyWithMouse();

            if (isScrollUp)
            {
                selectionIndex = Math.Max(0, selectionIndex - 3);
                doubleSlashNavigated = true;
                continue;
            }
            if (isScrollDown)
            {
                selectionIndex = Math.Min(visibleRows.Count - 1, selectionIndex + 3);
                doubleSlashNavigated = true;
                continue;
            }

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
                    doubleSlashNavigated = true;
                    continue;
                }
                else if (key.Key == ConsoleKey.UpArrow)
                {
                    searching = false;
                    selectionIndex = Math.Max(0, selectionIndex - 1);
                    doubleSlashNavigated = true;
                    continue;
                }
                else if (key.Key == ConsoleKey.PageDown)
                {
                    searching = false;
                    int pageStep = ScrollableListView.GetPageStep(Math.Max(3, Console.WindowHeight - 19));
                    selectionIndex = Math.Min(visibleRows.Count - 1, selectionIndex + pageStep);
                    doubleSlashNavigated = true;
                    continue;
                }
                else if (key.Key == ConsoleKey.PageUp)
                {
                    searching = false;
                    int pageStep = ScrollableListView.GetPageStep(Math.Max(3, Console.WindowHeight - 19));
                    selectionIndex = Math.Max(0, selectionIndex - pageStep);
                    doubleSlashNavigated = true;
                    continue;
                }
                else if (key.Key == ConsoleKey.Backspace || (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key == ConsoleKey.W))
                {
                    doubleSlashNavigated = false;
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

                    if (string.IsNullOrEmpty(searchBuffer))
                    {
                        searching = false;
                    }
                }
                else if (key.KeyChar >= 32 && key.KeyChar <= 126)
                {
                    searchBuffer += key.KeyChar;
                    doubleSlashNavigated = false;
                }
                selectionIndex = 0;
                continue;
            }


            // Normal mode keys
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.K:
                    if (visibleRows.Count > 0)
                    {
                        selectionIndex = (selectionIndex - 1 + visibleRows.Count) % visibleRows.Count;
                    }
                    doubleSlashNavigated = true;
                    break;
                case ConsoleKey.DownArrow:
                case ConsoleKey.J:
                    if (visibleRows.Count > 0)
                    {
                        selectionIndex = (selectionIndex + 1) % visibleRows.Count;
                    }
                    doubleSlashNavigated = true;
                    break;
                case ConsoleKey.PageUp:
                    selectionIndex = Math.Max(0, selectionIndex - ScrollableListView.GetPageStep(Math.Max(3, Console.WindowHeight - 19)));
                    doubleSlashNavigated = true;
                    break;
                case ConsoleKey.PageDown:
                    selectionIndex = Math.Min(visibleRows.Count - 1, selectionIndex + ScrollableListView.GetPageStep(Math.Max(3, Console.WindowHeight - 19)));
                    doubleSlashNavigated = true;
                    break;
                case ConsoleKey.Home:
                    selectionIndex = 0;
                    doubleSlashNavigated = true;
                    break;
                case ConsoleKey.End:
                    selectionIndex = Math.Max(0, visibleRows.Count - 1);
                    doubleSlashNavigated = true;
                    break;
                case ConsoleKey.C:
                case ConsoleKey.Y:
                    if (selectionIndex >= 0 && selectionIndex < visibleRows.Count)
                    {
                        var row = visibleRows[selectionIndex];
                        string copyText = "";
                        if (row.Type == VisibleRowType.Command && row.Node.Command != null)
                        {
                            copyText = $"/{row.Node.Command.Alias}";
                        }
                        else if (row.Type == VisibleRowType.Category || row.Type == VisibleRowType.Group)
                        {
                            copyText = row.Node.Label;
                        }

                        if (!string.IsNullOrEmpty(copyText))
                        {
                            ScreenChrome.CopyToClipboard(copyText);
                        }
                    }
                    break;
                case ConsoleKey.Divide:
                case ConsoleKey.Oem2:
                    searching = true;
                    doubleSlashNavigated = false;
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
                            else if (string.Equals(alias, "agyswitch", StringComparison.OrdinalIgnoreCase) ||
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
                        else if (row.Type == VisibleRowType.Command)
                        {
                            // Search upwards to find closest parent Group or Category
                            for (int i = selectionIndex - 1; i >= 0; i--)
                            {
                                var p = visibleRows[i];
                                if (p.Type == VisibleRowType.Group)
                                {
                                    _expandedGroups.Remove(p.Node.Id);
                                    selectionIndex = i;
                                    break;
                                }
                                else if (p.Type == VisibleRowType.Category)
                                {
                                    _expandedCategories.Remove(p.Node.Id);
                                    selectionIndex = i;
                                    break;
                                }
                            }
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
                        searchBuffer = key.KeyChar.ToString();
                        selectionIndex = 0;
                        doubleSlashNavigated = false;
                    }
                    break;
            }
        }
    }

    private void RenderTree(List<VisibleRow> rows, int selIdx, bool searching, string searchBuffer, bool doubleSlashNavigated)
    {
        int winWidth = 80;
        try { winWidth = Console.WindowWidth; } catch { }
        int winHeight = 30;
        try { winHeight = Console.WindowHeight; } catch { }

        var grid = new Grid();
        grid.AddColumn(new GridColumn().Width(winWidth).NoWrap());

        var isCompact = Config.IsMobileContext();
        int chromeOverhead = 5;
        int maxRows = Math.Max(5, winHeight - chromeOverhead);
        int topRow = 0;
        int endRow = 0;

        if (rows.Count == 0)
        {
            grid.AddRow(new Markup($"  [dim]No matching commands found for '{searchBuffer.EscapeMarkup()}'. Press Esc to clear.[/]"));
        }
        else
        {
            (topRow, endRow) = ScrollableListView.ComputeViewport(rows.Count, selIdx, maxRows);

            for (int i = topRow; i < endRow; i++)
            {
                var row = rows[i];
                var isSelected = (i == selIdx);
                var prefix = isSelected ? $"[{AgyThemeColors.Selected} bold]> [/]" : "  ";

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
                    var exitMarkup = isSelected
                        ? $"[{AgyThemeColors.Selected} bold]> {row.Node.Label.EscapeMarkup()}[/]"
                        : $"  {row.Node.Label.EscapeMarkup()}";
                    grid.AddRow(new Markup(exitMarkup));
                    continue;
                }

                var rawQ = searching ? searchBuffer.TrimStart('/').Trim() : "";

                if (row.Type == VisibleRowType.Category)
                {
                    var isExpanded = _expandedCategories.Contains(row.Node.Id) || !string.IsNullOrEmpty(searchBuffer);
                    var sign = isExpanded ? "-" : "+";
                    var catIcon = Icons.GetCategoryIcon(row.Node.Label);
                    var hk = Icons.GetCategoryHotkey(row.Node.Label);

                    var rawCatLabel = row.Node.Label.Trim();
                    var cleanCatLabel = rawCatLabel.StartsWith('[') && rawCatLabel.EndsWith(']') ? rawCatLabel[1..^1] : rawCatLabel;
                    var boldText = string.IsNullOrEmpty(rawQ) ? cleanCatLabel.EscapeMarkup() : SystemHelper.BoldFuzzyMatch(cleanCatLabel, rawQ);

                    string lineMarkup;
                    if (isSelected)
                    {
                        lineMarkup = $"[{AgyThemeColors.Selected} bold]> [[{sign}]] {catIcon} {boldText}[/]";
                        if (!string.IsNullOrEmpty(hk)) lineMarkup += $" [dim]({hk.EscapeMarkup()})[/]";
                    }
                    else
                    {
                        lineMarkup = $"  [bold {AgyThemeColors.Secondary}][[{sign}]][/] [bold {AgyThemeColors.Accent}]{catIcon} {boldText}[/]";
                        if (!string.IsNullOrEmpty(hk)) lineMarkup += $" [dim]({hk.EscapeMarkup()})[/]";
                    }
                    grid.AddRow(new Markup(lineMarkup));
                }
                else if (row.Type == VisibleRowType.Group)
                {
                    var isExpanded = _expandedGroups.Contains(row.Node.Id) || !string.IsNullOrEmpty(searchBuffer);
                    var sign = isExpanded ? "-" : "+";
                    var rawLabel = row.Node.Label.Trim();
                    var cleanLabelRaw = System.Text.RegularExpressions.Regex.Replace(rawLabel, @"^\[/[^\]]+\]\s*", "");
                    if (cleanLabelRaw.StartsWith('[') && cleanLabelRaw.EndsWith(']'))
                    {
                        cleanLabelRaw = cleanLabelRaw[1..^1];
                    }
                    var boldText = string.IsNullOrEmpty(rawQ) ? cleanLabelRaw.EscapeMarkup() : SystemHelper.BoldFuzzyMatch(cleanLabelRaw, rawQ);

                    string lineMarkup;
                    if (isSelected)
                    {
                        lineMarkup = $"[{AgyThemeColors.Selected} bold]> {treePrefix.EscapeMarkup()}[[{sign}]] 📂 {boldText}[/]";
                    }
                    else
                    {
                        lineMarkup = $"  [dim]{treePrefix.EscapeMarkup()}[/][bold {AgyThemeColors.Secondary}][[{sign}]][/] [bold {AgyThemeColors.Secondary}]📂 {boldText}[/]";
                    }
                    grid.AddRow(new Markup(lineMarkup));
                }
                else if (row.Type == VisibleRowType.Command)
                {
                    var cmd = row.Node.Command!;
                    var icon = Icons.GetCommandIcon(cmd.Alias, cmd.Category);

                    var boldAlias = string.IsNullOrEmpty(rawQ) ? cmd.Alias.EscapeMarkup() : SystemHelper.BoldFuzzyMatch(cmd.Alias, rawQ);
                    var boldDisplayName = string.IsNullOrEmpty(rawQ) ? cmd.DisplayName.EscapeMarkup() : SystemHelper.BoldFuzzyMatch(cmd.DisplayName, rawQ);

                    var displayLabel = $"/{boldAlias} — {boldDisplayName}";

                    string lineMarkup;
                    if (isSelected)
                    {
                        lineMarkup = $"[{AgyThemeColors.Selected} bold]> {treePrefix.EscapeMarkup()}{icon} {displayLabel}[/]";
                    }
                    else
                    {
                        lineMarkup = $"  [dim]{treePrefix.EscapeMarkup()}[/]{icon} [white]{displayLabel}[/]";
                    }
                    grid.AddRow(new Markup(lineMarkup));
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
                            BorderStyle = new Style(isSelected ? AgyThemeColors.GetSelectedColor() : AgyThemeColors.GetBorderColor()),
                            Header = new PanelHeader($"[bold {AgyThemeColors.Accent}]{alias} status[/]")
                        };

                        var indentGrid = new Grid();
                        indentGrid.AddColumn(new GridColumn().Width(row.Indent * 3));
                        indentGrid.AddColumn(new GridColumn());
                        indentGrid.AddRow(new Markup(""), indentPanel);

                        grid.AddRow(indentGrid);
                    }
                }
            }
        }

        string noteText = "";
        if (selIdx >= 0 && selIdx < rows.Count)
        {
            var highlighted = rows[selIdx];
            if (highlighted.Type == VisibleRowType.Command && highlighted.Node.Command != null)
            {
                var cmd = highlighted.Node.Command;
                var maxNoteLen = Math.Max(30, winWidth - 18);
                var cleanDesc = !string.IsNullOrWhiteSpace(cmd.Description)
                    ? cmd.Description
                    : (cmd.HelpLines != null && cmd.HelpLines.Length > 0 ? cmd.HelpLines[0] : "");
                noteText = $"/{cmd.Alias} — {cleanDesc}";
                if (noteText.Length > maxNoteLen) noteText = noteText[..maxNoteLen] + "…";
            }
            else if (highlighted.Type == VisibleRowType.Category)
            {
                var catNote = $"Category '{highlighted.Node.Label}' — Press [Enter] or [→] to expand/collapse.";
                var maxNoteLen = Math.Max(30, winWidth - 18);
                if (catNote.Length > maxNoteLen) catNote = catNote[..maxNoteLen] + "…";
                noteText = catNote;
            }
            else if (highlighted.Type == VisibleRowType.Group)
            {
                var grpNote = $"Group '{highlighted.Node.Label}' — Press [Enter] or [→] to expand/collapse.";
                var maxNoteLen = Math.Max(30, winWidth - 18);
                if (grpNote.Length > maxNoteLen) grpNote = grpNote[..maxNoteLen] + "…";
                noteText = grpNote;
            }
            else if (highlighted.Type == VisibleRowType.Exit)
            {
                noteText = "Press [Enter] to exit the Control Center.";
            }
            else
            {
                noteText = "Press [Enter] to select option.";
            }
        }
        else
        {
            noteText = "Use [↑/↓] or [j/k] to navigate commands.";
        }

        var filterRenderable = AgyUiComponents.RenderFilter(searchBuffer, searching);
        var scrollRenderable = AgyUiComponents.RenderScrollIndicator(rows.Count, topRow, endRow, maxRows);
        var noteRenderable = AgyUiComponents.RenderFooterNote(noteText, Math.Max(30, winWidth - 12));

        var statusGrid = new Grid();
        int col1Width = Math.Max(20, winWidth / 2);
        int col2Width = Math.Max(20, winWidth - col1Width);
        statusGrid.AddColumn(new GridColumn().Width(col1Width));
        statusGrid.AddColumn(new GridColumn().Width(col2Width).RightAligned());
        statusGrid.AddRow(filterRenderable, scrollRenderable);

        var keysText = "TUI Keys: [[↑/↓ j/k]] Nav · [[PgUp/PgDn]] Scroll · [[/]] Filter · [[c/y]] Copy · [[Enter/→]] Select · [[Esc/q]] Exit";
        var rawKeys = "TUI Keys: [↑/↓ j/k] Nav · [PgUp/PgDn] Scroll · [/] Filter · [c/y] Copy · [Enter/→] Select · [Esc/q] Exit";
        if (rawKeys.Length > winWidth - 1)
        {
            keysText = "TUI Keys: [[↑/↓ j/k]] Nav · [[/]] Filter · [[c/y]] Copy · [[Enter]] Select · [[Esc]] Exit";
            var rawShortKeys = "TUI Keys: [↑/↓ j/k] Nav · [/] Filter · [c/y] Copy · [Enter] Select · [Esc] Exit";
            if (rawShortKeys.Length > winWidth - 1) keysText = rawShortKeys[..(winWidth - 1)].EscapeMarkup();
        }

        var aliasText = "Shell Aliases: cg (Git) · cdk (Docker) · cnav (Nav) · cai (AI) · csys (Sys) · cnet (Net) · cssh (SSH)";
        if (aliasText.Length > winWidth - 1)
        {
            aliasText = aliasText[..(winWidth - 1)];
        }

        var hotkeyBar = new Markup($"[dim]{keysText}\n{aliasText.EscapeMarkup()}[/]");

        var layout = new Rows(
            grid,
            statusGrid,
            noteRenderable,
            hotkeyBar
        );

        ScreenChrome.WriteSmooth(layout);
    }
}
