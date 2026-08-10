using System.Text.RegularExpressions;

namespace AgyTui.UI.Core.Registries;

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
                "ide — Interactive Terminal IDE with file Explorer, Code Viewer, and Symbol Search.",
                " Keys: ↑↓/j/k navigate | Enter select | / search symbols | q back."
            ]),
        new("ask-ai", "Antigravity AI Agent", "AI pair programming deck & chat", "[AI Agent & Ollama]", "AI",
            ["ask-ai — Interactive AI pair programming deck & chat."]),
        new("vault", "Vault & Account Quotas", "Manage tokens, quota, and credentials", "[AGY Account Switch]", "Security",
            ["vault — Manage tokens, quota, and credentials."]),
        new("exit", "Exit Control Center", "Clean exit from TUI", "[Help & Docs]", "System",
            ["exit — Clean exit from Control Center."]),
        new("prune-workspaces", "Prune Stale Workspaces", "Removes non-existent paths from workspace registry", "[Workspace & Dev]", "Navigation",
            ["prune-workspaces — Prunes missing directory paths from workspace registry."]),
        new("discover-workspaces", "Auto-Discover Projects", "Discovers unregistered projects in base directory", "[Workspace & Dev]", "Navigation",
            ["discover-workspaces — Auto-scans container paths for new projects."]),
        new("daily-note", "Open Today's Daily Note", "Opens today's Obsidian daily note", "[Obsidian & Resources]", "Obsidian",
            ["daily-note — Opens today's markdown note in Obsidian vault."]),
        new("orphan-notes", "List Orphan Notes", "Finds notes with zero inbound/outbound wikilinks", "[Obsidian & Resources]", "Obsidian",
            ["orphan-notes — Lists orphan notes without wikilinks."]),
        new("mastery-tree", "Topic Mastery Tree", "Displays topic mastery tree view", "[Learn & Study]", "Learn",
            ["mastery-tree — Displays learning topic mastery hierarchy."]),
        new("p", "Navigate Workspace (Alias)", "Alias for proj workspace navigation", "[Workspace & Dev]", "Navigation",
            ["p <query> — Quick alias for proj workspace navigation."]),
        new("prj", "Navigate Workspace (Alias)", "Alias for proj workspace navigation", "[Workspace & Dev]", "Navigation",
            ["prj <query> — Quick alias for proj workspace navigation."]),
        new("ide-diff", "Diff Viewer", "Git diff viewer for current workspace", "[Workspace & Dev]", "IDE",
            [
                "ide-diff — Full-screen colorized side-by-side / unified git diff viewer.",
                " Shows staged and unstaged file modifications across the workspace."
            ]),
        new("ide-search", "Search Across Files", "Search pattern and symbols across workspace files", "[Workspace & Dev]", "IDE",
            [
                "ide-search — Workspace-wide code and symbol search tool.",
                " Scans .cs, .ps1, .ts, .js, .py files for classes, methods, and functions."
            ]),
        new("scaffold", "Scaffold New Project", "Create new project from template", "[Workspace & Dev]", "Scaffold",
            [
                "scaffold — Interactive project boilerplate creator.",
                " Templates: webapi · console · react (Vite) · blazorwasm · classlib · worker"
            ]),
        new("go", "Navigate & Launch Workspace", "Search and navigate to workspace or launch terminal session", "[Workspace & Dev]", "Navigation",
            ["go <query> — Jump to project workspace or launch terminal session."]),
        new("open-term", "Open New Terminal Session", "Launch new Windows Terminal / PowerShell session in workspace", "[Workspace & Dev]", "Navigation",
            ["open-term — Spawns a new Windows Terminal (wt.exe) or PowerShell window in active directory."]),
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

        // Secret Vault Tools
        new("secret-set", "Set Encrypted Secret", "Store an encrypted key-value pair in AgySecretVault", "[AGY Account Switch]", "Security",
            ["secret-set <key> <value> — Encrypt and save secret key in vault."], RequiresAgy: true),
        new("secret-get", "Get Encrypted Secret", "Decrypt and print secret key value from AgySecretVault", "[AGY Account Switch]", "Security",
            ["secret-get <key> — Decrypt and display secret key value."], RequiresAgy: true),
        new("secret-list", "List Encrypted Secrets", "List all encrypted secret keys in AgySecretVault", "[AGY Account Switch]", "Security",
            ["secret-list — Display all encrypted secret keys."], RequiresAgy: true),
        new("secret-remove", "Remove Encrypted Secret", "Remove secret key from AgySecretVault", "[AGY Account Switch]", "Security",
            ["secret-remove <key> — Remove secret key from vault."], RequiresAgy: true),

        // [AI Agent & Ollama]
        new("ai", "Invoke AI Agent", "Launch AI pair programming deck or query", "[AI Agent & Ollama]", "AI / LLM",
            ["ai [query] — Launch AI agent query or interactive deck."], RequiresAiOllama: true),
        new("cai", "Invoke AI Agent (Alias)", "Alias for ai command", "[AI Agent & Ollama]", "AI / LLM",
            ["cai [query] — Alias for ai command."], RequiresAiOllama: true),
        new("claude", "Claude Code (Auto Mode)", "Launch Claude Code CLI (resolves Cloud vs Ollama via AiProviderMode)", "[AI Agent & Ollama]", "AI / LLM",
            ["claude — Launch Claude Code CLI using runtime AiProviderMode setting."], RequiresAiOllama: true),
        new("claude", "Claude Code CLI", "Launch Claude Code CLI session", "[AI Agent & Ollama]", "Integrations",
            ["claude [prompt] — Launches Claude Code agent CLI with optional initial prompt."], RequiresAiOllama: true),
        new("ai-mode-check", "AI Mode Diagnostic", "Check resolved AI mode and reason for an alias", "[AI Agent & Ollama]", "Integrations",
            ["ai-mode-check <alias> — Prints resolved provider mode (cloud vs local) and resolution reason."], RequiresAiOllama: true),
        new("claude-ollama", "Claude Code (Force Ollama)", "Run Claude Code routed locally via Ollama daemon", "[AI Agent & Ollama]", "AI / LLM",
            ["claude-ollama — Run Claude Code routed locally via Ollama daemon."], RequiresAiOllama: true),
        new("codex", "Codex (Auto Mode)", "Launch Codex CLI (resolves Cloud vs Ollama via AiProviderMode)", "[AI Agent & Ollama]", "AI / LLM",
            ["codex — Launch Gemini Codex CLI using runtime AiProviderMode setting."], RequiresAiOllama: true),
        new("codex-cloud", "Codex (Force Cloud)", "Launch Gemini's Codex CLI (Cloud API direct)", "[AI Agent & Ollama]", "AI / LLM",
            ["codex-cloud — Launch Gemini's Codex CLI forcing cloud API direct access."], RequiresAiOllama: true),
        new("codex-ollama", "Codex (Force Ollama)", "Run Codex locally routed via Ollama daemon", "[AI Agent & Ollama]", "AI / LLM",
            ["codex-ollama — Run Codex CLI routed locally via Ollama daemon."], RequiresAiOllama: true),
        new("openclaw", "OpenClaw (Ollama)", "Launch OpenClaw via Ollama", "[AI Agent & Ollama]", "AI / LLM",
            ["openclaw — Launch OpenClaw model via local Ollama daemon."], RequiresAiOllama: true),
        new("hermes", "Hermes3 (Ollama)", "Launch Hermes3 via Ollama", "[AI Agent & Ollama]", "AI / LLM",
            ["hermes — Launch Hermes3 local reasoning model via Ollama."], RequiresAiOllama: true),
        new("hermesd", "Hermes3 debug mode", "Launch Hermes3 debug mode", "[AI Agent & Ollama]", "AI / LLM",
            ["hermesd — Launch Hermes3 in debug mode."], RequiresAiOllama: true),
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
        new("deck-status", "Antigravity Deck: Check Status", "Check if Antigravity Deck (Desk) local server is running", "[AGY Account Switch]", "Accounts",
            ["deck-status — Queries local port 3000 to verify Deck status. Also matches 'desk'."]),
        new("deck-setup", "Antigravity Deck: Setup/Initialize", "Setup local Antigravity Deck (Desk)", "[AGY Account Switch]", "Accounts",
            ["deck-setup — Initializes local Node.js environment for Deck. Also matches 'desk'."]),
        new("deck-start", "Antigravity Deck: Start Local", "Boot local Antigravity Deck (Desk)", "[AGY Account Switch]", "Accounts",
            ["deck-start — Launches Antigravity Deck dashboard at http://localhost:3000. Also matches 'desk'."]),
        new("deck-online", "Antigravity Deck: Go Online (Tunnel)", "Expose local Deck (Desk) via tunnel", "[AGY Account Switch]", "Accounts",
            ["deck-online — Exposes local Deck service via cloudflare/tailscale tunnel. Also matches 'desk'."]),
        new("desk-status", "Antigravity Desk: Check Status (Alias)", "Alias for deck-status", "[AGY Account Switch]", "Accounts",
            ["desk-status — Alias for deck-status."]),
        new("desk-setup", "Antigravity Desk: Setup/Initialize (Alias)", "Alias for deck-setup", "[AGY Account Switch]", "Accounts",
            ["desk-setup — Alias for deck-setup."]),
        new("desk-start", "Antigravity Desk: Start Local (Alias)", "Alias for deck-start", "[AGY Account Switch]", "Accounts",
            ["desk-start — Alias for deck-start."]),
        new("desk-online", "Antigravity Desk: Go Online (Alias)", "Alias for deck-online", "[AGY Account Switch]", "Accounts",
            ["desk-online — Alias for deck-online."]),
 
        // Antigravity Manager
        new("mgr-status", "Antigravity Manager: Check Status", "Check if Antigravity Manager backend is running on port 8045", "[AGY Account Switch]", "Accounts",
            ["mgr-status — Queries local port 8045 to verify Antigravity Manager status."]),
        new("mgr-setup", "Antigravity Manager: Setup/Initialize", "Setup local Antigravity Manager dependencies", "[AGY Account Switch]", "Accounts",
            ["mgr-setup — Runs npm install to initialize Antigravity Manager dependencies."]),
        new("mgr-start", "Antigravity Manager: Start Local", "Boot local Antigravity Manager Electron desktop app", "[AGY Account Switch]", "Accounts",
            ["mgr-start — Launches Antigravity Manager desktop application."]),
        new("mgr", "Antigravity Manager: Start Local (Alias)", "Alias for mgr-start", "[AGY Account Switch]", "Accounts",
            ["mgr — Alias for mgr-start."]),
        new("manager-status", "Antigravity Manager: Check Status (Alias)", "Alias for mgr-status", "[AGY Account Switch]", "Accounts",
            ["manager-status — Alias for mgr-status."]),
        new("manager-setup", "Antigravity Manager: Setup/Initialize (Alias)", "Alias for mgr-setup", "[AGY Account Switch]", "Accounts",
            ["manager-setup — Alias for mgr-setup."]),
        new("manager-start", "Antigravity Manager: Start Local (Alias)", "Alias for mgr-start", "[AGY Account Switch]", "Accounts",
            ["manager-start — Alias for mgr-start."]),
        new("agm", "Antigravity Manager: Start Local (Alias)", "Alias for mgr-start", "[AGY Account Switch]", "Accounts",
            ["agm — Alias for mgr-start."]),
        new("agm-status", "Antigravity Manager: Check Status (Alias)", "Alias for mgr-status", "[AGY Account Switch]", "Accounts",
            ["agm-status — Alias for mgr-status."]),
        new("agm-setup", "Antigravity Manager: Setup/Initialize (Alias)", "Alias for mgr-setup", "[AGY Account Switch]", "Accounts",
            ["agm-setup — Alias for mgr-setup."]),
        new("agm-start", "Antigravity Manager: Start Local (Alias)", "Alias for mgr-start", "[AGY Account Switch]", "Accounts",
            ["agm-start — Alias for mgr-start."]),

        new("agy-cli", "Launch Antigravity CLI (agy)", "Launch the google antigravity CLI tool terminal", "[AI Agent & Ollama]", "AI / LLM",
            ["agy-cli — Launches google antigravity CLI executable session (`agy`)."]),
        new("ai-history", "AI History Ledger", "Show ledger of past AI invocations", "[AI Agent & Ollama]", "AI / LLM",
            ["ai-history — Displays JSONL audit ledger of past AI agent invocations."]),

        // [AGY Account Switch]
        new("agyswitch", "Select Active Account", "Switch AGY account context", "[AGY Account Switch]", "Accounts",
            ["agyswitch — Switch active Google AGY / Gemini account credentials context."], RequiresAgy: true),
        new("agyquota", "View All Accounts", "Show quota usage summary for all accounts", "[AGY Account Switch]", "Accounts",
            ["agyquota — Displays 5-hour and weekly request limits across registered accounts."], RequiresAgy: true),
        new("account-tree", "Account Tree", "Show hierarchical active account details", "[AGY Account Switch]", "Accounts",
            ["account-tree — Renders active account hierarchy and token status."], RequiresAgy: true),
        new("quota-chart", "Quota Bar Chart", "Show bar chart of active account limits", "[AGY Account Switch]", "Accounts",
            ["quota-chart — Renders colorized ASCII bar chart of quota consumption."], RequiresAgy: true),
        new("cnav", "Registered Workspace Navigator", "Interactive selector for all registered workspaces", "[Workspace & Dev]", "Navigation",
            ["cnav — Interactive search and navigator for all registered project workspaces."]),
        new("dotnet-info", "[.NET] System & SDK Info", "Display dotnet environment and SDK version details", "[Workspace & Dev]", ".NET",
            ["dotnet-info — Runs `dotnet --info` to display installed SDKs, runtimes, and environment details."]),
        new("reset-agy", "Reset AGY Account Credentials", "Purge all .gemini credential data and reset active context", "[AGY Account Switch]", "Accounts",
            ["reset-agy — Wipes all .gemini_* directories, clears stored keyring tokens, and re-initializes clean default context."]),
        new("purge-accounts", "Purge Custom Accounts", "Purge non-default custom accounts and reset context to default", "[AGY Account Switch]", "Accounts",
            ["purge-accounts — Deletes all secondary custom account directories and resets active context to default."]),
        new("live-dashboard", "Live Dashboard", "Show real-time active account metrics table", "[AGY Account Switch]", "Accounts",
            ["live-dashboard — Live-updating multi-column table monitoring active accounts."], RequiresAgy: true),
        new("autoswitch", "Toggle Auto-Switch", "Toggle automatic project account switching", "[AGY Account Switch]", "Accounts",
            ["autoswitch — Enables/disables automatic account context switching."], RequiresAgy: true),
        new("no-auto-commit", "Toggle Multi-Agent Auto-Commit", "Toggle automatic git commits during multi-agent AGY tasks", "[AGY Account Switch]", "Accounts",
            ["no-auto-commit — Enables/disables automatic git commits during multi-agent AGY execution."]),
        new("autocommit", "Toggle Multi-Agent Auto-Commit (Alias)", "Alias for no-auto-commit toggle", "[AGY Account Switch]", "Accounts",
            ["autocommit — Alias for no-auto-commit toggle."]),
        new("grammar", "Grammar Drills by Level", "Practice Japanese (N5–N2) and English grammar points", "[Learn & Study]", "Study",
            ["grammar — Interactive grammar pattern drills by level."]),

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

        // [Learn & Study]
        new("learn", "Start Learning (auto)", "Start learning for a topic (auto-refresh)", "[Learn & Study]", "Learn",
            ["learn — Launches interactive study learning router for selected topic."]),
        new("learn-gen", "AI Content Generator", "Deeply generate flashcard decks, grammar, or quizzes via AGY / Claude CLI", "[Learn & Study]", "AI Learn",
            ["learn-gen — Deeply generates new flashcard decks, quizzes, and STAR answers via agy or claude CLI."]),
        new("obsidian", "Obsidian Vault Browser", "Search, browse by tag, and view daily notes in vault", "[Learn & Study]", "Vault",
            ["obsidian — Interactive Obsidian Vault note search, tag browser, daily notes, and graph renderer."]),
        new("refresh", "Rescan & Sync Vault Datasets", "Rescan Obsidian Vault and sync datasets to learn/", "[Learn & Study]", "Vault",
            ["refresh — Rescans Obsidian Vault notes and syncs flashcards, quizzes, and cheat sheets to learn/."]),
        new("vault-open", "Open Vault Folder", "Open Obsidian Vault directory in Windows Explorer", "[Learn & Study]", "Vault",
            ["vault-open — Opens Obsidian Vault directory in Windows File Explorer."]),
        new("flashcard", "Flashcard Deck Browser", "Open flashcard deck browser", "[Learn & Study]", "Learn",
            ["flashcard — Interactive flashcard deck viewer with SM-2 spaced-repetition scoring."]),
        new("vocab", "English Vocab Drill", "English vocabulary drill", "[Learn & Study]", "Learn",
            ["vocab — Practice English vocabulary definitions, synonyms, and context sentences."]),
        new("kana", "Kana Quiz", "Hiragana / katakana quiz", "[Learn & Study]", "Learn",
            ["kana — Interactive Japanese Hiragana and Katakana character recognition quiz."]),
        new("kanji", "Kanji Lookup", "Kanji lookup / stroke detail", "[Learn & Study]", "Learn",
            ["kanji — Look up Japanese Kanji radicals, stroke counts, and readings."]),
        new("jlpt", "JLPT Vocab Drill", "JLPT vocabulary drill", "[Learn & Study]", "Learn",
            ["jlpt — Vocabulary practice drills categorized by JLPT level (N5 to N1)."]),
        new("algo", "Algorithm Visualizer", "Algorithm visualizer (sort / search)", "[Learn & Study]", "Learn",
            ["algo — Interactive terminal visualization for sorting and searching algorithms."]),
        new("complexity", "Big-O Complexity Sheet", "Big-O complexity cheat-sheet", "[Learn & Study]", "Learn",
            ["complexity — Displays Big-O time and space complexity cheat-sheet table."]),
        new("problems", "DSA Problem Tracker", "DSA problem tracker", "[Learn & Study]", "Learn",
            ["problems — Track status, difficulty, and notes for LeetCode / DSA practice problems."]),
        new("snippets", "Code Snippet Library", "Code snippet library browser", "[Learn & Study]", "Learn",
            ["snippets — Browse and search reusable code snippets across multiple languages."]),
        new("sheets", "Cheat Sheet Browser", "Cheat-sheet browser (.txt files)", "[Learn & Study]", "Learn",
            ["sheets — Browse text cheat-sheets stored in your local reference library."]),
        new("quiz", "C# Quiz", "C# multiple-choice quiz", "[Learn & Study]", "Learn",
            ["quiz — Multiple-choice practice quiz testing C# and .NET concepts."]),
        new("interview", "Interview Question Bank", "Interview question bank", "[Learn & Study]", "Learn",
            ["interview — Browse technical interview questions for system design and coding."]),
        new("star", "STAR Answer Builder", "STAR answer builder", "[Learn & Study]", "Learn",
            ["star — Interactive wizard to structure behavioral responses using Situation, Task, Action, Result."]),
        new("mock", "Mock Interview Timer", "Mock interview timer", "[Learn & Study]", "Learn",
            ["mock — Practice timed interview responses with an interactive stopwatch."]),
        new("word-of-day", "Word of the Day", "Show today's word of the day", "[Learn & Study]", "Learn",
            ["word-of-day — Displays vocabulary word of the day with definition and usage example."]),

        // [Learn & Study] - Study Tracking
        new("session", "Start Pomodoro Session", "Start a Pomodoro study session", "[Learn & Study]", "Tracking",
            ["session — Launches 25-minute Pomodoro focus session timer."]),
        new("stats", "Study Statistics", "Study statistics and weekly chart", "[Learn & Study]", "Tracking",
            ["stats — Displays weekly study volume breakdown and retention charts."]),
        new("goals", "Daily Goals", "Daily learning goals", "[Learn & Study]", "Tracking",
            ["goals — View and manage daily learning targets and completed tasks."]),
        new("streak", "Study Streak", "Study streak display", "[Learn & Study]", "Tracking",
            ["streak — Displays current consecutive daily study streak counter."]),
        new("due", "Due Reviews", "Show due spaced-repetition reviews", "[Learn & Study]", "Tracking",
            ["due — Shows total count of flashcards due for SM-2 spaced repetition review today."]),
        new("progress", "Progress Dashboard", "Progress dashboard (bar chart + tree)", "[Learn & Study]", "Tracking",
            ["progress — Renders visual progress bar charts across all learning domains."]),
        new("weak", "Weak Items Queue", "Weak items queue (pre-session review)", "[Learn & Study]", "Tracking",
            ["weak — Review cards and concepts with low retention scores before starting a session."]),

        // [Obsidian & Resources]
        new("obs-graph", "Obsidian Graph View", "Obsidian wikilink graph", "[Obsidian & Resources]", "Obsidian",
            ["obs-graph — Visualizes inter-note wikilink relationships in your Obsidian vault."]),
        new("add-resource", "Add Resource", "Add a file/URL to resource registry", "[Obsidian & Resources]", "Resources",
            ["add-resource — Register a external file path or URL with custom tags."]),

        // [Appearance & Layout]
        new("mobile-setup", "Toggle Mobile Setup", "Toggle both prompt mobile mode and compact TUI layout mode", "[Appearance & Layout]", "Theme & Settings",
            ["mobile-setup — Toggles compact prompt and high-density TUI layout."]),
        new("mobile", "Toggle Mobile Setup (Alias)", "Alias for mobile-setup", "[Appearance & Layout]", "Theme & Settings",
            ["mobile — Alias for mobile-setup."]),
        new("theme", "Select Shell Theme", "Select Shell Theme", "[Appearance & Layout]", "Theme & Settings",
            ["theme — Interactive theme picker for Oh-My-Posh prompt themes."]),
        new("ui-mode", "Toggle UI Layout Mode", "Toggle between three-pane and flat-tree layouts", "[Appearance & Layout]", "Theme & Settings",
            ["ui-mode — Toggles between `three-pane` and `flat-tree` layout modes."]),
        new("density", "Toggle Console Density", "Toggle between comfortable and compact display densities", "[Appearance & Layout]", "Theme & Settings",
            ["density — Toggles line spacing density between `comfortable` and `compact`."]),
        new("favorite", "Toggle Favorite Command", "Pin or unpin a command alias to your Favorites category", "[Appearance & Layout]", "Theme & Settings",
            ["favorite <alias> — Toggles pinning of the specified command alias to your Favorites category."]),
        new("favorites", "List Favorite Commands", "List all pinned favorite command aliases", "[Appearance & Layout]", "Theme & Settings",
            ["favorites — Displays all command aliases currently pinned to Favorites."]),

        // [Help & Docs]
        new("cc", "Command Palette", "Open this Command Palette", "[Help & Docs]", "Help",
            ["cc — Launches interactive Command Palette."]),
        new("help", "Help Browser", "Open interactive help browser", "[Help & Docs]", "Help",
            ["help — Interactive browser listing all profile aliases, functions, and documentation."])
    ];

    static CommandRegistry()
    {
        var navCmds = new HashSet<string> { "prune-workspaces", "discover-workspaces", "proj", "cnav", "go", "open-term", "f" };
        var devToolsCmds = new HashSet<string> { "ide", "ide-diff", "ide-search", "scaffold" };

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
        var claudeCmds = new HashSet<string> { "claude", "claude-cloud", "claude-ollama" };
        var codexCmds = new HashSet<string> { "codex", "codex-cloud", "codex-ollama" };
        var ollamaCmds = new HashSet<string> { "ollama-status", "ollama-models", "ollama-pull", "ollama-start", "ollama-logs", "ollama-benchmark" };
        var deckCmds = new HashSet<string> { "deck-status", "deck-setup", "deck-start", "deck-online", "desk-status", "desk-setup", "desk-start", "desk-online" };
        var mgrCmds = new HashSet<string> { "mgr-status", "mgr-setup", "mgr-start", "mgr", "manager-status", "manager-setup", "manager-start", "agm", "agm-status", "agm-setup", "agm-start" };
        var sshCmds = new HashSet<string> { "ssh-info", "tailscale-status", "ssh-qr" };
        var accountMgrCmds = new HashSet<string> { "agyswitch", "agyquota", "vault", "reset-agy", "purge-accounts" };
        var quotaCmds = new HashSet<string> { "account-tree", "quota-chart", "live-dashboard" };
        var secretCmds = new HashSet<string> { "secret-set", "secret-get", "secret-list", "secret-remove" };
        var toggleCmds = new HashSet<string> { "autoswitch", "no-auto-commit", "autocommit" };
        var trackCmds = new HashSet<string> { "session", "stats", "goals", "streak", "due", "progress", "weak" };

        var jpCmds = new HashSet<string> { "kana", "kanji", "jlpt", "grammar" };
        var enCmds = new HashSet<string> { "word-of-day", "vocab", "flashcard", "grammar" };
        var csCmds = new HashSet<string> { "quiz", "snippets", "sheets" };
        var dsaCmds = new HashSet<string> { "algo", "complexity", "problems" };
        var careerCmds = new HashSet<string> { "interview", "star", "mock" };
        var obsidianCmds = new HashSet<string> { "obsidian", "obs-vault", "refresh", "sync", "vault-open", "daily-note", "orphan-notes", "obs-graph", "add-resource" };
        var appearanceCmds = new HashSet<string> { "theme", "ui-mode", "density", "mobile-setup", "favorite", "favorites" };

        var hiddenCmds = new HashSet<string> {
            "cai",
            "p", "prj", "glg", "gpu", "gus", "gundo", "grt", "grtu", "cor",
            "gm", "gmi", "gcf", "gconflictu", "gcfu", "gst", "gstashu", "gstu", "grb", "grebaseu", "grbu",
            "db", "dbu", "dt", "dtu", "dw", "da", "du", "dres", "dclean", "dki", "dkcpu", "dkcpd", "mobile",
            "desk-status", "desk-setup", "desk-start", "desk-online",
            "mgr", "manager-status", "manager-setup", "manager-start", "agm", "agm-status", "agm-setup", "agm-start"
        };

        var orderedAliases = new[]
        {
            // Category 1: [Favorites]
            "proj", "agyswitch", "open-term", "ide", "ask-ai",

            // Category 2: [Workspace & Dev]
            "proj", "cnav", "go", "open-term", "f", "ide", "ide-diff", "ide-search", "scaffold",
            "gs", "gsu", "ga", "gb", "gbr", "co", "cob", "gbd", "gcommit", "gcmt", "glo", "glog", "glou", "gpull", "gpush", "guf", "gf", "gd", "gr", "grh", "git-undo", "gclone", "gcloneu", "nexus", "repo-graph", "nexus-stats",
            "dbld", "dr", "dtst", "df", "dcl", "drestore", "dpublish", "dpack", "dpubpkg", "dwatch", "rebuild-tui", "clean-build", "add-migration", "update-db", "dotnet-info",
            "docker-health", "dkcl", "dkrmac", "dkstac", "dimg", "dlogs", "dcup", "dcdown",
            "aws-whoami", "aws-local", "aws-s3", "aws-sqs", "aws-ssm", "aws-sns", "aws-dynamodb", "aws-lambda",

            // Category 3: [AI Agent & Ollama]
            "ask-ai", "openclaw", "hermes", "claude", "codex", "agy-cli", "ai-history",
            "ollama-status", "ollama-models", "ollama-pull", "ollama-start", "ollama-logs", "ollama-benchmark",

            // Category 4: [AGY Account Switch]
            "vault", "agyswitch", "agyquota",
            "secret-set", "secret-get", "secret-list", "secret-remove",
            "account-tree", "quota-chart", "live-dashboard",
            "autoswitch", "no-auto-commit", "autocommit",
            "deck-status", "deck-setup", "deck-start", "deck-online", "mgr-status", "mgr-setup", "mgr-start",

            // Category 5: [Learn & Study]
            "learn", "learn-gen", "guide", "slash-manual", "skills",
            "session", "stats", "goals", "streak", "due", "progress", "weak",
            "obsidian", "refresh", "vault-open",
            "kana", "kanji", "jlpt", "grammar",
            "word-of-day", "vocab", "flashcard",
            "quiz", "snippets", "sheets",
            "algo", "complexity", "problems",
            "interview", "star", "mock",

            // Category 6: [Obsidian & Resources]
            "orphan-notes", "daily-note", "obs-graph", "add-resource",

            // Category 7: [Appearance & Layout]
            "theme", "ui-mode", "density", "favorite", "mobile-setup",

            // Category 8: [System & Network]
            "config", "disk", "public-ip", "kill-port", "ssh-info",
            "tailscale-status", "ssh-qr",
            "system-reload", "reload-cc", "reload-term", "reload-all",

            // Category 9: [Help & Docs]
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

            if (navCmds.Contains(alias)) { cmd.GroupPath = "/workspace-nav"; cmd.GroupName = "Workspace Navigation"; }
            else if (devToolsCmds.Contains(alias)) { cmd.GroupPath = "/dev-scaffold-tools"; cmd.GroupName = "Developer Tools & Scaffolding"; }
            else if (gitCmds.Contains(alias) || repoCmds.Contains(alias)) { cmd.GroupPath = "/git-tools"; cmd.GroupName = "Git & Repo Tools"; }
            else if (dotnetCmds.Contains(alias)) { cmd.GroupPath = "/dotnet-tools"; cmd.GroupName = ".NET Project Tools"; }
            else if (dockerCmds.Contains(alias)) { cmd.GroupPath = "/docker-tools"; cmd.GroupName = "Docker Tools"; }
            else if (awsCmds.Contains(alias)) { cmd.GroupPath = "/aws-tools"; cmd.GroupName = "AWS Tools"; }
            else if (claudeCmds.Contains(alias) && cmd.Category != "[AI Agent & Ollama]") { cmd.GroupPath = "/claude-agents"; cmd.GroupName = "Claude Agents"; }
            else if (codexCmds.Contains(alias) && cmd.Category != "[AI Agent & Ollama]") { cmd.GroupPath = "/codex-agents"; cmd.GroupName = "Codex Agents"; }
            else if (ollamaCmds.Contains(alias) && cmd.Category != "[AI Agent & Ollama]") { cmd.GroupPath = "/ollama-tools"; cmd.GroupName = "Ollama Tools"; }
            else if (deckCmds.Contains(alias)) { cmd.GroupPath = "/antigravity-deck"; cmd.GroupName = "Antigravity Deck (Desk)"; }
            else if (mgrCmds.Contains(alias)) { cmd.GroupPath = "/antigravity-manager"; cmd.GroupName = "Antigravity Manager"; }
            else if (accountMgrCmds.Contains(alias)) { cmd.GroupPath = "/account-mgr"; cmd.GroupName = "Account & Credentials Manager"; }
            else if (quotaCmds.Contains(alias)) { cmd.GroupPath = "/quota-views"; cmd.GroupName = "Quota & Analytics Views"; }
            else if (secretCmds.Contains(alias)) { cmd.GroupPath = "/secret-vault"; cmd.GroupName = "Secret Vault"; }
            else if (toggleCmds.Contains(alias)) { cmd.GroupPath = "/account-toggles"; cmd.GroupName = "Account Toggles"; }
            else if (jpCmds.Contains(alias))
            {
                cmd.GroupPath = (alias == "grammar") ? "/jp-suite,/english-vocab" : "/jp-suite";
                cmd.GroupName = "Japanese Suite";
            }
            else if (enCmds.Contains(alias)) { cmd.GroupPath = "/english-vocab"; cmd.GroupName = "English & Vocab"; }
            else if (csCmds.Contains(alias)) { cmd.GroupPath = "/csharp-master"; cmd.GroupName = "C# & Dev Masterclass"; }
            else if (dsaCmds.Contains(alias)) { cmd.GroupPath = "/dsa-architect"; cmd.GroupName = "DSA & System Design"; }
            else if (careerCmds.Contains(alias)) { cmd.GroupPath = "/career-interview"; cmd.GroupName = "Career & Interview Prep"; }
            else if (obsidianCmds.Contains(alias) && cmd.Category != "[Obsidian & Resources]") { cmd.GroupPath = "/obsidian-vault"; cmd.GroupName = "Obsidian Vault & Resources"; }
            else if (trackCmds.Contains(alias)) { cmd.GroupPath = "/track"; cmd.GroupName = "Track & Progress"; }
            else if (sshCmds.Contains(alias)) { cmd.GroupPath = "/ssh-tools"; cmd.GroupName = "SSH & Network Tools"; }
            else if (appearanceCmds.Contains(alias)) { cmd.GroupPath = "/appearance-favs"; cmd.GroupName = "Appearance & Favorites"; }
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

