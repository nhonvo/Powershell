using AgyTui.UI.Core.Navigation.Interfaces;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens;

public class ThemeScreen : IScreenView
{
    public string ScreenKey => "theme";
    public string Title => "Theme & Visual Appearance";

    public int GetItemCount(string searchFilter)
    {
        var themes = SubPageThemeNavigator.GetThemeNames();
        if (string.IsNullOrEmpty(searchFilter)) return themes.Length;
        return themes.Count(t => t.Contains(searchFilter, StringComparison.OrdinalIgnoreCase));
    }

    public IRenderable Render(Grid grid, ScreenState state)
    {
        grid.AddRow(new Markup("[cyan bold]Select Visual Theme:[/]\n"));
        if (!string.IsNullOrEmpty(state.SearchFilter))
        {
            grid.AddRow(new Markup($"[yellow]Search:[/] [white]{state.SearchFilter.EscapeMarkup()}[/]_\n"));
        }
        var themes = SubPageThemeNavigator.GetThemeNames();
        if (!string.IsNullOrEmpty(state.SearchFilter))
        {
            themes = themes.Where(t => t.Contains(state.SearchFilter, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        var currentTheme = Environment.GetEnvironmentVariable("THEME");

        for (var i = 0; i < themes.Length; i++)
        {
            var isSelected = (i == state.SelectedIndex);
            var isCurrent = string.Equals(themes[i], currentTheme, StringComparison.OrdinalIgnoreCase);
            var prefix = isSelected ? "[green bold]> [/]" : "  ";
            var suffix = isCurrent ? " [green](Active)[/]" : "";
            grid.AddRow(new Markup($"{prefix}[bold white]{themes[i].EscapeMarkup()}[/]{suffix}"));
        }
        grid.AddRow(new Markup("\n[dim]↑/↓ Navigate  ·  Enter Apply Theme  ·  Esc Cancel[/]"));
        return grid;
    }

    public ScreenNavigationResult HandleInput(ConsoleKeyInfo key, ScreenState state)
    {
        var themes = SubPageThemeNavigator.GetThemeNames();
        if (!string.IsNullOrEmpty(state.SearchFilter))
        {
            themes = themes.Where(t => t.Contains(state.SearchFilter, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        switch (key.Key)
        {
            case ConsoleKey.Enter:
                SubPageThemeNavigator.HandleSelection(state.SearchFilter, state.SelectedIndex);
                return new ScreenNavigationResult(NavigationAction.Handled);

            case ConsoleKey.Escape:
                return new ScreenNavigationResult(NavigationAction.Exit);
        }

        return new ScreenNavigationResult(NavigationAction.Continue);
    }
}
