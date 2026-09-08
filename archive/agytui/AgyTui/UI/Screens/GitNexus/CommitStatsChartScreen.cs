using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.GitNexus;

public sealed class CommitStatsChartScreen : IScreenView
{
    public string ScreenKey => "gstats";
    public string Category => "GitNexus";
    public string Title => "Commit Frequency & Stats Chart";

    public int GetItemCount(string searchFilter) => 12;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]📊 Git Nexus > Commit Frequency Chart[/]\n\nMulti-repository commit velocity bar chart..."))
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
