using Spectre.Console;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Quizzes;

public sealed class CsharpQuizScreen : IScreenView
{
    public string ScreenKey => "quiz-cs";
    public string Category => "Quizzes";
    public string Title => "C# Knowledge Practice Quiz";

    public int GetItemCount(string searchFilter) => 20;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]🎯 Interactive Quiz > C# Knowledge[/]\n\nMultiple-choice quiz session..."))
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
