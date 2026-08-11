using Spectre.Console;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Ide;

public sealed class GitDiffViewerScreen : IScreenView
{
    public string ScreenKey => "ide-diff";
    public string Category => "Ide";
    public string Title => "Git Diff Viewer Screen";

    public int GetItemCount(string searchFilter) => 50;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]💻 Terminal IDE > Git Diff Viewer[/]\n\nRendering workspace git diff..."))
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
