using AgyTui.UI.Core.Navigation.Abstractions;
using AgyTui.UI.Core.Abstractions;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Workspace.Helpers;

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
            var suffix = isCurrent ? " [bold green](Active)[/]" : "";
            grid.AddRow(new Markup($"{prefix}[bold white]{themes[i].EscapeMarkup()}[/]{suffix}"));
        }

        string filterInfo = !string.IsNullOrEmpty(state.SearchFilter) ? $" [yellow]Filter: {state.SearchFilter.EscapeMarkup()}[/]" : "";
        grid.AddRow(new Markup($"\n[bold cyan]Title: 🎨 System Settings > Theme & Visual Appearance (theme){filterInfo}[/]"));
        grid.AddRow(new Markup("[dim]Nav: ↑/↓ Move  │  Enter Apply Theme  │  / Search  │  Esc Cancel[/]"));
        grid.AddRow(new Markup("[bold white]Select theme: [/]"));
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

