using System.Text.RegularExpressions;

namespace AgyTui.UI.Core.Commands;

public sealed record CommandEntry(
    string Alias,
    string DisplayName,
    string Description,
    string Category,      // TUI section: e.g. "[Workspace & Dev]"
    string HelpCategory,  // Help topic: e.g. "Git"
    string[] HelpLines,   // Detailed help text lines
    bool RequiresAiOllama = false,
    bool RequiresAgy = false
)
{
    public bool ShowInTree { get; set; } = true;
    public string? GroupPath { get; set; }
    public string? GroupName { get; set; }
    public int SortOrder { get; set; }
}

public static class CommandRegistry
{
    public static readonly CommandEntry[] All =
    [
        // [Workspace & Dev]
        new("proj", "Navigate Workspace", "Navigate to a registered workspace", "[Workspace & Dev]", "Navigation",
            [
                "proj <query> — Navigate to a workspace matching <query>.",
                " If multiple matches are found an interactive selector opens.",
                " If exactly one matches, jumps immediately.",
                " Alias: p, prj"
            ]),
        new("ide", "Terminal IDE", "Launch terminal IDE session", "[Workspace & Dev]", "IDE",
            [
                "ide — Interactive Terminal IDE with file Explorer, Diff Viewer, Code Viewer, and Symbol Search.",
                " Keys: ↑↓/j/k navigate | Enter select | / search symbols | q back."
            ]),
        new("exit", "Exit Control Center", "Clean exit from TUI", "[Help & Docs]", "System",
            ["exit — Clean exit from Control Center."]),
        new("prune-workspaces", "Prune Stale Workspaces", "Removes non-existent paths from workspace registry", "[Workspace & Dev]", "Navigation",
            ["prune-workspaces — Prunes missing directory paths from workspace registry."]) { ShowInTree = false },
        new("discover-workspaces", "Auto-Discover Projects", "Discovers unregistered projects in base directory", "[Workspace & Dev]", "Navigation",
            ["discover-workspaces — Auto-scans container paths for new projects."]) { ShowInTree = false },
        new("daily-note", "Open Today's Daily Note", "Opens today's Obsidian daily note", "[Workspace & Dev]", "Obsidian",
            ["daily-note — Opens today's markdown note in Obsidian vault."]),
        new("orphan-notes", "List Orphan Notes", "Finds notes with zero inbound/outbound wikilinks", "[Workspace & Dev]", "Obsidian",
            ["orphan-notes — Lists orphan notes without wikilinks."]),
        new("p", "Navigate Workspace (Alias)", "Alias for proj workspace navigation", "[Workspace & Dev]", "Navigation",
            ["p <query> — Quick alias for proj workspace navigation."]),
        new("prj", "Navigate Workspace (Alias)", "Alias for proj workspace navigation", "[Workspace & Dev]", "Navigation",
            ["prj <query> — Quick alias for proj workspace navigation."]),
        new("ide-diff", "Diff Viewer", "Git diff viewer for current workspace", "[Workspace & Dev]", "IDE",
            [
                "ide-diff — Full-screen colorized side-by-side / unified git diff viewer.",
                " Shows staged and unstaged file modifications across the workspace."
            ]) { ShowInTree = false },
        new("ide-search", "Search Across Files", "Search pattern and symbols across workspace files", "[Workspace & Dev]", "IDE",
            [
                "ide-search — Workspace-wide code and symbol search tool.",
                " Scans .cs, .ps1, .ts, .js, .py files for classes, methods, and functions."
            ]) { ShowInTree = false },
        new("scaffold", "Scaffold New Project", "Create new project from template", "[Workspace & Dev]", "Scaffold",
            [
                "scaffold — Interactive project boilerplate creator.",
                " Templates: webapi · console · react (Vite) · blazorwasm · classlib · worker"
            ]),
        new("go", "Navigate & Launch Workspace", "Search and navigate to workspace or launch terminal session", "[Workspace & Dev]", "Navigation",
            ["go <query> — Jump to project workspace or launch terminal session."]) { ShowInTree = false },
        new("dpack", "[.NET] Pack NuGet Package", "Create .nupkg release package via dotnet pack", "[Workspace & Dev]", ".NET",
            ["dpack — Compiles Release package and outputs .nupkg to ./nupkg directory."]),
        new("dpubpkg", "[.NET] Publish Package to NuGet", "Push .nupkg package to NuGet registry or local feed", "[Workspace & Dev]", ".NET",
            ["dpubpkg — Prompts for package and API key to publish to NuGet feed."]),
        new("rebuild-tui", "[.NET] Rebuild Control Center TUI", "Recompile AgyTui.csproj and refresh binary", "[Workspace & Dev]", ".NET",
            [
                "rebuild-tui — Triggers `dotnet build` on AgyTui.csproj with zero warnings/errors enforcement.",
                " Recompiles the TUI binary executable in-place."
            ]),
        // Git Tools (/git-tools & /repo-dashboards)
        new("gs", "Git Status (Native)", "Standard native git status command", "[Workspace & Dev]", "Git",
            ["gs — Standard native `git status` execution."]),
        new("gsu", "✨ Git Status (Custom TUI Table)", "Color-coded Spectre TUI git status table", "[Workspace & Dev]", "Git",
            ["gsu — Color-coded Spectre TUI table formatting for git status. Alias: gsi, +gs"]),
        new("ga", "Git Add All (Native)", "Stage all modified and new files in workspace", "[Workspace & Dev]", "Git",
            ["ga — Executes `git add .` to stage all modified, deleted, and untracked files."]),
        new("gb", "Git Branch (Native)", "Standard native git branch command", "[Workspace & Dev]", "Git",
            ["gb — Standard native `git branch` command."]),
        new("gbr", "✨ Git Branch Manager", "List local and remote branches sorted by recent activity with quick checkout", "[Workspace & Dev]", "Git",
            ["gbr — Interactive branch manager sorted by commit date. Select any branch to checkout instantly."]),
        new("co", "Git Checkout (Native)", "Checkout branch or commit reference", "[Workspace & Dev]", "Git",
            ["co <branch> — Executes native `git checkout <branch>`."]),
        new("cob", "New Git Branch (Native)", "Create and checkout new branch", "[Workspace & Dev]", "Git",
            ["cob <branch> — Executes native `git checkout -b <branch>`."]),
        new("gbd", "Delete Git Branch (Native)", "Delete local branch", "[Workspace & Dev]", "Git",
            ["gbd <branch> — Executes native `git branch -d <branch>`."]),
        new("gcommit", "Git Commit (Native)", "Commit staged changes with message", "[Workspace & Dev]", "Git",
            ["gcommit -m \"msg\" — Executes native `git commit -m \"msg\"`."]),
        new("gcmt", "✨ Conventional Commit", "Conventional commit wizard with optional AI diff draft", "[Workspace & Dev]", "Git",
            [
                "gcmt — Conventional commit wizard. Prompts for:",
                " 1. Type: feat | fix | docs | style | refactor | test | chore | ci",
                " 2. Scope (optional)",
                " 3. Short description (5–72 chars)",
                " 4. Breaking changes / issues closed"
            ]),
        new("glo", "Git Commit Log Graph (Native)", "Single-line branch commit log graph", "[Workspace & Dev]", "Git",
            ["glo — Executes `git log --graph --oneline --decorate --all`."]),
        new("glg", "Git Commit Log Graph (Alias)", "Alias for glo commit log graph", "[Workspace & Dev]", "Git",
            ["glg — Alias for glo commit log graph."]),
        new("glog", "Git Commit Log Pretty (Native)", "Pretty formatted commit history", "[Workspace & Dev]", "Git",
            ["glog — Executes `git log --pretty=format:\"%h - %an, %ar : %s\"`."]),
        new("glou", "✨ Git Commit Log Pager", "Interactive Spectre commit log pager", "[Workspace & Dev]", "Git",
            ["glou — Shows last 50 commits scrollable via built-in Spectre pager. Alias: gloi, +glo"]),
        new("gpull", "Git Pull Remote (Native)", "Pull latest commits from remote tracking branch", "[Workspace & Dev]", "Git",
            ["gpull — Executes native `git pull` on current branch."]),
        new("gpu", "Git Pull Remote (Alias)", "Alias for gpull remote pull", "[Workspace & Dev]", "Git",
            ["gpu — Alias for gpull remote pull."]),
        new("gpush", "Git Push Remote (Native)", "Push local commits to remote tracking branch", "[Workspace & Dev]", "Git",
            ["gpush — Executes native `git push`."]),
        new("gus", "Git Push Remote (Alias)", "Alias for gpush remote push", "[Workspace & Dev]", "Git",
            ["gus — Alias for gpush remote push."]),
        new("guf", "Git Push Force (Native)", "Force push local commits to remote", "[Workspace & Dev]", "Git",
            ["guf — Executes native `git push --force`."]),
        new("gf", "Git Fetch Remote (Native)", "Fetch latest branch references from remote repository", "[Workspace & Dev]", "Git",
            ["gf — Executes native `git fetch`."]),
        new("gd", "Git Diff (Native)", "Native git diff viewer for modified files", "[Workspace & Dev]", "Git",
            ["gd — Executes native `git diff`."]),
        new("gr", "Git Reset Soft (Native)", "Soft-reset HEAD to previous commit", "[Workspace & Dev]", "Git",
            ["gr — Executes native `git reset --soft HEAD~1`."]),
        new("grh", "Git Reset Hard (Native)", "Hard-reset working tree to HEAD", "[Workspace & Dev]", "Git",
            ["grh — Executes native `git reset --hard`."]),
        new("git-undo", "✨ Git Undo Last Commit", "Soft-reset the last local commit with TUI confirmation", "[Workspace & Dev]", "Git",
            ["git-undo — Soft-reset the last commit (`git reset --soft HEAD~1`) with interactive TUI prompt."]),
        new("gundo", "✨ Git Undo Last Commit (Alias)", "Alias for git-undo soft reset", "[Workspace & Dev]", "Git",
            ["gundo — Alias for git-undo soft reset."]),
        new("gclone", "Git Clone Project (Native)", "Clone git repository into working directory", "[Workspace & Dev]", "Git",
            ["gclone <url> — Executes native `git clone <url>`."]),
        new("gcloneu", "✨ Clone Project Assistant", "Interactive TUI repo cloning assistant", "[Workspace & Dev]", "Git",
            ["gcloneu — Interactive TUI prompt asking for repo URL, auto-resolving destination in ~/Documents."]),
        new("gremote", "Git Remotes List (Native)", "List remote repositories and URLs", "[Workspace & Dev]", "Git",
            ["gremote — Executes native `git remote -v`. Alias: grt"]),
        new("gremoteu", "✨ Git Remote Manager", "Interactive remote manager and fetch wizard", "[Workspace & Dev]", "Git",
            ["gremoteu — Custom TUI remote manager table with fetch & add actions. Alias: grtu"]),
        new("gco-remote", "Git Checkout Remote Branch (Native)", "Checkout tracking branch from remote", "[Workspace & Dev]", "Git",
            ["gco-remote <remote/branch> — Checkout tracking branch. Alias: cor"]),
        new("gmerge", "Git Merge Branch (Native)", "Merge specified branch into current HEAD", "[Workspace & Dev]", "Git",
            ["gmerge <branch> — Executes native `git merge <branch>`. Alias: gm"]),
        new("gmergeu", "✨ Git Merge Wizard", "Interactive branch merge selector and conflict launcher", "[Workspace & Dev]", "Git",
            ["gmergeu — Interactive branch picker for merging into HEAD. Alias: gmi"]),
        new("gconflict", "✨ Git Conflict Resolution Helper", "Inspect and resolve merge conflicts", "[Workspace & Dev]", "Git",
            ["gconflict — List unmerged conflict files and offer ours/theirs resolution actions. Alias: gcf, gconflictu, gcfu"]),
        new("gstash", "✨ Git Stash Manager", "List, save, pop, and apply git stashes", "[Workspace & Dev]", "Git",
            ["gstash — Interactive stash manager dashboard. Alias: gst, gstashu, gstu"]),
        new("grebase", "✨ Git Rebase Wizard", "Rebase current branch onto target branch", "[Workspace & Dev]", "Git",
            ["grebase <branch> — Rebase current branch onto target branch. Alias: grb, grebaseu, grbu"]),
        new("nexus", "✨ Repo Nexus Graph", "Git Nexus multi-repo dashboard", "[Workspace & Dev]", "Git",
            ["nexus — Renders a multi-repository workspace dependency and git status dashboard."]),
        new("repo-graph", "✨ Repository dependency graph", "Repository dependency graph", "[Workspace & Dev]", "Git",
            ["repo-graph — Displays dependency tree and inter-project relationship links."]),
        new("nexus-stats", "✨ Git Nexus commit stats", "Git Nexus commit stats", "[Workspace & Dev]", "Git",
            ["nexus-stats — Summarizes commit velocity, active authors, and modification volume across repos."]),

        // .NET Tools (/dotnet-tools)
        new("dbld", "[.NET] Build Project (Native)", "dotnet build in active workspace", "[Workspace & Dev]", ".NET",
            ["dbld — Standard native `dotnet build` in active workspace."]),
        new("db", "[.NET] Build Project (Native Alias)", "Alias for dbld dotnet build", "[Workspace & Dev]", ".NET",
            ["db — Alias for dbld dotnet build."]),
        new("dbldu", "✨ [.NET] Build Project", "Build project with Spectre progress spinner & summary", "[Workspace & Dev]", ".NET",
            ["dbldu — Custom TUI build runner with progress animation and warning/error summary. Alias: dbu"]),
        new("dr", "[.NET] Run Project (Native)", "dotnet run active project in workspace", "[Workspace & Dev]", ".NET",
            ["dr — Standard native `dotnet run` in active workspace."]),
        new("dru", "✨ [.NET] Run Project", "Interactive process runner & log viewer", "[Workspace & Dev]", ".NET",
            ["dru — Custom TUI runner for project execution."]),
        new("dtst", "[.NET] Test Project (Native)", "dotnet test in active workspace", "[Workspace & Dev]", ".NET",
            ["dtst — Standard native `dotnet test` in active workspace."]),
        new("dt", "[.NET] Test Project (Native Alias)", "Alias for dtst dotnet test", "[Workspace & Dev]", ".NET",
            ["dt — Alias for dtst dotnet test."]),
        new("dtstu", "✨ [.NET] Test Project", "Interactive test runner & Spectre result table", "[Workspace & Dev]", ".NET",
            ["dtstu — Custom TUI test runner with test result table. Alias: dtu"]),
        new("df", "[.NET] Format Code (Native)", "dotnet format code style & linting rules", "[Workspace & Dev]", ".NET",
            ["df — Runs native `dotnet format` to apply standard C# formatting rules."]),
        new("dcl", "[.NET] Clean Solution (Native)", "dotnet clean build output directory", "[Workspace & Dev]", ".NET",
            ["dcl — Runs native `dotnet clean` to clear build target outputs."]),
        new("drestore", "[.NET] Restore Packages (Native)", "dotnet restore packages in active workspace", "[Workspace & Dev]", ".NET",
            ["drestore — Executes native `dotnet restore` to resolve NuGet package dependencies."]),
        new("dres", "[.NET] Restore Packages (Native Alias)", "Alias for drestore dotnet restore", "[Workspace & Dev]", ".NET",
            ["dres — Alias for drestore dotnet restore."]),
        new("dpublish", "[.NET] Publish Release (Native)", "dotnet publish release binary in active workspace", "[Workspace & Dev]", ".NET",
            ["dpublish — Executes native `dotnet publish -c Release` for production binaries."]),
        new("dwatch", "[.NET] Watch Live-Reload (Native)", "dotnet watch run continuous dev loop", "[Workspace & Dev]", ".NET",
            ["dwatch — Runs native `dotnet watch run` for continuous live-reloading."]),
        new("dw", "[.NET] Watch Live-Reload (Native Alias)", "Alias for dwatch live reload", "[Workspace & Dev]", ".NET",
            ["dw — Alias for dwatch live reload."]),
        new("clean-build", "✨ Clean & Rebuild Artifacts", "Remove bin/ and obj/ recursively", "[Workspace & Dev]", ".NET",
            ["clean-build — Targeted bin/ and obj/ directory purge with lock handle checks."]),
        new("dclean", "✨ Clean & Rebuild Artifacts (Alias)", "Alias for clean-build", "[Workspace & Dev]", ".NET",
            ["dclean — Alias for clean-build."]),
        new("rebuild-tui", "✨ Rebuild AgyTui Executable", "Recompile AgyTui single-file binary in-place", "[Workspace & Dev]", ".NET",
            ["rebuild-tui — Recompiles AgyTui.exe binary in-place."]),
        new("add-migration", "[.NET] Add EF Migration", "EF Core: add migration", "[Workspace & Dev]", ".NET",
            ["add-migration <name> — Runs `dotnet ef migrations add <name>`."]),
        new("da", "[.NET] Add EF Migration (Alias)", "Alias for add-migration", "[Workspace & Dev]", ".NET",
            ["da — Alias for add-migration."]),
        new("update-db", "[.NET] Update EF Database", "EF Core: update database", "[Workspace & Dev]", ".NET",
            ["update-db — Runs `dotnet ef database update`."]),
        new("du", "[.NET] Update EF Database (Alias)", "Alias for update-db", "[Workspace & Dev]", ".NET",
            ["du — Alias for update-db."]),

        // Docker Tools (/docker-tools)
        new("dk", "Docker Container List (Native)", "Standard native docker ps container list", "[Workspace & Dev]", "Docker",
            ["dk — Standard native `docker ps` execution."]),
        new("dku", "✨ Docker Container Dashboard", "Live Spectre TUI container status table", "[Workspace & Dev]", "Docker",
            ["dku — Color-coded Spectre TUI table for container status & ports. Alias: dki"]),
        new("docker-health", "✨ Docker Health Check", "Show container health & resource utilization", "[Workspace & Dev]", "Docker",
            ["docker-health — Displays real-time CPU, memory, network I/O, and daemon connectivity."]),
        new("dkcl", "✨ Clean Docker Containers", "Docker cleanup TUI dashboard", "[Workspace & Dev]", "Docker",
            ["dkcl — Docker cleanup TUI dashboard for containers, images, and volumes."]),
        new("dkrmac", "Docker Remove All Containers (Native)", "Stop and remove all Docker containers forcefully", "[Workspace & Dev]", "Docker",
            ["dkrmac — Forcefully stops and removes all Docker containers."]),
        new("dkstac", "Docker Stop All Containers (Native)", "Stop all running Docker containers", "[Workspace & Dev]", "Docker",
            ["dkstac — Sends SIGTERM to stop all active Docker containers."]),
        new("dimg", "Docker Images List (Native)", "Standard native docker images execution", "[Workspace & Dev]", "Docker",
            ["dimg — Standard native `docker images`."]),
        new("dimgu", "✨ Docker Image Manager", "Spectre TUI image manager & dangling layer cleaner", "[Workspace & Dev]", "Docker",
            ["dimgu — Custom TUI image manager table."]),
        new("dlogs", "Docker Container Logs (Native)", "Standard native docker logs tailing", "[Workspace & Dev]", "Docker",
            ["dlogs — Standard native `docker logs`."]),
        new("dlogsu", "✨ Container Log Tailer", "Scrollable Spectre Pager container log viewer", "[Workspace & Dev]", "Docker",
            ["dlogsu — Custom TUI container log tailer."]),
        new("dcup", "Docker Compose Up (Native)", "docker compose up -d", "[Workspace & Dev]", "Docker",
            ["dcup — Runs native `docker compose up -d`."]),
        new("dkcpu", "Docker Compose Up (Alias)", "Alias for dcup", "[Workspace & Dev]", "Docker",
            ["dkcpu — Alias for dcup."]),
        new("dcdown", "Docker Compose Down (Native)", "docker compose down", "[Workspace & Dev]", "Docker",
            ["dcdown — Runs native `docker compose down`."]),
        new("dkcpd", "Docker Compose Down (Alias)", "Alias for dcdown", "[Workspace & Dev]", "Docker",
            ["dkcpd — Alias for dcdown."]),

        // AWS Tools (/aws-tools)
        new("aws-whoami", "AWS Identity Info (Native)", "Inspect active AWS STS caller identity", "[Workspace & Dev]", "AWS",
            ["aws-whoami — Executes native `aws sts get-caller-identity`."]),
        new("aws-whoamiu", "✨ AWS Identity Inspector", "Spectre TUI identity & credential inspector card", "[Workspace & Dev]", "AWS",
            ["aws-whoamiu — Formatted Spectre card showing Account ID, IAM Arn, and region."]),
        new("aws-local", "✨ LocalStack Service Check", "LocalStack sandbox diagnostics", "[Workspace & Dev]", "AWS / LocalStack",
            ["aws-local — Query running LocalStack sandbox on http://localhost:4566."]),
        new("aws-s3", "AWS S3 Buckets (Native)", "List local or cloud S3 buckets via CLI", "[Workspace & Dev]", "AWS",
            ["aws-s3 — Executes native `aws s3 ls`."]),
        new("aws-s3u", "✨ AWS S3 Bucket Explorer", "Spectre TUI S3 bucket inspector table", "[Workspace & Dev]", "AWS",
            ["aws-s3u — Color-coded Spectre table listing buckets, creation dates, and object counts."]),
        new("aws-sqs", "AWS SQS Queues (Native)", "List local or cloud SQS queues", "[Workspace & Dev]", "AWS",
            ["aws-sqs — Executes native `aws sqs list-queues`."]),
        new("aws-ssm", "AWS SSM Parameter Store (Native)", "Inspect Parameter Store key-value pairs", "[Workspace & Dev]", "AWS",
            ["aws-ssm — Executes native `aws ssm describe-parameters`."]),
        new("aws-sns", "AWS SNS Topics (Native)", "Inspect notification topics", "[Workspace & Dev]", "AWS",
            ["aws-sns — Executes native `aws sns list-topics`."]),
        new("aws-dynamodb", "AWS DynamoDB Tables (Native)", "Inspect DynamoDB tables", "[Workspace & Dev]", "AWS",
            ["aws-dynamodb — Executes native `aws dynamodb list-tables`."]),
        new("aws-lambda", "AWS Lambda Functions (Native)", "Inspect serverless functions", "[Workspace & Dev]", "AWS",
            ["aws-lambda — Executes native `aws lambda list-functions`."]),

        // [AI Agent & Ollama]
        new("ai", "Invoke Local AI Agent", "Launch local AI pair programming agent or query", "[AI Agent & Ollama]", "AI / LLM",
            ["ai [query] — Launch local AI agent query or interactive picker."], RequiresAiOllama: true),
        new("cai", "Invoke AI Agent (Alias)", "Alias for ai command", "[AI Agent & Ollama]", "AI / LLM",
            ["cai [query] — Alias for ai command."], RequiresAiOllama: true),
        new("openclaw", "OpenClaw (Ollama)", "Launch OpenClaw via Ollama", "[AI Agent & Ollama]", "AI / LLM",
            ["openclaw — Launch OpenClaw model via local Ollama daemon."], RequiresAiOllama: true),
        new("hermes", "Hermes3 (Ollama)", "Launch Hermes3 via Ollama", "[AI Agent & Ollama]", "AI / LLM",
            ["hermes — Launch Hermes3 local reasoning model via Ollama."], RequiresAiOllama: true),
        new("hermesd", "Hermes3 debug mode", "Launch Hermes3 debug mode", "[AI Agent & Ollama]", "AI / LLM",
            ["hermesd — Launch Hermes3 in debug mode."], RequiresAiOllama: true),
        new("ollama", "Ollama: Local AI Center", "Interactive dashboard for Ollama models & daemon", "[AI Agent & Ollama]", "AI / LLM",
            ["ollama — Interactive local AI daemon & model management center."]),
        new("ollama-status", "Ollama: Check Daemon Status", "Check local Ollama server status and pulled models", "[AI Agent & Ollama]", "AI / LLM",
            ["ollama-status — Checks if Ollama server process is listening on http://localhost:11434."], RequiresAiOllama: true),
        new("ollama-models", "Ollama: Manage Models", "List/inspect/delete pulled models", "[AI Agent & Ollama]", "AI / LLM",
            ["ollama-models — Interactive model manager."], RequiresAiOllama: true),
        new("ollama-pull", "Ollama: Pull New Model", "Fetch a new model", "[AI Agent & Ollama]", "AI / LLM",
            ["ollama-pull — Download new model from Ollama library."], RequiresAiOllama: true),
        new("ollama-start", "Ollama: Start Daemon", "Boot the background daemon", "[AI Agent & Ollama]", "AI / LLM",
            ["ollama-start — Launches the background `ollama serve` process."], RequiresAiOllama: true),
        new("ollama-logs", "Ollama: View Server Logs", "Show last 50 lines of server logs", "[AI Agent & Ollama]", "AI / LLM",
            ["ollama-logs — Tails output log entries from local Ollama daemon."], RequiresAiOllama: true),
        new("ollama-benchmark", "Ollama: Benchmark Models", "Benchmark performance of local Ollama models", "[AI Agent & Ollama]", "AI / LLM",
            ["ollama-benchmark — Measures prompt evaluation speed (tokens/sec)."], RequiresAiOllama: true),
        new("agy-cli", "Launch Antigravity CLI (agy)", "Launch the google antigravity CLI tool terminal", "[AI Agent & Ollama]", "AI / LLM",
            ["agy-cli — Launches google antigravity CLI executable session (`agy`)."]),
        new("ai-history", "AI History Ledger", "Show ledger of past AI invocations", "[AI Agent & Ollama]", "AI / LLM",
            ["ai-history — Displays JSONL audit ledger of past AI agent invocations."]),
        new("cnav", "Registered Workspace Navigator", "Interactive selector for all registered workspaces", "[Workspace & Dev]", "Navigation",
            ["cnav — Interactive search and navigator for all registered project workspaces."]),
        new("dotnet-info", "[.NET] System & SDK Info", "Display dotnet environment and SDK version details", "[Workspace & Dev]", ".NET",
            ["dotnet-info — Runs `dotnet --info` to display installed SDKs, runtimes, and environment details."]),
        // [System & Network]
        new("disk", "Disk Usage", "Show disk usage and health", "[System & Network]", "System",
            ["disk — Disk partitions, free space ratios, health status."]),
        new("usage", "Disk Usage (Alias)", "Alias for disk usage summary", "[System & Network]", "System",
            ["usage — Alias for disk usage summary."]) { ShowInTree = false },
        new("public-ip", "Public IP Address", "Resolve public IPv4 address", "[System & Network]", "System",
            ["public-ip — Resolve external IPv4 via REST fallback chain."]),
        new("myip", "Public IP Address (Alias)", "Alias for public-ip resolution", "[System & Network]", "System",
            ["myip — Alias for public-ip resolution."]) { ShowInTree = false },
        new("kill-port", "Kill Port", "Kill process by port number", "[System & Network]", "System",
            ["kill-port <n> — Terminate the process listening on TCP port <n>."]),
        new("ssh-info", "SSH Connection Info", "SSH connection summary", "[System & Network]", "SSH",
            ["ssh-info — Local IPs, Tailscale address, active SSH connections."]),
        new("tailscale-status", "Tailscale Status", "Parse tailscale status --json for peer connectivity", "[System & Network]", "Network",
            ["tailscale-status — Parses `tailscale status --json` to list connected mesh peers."]),
        new("ssh-qr", "SSH Terminal QR Code", "Generate terminal QR code for SSH connection parameters", "[System & Network]", "SSH",
            ["ssh-qr — Renders terminal QR code containing SSH connection string."]),
        new("system-reload", "System & Terminal Reload Menu", "Interactive menu to reload CC TUI or Terminal profile & session", "[System & Network]", "Reload",
            ["system-reload — Interactive menu to reload CC TUI or Terminal session."]) { GroupPath = "/system-reload" },
        new("sys-reload", "System & Terminal Reload (Alias)", "Alias for system-reload menu", "[System & Network]", "Reload",
            ["sys-reload — Alias for system-reload."]) { GroupPath = "/system-reload", ShowInTree = false },
        new("reload-cc", "Reload Control Center TUI", "Rebuild code and restart Control Center TUI binary session", "[System & Network]", "Reload",
            ["reload-cc — Rebuild code and restart Control Center TUI binary session."]) { GroupPath = "/system-reload" },
        new("rcc", "Reload Control Center TUI (Alias)", "Alias for reload-cc", "[System & Network]", "Reload",
            ["rcc — Alias for reload-cc."]) { GroupPath = "/system-reload", ShowInTree = false },
        new("reload-term", "Reload Terminal Profile", "Reload PowerShell profile ($PROFILE) and refresh active terminal environment", "[System & Network]", "Reload",
            ["reload-term — Reload PowerShell profile ($PROFILE) and refresh active terminal environment."]) { GroupPath = "/system-reload" },
        new("rterm", "Reload Terminal Profile (Alias)", "Alias for reload-term", "[System & Network]", "Reload",
            ["rterm — Alias for reload-term."]) { GroupPath = "/system-reload", ShowInTree = false },
        new("reload-all", "Reload Terminal & Control Center", "Full system refresh (Reload $PROFILE, rebuild AgyTui, and restart TUI)", "[System & Network]", "Reload",
            ["reload-all — Full system refresh: reload $PROFILE and rebuild/restart TUI."]) { GroupPath = "/system-reload" },
        new("rall", "Reload Terminal & Control Center (Alias)", "Alias for reload-all", "[System & Network]", "Reload",
            ["rall — Alias for reload-all."]) { GroupPath = "/system-reload", ShowInTree = false },

        // [Obsidian Vault in Workspace & Dev]
        new("obsidian", "Obsidian Vault Browser", "Search, browse by tag, and view daily notes in vault", "[Workspace & Dev]", "Obsidian",
            ["obsidian — Interactive Obsidian Vault note search, tag browser, daily notes, and graph renderer."]),
        new("refresh", "Rescan & Sync Vault Datasets", "Rescan Obsidian Vault and sync datasets", "[Workspace & Dev]", "Obsidian",
            ["refresh — Rescans Obsidian Vault notes and syncs datasets."]),
        new("vault-open", "Open Vault Folder", "Open Obsidian Vault directory in Windows Explorer", "[Workspace & Dev]", "Obsidian",
            ["vault-open — Opens Obsidian Vault directory in Windows File Explorer."]),
        new("obs-graph", "Obsidian Graph View", "Obsidian wikilink graph", "[Workspace & Dev]", "Obsidian",
            ["obs-graph — Visualizes inter-note wikilink relationships in your Obsidian vault."]),
        new("add-resource", "Add Resource", "Add a file/URL to resource registry", "[Workspace & Dev]", "Resources",
            ["add-resource — Register a external file path or URL with custom tags."]),

        // [Appearance & Favorites in System & Network]
        new("mobile-setup", "Toggle Mobile Setup", "Toggle both prompt mobile mode and compact TUI layout mode", "[System & Network]", "Theme & Settings",
            ["mobile-setup — Toggles compact prompt and high-density TUI layout."]),
        new("mobile", "Toggle Mobile Setup (Alias)", "Alias for mobile-setup", "[System & Network]", "Theme & Settings",
            ["mobile — Alias for mobile-setup."]),
        new("theme", "Select Shell Theme", "Select Shell Theme", "[System & Network]", "Theme & Settings",
            ["theme — Interactive theme picker for Oh-My-Posh prompt themes."]),
        new("ui-mode", "Toggle UI Layout Mode", "Toggle between three-pane and flat-tree layouts", "[System & Network]", "Theme & Settings",
            ["ui-mode — Toggles between `three-pane` and `flat-tree` layout modes."]),
        new("density", "Toggle Console Density", "Toggle between comfortable and compact display densities", "[System & Network]", "Theme & Settings",
            ["density — Toggles line spacing density between `comfortable` and `compact`."]),
        new("favorites", "List Favorite Commands", "List all pinned favorite command aliases", "[System & Network]", "Theme & Settings",
            ["favorites — Displays all command aliases currently pinned to Favorites."]),

        // [Help & Docs]
        new("cc", "Command Palette", "Open this Command Palette", "[Help & Docs]", "Help",
            ["cc — Launches interactive Command Palette."]),
        new("help", "Help Browser", "Open interactive help browser", "[Help & Docs]", "Help",
            ["help — Interactive browser listing all profile aliases, functions, and documentation."])
    ];

