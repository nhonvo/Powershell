using AgyTui.UI.Core.Navigation.Abstractions;
using AgyTui.UI.Core.Abstractions;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Workspace.Helpers;

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
        var filtered = string.IsNullOrEmpty(state.SearchFilter)
            ? Topics
            : Topics.Where(t => t.Contains(state.SearchFilter, StringComparison.OrdinalIgnoreCase)).ToArray();

        for (var i = 0; i < filtered.Length; i++)
        {
            var isSelected = (i == state.SelectedIndex);
            var prefix = isSelected ? "[green bold]> [/]" : "  ";
            grid.AddRow(new Markup($"{prefix}[bold white]{filtered[i].EscapeMarkup()}[/]"));
        }

        string filterInfo = !string.IsNullOrEmpty(state.SearchFilter) ? $" [yellow]Filter: {state.SearchFilter.EscapeMarkup()}[/]" : "";
        grid.AddRow(new Markup($"\n[bold cyan]Title: 🎯 Learning Suite > AI Learning & Topic Selector (topic){filterInfo}[/]"));
        grid.AddRow(new Markup("[dim]Nav: ↑/↓ Move  │  Enter Select Topic  │  / Search  │  Esc Cancel[/]"));
        grid.AddRow(new Markup("[bold white]Select topic: [/]"));
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

