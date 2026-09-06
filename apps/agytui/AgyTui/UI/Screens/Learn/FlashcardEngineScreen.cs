using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Learn;

public sealed class FlashcardEngineScreen : IScreenView
{
    public string ScreenKey => "flashcards";
    public string Category => "Learn";
    public string Title => "SM-2 Flashcard Repetition Engine";

    public int GetItemCount(string searchFilter) => 25;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]🎴 Flashcard Engine > SM-2 Spaced Repetition[/]\n\nCard Review Session..."))
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
