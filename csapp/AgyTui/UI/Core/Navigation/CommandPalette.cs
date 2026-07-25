namespace AgyTui.UI.Core.Navigation;

public sealed record PaletteCommand(string Alias, string Description, string Category);

public static class CommandPalette
{
    public static readonly PaletteCommand[] Commands = AgyTui.Core.Registries.CommandRegistry.All
        .Select(c => new PaletteCommand(c.Alias, c.Description, c.HelpCategory))
        .ToArray();

    public static IEnumerable<PaletteCommand>? GetFilteredCommands(int catIdx, string[] categories, PaletteCommand[] commands)
    {
        if (catIdx < 0) return null;
        if (catIdx == 0) return commands;
        return commands.Where(c => c.Category == categories[catIdx - 1]);
    }

    public static void Show()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[bold cyan]Command Palette[/]").RuleStyle("grey"));
        var categories = Commands.Select(c => c.Category).Distinct().ToArray();
        var catIdx = SpectreMenu.Show("Category", ["All", .. categories], 0, true);
        var filtered = GetFilteredCommands(catIdx, categories, Commands);
        if (filtered == null) return;

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
