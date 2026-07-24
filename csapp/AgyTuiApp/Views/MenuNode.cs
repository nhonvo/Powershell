using System;
using System.Collections.Generic;
using System.Linq;
using AgyTui.Registry;

namespace AgyTui;

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
            "[Workspace & Dev]",
            "[AI Agent & Ollama]",
            "[AGY Account Switch]",
            "[System & Network]",
            "[Learn & Study]",
            "[Track & Progress]",
            "[Obsidian & Resources]",
            "[Appearance & Layout]",
            "[Help & Docs]"
        };

        var categoryNodes = new List<MenuNode>();

        foreach (var catName in categoryNames)
        {
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
                    "/quota-views" => "Quota Views",
                    _ => group.First().Command.GroupName ?? groupPath
                };
                
                var formattedLabel = groupPath.StartsWith("/") ? $" [{groupPath}] {groupLabel}" : $" [{groupLabel}]";
                if (groupPath == "/jp-suite" || groupPath == "/english-vocab" || groupPath == "/csharp-master" || groupPath == "/dsa-architect" || groupPath == "/career-interview" || groupPath == "/obsidian-vault" || groupPath == "/git-tools")
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

            // Sort ungrouped commands by SortOrder
            var sortedUngrouped = ungrouped
                .OrderBy(node => node.Command!.SortOrder)
                .ToList();

            // Sort group nodes by SortOrder of their minimum child
            var sortedGroups = groupsList
                .OrderBy(node => node.Children.Length > 0 ? node.Children.Min(c => c.Command!.SortOrder) : 999)
                .ToList();

            // Combine: sorted ungrouped first, then sorted groups
            var sortedChildren = new List<MenuNode>();
            sortedChildren.AddRange(sortedUngrouped);
            sortedChildren.AddRange(sortedGroups);
            var sortedChildrenArray = sortedChildren.ToArray();

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
