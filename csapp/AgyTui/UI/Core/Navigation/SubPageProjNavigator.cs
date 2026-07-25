using Spectre.Console.Rendering;

namespace AgyTui.UI.Core.Navigation;

public static class SubPageProjNavigator
{
    public struct FlatItem
    {
        public WorkspaceEntry Workspace;
        public int WorkspaceIndex;
        public int ActionIndex;
    }

    public static int SelectedWorkspaceIndex = 0;
    public static int SelectedActionIndex = -1;
    public static int ExpandedWorkspaceIndex = -1;

    public static List<FlatItem> GetFlatList(WorkspaceEntry[] workspaces)
    {
        var list = new List<FlatItem>();
        for (int i = 0; i < workspaces.Length; i++)
        {
            var w = workspaces[i];
            list.Add(new FlatItem { Workspace = w, WorkspaceIndex = i, ActionIndex = -1 });
            if (i == ExpandedWorkspaceIndex)
            {
                for (int j = 0; j < WorkspaceRegistry.SharedWorkspaceActions.Length; j++)
                {
                    list.Add(new FlatItem { Workspace = w, WorkspaceIndex = i, ActionIndex = j });
                }
            }
        }
        return list;
    }

    public static bool HandleEnter(WorkspaceEntry[] workspaces, List<FlatItem> flatList, int detailsSel)
    {
        if (detailsSel >= 0 && detailsSel < flatList.Count)
        {
            var item = flatList[detailsSel];
            if (item.ActionIndex == -1)
            {
                WorkspaceRegistry.HandleWorkspaceAction(item.Workspace, 0);
                return true;
            }
            else
            {
                WorkspaceRegistry.HandleWorkspaceAction(item.Workspace, item.ActionIndex);
                return true;
            }
        }
        return false;
    }

    public static IRenderable Render(Grid grid, string searchBuffer, WorkspaceEntry[] workspaces, List<FlatItem> flatList, int selIdx, string currentDir)
    {
        int termH = 30;
        try { termH = Console.WindowHeight; } catch { }
        int maxRows = Math.Max(5, termH - 18);
        int topRow = 0;
        int endRow = 0;

        (topRow, endRow) = ScrollableListView.ComputeViewport(flatList.Count, selIdx, maxRows);

        for (int i = topRow; i < endRow; i++)
        {
            var item = flatList[i];
            var isSelected = (i == selIdx);

            if (item.ActionIndex == -1)
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

                var expandSign = (item.WorkspaceIndex == ExpandedWorkspaceIndex) ? "[[-]] " : "[[+]] ";

                grid.AddRow(new Markup($"{prefix}{expandSign}📁 {status}{nameMarkup}{branchSuffix} {pathMarkup}"));
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

        var selTarget = (SelectedWorkspaceIndex >= 0 && SelectedWorkspaceIndex < workspaces.Length) ? workspaces[SelectedWorkspaceIndex]?.WorkspacePath : null;
        var targetDisplay = !string.IsNullOrEmpty(selTarget) ? selTarget : "No workspace selected";

        return new Rows(
            grid,
            new Rule().RuleStyle("cyan dim"),
            new Markup(scrollStatus),
            new Markup($"  [dim]Selected Target:[/] [bold cyan]{targetDisplay.EscapeMarkup()}[/]"),
            new Markup("\n[bold cyan][[Enter]][/] Toggle/Run  ·  [bold cyan][[Esc]][/] Cancel")
        );
    }
}
