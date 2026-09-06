namespace AgyTui.UI.Core.Abstractions;

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
