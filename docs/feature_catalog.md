# 🛸 Full Feature Catalog — PowerShell Profile & AgyTui Control Center

Exhaustive hierarchical inventory of all features, tools, commands, TUI screens, services, and workflows across `Microsoft.PowerShell_profile.ps1` and `AgyTuiApp`.

---

### 📁 1. Workspace & Navigation
- **Workspace Navigator (`proj` / `p` / `prj` / `go`)**: Interactive fuzzy & substring workspace search with automatic target directory switching.
- **Auto-Discovery Engine**: Scans configured base directories (`ProjectsBaseDir`, `~/project`, `~/Documents`, `~/Desktop`) for `.git`, `.csproj`, `.sln`, and `package.json` roots.
- **Shared Workspace Actions**:
  - `📂 Change Directory on exit (cd)`
  - `🚀 Launch New Terminal Session (wt / pwsh)`
  - `💻 Open in Terminal IDE (/ide)`
  - `📁 Open in Windows File Explorer (f)`
  - `🔀 View Git Status & Diff (/ide-diff)`
- **Open Explorer (`f`)**: Instant launch of Windows File Explorer at the current directory.
- **New Terminal Session (`open-term`)**: Spawns a new Windows Terminal (`wt.exe`) or PowerShell session in the workspace directory.
- **Directory Navigation Helpers**:
  - `Set-LocationParent` (`..`): Step up one directory level.
  - `Set-LocationGrandParent` (`...`): Step up two directory levels.

---

### 💻 2. Terminal IDE Suite
- **Interactive Terminal IDE (`ide`)**:
  - **File Explorer**: In-terminal directory tree browser with file/folder icons.
  - **Code Viewer**: Colorized text and source file viewer with line numbering.
  - **Symbol Search**: In-file symbol and function definition navigator.
- **Workspace Diff Viewer (`ide-diff` / `gd`)**: Colorized side-by-side / unified git diff viewer for workspace modifications.
- **Workspace Code Search (`ide-search`)**: Workspace-wide regex and string pattern search across `.cs`, `.ps1`, `.ts`, `.js`, and `.py` source files.
- **SQLite Database Browser (`db-tui`)**: Interactive schema inspector and SQL query browser powered by `sqlite3`.

---

### 🌿 3. Git & Repository Management
- **Git Status Summary (`gs`)**: Colorized short-format git status summary (`git status --short`).
- **Stage All Modifications (`ga`)**: Stage all modified, deleted, and untracked files (`git add .`).
- **Git Branch Manager (`gbr` / `gb`)**: Interactive branch switcher sorted by recent commit date with worktree/submodule support.
- **Conventional Commit Wizard (`gcmt`)**: Structured commit builder supporting Type (`feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`, `ci`), Scope, Description, and Breaking Changes.
- **Git Commit Log Graph (`glog` / `glo` / `glg`)**: Interactive 50-commit branch graph viewer (`git log --oneline --graph`).
- **Git Sync Controls**:
  - **Fetch Remote (`gf`)**: Fetch latest remote refs without merging.
  - **Pull Remote (`gpull` / `gpu`)**: Incorporate upstream commits (`git pull`).
  - **Push Remote (`gpush` / `gus`)**: Push local commits to remote tracking branch (`git push`).
- **Git Undo (`git-undo` / `gundo`)**: Soft-reset last local commit (`git reset --soft HEAD~1`), preserving staged changes in index.
- **Git Nexus Dashboard (`nexus`)**: Multi-repository dependency graph and commit velocity dashboard.
- **Repo Dependency Graph (`repo-graph`)**: Cross-project dependency tree and inter-repo relationship visualization.
- **Nexus Commit Stats (`nexus-stats`)**: Commit velocity and author contribution metrics across workspace repos.

---

