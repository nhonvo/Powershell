using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AgyTui.Registry;

public sealed record CommandEntry(
    string Alias,
    string DisplayName,
    string Description,
    string Category,      // TUI section: e.g. "[Workspace & Dev]"
    string HelpCategory,  // Help topic: e.g. "Git"
    string[] HelpLines,   // Detailed help text lines
    bool RequiresAiOllama = false,
    bool RequiresAgy = false
);

public static class CommandRegistry
{
    public static readonly CommandEntry[] All = new CommandEntry[]
    {
        // [Workspace & Dev]
        new("proj", "Navigate Workspace", "Navigate to a registered workspace", "[Workspace & Dev]", "Navigation",
            new[] {
                "proj <query> — Navigate to a workspace matching <query>.",
                " If multiple matches are found an interactive selector opens.",
                " If exactly one matches, jumps immediately.",
                "Alias: p"
            }),
        new("p", "Navigate Workspace (Alias)", "Alias for proj", "[Workspace & Dev]", "Navigation",
            Array.Empty<string>()),
        new("ide", "Terminal IDE", "Launch terminal IDE session", "[Workspace & Dev]", "IDE",
            Array.Empty<string>()),
        new("ide-diff", "Diff Viewer", "Git diff viewer for current dir", "[Workspace & Dev]", "IDE",
            Array.Empty<string>()),
        new("ide-search", "Search Across Files", "Search pattern across files", "[Workspace & Dev]", "IDE",
            Array.Empty<string>()),
        new("dbld", "[.NET] Build Project", "dotnet build in active workspace", "[Workspace & Dev]", ".NET",
            new[] {
                "dbld — dotnet build in the active workspace."
            }),
        new("dtst", "[.NET] Test Project", "dotnet test in active workspace", "[Workspace & Dev]", ".NET",
            new[] {
                "dtst — dotnet test in the active workspace."
            }),
        new("clean-build", "[.NET] Clean Build Artifacts", "Remove bin/ and obj/ recursively", "[Workspace & Dev]", ".NET",
            new[] {
                "clean-build — Recursively delete all bin/ and obj/ folders."
            }),
        new("add-migration", "[.NET] Add EF Migration", "EF Core: add migration", "[Workspace & Dev]", ".NET",
            new[] {
                "add-migration — dotnet ef migrations add <name>"
            }),
        new("update-db", "[.NET] Update EF Database", "EF Core: update database", "[Workspace & Dev]", ".NET",
            new[] {
                "update-db — dotnet ef database update"
            }),
        new("scaffold", "Scaffold New Project", "Create new project from template", "[Workspace & Dev]", "Scaffold",
            new[] {
                "scaffold — Interactive project boilerplate creator.",
                " Templates: webapi · console · react (Vite) · blazorwasm · classlib · worker"
            }),
        new("gs", "Git Status", "Git status summary", "[Workspace & Dev]", "Git",
            new[] {
                "gs — Short git status (--short) with color coding."
            }),
        new("gcmt", "Conventional Commit", "Conventional commit wizard", "[Workspace & Dev]", "Git",
            new[] {
                "gcmt — Conventional commit wizard. Prompts for:",
                " 1. Type: feat | fix | docs | style | refactor | test | chore | ci",
                " 2. Scope (optional)",
                " 3. Short description (5–72 chars)",
                " 4. Breaking changes / issues closed"
            }),
        new("git-undo", "Git Undo Last Commit", "Soft-reset the last local commit", "[Workspace & Dev]", "Git",
            new[] {
                "git-undo — Soft-reset the last commit (keeps changes staged)."
            }),
        new("nexus", "Repo Nexus Graph", "Git Nexus multi-repo dashboard", "[Workspace & Dev]", "Git",
            Array.Empty<string>()),
        new("repo-graph", "Repository dependency graph", "Repository dependency graph", "[Workspace & Dev]", "Git",
            Array.Empty<string>()),
        new("nexus-stats", "Git Nexus commit stats", "Git Nexus commit stats", "[Workspace & Dev]", "Git",
            Array.Empty<string>()),

        // [AI Agent & Ollama]
        new("claude", "Claude Code (Cloud Default)", "Launch Claude Code CLI", "[AI Agent & Ollama]", "AI / LLM",
            new[] {
                "claude — Launch Claude Code CLI."
            }, RequiresAiOllama: true),
        new("claude-cloud", "Claude Code (Cloud)", "Launch Claude Code CLI utilizing cloud APIs", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>(), RequiresAiOllama: true),
        new("claude-ollama", "Claude Code (Ollama)", "Run Claude Code routed locally via Ollama", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>(), RequiresAiOllama: true),
        new("codex", "Codex (Cloud Default)", "Launch Codex CLI", "[AI Agent & Ollama]", "AI / LLM",
            new[] {
                "codex — Launch Codex CLI."
            }, RequiresAiOllama: true),
        new("codex-cloud", "Codex (Cloud)", "Launch Gemini's Codex CLI (Cloud)", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>(), RequiresAiOllama: true),
        new("codex-ollama", "Codex (Ollama)", "Run Codex locally routed via Ollama", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>(), RequiresAiOllama: true),
        new("openclaw", "OpenClaw (Ollama)", "Launch OpenClaw via Ollama", "[AI Agent & Ollama]", "AI / LLM",
            new[] {
                "openclaw — Launch OpenClaw model via local Ollama daemon."
            }, RequiresAiOllama: true),
        new("hermes", "Hermes3 (Ollama)", "Launch Hermes3 via Ollama", "[AI Agent & Ollama]", "AI / LLM",
            new[] {
                "hermes — Launch Hermes3 model via Ollama."
            }, RequiresAiOllama: true),
        new("hermesd", "Hermes3 debug mode", "Launch Hermes3 debug mode", "[AI Agent & Ollama]", "AI / LLM",
            new[] {
                "hermesd — Launch Hermes3 in debug mode.",
                " Note: Ollama daemon on port 11434 is started automatically if offline."
            }, RequiresAiOllama: true),
        new("ollama-status", "Ollama: Check Daemon Status", "Check local Ollama server status and pulled models", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>(), RequiresAiOllama: true),
        new("ollama-models", "Ollama: Manage Models", "List/inspect/delete pulled models", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>(), RequiresAiOllama: true),
        new("ollama-pull", "Ollama: Pull New Model", "Fetch a new model", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>(), RequiresAiOllama: true),
        new("ollama-start", "Ollama: Start Daemon", "Boot the background daemon", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>(), RequiresAiOllama: true),
        new("ollama-logs", "Ollama: View Server Logs", "Show last 50 lines of server logs", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>(), RequiresAiOllama: true),
        new("deck-setup", "Antigravity Deck: Setup/Initialize", "Setup local Antigravity Deck", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>()),
        new("deck-start", "Antigravity Deck: Start Local", "Boot local Antigravity Deck", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>()),
        new("deck-online", "Antigravity Deck: Go Online (Tunnel)", "Expose local Deck via tunnel", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>()),
        new("agy-cli", "Launch Antigravity CLI (agy)", "Launch the google antigravity CLI tool terminal", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>()),

        // [AGY Account Switch]
        new("agyswitch", "Select Active Account", "Switch AGY account context", "[AGY Account Switch]", "Accounts",
            new[] {
                "agyswitch — Switch the active AGY/Gemini account context."
            }, RequiresAgy: true),
        new("agyquota", "View All Accounts", "Show quota usage summary for all accounts", "[AGY Account Switch]", "Accounts",
            new[] {
                "agyquota — Show quota usage summary for all accounts."
            }, RequiresAgy: true),
        new("account-tree", "Account Tree", "Show hierarchical active account details", "[AGY Account Switch]", "Accounts",
            Array.Empty<string>(), RequiresAgy: true),
        new("quota-chart", "Quota Bar Chart", "Show bar chart of active account limits", "[AGY Account Switch]", "Accounts",
            Array.Empty<string>(), RequiresAgy: true),
        new("live-dashboard", "Live Dashboard", "Show real-time active account metrics table", "[AGY Account Switch]", "Accounts",
            Array.Empty<string>(), RequiresAgy: true),
        new("autoswitch", "Toggle Auto-Switch", "Toggle automatic project account switching", "[AGY Account Switch]", "Accounts",
            Array.Empty<string>(), RequiresAgy: true),

        // [Docker & Databases]
        new("dkcl", "Docker Cleanup", "Docker cleanup TUI dashboard", "[Docker & Databases]", "Docker",
            new[] {
                "dkcl — Docker cleanup TUI dashboard. Options:",
                " • Stop & remove all containers",
                " • Prune images and dangling layers",
                " • Delete unused volumes / networks",
                " • Full cleanup (all of the above)"
            }),
        new("dcup", "Docker Compose Up", "docker compose up -d", "[Docker & Databases]", "Docker",
            new[] {
                "dcup — docker compose up -d"
            }),
        new("dcdown", "Docker Compose Down", "docker compose down", "[Docker & Databases]", "Docker",
            new[] {
                "dcdown — docker compose down"
            }),
        new("aws-local", "LocalStack Info", "LocalStack sandbox diagnostics", "[Docker & Databases]", "AWS / LocalStack",
            new[] {
                "aws-local — Query running LocalStack sandbox on http://localhost:4566.",
                " Shows: S3 buckets, SQS queues, Lambda functions."
            }),
        new("db-tui", "SQLite Browser", "SQLite schema and data viewer", "[Docker & Databases]", "Database",
            new[] {
                "db-tui <path> — Open SQLite file in interactive schema/data viewer.",
                " Requires sqlite3 CLI on PATH."
            }),

        // [System & Network]
        new("disk", "Disk Usage", "Show disk usage and health", "[System & Network]", "System",
            new[] {
                "disk — Disk partitions, free space ratios, health status."
            }),
        new("public-ip", "Public IP Address", "Resolve public IPv4 address", "[System & Network]", "System",
            new[] {
                "public-ip — Resolve external IPv4 via REST fallback chain."
            }),
        new("kill-port", "Kill Port", "Kill process by port number", "[System & Network]", "System",
            new[] {
                "kill-port <n> — Terminate the process listening on TCP port <n>."
            }),
        new("ssh-info", "SSH Connection Info", "SSH connection summary", "[System & Network]", "System",
            new[] {
                "ssh-info — Local IPs, Tailscale address, active SSH connections."
            }),

        // [Learn & Study]
        new("learn", "Start Learning (auto)", "Start learning for a topic (auto-refresh)", "[Learn & Study]", "Learn",
            Array.Empty<string>()),
        new("flashcard", "Flashcard Deck Browser", "Open flashcard deck browser", "[Learn & Study]", "Learn",
            Array.Empty<string>()),
        new("vocab", "English Vocab Drill", "English vocabulary drill", "[Learn & Study]", "Learn",
            Array.Empty<string>()),
        new("kana", "Kana Quiz", "Hiragana / katakana quiz", "[Learn & Study]", "Learn",
            Array.Empty<string>()),
        new("kanji", "Kanji Lookup", "Kanji lookup / stroke detail", "[Learn & Study]", "Learn",
            Array.Empty<string>()),
        new("jlpt", "JLPT Vocab Drill", "JLPT vocabulary drill", "[Learn & Study]", "Learn",
            Array.Empty<string>()),
        new("algo", "Algorithm Visualizer", "Algorithm visualizer (sort / search)", "[Learn & Study]", "Learn",
            Array.Empty<string>()),
        new("complexity", "Big-O Complexity Sheet", "Big-O complexity cheat-sheet", "[Learn & Study]", "Learn",
            Array.Empty<string>()),
        new("problems", "DSA Problem Tracker", "DSA problem tracker", "[Learn & Study]", "Learn",
            Array.Empty<string>()),
        new("snippets", "Code Snippet Library", "Code snippet library browser", "[Learn & Study]", "Learn",
            Array.Empty<string>()),
        new("sheets", "Cheat Sheet Browser", "Cheat-sheet browser (.txt files)", "[Learn & Study]", "Learn",
            Array.Empty<string>()),
        new("quiz", "C# Quiz", "C# multiple-choice quiz", "[Learn & Study]", "Learn",
            Array.Empty<string>()),
        new("interview", "Interview Question Bank", "Interview question bank", "[Learn & Study]", "Learn",
            Array.Empty<string>()),
        new("star", "STAR Answer Builder", "STAR answer builder", "[Learn & Study]", "Learn",
            Array.Empty<string>()),
        new("mock", "Mock Interview Timer", "Mock interview timer", "[Learn & Study]", "Learn",
            Array.Empty<string>()),
        new("word-of-day", "Word of the Day", "Show today's word of the day", "[Learn & Study]", "Learn",
            Array.Empty<string>()),

        // [Track & Progress]
        new("session", "Start Pomodoro Session", "Start a Pomodoro study session", "[Track & Progress]", "Tracking",
            Array.Empty<string>()),
        new("stats", "Study Statistics", "Study statistics and weekly chart", "[Track & Progress]", "Tracking",
            Array.Empty<string>()),
        new("goals", "Daily Goals", "Daily learning goals", "[Track & Progress]", "Tracking",
            Array.Empty<string>()),
        new("streak", "Study Streak", "Study streak display", "[Track & Progress]", "Tracking",
            Array.Empty<string>()),
        new("due", "Due Reviews", "Show due spaced-repetition reviews", "[Track & Progress]", "Tracking",
            Array.Empty<string>()),
        new("progress", "Progress Dashboard", "Progress dashboard (bar chart + tree)", "[Track & Progress]", "Tracking",
            Array.Empty<string>()),
        new("weak", "Weak Items Queue", "Weak items queue (pre-session review)", "[Track & Progress]", "Tracking",
            Array.Empty<string>()),

        // [Obsidian & Resources]
        new("obsidian", "Obsidian Vault Config", "Configure / browse Obsidian vault", "[Obsidian & Resources]", "Obsidian",
            Array.Empty<string>()),
        new("obs-graph", "Obsidian Graph View", "Obsidian wikilink graph", "[Obsidian & Resources]", "Obsidian",
            Array.Empty<string>()),
        new("refresh", "Refresh Learning Data", "Refresh learning data from vault", "[Obsidian & Resources]", "Resources",
            Array.Empty<string>()),
        new("add-resource", "Add Resource", "Add a file/URL to resource registry", "[Obsidian & Resources]", "Resources",
            Array.Empty<string>()),

        // [Theme & Settings]
        new("cc", "Command Palette", "Open this Command Palette", "[Theme & Settings]", "Help",
            Array.Empty<string>()),
        new("help", "Help Browser", "Open interactive help browser", "[Theme & Settings]", "Help",
            Array.Empty<string>()),
        new("theme", "Select Shell Theme", "Select Shell Theme", "[Theme & Settings]", "Theme & Settings",
            Array.Empty<string>())
    };

