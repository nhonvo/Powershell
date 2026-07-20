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
        new("scaffold", "Scaffold New Project", "Create new project from template", "[Workspace & Dev]", "Scaffold",
            new[] {
                "scaffold — Interactive project boilerplate creator.",
                " Templates: webapi · console · react (Vite) · blazorwasm · classlib · worker"
            }),
        new("db-tui", "SQLite Browser", "SQLite schema and data viewer", "[Workspace & Dev]", "Database",
            new[] {
                "db-tui <path> — Open SQLite file in interactive schema/data viewer.",
                " Requires sqlite3 CLI on PATH."
            }),

        // Git Tools (/git-tools & /repo-dashboards)
        new("gs", "Git Status", "Git status summary", "[Workspace & Dev]", "Git",
            new[] {
                "gs — Short git status (--short) with color coding."
            }),
        new("ga", "Git Add All", "Stage all modified and new files in workspace", "[Workspace & Dev]", "Git",
            Array.Empty<string>()),
        new("gbr", "Git Branch Manager", "List local and remote branches sorted by recent activity with quick checkout", "[Workspace & Dev]", "Git",
            Array.Empty<string>()),
        new("gcmt", "Conventional Commit", "Conventional commit wizard", "[Workspace & Dev]", "Git",
            new[] {
                "gcmt — Conventional commit wizard. Prompts for:",
                " 1. Type: feat | fix | docs | style | refactor | test | chore | ci",
                " 2. Scope (optional)",
                " 3. Short description (5–72 chars)",
                " 4. Breaking changes / issues closed"
            }),
        new("glog", "Git Commit Log", "Paged single-repo commit log graph (--oneline --graph)", "[Workspace & Dev]", "Git",
            Array.Empty<string>()),
        new("gpull", "Git Pull Remote", "Pull latest commits from remote tracking branch", "[Workspace & Dev]", "Git",
            Array.Empty<string>()),
        new("gpush", "Git Push Remote", "Push local commits to remote tracking branch", "[Workspace & Dev]", "Git",
            Array.Empty<string>()),
        new("gf", "Git Fetch Remote", "Fetch latest branch references from remote repository", "[Workspace & Dev]", "Git",
            Array.Empty<string>()),
        new("gd", "Git Diff Viewer", "Interactive git diff viewer for modified files", "[Workspace & Dev]", "Git",
            Array.Empty<string>()),
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

        // .NET Tools (/dotnet-tools)
        new("dbld", "[.NET] Build Project", "dotnet build in active workspace", "[Workspace & Dev]", ".NET",
            new[] {
                "dbld — dotnet build in the active workspace."
            }),
        new("dr", "[.NET] Run Project", "dotnet run active project in workspace", "[Workspace & Dev]", ".NET",
            Array.Empty<string>()),
        new("dtst", "[.NET] Test Project", "dotnet test in active workspace", "[Workspace & Dev]", ".NET",
            new[] {
                "dtst — dotnet test in the active workspace."
            }),
        new("df", "[.NET] Format Code", "dotnet format code style & linting rules", "[Workspace & Dev]", ".NET",
            Array.Empty<string>()),
        new("dcl", "[.NET] Clean Solution", "dotnet clean build output directory", "[Workspace & Dev]", ".NET",
            Array.Empty<string>()),
        new("drestore", "[.NET] Restore Packages", "dotnet restore packages in active workspace", "[Workspace & Dev]", ".NET",
            Array.Empty<string>()),
        new("dpublish", "[.NET] Publish Release", "dotnet publish release binary in active workspace", "[Workspace & Dev]", ".NET",
            Array.Empty<string>()),
        new("dwatch", "[.NET] Watch Live-Reload", "dotnet watch run continuous dev loop", "[Workspace & Dev]", ".NET",
            Array.Empty<string>()),
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

        // Docker Tools (/docker-tools)
        new("docker-health", "Docker Health Dashboard", "Show container health & resource utilization", "[Workspace & Dev]", "Docker",
            Array.Empty<string>()),
        new("dkcl", "Docker Cleanup", "Docker cleanup TUI dashboard", "[Workspace & Dev]", "Docker",
            new[] {
                "dkcl — Docker cleanup TUI dashboard. Options:",
                " • Stop & remove all containers",
                " • Prune images and dangling layers",
                " • Delete unused volumes / networks",
                " • Full cleanup (all of the above)"
            }),
        new("dkrmac", "Docker Remove All Containers", "Stop and remove all Docker containers forcefully", "[Workspace & Dev]", "Docker",
            Array.Empty<string>()),
        new("dkstac", "Docker Stop All Containers", "Stop all running Docker containers", "[Workspace & Dev]", "Docker",
            Array.Empty<string>()),
        new("dimg", "Docker Image Manager", "List and inspect local Docker images and layer sizes", "[Workspace & Dev]", "Docker",
            Array.Empty<string>()),
        new("dlogs", "Docker Container Logs", "Tail output logs for a selected running container", "[Workspace & Dev]", "Docker",
            Array.Empty<string>()),
        new("dcup", "Docker Compose Up", "docker compose up -d", "[Workspace & Dev]", "Docker",
            new[] {
                "dcup — docker compose up -d"
            }),
        new("dcdown", "Docker Compose Down", "docker compose down", "[Workspace & Dev]", "Docker",
            new[] {
                "dcdown — docker compose down"
            }),

        // AWS Tools (/aws-tools)
        new("aws-whoami", "AWS Identity Info", "Inspect active AWS STS caller identity, profile, and region", "[Workspace & Dev]", "AWS",
            Array.Empty<string>()),
        new("aws-local", "LocalStack Info", "LocalStack sandbox diagnostics", "[Workspace & Dev]", "AWS / LocalStack",
            new[] {
                "aws-local — Query running LocalStack sandbox on http://localhost:4566.",
                " Shows: S3 buckets, SQS queues, Lambda functions."
            }),
        new("aws-s3", "AWS S3 Buckets", "List local or cloud S3 buckets", "[Workspace & Dev]", "AWS",
            Array.Empty<string>()),
        new("aws-sqs", "AWS SQS Queues", "List local or cloud SQS queues", "[Workspace & Dev]", "AWS",
            Array.Empty<string>()),

        // [AI Agent & Ollama]
        new("claude", "Claude Code (Auto Mode)", "Launch Claude Code CLI (resolves Cloud vs Ollama via AiProviderMode)", "[AI Agent & Ollama]", "AI / LLM",
            new[] {
                "claude — Launch Claude Code CLI using runtime AiProviderMode setting."
            }, RequiresAiOllama: true),
        new("claude-cloud", "Claude Code (Force Cloud)", "Launch Claude Code CLI utilizing cloud APIs directly", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>(), RequiresAiOllama: true),
        new("claude-ollama", "Claude Code (Force Ollama)", "Run Claude Code routed locally via Ollama daemon", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>(), RequiresAiOllama: true),
        new("codex", "Codex (Auto Mode)", "Launch Codex CLI (resolves Cloud vs Ollama via AiProviderMode)", "[AI Agent & Ollama]", "AI / LLM",
            new[] {
                "codex — Launch Codex CLI using runtime AiProviderMode setting."
            }, RequiresAiOllama: true),
        new("codex-cloud", "Codex (Force Cloud)", "Launch Gemini's Codex CLI (Cloud API direct)", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>(), RequiresAiOllama: true),
        new("codex-ollama", "Codex (Force Ollama)", "Run Codex locally routed via Ollama daemon", "[AI Agent & Ollama]", "AI / LLM",
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
        new("ollama-benchmark", "Ollama: Benchmark Models", "Benchmark performance of local Ollama models", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>(), RequiresAiOllama: true),
        new("deck-status", "Antigravity Deck: Check Status", "Check if Antigravity Deck local server is running", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>()),
        new("deck-setup", "Antigravity Deck: Setup/Initialize", "Setup local Antigravity Deck", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>()),
        new("deck-start", "Antigravity Deck: Start Local", "Boot local Antigravity Deck", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>()),
        new("deck-online", "Antigravity Deck: Go Online (Tunnel)", "Expose local Deck via tunnel", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>()),
        new("agy-cli", "Launch Antigravity CLI (agy)", "Launch the google antigravity CLI tool terminal", "[AI Agent & Ollama]", "AI / LLM",
            Array.Empty<string>()),
        new("ai-history", "AI History Ledger", "Show ledger of past AI invocations", "[AI Agent & Ollama]", "AI / LLM",
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
        new("ssh-info", "SSH Connection Info", "SSH connection summary", "[System & Network]", "SSH",
            new[] {
                "ssh-info — Local IPs, Tailscale address, active SSH connections."
            }),
        new("tailscale-status", "Tailscale Status", "Parse tailscale status --json for peer connectivity", "[System & Network]", "Network",
            Array.Empty<string>()),
        new("ssh-qr", "SSH Terminal QR Code", "Generate terminal QR code for SSH connection parameters", "[System & Network]", "SSH",
            Array.Empty<string>()),

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

        // [Appearance & Layout]
        new("mobile-setup", "Toggle Mobile Setup", "Toggle both prompt mobile mode and compact TUI layout mode", "[Appearance & Layout]", "Theme & Settings",
            Array.Empty<string>()),
        new("theme", "Select Shell Theme", "Select Shell Theme", "[Appearance & Layout]", "Theme & Settings",
            Array.Empty<string>()),
        new("ui-mode", "Toggle UI Layout Mode", "Toggle between three-pane and flat-tree layouts", "[Appearance & Layout]", "Theme & Settings",
            Array.Empty<string>()),
        new("density", "Toggle Console Density", "Toggle between comfortable and compact display densities", "[Appearance & Layout]", "Theme & Settings",
            Array.Empty<string>()),

        // [Help & Docs]
        new("cc", "Command Palette", "Open this Command Palette", "[Help & Docs]", "Help",
            Array.Empty<string>()),
        new("help", "Help Browser", "Open interactive help browser", "[Help & Docs]", "Help",
            Array.Empty<string>()),
        new("hotkeys", "Profile Hotkeys Guide", "Show all PowerShell profile shortcut hotkeys grouped by domain", "[Help & Docs]", "Help",
            Array.Empty<string>())
    };

    public static void AssertSwitchCases()
    {
        string[] searchPaths = {
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Program.cs"),
            Path.Combine(AppContext.BaseDirectory, "Program.cs"),
            Path.Combine(Directory.GetCurrentDirectory(), "AgyTuiApp", "Program.cs")
        };

        string? programCsPath = searchPaths.FirstOrDefault(File.Exists);
        if (programCsPath == null) return;

        string code = File.ReadAllText(programCsPath);
        var matches = Regex.Matches(code, @"case\s+""([^""]+)""\s*:");
        var handledCases = matches.Select(m => m.Groups[1].Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unhandled = All.Where(c => !handledCases.Contains(c.Alias)).Select(c => c.Alias).ToList();
        if (unhandled.Count > 0)
        {
            throw new InvalidOperationException($"The following CommandRegistry aliases have no switch case in Program.cs: {string.Join(", ", unhandled)}");
        }
    }
}
