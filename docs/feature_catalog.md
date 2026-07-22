# 🛸 Full Feature Catalog — PowerShell Profile & AgyTui Control Center

Exhaustive, deeply nested specification of all features, sub-components, UI views, keyboard controls, hotkeys, combo chords, status widgets, and parameters across `Microsoft.PowerShell_profile.ps1` and `AgyTuiApp`.

---

### 📁 1. Workspace & Project Navigation Suite
- **Workspace Navigator (`proj` / `p` / `prj` / `go`)**:
  - **Functionality**: Fuzzy and substring matching navigator that scans, filters, and jumps between registered development workspaces.
  - **Sub-Features**:
    - **Sub-Picker Live Search**: Incremental type-ahead search filtering workspaces by name or target directory path.
    - **Single-Match Jump**: Auto-executes target jump if exactly one workspace matches the query string.
    - **Interactive Target Selector**: Multi-choice selection menu rendered when multiple matching workspaces exist.
  - **Shared Workspace Actions**:
    - `📂 Change Directory on exit (cd)`: Writes target directory to `$AgySourceHome/selected_project.txt` for automatic shell `cd` on exit.
    - `🚀 Launch New Terminal Session (wt / pwsh)`: Spawns an isolated Windows Terminal (`wt.exe`) or PowerShell window pointing to the project directory.
    - `💻 Open in Terminal IDE (/ide)`: Immediately launches the built-in Terminal IDE in the target workspace.
    - `📁 Open in Windows File Explorer (f)`: Launches native Windows File Explorer (`explorer.exe`) at the target workspace.
    - `🔀 View Git Status & Diff (/ide-diff)`: Displays full-screen side-by-side or unified git diff viewer for the target workspace.
  - **Auto-Discovery Engine**:
    - Scans configured base paths: `Config.Current.ProjectsBaseDir`, `~/project`, `~/Documents`, `~/Desktop`.
    - Detects project roots containing `.git` (directories, worktree files, and submodules), `*.csproj`, `*.sln`, or `package.json`.
    - Skips hidden directories (`.*`) and `node_modules`.
  - **Hotkeys & Controls**:
    - `↑` / `↓` or `j` / `k`: Move row cursor.
    - `PageUp` / `PageDown` or `d` / `u`: Scroll viewport by visible row page count (`maxRows`).
    - `Home` / `End`: Jump to top or bottom of list.
    - `/`: Activate live incremental search mode.
    - `Enter` / `→`: Confirm selection and open workspace action menu.
    - `Esc` / `q` / `←`: Clear search buffer or exit picker.

- **Open File Explorer (`f`)**:
  - **Functionality**: Instantly opens Windows File Explorer targeting the active working directory or specified path parameter.

- **New Terminal Session (`open-term`)**:
  - **Functionality**: Spawns a new Windows Terminal (`wt.exe -d <path>`) or fallback PowerShell process (`pwsh.exe`) in the active workspace directory.

- **Shell Directory Navigation Shortcuts**:
  - `..` (`Set-LocationParent`): Navigate up one directory level (`cd ..`).
  - `...` (`Set-LocationGrandParent`): Navigate up two directory levels (`cd ../..`).

---

### 💻 2. Terminal IDE Suite
- **Interactive Terminal IDE (`ide`)**:
  - **Sub-Components & Sidebars**:
    - **File Explorer Sidebar**: Left-hand directory tree navigator with file extension icons (Nerd Font & UTF-8 fallback support).
    - **Code Viewer Panel**: Main reading pane with ANSI syntax highlighting, line numbering, and page bounds.
    - **Symbol Navigator**: Scans `.cs`, `.ps1`, `.ts`, `.js`, and `.py` files for class, interface, method, and function declarations.
  - **Hotkeys & Controls**:
    - `↑` / `↓` or `j` / `k`: Navigate directory tree / scroll source code.
    - `Enter`: Drill down into directory or open source file in viewer panel.
    - `/`: Search for symbols or function definitions across current file.
    - `Esc` / `q`: Return to parent directory or exit IDE.

- **Workspace Diff Viewer (`ide-diff` / `gd`)**:
  - **Functionality**: Interactive colorized diff viewer showing staged (`git diff --staged`) and unstaged changes across all workspace files.
  - **Controls**: `↑`/`↓` line scrolling, `PageUp`/`PageDown` page jumps, `q`/`Esc` exit.

