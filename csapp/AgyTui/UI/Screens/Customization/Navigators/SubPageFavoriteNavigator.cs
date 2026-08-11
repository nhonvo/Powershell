using Spectre.Console;
using Spectre.Console.Rendering;
using AgyTui.Infrastructure.Di;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.UI.Screens.Customization.Navigators;

public static class SubPageFavoriteNavigator
{
    public record FavoriteItem(string Alias, string DisplayName, bool IsAction);

    public static List<FavoriteItem> GetFavoriteItems(string searchBuffer = "")
    {
        var currentFavs = (Config.Current.Ui.FavoriteAliases ?? Config.DefaultFavoriteAliases).ToList();
        var list = new List<FavoriteItem>();

        foreach (var favAlias in currentFavs)
        {
            var cmd = CommandRegistry.GetByAlias(favAlias);
            var nameCol = cmd != null ? cmd.DisplayName : "Custom Alias";

            if (string.IsNullOrEmpty(searchBuffer) ||
                favAlias.Contains(searchBuffer, StringComparison.OrdinalIgnoreCase) ||
                nameCol.Contains(searchBuffer, StringComparison.OrdinalIgnoreCase))
            {
                list.Add(new FavoriteItem(favAlias, nameCol, false));
            }
        }

        if (string.IsNullOrEmpty(searchBuffer) || "Add New Favorite".Contains(searchBuffer, StringComparison.OrdinalIgnoreCase))
        {
            list.Add(new FavoriteItem("add", "➕ Add New Favorite", true));
        }

        if (string.IsNullOrEmpty(searchBuffer) || "Reset Favorites to Defaults".Contains(searchBuffer, StringComparison.OrdinalIgnoreCase))
        {
            list.Add(new FavoriteItem("reset", "🔄 Reset Favorites to Defaults", true));
        }

        return list;
    }

    public static bool HandleSelection(string searchBuffer, int selIdx)
    {
        var items = GetFavoriteItems(searchBuffer);
        if (selIdx < 0 || selIdx >= items.Count) return false;

        var item = items[selIdx];

        if (item.IsAction)
        {
            if (item.Alias == "add")
            {
                AddNewFavorite();
                return false;
            }
            else if (item.Alias == "reset")
            {
                ResetFavorites();
                return false;
            }
        }
        else
        {
            var router = Bootstrapper.ServiceProvider?.GetService<ICommandRouter>();
            router?.Execute(item.Alias, Array.Empty<string>());
            return true;
        }

        return false;
    }

    public static void AddNewFavorite()
    {
        var currentFavs = (Config.Current.Ui.FavoriteAliases ?? Config.DefaultFavoriteAliases).ToList();
        var availableCmds = CommandRegistry.All
            .Where(c => c.ShowInTree && !currentFavs.Contains(c.Alias, StringComparer.OrdinalIgnoreCase) && c.Alias != "favorite")
            .OrderBy(c => c.Alias)
            .ToList();

        if (availableCmds.Count == 0)
        {
            SpectrePanel.Info("All available commands are already in Favorites.");
            Thread.Sleep(1200);
            return;
        }

        var addChoices = availableCmds
            .Select(c => $"{c.Alias.PadRight(22)} │ {Markup.Escape(c.DisplayName)}")
            .Concat(["⬅️ Back"])
            .ToList();

        var addSelected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select command to [green]add[/] to Favorites:")
                .PageSize(15)
                .EnableSearch()
                .SearchPlaceholderText("[grey](Type to search commands...)[/]")
                .AddChoices(addChoices));

        if (addSelected != "⬅️ Back")
        {
            var selectedAlias = addSelected.Split('│')[0].Trim();
            if (!currentFavs.Contains(selectedAlias, StringComparer.OrdinalIgnoreCase))
            {
                currentFavs.Add(selectedAlias);
                Config.Current.Ui.FavoriteAliases = [.. currentFavs];
                Config.Save();
                SpectrePanel.Success($"Added '{selectedAlias}' to Favorites (saved to SQLite DB).");
                Thread.Sleep(1200);
            }
        }
    }

    public static void ResetFavorites()
    {
        Console.CursorVisible = true;
        AnsiConsole.Clear();
        if (AnsiConsole.Confirm("Are you sure you want to reset Favorites to default list?"))
        {
            Config.Current.Ui.FavoriteAliases = [.. Config.DefaultFavoriteAliases];
            Config.Save();
            SpectrePanel.Success($"Reset Favorites to default list.");
            Thread.Sleep(1200);
        }
        Console.CursorVisible = false;
    }

    public static IRenderable Render(Grid grid, string searchBuffer, int selIdx)
    {
        var items = GetFavoriteItems(searchBuffer);

        for (var i = 0; i < items.Count; i++)
        {
            var isSelected = (i == selIdx);
            var prefix = isSelected ? "[green bold]> [/]" : "  ";
            var item = items[i];

            if (item.IsAction)
            {
                var nameMarkup = isSelected ? $"[bold green]{item.DisplayName.EscapeMarkup()}[/]" : $"[dim white]{item.DisplayName.EscapeMarkup()}[/]";
                grid.AddRow(new Markup($"{prefix}{nameMarkup}"));
            }
            else
            {
                var aliasCol = item.Alias.PadRight(22);
                var nameCol = item.DisplayName;
                var lineMarkup = isSelected
                    ? $"⚡ [bold green]{aliasCol.EscapeMarkup()}[/] │ [bold white]{nameCol.EscapeMarkup()}[/]"
                    : $"⚡ [cyan]{aliasCol.EscapeMarkup()}[/] │ [white]{nameCol.EscapeMarkup()}[/]";
                grid.AddRow(new Markup($"{prefix}{lineMarkup}"));
            }
        }

        string filterInfo = !string.IsNullOrEmpty(searchBuffer) ? $" [yellow]Filter: {searchBuffer.EscapeMarkup()}[/]" : "";
        grid.AddRow(new Markup($"\n[bold cyan]Title: ⭐️ Favorites Manager > Quick Command Launcher (favorite){filterInfo}[/]"));
        grid.AddRow(new Markup("[dim]Nav: ↑/↓ Move  │  Enter Execute / Select  │  [[a]] Add  │  [[r]] Reset  │  / Search  │  Esc Exit[/]"));
        grid.AddRow(new Markup("[bold white]Select favorite: [/]"));

        return grid;
    }
}