    public static void AssertSwitchCases()
    {
        string[] searchPaths = {
            "Program.cs",
            "../Program.cs",
            "../../Program.cs",
            "../../../Program.cs",
            "../AgyTuiApp/Program.cs",
            "../../AgyTuiApp/Program.cs",
            "AgyTuiApp/Program.cs"
        };
        string? programCsPath = null;
        foreach (var path in searchPaths)
        {
            var absolutePath = Path.GetFullPath(path);
            if (File.Exists(absolutePath))
            {
                programCsPath = absolutePath;
                break;
            }
        }
        if (programCsPath == null) return;

        var content = File.ReadAllText(programCsPath);
        var matches = Regex.Matches(content, @"case\s*""([^""]+)""\s*:");
        var cases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in matches)
        {
            cases.Add(match.Groups[1].Value);
        }

        var missing = new List<string>();
        foreach (var entry in All)
        {
            // Skip alias 'p' because it is handled by 'proj'
            if (entry.Alias == "p") continue;
            // Skip repo-graph because it is handled by 'nexus'
            if (entry.Alias == "repo-graph") continue;

            if (!cases.Contains(entry.Alias))
            {
                missing.Add(entry.Alias);
            }
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException($"Drift detected: The following aliases in CommandRegistry.All are missing from Program.RunCommand switch: {string.Join(", ", missing)}");
        }
    }
}
