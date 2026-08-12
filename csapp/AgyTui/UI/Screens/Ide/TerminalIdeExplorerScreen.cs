using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Ide;

public sealed class TerminalIdeExplorerScreen : IScreenView
{
    public string ScreenKey => "ide";
    public string Category => "Ide";
    public string Title => "Terminal IDE Explorer";

    public int GetItemCount(string searchFilter) => 20;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]💻 Terminal IDE Explorer[/]\n\nInteractive File Explorer & Viewport..."))
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
