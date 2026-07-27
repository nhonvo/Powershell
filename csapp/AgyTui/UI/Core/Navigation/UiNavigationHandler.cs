namespace AgyTui.UI.Core.Navigation;

using AgyTui.UI.Core.Navigation.Interfaces;

public class UiNavigationHandler : IUiNavigationHandler
{
    private readonly Stack<string> _history = new();

    public IReadOnlyCollection<string> NavigationHistory => _history.ToArray();

    public bool NavigateTo(string screenKey, params string[] args)
    {
        if (string.IsNullOrWhiteSpace(screenKey)) return false;
        PushState(screenKey);

        var query = args != null && args.Length > 0 ? args[0] : "";
        switch (screenKey.ToLowerInvariant())
        {
            case "account":
            case "accounts":
            case "agyswitch":
                SubPageNavigator.Run("agyswitch", query);
                return true;

            case "project":
            case "projects":
            case "proj":
                SubPageNavigator.Run("proj", query);
                return true;

            case "theme":
            case "themes":
                SubPageNavigator.Run("theme", query);
                return true;

            case "palette":
            case "cmd-palette":
                CommandPalette.Show();
                return true;

            case "cc":
                CcNavigator.Run();
                return true;

            case "learn":
            case "topic":
                SubPageNavigator.Run("topic", query);
                return true;

            default:
                SubPageNavigator.Run(screenKey, query);
                return true;
        }
    }

    public void PushState(string screenKey)
    {
        if (!string.IsNullOrWhiteSpace(screenKey))
        {
            _history.Push(screenKey);
        }
    }

    public string? PopState()
    {
        return _history.Count > 0 ? _history.Pop() : null;
    }

    public void ShowAccountSelector() => NavigateTo("agyswitch");
    public void ShowProjectSelector() => NavigateTo("proj");
    public void ShowThemeSelector() => NavigateTo("theme");
    public void LaunchCommandPalette() => NavigateTo("palette");
    public void LaunchCcNavigator() => NavigateTo("cc");
}
