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
        public bool IsChildWorkspace;
        public int ChildCount;
    }

    public static int SelectedWorkspaceIndex = 0;
    public static int SelectedActionIndex = -1;
    public static int ExpandedWorkspaceIndex = -1;

    public static List<FlatItem> GetFlatList(WorkspaceEntry[] allWorkspaces, string searchBuffer = "")
    {
        var list = new List<FlatItem>();
        var roots = WorkspaceRegistry.GetRootWorkspaces();

        string query = searchBuffer.Trim();

        for (int i = 0; i < roots.Length; i++)
        {
            var w = roots[i];
            var children = WorkspaceRegistry.GetChildWorkspaces(w.WorkspacePath);

            if (!string.IsNullOrEmpty(query))
            {
                bool rootMatch = SystemHelper.Instance.IsFuzzyMatch(w.Name, query);
                var matchingChildren = children.Where(c => SystemHelper.Instance.IsFuzzyMatch(c.Name, query)).ToList();
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
                    ChildCount = hasChildMatch ? matchingChildren.Count : children.Length
                });

                if (hasChildMatch || i == ExpandedWorkspaceIndex)
                {
                    for (int c = 0; c < children.Length; c++)
                    {
                        var child = children[c];
                        if (!string.IsNullOrEmpty(query) && !SystemHelper.Instance.IsFuzzyMatch(child.Name, query)) continue;

                        list.Add(new FlatItem
                        {
                            Workspace = child,
                            WorkspaceIndex = i,
                            ActionIndex = -1,
                            IsChildWorkspace = true
                        });
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
                    ChildCount = children.Length
                });

                if (i == ExpandedWorkspaceIndex)
                {
                    for (int c = 0; c < children.Length; c++)
                    {
                        var child = children[c];
                        list.Add(new FlatItem
                        {
                            Workspace = child,
                            WorkspaceIndex = i,
                            ActionIndex = -1,
                            IsChildWorkspace = true
                        });
                    }
                }
            }
        }
        return list;
    }

    public static bool HandleEnter(WorkspaceEntry[] allWorkspaces, List<FlatItem> flatList, int detailsSel, string searchBuffer = "")
    {
        return HandleKeyInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false), allWorkspaces, flatList, detailsSel, searchBuffer);
    }

    public static bool HandleKeyInput(ConsoleKeyInfo key, WorkspaceEntry[] allWorkspaces, List<FlatItem> flatList, int detailsSel, string searchBuffer = "")
    {
        if (detailsSel < 0 || detailsSel >= flatList.Count) return false;
        var item = flatList[detailsSel];

        // Number shortcuts 1-9 trigger actions ONLY when search filter is empty or Alt modifier is held
        if ((key.KeyChar >= '1' && key.KeyChar <= '9') && (string.IsNullOrEmpty(searchBuffer) || key.Modifiers.HasFlag(ConsoleModifiers.Alt)))
        {
            int actionIdx = key.KeyChar - '1';
            if (actionIdx < WorkspaceRegistry.SharedWorkspaceActions.Length)
            {
                var res = WorkspaceRegistry.HandleWorkspaceAction(item.Workspace, actionIdx);
                return res == "EXIT";
            }
        }

        if (key.Key == ConsoleKey.Tab || key.Key == ConsoleKey.RightArrow || (key.Key == ConsoleKey.Spacebar && string.IsNullOrEmpty(item.Workspace?.Name)))
        {
            ExpandedWorkspaceIndex = (ExpandedWorkspaceIndex == item.WorkspaceIndex) ? -1 : item.WorkspaceIndex;
            return false;
        }

        if (key.Key == ConsoleKey.LeftArrow)
        {
            if (ExpandedWorkspaceIndex != -1)
            {
                ExpandedWorkspaceIndex = -1;
                return false;
            }
        }

        if (key.Key == ConsoleKey.Enter)
        {
            var res = WorkspaceRegistry.HandleWorkspaceAction(item.Workspace, 0);
            return res == "EXIT";
        }

        return false;
    }

    public static IRenderable Render(Grid outerGrid, string searchBuffer, WorkspaceEntry[] allWorkspaces, List<FlatItem> flatList, int selIdx, string currentDir)
    {
        int termH = 30;
        try { termH = Console.WindowHeight; } catch (Exception ex) { LogHelper.Log($"[SubPageProjNavigator] WindowHeight non-fatal: {ex.Message}", "DEBUG"); }
        int maxRows = Math.Max(6, termH - 10);

        var (topRow, endRow) = ScrollableListView.ComputeViewport(flatList.Count, selIdx, maxRows);

        // Build Left Panel: Workspaces List
        var leftTable = new Table().Border(TableBorder.None).NoBorder().Expand();
        leftTable.AddColumn(new TableColumn("[bold cyan]📁 WORKSPACES[/]").LeftAligned());

        var filterLine = !string.IsNullOrEmpty(searchBuffer)
            ? $"[yellow]Filter:[/] [white]{searchBuffer.EscapeMarkup()}[/]_\n"
            : "[dim]Type to filter (Esc to clear)[/]\n";
        leftTable.AddRow(new Markup(filterLine));

        if (flatList.Count == 0)
        {
            leftTable.AddRow(new Markup($"  [dim]No workspaces matching '{searchBuffer.EscapeMarkup()}'.[/]"));
        }
        else
        {
            for (int i = topRow; i < endRow; i++)
            {
                var item = flatList[i];
                var isSelected = (i == selIdx);
                var ws = item.Workspace;
                var isCurrent = !string.IsNullOrEmpty(ws.WorkspacePath) && string.Equals(ws.WorkspacePath.TrimEnd('\\', '/'), currentDir.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);

                if (item.IsChildWorkspace)
                {
                    var prefix = isSelected ? "  [green bold]❯──[/] " : "  ├── ";
                    var status = isCurrent ? "[bold black on green] ACTIVE [/] " : "";
                    var boldName = string.IsNullOrEmpty(searchBuffer) ? ws.Name.EscapeMarkup() : SystemHelper.Instance.BoldFuzzyMatch(ws.Name, searchBuffer);
                    var nameMarkup = isSelected ? $"[bold green]{boldName}[/]" : $"[cyan]{boldName}[/]";
                    leftTable.AddRow(new Markup($"{prefix}📄 {status}{nameMarkup}"));
                }
                else
                {
                    var prefix = isSelected ? "[green bold]❯ [/]" : "  ";
                    var status = isCurrent ? "[bold black on green] ACTIVE [/] " : "";
                    var branch = WorkspaceRegistry.GetGitBranch(ws.WorkspacePath);
                    var branchSuffix = !string.IsNullOrEmpty(branch) ? $" [yellow]🌿 {branch}[/]" : "";

                    var boldName = string.IsNullOrEmpty(searchBuffer) ? ws.Name.EscapeMarkup() : SystemHelper.Instance.BoldFuzzyMatch(ws.Name, searchBuffer);
                    var nameMarkup = isSelected ? $"[bold green]{boldName}[/]" : $"[bold white]{boldName}[/]";

                    var icon = item.ChildCount > 0 ? "📦" : "📁";
                    var badge = item.ChildCount > 0 ? $" [dim yellow][[{item.ChildCount}]][/]" : "";
                    var expandSign = (item.WorkspaceIndex == ExpandedWorkspaceIndex) ? "[[-]] " : "[[+]] ";

                    leftTable.AddRow(new Markup($"{prefix}{expandSign}{icon} {status}{nameMarkup}{badge}{branchSuffix}"));
                }
            }
        }

        string scrollStatus = flatList.Count > maxRows
            ? $"[yellow]▲ {topRow} above[/] · [yellow]▼ {flatList.Count - endRow} below[/]"
            : "[dim]Showing all workspaces[/]";
        leftTable.AddRow(new Markup($"\n[dim]{scrollStatus}[/]"));

        // Build Right Panel: Target Details & Actions
        var rightTable = new Table().Border(TableBorder.None).NoBorder().Expand();
        rightTable.AddColumn(new TableColumn("[bold yellow]🎯 TARGET DETAILS & ACTIONS[/]").LeftAligned());

        var selectedItem = (selIdx >= 0 && selIdx < flatList.Count) ? flatList[selIdx] : default;
        if (selectedItem.Workspace != null)
        {
            var ws = selectedItem.Workspace;
            var branch = WorkspaceRegistry.GetGitBranch(ws.WorkspacePath);
            var branchText = !string.IsNullOrEmpty(branch) ? $" [yellow]🌿 {branch}[/]" : "";

            rightTable.AddRow(new Markup($"[bold green]📦 {ws.Name.EscapeMarkup()}[/]{branchText}"));
            rightTable.AddRow(new Markup($"[dim]Path:[/] [cyan]{ws.WorkspacePath.EscapeMarkup()}[/]"));
            rightTable.AddRow(new Rule().RuleStyle("dim cyan"));

            rightTable.AddRow(new Markup("[bold yellow]⚡ ACTIONS (Press 1-9 or Enter):[/]"));
            rightTable.AddRow(new Markup("  [bold green][[Enter]][/] 📂 Change Directory to workspace"));

            for (int a = 0; a < WorkspaceRegistry.SharedWorkspaceActions.Length; a++)
            {
                var actionName = WorkspaceRegistry.SharedWorkspaceActions[a];
                var keyNum = a + 1 <= 9 ? $"[bold yellow][[ {a + 1} ]][/]" : "    ";
                rightTable.AddRow(new Markup($"  {keyNum} {actionName.EscapeMarkup()}"));
            }

            var children = WorkspaceRegistry.GetChildWorkspaces(ws.WorkspacePath);
            if (children.Length > 0)
            {
                rightTable.AddRow(new Rule().RuleStyle("dim cyan"));
                rightTable.AddRow(new Markup($"[bold cyan]📄 SUB-MODULES ({children.Length}):[/]"));
                foreach (var child in children.Take(5))
                {
                    rightTable.AddRow(new Markup($"  ├── [white]{child.Name.EscapeMarkup()}[/]"));
                }
            }
        }
        else
        {
            rightTable.AddRow(new Markup("[dim]No workspace selected[/]"));
        }

        // Combine into 2-Column Layout
        var layoutGrid = new Grid();
        layoutGrid.AddColumn(new GridColumn().Width(52));
        layoutGrid.AddColumn(new GridColumn());
        layoutGrid.AddRow(leftTable, rightTable);

        return new Rows(
            layoutGrid,
            new Rule().RuleStyle("cyan dim"),
            new Markup("[bold cyan][[Enter]][/] Open Workspace  ·  [bold cyan][[1-9]][/] Run Direct Action  ·  [bold cyan][[Tab / →]][/] Sub-modules  ·  [bold cyan][[Esc]][/] Clear / Cancel")
        );
    }
}
