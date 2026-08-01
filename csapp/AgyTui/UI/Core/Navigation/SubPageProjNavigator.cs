using Spectre.Console;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Core.Navigation;

public static class SubPageProjNavigator
{
    public struct FlatItem
    {
        public WorkspaceEntry Workspace;
        public int WorkspaceIndex;
        public int ActionIndex;
        public bool IsChildGroupHeader;
        public bool IsChildWorkspace;
        public int Depth;
        public int ChildCount;
    }

    public static int SelectedWorkspaceIndex = 0;
    public static int SelectedActionIndex = -1;
    public static int ExpandedWorkspaceIndex = -1;
    public static string? ExpandedChildWorkspacePath = null;
    public static string? ExpandedActionsWorkspacePath = null;

    public static List<FlatItem> GetFlatList(WorkspaceEntry[] allWorkspaces, string searchBuffer = "")
    {
        var list = new List<FlatItem>();
        var roots = WorkspaceRegistry.GetRootWorkspaces();

        if (ExpandedWorkspaceIndex == -1 && string.IsNullOrEmpty(searchBuffer) && roots.Length > 0)
        {
            ExpandedWorkspaceIndex = 0;
        }

        for (int i = 0; i < roots.Length; i++)
        {
            var w = roots[i];
            var children = WorkspaceRegistry.GetChildWorkspaces(w.WorkspacePath);
            string query = searchBuffer.Trim();

            if (!string.IsNullOrEmpty(query))
            {
                bool rootMatch = SystemHelper.IsFuzzyMatch(w.Name, query);
                var matchingChildren = children.Where(c => SystemHelper.IsFuzzyMatch(c.Name, query)).ToList();
                bool hasChildMatch = matchingChildren.Count > 0;

                if (!rootMatch && !hasChildMatch)
                {
                    continue;
                }

                list.Add(new FlatItem
                {
                    Workspace = w,
                    WorkspaceIndex = i,
                    ActionIndex = -1,
                    Depth = 0,
                    ChildCount = hasChildMatch ? matchingChildren.Count : children.Length
                });

                // Expand root ONLY if child nodes matched query
                if (hasChildMatch)
                {
                    for (int c = 0; c < children.Length; c++)
                    {
                        var child = children[c];
                        if (!SystemHelper.IsFuzzyMatch(child.Name, query)) continue;

                        list.Add(new FlatItem
                        {
                            Workspace = child,
                            WorkspaceIndex = i,
                            ActionIndex = -300 - c,
                            IsChildWorkspace = true,
                            Depth = 1
                        });

                        if (ExpandedActionsWorkspacePath == child.WorkspacePath)
                        {
                            for (int j = 0; j < WorkspaceRegistry.SharedWorkspaceActions.Length; j++)
                            {
                                list.Add(new FlatItem { Workspace = child, WorkspaceIndex = i, ActionIndex = j, Depth = 2 });
                            }
                        }
                    }
                }
            }
            else
            {
                list.Add(new FlatItem
                {
                    Workspace = w,
                    WorkspaceIndex = i,
                    ActionIndex = -1,
                    Depth = 0,
                    ChildCount = children.Length
                });

                bool isExpanded = (i == ExpandedWorkspaceIndex);
                if (isExpanded)
                {
                    if (children.Length > 0)
                    {
                        for (int c = 0; c < children.Length; c++)
                        {
                            var child = children[c];
                            list.Add(new FlatItem
                            {
                                Workspace = child,
                                WorkspaceIndex = i,
                                ActionIndex = -300 - c,
                                IsChildWorkspace = true,
                                Depth = 1
                            });

                            if (ExpandedActionsWorkspacePath == child.WorkspacePath)
                            {
                                for (int j = 0; j < WorkspaceRegistry.SharedWorkspaceActions.Length; j++)
                                {
                                    list.Add(new FlatItem { Workspace = child, WorkspaceIndex = i, ActionIndex = j, Depth = 2 });
                                }
                            }
                        }
                    }

                    if (w.Links != null && w.Links.Length > 0)
                    {
                        list.Add(new FlatItem { Workspace = w, WorkspaceIndex = i, ActionIndex = -100, Depth = 1 });
                        for (int k = 0; k < w.Links.Length; k++)
                        {
                            list.Add(new FlatItem { Workspace = w, WorkspaceIndex = i, ActionIndex = -2 - k, Depth = 2 });
                        }
                    }

                    if (ExpandedActionsWorkspacePath == w.WorkspacePath)
                    {
                        for (int j = 0; j < WorkspaceRegistry.SharedWorkspaceActions.Length; j++)
                        {
                            list.Add(new FlatItem { Workspace = w, WorkspaceIndex = i, ActionIndex = j, Depth = 1 });
                        }
                    }
                }
            }
        }
        return list;
    }

