using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Database;

public class AddMigrationScreen : IScreenView
{
    public string ScreenKey => "add-migration";
    public string Title => "EF Core Add Migration";

    public int GetItemCount(string searchFilter) => 1;

    public IRenderable Render(Grid grid, ScreenState state)
    {
        grid.AddRow(new Markup("EF Core Migration Generator: Enter migration name to run 'dotnet ef migrations add <Name>'"));
        grid.AddRow(new Markup("\n[bold cyan]Title: 🗄️ EF Core > Add Migration (add-migration)[/]"));
        grid.AddRow(new Markup("[dim]Nav: Enter Execute  │  Esc Exit[/]"));
        grid.AddRow(new Markup("[bold white]Select option: [/]"));
        return grid;
    }

    public ScreenNavigationResult HandleInput(ConsoleKeyInfo key, ScreenState state)
    {
        if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Q)
        {
            return new ScreenNavigationResult(NavigationAction.Exit);
        }
        return new ScreenNavigationResult(NavigationAction.Continue);
    }
}


