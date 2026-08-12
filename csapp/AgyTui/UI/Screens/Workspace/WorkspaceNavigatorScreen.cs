using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Workspace;

public class WorkspaceNavigatorScreen : IScreenView
{
    public string ScreenKey => "proj";
    public string Title => "Workspace Navigator";

    public int GetItemCount(string searchFilter)
    {
        var workspaces = WorkspaceRegistry.GetWorkspaces();
        if (!string.IsNullOrEmpty(searchFilter))
        {
            workspaces = workspaces.Where(w => w != null &&
                ((w.Name != null && SystemHelper.Instance.IsFuzzyMatch(w.Name, searchFilter)) ||
                 (w.WorkspacePath != null && SystemHelper.Instance.IsFuzzyMatch(w.WorkspacePath, searchFilter)))).ToArray();
        }
        var flatList = SubPageProjNavigator.GetFlatList(workspaces, searchFilter);
        return flatList.Count;
    }

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var workspaces = WorkspaceRegistry.GetWorkspaces();
        if (!string.IsNullOrEmpty(state.SearchFilter))
        {
            workspaces = workspaces.Where(w => w != null &&
                ((w.Name != null && SystemHelper.Instance.IsFuzzyMatch(w.Name, state.SearchFilter)) ||
                 (w.WorkspacePath != null && SystemHelper.Instance.IsFuzzyMatch(w.WorkspacePath, state.SearchFilter)))).ToArray();
        }
        var flatList = SubPageProjNavigator.GetFlatList(workspaces, state.SearchFilter);
        var currentDir = Directory.GetCurrentDirectory();
        return SubPageProjNavigator.Render(grid, state.SearchFilter, workspaces, flatList, state.SelectedIndex, currentDir);
    }

    public ScreenNavigationResult HandleInput(ConsoleKeyInfo key, ScreenState state)
    {
        if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Q)
        {
            return new ScreenNavigationResult(NavigationAction.Exit);
        }

        var workspaces = WorkspaceRegistry.GetWorkspaces();
        if (!string.IsNullOrEmpty(state.SearchFilter))
        {
            workspaces = workspaces.Where(w => w != null &&
                ((w.Name != null && SystemHelper.Instance.IsFuzzyMatch(w.Name, state.SearchFilter)) ||
                 (w.WorkspacePath != null && SystemHelper.Instance.IsFuzzyMatch(w.WorkspacePath, state.SearchFilter)))).ToArray();
        }
        var flatList = SubPageProjNavigator.GetFlatList(workspaces, state.SearchFilter);

        if (key.Key == ConsoleKey.Enter)
        {
            bool shouldExit = SubPageProjNavigator.HandleEnter(workspaces, flatList, state.SelectedIndex, state.SearchFilter);
            return new ScreenNavigationResult(shouldExit ? NavigationAction.Exit : NavigationAction.Handled);
        }

        if (key.KeyChar >= '1' && key.KeyChar <= '9')
        {
            bool shouldExit = SubPageProjNavigator.HandleKeyInput(key, workspaces, flatList, state.SelectedIndex, state.SearchFilter);
            return new ScreenNavigationResult(shouldExit ? NavigationAction.Exit : NavigationAction.Handled);
        }

        return new ScreenNavigationResult(NavigationAction.Continue);
    }
}

