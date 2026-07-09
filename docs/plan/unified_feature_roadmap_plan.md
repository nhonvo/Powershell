# Unified Profile Feature Roadmap & Rebuild Plan

This document consolidates and unifies all planned features, specifications, and architectural details from all 14 plan files in the `docs/plan/` directory. All features are organized under logical flows and formatted using the standardized **Feature - User Story** structure.

---

## 1. Core Command Center Loop & Visuals (`cc`)

### [Feature 1.1] Full-Screen Interactive Command Center Loop
*   **Feature:** Rebuild the `cc` command to boot into a full-screen, dedicated TUI dashboard displaying a stylized block ASCII `CC` logo, active credentials status, system time, and a search-on-type Command Palette.
*   **User Story:** As a developer, I want to launch a clean, interactive dashboard from my shell, so that I can visually trigger custom commands, search through all aliases, and easily navigate sub-menus without cluttering my terminal scrollback.
*   **Implementation Tasks:**
    - [x] Create the full-screen layout loop and header blocks.
    - [x] Support search-on-type so typing filters the list instantly without starting in search query input mode.
    - [x] Restore default settings to display the logo and list options directly on startup.

### [Feature 1.2] Flicker-Free Canvas & Output Partitioning
*   **Feature:** Implement screen-wiping mechanisms that calculate panel sizes and overwrite them with empty lines during menu transitions, preventing text artifacts and console flickering.
*   **User Story:** As a terminal user, I want the TUI selector interface to cleanly redraw in-place, so that I do not see overlapping console outputs or duplicated menu fragments when scrolling or filtering.
*   **Implementation Tasks:**
    - [x] Cache current cursor coordinates and calculate lines to redraw dynamically.
    - [x] Overwrite trailing items with blank spaces before redrawing.
    - [x] Add double-line empty spacing (`\n\n`) when launching shell processes to separate the menu loop outputs.

### [Feature 1.3] Dynamic Theme Preview & Control Center Sync
*   **Feature:** Enhance theme selection with table-formatted lists displaying segment type breakdowns and colored emojis mapping to hex values. Dynamically extract the dominant segment background color from the active theme's JSON to sync the Control Center's (`cc`) TUI color highlights automatically without shell reloads.
*   **User Story:** As a developer switching themes, I want to quickly visualize theme layouts using segment lists and colored indicators, and have the Command Center colors automatically match the active theme.
*   **Implementation Tasks:**
    - [x] Extract Oh My Posh theme segment types and background colors to show visual table rows (e.g. `🔵 session  🟣 path`).
    - [x] Dynamically parse background color hexes of the first block segment in the active theme config.
    - [x] Map hex colors to standard ConsoleColor values.
    - [x] Wire `InitializeTuiColors` to update `$Global:TuiColors` instantly upon selection.

---

## 2. Workspace & Code Navigation Flow (Part 1)

### [Feature 2.1] Terminal-Based IDE with File Sidebar (VS Code Alternative)
*   **Feature:** Integrate workspace navigator (`prj`/`proj`) to hop between projects and instantly launch them into a terminal-based editor (Micro or Neovim) configured with a file sidebar tree explorer.
*   **User Story:** As a modal or terminal editor user, I want my workspace navigator to offer an action to open the chosen project inside Micro or Neovim, so that I can edit files and browse directory structures without leaving my console window.
*   **Implementation Tasks:**
    - [x] Configure Micro editor json settings to auto-install and bind the `filemanager` tree sidebar.
    - [x] Bind `Ctrl-b` to `command:filemanager.toggle_tree` in Micro configuration.
    - [x] Enable `filemanager.openonstart` in Micro settings to launch the sidebar immediately.
    - [x] Verify paths and check if `micro` is on `$env:PATH`, offering an auto-installation prompt via Winget (`zyedidia.micro`) if missing.
    - [x] Set up Neovim launcher hook as a fallback for modal editing.
    - [x] Register PSReadLine key binding `Ctrl+Shift+i` to launch the IDE dashboard directly.

### [Feature 2.2] Interactive Workspace Search & Git Status Badges
*   **Feature:** Pull Git branch status summaries and modified file counts asynchronously or via non-blocking queries, rendering them as branch badges directly on project list rows.
*   **User Story:** As a developer hopping between workspace repositories, I want my project selection list to show branch names and pending changes next to each project row, so that I can prioritize work without checking git status manually.
*   **Implementation Tasks:**
    - [x] Check paths and resolve active project lists.
    - [x] Query Git status details for active branches.
    - [x] Append Git status descriptions to navigation labels.
    - [x] Add loading spinner animation to prevent screen freezes during directory scanning.

