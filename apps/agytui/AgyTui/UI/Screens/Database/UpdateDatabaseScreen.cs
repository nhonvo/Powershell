using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Database;

public sealed class UpdateDatabaseScreen : IScreenView
{
    public string ScreenKey => "update-db";
    public string Category => "Database";
    public string Title => "EF Core Update Database";

    public int GetItemCount(string searchFilter) => 1;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var panel = new Panel(new Markup("[bold cyan]🗄️ EF Core > Update Database[/]\n\nApplying pending EF Core migrations to database..."))
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
