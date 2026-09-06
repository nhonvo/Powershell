using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Career;

public sealed class StarBuilderScreen : IScreenView
{
    public string ScreenKey => "star-builder";
    public string Category => "Career";
    public string Title => "STAR Behavioral Answer Builder";

    public int GetItemCount(string searchFilter) => 4;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]⭐ STAR Builder > Structured Card[/]\n\nSituation, Task, Action, Result Form Builder..."))
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
