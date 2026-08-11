using AgyTui.UI.Core.Navigation.Abstractions;
using AgyTui.UI.Core.Abstractions;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Workspace.Helpers;

public class ProjectScreen : IScreenView
{
    public string ScreenKey => "proj";
    public string Title => "Project Workspace Manager";

    public int GetItemCount(string searchFilter)
    {
        var allWorkspaces = WorkspaceRegistry.GetWorkspaces();
        var flatList = SubPageProjNavigator.GetFlatList(allWorkspaces, searchFilter);
        return flatList.Count;
    }

    public IRenderable Render(Grid grid, ScreenState state)
    {
        grid.AddRow(new Markup("[cyan bold]Registered Workspace Navigator (cnav):[/]\n"));
        if (!string.IsNullOrEmpty(state.SearchFilter))
        {
            grid.AddRow(new Markup($"[yellow]Search:[/] [white]{state.SearchFilter.EscapeMarkup()}[/]_\n"));
        }

        var allWorkspaces = WorkspaceRegistry.GetWorkspaces();
        var flatList = SubPageProjNavigator.GetFlatList(allWorkspaces, state.SearchFilter);
        var currentDir = Directory.GetCurrentDirectory();

        return SubPageProjNavigator.Render(grid, state.SearchFilter, allWorkspaces, flatList, state.SelectedIndex, currentDir);
    }

    public ScreenNavigationResult HandleInput(ConsoleKeyInfo key, ScreenState state)
    {
        var allWorkspaces = WorkspaceRegistry.GetWorkspaces();
        var flatList = SubPageProjNavigator.GetFlatList(allWorkspaces, state.SearchFilter);

        if (key.Key == ConsoleKey.Escape)
        {
            return new ScreenNavigationResult(NavigationAction.Exit);
        }

        bool handled = SubPageProjNavigator.HandleKeyInput(key, allWorkspaces, flatList, state.SelectedIndex);
        if (handled)
        {
            return new ScreenNavigationResult(NavigationAction.Handled);
        }

        return new ScreenNavigationResult(NavigationAction.Continue);
    }
}

