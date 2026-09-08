using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Customization.Navigators;

public static class SubPageThemeNavigator
{
    public static string[] GetThemeNames()
    {
        var themesPath = Environment.GetEnvironmentVariable("POSH_THEMES_PATH");
        if (string.IsNullOrEmpty(themesPath) || !Directory.Exists(themesPath))
        {
            themesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asset", "powershell-themes");
            if (!Directory.Exists(themesPath))
            {
                themesPath = Path.Combine(Directory.GetCurrentDirectory(), "asset", "powershell-themes");
            }
        }
        if (!Directory.Exists(themesPath)) return Array.Empty<string>();
        return Directory.GetFiles(themesPath, "*.omp.json").Select(f => Path.GetFileName(f).Replace(".omp.json", "")).OrderBy(f => f).ToArray();
    }

    public static bool HandleSelection(string searchBuffer, int detailsSel, IThemeManager? themeManager = null)
    {
        var themeNames = GetThemeNames();
        if (!string.IsNullOrEmpty(searchBuffer))
        {
            themeNames = themeNames.Where(t => t.Contains(searchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        if (detailsSel < 0 || detailsSel >= themeNames.Length) return false;

        var selectedTheme = themeNames[detailsSel];
        var themesPath = Environment.GetEnvironmentVariable("POSH_THEMES_PATH");
        if (string.IsNullOrEmpty(themesPath))
        {
            themesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asset", "powershell-themes");
            if (!Directory.Exists(themesPath))
            {
                themesPath = Path.Combine(Directory.GetCurrentDirectory(), "asset", "powershell-themes");
            }
        }

        var themePath = (themeManager ?? new ThemeManager()).SetTheme(themesPath, selectedTheme);
        if (!string.IsNullOrEmpty(themePath))
        {
            var agyHome = AppPaths.GeminiHome;
            Directory.CreateDirectory(agyHome);
            var selectedThemeFile = Path.Combine(agyHome, "selected_theme.txt");
            File.WriteAllText(selectedThemeFile, themePath);
        }
        SpectrePanel.Success($"Selected theme '{selectedTheme}'. Theme will apply on exit.");
        Thread.Sleep(1000);
        return true;
    }

    public static IRenderable Render(Grid grid, string searchBuffer, int selIdx)
    {
        var themeNames = GetThemeNames();
        var filtered = string.IsNullOrEmpty(searchBuffer)
            ? themeNames
            : themeNames.Where(t => t.Contains(searchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();

        var currentTheme = Environment.GetEnvironmentVariable("THEME");

        int maxRows = 12;
        int topRow = 0;
        int endRow = 0;

        if (filtered.Length == 0)
        {
            grid.AddRow(new Markup($"  [dim]No themes matching '{searchBuffer.EscapeMarkup()}'.[/]"));
        }
        else
        {
            (topRow, endRow) = ScrollableListView.ComputeViewport(filtered.Length, selIdx, maxRows);

            for (var i = topRow; i < endRow; i++)
            {
                var isSelected = (i == selIdx);
                var isActive = string.Equals(filtered[i], currentTheme, StringComparison.OrdinalIgnoreCase);
                var prefix = isSelected ? "[green bold]> [/]" : "  ";
                var suffix = isActive ? " [bold green](Active)[/]" : "";
                var nameMarkup = isSelected ? $"[bold green]{filtered[i].EscapeMarkup()}[/]" : $"[white]{filtered[i].EscapeMarkup()}[/]";
                grid.AddRow(new Markup($"{prefix}{nameMarkup}{suffix}"));
            }
        }

        string filterLine = !string.IsNullOrEmpty(searchBuffer) ? $" [yellow]Filter: {searchBuffer.EscapeMarkup()}[/]" : "";
        grid.AddRow(new Markup($"\n[bold cyan]Title: 🎨 System Settings > Theme & Visual Appearance (theme){filterLine}[/]"));
        grid.AddRow(new Markup("[dim]Nav: ↑/↓ Move  │  Enter Apply Theme  │  / Search  │  Esc Cancel[/]"));
        grid.AddRow(new Markup("[bold white]Select theme: [/]"));

        return grid;
    }
}
