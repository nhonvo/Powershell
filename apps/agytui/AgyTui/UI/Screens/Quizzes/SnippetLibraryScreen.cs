using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Quizzes;

public sealed class SnippetLibraryScreen : IScreenView
{
    public string ScreenKey => "snippets";
    public string Category => "Quizzes";
    public string Title => "Code Snippet Library Browser";

    public int GetItemCount(string searchFilter) => 30;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]⚡ Snippet Library > Browser[/]\n\nReusable multi-language code snippets..."))
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
