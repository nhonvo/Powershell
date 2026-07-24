using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace AgyTui;

public static class SubPageNavigator
{
    private static string _detailsSearchBuffer = "";

    public static void Run(string mode, string initialQuery = "")
    {
        mode = mode.ToLowerInvariant();
        int detailsSel = 0;

        if (mode == "agyswitch")
        {
            var accs = AgyAccountCore.GetAccounts();
            var activeAcc = AgyAccountCore.GetActiveAccount();
            detailsSel = Array.IndexOf(accs, activeAcc);
            if (detailsSel < 0) detailsSel = 0;
        }
        else if (mode == "theme")
        {
            var themeFiles = SubPageThemeNavigator.GetThemeNames();
            var currentTheme = Environment.GetEnvironmentVariable("THEME");
            detailsSel = Array.IndexOf(themeFiles, currentTheme);
            if (detailsSel < 0) detailsSel = 0;
        }
        else if (mode == "proj")
        {
            SubPageProjNavigator.SelectedWorkspaceIndex = 0;
            SubPageProjNavigator.SelectedActionIndex = -1;
            SubPageProjNavigator.ExpandedWorkspaceIndex = -1;
        }

        _detailsSearchBuffer = initialQuery;

        while (true)
        {
            int itemsCount = 0;
            var flatList = new List<SubPageProjNavigator.FlatItem>();
            var workspaces = Array.Empty<WorkspaceEntry>();

            if (mode == "agyswitch")
            {
                var accs = AgyAccountCore.GetAccounts();
                if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                {
                    accs = accs.Where(a => a.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
                }
                itemsCount = accs.Length;
            }
            else if (mode == "theme")
            {
                var themes = SubPageThemeNavigator.GetThemeNames();
                if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                {
                    themes = themes.Where(t => t.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
                }
                itemsCount = themes.Length;
            }
            else if (mode == "learn" || mode == "session" || mode == "weak")
            {
                var topics = new[] { "jp", "en", "cs", "dsa", "interview", "[Type Custom Topic...]" };
                if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                {
                    topics = topics.Where(t => t.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
                }
                itemsCount = topics.Length;
            }
            else if (mode == "proj")
            {
                workspaces = WorkspaceRegistry.GetWorkspaces();
                if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                {
                    workspaces = workspaces.Where(w => w != null && 
                        ((w.Name != null && SystemHelper.IsFuzzyMatch(w.Name, _detailsSearchBuffer)) ||
                         (w.WorkspacePath != null && SystemHelper.IsFuzzyMatch(w.WorkspacePath, _detailsSearchBuffer)))).ToArray();
                }
                
                flatList = SubPageProjNavigator.GetFlatList(workspaces);
                itemsCount = flatList.Count;
                
                detailsSel = flatList.FindIndex(item => item.WorkspaceIndex == SubPageProjNavigator.SelectedWorkspaceIndex && item.ActionIndex == SubPageProjNavigator.SelectedActionIndex);
                if (detailsSel < 0)
                {
                    if (flatList.Count > 0)
                    {
                        detailsSel = 0;
                        SubPageProjNavigator.SelectedWorkspaceIndex = flatList[0].WorkspaceIndex;
                        SubPageProjNavigator.SelectedActionIndex = flatList[0].ActionIndex;
                    }
                    else
                    {
                        detailsSel = 0;
                        SubPageProjNavigator.SelectedWorkspaceIndex = 0;
                        SubPageProjNavigator.SelectedActionIndex = -1;
                    }
                }
            }

            ScreenChrome.RenderFrame(() =>
            {
                RenderSubPageSelection(mode, detailsSel, workspaces, flatList);
            }, forceClear: true);

            var key = Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    if (itemsCount > 0) detailsSel = (detailsSel - 1 + itemsCount) % itemsCount;
                    break;
                case ConsoleKey.K:
                    if (string.IsNullOrEmpty(_detailsSearchBuffer))
                    {
                        if (itemsCount > 0) detailsSel = (detailsSel - 1 + itemsCount) % itemsCount;
                    }
                    else
                    {
                        _detailsSearchBuffer += key.KeyChar;
                        detailsSel = 0;
                        if (mode == "proj")
                        {
                            SubPageProjNavigator.SelectedWorkspaceIndex = 0;
                            SubPageProjNavigator.SelectedActionIndex = -1;
                            SubPageProjNavigator.ExpandedWorkspaceIndex = -1;
                        }
                    }
                    break;
                case ConsoleKey.DownArrow:
                    if (itemsCount > 0) detailsSel = (detailsSel + 1) % itemsCount;
                    break;
                case ConsoleKey.J:
                    if (string.IsNullOrEmpty(_detailsSearchBuffer))
                    {
                        if (itemsCount > 0) detailsSel = (detailsSel + 1) % itemsCount;
                    }
                    else
                    {
                        _detailsSearchBuffer += key.KeyChar;
                        detailsSel = 0;
                        if (mode == "proj")
                        {
                            SubPageProjNavigator.SelectedWorkspaceIndex = 0;
                            SubPageProjNavigator.SelectedActionIndex = -1;
                            SubPageProjNavigator.ExpandedWorkspaceIndex = -1;
                        }
                    }
                    break;
                case ConsoleKey.PageUp:
                    if (itemsCount > 0) detailsSel = Math.Max(0, detailsSel - ScrollableListView.GetPageStep(Math.Max(3, Console.WindowHeight - 19)));
                    break;
                case ConsoleKey.PageDown:
                    if (itemsCount > 0) detailsSel = Math.Min(itemsCount - 1, detailsSel + ScrollableListView.GetPageStep(Math.Max(3, Console.WindowHeight - 19)));
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
                        if (mode == "proj")
                        {
                            SubPageProjNavigator.SelectedWorkspaceIndex = 0;
                            SubPageProjNavigator.SelectedActionIndex = -1;
                            SubPageProjNavigator.ExpandedWorkspaceIndex = -1;
                        }
                    }
                    break;
                case ConsoleKey.Enter:
                    if (mode == "proj")
                    {
                        bool shouldExit = SubPageProjNavigator.HandleEnter(workspaces, flatList, detailsSel);
                        if (shouldExit) return;
                    }
                    else if (mode == "theme")
                    {
                        bool shouldExit = SubPageThemeNavigator.HandleSelection(_detailsSearchBuffer, detailsSel);
                        if (shouldExit) return;
                    }
                    else if (mode == "agyswitch")
                    {
                        bool shouldExit = SubPageAccountNavigator.HandleSelection(_detailsSearchBuffer, detailsSel);
                        if (shouldExit) return;
                    }
                    else if (mode == "learn" || mode == "session" || mode == "weak")
                    {
                        bool shouldExit = SubPageTopicNavigator.HandleSelection(mode, _detailsSearchBuffer, detailsSel);
                        if (shouldExit) return;
                    }
                    break;
                case ConsoleKey.RightArrow:
                case ConsoleKey.L:
                    if (mode == "proj" && flatList.Count > 0)
                    {
                        if (detailsSel >= 0 && detailsSel < flatList.Count)
                        {
                            var item = flatList[detailsSel];
                            if (item.ActionIndex == -1)
                            {
                                SubPageProjNavigator.ExpandedWorkspaceIndex = item.WorkspaceIndex;
                            }
                        }
                    }
                    break;
                case ConsoleKey.LeftArrow:
                case ConsoleKey.H:
                    if (mode == "proj" && flatList.Count > 0)
                    {
                        if (detailsSel >= 0 && detailsSel < flatList.Count)
                        {
                            var item = flatList[detailsSel];
                            if (item.ActionIndex == -1)
                            {
                                if (SubPageProjNavigator.ExpandedWorkspaceIndex == item.WorkspaceIndex)
                                {
                                    SubPageProjNavigator.ExpandedWorkspaceIndex = -1;
                                }
                            }
                            else
                            {
                                SubPageProjNavigator.ExpandedWorkspaceIndex = -1;
                                SubPageProjNavigator.SelectedActionIndex = -1;
                                SubPageProjNavigator.SelectedWorkspaceIndex = item.WorkspaceIndex;
                            }
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                        {
                            _detailsSearchBuffer = "";
                            detailsSel = 0;
                        }
                        else
                        {
                            return;
                        }
                    }
                    break;
                case ConsoleKey.A:
                    if (mode == "agyswitch")
                    {
                        SubPageAccountNavigator.CreateAccount();
                    }
                    else
                    {
                        _detailsSearchBuffer += key.KeyChar;
                        detailsSel = 0;
                        if (mode == "proj")
                        {
                            SubPageProjNavigator.SelectedWorkspaceIndex = 0;
                            SubPageProjNavigator.SelectedActionIndex = -1;
                            SubPageProjNavigator.ExpandedWorkspaceIndex = -1;
                        }
                    }
                    break;
                case ConsoleKey.D:
                    if (mode == "agyswitch")
                    {
                        SubPageAccountNavigator.DeleteAccount(_detailsSearchBuffer, detailsSel);
                    }
                    else
                    {
                        _detailsSearchBuffer += key.KeyChar;
                        detailsSel = 0;
                        if (mode == "proj")
                        {
                            SubPageProjNavigator.SelectedWorkspaceIndex = 0;
                            SubPageProjNavigator.SelectedActionIndex = -1;
                            SubPageProjNavigator.ExpandedWorkspaceIndex = -1;
                        }
                    }
                    break;
                case ConsoleKey.O:
                    if (mode == "agyswitch")
                    {
                        SubPageAccountNavigator.LogoutAccount(_detailsSearchBuffer, detailsSel);
                    }
                    else
                    {
                        _detailsSearchBuffer += key.KeyChar;
                        detailsSel = 0;
                        if (mode == "proj")
                        {
                            SubPageProjNavigator.SelectedWorkspaceIndex = 0;
                            SubPageProjNavigator.SelectedActionIndex = -1;
                            SubPageProjNavigator.ExpandedWorkspaceIndex = -1;
                        }
                    }
                    break;
                case ConsoleKey.Escape:
                case ConsoleKey.Q:
                    if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                    {
                        _detailsSearchBuffer = "";
                        detailsSel = 0;
                        if (mode == "proj")
                        {
                            SubPageProjNavigator.SelectedWorkspaceIndex = 0;
                            SubPageProjNavigator.SelectedActionIndex = -1;
                            SubPageProjNavigator.ExpandedWorkspaceIndex = -1;
                        }
                    }
                    else
                    {
                        return;
                    }
                    break;
                default:
                    if (key.KeyChar >= 32 && key.KeyChar <= 126 && key.Key != ConsoleKey.Enter)
                    {
                        if (mode == "agyswitch" && (key.Key == ConsoleKey.A || key.Key == ConsoleKey.D || key.Key == ConsoleKey.O) && string.IsNullOrEmpty(_detailsSearchBuffer))
                        {
                            break;
                        }
                        _detailsSearchBuffer += key.KeyChar;
                        detailsSel = 0;
                        if (mode == "proj")
                        {
                            SubPageProjNavigator.SelectedWorkspaceIndex = 0;
                            SubPageProjNavigator.SelectedActionIndex = -1;
                            SubPageProjNavigator.ExpandedWorkspaceIndex = -1;
                        }
                    }
                    break;
            }

            if (mode == "proj" && itemsCount > 0)
            {
                if (detailsSel < 0) detailsSel = 0;
                if (detailsSel >= flatList.Count) detailsSel = flatList.Count - 1;
                
                if (flatList.Count > 0)
                {
                    var selectedItem = flatList[detailsSel];
                    SubPageProjNavigator.SelectedWorkspaceIndex = selectedItem.WorkspaceIndex;
                    SubPageProjNavigator.SelectedActionIndex = selectedItem.ActionIndex;
                }
            }
        }
    }

    private static string DeletePreviousWord(string buffer)
    {
        if (string.IsNullOrEmpty(buffer)) return "";
        int i = buffer.Length - 1;
        while (i >= 0 && char.IsWhiteSpace(buffer[i])) i--;
        while (i >= 0 && !char.IsWhiteSpace(buffer[i])) i--;
        return buffer[..(i + 1)];
    }

    private static void RenderSubPageSelection(string mode, int selIdx, WorkspaceEntry[] workspaces, List<SubPageProjNavigator.FlatItem> flatList)
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn().NoWrap());

        IRenderable content = grid;

        if (mode == "agyswitch")
        {
            content = SubPageAccountNavigator.Render(grid, _detailsSearchBuffer, selIdx);
        }
        else if (mode == "theme")
        {
            content = SubPageThemeNavigator.Render(grid, _detailsSearchBuffer, selIdx);
        }
        else if (mode == "learn" || mode == "session" || mode == "weak")
        {
            content = SubPageTopicNavigator.Render(grid, mode, _detailsSearchBuffer, selIdx);
        }
        else if (mode == "proj")
        {
            var currentDir = Directory.GetCurrentDirectory();
            grid.AddRow(new Markup($"[bold green]📁 Registered Workspace Navigator (cnav)[/] [dim]({workspaces.Length} workspaces)[/]:\n"));

            if (!string.IsNullOrEmpty(_detailsSearchBuffer))
            {
                grid.AddRow(new Markup($"[yellow]Search:[/] [white]{_detailsSearchBuffer.EscapeMarkup()}[/]_\n"));
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
                content = SubPageProjNavigator.Render(grid, _detailsSearchBuffer, workspaces, flatList, selIdx, currentDir);
            }
        }

        var panel = new Panel(content)
        {
            Header = new PanelHeader($"[bold cyan] {mode.ToUpperInvariant()} Selector [/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1),
            Expand = true
        };
        ScreenChrome.WriteSmooth(panel);
    }
}
