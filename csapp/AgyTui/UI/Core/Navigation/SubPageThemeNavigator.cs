using Spectre.Console.Rendering;
using AgyTui.Infrastructure.Di;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.UI.Core.Navigation;

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

        grid.AddRow(new Markup($"[cyan bold]Select Oh My Posh Theme[/] [dim]({filtered.Length}/{themeNames.Length} themes)[/]:\n"));
        if (!string.IsNullOrEmpty(searchBuffer))
        {
            grid.AddRow(new Markup($"[yellow]Search:[/] [white]{searchBuffer.EscapeMarkup()}[/]_\n"));
        }
        else
        {
            grid.AddRow(new Markup("[dim]Type to filter themes (Esc to clear / cancel)[/]\n"));
        }

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
                var suffix = isActive ? " [bold green][[ACTIVE]][/]" : "";
                var nameMarkup = isSelected ? $"[bold green]{filtered[i].EscapeMarkup()}[/]" : $"[white]{filtered[i].EscapeMarkup()}[/]";
                grid.AddRow(new Markup($"{prefix}{nameMarkup}{suffix}"));
            }
        }

        string scrollStatus = "";
        if (filtered.Length > maxRows)
        {
            var aboveStr = topRow > 0 ? $"[yellow]▲ {topRow} items above[/]" : "[grey]▲ Start of list[/]";
            var belowStr = (endRow < filtered.Length) ? $"[yellow]▼ {filtered.Length - endRow} items below[/]" : "[grey]▼ End of list[/]";
            scrollStatus = $"  {aboveStr}   ·   {belowStr}";
        }
        else
        {
            scrollStatus = "  [grey]▲ Start of list   ·   ▼ End of list[/]";
        }

        return new Rows(
            grid,
            new Rule().RuleStyle("cyan dim"),
            new Markup(scrollStatus),
            new Markup("\n[dim]↑/↓/j/k Navigate  ·  PgDn/PgUp Page  ·  Enter Select  ·  Esc Cancel[/]")
        );
    }
}
