using Spectre.Console;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Core.Components;

public static class FooterTitleBar
{
    public static IRenderable Render(string category, string title, string navHints, string searchFilter = "")
    {
        var grid = new Grid().AddColumn(new GridColumn().NoWrap());
        string filterText = !string.IsNullOrEmpty(searchFilter) ? $" [yellow]Filter: {searchFilter.EscapeMarkup()}[/]" : "";

        grid.AddRow(new Markup($"\n[bold cyan]Title: 🛸 {category.EscapeMarkup()} > {title.EscapeMarkup()}{filterText}[/]"));
        grid.AddRow(new Markup($"[dim]Nav: {navHints.EscapeMarkup()}[/]"));
        grid.AddRow(new Markup("[bold white]Select option: [/]"));

        return grid;
    }
}
