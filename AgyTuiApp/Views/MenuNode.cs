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
        var allCommands = CommandRegistry.All.ToDictionary(c => c.Alias, StringComparer.OrdinalIgnoreCase);

        // Group 1: .NET Project Tools
        var dotnetTools = new MenuNode(
            "/dotnet-tools",
            " [.NET Project Tools]",
            MenuNodeKind.Group,
            new[]
            {
                CreateCommandNode(allCommands["dbld"]),
                CreateCommandNode(allCommands["dtst"]),
                CreateCommandNode(allCommands["clean-build"]),
                CreateCommandNode(allCommands["add-migration"]),
                CreateCommandNode(allCommands["update-db"])
            },
            null
        );

        // Group 2: Ollama Tools
        var ollamaTools = new MenuNode(
            "/ollama-tools",
            " [/ollama-tools] Ollama Tools",
            MenuNodeKind.Group,
            new[]
            {
                CreateCommandNode(allCommands["ollama-status"]),
                CreateCommandNode(allCommands["ollama-models"]),
                CreateCommandNode(allCommands["ollama-pull"]),
                CreateCommandNode(allCommands["ollama-start"]),
                CreateCommandNode(allCommands["ollama-logs"]),
                CreateCommandNode(allCommands["ollama-benchmark"])
            },
            null
        );

        // Group 3: Antigravity Deck
        var deckTools = new MenuNode(
            "/antigravity-deck",
            " [/antigravity-deck] Antigravity Deck",
            MenuNodeKind.Group,
            new[]
            {
                CreateCommandNode(allCommands["deck-setup"]),
                CreateCommandNode(allCommands["deck-start"]),
                CreateCommandNode(allCommands["deck-online"])
            },
            null
        );

        // Group 4: Quota Views
        var quotaViews = new MenuNode(
            "/quota-views",
            " [/quota-views] Quota Views",
            MenuNodeKind.Group,
            new[]
            {
                CreateCommandNode(allCommands["account-tree"]),
                CreateCommandNode(allCommands["quota-chart"]),
                CreateCommandNode(allCommands["live-dashboard"])
            },
            null
        );

        // Category 1: [Workspace & Dev]
        var workspaceDev = new MenuNode(
            "workspace-dev",
            "[Workspace & Dev]",
            MenuNodeKind.Category,
            new[]
            {
                CreateCommandNode(allCommands["proj"]),
                CreateCommandNode(allCommands["ide"]),
                CreateCommandNode(allCommands["ide-diff"]),
                CreateCommandNode(allCommands["ide-search"]),
                dotnetTools,
                CreateCommandNode(allCommands["scaffold"]),
                CreateCommandNode(allCommands["gs"]),
                CreateCommandNode(allCommands["gcmt"]),
                CreateCommandNode(allCommands["git-undo"]),
                CreateCommandNode(allCommands["nexus"]),
                CreateCommandNode(allCommands["repo-graph"]),
                CreateCommandNode(allCommands["nexus-stats"])
            },
            null
        );

        // Category 2: [AI Agent & Ollama]
        var aiAgentOllama = new MenuNode(
            "ai-agent-ollama",
            "[AI Agent & Ollama]",
            MenuNodeKind.Category,
            new[]
            {
                CreateCommandNode(allCommands["claude"]),
                CreateCommandNode(allCommands["claude-cloud"]),
                CreateCommandNode(allCommands["claude-ollama"]),
                CreateCommandNode(allCommands["codex"]),
                CreateCommandNode(allCommands["codex-cloud"]),
                CreateCommandNode(allCommands["codex-ollama"]),
                CreateCommandNode(allCommands["openclaw"]),
                CreateCommandNode(allCommands["hermes"]),
                ollamaTools,
                deckTools,
                CreateCommandNode(allCommands["agy-cli"]),
                CreateCommandNode(allCommands["ai-history"])
            },
            null
        );

        // Category 3: [AGY Account Switch]
        var agyAccountSwitch = new MenuNode(
            "agy-account-switch",
            "[AGY Account Switch]",
            MenuNodeKind.Category,
            new[]
            {
                CreateCommandNode(allCommands["agyswitch"]),
                CreateCommandNode(allCommands["agyquota"]),
                quotaViews,
                CreateCommandNode(allCommands["autoswitch"])
            },
            null
        );

        // Category 4: [Docker & Databases]
        var dockerDatabases = new MenuNode(
            "docker-databases",
            "[Docker & Databases]",
            MenuNodeKind.Category,
            new[]
            {
                CreateCommandNode(allCommands["dkcl"]),
                CreateCommandNode(allCommands["dcup"]),
                CreateCommandNode(allCommands["dcdown"]),
                CreateCommandNode(allCommands["aws-local"]),
                CreateCommandNode(allCommands["db-tui"])
            },
            null
        );

        // Category 5: [System & Network]
        var systemNetwork = new MenuNode(
            "system-network",
            "[System & Network]",
            MenuNodeKind.Category,
            new[]
            {
                CreateCommandNode(allCommands["disk"]),
                CreateCommandNode(allCommands["public-ip"]),
                CreateCommandNode(allCommands["ssh-info"]),
                CreateCommandNode(allCommands["kill-port"])
            },
            null
        );

        // Category 6: [Learn & Study]
        var learnStudy = new MenuNode(
            "learn-study",
            "[Learn & Study]",
            MenuNodeKind.Category,
            new[]
            {
                CreateCommandNode(allCommands["learn"]),
                CreateCommandNode(allCommands["flashcard"]),
                CreateCommandNode(allCommands["vocab"]),
                CreateCommandNode(allCommands["kana"]),
                CreateCommandNode(allCommands["kanji"]),
                CreateCommandNode(allCommands["jlpt"]),
                CreateCommandNode(allCommands["algo"]),
                CreateCommandNode(allCommands["complexity"]),
                CreateCommandNode(allCommands["problems"]),
                CreateCommandNode(allCommands["snippets"]),
                CreateCommandNode(allCommands["sheets"]),
                CreateCommandNode(allCommands["quiz"]),
                CreateCommandNode(allCommands["interview"]),
                CreateCommandNode(allCommands["star"]),
                CreateCommandNode(allCommands["mock"]),
                CreateCommandNode(allCommands["word-of-day"])
            },
            null
        );

        // Category 7: [Track & Progress]
        var trackProgress = new MenuNode(
            "track-progress",
            "[Track & Progress]",
            MenuNodeKind.Category,
            new[]
            {
                CreateCommandNode(allCommands["session"]),
                CreateCommandNode(allCommands["stats"]),
                CreateCommandNode(allCommands["goals"]),
                CreateCommandNode(allCommands["streak"]),
                CreateCommandNode(allCommands["due"]),
                CreateCommandNode(allCommands["progress"]),
                CreateCommandNode(allCommands["weak"])
            },
            null
        );

        // Category 8: [Obsidian & Resources]
        var obsidianResources = new MenuNode(
            "obsidian-resources",
            "[Obsidian & Resources]",
            MenuNodeKind.Category,
            new[]
            {
                CreateCommandNode(allCommands["obsidian"]),
                CreateCommandNode(allCommands["obs-graph"]),
                CreateCommandNode(allCommands["refresh"]),
                CreateCommandNode(allCommands["add-resource"])
            },
            null
        );

        // Category 9: [Theme & Settings]
        var themeSettings = new MenuNode(
            "theme-settings",
            "[Theme & Settings]",
            MenuNodeKind.Category,
            new[]
            {
                CreateCommandNode(allCommands["cc"]),
                CreateCommandNode(allCommands["help"]),
                CreateCommandNode(allCommands["theme"]),
                CreateCommandNode(allCommands["ui-mode"]),
                CreateCommandNode(allCommands["density"])
            },
            null
        );

        // Separator
        var sep = new MenuNode(
            "separator",
            "────────────────────────────",
            MenuNodeKind.Separator,
            Array.Empty<MenuNode>(),
            null
        );

        // Category 10: Exit
        var exit = new MenuNode(
            "exit",
            "[Exit] Exit Control Center",
            MenuNodeKind.Exit,
            Array.Empty<MenuNode>(),
            null
        );

        return new MenuNode(
            "root",
            "Control Center Root",
            MenuNodeKind.Category,
            new[]
            {
                workspaceDev,
                aiAgentOllama,
                agyAccountSwitch,
                dockerDatabases,
                systemNetwork,
                learnStudy,
                trackProgress,
                obsidianResources,
                themeSettings,
                sep,
                exit
            },
            null
        );
    }

    private static MenuNode CreateCommandNode(CommandEntry cmd)
    {
        return new MenuNode(
            cmd.Alias,
            cmd.DisplayName,
            MenuNodeKind.Command,
            Array.Empty<MenuNode>(),
            cmd
        );
    }
}
