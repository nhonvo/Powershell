using Spectre.Console;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Workspace;

public sealed class WorkspacePruneScreen : IScreenView
{
    public string ScreenKey => "prune-workspaces";
    public string Category => "Workspace";
    public string Title => "Prune Stale Directory Links";

    public int GetItemCount(string searchFilter) => 5;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]💼 Workspace Manager > Prune Stale Workspaces[/]\n\nScanning workspace registry for missing directory paths..."))
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