### [Feature 2.3] Project Actions Submenu
*   **Feature:** After selecting a project from the workspace list, render a secondary interactive Action Submenu instead of triggering hidden hotkeys.
*   **User Story:** As a developer, I want to see a clear list of actions after selecting a project (e.g., Open in VS Code, Open in Terminal IDE, Open in New Terminal Tab, Start Docker Compose), so that I don't have to memorize keyboard shortcuts to launch my environments.
*   **Implementation Tasks:**
    - [ ] Build a secondary `TerminalMenu` that appears when a project is pressed via `Enter`.
    - [ ] Add explicit options: `[1] Open in VS Code`, `[2] Open in Terminal IDE (Micro/Nvim)`, `[3] Open in New Terminal Window`, `[4] Navigate here in current terminal`.
    - [ ] Integrate Agy IDE bindings if applicable.

### [Feature 2.4] High-Speed Workspace Directory Caching
*   **Feature:** Cache the results of the filesystem `Get-ChildItem` directory scan to a local JSON/CSV file (`~/.gemini/antigravity/workspace_cache.json`) to dramatically reduce startup lag when opening the Project menu.
*   **User Story:** As a power user with hundreds of nested directories, I want the project list to load instantly without waiting for disk I/O scans, so that context switching is instantaneous.
*   **Implementation Tasks:**
    - [ ] Implement a background job or cache-invalidation trigger to refresh the directory list daily or manually via a hotkey (`F5`).
    - [ ] Read from the cache file first when `[Project]` is invoked.

### [Feature 2.5] Dynamic Project Pinning & TUI Git Cloner (Suggested Enhancements)
*   **Feature:** Allow users to press `[Ctrl+P]` on a project row to dynamically pin it to the top of the list (Priority list), and add a `clone-project` command that clones a repo and auto-registers it.
*   **User Story:** As an active developer, I want to quickly favorite my current active sprint projects to the top of my list, and have new Git clones automatically appear in my workspace navigator without editing profile scripts manually.
*   **Implementation Tasks:**
    - [ ] Update `ProfileNavigator.ps1` to read/write priority lists from a configuration file instead of hardcoded arrays.
    - [ ] Map a keyboard shortcut in `TerminalMenu` for pinning.
    - [ ] Create `clone-project <url>` wrapper that handles `git clone` and caches the new path.

---

## 3. Multi-Account Management & Local AI Flow (Part 2)

### [Feature 3.1] Unified Account Command & Sandboxing (`acc`)
*   **Feature:** Consolidate keyring credentials and accounts switching under a single `acc` command that supports both interactive TUI toggles and temporary execution sandboxes.
*   **User Story:** As a developer using multiple workspace profiles, I want to dynamically change accounts or run specific scripts temporarily under another profile context, so that my credential settings remain isolated and clean.
*   **Implementation Tasks:**
    - [x] Implement DPAPI secret encryption and active token backup logic.
    - [x] Build the unified interactive account switcher dashboard.
    - [x] Implement the `-temp` switch parameter to run scriptblocks inside an isolated temporary directory sandbox.
    - [x] Render daily usage history graphs and rolling 7-day quota status charts inside the `acc` dashboard.

### [Feature 3.2] Local AI Providers & Sandbox Contexts (`ai`)
*   **Feature:** Provide wrappers for native local Ollama integration, isolating local Codex runs, removing account credential context dependencies from local LLMs, and pulling model tags dynamically.
*   **User Story:** As an offline developer using local LLMs, I want my AI launcher to set sandbox folders for Codex CLI configs, pull models, and check server status cleanly, so that I can execute tasks without API delays or token leaks.
*   **Implementation Tasks:**
    - [x] Create Claude, Hermes, and Codex local Ollama launchers.
    - [x] Auto-generate minimal config sandboxes for local Codex runs to disable global MCP/skills.
    - [x] Clear global account context checks when executing local Ollama wrappers.
    - [x] Add Ollama management options (Pull, List, Set default model, view logs) inside the AI Hub Dashboard.