### 🔨 4. .NET Development Tools
- **Build Solution/Project (`dbld` / `db`)**: Run `dotnet build` in active workspace.
- **Run Application (`dr`)**: Run `dotnet run` in active workspace.
- **Execute Test Suite (`dtst` / `dt`)**: Run `dotnet test` test suite.
- **Format Code (`df`)**: Run `dotnet format` code style enforcer.
- **Clean Project (`dcl`)**: Run `dotnet clean` build output cleaner.
- **Restore Dependencies (`drestore` / `dres`)**: Run `dotnet restore` NuGet package resolver.
- **Publish Release Binary (`dpublish`)**: Run `dotnet publish -c Release`.
- **Live-Reload Watch Loop (`dwatch` / `dw`)**: Continuous dev watch loop (`dotnet watch run`).
- **Deep Clean Build Artifacts (`clean-build` / `dclean`)**: Recursively destroy `bin/` and `obj/` directories across the workspace.
- **EF Core Migration Tooling**:
  - **Add Migration (`add-migration` / `da`)**: Run `dotnet ef migrations add <name>`.
  - **Update Database (`update-db` / `du`)**: Run `dotnet ef database update`.
- **NuGet Package Release Tools**:
  - **Pack NuGet Package (`dpack`)**: Compile Release package and output `.nupkg` to `./nupkg`.
  - **Publish Package (`dpubpkg`)**: Push `.nupkg` package to NuGet registry using secure environment variables.
- **Rebuild Control Center TUI (`rebuild`)**: Recompile `AgyTuiApp.csproj` in-place with zero warnings/errors enforcement.

---

### 🐳 5. Docker Container Suite
- **Docker Health Dashboard (`docker-health`)**: Real-time CPU, memory, network I/O, and container health metrics.
- **Docker Cleanup Dashboard (`dkcl`)**: Interactive TUI for stopping/removing containers, pruning dangling image layers, and deleting unused volumes/networks.
- **Stop All Containers (`dkstac`)**: Gracefully stop all running containers.
- **Remove All Containers (`dkrmac`)**: Forcefully stop and delete all containers.
- **Docker Image Manager (`dimg`)**: Inspect local Docker images, repository tags, and layer sizes.
- **Container Log Viewer (`dlogs`)**: Interactive log tailing viewer for active containers.
- **Docker Compose Controls**:
  - **Compose Up (`dcup` / `dkcpu`)**: Launch background container services (`docker compose up -d`).
  - **Compose Down (`dcdown` / `dkcpd`)**: Tear down container stack (`docker compose down`).

---

### ☁️ 6. AWS & Cloud Diagnostics
- **AWS STS Identity (`aws-whoami`)**: Inspect caller identity, ARN, active profile, and region (`aws sts get-caller-identity`).
- **LocalStack Sandbox Diagnostics (`aws-local`)**: LocalStack endpoint health checker (`http://localhost:4566`).
- **S3 Bucket Inspector (`aws-s3`)**: List S3 storage buckets (`aws s3 ls`) with LocalStack fallback.
- **SQS Queue Inspector (`aws-sqs`)**: List SQS message queues (`aws sqs list-queues`).
- **SSM Parameter Store Inspector (`aws-ssm`)**: Inspect SSM Parameter Store key-value entries (`aws ssm describe-parameters`).
- **SNS Topic Inspector (`aws-sns`)**: List notification topics (`aws sns list-topics`).
- **DynamoDB Table Inspector (`aws-dynamodb`)**: List DynamoDB tables (`aws dynamodb list-tables`).
- **Lambda Function Inspector (`aws-lambda`)**: List serverless functions (`aws lambda list-functions`).

---

### 🤖 7. AI Agent & Ollama Integration
- **Claude Code CLI Integrations**:
  - **Auto Provider Router (`claude`)**: Launch Claude Code resolving cloud vs. local Ollama backend via `AiProviderMode`.
  - **Force Cloud (`claude-cloud`)**: Launch Claude Code forcing Cloud API endpoints.
  - **Force Ollama (`claude-ollama`)**: Route Claude Code locally through local Ollama daemon.
- **Gemini / Codex CLI Integrations**:
  - **Auto Provider Router (`codex`)**: Launch Codex CLI using runtime `AiProviderMode`.
  - **Force Cloud (`codex-cloud`)**: Launch Codex CLI forcing Cloud API direct access.
  - **Force Ollama (`codex-ollama`)**: Route Codex CLI through local Ollama daemon.
