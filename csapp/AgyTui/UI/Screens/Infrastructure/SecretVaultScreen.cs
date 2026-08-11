using Spectre.Console;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Infrastructure;

public sealed class SecretVaultScreen : IScreenView
{
    public string ScreenKey => "agy-vault";
    public string Category => "Infrastructure";
    public string Title => "DPAPI Encrypted Secret Store";

    public int GetItemCount(string searchFilter) => 5;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]🔒 AGY Vault > Encrypted Secrets[/]\n\nManaging DPAPI protected credential vault..."))
        {
            Border = BoxBorder.Rounded,
            Expand = true
        };
        return panel;
    }

    public ScreenNavigationResult HandleInput(ConsoleKeyInfo key, ScreenState state)
    {
        if (key.Key == ConsoleKey.Escape)
            return new ScreenNavigationResult(NavigationAction.Exit);

        return new ScreenNavigationResult(NavigationAction.Continue);
    }
}
