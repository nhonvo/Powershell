using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Ollama;

public sealed class OllamaModelManagerScreen : IScreenView
{
    public string ScreenKey => "ollama-models";
    public string Category => "Ollama";
    public string Title => "Pulled Model Manager";

    public int GetItemCount(string searchFilter) => 10;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]🤖 Ollama > Pulled Model Manager[/]\n\nListing available local Ollama LLM models..."))
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