- **Specialized Local Models**:
  - **OpenClaw (`openclaw`)**: Launch OpenClaw local LLM via Ollama.
  - **Hermes3 (`hermes`)**: Launch Hermes3 local reasoning model via Ollama.
  - **Hermes3 Debug (`hermesd`)**: Launch Hermes3 in verbose debug mode.
- **Ollama Management Suite**:
  - **Daemon Status (`ollama-status`)**: Non-blocking check for local Ollama server (`http://localhost:11434`).
  - **Model Manager (`ollama-models`)**: Inspect and manage pulled model weights.
  - **Pull Model (`ollama-pull`)**: Download new model weights from Ollama library.
  - **Start Daemon (`ollama-start`)**: Boot background daemon (`ollama serve`).
  - **Server Logs (`ollama-logs`)**: Tail last 50 lines of Ollama server logs.
  - **Benchmark Tool (`ollama-benchmark`)**: Measure model evaluation speed (tokens/sec).
- **Antigravity Deck Dashboard**:
  - **Deck Status (`deck-status`)**: Check if local Deck dashboard server is listening on port 3000.
  - **Deck Setup (`deck-setup`)**: Initialize Node.js dependencies for local Deck.
  - **Deck Start (`deck-start`)**: Launch local Deck web interface at `http://localhost:3000`.
  - **Deck Online (`deck-online`)**: Expose local Deck interface via Cloudflare / Tailscale tunnel.
- **Antigravity CLI Launcher (`agy-cli`)**: Launch executable session for Google Antigravity CLI (`agy`).
- **AI Audit Ledger (`ai-history`)**: View historical JSONL audit ledger of past AI agent invocations.

---

### 🔐 8. AGY Account & Credential Management
- **Account Switcher (`agyswitch`)**: Fast interactive switcher for Google AGY / Gemini account context credentials.
- **Account Quota Tracker (`agyquota`)**: Summary view of 5-hour and weekly request limits across all registered accounts.
- **Account Hierarchy Inspector (`account-tree`)**: Renders active account hierarchy and DPAPI token status.
- **Quota Bar Chart (`quota-chart`)**: Colorized ASCII bar chart of quota consumption metrics.
- **Live Account Dashboard (`live-dashboard`)**: Real-time updating table monitoring quota usage across registered accounts.
- **Auto-Switch Toggle (`autoswitch`)**: Enable/disable automatic directory-based account context switching.
- **Multi-Agent Auto-Commit Toggle (`no-auto-commit` / `autocommit`)**: Toggle automatic git commit execution during multi-agent task execution.
- **Encrypted Secret Vault (`AgySecretVault`)**: DPAPI-encrypted credential storage for API keys and tokens.

---

### 🧠 9. Learn & Study Suite
- **Interactive Study Router (`learn`)**: Main interactive learning router with topic selection.
- **AI Content Generator (`learn-gen`)**: Deeply generate new flashcard decks, grammar points, and quizzes using `agy` / `claude` CLI with JSON schema validation.
- **Obsidian Vault Integration**:
  - **Vault Browser (`obsidian`)**: Interactive search, tag navigation, daily note viewer, and wikilink graph renderer.
  - **Rescan & Sync (`refresh`)**: Rescan Obsidian Vault notes and sync datasets to `learn/`.
  - **Open Vault Directory (`vault-open`)**: Launch Windows File Explorer at Obsidian Vault location.
