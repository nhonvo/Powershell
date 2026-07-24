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

public sealed class ThreePaneRenderer : MenuRendererBase
{
    private readonly HashSet<string> _expandedGroups = new();

    public override void Run(MenuNode root)
    {
        var leftSel = 0;
        var midSel = 0;
        var midActive = false;

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

            if (midSel >= visibleItems.Count) midSel = Math.Max(0, visibleItems.Count - 1);

            ScreenChrome.RenderFrame(() =>
            {
                RenderPanes(categories, leftSel, visibleItems, midSel, midActive);
            });

            var key = Console.ReadKey(true);


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
                                if (string.Equals(alias, "agyswitch", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(alias, "theme", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(alias, "learn", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(alias, "session", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(alias, "weak", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(alias, "proj", StringComparison.OrdinalIgnoreCase))
                                {
                                    SubPageNavigator.Run(alias);
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

    private static bool IsSep(MenuNode[] categories, int idx) => categories[idx].Label.Length > 0 && categories[idx].Label[0] == '─';

    private void RenderPanes(
        MenuNode[] categories,
        int leftSel,
        List<MenuNode> visibleItems,
        int midSel,
        bool midActive)
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
            if (i == leftSel) leftSb.AppendLine(midActive ? $"[cyan bold]> {labelWithIcon}[/]" : $"[green bold]> {labelWithIcon}[/]");
            else leftSb.AppendLine($"  {labelWithIcon}");
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
            else

            {
                var rightSb = new StringBuilder();
                rightSb.AppendLine($"[bold white]{display.EscapeMarkup()}[/]");
                rightSb.AppendLine($"[dim]alias:[/] [yellow]{alias.EscapeMarkup()}[/]");

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
            Header = new PanelHeader("[bold cyan]Menu[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(!midActive ? Color.Cyan1 : Color.Grey)
        };
        var midPanel = new Panel(midSb.ToString())
        {
            Header = new PanelHeader("[bold cyan]Options[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(midActive ? Color.Cyan1 : Color.Grey)
        };
        var rightPanel = new Panel(detailsContent)
        {
            Header = new PanelHeader("[bold cyan]Details[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Grey)
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