- **Workspace Code Search (`ide-search`)**:
  - **Functionality**: Workspace-wide code and symbol search tool utilizing ripgrep / regex matching across source code files.

- **SQLite Database Browser (`db-tui`)**:
  - **Functionality**: Interactive schema inspector and data browser for `.db` / `.sqlite` files.
  - **Sub-Features**:
    - Schema table list and row count statistics.
    - Interactive SQL query execution prompt with write-guard protection against `.shell` / `.load` injection.
    - Backs up database before executing mutating SQL statements (`INSERT`, `UPDATE`, `DELETE`, `DROP`).

---

### 🌿 3. Git & Repository Management
- **Git Status Summary (`gs`)**:
  - **Functionality**: Colorized short-format git status summary (`git status --short`) displaying untracked (`??`), modified (`M`), staged (`A`), and branch tracking details.

- **Stage All Modifications (`ga`)**:
  - **Functionality**: Executes `git add .` to stage all modified, deleted, and untracked files in the current repository.

- **Git Branch Manager (`gbr` / `gb`)**:
  - **Functionality**: Interactive branch selector sorted by recent commit date.
  - **Sub-Features**:
    - Displays current branch indicator `* (active)`.
    - Supports git worktrees and git submodules by parsing `gitdir:` file references to locate `HEAD`.
    - Instant checkout upon pressing `Enter`.

- **Conventional Commit Wizard (`gcmt`)**:
  - **Functionality**: Guided 4-step prompt sequence to format commits according to Conventional Commit specifications:
    1. **Type Selector**: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`, `ci`.
    2. **Scope**: Optional module or component scope (e.g. `tui`, `auth`, `db`).
    3. **Description**: Concise summary line (5–72 characters).
    4. **Breaking Changes / Issues**: Notes breaking changes or closed issue IDs (`Closes #123`).

- **Git Commit Log Graph (`glog` / `glo` / `glg`)**:
  - **Functionality**: Paged 50-commit branch graph viewer (`git log --oneline --graph --decorate`).

- **Git Sync Operations**:
  - `gf` (`Git Fetch`): Executes `git fetch` to pull remote references without altering local HEAD.
  - `gpull` / `gpu` (`Git Pull`): Executes `git pull` on current branch.
  - `gpush` / `gus` (`Git Push`): Executes `git push` to upload local commits to upstream branch.

- **Git Undo (`git-undo` / `gundo`)**:
  - **Functionality**: Soft-resets last commit (`git reset --soft HEAD~1`), keeping all file changes staged in the index.

- **Git Nexus Dashboard (`nexus`)**:
  - **Functionality**: Multi-repository status overview rendering Git commit graphs, unstaged file counts, and inter-project dependencies.
  - `repo-graph`: Displays cross-project dependency tree links.
  - `nexus-stats`: Calculates commit velocity and author statistics across all registered workspaces.

---

### 🔨 4. .NET Development Tools
- **Core Build & Run Commands**:
  - `dbld` / `db`: Executes `dotnet build` in active workspace.
  - `dr`: Executes `dotnet run` in active workspace.
  - `dtst` / `dt`: Executes `dotnet test` test suite.
  - `df`: Runs `dotnet format` code style enforcer.
  - `dcl`: Runs `dotnet clean` build target cleaner.
  - `drestore` / `dres`: Executes `dotnet restore` to resolve NuGet packages.
  - `dpublish`: Executes `dotnet publish -c Release` to output production binaries.
  - `dwatch` / `dw`: Runs `dotnet watch run` for continuous live-reloading.

- **Clean Build Artifacts (`clean-build` / `dclean`)**:
  - **Functionality**: Recursively finds and deletes all `bin/` and `obj/` directories across the workspace.

- **EF Core Migration Commands**:
  - `add-migration` / `da`: Prompts for migration name and runs `dotnet ef migrations add <name>`.
  - `update-db` / `du`: Runs `dotnet ef database update`.

- **NuGet Package Publishing**:
  - `dpack`: Compiles Release package and outputs `.nupkg` to `./nupkg`.
  - `dpubpkg`: Interactively prompts for `.nupkg` file and NuGet API key, passing credentials securely via `NUGET_API_KEY` environment variables.

- **Rebuild Control Center TUI (`rebuild`)**:
  - **Functionality**: Runs `psapp/scripts/build_dev.ps1` to recompile `AgyTuiApp.csproj` with `-p:TreatWarningsAsErrors=true`. Handles binary locking by renaming active DLL to `*.old_*` before compilation.