    public static bool HandleEnter(WorkspaceEntry[] allWorkspaces, List<FlatItem> flatList, int detailsSel)
    {
        return HandleKeyInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false), allWorkspaces, flatList, detailsSel);
    }

    public static bool HandleKeyInput(ConsoleKeyInfo key, WorkspaceEntry[] allWorkspaces, List<FlatItem> flatList, int detailsSel)
    {
        if (detailsSel < 0 || detailsSel >= flatList.Count) return false;
        var item = flatList[detailsSel];

        if (key.Key == ConsoleKey.A || (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key == ConsoleKey.A))
        {
            ExpandedActionsWorkspacePath = (ExpandedActionsWorkspacePath == item.Workspace.WorkspacePath) ? null : item.Workspace.WorkspacePath;
            return false;
        }

        if (key.Key == ConsoleKey.Tab || key.Key == ConsoleKey.Spacebar || key.Key == ConsoleKey.RightArrow)
        {
            if (item.ActionIndex == -1)
            {
                ExpandedWorkspaceIndex = (ExpandedWorkspaceIndex == item.WorkspaceIndex) ? -1 : item.WorkspaceIndex;
                return false;
            }
            if (item.IsChildWorkspace)
            {
                ExpandedChildWorkspacePath = (ExpandedChildWorkspacePath == item.Workspace.WorkspacePath) ? null : item.Workspace.WorkspacePath;
                return false;
            }
        }

        if (key.Key == ConsoleKey.LeftArrow)
        {
            if (ExpandedChildWorkspacePath != null)
            {
                ExpandedChildWorkspacePath = null;
                return false;
            }
            if (ExpandedWorkspaceIndex != -1)
            {
                ExpandedWorkspaceIndex = -1;
                return false;
            }
        }

        if (key.Key == ConsoleKey.Enter)
        {
            if (item.IsChildGroupHeader)
            {
                return false;
            }
            if (item.IsChildWorkspace)
            {
                var res = WorkspaceRegistry.HandleWorkspaceAction(item.Workspace, 0);
                return res == "EXIT";
            }
            if (item.ActionIndex == -1)
            {
                var res = WorkspaceRegistry.HandleWorkspaceAction(item.Workspace, 0);
                return res == "EXIT";
            }
            if (item.ActionIndex == -100)
            {
                WorkspaceRegistry.ManageWorkspaceLinks(item.Workspace);
                return false;
            }
            if (item.ActionIndex <= -2 && item.ActionIndex > -100)
            {
                int linkIdx = -2 - item.ActionIndex;
                if (item.Workspace.Links != null && linkIdx >= 0 && linkIdx < item.Workspace.Links.Length)
                {
                    var link = item.Workspace.Links[linkIdx];
                    WorkspaceRegistry.OpenUrl(link.Url);
                }
                return false;
            }
            if (item.ActionIndex >= 0)
            {
                var res = WorkspaceRegistry.HandleWorkspaceAction(item.Workspace, item.ActionIndex);
                return res == "EXIT";
            }
        }

        return false;
    }

    public static IRenderable Render(Grid grid, string searchBuffer, WorkspaceEntry[] allWorkspaces, List<FlatItem> flatList, int selIdx, string currentDir)
    {
        int termH = 30;
        try { termH = Console.WindowHeight; } catch (Exception ex) { LogHelper.Log($"[SubPageProjNavigator] WindowHeight non-fatal: {ex.Message}", "DEBUG"); }
        int maxRows = Math.Max(5, termH - 18);
        int topRow = 0;
        int endRow = 0;

        (topRow, endRow) = ScrollableListView.ComputeViewport(flatList.Count, selIdx, maxRows);

        for (int i = topRow; i < endRow; i++)
        {
            var item = flatList[i];
            var isSelected = (i == selIdx);

            if (item.IsChildWorkspace)
            {
                var ws = item.Workspace;
                var isCurrent = !string.IsNullOrEmpty(ws.WorkspacePath) && string.Equals(ws.WorkspacePath.TrimEnd('\\', '/'), currentDir.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);

                var bullet = "  ├── ";
                var prefix = isSelected ? "  [green bold]❯──[/] " : $"{bullet}";
                var status = isCurrent ? "[bold black on green] ACTIVE [/] " : "";
                var boldName = string.IsNullOrEmpty(searchBuffer) ? ws.Name.EscapeMarkup() : SystemHelper.BoldFuzzyMatch(ws.Name, searchBuffer);
                var boldPath = string.IsNullOrEmpty(searchBuffer) ? ws.WorkspacePath.EscapeMarkup() : SystemHelper.BoldFuzzyMatch(ws.WorkspacePath, searchBuffer);
                var nameMarkup = isSelected ? $"[bold green]{boldName}[/]" : $"[cyan]{boldName}[/]";

                var expandSign = (ExpandedChildWorkspacePath == ws.WorkspacePath) ? "[[-]] " : "[[+]] ";
                grid.AddRow(new Markup($"{prefix}{expandSign}📄 {status}{nameMarkup} [dim]· {boldPath}[/]"));
            }
            else if (item.ActionIndex == -1)
            {
                var ws = item.Workspace;
                var isCurrent = !string.IsNullOrEmpty(ws.WorkspacePath) && string.Equals(ws.WorkspacePath.TrimEnd('\\', '/'), currentDir.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);

                var prefix = isSelected ? "[green bold]❯ [/]" : "  ";
                var status = isCurrent ? "[bold black on green] ACTIVE [/] " : "";
                var branch = WorkspaceRegistry.GetGitBranch(ws.WorkspacePath);
                var branchSuffix = !string.IsNullOrEmpty(branch) ? $" [yellow]🌿 {branch}[/]" : "";

                var boldName = string.IsNullOrEmpty(searchBuffer) ? ws.Name.EscapeMarkup() : SystemHelper.BoldFuzzyMatch(ws.Name, searchBuffer);
                var boldPath = string.IsNullOrEmpty(searchBuffer) ? ws.WorkspacePath.EscapeMarkup() : SystemHelper.BoldFuzzyMatch(ws.WorkspacePath, searchBuffer);

                var nameMarkup = isSelected ? $"[bold green]{boldName}[/]" : $"[bold white]{boldName}[/]";
                var pathMarkup = $"[dim]· {boldPath}[/]";

                var icon = item.ChildCount > 0 ? "📦" : "📁";
                var badge = item.ChildCount > 0 ? $" [dim yellow][[{item.ChildCount} sub-modules]][/]" : "";
                var expandSign = (item.WorkspaceIndex == ExpandedWorkspaceIndex) ? "[[-]] " : "[[+]] ";

                grid.AddRow(new Markup($"{prefix}{expandSign}{icon} {status}{nameMarkup}{badge}{branchSuffix} {pathMarkup}"));
            }
            else if (item.ActionIndex == -100)
            {
                var bullet = "├── ";
                var prefix = isSelected ? "  [green bold]❯──[/] " : $"  {bullet}";
                var labelMarkup = isSelected
                    ? "[bold green]🔗 Project Links (Enter to Manage)[/]"
                    : "[cyan]🔗 Project Links[/]";
                grid.AddRow(new Markup($"{prefix}{labelMarkup}"));
            }
            else if (item.ActionIndex <= -2 && item.ActionIndex > -100)
            {
                int linkIdx = -2 - item.ActionIndex;
                var link = item.Workspace.Links![linkIdx];
                var isLastLink = (linkIdx == item.Workspace.Links.Length - 1);

                var bullet = isLastLink ? "│   └── " : "│   ├── ";
                var prefix = isSelected ? "  [green bold]❯───[/] " : $"{bullet}";

                var labelMarkup = isSelected
                    ? $"[bold green]🌐 {link.Label.EscapeMarkup()}: {link.Url.EscapeMarkup()}[/]"
                    : $"[dim]🌐 {link.Label.EscapeMarkup()}: {link.Url.EscapeMarkup()}[/]";
                grid.AddRow(new Markup($"{prefix}{labelMarkup}"));
            }
            else
            {
                if (item.Depth == 3)
                {
                    var isLast = (item.ActionIndex == WorkspaceRegistry.SharedWorkspaceActions.Length - 1);
                    var bullet = isLast ? "  │   │   └── " : "  │   │   ├── ";
                    var prefix = isSelected ? "  [green bold]❯─────[/] " : $"{bullet}";
                    var actionLabel = WorkspaceRegistry.SharedWorkspaceActions[item.ActionIndex];
                    var labelMarkup = isSelected ? $"[bold green]{actionLabel.EscapeMarkup()}[/]" : $"[dim]{actionLabel.EscapeMarkup()}[/]";
                    grid.AddRow(new Markup($"{prefix}{labelMarkup}"));
                }
                else
                {
                    var isLast = (item.ActionIndex == WorkspaceRegistry.SharedWorkspaceActions.Length - 1);
                    var bullet = isLast ? "└── " : "├── ";
                    var prefix = isSelected ? "  [green bold]❯──[/] " : $"  {bullet}";
                    var actionLabel = WorkspaceRegistry.SharedWorkspaceActions[item.ActionIndex];
                    var labelMarkup = isSelected ? $"[bold green]{actionLabel.EscapeMarkup()}[/]" : $"[dim]{actionLabel.EscapeMarkup()}[/]";
                    grid.AddRow(new Markup($"{prefix}{labelMarkup}"));
                }
            }
        }

        string scrollStatus = "";
        if (flatList.Count > maxRows)
        {
            var aboveStr = topRow > 0 ? $"[yellow]▲ {topRow} items above[/]" : "[grey]▲ Start of list[/]";
            var belowStr = (endRow < flatList.Count) ? $"[yellow]▼ {flatList.Count - endRow} items below[/]" : "[grey]▼ End of list[/]";
            scrollStatus = $"  {aboveStr}   ·   {belowStr}";
        }
        else
        {
            scrollStatus = "  [grey]▲ Start of list   ·   ▼ End of list[/]";
        }

        var selectedItem = (selIdx >= 0 && selIdx < flatList.Count) ? flatList[selIdx] : default;
        var targetDisplay = selectedItem.Workspace != null ? selectedItem.Workspace.WorkspacePath : "No workspace selected";

        return new Rows(
            grid,
            new Rule().RuleStyle("cyan dim"),
            new Markup(scrollStatus),
            new Markup($"  [dim]Selected Target:[/] [bold cyan]{targetDisplay.EscapeMarkup()}[/]"),
            new Markup("\n[bold cyan][[Enter]][/] Open Target Folder  ·  [bold cyan][[Tab / Space]][/] Expand Sub-modules  ·  [bold cyan][[A]][/] Toggle Actions  ·  [bold cyan][[Esc]][/] Cancel")
        );
    }
}
