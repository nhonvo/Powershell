using Spectre.Console;
using Spectre.Console.Rendering;
using AgyTui.UI.Core.Navigation.Abstractions;
using AgyTui.UI.Core.Abstractions;

namespace AgyTui.UI.Screens.Diagnostics;

public class SystemDiagnosticLogScreen : IScreenView
{
    public string ScreenKey => "/log";
    public string Title => "System Diagnostic Log Viewer";

    private static readonly string[] LogEntries = new[]
    {
        "[07:25:01] [INFO] [Bootstrapper] Antigravity TUI Engine initialized.",
        "[07:25:02] [INFO] [ConfigService] Configuration loaded successfully.",
        "[07:25:02] [INFO] [AgyAccountStore] Active account resolved: default (✔ Logged In)",
        "[07:25:03] [DEBUG] [OllamaClient] Daemon status checked: ONLINE",
        "[07:25:04] [INFO] [GitNexus] Workspace repository scan completed."
    };

    public int GetItemCount(string searchFilter) => LogEntries.Length;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        foreach (var entry in LogEntries)
        {
            grid.AddRow(new Markup(entry));
        }

        string filterInfo = !string.IsNullOrEmpty(state.SearchFilter) ? $" [yellow]Filter: {state.SearchFilter.EscapeMarkup()}[/]" : "";
        grid.AddRow(new Markup($"\n[bold cyan]Title: 📑 System Diagnostic Log Viewer (/log){filterInfo}[/]"));
        grid.AddRow(new Markup("[dim]Nav: ↑/↓ Scroll  │  / Search  │  Esc Exit[/]"));
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


