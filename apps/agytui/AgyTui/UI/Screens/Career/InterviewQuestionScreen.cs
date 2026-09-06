using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Career;

public sealed class InterviewQuestionScreen : IScreenView
{
    public string ScreenKey => "interview";
    public string Category => "Career";
    public string Title => "Technical Interview Question Bank";

    public int GetItemCount(string searchFilter) => 40;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]💼 Career Suite > Interview Question Bank[/]\n\nSystem Design & Technical Coding Questions..."))
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