    static CommandRegistry()
    {
        var workspaceDevCmds = new HashSet<string> { "proj", "ide", "scaffold" };

        var gitCmds = new HashSet<string> {
            "gs", "gsu", "ga", "gb", "gbr", "co", "cob", "gbd", "gcommit", "gcmt",
            "glo", "glg", "glog", "glou", "gpull", "gpu", "gpush", "gus", "guf",
            "gf", "gd", "gr", "grh", "git-undo", "gundo", "gclone", "gcloneu",
            "gremote", "gremoteu", "gco-remote", "grt", "grtu", "cor",
            "gmerge", "gmergeu", "gconflict", "gstash", "grebase",
            "gm", "gmi", "gcf", "gconflictu", "gcfu", "gst", "gstashu", "gstu", "grb", "grebaseu", "grbu"
        };
        var repoCmds = new HashSet<string> { "nexus", "repo-graph", "nexus-stats" };
        var dotnetCmds = new HashSet<string> { "dbld", "db", "dbldu", "dbu", "dr", "dru", "dtst", "dt", "dtstu", "dtu", "df", "dcl", "drestore", "dres", "dpublish", "dwatch", "dw", "rebuild-tui", "clean-build", "dclean", "add-migration", "da", "update-db", "du", "dpack", "dpubpkg", "dotnet-info" };
        var dockerCmds = new HashSet<string> { "dk", "dku", "dki", "docker-health", "dkcl", "dkrmac", "dkstac", "dimg", "dimgu", "dlogs", "dlogsu", "dcup", "dkcpu", "dcdown", "dkcpd" };
        var awsCmds = new HashSet<string> { "aws-whoami", "aws-whoamiu", "aws-local", "aws-s3", "aws-s3u", "aws-sqs", "aws-ssm", "aws-sns", "aws-dynamodb", "aws-lambda" };
        var ollamaCmds = new HashSet<string> { "ollama", "ai", "openclaw", "hermes", "hermesd", "agy-cli", "ai-history", "ollama-status", "ollama-models", "ollama-pull", "ollama-start", "ollama-logs", "ollama-benchmark" };
        var sshCmds = new HashSet<string> { "ssh-info", "tailscale-status", "ssh-qr" };
        var obsidianCmds = new HashSet<string> { "obsidian", "refresh", "vault-open", "daily-note", "orphan-notes", "obs-graph", "add-resource" };
        var appearanceCmds = new HashSet<string> { "theme", "ui-mode", "density", "mobile-setup", "favorites" };

        var hiddenCmds = new HashSet<string> {
            "cai",
            "p", "prj", "cnav", "go", "prune-workspaces", "discover-workspaces", "ide-diff", "ide-search",
            "glg", "gpu", "gus", "gundo", "grt", "grtu", "cor",
            "gm", "gmi", "gcf", "gconflictu", "gcfu", "gst", "gstashu", "gstu", "grb", "grebaseu", "grbu",
            "db", "dbu", "dt", "dtu", "dw", "da", "du", "dres", "dclean", "dki", "dkcpu", "dkcpd", "mobile",
            "hermes", "hermesd", "openclaw", "agy-cli", "ai-history",
            "ollama-status", "ollama-models", "ollama-pull", "ollama-start", "ollama-logs", "ollama-benchmark"
        };

        var orderedAliases = new[]
        {
            // Category 1: [Favorites]
            "proj", "ide", "ollama",

            // Category 2: [Workspace & Dev]
            "proj", "ide", "scaffold",
            "gs", "gsu", "ga", "gb", "gbr", "co", "cob", "gbd", "gcommit", "gcmt", "glo", "glog", "glou", "gpull", "gpush", "guf", "gf", "gd", "gr", "grh", "git-undo", "gclone", "gcloneu",
            "nexus", "repo-graph", "nexus-stats",
            "dbld", "dr", "dtst", "df", "dcl", "drestore", "dpublish", "dpack", "dpubpkg", "dwatch", "rebuild-tui", "clean-build", "add-migration", "update-db", "dotnet-info",
            "docker-health", "dkcl", "dkrmac", "dkstac", "dimg", "dlogs", "dcup", "dcdown",
            "aws-whoami", "aws-local", "aws-s3", "aws-sqs", "aws-ssm", "aws-sns", "aws-dynamodb", "aws-lambda",
            "obsidian", "daily-note", "vault-open", "refresh", "orphan-notes", "obs-graph", "add-resource",

            // Category 3: [AI Agent & Ollama]
            "ollama", "ai",

            // Category 4: [System & Network]
            "theme", "ui-mode", "density", "favorites", "mobile-setup",
            "disk", "public-ip", "kill-port", "ssh-info",
            "tailscale-status", "ssh-qr",
            "system-reload", "reload-cc", "reload-term", "reload-all",

            // Category 5: [Help & Docs]
            "cc", "help", "exit"
        };

        var orderMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < orderedAliases.Length; i++)
        {
            orderMap[orderedAliases[i]] = i;
        }

