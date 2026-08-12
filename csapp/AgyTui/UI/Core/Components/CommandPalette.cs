namespace AgyTui.UI.Core.Components;

public sealed record PaletteCommand(string Alias, string Description, string Category);

public class CommandPaletteService : ICommandPalette
{
    public static readonly PaletteCommand[] Commands = CommandRegistry.All
        .Select(c => new PaletteCommand(c.Alias, c.Description, c.HelpCategory))
        .ToArray();

    public IEnumerable<PaletteCommand>? GetFilteredCommands(int catIdx, string[] categories, PaletteCommand[] commands)
    {
        if (catIdx < 0) return null;
        if (catIdx == 0) return commands;
        return commands.Where(c => c.Category == categories[catIdx - 1]);
    }

    public void Show()
    {
        CcNavigator.Run();
    }
}

public static class CommandPalette
{
    private static readonly ICommandPalette _service = new CommandPaletteService();
    public static ICommandPalette Instance => _service;

    public static readonly PaletteCommand[] Commands = CommandPaletteService.Commands;

    public static IEnumerable<PaletteCommand>? GetFilteredCommands(int catIdx, string[] categories, PaletteCommand[] commands) => _service.GetFilteredCommands(catIdx, categories, commands);
    public static void Show() => _service.Show();
}

