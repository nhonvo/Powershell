namespace AgyTui.Infrastructure.Common;

public interface IThemeManager
{
    string? SelectThemeInteractive(string themesPath, string? currentTheme);
    string? SetTheme(string themesPath, string selectedTheme);
    string SetMobileMode(string profilePath, string targetMode);
    string ResolveStartupTheme(string themesPath);
}
