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
        CcNavigator.Run();
    }
}
