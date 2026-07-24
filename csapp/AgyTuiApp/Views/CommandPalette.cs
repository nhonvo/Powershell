using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public sealed record PaletteCommand(string Alias, string Description, string Category);

public static class CommandPalette
{
    public static readonly PaletteCommand[] Commands = AgyTui.Registry.CommandRegistry.All
        .Select(c => new PaletteCommand(c.Alias, c.Description, c.HelpCategory))
        .ToArray();

    public static void Show()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[bold cyan]Command Palette[/]").RuleStyle("grey"));
        var categories = Commands.Select(c => c.Category).Distinct().ToArray();
        var catIdx = SpectreMenu.Show("Category", ["All", .. categories], 0, true);
        IEnumerable<PaletteCommand> filtered = catIdx <= 0 ? Commands : Commands.Where(c => c.Category == categories[catIdx - 1]);
        var items = filtered.Select(c => $"{c.Alias,-20} {c.Description}").ToArray();
        var cmds = filtered.Select(c => c.Alias).ToArray();
        var selected = SpectreMenu.Show(["Command Palette", "Select a command to view details"], items, cmds, 0, true, false);
        if (selected >= 0)
        {
            var cmd = filtered.ElementAt(selected);
            AnsiConsole.Write(new Panel($"[bold]Alias:[/] {cmd.Alias.EscapeMarkup()}\n" + $"[bold]Category:[/] {cmd.Category.EscapeMarkup()}\n" + $"[bold]Description:[/] {cmd.Description.EscapeMarkup()}")
            {
                Header = new PanelHeader("[bold cyan]Command Details[/]"),
                Border = BoxBorder.Rounded
            });
        }
    }
}
