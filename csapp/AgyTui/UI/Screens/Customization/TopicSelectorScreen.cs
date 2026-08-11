using Spectre.Console;
using Spectre.Console.Rendering;
using AgyTui.UI.Core.Navigation;
using AgyTui.UI.Core.Navigation.Abstractions;
using AgyTui.UI.Core.Abstractions;

namespace AgyTui.UI.Screens.Customization;

public class TopicSelectorScreen : IScreenView
{
    public string ScreenKey => "topic";
    public string Title => "AI Learning Topic Selector";

    public int GetItemCount(string searchFilter)
    {
        var topics = new[] { "jp", "en", "cs", "dsa", "interview", "[Type Custom Topic...]" };
        if (string.IsNullOrEmpty(searchFilter)) return topics.Length;
        return topics.Count(t => t.Contains(searchFilter, StringComparison.OrdinalIgnoreCase));
    }

    public IRenderable Render(Grid grid, ScreenState state)
    {
        return SubPageTopicNavigator.Render(grid, "topic", state.SearchFilter, state.SelectedIndex);
    }

    public ScreenNavigationResult HandleInput(ConsoleKeyInfo key, ScreenState state)
    {
        if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Q)
        {
            return new ScreenNavigationResult(NavigationAction.Exit);
        }

        if (key.Key == ConsoleKey.Enter)
        {
            bool shouldExit = SubPageTopicNavigator.HandleSelection("topic", state.SearchFilter, state.SelectedIndex);
            return new ScreenNavigationResult(shouldExit ? NavigationAction.Exit : NavigationAction.Handled);
        }

        return new ScreenNavigationResult(NavigationAction.Continue);
    }
}

