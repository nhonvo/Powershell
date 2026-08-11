using Spectre.Console;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Ollama;

public sealed class OllamaBenchmarkScreen : IScreenView
{
    public string ScreenKey => "ollama-benchmark";
    public string Category => "Ollama";
    public string Title => "Local LLM Performance Benchmark";

    public int GetItemCount(string searchFilter) => 5;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]🤖 Ollama > Performance Benchmark[/]\n\nMeasuring prompt evaluation & tok/s performance..."))
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
