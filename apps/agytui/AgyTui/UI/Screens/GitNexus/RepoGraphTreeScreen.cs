using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.GitNexus;

public sealed class RepoGraphTreeScreen : IScreenView
{
    public string ScreenKey => "repograph";
    public string Category => "GitNexus";
    public string Title => "Project Dependency Graph Tree";

    public int GetItemCount(string searchFilter) => 15;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]🕸️ Repo Graph > Project Dependencies[/]\n\nMulti-repository workspace dependency tree..."))
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
