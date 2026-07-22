# 🛸 Full Feature Catalog — PowerShell Profile & AgyTui Control Center

Exhaustive, deeply nested specification of all features, sub-components, UI views, execution mechanics, file storage locations, keyboard controls, hotkeys, combo chords, status widgets, and config flags across `Microsoft.PowerShell_profile.ps1` and `AgyTuiApp`.

---

### 📁 1. Workspace & Project Navigation Suite
- **Workspace Navigator (`proj` / `p` / `prj` / `go`)**:
  - **Functionality**: Fuzzy and substring matching navigator that scans, filters, and jumps between registered development workspaces.
  - **Execution Mechanics**:
    - Queries `WorkspaceRegistry.GetWorkspaces()` backed by a 5-second `TtlCache`.
    - Matches search queries against `WorkspaceEntry.Name` and `WorkspaceEntry.WorkspacePath` using case-insensitive substring search (or optional regex pattern matching).
  - **Sub-Picker Live Search**:
    - Maintains `_detailsSearchBuffer` capturing printable keystrokes in real time.
    - Filters visible list entries dynamically on every keystroke without requiring `Enter`.
  - **Single-Match Jump**:
    - Bypasses target selection prompt and jumps immediately if exactly one registered workspace matches the query.
  - **Interactive Target Selector**:
    - Displays a scrollable menu of matching workspaces formatted with active git branch decorations `[branch-name]` and full filesystem paths.
  - **Shared Workspace Actions**:
    - `📂 Change Directory on exit (cd)`:
      - **Mechanism**: Writes the resolved target directory path to `$AgySourceHome/selected_project.txt`.
      - **Shell Hook Integration**: The PowerShell profile prompt hook (`[AgyAccountManager]::RegisterPromptHook`) checks for `selected_project.txt` upon TUI exit and executes `Set-Location <Path>` automatically.
      - **Fallback Path Handling**: Defaults to `C:\Users\Public\.gemini\selected_project.txt` or `~/.gemini/selected_project.txt` if custom paths are unset.
      - **Exit Trigger**: Closes the TUI session and returns control to the shell prompt initialized at the chosen workspace path.
    - `🚀 Launch New Terminal Session (wt / pwsh)`:
      - **Mechanism**: Calls `SystemHelper.OpenNewTerminalSession(path)`.
      - **Execution**: Spawns `wt.exe -d "<path>"` via `Process.Start`. If `wt.exe` is not found, falls back to `pwsh.exe -NoExit -Command "Set-Location '<path>'"`.
    - `💻 Open in Terminal IDE (/ide)`:
      - **Mechanism**: Executes `TerminalIde.Open(path)` within the active process.
      - **UI Transition**: Clears terminal screen and switches to the multi-pane file browser and code viewer.
    - `📁 Open in Windows File Explorer (f)`:
      - **Mechanism**: Executes `SystemHelper.OpenExplorer(path)`.
      - **Execution**: Spawns `explorer.exe "<path>"` using native shell execution.
    - `🔀 View Git Status & Diff (/ide-diff)`:
      - **Mechanism**: Invokes `GitDiffViewer.ShowDiff(path)`.
      - **UI Transition**: Opens full-screen colorized side-by-side / unified git diff viewer for modified files in the workspace.
  - **Auto-Discovery Engine**:
    - **Configured Base Path Scanning**: Iterates over `Config.Current.ProjectsBaseDir`, `~/project`, `~/Documents`, and `~/Desktop`.
    - **Project Marker Detection**: Scans subdirectories for `.git` (directories, worktrees, or submodule files), `*.csproj`, `*.sln`, or `package.json`.
    - **Isolation & Performance**: Wraps per-directory evaluation in localized `try/catch` blocks so permission errors or broken symlinks do not halt discovery for sibling directories. Skips hidden folders (`.*`) and `node_modules`.
    - **Persistence**: Auto-discovered entries are saved to `$AgySourceHome/antigravity/priority_workspaces.json`.

- **Open File Explorer (`f`)**:
  - **Functionality**: Instantly opens Windows File Explorer targeting the active working directory or specified path parameter.
  - **Execution**: Calls `SystemHelper.OpenExplorer(pwd)` using `ProcessStartInfo("explorer.exe", path)`.

- **New Terminal Session (`open-term`)**:
  - **Functionality**: Spawns a standalone Windows Terminal or PowerShell window at the current location.
  - **Execution**: Invokes `SystemHelper.OpenNewTerminalSession` with process isolation, preventing child process blocking on the main shell session.

- **Shell Directory Navigation Shortcuts**:
  - `..` (`Set-LocationParent`):
    - **Mechanism**: Alias for `Set-Location ..`. Steps up one directory level.
  - `...` (`Set-LocationGrandParent`):
    - **Mechanism**: Alias for `Set-Location ../..`. Steps up two directory levels.

---

