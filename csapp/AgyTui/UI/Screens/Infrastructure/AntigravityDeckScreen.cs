using Spectre.Console;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Infrastructure;

public sealed class AntigravityDeckScreen : IScreenView
{
    public string ScreenKey => "agy-deck";
    public string Category => "Infrastructure";
    public string Title => "Antigravity Deck Micro-Server";

    public int GetItemCount(string searchFilter) => 4;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]🌌 Antigravity Deck > Micro-Server[/]\n\nLocal deck server on port 3000..."))
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
