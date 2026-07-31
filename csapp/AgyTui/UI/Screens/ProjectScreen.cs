using AgyTui.UI.Core.Navigation.Interfaces;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens;

public class ProjectScreen : IScreenView
{
    public string ScreenKey => "proj";
    public string Title => "Project Workspace Manager";

    public int GetItemCount(string searchFilter)
    {
        var workspaces = WorkspaceRegistry.GetWorkspaces();
        if (string.IsNullOrEmpty(searchFilter)) return workspaces.Length;
        return workspaces.Count(w => w != null &&
            ((w.Name != null && SystemHelper.IsFuzzyMatch(w.Name, searchFilter)) ||
             (w.WorkspacePath != null && SystemHelper.IsFuzzyMatch(w.WorkspacePath, searchFilter))));
    }

    public IRenderable Render(Grid grid, ScreenState state)
    {
        grid.AddRow(new Markup("[cyan bold]Select Workspace or Action:[/]\n"));
        if (!string.IsNullOrEmpty(state.SearchFilter))
        {
            grid.AddRow(new Markup($"[yellow]Search:[/] [white]{state.SearchFilter.EscapeMarkup()}[/]_\n"));
        }
        var workspaces = WorkspaceRegistry.GetWorkspaces();
        if (!string.IsNullOrEmpty(state.SearchFilter))
        {
            workspaces = workspaces.Where(w => w != null &&
                ((w.Name != null && SystemHelper.IsFuzzyMatch(w.Name, state.SearchFilter)) ||
                 (w.WorkspacePath != null && SystemHelper.IsFuzzyMatch(w.WorkspacePath, state.SearchFilter)))).ToArray();
        }

        grid.AddRow(new Markup($"[dim]Workspaces ({workspaces.Length}):[/]"));
        for (var i = 0; i < workspaces.Length; i++)
        {
            var isSel = (i == state.SelectedIndex);
            var prefix = isSel ? "[green bold]> [/]" : "  ";
            var name = workspaces[i]?.Name ?? "Unknown";
            var path = workspaces[i]?.WorkspacePath ?? "";
            grid.AddRow(new Markup($"{prefix}[bold white]{name.EscapeMarkup()}[/] [dim]({path.EscapeMarkup()})[/]"));
        }
        grid.AddRow(new Markup("\n[dim]↑/↓ Navigate  ·  Enter Select Workspace  ·  Esc Cancel[/]"));
        return grid;
    }

    public ScreenNavigationResult HandleInput(ConsoleKeyInfo key, ScreenState state)
    {
        var workspaces = WorkspaceRegistry.GetWorkspaces();
        if (!string.IsNullOrEmpty(state.SearchFilter))
        {
            workspaces = workspaces.Where(w => w != null &&
                ((w.Name != null && SystemHelper.IsFuzzyMatch(w.Name, state.SearchFilter)) ||
                 (w.WorkspacePath != null && SystemHelper.IsFuzzyMatch(w.WorkspacePath, state.SearchFilter)))).ToArray();
        }

        switch (key.Key)
        {
            case ConsoleKey.Enter:
                if (state.SelectedIndex >= 0 && state.SelectedIndex < workspaces.Length)
                {
                    var target = workspaces[state.SelectedIndex];
                    if (target != null)
                    {
                        WorkspaceRegistry.HandleWorkspaceAction(target, 0);
                    }
                    return new ScreenNavigationResult(NavigationAction.Handled);
                }
                break;

            case ConsoleKey.Escape:
                return new ScreenNavigationResult(NavigationAction.Exit);
        }

        return new ScreenNavigationResult(NavigationAction.Continue);
    }
}
