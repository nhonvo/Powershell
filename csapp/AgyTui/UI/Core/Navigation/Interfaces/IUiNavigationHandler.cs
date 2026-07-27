namespace AgyTui.UI.Core.Navigation.Interfaces;

public interface IUiNavigationHandler
{
    bool NavigateTo(string screenKey, params string[] args);
    void PushState(string screenKey);
    string? PopState();
    void ShowAccountSelector();
    void ShowProjectSelector();
    void ShowThemeSelector();
    void LaunchCommandPalette();
    void LaunchCcNavigator();
    IReadOnlyCollection<string> NavigationHistory { get; }
}
