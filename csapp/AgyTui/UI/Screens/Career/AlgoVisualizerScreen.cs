using Spectre.Console;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Career;

public sealed class AlgoVisualizerScreen : IScreenView
{
    public string ScreenKey => "algo-viz";
    public string Category => "Career";
    public string Title => "Algorithm Visualizer Trace";

    public int GetItemCount(string searchFilter) => 8;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]🧩 Algo Visualizer > Sorting & Search Trace[/]\n\nInteractive sorting algorithm trace..."))
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
