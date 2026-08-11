using Spectre.Console;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Infrastructure;

public sealed class AwsInspectorScreen : IScreenView
{
    public string ScreenKey => "aws-status";
    public string Category => "Infrastructure";
    public string Title => "AWS Infrastructure Inspector";

    public int GetItemCount(string searchFilter) => 6;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]☁️ AWS Infrastructure Inspector[/]\n\nLocalStack & AWS Services Health..."))
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
