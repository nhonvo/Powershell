using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Workspace;

public sealed class WorkspaceDiscoverScreen : IScreenView
{
    public string ScreenKey => "discover-workspaces";
    public string Category => "Workspace";
    public string Title => "Auto-Discover Unregistered Projects";

    public int GetItemCount(string searchFilter) => 10;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]💼 Workspace Manager > Auto-Discover Projects[/]\n\nScanning workspace root for unregistered projects..."))
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
