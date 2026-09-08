using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.GitNexus;

public class MultiRepoSyncScreen : IScreenView
{
    public string ScreenKey => "gbr";
    public string Title => "Multi-Workspace Sync & Branch Status";

    private static readonly string[] Repos = new[]
    {
        "📦 Powershell          │ branch: main       │ status: ✔ Clean (Up to date)",
        "📦 csapp               │ branch: main       │ status: ⚡ 2 commits ahead",
        "📦 psapp               │ branch: develop    │ status: ✔ Clean"
    };

    public int GetItemCount(string searchFilter) => Repos.Length;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        for (int i = 0; i < Repos.Length; i++)
        {
            var isSel = i == state.SelectedIndex;
            var prefix = isSel ? "[green bold]> [/]" : "  ";
            var lineMarkup = isSel ? $"[bold green]{Repos[i].EscapeMarkup()}[/]" : $"[white]{Repos[i].EscapeMarkup()}[/]";
            grid.AddRow(new Markup($"{prefix}{lineMarkup}"));
        }

        grid.AddRow(new Markup("\n[bold cyan]Title: 🐙 Git Nexus > Multi-Workspace Sync & Branch Status (gbr)[/]"));
        grid.AddRow(new Markup("[dim]Nav: ↑/↓ Move  │  Enter Sync All  │  Esc Back[/]"));
        grid.AddRow(new Markup("[bold white]Select option: [/]"));
        return grid;
    }

    public ScreenNavigationResult HandleInput(ConsoleKeyInfo key, ScreenState state)
    {
        if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Q)
        {
            return new ScreenNavigationResult(NavigationAction.Exit);
        }
        return new ScreenNavigationResult(NavigationAction.Continue);
    }
}

