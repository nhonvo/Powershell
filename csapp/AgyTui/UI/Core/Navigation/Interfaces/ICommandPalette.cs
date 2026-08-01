namespace AgyTui.UI.Core.Navigation.Interfaces;

public interface ICommandPalette
{
    void Show();
    IEnumerable<PaletteCommand>? GetFilteredCommands(int catIdx, string[] categories, PaletteCommand[] commands);
}
