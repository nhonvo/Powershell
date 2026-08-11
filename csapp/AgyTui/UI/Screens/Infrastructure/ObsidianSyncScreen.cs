using Spectre.Console;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Infrastructure;

public sealed class ObsidianSyncScreen : IScreenView
{
    public string ScreenKey => "obsidian";
    public string Category => "Infrastructure";
    public string Title => "Obsidian Vault Daily Note Sync";

    public int GetItemCount(string searchFilter) => 10;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]📓 Obsidian Bridge > Vault Sync[/]\n\nSyncing Obsidian vault notes & daily notes..."))
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
