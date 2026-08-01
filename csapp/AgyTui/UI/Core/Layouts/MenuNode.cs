namespace AgyTui.UI.Core.Layouts;

public enum MenuNodeKind
{
    Category,
    Group,
    Command,
    Separator,
    Exit
}

public sealed record MenuNode(
    string Id,
    string Label,
    MenuNodeKind Kind,
    MenuNode[] Children,
    CommandEntry? Command
)
{
    public string SearchKey { get; } = Label.ToLowerInvariant();
}

public static class MenuNodeBuilder
{
    public static MenuNode BuildTree()
    {
        var visibleCommands = CommandRegistry.All
            .Where(c => c.ShowInTree)
            .ToList();

        var categoryNames = new[]
        {
            "[Favorites]",
            "[Workspace & Dev]",
            "[AI Agent & Ollama]",
            "[AGY Account Switch]",
            "[Learn & Study]",
            "[Obsidian & Resources]",
            "[Appearance & Layout]",
            "[System & Network]",
            "[Help & Docs]"
        };

        var favoriteAliases = AgyTui.Infrastructure.Configuration.Config.Current.Ui.FavoriteAliases ?? AgyTui.Infrastructure.Configuration.Config.DefaultFavoriteAliases;

        var categoryNodes = new List<MenuNode>();

        foreach (var catName in categoryNames)
        {
            if (catName == "[Favorites]")
            {
                var favCommands = favoriteAliases
                    .Select(a => CommandRegistry.GetByAlias(a))
                    .Where(c => c != null)
                    .Select(c => CreateCommandNode(c!))
                    .ToArray();

                if (favCommands.Length > 0)
                {
                    categoryNodes.Add(new MenuNode("favorites", "[Favorites]", MenuNodeKind.Category, favCommands, null));
                }
                continue;
            }

            var catCommands = visibleCommands
                .Where(c => string.Equals(c.Category, catName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (catCommands.Count == 0) continue;

            // Direct children (ungrouped)
            var ungrouped = catCommands
                .Where(c => string.IsNullOrEmpty(c.GroupPath))
                .Select(CreateCommandNode)
                .ToList();

            // Grouped children (split comma-separated paths for multi-group commands)
            var groupedCommands = catCommands
                .Where(c => !string.IsNullOrEmpty(c.GroupPath))
                .SelectMany(c => c.GroupPath!.Split(',').Select(path => new { Path = path.Trim(), Command = c }))
                .GroupBy(x => x.Path)
                .ToList();

            var groupsList = new List<MenuNode>();
            foreach (var group in groupedCommands)
            {
                var groupPath = group.Key;
                var groupLabel = groupPath switch
                {
                    "/jp-suite" => "Japanese Suite",
                    "/english-vocab" => "English & Vocab",
                    "/csharp-master" => "C# & Dev Masterclass",
                    "/dsa-architect" => "DSA & System Design",
                    "/career-interview" => "Career & Interview Prep",
                    "/obsidian-vault" => "Obsidian Vault & Sync",
                    "/git-tools" => "Git & Repo Tools",
                    "/dotnet-tools" => ".NET Project Tools",
                    "/docker-tools" => "Docker Tools",
                    "/aws-tools" => "AWS Tools",
                    "/claude-agents" => "Claude Agents",
                    "/codex-agents" => "Codex Agents",
                    "/ollama-tools" => "Ollama Tools",
                    "/antigravity-deck" => "Antigravity Deck (Desk)",
                    "/antigravity-manager" => "Antigravity Manager",
                    "/ssh-tailscale" => "SSH & Tailscale",
                    "/system-reload" => "System & Terminal Reload",
                    "/quota-views" => "Quota Views",
                    "/track" => "Track & Progress",
                    _ => group.First().Command.GroupName ?? groupPath
                };

                var formattedLabel = groupPath.StartsWith("/") ? $" [{groupPath}] {groupLabel}" : $" [{groupLabel}]";
                if (groupPath == "/jp-suite" || groupPath == "/english-vocab" || groupPath == "/csharp-master" || groupPath == "/dsa-architect" || groupPath == "/career-interview" || groupPath == "/obsidian-vault" || groupPath == "/git-tools" || groupPath == "/track")
                {
                    formattedLabel = $" [{groupLabel}]";
                }

                var groupItems = group
                    .Select(x => x.Command)
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.DisplayName)
                    .Select(CreateCommandNode)
                    .ToArray();

                var groupNode = new MenuNode(
                    groupPath,
                    formattedLabel,
                    MenuNodeKind.Group,
                    groupItems,
                    null
                );

                groupsList.Add(groupNode);
            }

            var groupOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["/git-tools"] = 10,
                ["/dotnet-tools"] = 20,
                ["/docker-tools"] = 30,
                ["/aws-tools"] = 40,
                ["/ollama-tools"] = 10,
                ["/secret-vault"] = 10,
                ["/quota-views"] = 20,
                ["/account-toggles"] = 30,
                ["/antigravity-deck"] = 40,
                ["/antigravity-manager"] = 50,
                ["/track"] = 10,
                ["/obsidian-vault"] = 20,
                ["/jp-suite"] = 30,
                ["/english-vocab"] = 40,
                ["/csharp-master"] = 50,
                ["/dsa-architect"] = 60,
                ["/career-interview"] = 70,
                ["/ssh-tools"] = 10,
                ["/system-reload"] = 20
            };

            // Combine all children (both ungrouped and groups) and sort them strictly
            var allChildren = new List<MenuNode>();
            allChildren.AddRange(ungrouped.OrderBy(u => u.Command!.SortOrder));
            allChildren.AddRange(groupsList.OrderBy(g => groupOrder.GetValueOrDefault(g.Id, 9999)));

            var sortedChildrenArray = allChildren.ToArray();

            var catId = catName.Trim('[', ']').ToLowerInvariant().Replace(" & ", "-").Replace(" ", "-");

            var catNode = new MenuNode(
                catId,
                catName,
                MenuNodeKind.Category,
                sortedChildrenArray,
                null
            );

            categoryNodes.Add(catNode);
        }

        var sep = new MenuNode(
            "separator",
            "────────────────────────────",
            MenuNodeKind.Separator,
            Array.Empty<MenuNode>(),
            null
        );

        var exit = new MenuNode(
            "exit",
            "[Exit] Exit Control Center",
            MenuNodeKind.Exit,
            Array.Empty<MenuNode>(),
            null
        );

        var finalChildren = new List<MenuNode>(categoryNodes);
        finalChildren.Add(sep);
        finalChildren.Add(exit);

        return new MenuNode(
            "root",
            "Control Center Root",
            MenuNodeKind.Category,
            finalChildren.ToArray(),
            null
        );
    }

    private static MenuNode CreateCommandNode(CommandEntry entry)
    {
        return new MenuNode(
            entry.Alias,
            $"/{entry.Alias} — {entry.DisplayName}",
            MenuNodeKind.Command,
            Array.Empty<MenuNode>(),
            entry
        );
    }
}