---

### 🐳 5. Docker Container Suite
- **Docker Health Dashboard (`docker-health`)**:
  - **Functionality**: Renders real-time CPU %, Memory Usage / Limit, Network I/O, and container status for all containers.

- **Docker Cleanup TUI (`dkcl`)**:
  - **Sub-Actions**:
    - Stop & remove all active containers.
    - Prune unused images and dangling build layers.
    - Delete unused volumes and networks.

- **Container Management Commands**:
  - `dkstac`: Sends `SIGTERM` to stop all running containers.
  - `dkrmac`: Forcefully stops and removes all containers (`docker rm -f`).
  - `dimg`: Lists local Docker images formatted with Repository, Tag, Image ID, Size, and Created Date.
  - `dlogs`: Interactive container selection list to tail container logs (`docker logs -f --tail 100`).

- **Docker Compose Controls**:
  - `dcup` / `dkcpu`: Executes `docker compose up -d` in background mode.
  - `dcdown` / `dkcpd`: Executes `docker compose down` to stop and remove stack services.

---

### ☁️ 6. AWS & Cloud Diagnostics
- **AWS Identity & Environment**:
  - `aws-whoami`: Runs `aws sts get-caller-identity` to display Account ID, UserId, and ARN.
  - `aws-local`: Connects to LocalStack endpoint (`http://localhost:4566`) to verify sandbox service availability.

- **Cloud/Local Resource Inspectors**:
  - `aws-s3`: Lists S3 buckets (`aws s3 ls`).
  - `aws-sqs`: Lists SQS message queues (`aws sqs list-queues`).
  - `aws-ssm`: Displays Parameter Store parameters (`aws ssm describe-parameters`).
  - `aws-sns`: Lists SNS notification topics (`aws sns list-topics`).
  - `aws-dynamodb`: Lists DynamoDB tables (`aws dynamodb list-tables`).
  - `aws-lambda`: Lists serverless Lambda functions (`aws lambda list-functions`).

---

### 🤖 7. AI Agent & Ollama Integration Suite
- **Claude Code CLI Invocations**:
  - `claude`: Auto-selects Cloud API or Ollama local daemon based on `AiProviderMode`.
  - `claude-cloud`: Forces direct Cloud API invocation.
  - `claude-ollama`: Routes Claude requests through local Ollama daemon.

- **Gemini / Codex CLI Invocations**:
  - `codex`: Auto-selects provider based on `AiProviderMode`.
  - `codex-cloud`: Forces direct Gemini Cloud API access.
  - `codex-ollama`: Routes Codex requests through local Ollama daemon.

- **Specialized Local Models**:
  - `openclaw`: Launches OpenClaw model via Ollama.
  - `hermes`: Launches Hermes3 local reasoning model via Ollama.
  - `hermesd`: Launches Hermes3 in verbose debug mode.

- **Ollama Management Suite**:
  - `ollama-status`: Asynchronous non-blocking status widget checking daemon health on `http://localhost:11434`.
  - `ollama-models`: Interactive model weights manager (inspect, remove).
  - `ollama-pull`: Download new model weights from Ollama registry.
  - `ollama-start`: Spawns background `ollama serve` daemon process.
  - `ollama-logs`: Tails last 50 lines of local Ollama server log file.
  - `ollama-benchmark`: Measures prompt evaluation speed and token generation rate (tokens/sec).

- **Antigravity Deck Suite**:
  - `deck-status`: Queries port 3000 to verify Deck dashboard status.
  - `deck-setup`: Installs local Node.js npm packages for Deck.
  - `deck-start`: Launches local Deck web app at `http://localhost:3000`.
  - `deck-online`: Exposes local Deck dashboard via Cloudflare or Tailscale tunnel.

- **Antigravity CLI Launcher (`agy-cli`)**:
  - Launches executable terminal session for Google Antigravity CLI (`agy`).

- **AI Invocations History Ledger (`ai-history`)**:
  - Displays JSONL audit trail of past AI agent invocations, timestamps, model parameters, and target files.

---

### 🔐 8. AGY Account & Credential Management
- **Account Switcher (`agyswitch`)**:
  - **Functionality**: Interactive switcher for Google AGY / Gemini account credentials.
  - **Sub-Actions**:
    - `[Enter]`: Switch active credential context to selected account.
    - `[A]`: Interactively add/register new account credentials.
    - `[D]`: Delete selected account context.
    - `[O]`: Log out of selected account and clear active tokens.
    - **Live Search**: Type-ahead filter (`_detailsSearchBuffer`) to filter account names.