---

## 4. Operations & Diagnostics Flow (Part 3)

### [Feature 4.1] Centralized Operations & Docker TUI Dashboard (`dkcl`)
*   **Feature:** Group system diagnostic and container monitoring tools (Docker container visualizer, Cpu/Ram gauges, and port blocker) into a single, unified Operations Dashboard.
*   **User Story:** As a devops developer, I want to manage running local services and inspect resources in one place, so that I do not need to execute separate scripts or terminal commands.
*   **Implementation Tasks:**
    - [x] Implement interactive container management TUI (`dkcl`).
    - [x] Integrate Compose grouping to show containers grouped by project labels.
    - [x] Wire shortcut keys inside `dkcl` to pipe container outputs directly to the log streaming engine.
    - [x] Implement container action submenu (Stop, Logs, Shell, Restart, Remove).

### [Feature 4.2] System Resources Monitor & Socket Cleaner (`sysmon` / `killport`)
*   **Feature:** Integrate resource inspection meters with process blockers to resolve port collisions from the same screen.
*   **User Story:** As a web developer, I want to see system resource stats and terminate port-hogging processes instantly, so that I can resolve port collisions when launching local web servers.
*   **Implementation Tasks:**
    - [x] Create cpu and memory diagnostics gauges (`sysmon`).
    - [x] Implement target port listener checkers (`Get-NetTCPConnection`) and termination processes (`killport`).
    - [x] Bind process killers directly inside the resource monitor dashboard.

### [Feature 4.3] Fuzzy History Searcher (`Ctrl+R`)
*   **Feature:** Bind `Ctrl+r` to a dynamic `TerminalMenu` search bar populated with past commands from PowerShell history.
*   **User Story:** As a terminal user, I want a visual fuzzy searcher for my terminal history, so that I can easily find and execute past complex commands.
*   **Implementation Tasks:**
    - [x] Set PSReadLine key handler for `Ctrl+r`.
    - [x] Read `Get-History` and persistent PSReadLine histories into the terminal menu.
    - [x] Disable handler when `$Global:AiMode -eq $true`.

---

## 5. Project Scaffolding & Version Control Flow (Part 4)

### [Feature 5.1] Template-Driven Scaffolding & CI Checks (`new-project`)
*   **Feature:** Bootstrap new projects using configurable layouts including predefined files like default Dockerfiles and database configs, combined with pre-commit CI script checks.
*   **User Story:** As a developer creating new microservices, I want to scaffold projects with prefilled Dockerfiles and configurations, so that my project setup matches local best practices.
*   **Implementation Tasks:**
    - [x] Create interactive templates picker for .NET API/Console, React/Vite UI, Node Console.
    - [x] Auto-register generated directories to `ProfileNavigator.ps1`.
    - [x] Implement AI-safe direct non-interactive argument support (`-Template`, `-Name`, `-Port`).

### [Feature 5.2] Semantic Git Checker & AI Commit Prefills (`co` / `gcmt`)
*   **Feature:** Interactive Git branch checkouts, Conventional Commit formatting, stage verification checks, and local Ollama-based commit message suggestions.
*   **User Story:** As a developer staging commits, I want a Conventional Commit assistant that validates staged changes and generates semantic summaries using local models, so that my repository logs remain clean.
*   **Implementation Tasks:**
    - [x] Build branch selector (`co`) using `TerminalMenu`.
    - [x] Build 3-step Conventional Commit wizard (`gcmt`) (Type -> Scope -> Desc).
    - [x] Implement stage validation check (`git diff --cached`).
    - [x] Wire dynamic `[Tab]` completion triggers to send diff contexts to Ollama models.

---

## 6. Console Loading Spinner (Animator)

### [Feature 6.1] Asynchronous Console Loader Spinner
*   **Feature:** Implement an asynchronous, non-blocking terminal loading spinner that uses the 10-frame character cycle: `⠋`, `⠙`, `⠹`, `⠸`, `⠼`, `⠴`, `⠦`, `⠧`, `⠇`, `⠏`.
*   **User Story:** As a developer executing slow commands, I want to see a clean, animated spinner, so that I know the shell is working instead of freezing.
*   **Implementation Tasks:**
    - [x] Write background runspace/job wrapper in `LogHelper.ps1` to spin characters without blocking key inputs.
    - [x] Render dynamic progress messages next to the spinner.
    - [x] Replace spinner on completion with formatted badges: `[OK]` (Green), `[WARN]` (Yellow), or `[ERR]` (Red).