        foreach (var cmd in All)
        {
            var alias = cmd.Alias;
            if (hiddenCmds.Contains(alias))
            {
                cmd.ShowInTree = false;
                continue;
            }

            if (orderMap.TryGetValue(alias, out var order))
            {
                cmd.SortOrder = order;
            }

            if (workspaceDevCmds.Contains(alias)) { cmd.GroupPath = "/workspace-dev"; cmd.GroupName = "Workspace & Developer Tools"; }
            else if (repoCmds.Contains(alias)) { cmd.GroupPath = "/git-nexus"; cmd.GroupName = "Git Nexus Graph & Stats"; }
            else if (gitCmds.Contains(alias)) { cmd.GroupPath = "/git-tools"; cmd.GroupName = "Git & Repo Tools"; }
            else if (dotnetCmds.Contains(alias)) { cmd.GroupPath = "/dotnet-tools"; cmd.GroupName = ".NET Project Tools"; }
            else if (dockerCmds.Contains(alias)) { cmd.GroupPath = "/docker-tools"; cmd.GroupName = "Docker Tools"; }
            else if (awsCmds.Contains(alias)) { cmd.GroupPath = "/aws-tools"; cmd.GroupName = "AWS Tools"; }
            else if (ollamaCmds.Contains(alias)) { cmd.GroupPath = "/ollama-tools"; cmd.GroupName = "Ollama & Local AI Agents"; }
            else if (obsidianCmds.Contains(alias)) { cmd.GroupPath = "/obsidian-vault"; cmd.GroupName = "Obsidian Vault & Resources"; }
            else if (appearanceCmds.Contains(alias)) { cmd.GroupPath = "/appearance-favs"; cmd.GroupName = "Appearance & Favorites"; }
            else if (sshCmds.Contains(alias)) { cmd.GroupPath = "/ssh-tools"; cmd.GroupName = "SSH & Network Tools"; }
        }
    }

    public static CommandEntry? GetByAlias(string alias)
    {
        return All.FirstOrDefault(c => string.Equals(c.Alias, alias, StringComparison.OrdinalIgnoreCase));
    }

    public static void AssertSwitchCases()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        string? routerCsPath = null;
        while (dir != null)
        {
            var p = Path.Combine(dir.FullName, "CommandRouter.cs");
            if (File.Exists(p)) { routerCsPath = p; break; }
            var sub = Path.Combine(dir.FullName, "csapp", "AgyTui", "UI", "Core", "Navigation", "CommandRouter.cs");
            if (File.Exists(sub)) { routerCsPath = sub; break; }
            var subNew = Path.Combine(dir.FullName, "UI", "Core", "Navigation", "CommandRouter.cs");
            if (File.Exists(subNew)) { routerCsPath = subNew; break; }
            dir = dir.Parent;
        }
        if (routerCsPath == null) return;

        string code = File.ReadAllText(routerCsPath);
        var matches = Regex.Matches(code, @"case\s+""([^""]+)""\s*:");
        var handledCases = matches.Select(m => m.Groups[1].Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unhandled = All.Where(c => !handledCases.Contains(c.Alias)).Select(c => c.Alias).ToList();
        if (unhandled.Count > 0)
        {
            throw new InvalidOperationException($"The following CommandRegistry aliases have no switch case in CommandRouter.cs: {string.Join(", ", unhandled)}");
        }
    }

    public static void AssertAllAliasesReachable(MenuNode root)
    {
        var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Traverse(MenuNode node)
        {
            if (node.Command != null) reachable.Add(node.Command.Alias);
            foreach (var child in node.Children) Traverse(child);
        }
        Traverse(root);

        var mainCommands = All.Where(c => c.ShowInTree && !c.Description.StartsWith("Alias for", StringComparison.OrdinalIgnoreCase));
        var unhandled = mainCommands.Where(c => !reachable.Contains(c.Alias)).Select(c => c.Alias).ToList();
        if (unhandled.Count > 0)
        {
            throw new InvalidOperationException($"The following main CommandRegistry aliases are unreachable in MenuNode tree: {string.Join(", ", unhandled)}");
        }
    }
}
