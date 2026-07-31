using Spectre.Console.Rendering;

namespace AgyTui.UI.Core.Navigation.Interfaces;

public enum NavigationAction
{
    Continue,
    Exit,
    SwitchScreen,
    Handled
}

public sealed record ScreenState(
    string SearchFilter,
    int SelectedIndex,
    int ExtraIndex = -1
);

public sealed record ScreenNavigationResult(
    NavigationAction Action,
    string? TargetScreen = null,
    string? InitialQuery = null
);

public interface IScreenView
{
    string ScreenKey { get; }
    string Title { get; }
    int GetItemCount(string searchFilter);
    IRenderable Render(Grid grid, ScreenState state);
    ScreenNavigationResult HandleInput(ConsoleKeyInfo key, ScreenState state);
}
