using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Customization;

public class FavoritesManagerScreen : IScreenView
{
    public string ScreenKey => "favorite";
    public string Title => "Favorites Manager";

    public int GetItemCount(string searchFilter)
    {
        return SubPageFavoriteNavigator.GetFavoriteItems(searchFilter).Count;
    }

    public IRenderable Render(Grid grid, ScreenState state)
    {
        return SubPageFavoriteNavigator.Render(grid, state.SearchFilter, state.SelectedIndex);
    }

    public ScreenNavigationResult HandleInput(ConsoleKeyInfo key, ScreenState state)
    {
        if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Q)
        {
            return new ScreenNavigationResult(NavigationAction.Exit);
        }

        if (key.Key == ConsoleKey.Enter)
        {
            bool shouldExit = SubPageFavoriteNavigator.HandleSelection(state.SearchFilter, state.SelectedIndex);
            return new ScreenNavigationResult(shouldExit ? NavigationAction.Exit : NavigationAction.Handled);
        }

        if (key.Key == ConsoleKey.A)
        {
            SubPageFavoriteNavigator.AddNewFavorite();
            return new ScreenNavigationResult(NavigationAction.Handled);
        }

        if (key.Key == ConsoleKey.R)
        {
            SubPageFavoriteNavigator.ResetFavorites();
            return new ScreenNavigationResult(NavigationAction.Handled);
        }

        return new ScreenNavigationResult(NavigationAction.Continue);
    }
}

