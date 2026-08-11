using Spectre.Console;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Learn;

public sealed class StudyStatisticsScreen : IScreenView
{
    public string ScreenKey => "study-stats";
    public string Category => "Learn";
    public string Title => "Study Console Statistics & Progress";

    public int GetItemCount(string searchFilter) => 7;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]📊 Study Console > Progress & Streak Dashboard[/]\n\nWeekly Pomodoro & Retention Breakdown..."))
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