- **Account Quota & Limits Inspector**:
  - `agyquota`: Displays 5-hour and weekly request limits across registered accounts.
  - `account-tree`: Hierarchical tree rendering active accounts, token status (`Logged In` / `Not Logged In`), conversation counts, skill counts, and quota percentages.
  - `quota-chart`: Renders ASCII bar chart of 5-hour and weekly quota consumption.
  - `live-dashboard`: Real-time updating multi-column metrics table.

- **Context & Switch Controls**:
  - `autoswitch`: Toggles automatic directory-based account context switching (`[AgyAccountManager]::AutoSwitchOnDirectoryChange`).
  - `no-auto-commit` / `autocommit`: Toggles automatic git commits during multi-agent task execution.

- **Encrypted Secret Vault (`AgySecretVault`)**:
  - Stores credentials in `$AgySourceHome/secrets.json` using DPAPI encryption (`TokenVault.Protect` / `TokenVault.Unprotect`).

---

### 🧠 9. Learn & Study Suite
- **Interactive Study Router (`learn`)**:
  - **Sub-Topics**: `jp` (Japanese), `en` (English Vocab), `cs` (C#), `dsa` (Algorithms), `interview` (Questions), `custom` (User-defined topic).

- **AI Content Generator (`learn-gen`)**:
  - Generates new flashcard decks, grammar patterns, and interview STAR responses via `agy` or `claude` CLI. Validates output with `JsonDocument.Parse` before writing to `learn/`.

- **Obsidian Vault Integration**:
  - `obsidian`: Vault note search, tag browser, daily note reader, and wikilink graph renderer (`obs-graph`).
  - `refresh`: Rescans Obsidian Vault notes and synchronizes flashcards, quizzes, and cheat sheets into `learn/`.
  - `vault-open`: Launches Windows File Explorer at Obsidian Vault location.

- **Interactive Study Modules**:
  - `flashcard`: Flashcard deck viewer with SM-2 spaced-repetition scoring algorithm (`Again`, `Hard`, `Good`, `Easy`).
  - `grammar`: Practice drills for Japanese (N5–N2) and English grammar patterns.
  - `vocab`: Practice English vocabulary definitions, synonyms, and context sentences.
  - `kana`: Hiragana and Katakana character recognition quiz with empty-pool guards.
  - `kanji`: Radical search, stroke count breakdown, and readings lookup.
  - `jlpt`: JLPT vocabulary practice drills categorized by level (N5 to N1).
  - `algo`: Interactive terminal visualization for sorting and search algorithms.
  - `complexity`: Time and space Big-O complexity cheat-sheet matrix.
  - `problems`: Track status (`Solved`, `In Progress`, `Todo`), difficulty (`Easy`, `Medium`, `Hard`), and notes for DSA problems.
  - `snippets`: Multilingual code snippet browser with syntax highlighting.
  - `sheets`: Reference cheat-sheet browser (`.txt` / `.md` files).
  - `quiz`: Multiple-choice C# and .NET concepts test with score reporting.
  - `interview`: Question bank for technical system design and coding interviews.
  - `star`: Wizard for structuring Situation, Task, Action, Result behavioral responses.
  - `mock`: Timed mock interview response stopwatch.
  - `word-of-day`: Displays daily vocabulary word with definitions and usage example.

---

### 📊 10. Tracking & Progress Analytics
- **Pomodoro Focus Session (`session`)**:
  - 25-minute focus timer with visual progress bar and countdown notifications.

- **Study Statistics (`stats`)**:
  - Renders weekly study volume breakdown and retention charts spanning an exact 7-day window.

- **Daily Goals & Streak Counter**:
  - `goals`: View and update daily learning targets and completed tasks.
  - `streak`: Displays current consecutive daily study streak with future-date skew protection.

- **Review Queues**:
  - `due`: Summary of flashcards due for SM-2 spaced repetition review today.
  - `progress`: Overall progress bar charts across all learning categories.
  - `weak`: Pre-session review queue for low-scoring cards.

---

### ⚙️ 11. System & Networking Diagnostics
- **Disk Space Inspector (`disk` / `usage`)**:
  - Expandable status widget showing drive letters, filesystem types, total capacity, free space, usage percentages, and health status (`Healthy` < 75%, `Warning` >= 75%, `Critical` >= 90%).

- **Public IP Resolver (`public-ip` / `myip`)**:
  - Non-blocking widget resolving public IPv4 address via REST fallback API chain (`api.ipify.org`).

- **Kill Process on Port (`kill-port`)**:
  - Terminates process listening on target TCP port number (`kill-port <port>`).

- **SSH & Network Info (`ssh-info`)**:
  - Displays local IPv4 addresses, Tailscale mesh IP, active SSH connections, and Termux SSH guide.

- **Tailscale Mesh Diagnostics (`tailscale-status`)**:
  - Parses `tailscale status --json` to display connected mesh peers, IPs, and OS types.

- **Mobile SSH Tools**:
  - `ssh-qr`: Renders terminal QR code containing SSH connection string for mobile scanner.
  - `ssh-addkey-mobile` (`StartMobileSshKeyReceiver`): One-time token-authenticated listener for adding mobile public keys to `authorized_keys`.

---

### 🎨 12. Appearance, Layout & Control Center Engine
- **TUI Renderer Layouts (`ui-mode`)**:
  - `three-pane`: Traditional 3-column pane selector (Categories | Sub-pages | Details).
  - `flat-tree`: Modern tree renderer with expandable nodes, inline status widgets, and live search filtering.

- **Console Display Density (`density`)**:
  - `comfortable`: Full vertical spacing with description subtitles.
  - `compact`: High-density layout hiding non-selected row subtitles.

- **Theme Manager (`theme`)**:
  - Interactive theme selector for Oh-My-Posh prompt themes.

- **Mobile Setup Mode (`mobile-setup` / `mobile`)**:
  - Unified toggle configuring prompt mobile theme, compact TUI display density, and mobile context layout (`ConfigService.IsMobileContext()`).

- **Interactive Pager (`SpectrePager`)**:
  - Full-screen text reader with live incremental search (`/`), page navigation (`d`/`u`, `PgDn`/`PgUp`, `Home`/`End`, `g`/`G`), and line counts.

- **Shared Scrollable List View (`ScrollableListView`)**:
  - Unified viewport calculation (`ComputeViewport`) and scroll indicator component (`▲ N item(s) above`, `▼ N item(s) below`).

- **Command Palette & Help**:
  - `cc`: Main Command Palette launcher.
  - `help`: Interactive searchable help browser.
  - `hotkeys`: Hotkey guide grouped by domain.

---

### ⌨️ 13. Comprehensive Keyboard Controls, Hotkeys & Chords

#### A. TUI Tree Navigation (`flat-tree` / `three-pane`)
- `↑` / `k`: Move row selection up by 1.
- `↓` / `j`: Move row selection down by 1.
- `PageUp`: Move row selection up by visible viewport row count (`maxRows`).
- `PageDown`: Move row selection down by visible viewport row count (`maxRows`).
- `Home`: Jump selection to the very top row.
- `End`: Jump selection to the very bottom row.
- `/` or `?`: Activate live search input mode.
- `Enter` / `→`: Expand/collapse category or group, expand/collapse inline status widget, or run selected command.
- `←`: Collapse current category, group, or widget node.
- `Esc` / `q`: Clear active search filter or exit TUI back to shell.

#### B. In-App Sub-Picker Hotkeys
- `a`: Add new account (when in `agyswitch` mode).
- `d`: Delete account (when in `agyswitch` mode).
- `o`: Log out of account (when in `agyswitch` mode).

#### C. Global Shell Keyboard Chords & Aliases
- `Ctrl + Space`: Auto-complete suggestion chord.
- `Ctrl + Shift + C`: Launch Control Center TUI (`Invoke-ControlCenter`).
- `Ctrl + Shift + B`: Build project (`Invoke-DotNetBuild`).
- `Ctrl + Shift + T`: Run test suite (`Invoke-DotNetTest`).
- `F7`: View PowerShell command execution history (`Get-History`).

#### D. Quick Domain Command Aliases
- `cg`: Git status (`gs`).
- `cdk`: Docker health (`docker-health`).
- `cnav`: Workspace navigator (`proj`).
- `cai`: AI agent menu (`claude`).
- `csys`: System disk usage (`disk`).
- `cnet`: Network status (`ssh-info`).
- `cssh`: SSH connection info (`ssh-info`).
