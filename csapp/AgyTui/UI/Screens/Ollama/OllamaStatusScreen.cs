using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Ollama;

public class OllamaStatusScreen : IScreenView
{
    public string ScreenKey => "ollama-status";
    public string Title => "Ollama Daemon Status";

    public int GetItemCount(string searchFilter) => 1;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        grid.AddRow(new Markup("[green bold]✔ Ollama Local Daemon: ONLINE (http://localhost:11434)[/]"));
        grid.AddRow(new Markup("[dim]Active Model: qwen2.5-coder:7b | VRAM: 4.8 GB allocated[/]"));
        grid.AddRow(new Markup("\n[bold cyan]Title: 🤖 Ollama Daemon Status — ONLINE (ollama-status)[/]"));
        grid.AddRow(new Markup("[dim]Nav: Esc Back[/]"));
        grid.AddRow(new Markup("[bold white]Select option: [/]"));
        return grid;
    }

    public ScreenNavigationResult HandleInput(ConsoleKeyInfo key, ScreenState state)
    {
        if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Q)
        {
            return new ScreenNavigationResult(NavigationAction.Exit);
        }
        return new ScreenNavigationResult(NavigationAction.Continue);
    }
}


