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
                " Alias: p, prj"
            }),
        new("p", "Navigate Workspace (Alias)", "Alias for proj workspace navigation", "[Workspace & Dev]", "Navigation",
            new[] { "p <query> — Quick alias for proj workspace navigation." }),
        new("prj", "Navigate Workspace (Alias)", "Alias for proj workspace navigation", "[Workspace & Dev]", "Navigation",
            new[] { "prj <query> — Quick alias for proj workspace navigation." }),
        new("f", "Open Explorer", "Open File Explorer in active workspace", "[Workspace & Dev]", "Navigation",
            new[] { "f — Opens Windows File Explorer at the current working directory." }),
        new("ide", "Terminal IDE", "Launch terminal IDE session", "[Workspace & Dev]", "IDE",
            new[] {
                "ide — Interactive Terminal IDE with file Explorer, Code Viewer, and Symbol Search.",
                " Keys: ↑↓/j/k navigate | Enter select | / search symbols | q back."
            }),
        new("ide-diff", "Diff Viewer", "Git diff viewer for current workspace", "[Workspace & Dev]", "IDE",
            new[] {
                "ide-diff — Full-screen colorized side-by-side / unified git diff viewer.",
                " Shows staged and unstaged file modifications across the workspace."
            }),
        new("ide-search", "Search Across Files", "Search pattern and symbols across workspace files", "[Workspace & Dev]", "IDE",
            new[] {
                "ide-search — Workspace-wide code and symbol search tool.",
                " Scans .cs, .ps1, .ts, .js, .py files for classes, methods, and functions."
            }),
        new("scaffold", "Scaffold New Project", "Create new project from template", "[Workspace & Dev]", "Scaffold",
            new[] {
                "scaffold — Interactive project boilerplate creator.",
                " Templates: webapi · console · react (Vite) · blazorwasm · classlib · worker"
            }),
        new("rebuild", "[.NET] Rebuild Control Center TUI", "Recompile AgyTuiApp.csproj and refresh binary", "[Workspace & Dev]", ".NET",
            new[] {
                "rebuild — Triggers `dotnet build` on AgyTuiApp.csproj with zero warnings/errors enforcement.",
                " Recompiles the TUI binary executable in-place."
            }),
        new("db-tui", "SQLite Browser", "SQLite schema and data viewer", "[Workspace & Dev]", "Database",
            new[] {
                "db-tui <path> — Open SQLite file in interactive schema/data viewer.",
                " Requires sqlite3 CLI on PATH."
            }),

        // Git Tools (/git-tools & /repo-dashboards)
        new("gs", "Git Status", "Git status summary", "[Workspace & Dev]", "Git",
            new[] {
                "gs — Short git status (--short) with color coding.",
                " Displays untracked, modified, staged, and branch tracking status."
            }),
        new("ga", "Git Add All", "Stage all modified and new files in workspace", "[Workspace & Dev]", "Git",
            new[] {
                "ga — Executes `git add .` to stage all modified, deleted, and untracked files.",
                " Prepares all changes for the next commit."
            }),
        new("gb", "Git Branch Manager (Alias)", "Alias for gbr branch manager", "[Workspace & Dev]", "Git",
            new[] { "gb — Alias for gbr branch manager." }),
        new("gbr", "Git Branch Manager", "List local and remote branches sorted by recent activity with quick checkout", "[Workspace & Dev]", "Git",
            new[] {
                "gbr — Interactive branch manager sorted by commit date.",
                " Select any branch to checkout instantly."
            }),
        new("gcmt", "Conventional Commit", "Conventional commit wizard", "[Workspace & Dev]", "Git",
            new[] {
                "gcmt — Conventional commit wizard. Prompts for:",
                " 1. Type: feat | fix | docs | style | refactor | test | chore | ci",
                " 2. Scope (optional)",
                " 3. Short description (5–72 chars)",
                " 4. Breaking changes / issues closed"
            }),
        new("glog", "Git Commit Log", "Paged single-repo commit log graph (--oneline --graph)", "[Workspace & Dev]", "Git",
            new[] {
                "glog — Shows last 50 commits formatted as a single-line graph with branch decorations.",
                " Output is scrollable via the built-in Spectre pager."
            }),
        new("glo", "Git Commit Log (Alias)", "Alias for glog commit log graph", "[Workspace & Dev]", "Git",
            new[] { "glo — Alias for glog commit log graph." }),
        new("glg", "Git Commit Log (Alias)", "Alias for glog commit log graph", "[Workspace & Dev]", "Git",
            new[] { "glg — Alias for glog commit log graph." }),
        new("gpull", "Git Pull Remote", "Pull latest commits from remote tracking branch", "[Workspace & Dev]", "Git",
            new[] { "gpull — Executes `git pull` on current branch to incorporate remote changes." }),
        new("gpu", "Git Pull Remote (Alias)", "Alias for gpull remote pull", "[Workspace & Dev]", "Git",
            new[] { "gpu — Alias for gpull remote pull." }),
        new("gpush", "Git Push Remote", "Push local commits to remote tracking branch", "[Workspace & Dev]", "Git",
            new[] { "gpush — Executes `git push` to upload local commits to upstream tracking branch." }),
        new("gus", "Git Push Remote (Alias)", "Alias for gpush remote push", "[Workspace & Dev]", "Git",
            new[] { "gus — Alias for gpush remote push." }),
        new("gf", "Git Fetch Remote", "Fetch latest branch references from remote repository", "[Workspace & Dev]", "Git",
            new[] { "gf — Executes `git fetch` to update local tracking references without merging." }),
        new("gd", "Git Diff Viewer", "Interactive git diff viewer for modified files", "[Workspace & Dev]", "Git",
            new[] { "gd — Shortcut for launching the interactive Git Diff Viewer for the current directory." }),
        new("git-undo", "Git Undo Last Commit", "Soft-reset the last local commit", "[Workspace & Dev]", "Git",
            new[] {
                "git-undo — Soft-reset the last commit (`git reset --soft HEAD~1`).",
                " Keeps all file modifications staged in the index."
            }),
        new("gundo", "Git Undo Last Commit (Alias)", "Alias for git-undo soft reset", "[Workspace & Dev]", "Git",
            new[] { "gundo — Alias for git-undo soft reset." }),
        new("nexus", "Repo Nexus Graph", "Git Nexus multi-repo dashboard", "[Workspace & Dev]", "Git",
            new[] { "nexus — Renders a multi-repository workspace dependency and git status dashboard." }),
        new("repo-graph", "Repository dependency graph", "Repository dependency graph", "[Workspace & Dev]", "Git",
            new[] { "repo-graph — Displays dependency tree and inter-project relationship links." }),
        new("nexus-stats", "Git Nexus commit stats", "Git Nexus commit stats", "[Workspace & Dev]", "Git",
            new[] { "nexus-stats — Summarizes commit velocity, active authors, and modification volume across repos." }),

        // .NET Tools (/dotnet-tools)
        new("dbld", "[.NET] Build Project", "dotnet build in active workspace", "[Workspace & Dev]", ".NET",
            new[] { "dbld — Executes `dotnet build` in active project workspace." }),
        new("db", "[.NET] Build Project (Alias)", "Alias for dbld dotnet build", "[Workspace & Dev]", ".NET",
            new[] { "db — Alias for dbld dotnet build." }),
        new("dr", "[.NET] Run Project", "dotnet run active project in workspace", "[Workspace & Dev]", ".NET",
            new[] { "dr — Executes `dotnet run` in active workspace to launch the application binary." }),
        new("dtst", "[.NET] Test Project", "dotnet test in active workspace", "[Workspace & Dev]", ".NET",
            new[] { "dtst — Executes `dotnet test` in active workspace." }),
        new("dt", "[.NET] Test Project (Alias)", "Alias for dtst dotnet test", "[Workspace & Dev]", ".NET",
            new[] { "dt — Alias for dtst dotnet test." }),
        new("df", "[.NET] Format Code", "dotnet format code style & linting rules", "[Workspace & Dev]", ".NET",
            new[] { "df — Runs `dotnet format` to apply standard C# formatting rules." }),
        new("dcl", "[.NET] Clean Solution", "dotnet clean build output directory", "[Workspace & Dev]", ".NET",
            new[] { "dcl — Runs `dotnet clean` to clear build target outputs." }),
        new("drestore", "[.NET] Restore Packages", "dotnet restore packages in active workspace", "[Workspace & Dev]", ".NET",
            new[] { "drestore — Executes `dotnet restore` to resolve NuGet package dependencies." }),
        new("dres", "[.NET] Restore Packages (Alias)", "Alias for drestore dotnet restore", "[Workspace & Dev]", ".NET",
            new[] { "dres — Alias for drestore dotnet restore." }),
        new("dpublish", "[.NET] Publish Release", "dotnet publish release binary in active workspace", "[Workspace & Dev]", ".NET",
            new[] { "dpublish — Executes `dotnet publish -c Release` for production binaries." }),
        new("dwatch", "[.NET] Watch Live-Reload", "dotnet watch run continuous dev loop", "[Workspace & Dev]", ".NET",
            new[] { "dwatch — Runs `dotnet watch run` for continuous live-reloading." }),
        new("dw", "[.NET] Watch Live-Reload (Alias)", "Alias for dwatch live reload", "[Workspace & Dev]", ".NET",
            new[] { "dw — Alias for dwatch live reload." }),
        new("clean-build", "[.NET] Clean Build Artifacts", "Remove bin/ and obj/ recursively", "[Workspace & Dev]", ".NET",
            new[] { "clean-build — Recursively deletes all `bin/` and `obj/` directories." }),
        new("dclean", "[.NET] Clean Build Artifacts (Alias)", "Alias for clean-build", "[Workspace & Dev]", ".NET",
            new[] { "dclean — Alias for clean-build." }),
        new("add-migration", "[.NET] Add EF Migration", "EF Core: add migration", "[Workspace & Dev]", ".NET",
            new[] { "add-migration <name> — Runs `dotnet ef migrations add <name>`." }),
        new("da", "[.NET] Add EF Migration (Alias)", "Alias for add-migration", "[Workspace & Dev]", ".NET",
            new[] { "da — Alias for add-migration." }),
        new("update-db", "[.NET] Update EF Database", "EF Core: update database", "[Workspace & Dev]", ".NET",
            new[] { "update-db — Runs `dotnet ef database update`." }),
        new("du", "[.NET] Update EF Database (Alias)", "Alias for update-db", "[Workspace & Dev]", ".NET",
            new[] { "du — Alias for update-db." }),

        // Docker Tools (/docker-tools)
        new("docker-health", "Docker Health Dashboard", "Show container health & resource utilization", "[Workspace & Dev]", "Docker",
            new[] { "docker-health — Displays real-time CPU, memory, network I/O, and container health." }),
        new("dkcl", "Docker Cleanup", "Docker cleanup TUI dashboard", "[Workspace & Dev]", "Docker",
            new[] {
                "dkcl — Docker cleanup TUI dashboard. Options:",
                " • Stop & remove all containers",
                " • Prune images and dangling layers",
                " • Delete unused volumes / networks"
            }),
        new("dkrmac", "Docker Remove All Containers", "Stop and remove all Docker containers forcefully", "[Workspace & Dev]", "Docker",
            new[] { "dkrmac — Forcefully stops and removes all Docker containers." }),
        new("dkstac", "Docker Stop All Containers", "Stop all running Docker containers", "[Workspace & Dev]", "Docker",
            new[] { "dkstac — Sends SIGTERM to stop all active Docker containers." }),
        new("dimg", "Docker Image Manager", "List and inspect local Docker images and layer sizes", "[Workspace & Dev]", "Docker",
            new[] { "dimg — Lists local Docker images formatted with Repository, Tag, Size, and Date." }),
        new("dlogs", "Docker Container Logs", "Tail output logs for a selected running container", "[Workspace & Dev]", "Docker",
            new[] { "dlogs — Interactive container log viewer. Select container to tail logs." }),
        new("dcup", "Docker Compose Up", "docker compose up -d", "[Workspace & Dev]", "Docker",
            new[] { "dcup — Runs `docker compose up -d`." }),
        new("dkcpu", "Docker Compose Up (Alias)", "Alias for dcup", "[Workspace & Dev]", "Docker",
            new[] { "dkcpu — Alias for dcup." }),
        new("dcdown", "Docker Compose Down", "docker compose down", "[Workspace & Dev]", "Docker",
            new[] { "dcdown — Runs `docker compose down`." }),
        new("dkcpd", "Docker Compose Down (Alias)", "Alias for dcdown", "[Workspace & Dev]", "Docker",
            new[] { "dkcpd — Alias for dcdown." }),

        // AWS Tools (/aws-tools)
        new("aws-whoami", "AWS Identity Info", "Inspect active AWS STS caller identity, profile, and region", "[Workspace & Dev]", "AWS",
            new[] { "aws-whoami — Executes `aws sts get-caller-identity`." }),
        new("aws-local", "LocalStack Info", "LocalStack sandbox diagnostics", "[Workspace & Dev]", "AWS / LocalStack",
            new[] { "aws-local — Query running LocalStack sandbox on http://localhost:4566." }),
        new("aws-s3", "AWS S3 Buckets", "List local or cloud S3 buckets", "[Workspace & Dev]", "AWS",
            new[] { "aws-s3 — Executes `aws s3 ls` (with LocalStack fallback)." }),
        new("aws-sqs", "AWS SQS Queues", "List local or cloud SQS queues", "[Workspace & Dev]", "AWS",
            new[] { "aws-sqs — Executes `aws sqs list-queues` (with LocalStack fallback)." }),
        new("aws-ssm", "AWS SSM Parameter Store", "Inspect Parameter Store key-value pairs", "[Workspace & Dev]", "AWS",
            new[] { "aws-ssm — Executes `aws ssm describe-parameters`." }),
        new("aws-sns", "AWS SNS Topics", "Inspect notification topics", "[Workspace & Dev]", "AWS",
            new[] { "aws-sns — Executes `aws sns list-topics`." }),
        new("aws-dynamodb", "AWS DynamoDB Tables", "Inspect DynamoDB tables", "[Workspace & Dev]", "AWS",
            new[] { "aws-dynamodb — Executes `aws dynamodb list-tables`." }),
        new("aws-lambda", "AWS Lambda Functions", "Inspect serverless functions", "[Workspace & Dev]", "AWS",
            new[] { "aws-lambda — Executes `aws lambda list-functions`." }),

        // [AI Agent & Ollama]
        new("claude", "Claude Code (Auto Mode)", "Launch Claude Code CLI (resolves Cloud vs Ollama via AiProviderMode)", "[AI Agent & Ollama]", "AI / LLM",
            new[] { "claude — Launch Claude Code CLI using runtime AiProviderMode setting." }, RequiresAiOllama: true),
        new("claude-cloud", "Claude Code (Force Cloud)", "Launch Claude Code CLI utilizing cloud APIs directly", "[AI Agent & Ollama]", "AI / LLM",
            new[] { "claude-cloud — Launch Claude Code CLI forcing cloud API credentials." }, RequiresAiOllama: true),
        new("claude-ollama", "Claude Code (Force Ollama)", "Run Claude Code routed locally via Ollama daemon", "[AI Agent & Ollama]", "AI / LLM",
            new[] { "claude-ollama — Run Claude Code routed locally via Ollama daemon." }, RequiresAiOllama: true),
        new("codex", "Codex (Auto Mode)", "Launch Codex CLI (resolves Cloud vs Ollama via AiProviderMode)", "[AI Agent & Ollama]", "AI / LLM",
            new[] { "codex — Launch Gemini Codex CLI using runtime AiProviderMode setting." }, RequiresAiOllama: true),
        new("codex-cloud", "Codex (Force Cloud)", "Launch Gemini's Codex CLI (Cloud API direct)", "[AI Agent & Ollama]", "AI / LLM",
            new[] { "codex-cloud — Launch Gemini's Codex CLI forcing cloud API direct access." }, RequiresAiOllama: true),
        new("codex-ollama", "Codex (Force Ollama)", "Run Codex locally routed via Ollama daemon", "[AI Agent & Ollama]", "AI / LLM",
            new[] { "codex-ollama — Run Codex CLI routed locally via Ollama daemon." }, RequiresAiOllama: true),
        new("openclaw", "OpenClaw (Ollama)", "Launch OpenClaw via Ollama", "[AI Agent & Ollama]", "AI / LLM",
            new[] { "openclaw — Launch OpenClaw model via local Ollama daemon." }, RequiresAiOllama: true),
        new("hermes", "Hermes3 (Ollama)", "Launch Hermes3 via Ollama", "[AI Agent & Ollama]", "AI / LLM",
            new[] { "hermes — Launch Hermes3 local reasoning model via Ollama." }, RequiresAiOllama: true),
        new("hermesd", "Hermes3 debug mode", "Launch Hermes3 debug mode", "[AI Agent & Ollama]", "AI / LLM",
            new[] { "hermesd — Launch Hermes3 in debug mode." }, RequiresAiOllama: true),
        new("ollama-status", "Ollama: Check Daemon Status", "Check local Ollama server status and pulled models", "[AI Agent & Ollama]", "AI / LLM",
            new[] { "ollama-status — Checks if Ollama server process is listening on http://localhost:11434." }, RequiresAiOllama: true),
        new("ollama-models", "Ollama: Manage Models", "List/inspect/delete pulled models", "[AI Agent & Ollama]", "AI / LLM",
            new[] { "ollama-models — Interactive model manager." }, RequiresAiOllama: true),
        new("ollama-pull", "Ollama: Pull New Model", "Fetch a new model", "[AI Agent & Ollama]", "AI / LLM",
            new[] { "ollama-pull — Download new model from Ollama library." }, RequiresAiOllama: true),
        new("ollama-start", "Ollama: Start Daemon", "Boot the background daemon", "[AI Agent & Ollama]", "AI / LLM",
            new[] { "ollama-start — Launches the background `ollama serve` process." }, RequiresAiOllama: true),
        new("ollama-logs", "Ollama: View Server Logs", "Show last 50 lines of server logs", "[AI Agent & Ollama]", "AI / LLM",
            new[] { "ollama-logs — Tails output log entries from local Ollama daemon." }, RequiresAiOllama: true),
        new("ollama-benchmark", "Ollama: Benchmark Models", "Benchmark performance of local Ollama models", "[AI Agent & Ollama]", "AI / LLM",
            new[] { "ollama-benchmark — Measures prompt evaluation speed (tokens/sec)." }, RequiresAiOllama: true),
        new("deck-status", "Antigravity Deck: Check Status", "Check if Antigravity Deck local server is running", "[AI Agent & Ollama]", "AI / LLM",
            new[] { "deck-status — Queries local port 3000 to verify Deck status." }),
        new("deck-setup", "Antigravity Deck: Setup/Initialize", "Setup local Antigravity Deck", "[AI Agent & Ollama]", "AI / LLM",
            new[] { "deck-setup — Initializes local Node.js environment for Deck." }),
        new("deck-start", "Antigravity Deck: Start Local", "Boot local Antigravity Deck", "[AI Agent & Ollama]", "AI / LLM",
            new[] { "deck-start — Launches Antigravity Deck dashboard at http://localhost:3000." }),
        new("deck-online", "Antigravity Deck: Go Online (Tunnel)", "Expose local Deck via tunnel", "[AI Agent & Ollama]", "AI / LLM",
            new[] { "deck-online — Exposes local Deck service via cloudflare/tailscale tunnel." }),
        new("agy-cli", "Launch Antigravity CLI (agy)", "Launch the google antigravity CLI tool terminal", "[AI Agent & Ollama]", "AI / LLM",
            new[] { "agy-cli — Launches google antigravity CLI executable session (`agy`)." }),
        new("ai-history", "AI History Ledger", "Show ledger of past AI invocations", "[AI Agent & Ollama]", "AI / LLM",
            new[] { "ai-history — Displays JSONL audit ledger of past AI agent invocations." }),

        // [AGY Account Switch]
        new("agyswitch", "Select Active Account", "Switch AGY account context", "[AGY Account Switch]", "Accounts",
            new[] { "agyswitch — Switch active Google AGY / Gemini account credentials context." }, RequiresAgy: true),
        new("agyquota", "View All Accounts", "Show quota usage summary for all accounts", "[AGY Account Switch]", "Accounts",
            new[] { "agyquota — Displays 5-hour and weekly request limits across registered accounts." }, RequiresAgy: true),
        new("account-tree", "Account Tree", "Show hierarchical active account details", "[AGY Account Switch]", "Accounts",
            new[] { "account-tree — Renders active account hierarchy and token status." }, RequiresAgy: true),
        new("quota-chart", "Quota Bar Chart", "Show bar chart of active account limits", "[AGY Account Switch]", "Accounts",
            new[] { "quota-chart — Renders colorized ASCII bar chart of quota consumption." }, RequiresAgy: true),
        new("live-dashboard", "Live Dashboard", "Show real-time active account metrics table", "[AGY Account Switch]", "Accounts",
            new[] { "live-dashboard — Live-updating multi-column table monitoring active accounts." }, RequiresAgy: true),
        new("autoswitch", "Toggle Auto-Switch", "Toggle automatic project account switching", "[AGY Account Switch]", "Accounts",
            new[] { "autoswitch — Enables/disables automatic account context switching." }, RequiresAgy: true),
        new("no-auto-commit", "Toggle Multi-Agent Auto-Commit", "Toggle automatic git commits during multi-agent AGY tasks", "[AGY Account Switch]", "Accounts",
            new[] { "no-auto-commit — Enables/disables automatic git commits during multi-agent AGY execution." }),
        new("autocommit", "Toggle Multi-Agent Auto-Commit (Alias)", "Alias for no-auto-commit toggle", "[AGY Account Switch]", "Accounts",
            new[] { "autocommit — Alias for no-auto-commit toggle." }),

        // [System & Network]
        new("disk", "Disk Usage", "Show disk usage and health", "[System & Network]", "System",
            new[] { "disk — Disk partitions, free space ratios, health status." }),
        new("usage", "Disk Usage (Alias)", "Alias for disk usage summary", "[System & Network]", "System",
            new[] { "usage — Alias for disk usage summary." }),
        new("public-ip", "Public IP Address", "Resolve public IPv4 address", "[System & Network]", "System",
            new[] { "public-ip — Resolve external IPv4 via REST fallback chain." }),
        new("myip", "Public IP Address (Alias)", "Alias for public-ip resolution", "[System & Network]", "System",
            new[] { "myip — Alias for public-ip resolution." }),
        new("kill-port", "Kill Port", "Kill process by port number", "[System & Network]", "System",
            new[] { "kill-port <n> — Terminate the process listening on TCP port <n>." }),
        new("ssh-info", "SSH Connection Info", "SSH connection summary", "[System & Network]", "SSH",
            new[] { "ssh-info — Local IPs, Tailscale address, active SSH connections." }),
        new("tailscale-status", "Tailscale Status", "Parse tailscale status --json for peer connectivity", "[System & Network]", "Network",
            new[] { "tailscale-status — Parses `tailscale status --json` to list connected mesh peers." }),
        new("ssh-qr", "SSH Terminal QR Code", "Generate terminal QR code for SSH connection parameters", "[System & Network]", "SSH",
            new[] { "ssh-qr — Renders terminal QR code containing SSH connection string." }),

        // [Learn & Study]
        new("learn", "Start Learning (auto)", "Start learning for a topic (auto-refresh)", "[Learn & Study]", "Learn",
            new[] { "learn — Launches interactive study learning router for selected topic." }),
        new("flashcard", "Flashcard Deck Browser", "Open flashcard deck browser", "[Learn & Study]", "Learn",
            new[] { "flashcard — Interactive flashcard deck viewer with SM-2 spaced-repetition scoring." }),
        new("vocab", "English Vocab Drill", "English vocabulary drill", "[Learn & Study]", "Learn",
            new[] { "vocab — Practice English vocabulary definitions, synonyms, and context sentences." }),
        new("kana", "Kana Quiz", "Hiragana / katakana quiz", "[Learn & Study]", "Learn",
            new[] { "kana — Interactive Japanese Hiragana and Katakana character recognition quiz." }),
        new("kanji", "Kanji Lookup", "Kanji lookup / stroke detail", "[Learn & Study]", "Learn",
            new[] { "kanji — Look up Japanese Kanji radicals, stroke counts, and readings." }),
        new("jlpt", "JLPT Vocab Drill", "JLPT vocabulary drill", "[Learn & Study]", "Learn",
            new[] { "jlpt — Vocabulary practice drills categorized by JLPT level (N5 to N1)." }),
        new("algo", "Algorithm Visualizer", "Algorithm visualizer (sort / search)", "[Learn & Study]", "Learn",
            new[] { "algo — Interactive terminal visualization for sorting and searching algorithms." }),
        new("complexity", "Big-O Complexity Sheet", "Big-O complexity cheat-sheet", "[Learn & Study]", "Learn",
            new[] { "complexity — Displays Big-O time and space complexity cheat-sheet table." }),
        new("problems", "DSA Problem Tracker", "DSA problem tracker", "[Learn & Study]", "Learn",
            new[] { "problems — Track status, difficulty, and notes for LeetCode / DSA practice problems." }),
        new("snippets", "Code Snippet Library", "Code snippet library browser", "[Learn & Study]", "Learn",
            new[] { "snippets — Browse and search reusable code snippets across multiple languages." }),
        new("sheets", "Cheat Sheet Browser", "Cheat-sheet browser (.txt files)", "[Learn & Study]", "Learn",
            new[] { "sheets — Browse text cheat-sheets stored in your local reference library." }),
        new("quiz", "C# Quiz", "C# multiple-choice quiz", "[Learn & Study]", "Learn",
            new[] { "quiz — Multiple-choice practice quiz testing C# and .NET concepts." }),
        new("interview", "Interview Question Bank", "Interview question bank", "[Learn & Study]", "Learn",
            new[] { "interview — Browse technical interview questions for system design and coding." }),
        new("star", "STAR Answer Builder", "STAR answer builder", "[Learn & Study]", "Learn",
            new[] { "star — Interactive wizard to structure behavioral responses using Situation, Task, Action, Result." }),
        new("mock", "Mock Interview Timer", "Mock interview timer", "[Learn & Study]", "Learn",
            new[] { "mock — Practice timed interview responses with an interactive stopwatch." }),
        new("word-of-day", "Word of the Day", "Show today's word of the day", "[Learn & Study]", "Learn",
            new[] { "word-of-day — Displays vocabulary word of the day with definition and usage example." }),

        // [Track & Progress]
        new("session", "Start Pomodoro Session", "Start a Pomodoro study session", "[Track & Progress]", "Tracking",
            new[] { "session — Launches 25-minute Pomodoro focus session timer." }),
        new("stats", "Study Statistics", "Study statistics and weekly chart", "[Track & Progress]", "Tracking",
            new[] { "stats — Displays weekly study volume breakdown and retention charts." }),
        new("goals", "Daily Goals", "Daily learning goals", "[Track & Progress]", "Tracking",
            new[] { "goals — View and manage daily learning targets and completed tasks." }),
        new("streak", "Study Streak", "Study streak display", "[Track & Progress]", "Tracking",
            new[] { "streak — Displays current consecutive daily study streak counter." }),
        new("due", "Due Reviews", "Show due spaced-repetition reviews", "[Track & Progress]", "Tracking",
            new[] { "due — Shows total count of flashcards due for SM-2 spaced repetition review today." }),
        new("progress", "Progress Dashboard", "Progress dashboard (bar chart + tree)", "[Track & Progress]", "Tracking",
            new[] { "progress — Renders visual progress bar charts across all learning domains." }),
        new("weak", "Weak Items Queue", "Weak items queue (pre-session review)", "[Track & Progress]", "Tracking",
            new[] { "weak — Review cards and concepts with low retention scores before starting a session." }),

        // [Obsidian & Resources]
        new("obsidian", "Obsidian Vault Config", "Configure / browse Obsidian vault", "[Obsidian & Resources]", "Obsidian",
            new[] { "obsidian — Configure local Obsidian vault directory path and sync settings." }),
        new("obs-graph", "Obsidian Graph View", "Obsidian wikilink graph", "[Obsidian & Resources]", "Obsidian",
            new[] { "obs-graph — Visualizes inter-note wikilink relationships in your Obsidian vault." }),
        new("refresh", "Refresh Learning Data", "Refresh learning data from vault", "[Obsidian & Resources]", "Resources",
            new[] { "refresh — Re-indexes Markdown flashcards and notes from Obsidian vault." }),
        new("add-resource", "Add Resource", "Add a file/URL to resource registry", "[Obsidian & Resources]", "Resources",
            new[] { "add-resource — Register a external file path or URL with custom tags." }),

        // [Appearance & Layout]
        new("mobile-setup", "Toggle Mobile Setup", "Toggle both prompt mobile mode and compact TUI layout mode", "[Appearance & Layout]", "Theme & Settings",
            new[] { "mobile-setup — Toggles compact prompt and high-density TUI layout." }),
        new("mobile", "Toggle Mobile Setup (Alias)", "Alias for mobile-setup", "[Appearance & Layout]", "Theme & Settings",
            new[] { "mobile — Alias for mobile-setup." }),
        new("theme", "Select Shell Theme", "Select Shell Theme", "[Appearance & Layout]", "Theme & Settings",
            new[] { "theme — Interactive theme picker for Oh-My-Posh prompt themes." }),
        new("ui-mode", "Toggle UI Layout Mode", "Toggle between three-pane and flat-tree layouts", "[Appearance & Layout]", "Theme & Settings",
            new[] { "ui-mode — Toggles between `three-pane` and `flat-tree` layout modes." }),
        new("density", "Toggle Console Density", "Toggle between comfortable and compact display densities", "[Appearance & Layout]", "Theme & Settings",
            new[] { "density — Toggles line spacing density between `comfortable` and `compact`." }),

        // [Help & Docs]
        new("cc", "Command Palette", "Open this Command Palette", "[Help & Docs]", "Help",
            new[] { "cc — Launches interactive Command Palette." }),
        new("help", "Help Browser", "Open interactive help browser", "[Help & Docs]", "Help",
            new[] { "help — Interactive browser listing all profile aliases, functions, and documentation." }),
        new("hotkeys", "Profile Hotkeys Guide", "Show all PowerShell profile shortcut hotkeys grouped by domain", "[Help & Docs]", "Help",
            new[] { "hotkeys — Displays profile keyboard shortcuts grouped by domain (git, docker, aws, sys, ai, nav)." })
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
