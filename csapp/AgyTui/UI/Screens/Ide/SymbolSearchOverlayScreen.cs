using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Ide;

public sealed class SymbolSearchOverlayScreen : IScreenView
{
    public string ScreenKey => "symbol";
    public string Category => "Ide";
    public string Title => "Symbol Search Overlay";

    public int GetItemCount(string searchFilter) => 15;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]💻 Terminal IDE > Symbol Search[/]\n\nSearching codebase symbols (classes, methods, interfaces)..."))
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
