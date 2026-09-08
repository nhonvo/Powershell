namespace AgyTui.UI.Core.Navigation;

public class UiNavigationHandler : IUiNavigationHandler
{
    private readonly Dictionary<string, IScreenView> _screens;
    private readonly Stack<string> _history = new();

    public UiNavigationHandler(IEnumerable<IScreenView>? screens = null)
    {
        _screens = new Dictionary<string, IScreenView>(StringComparer.OrdinalIgnoreCase);
        if (screens != null)
        {
            foreach (var screen in screens)
            {
                _screens[screen.ScreenKey] = screen;
            }
        }
    }

    public IReadOnlyCollection<string> NavigationHistory => _history.ToArray();

    public bool NavigateTo(string screenKey, params string[] args)
    {
        if (string.IsNullOrWhiteSpace(screenKey)) return false;
        PushState(screenKey);

        var query = args != null && args.Length > 0 ? args[0] : "";
        var keyLower = screenKey.ToLowerInvariant();

        string targetKey = keyLower switch
        {
            "account" or "accounts" or "agyswitch" => "agyswitch",
            "project" or "projects" or "proj" => "proj",
            "theme" or "themes" => "theme",
            "learn" or "topic" => "topic",
            _ => keyLower
        };

        if (keyLower is "palette" or "cmd-palette")
        {
            CommandPalette.Show();
            return true;
        }

        if (keyLower is "cc")
        {
            CcNavigator.Run();
            return true;
        }

        if (_screens.TryGetValue(targetKey, out var screenView))
        {
            SubPageNavigator.RunScreen(screenView, query);
            return true;
        }

        SubPageNavigator.Run(screenKey, query);
        return true;
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