### 💻 2. Terminal IDE Suite
- **Interactive Terminal IDE (`ide`)**:
  - **Functionality**: In-terminal development environment with file navigation, file viewing, and symbol resolution.
  - **Sub-Components**:
    - **File Explorer Sidebar**:
      - Renders directory tree with file extension icons (Nerd Font and UTF-8 fallbacks).
      - Displays `.. (go up)` navigation option and parent directory context.
    - **Code Viewer Panel**:
      - Renders file contents with ANSI syntax highlighting.
      - Includes line numbering and page bound indicators.
    - **Symbol Navigator**:
      - Scans source files (`.cs`, `.ps1`, `.ts`, `.js`, `.py`) for class, interface, method, and function declarations using regex pattern extractors.
  - **Controls & Hotkeys**:
    - `↑` / `↓` or `j` / `k`: Navigate directory tree or scroll source code view.
    - `Enter`: Open directory or inspect source file in viewer panel.
    - `/`: Search for symbols or function definitions.
    - `Esc` / `q`: Return to parent directory or exit IDE.

- **Workspace Diff Viewer (`ide-diff` / `gd`)**:
  - **Functionality**: Interactive colorized diff viewer showing staged (`git diff --staged`) and unstaged changes across all workspace files.
  - **Execution**: Runs `git diff` via `ProcessRunner.Run` with `ArgumentList` safety. Renders additions in green (`+`) and deletions in red (`-`).

- **Workspace Code Search (`ide-search`)**:
  - **Functionality**: Workspace-wide code and symbol search tool utilizing ripgrep / regex matching across source code files.
  - **Execution**: Executes `rg` (ripgrep) or fallback directory traversal, listing file paths, line numbers, and code snippets.

- **SQLite Database Browser (`db-tui`)**:
  - **Functionality**: Interactive schema inspector and data browser for `.db` / `.sqlite` files.
  - **Security & Safety**:
    - **Write-Guard Protection**: Intercepts queries starting with `.shell`, `.load`, or unsafe sqlite3 dot-commands to prevent process execution.
    - **Auto-Backup**: Automatically creates `.bak` copy of database file prior to executing mutating SQL queries (`INSERT`, `UPDATE`, `DELETE`, `DROP`, `ALTER`).
  - **UI Rendering**: Displays tables and query results formatted via Spectre `Table` grid.

---

### 🌿 3. Git & Repository Management
- **Git Status Summary (`gs`)**:
  - **Functionality**: Colorized short-format git status summary (`git status --short`).
  - **Output**: Categorizes modifications into untracked (`??` red), modified (`M` yellow), staged (`A` green), and current branch tracking state.

- **Stage All Modifications (`ga`)**:
  - **Functionality**: Executes `git add .` to stage all modified, deleted, and untracked files in the current repository.

- **Git Branch Manager (`gbr` / `gb`)**:
  - **Functionality**: Interactive branch selector sorted by recent commit date.
  - **Worktree & Submodule Support**: Parses `.git` files containing `gitdir: <path>` references to resolve `HEAD` branch name accurately for worktrees and submodules.
  - **Checkout Execution**: Runs `git checkout <branch>` instantly upon `Enter` selection.

- **Conventional Commit Wizard (`gcmt`)**:
  - **Functionality**: Guided 4-step prompt sequence to format commits according to Conventional Commit specifications:
    1. **Type Selector**: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`, `ci`.
    2. **Scope**: Optional module or component scope (e.g. `tui`, `auth`, `db`).
    3. **Description**: Concise summary line (5–72 characters).
    4. **Breaking Changes / Issues**: Notes breaking changes or closed issue IDs (`Closes #123`).
  - **Execution**: Passes formatted message string to `git commit -m` via `ProcessRunner.Run` using `ArgumentList` array splatting.

- **Git Commit Log Graph (`glog` / `glo` / `glg`)**:
  - **Functionality**: Paged 50-commit branch graph viewer (`git log --oneline --graph --decorate`). Renders branch points and tags in multi-color ANSI formatting.

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
  - `add-migration` / `da`: Prompts for migration name and runs `dotnet ef migrations add <name>` using `ArgumentList`.
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

- **TUI Tree Navigation**:
  - `↑` / `k`: Selection up by 1.
  - `↓` / `j`: Selection down by 1.
  - `PageUp`: Scroll up by `maxRows`.
  - `PageDown`: Scroll down by `maxRows`.
  - `Home`: Jump to top row.
  - `End`: Jump to bottom row.
  - `/` or `?`: Activate live search filter mode.
  - `Enter` / `→`: Expand/collapse node/widget, or execute command.
  - `←`: Collapse current node or widget.
  - `Esc` / `q`: Clear search filter or exit TUI back to shell.
- **In-App Sub-Picker Hotkeys**:
  - `a`: Add new account.
  - `d`: Delete account.
  - `o`: Log out of account.
- **Global Shell Keyboard Chords**:
  - `Ctrl + Space`: Auto-complete suggestion chord.
  - `Ctrl + Shift + C`: Control Center TUI (`Invoke-ControlCenter`).
  - `Ctrl + Shift + B`: Build project (`Invoke-DotNetBuild`).
  - `Ctrl + Shift + T`: Run test suite (`Invoke-DotNetTest`).
  - `F7`: PowerShell command history (`Get-History`).
- **Quick Domain Command Aliases**:
  - `cg`: Git status (`gs`).
  - `cdk`: Docker health (`docker-health`).
  - `cnav`: Workspace navigator (`proj`).
  - `cai`: AI agent menu (`claude`).
  - `csys`: System disk usage (`disk`).
  - `cnet`: Network status (`ssh-info`).
  - `cssh`: SSH connection info (`ssh-info`).
