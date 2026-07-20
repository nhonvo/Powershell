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

        // Group 1: Git Tools
        var gitTools = new MenuNode(
            "/git-tools",
            " [/git-tools] Git Tools",
            MenuNodeKind.Group,
            new[]
            {
                CreateCommandNode(allCommands["gs"]),
                CreateCommandNode(allCommands["ga"]),
                CreateCommandNode(allCommands["gbr"]),
                CreateCommandNode(allCommands["gcmt"]),
                CreateCommandNode(allCommands["glog"]),
                CreateCommandNode(allCommands["gpull"]),
                CreateCommandNode(allCommands["gpush"]),
                CreateCommandNode(allCommands["gf"]),
                CreateCommandNode(allCommands["gd"]),
                CreateCommandNode(allCommands["git-undo"])
            },
            null
        );

        // Group 2: Repo Dashboards
        var repoDashboards = new MenuNode(
            "/repo-dashboards",
            " [/repo-dashboards] Repo Dashboards",
            MenuNodeKind.Group,
            new[]
            {
                CreateCommandNode(allCommands["nexus"]),
                CreateCommandNode(allCommands["repo-graph"]),
                CreateCommandNode(allCommands["nexus-stats"])
            },
            null
        );

        // Group 3: .NET Project Tools
        var dotnetTools = new MenuNode(
            "/dotnet-tools",
            " [/dotnet-tools] .NET Project Tools",
            MenuNodeKind.Group,
            new[]
            {
                CreateCommandNode(allCommands["dbld"]),
                CreateCommandNode(allCommands["dr"]),
                CreateCommandNode(allCommands["dtst"]),
                CreateCommandNode(allCommands["df"]),
                CreateCommandNode(allCommands["dcl"]),
                CreateCommandNode(allCommands["drestore"]),
                CreateCommandNode(allCommands["dpublish"]),
                CreateCommandNode(allCommands["dwatch"]),
                CreateCommandNode(allCommands["rebuild"]),
                CreateCommandNode(allCommands["clean-build"]),
                CreateCommandNode(allCommands["add-migration"]),
                CreateCommandNode(allCommands["update-db"])
            },
            null
        );

        // Group 4: Docker Tools
        var dockerTools = new MenuNode(
            "/docker-tools",
            " [/docker-tools] Docker Tools",
            MenuNodeKind.Group,
            new[]
            {
                CreateCommandNode(allCommands["docker-health"]),
                CreateCommandNode(allCommands["dkcl"]),
                CreateCommandNode(allCommands["dkrmac"]),
                CreateCommandNode(allCommands["dkstac"]),
                CreateCommandNode(allCommands["dimg"]),
                CreateCommandNode(allCommands["dlogs"]),
                CreateCommandNode(allCommands["dcup"]),
                CreateCommandNode(allCommands["dcdown"])
            },
            null
        );

        // Group 5: AWS Tools
        var awsTools = new MenuNode(
            "/aws-tools",
            " [/aws-tools] AWS Tools",
            MenuNodeKind.Group,
            new[]
            {
                CreateCommandNode(allCommands["aws-whoami"]),
                CreateCommandNode(allCommands["aws-local"]),
                CreateCommandNode(allCommands["aws-s3"]),
                CreateCommandNode(allCommands["aws-sqs"]),
                CreateCommandNode(allCommands["aws-ssm"]),
                CreateCommandNode(allCommands["aws-sns"]),
                CreateCommandNode(allCommands["aws-dynamodb"]),
                CreateCommandNode(allCommands["aws-lambda"])
            },
            null
        );

        // Group 6: Claude Agents
        var claudeAgents = new MenuNode(
            "/claude-agents",
            " [/claude] Claude Agents",
            MenuNodeKind.Group,
            new[]
            {
                CreateCommandNode(allCommands["claude"]),
                CreateCommandNode(allCommands["claude-cloud"]),
                CreateCommandNode(allCommands["claude-ollama"])
            },
            null
        );

        // Group 7: Codex Agents
        var codexAgents = new MenuNode(
            "/codex-agents",
            " [/codex] Codex Agents",
            MenuNodeKind.Group,
            new[]
            {
                CreateCommandNode(allCommands["codex"]),
                CreateCommandNode(allCommands["codex-cloud"]),
                CreateCommandNode(allCommands["codex-ollama"])
            },
            null
        );

        // Group 8: Ollama Tools
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

        // Group 9: Antigravity Deck
        var deckTools = new MenuNode(
            "/antigravity-deck",
            " [/antigravity-deck] Antigravity Deck",
            MenuNodeKind.Group,
            new[]
            {
                CreateCommandNode(allCommands["deck-status"]),
                CreateCommandNode(allCommands["deck-setup"]),
                CreateCommandNode(allCommands["deck-start"]),
                CreateCommandNode(allCommands["deck-online"])
            },
            null
        );

        // Group 10: SSH & Tailscale
        var sshTailscale = new MenuNode(
            "/ssh-tailscale",
            " [/ssh-tailscale] SSH & Tailscale",
            MenuNodeKind.Group,
            new[]
            {
                CreateCommandNode(allCommands["ssh-info"]),
                CreateCommandNode(allCommands["tailscale-status"]),
                CreateCommandNode(allCommands["ssh-qr"])
            },
            null
        );

        // Group 11: Quota Views
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
                CreateCommandNode(allCommands["scaffold"]),
                gitTools,
                repoDashboards,
                dotnetTools,
                dockerTools,
                awsTools,
                CreateCommandNode(allCommands["db-tui"])
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
                claudeAgents,
                codexAgents,
                CreateCommandNode(allCommands["openclaw"]),
                CreateCommandNode(allCommands["hermes"]),
                CreateCommandNode(allCommands["hermesd"]),
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

        // Category 4: [System & Network]
        var systemNetwork = new MenuNode(
            "system-network",
            "[System & Network]",
            MenuNodeKind.Category,
            new[]
            {
                CreateCommandNode(allCommands["disk"]),
                CreateCommandNode(allCommands["public-ip"]),
                CreateCommandNode(allCommands["kill-port"]),
                sshTailscale
            },
            null
        );

        // Sub-groups for Category 5: Learn & Study
        var jpSuite = new MenuNode("/jp-suite", " [Japanese Suite]", MenuNodeKind.Group, new[]
        {
            CreateCommandNode(allCommands["kana"]),
            CreateCommandNode(allCommands["kanji"]),
            CreateCommandNode(allCommands["jlpt"]),
            CreateCommandNode(allCommands["grammar"])
        }, null);

        var englishVocab = new MenuNode("/english-vocab", " [English & Vocab]", MenuNodeKind.Group, new[]
        {
            CreateCommandNode(allCommands["word-of-day"]),
            CreateCommandNode(allCommands["vocab"]),
            CreateCommandNode(allCommands["flashcard"]),
            CreateCommandNode(allCommands["grammar"])
        }, null);

        var csharpMaster = new MenuNode("/csharp-master", " [C# & Dev Masterclass]", MenuNodeKind.Group, new[]
        {
            CreateCommandNode(allCommands["quiz"]),
            CreateCommandNode(allCommands["snippets"]),
            CreateCommandNode(allCommands["sheets"])
        }, null);

        var dsaArchitect = new MenuNode("/dsa-architect", " [DSA & System Design]", MenuNodeKind.Group, new[]
        {
            CreateCommandNode(allCommands["algo"]),
            CreateCommandNode(allCommands["complexity"]),
            CreateCommandNode(allCommands["problems"])
        }, null);

        var careerInterview = new MenuNode("/career-interview", " [Career & Interview Prep]", MenuNodeKind.Group, new[]
        {
            CreateCommandNode(allCommands["interview"]),
            CreateCommandNode(allCommands["star"]),
            CreateCommandNode(allCommands["mock"])
        }, null);

        var obsidianVault = new MenuNode("/obsidian-vault", " [Obsidian Vault & Sync]", MenuNodeKind.Group, new[]
        {
            CreateCommandNode(allCommands["obsidian"]),
            CreateCommandNode(allCommands["refresh"]),
            CreateCommandNode(allCommands["vault-open"])
        }, null);

        // Category 5: [Learn & Study]
        var learnStudy = new MenuNode(
            "learn-study",
            "[Learn & Study]",
            MenuNodeKind.Category,
            new[]
            {
                CreateCommandNode(allCommands["learn"]),
                obsidianVault,
                jpSuite,
                englishVocab,
                csharpMaster,
                dsaArchitect,
                careerInterview
            },
            null
        );

        // Category 6: [Track & Progress]
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

        // Category 7: [Obsidian & Resources]
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

        // Category 8: [Appearance & Layout]
        var appearanceLayout = new MenuNode(
            "appearance-layout",
            "[Appearance & Layout]",
            MenuNodeKind.Category,
            new[]
            {
                CreateCommandNode(allCommands["theme"]),
                CreateCommandNode(allCommands["ui-mode"]),
                CreateCommandNode(allCommands["density"]),
                CreateCommandNode(allCommands["mobile-setup"])
            },
            null
        );

        // Category 9: [Help & Docs]
        var helpDocs = new MenuNode(
            "help-docs",
            "[Help & Docs]",
            MenuNodeKind.Category,
            new[]
            {
                CreateCommandNode(allCommands["cc"]),
                CreateCommandNode(allCommands["help"]),
                CreateCommandNode(allCommands["hotkeys"])
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
                systemNetwork,
                learnStudy,
                trackProgress,
                obsidianResources,
                appearanceLayout,
                helpDocs,
                sep,
                exit
            },
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
