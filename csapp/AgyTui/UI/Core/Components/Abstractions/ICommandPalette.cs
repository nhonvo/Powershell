namespace AgyTui.UI.Core.Components.Abstractions;

public interface ICommandPalette
{
    void Show();
    IEnumerable<PaletteCommand>? GetFilteredCommands(int catIdx, string[] categories, PaletteCommand[] commands);
}
