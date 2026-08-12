using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Diagnostics;

public sealed class DockerContainerLogScreen : IScreenView
{
    public string ScreenKey => "dlogsu";
    public string Category => "Diagnostics";
    public string Title => "Docker Container Logs Viewer";

    public int GetItemCount(string searchFilter) => 50;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]📑 Docker Container Logs Stream[/]\n\nTailing logs from active container containers..."))
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
