using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Quizzes;

public sealed class KanaQuizScreen : IScreenView
{
    public string ScreenKey => "kana-quiz";
    public string Category => "Quizzes";
    public string Title => "Japanese Kana Practice Quiz";

    public int GetItemCount(string searchFilter) => 46;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]🌸 Japanese Suite > Kana Practice[/]\n\nHiragana & Katakana Recognition Drill..."))
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
