using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Learn;

public sealed class MasterLearnHubScreen : IScreenView
{
    public string ScreenKey => "learn";
    public string Category => "Learn";
    public string Title => "Master Learning Suite Hub";

    public int GetItemCount(string searchFilter) => 12;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]🎓 Master Learning Suite Hub[/]\n\nAntigravity Spaced Repetition & Study Hub..."))
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
