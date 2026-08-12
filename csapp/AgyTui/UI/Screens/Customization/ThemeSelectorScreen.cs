using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Customization;

public class ThemeSelectorScreen : IScreenView
{
    public string ScreenKey => "theme";
    public string Title => "Oh My Posh Visual Theme Selector";

    public int GetItemCount(string searchFilter)
    {
        var themes = SubPageThemeNavigator.GetThemeNames();
        if (string.IsNullOrEmpty(searchFilter)) return themes.Length;
        return themes.Count(t => t.Contains(searchFilter, StringComparison.OrdinalIgnoreCase));
    }

    public IRenderable Render(Grid grid, ScreenState state)
    {
        return SubPageThemeNavigator.Render(grid, state.SearchFilter, state.SelectedIndex);
    }

    public ScreenNavigationResult HandleInput(ConsoleKeyInfo key, ScreenState state)
    {
        if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Q)
        {
            return new ScreenNavigationResult(NavigationAction.Exit);
        }

        if (key.Key == ConsoleKey.Enter)
        {
            bool shouldExit = SubPageThemeNavigator.HandleSelection(state.SearchFilter, state.SelectedIndex);
            return new ScreenNavigationResult(shouldExit ? NavigationAction.Exit : NavigationAction.Handled);
        }

        return new ScreenNavigationResult(NavigationAction.Continue);
    }
}