---

## 7. Dual-Mode Terminal Safety & AI Detection

### [Feature 7.1] Automated AI Agent Shell Profiling
*   **Feature:** Dynamically analyze startup environment parameters (`$env:AI_MODE`, `$env:TERM_PROGRAM`, `$env:GEMINI_API_KEY`) to detect AI agents vs human sessions, converting rich TUI outputs into token-saving data streams.
*   **User Story:** As an AI coding assistant, I want the shell profile to silently boot and commands to return compact data structures without blocking menus, so that I can execute tasks without wasting token budgets.
*   **Implementation Tasks:**
    - [x] Implement context detection hooks for Claude, Copilot, Codex, Cursor, and Gemini.
    - [x] Bypass ASCII logos and UI elements to save ~300 startup tokens.
    - [x] Convert `sysmon` to output raw single lines (saving 75% tokens).
    - [x] Convert `dkcl` to output tab-separated tables.
    - [x] Convert `killport` to output raw key-value parameters.
    - [x] Convert `co` to output flat branch lists.

---

## 8. Integrated Utilities (Log stream, DB TUI & Secret Vault)

### [Feature 8.1] Encrypted Secrets Vault (`sec`)
*   **Feature:** TUI secret key injector securing credentials utilizing Windows Data Protection API (DPAPI) file serialization.
*   **User Story:** As a developer using private API tokens, I want to securely store my passwords in an encrypted vault locally, so that they are not written as plain text in source code files.
*   **Implementation Tasks:**
    - [x] Bind encryption/decryption tasks using .NET security namespaces.
    - [x] Dynamically load decrypted secrets into current terminal environment scopes on startup.

### [Feature 8.2] Log Multiplexer Streamer (`logstream`)
*   **Feature:** Color-highlighted log stream aggregates reading from both static logs and active docker containers in real-time.
*   **User Story:** As an operations engineer tailing multiple service systems, I want to inspect merged log files formatted with red/yellow syntax highlighting, so that I can detect issues immediately.
*   **Implementation Tasks:**
    - [x] Implement regex pattern highlights for errors and warnings.
    - [x] Build input pipeline listeners that read from file tails.

### [Feature 8.3] Database Catalog Schema Explorer (`db-tui`)
*   **Feature:** SQLite database visual inspector outputting table outlines and limited data preview rows.
*   **User Story:** As a developer inspecting application schemas, I want to preview database tables directly in my shell, so that I do not need to launch separate database management tools.
*   **Implementation Tasks:**
    - [x] Write schema reader utilizing ADO.NET SQLite connections.
    - [x] Build table selection lists and row limit boundaries.

---

## 9. Code Quality, Formatter & AST Refactoring

### [Feature 9.1] AST Refactoring Assistant (`refactor`)
*   **Feature:** Code style formatter (`Prettier`, `ESLint`, `dotnet format`) and AST file-size scanner identifying split targets inside monolithic scripts.
*   **User Story:** As a developer refactoring large codebases, I want the helper to format syntax styles and automate splitting >500 line monolithic files into modular parts, so that the codebase remains maintainable.
*   **Implementation Tasks:**
    - [x] Build formatter dashboard supporting multi-language syntax linters.
    - [x] Implement monolithic file scan (`refactor -breakdown`).
    - [x] Parse Abstract Syntax Trees (AST) in PowerShell to find split boundaries.

---

## 10. Verification & Pester Unit Tests

### [Feature 10.1] AST Syntax Validation & Mocks Runner
*   **Feature:** Pester unit test suites testing profile syntax, alias declarations, and external CLI mock validations running locally and in CI (`run_tests.ps1`).
*   **User Story:** As a developer validating shell changes, I want the testing framework to mock third-party CLI binaries, verify scripts syntax, and run in CI, so that no profile updates introduce regressions.
*   **Implementation Tasks:**
    - [x] Implement AST syntax parsing tests on all helper files.
    - [x] Create Pester tests mocking `git`, `docker`, `ollama`, and `aws`.
    - [x] Validate execution pathways under both interactive and AI-agent simulated settings (`$env:AI_MODE = 'true'`).
    - [x] Build dual-mode assertions for command outputs.
