using Spectre.Console.Rendering;

namespace AgyTui.UI.Core.Abstractions;

public interface IScreenView
{
    string ScreenKey { get; }
    string Category => "General";
    string Title { get; }
    int GetItemCount(string searchFilter);
    IRenderable Render(Grid grid, ScreenState state);
    ScreenNavigationResult HandleInput(ConsoleKeyInfo key, ScreenState state);
}
