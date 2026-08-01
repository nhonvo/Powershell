using AgyTui.UI.Core.Navigation.Interfaces;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Workspace;

public class TopicScreen : IScreenView
{
    private static readonly string[] Topics = ["jp", "en", "cs", "dsa", "interview", "[Type Custom Topic...]"];

    public string ScreenKey => "topic";
    public string Title => "AI Learning & Topic Selector";

    public int GetItemCount(string searchFilter)
    {
        if (string.IsNullOrEmpty(searchFilter)) return Topics.Length;
        return Topics.Count(t => t.Contains(searchFilter, StringComparison.OrdinalIgnoreCase));
    }

    public IRenderable Render(Grid grid, ScreenState state)
    {
        grid.AddRow(new Markup("[cyan bold]Select Learning Topic:[/]\n"));
        if (!string.IsNullOrEmpty(state.SearchFilter))
        {
            grid.AddRow(new Markup($"[yellow]Search:[/] [white]{state.SearchFilter.EscapeMarkup()}[/]_\n"));
        }
        var filtered = string.IsNullOrEmpty(state.SearchFilter)
            ? Topics
            : Topics.Where(t => t.Contains(state.SearchFilter, StringComparison.OrdinalIgnoreCase)).ToArray();

        for (var i = 0; i < filtered.Length; i++)
        {
            var isSelected = (i == state.SelectedIndex);
            var prefix = isSelected ? "[green bold]> [/]" : "  ";
            grid.AddRow(new Markup($"{prefix}[bold white]{filtered[i].EscapeMarkup()}[/]"));
        }
        grid.AddRow(new Markup("\n[dim]↑/↓ Navigate  ·  Enter Select Topic  ·  Esc Cancel[/]"));
        return grid;
    }

    public ScreenNavigationResult HandleInput(ConsoleKeyInfo key, ScreenState state)
    {
        var filtered = string.IsNullOrEmpty(state.SearchFilter)
            ? Topics
            : Topics.Where(t => t.Contains(state.SearchFilter, StringComparison.OrdinalIgnoreCase)).ToArray();

        switch (key.Key)
        {
            case ConsoleKey.Enter:
                SubPageTopicNavigator.HandleSelection("learn", state.SearchFilter, state.SelectedIndex);
                return new ScreenNavigationResult(NavigationAction.Handled);

            case ConsoleKey.Escape:
                return new ScreenNavigationResult(NavigationAction.Exit);
        }

        return new ScreenNavigationResult(NavigationAction.Continue);
    }
}
