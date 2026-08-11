using Spectre.Console;
using Spectre.Console.Rendering;
using AgyTui.UI.Core.Layouts;
using AgyTui.UI.Core.Navigation.Abstractions;
using AgyTui.UI.Core.Abstractions;

namespace AgyTui.UI.Screens.Scaffolder;

public class ScaffoldScreen : IScreenView
{
    public string ScreenKey => "scaffold";
    public string Title => "Project Scaffolder";

    private static readonly string[] Templates = new[]
    {
        "webapi      — .NET Web API with Controller / Minimal API setup",
        "console     — Modern .NET Console App with Dependency Injection",
        "react       — React + TypeScript Web App (scaffolds via Vite)",
        "blazorwasm  — Blazor WebAssembly Standalone Application",
        "classlib    — .NET Class Library for reusable NuGet packages",
        "worker      — Background Worker Service process template"
    };

    public int GetItemCount(string searchFilter)
    {
        if (string.IsNullOrEmpty(searchFilter)) return Templates.Length;
        return Templates.Count(t => t.Contains(searchFilter, StringComparison.OrdinalIgnoreCase));
    }

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var items = string.IsNullOrEmpty(state.SearchFilter)
            ? Templates
            : Templates.Where(t => t.Contains(state.SearchFilter, StringComparison.OrdinalIgnoreCase)).ToArray();

        for (int i = 0; i < items.Length; i++)
        {
            var isSelected = (i == state.SelectedIndex);
            var prefix = isSelected ? "[green bold]> [/]" : "  ";
            var lineMarkup = isSelected ? $"[bold green]{items[i].EscapeMarkup()}[/]" : $"[white]{items[i].EscapeMarkup()}[/]";
            grid.AddRow(new Markup($"{prefix}{lineMarkup}"));
        }

        string filterInfo = !string.IsNullOrEmpty(state.SearchFilter) ? $" [yellow]Filter: {state.SearchFilter.EscapeMarkup()}[/]" : "";
        grid.AddRow(new Markup($"\n[bold cyan]Title: 🔨 Project Scaffolder > Select Boilerplate Template (scaffold){filterInfo}[/]"));
        grid.AddRow(new Markup("[dim]Nav: [[1-6]] Select Template  │  ↑/↓ Move  │  Enter Confirm  │  Esc Cancel[/]"));
        grid.AddRow(new Markup("[bold white]Select template [1-6] or Esc Cancel: [/]"));

        return grid;
    }

    public ScreenNavigationResult HandleInput(ConsoleKeyInfo key, ScreenState state)
    {
        if (key.Key == ConsoleKey.Escape)
        {
            return new ScreenNavigationResult(NavigationAction.Exit);
        }

        if (key.Key == ConsoleKey.Enter)
        {
            SpectrePanel.Success("Scaffolding selected project template...");
            Thread.Sleep(1000);
            return new ScreenNavigationResult(NavigationAction.Handled);
        }

        return new ScreenNavigationResult(NavigationAction.Continue);
    }
}

