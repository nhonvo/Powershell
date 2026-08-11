using Spectre.Console;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Ide;

public sealed class PatternSearchScreen : IScreenView
{
    public string ScreenKey => "ide-search";
    public string Category => "Ide";
    public string Title => "Workspace Pattern Search";

    public int GetItemCount(string searchFilter) => 30;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]💻 Terminal IDE > Pattern Search[/]\n\nSearching regex & text patterns across workspace files..."))
        {
            Border = BoxBorder.Rounded,
            Expand = true
        };
        return panel;
    }

    public ScreenNavigationResult HandleInput(ConsoleKeyInfo key, ScreenState state)
    {
        if (key.Key == ConsoleKey.Escape)
            return new ScreenNavigationResult(NavigationAction.Exit);

        return new ScreenNavigationResult(NavigationAction.Continue);
    }
}
