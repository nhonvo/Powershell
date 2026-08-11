using AgyTui.UI.Core.Layouts.Abstractions;
using AgyTui.UI.Core.Commands;

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

public abstract class MenuRendererBase : IMenuRenderer
{
    public abstract void Run(MenuNode root);

    private static readonly TtlCache<string, (bool Ai, bool Agy)> _enabledCache = new(TimeSpan.FromSeconds(2));

    protected static MenuNode[] GetActiveChildren(MenuNode parent)
    {
        var (enableAi, enableAgy) = _enabledCache.GetOrCompute("flags", () => (Config.Current.Ai.EnableOllama, Config.Current.Ai.EnableAgy));

        var list = new List<MenuNode>();
        foreach (var child in parent.Children)
        {
            if (child.Kind == MenuNodeKind.Category)
            {
                if (child.Label.Contains("AI Agent & Ollama") && !enableAi) continue;
                if (child.Label.Contains("AGY Account Switch") && !enableAgy) continue;
            }
            else if (child.Kind == MenuNodeKind.Command && child.Command != null)
            {
                if (child.Command.RequiresAiOllama && !enableAi) continue;
                if (child.Command.RequiresAgy && !enableAgy) continue;
            }

            if (child.Id == "agy-cli" && !enableAgy)
            {
                var originalCmd = child.Command!;
                var rewrittenCmd = originalCmd with { DisplayName = "Launch Claude Code CLI (claude)" };
                list.Add(child with { Label = "Launch Claude Code CLI (claude)", Command = rewrittenCmd });
                continue;
            }

            list.Add(child);
        }
        return list.ToArray();
    }

    protected static string[] GetThemeNames()
    {
        var themesPath = Environment.GetEnvironmentVariable("POSH_THEMES_PATH");
        if (string.IsNullOrEmpty(themesPath) || !Directory.Exists(themesPath))
        {
            themesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asset", "powershell-themes");
            if (!Directory.Exists(themesPath))
            {
                themesPath = Path.Combine(Directory.GetCurrentDirectory(), "asset", "powershell-themes");
            }
        }
        if (!Directory.Exists(themesPath)) return Array.Empty<string>();
        return Directory.GetFiles(themesPath, "*.omp.json").Select(f => Path.GetFileName(f).Replace(".omp.json", "")).OrderBy(f => f).ToArray();
    }

    protected static string DeletePreviousWord(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var trimmed = text.TrimEnd();
        if (string.IsNullOrEmpty(trimmed)) return "";
        int lastSpace = trimmed.LastIndexOf(' ');
        if (lastSpace < 0) return "";
        return trimmed[..lastSpace].TrimEnd();
    }

    public static (int startIdx, int endIdx) ComputeViewport(int totalItems, int selectedIndex, int maxVisibleItems)
    {
        if (totalItems <= 0) return (0, 0);
        if (maxVisibleItems <= 0) maxVisibleItems = 10;

        selectedIndex = Math.Clamp(selectedIndex, 0, totalItems - 1);

        int startIdx = Math.Max(0, selectedIndex - (maxVisibleItems / 2));
        int endIdx = startIdx + maxVisibleItems;

        if (endIdx > totalItems)
        {
            endIdx = totalItems;
            startIdx = Math.Max(0, endIdx - maxVisibleItems);
        }

        return (startIdx, endIdx);
    }
}

public class MenuNodeBuilderService : IMenuNodeBuilder
{
    public MenuNode BuildTree()
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
                var favCommandsList = favoriteAliases
                    .Select(a => CommandRegistry.GetByAlias(a))
                    .Where(c => c != null)
                    .Select(c => CreateCommandNode(c!))
                    .ToList();

                var favoriteManageCmd = CommandRegistry.GetByAlias("favorite");
                if (favoriteManageCmd != null && !favCommandsList.Any(c => c.Command?.Alias == "favorite"))
                {
                    favCommandsList.Add(CreateCommandNode(favoriteManageCmd));
                }

                if (favCommandsList.Count > 0)
                {
                    categoryNodes.Add(new MenuNode("favorites", "[Favorites]", MenuNodeKind.Category, favCommandsList.ToArray(), null));
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
                    "/workspace-nav" => "Workspace Navigation",
                    "/dev-scaffold-tools" => "Developer Tools & Scaffolding",
                    "/account-mgr" => "Account & Credentials Manager",
                    "/quota-views" => "Quota & Analytics Views",
                    "/jp-suite" => "Japanese Suite",
                    "/english-vocab" => "English & Vocab",
                    "/csharp-master" => "C# & Dev Masterclass",
                    "/dsa-architect" => "DSA & System Design",
                    "/career-interview" => "Career & Interview Prep",
                    "/obsidian-vault" => "Obsidian Vault & Resources",
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
                    "/track" => "Track & Progress",
                    _ => group.First().Command.GroupName ?? groupPath
                };

                var formattedLabel = groupPath.StartsWith("/") ? $" [{groupPath}] {groupLabel}" : $" [{groupLabel}]";
                if (groupPath == "/workspace-nav" || groupPath == "/dev-scaffold-tools" || groupPath == "/jp-suite" || groupPath == "/english-vocab" || groupPath == "/csharp-master" || groupPath == "/dsa-architect" || groupPath == "/career-interview" || groupPath == "/obsidian-vault" || groupPath == "/git-tools" || groupPath == "/dotnet-tools" || groupPath == "/docker-tools" || groupPath == "/aws-tools" || groupPath == "/track")
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
                ["/workspace-nav"] = 1,
                ["/dev-scaffold-tools"] = 2,
                ["/scaffold-tools"] = 3,
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
                ["/system-reload"] = 20,
                ["/appearance-favs"] = 30
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

public static class MenuNodeBuilder
{
    private static readonly IMenuNodeBuilder _service = new MenuNodeBuilderService();
    public static IMenuNodeBuilder Instance => _service;

    public static MenuNode BuildTree() => _service.BuildTree();
}