- **Interactive Study Modules**:
  - **Flashcard Deck Browser (`flashcard`)**: Flashcard deck viewer with SM-2 spaced-repetition scoring.
  - **Grammar Drills (`grammar`)**: Practice Japanese (N5–N2) and English grammar points.
  - **English Vocab Drill (`vocab`)**: Vocabulary definitions, synonyms, and context sentences.
  - **Kana Quiz (`kana`)**: Interactive Japanese Hiragana and Katakana recognition quiz with empty-pool guards.
  - **Kanji Lookup (`kanji`)**: Radical search, stroke counts, and reading lookups.
  - **JLPT Vocab Practice (`jlpt`)**: Vocabulary drills categorized by JLPT level (N5 to N1).
  - **Algorithm Visualizer (`algo`)**: Terminal visualization of sorting and search algorithms.
  - **Big-O Complexity Sheet (`complexity`)**: Quick reference time/space complexity matrix.
  - **DSA Problem Tracker (`problems`)**: Track LeetCode / DSA problem statuses, difficulty, and notes.
  - **Code Snippet Library (`snippets`)**: Multilingual reusable code snippet browser.
  - **Cheat Sheet Browser (`sheets`)**: Plaintext reference cheat-sheet viewer.
  - **C# Quiz (`quiz`)**: Multiple-choice C# and .NET concepts test.
  - **Interview Question Bank (`interview`)**: System design and coding interview question bank.
  - **STAR Answer Builder (`star`)**: Wizard to construct Situation, Task, Action, Result behavioral interview responses.
  - **Mock Interview Timer (`mock`)**: Practice timed interview responses with stopwatch.
  - **Word of the Day (`word-of-day`)**: Daily vocabulary word with definitions and example sentences.

---

### 📊 10. Tracking & Progress Analytics
- **Pomodoro Focus Timer (`session`)**: 25-minute Pomodoro focus session timer.
- **Study Statistics (`stats`)**: Weekly study volume breakdowns and retention charts with fixed 7-day window calculations.
- **Daily Goals (`goals`)**: Daily learning targets and task tracker.
- **Study Streak Counter (`streak`)**: Consecutive daily study streak tracker with future-date guards.
- **Due Reviews Summary (`due`)**: Count of flashcards due for SM-2 spaced repetition review.
- **Progress Dashboard (`progress`)**: Progress bar charts across all learning domains.
- **Weak Items Queue (`weak`)**: Pre-session review queue for low-retention cards.

---

### ⚙️ 11. System & Networking Diagnostics
- **Disk Usage & Health (`disk` / `usage`)**: Drive partitions, free space ratios, and health status summary.
- **Public IP Resolution (`public-ip` / `myip`)**: External IPv4 address resolver via fallback REST API chain.
- **Kill Port Listener (`kill-port`)**: Terminate process listening on specified TCP port.
- **SSH Connection Info (`ssh-info`)**: Summary of local IPs, Tailscale address, and active SSH connections.
- **Tailscale Mesh Peers (`tailscale-status`)**: Parse `tailscale status --json` to list connected mesh network peers.
- **SSH Terminal QR Code (`ssh-qr`)**: Generate terminal QR code containing SSH connection string for mobile devices.
- **Mobile SSH Key Receiver (`StartMobileSshKeyReceiver` / `ssh-addkey-mobile`)**: Token-authenticated receiver for authorizing mobile SSH public keys.

---

### 🎨 12. Appearance, Layout & Control Center Modes
- **Layout Modes (`ui-mode`)**: Toggle between `three-pane` and `flat-tree` TUI renderers.
- **Display Density (`density`)**: Toggle line spacing density between `comfortable` and `compact`.
- **Theme Selector (`theme`)**: Interactive Oh-My-Posh prompt theme selector.
- **Mobile Setup (`mobile-setup` / `mobile`)**: Unified toggle for mobile prompt theme, compact TUI density, and mobile context layout (`ConfigService.IsMobileContext()`).
- **Interactive Pager (`SpectrePager`)**: Full-screen text pager with live incremental search (`/`), scroll indicators, and page navigation (`d`/`u`, `PgDn`/`PgUp`, `Home`/`End`).
- **Shared Scrollable List View (`ScrollableListView`)**: Viewport calculation and scroll indicator component (`▲ N item(s) above`, `▼ N item(s) below`).
- **Command Palette (`cc`)**: Interactive Command Palette.
- **Interactive Help Browser (`help`)**: Searchable help browser listing all commands, aliases, and documentation.
- **Profile Hotkeys Guide (`hotkeys`)**: Keyboard shortcuts guide grouped by domain.
