# Plan: Full-Screen Command Shell Loop Rebuild (cc) & Reflow of the 4 Big Flows

This plan details the design and implementation tasks to rebuild the `cc` (Control Center) command into a full-screen **Interactive Command Shell Loop** (similar to `claude-cli` or `agy-cli`) that accepts direct commands, triggers searchable menus on `/`, and reflows all custom profile functions into four defined "Big Flows".

---

## 1. Enriched Classification of the 4 Big Flows

We have scanned the existing profile helper files and reorganized the profile's business logic into four distinct parts, defining the implementation scenarios and proposed enhancements for each:

---

### Part 1: Workspace & Code Navigation Flow (Enriched)
*   **Core Feature Set:** Workspace hopping (`proj` / `prj`), case-insensitive matching, and async Git branch status badges.
*   **Enhancement: Terminal-Based IDE with File Sidebar (VS Code alternative)**
    - Implement an option when executing `proj` to launch the project directly into a terminal-based IDE setup instead of just changing directory.
    - **TUI Editor Integrations:**
      - **Micro Editor Integration:** Check `$env:PATH` for `micro` and auto-install via `winget` if missing. Configure `micro` settings under `~/.config/micro/settings.json` to enable the `filemanager` plugin and bind `Ctrl+B` to toggle the sidebar folder tree layout (identical to VS Code).
      - **Neovim Editor Integration:** Provide launcher fallback hooks to open Neovim with file trees (`NvimTree` / `netrw` explorer).
    - **Key Bindings:**
      - Set up PSReadLine key bindings so typing `Ctrl+Shift+i` inside a project folder instantly starts the Terminal IDE dashboard.

---

### Part 2: Multi-Account Management & Local AI Flow (Refined)
*   **Core Feature Set:** DPAPI secure vault (`sec`), isolated `$env:GEMINI_HOME` account directories, self-healing junction maps (to global config/history folders), and Ollama model wrappers (`ask-ai`, `claude`, `hermes`).
*   **Refinement of Flow & UI:**
    - **Unified Account Command (`acc`):** Replace complex helper scripts with a single `acc` command supporting interactive dropdowns (when run without arguments) and direct flags (e.g. `acc use <name>`, `acc status`, `acc run <name> <cmd>`).
    - **Sandboxed Execution:** Implement `acc -temp <name> { <scriptblock> }` using `try/finally` protection to run commands under an isolated account directory without permanently modifying the session's active account.
    - **TUI Quota Visualization:** Render a visual daily usage history graph and current quota status chart (tracking 5-hour limits and rolling 7-day request history) inside the `acc` dashboard details screen.

---

### Part 3: Operations & Diagnostics Flow (Centralized)
*   **Core Feature Set:** Docker container lists (`dps`), performance gauges (`sysmon`), active port blocker (`killport`), SQLite viewer (`db-tui`), and colorized logs (`logstream`).
*   **Refinement of Flow & UI (Centralization):**
    - **Operations Command Central:** Group these individual diagnostic tools into a single, unified operations dashboard.
    - **Interactive Logs Hook:** Inside the `dkcl` Docker TUI, enable a shortcut key (e.g. `L`) to spawn `logstream` directly against the selected container's log tail, rather than running them as separate utilities.
    - **Process & Port Inspector:** Integrate `killport` directly inside the `sysmon` CPU/RAM dashboard so the user can inspect listening sockets and terminate hogging processes from a single screen.

---

### Part 4: Project Scaffolding & Version Control Flow (Enhanced)
*   **Core Feature Set:** Project Scaffolder (`new-project`), checkout picker (`co`), and Conventional Commit wizard (`gcmt`).
*   **Enhancement Suggestions:**
    - **Conventional Commit AI Prompt Tuning:** Refine the prompt template sent to local Ollama in `gcmt` to analyze staged changes, automatically categorize files into Git semantic zones, and suggest conventional scopes.
    - **Template Scaffolding Defaults:** Update `new-project` template layouts to include a default `Dockerfile` and database config setup.
    - **CI/CD Integration Checks:** Integrate automated linting and syntax validations on staged files before executing git commits.

---

## 2. Rebuilding the Command Center Shell (`cc`)

### The Goal
Convert `cc` into a dedicated full-screen menu-driven CLI dashboard with an ASCII header and default searchable menu.

### Design Details
1.  **ASCII Header:** Prints a premium ASCII `CC` logo with active account and connection status.
2.  **Default Searchable Menu:** Boots directly into the searchable Command Palette list view (`$SearchByDefault = $true`) listing all profile aliases.
3.  **In-Place execution & Return:** Clears the screen to execute the selected command, waits for any keypress on completion, and returns cleanly to the menu loop at the same cursor index.
4.  **Exit Command:** Includes a dedicated `[Exit Control Center]` menu option (or exits on `Esc` / `Ctrl+C`) to return back to standard PowerShell.

---

## 3. Tasks

### Phase 3: Interactive Command Shell Loop
- [x] Implement `Get-CustomCommands` input loop in `Aliases.ps1` to handle command inputs, `exit` triggers, and `/` commands.
- [x] Add command syntax dispatcher matching direct inputs to aliases.
- [x] Verify execution loops, command paletted select returns, and screen clears.

### Phase 4: Clean Visual Transitions & Screen Clears
- [ ] Implement line boundary calculations in `TerminalMenu.ps1` to cleanly overwrite old TUI panels with blank spaces before redrawing.
- [ ] Insert double empty line paddings (`\n\n`) when exiting TUI environments and executing commands inside `Aliases.ps1` to avoid overlapping outputs.
- [ ] Cache console cursor coordinates before launching TUI dashboards and restore them on exit.

### Phase 5: Terminal Loading Spinners
- [ ] Implement an asynchronous/non-blocking loading spinner in `LogHelper.ps1` using the character frame set `⠋⠙⠹⠸⠼⠴⠦⠧⠇⠏`.
- [ ] Integrate spinners into slow network, account, and database catalog check sequences.
- [ ] Format visual success/failure states (`[OK]`, `[WARN]`, `[ERR]`) on spinner completion.

### Phase 6: Redefined Ollama AI CLI Flow (AI Hub)
- [ ] Create a unified TUI client dispatcher command (`ai`) inside `AiHelper.ps1`.
- [ ] Query available local Ollama models (`ollama list`) and display them in a searchable dropdown selector.
- [ ] Bind model selections dynamically to `$env:GEMINI_HOME` so switching accounts updates AI preferences.
- [ ] Provide options to launch the `claude`, `hermes`, and `codex` CLIs pre-configured with the selected local model context.

### Phase 7: Additional Enhancements
- [ ] Register `Ctrl+Space` using PSReadLine key handlers to trigger the `cc` Command Shell loop.
- [ ] Wire the Oh My Posh theme switch handler to trigger `InitializeTuiColors()` dynamically.
- [ ] Integrate an ASCII Git branch graph visualizer inside `GitHelper.ps1` post Conventional Commits.

---

## 4. UI Color & Behavior Standards (TUI Guidelines)

To establish a premium, cohesive command interface across the entire PowerShell environment (matching `agy-cli` and `claude-cli`), we define the following styling and interaction rules:

### A. Centralized Theme Color Palette
All custom TUI dashboards, status checkers, and terminal views must resolve colors dynamically via `$Global:TuiColors` initialized by `[TerminalMenu]::InitializeTuiColors()`. No static color values (like `Write-Host -ForegroundColor Cyan` or `Yellow`) are permitted.
- **Header:** Bright accent color used for borders, titles, and section splits (e.g. Tokyonight Cyan, Dracula Magenta).
- **Selected:** High-contrast focus color used to highlight the active menu cursor (e.g. Tokyonight Magenta, Dracula DarkCyan).
- **Regular:** Muted, readable base color for general list details and descriptions (usually Gray).
- **Suggest/Search:** Attention-drawing color for search queries and match suggestions (e.g. Yellow).
- **Footer/Muted:** Low-contrast shade for page numbers, keys mappings, and passive metadata (DarkGray).
- **Alert:** Bright error/alert states (Red).

### B. Canvas Management & Screen Clears (Anti-Artifact Rules)
- **Flicker-Free Clears:** All TUIs must refresh in-place rather than calling `Clear-Host` on every keystroke, preventing heavy console screen flicker.
- **Wiping Unused Space:** When transitioning between menus (e.g. from parent Command Shell into the searchable Command Palette, or selecting a sub-menu), the rendering engine must calculate the exact line height of the previous drawing box and overwrite those lines with blank spaces (`" " * [Console]::WindowWidth`) before drawing. This ensures **no leftover lines or double boundaries** clutter the interface.
- **Boundary Separation:** When launching a terminal utility or command, the TUI must cleanly output double empty lines (`\n\n`) to clearly partition the menu state from command execution text.
- **Cursor State Restore:** Before launching a TUI, cache current cursor coordinates. On exit, restore console coordinates and reset window scroll heights.

### C. Standardized Search Interactions
- **Search-First Design:** Search-heavy navigation components (like `proj`, `cc`, `co`) must open with filtering enabled by default. Typing any letter filters results instantly.
- **Input Keys:**
  - `Enter`: Select and execute the highlighted command.
  - `Esc` or `Ctrl+C`: Instantly close the menu and return to previous state.
  - `Backspace`: Erase search query characters.
  - `Up` / `Down` arrows: Scroll through filtered matches.

### D. Consistent Sub-menu Tree Navigation
- **Navigation Nesting:** Selecting an item from a list (e.g. selecting an account in `acc` or container in `dkcl`) must transition into a secondary TUI action list instead of dumping raw outputs to stdout.
- **Unified Exit Key:** All interactive sub-menus must reserve the bottom item as `[Back]` (or `[x] Back`) to navigate up the TUI menu tree cleanly.

---

## 5. Console Loading Spinner (Animator)

To match the premium CLI experience of `claude-cli` and `agy-cli`, slow operations (such as server checks, credentials decryption, database loads, and container queries) will utilize a terminal spinner animation instead of freezing:

1.  **Spinner Frames:** Use the standard 10-frame character cycle: `⠋`, `⠙`, `⠹`, `⠸`, `⠼`, `⠴`, `⠦`, `⠧`, `⠇`, `⠏`.
2.  **Implementation:**
    - Spinners run inside a non-blocking scriptblock loop.
    - Standard pattern uses `[Console]::Write` with `\r` (carriage return) to overwrite the spinner character in-place, keeping the terminal line clean.
3.  **Completion States:**
    - On success, replace the spinner with `[OK]` (Green text).
    - On warning, replace with `[WARN]` (Yellow text).
    - On failure, replace with `[ERR]` (Red text).

---

## 6. Redefining the Multi-Agy & Ollama AI CLI Flow

All local AI wrappers and server dependencies are routed through a unified **AI Hub Dashboard** command (`ai`):

1.  **Server Status Checker:** Automatically queries `http://localhost:11434` with the spinner loader. If stopped, prompts to auto-start Ollama Proxy on port 11435.
2.  **Model Catalog Visualizer:** Queries local model tags via `ollama list` and renders them in a searchable dropdown selector.
3.  **Client Dispatcher:**
    - Select a model (e.g. `llama3`, `deepseek-coder`) and choose which client to load:
      - **Claude CLI (`claude`):** Standard developer chat console.
      - **Hermes TUI (`hermes`):** Local autonomous agent workspace.
      - **Codex CLI (`codex`):** Command generator tool.
4.  **Sandbox Context:** Ensure model selections and token settings are saved locally to `$env:GEMINI_HOME` so that switching Antigravity accounts automatically swaps active model preferences.

---

## 7. Additional Proposed Enhancements

-   **Global Hotkey Launcher:** Register `Ctrl+Space` using PSReadLine key handlers to immediately bring up the `cc` Command Shell loop from anywhere in the terminal.
-   **Dynamic Theme Synchronizer:** Listen to Oh My Posh theme switches in `ThemeHelper` and trigger `InitializeTuiColors()` dynamically to swap TUI palette highlights instantly without shell reboots.
-   **Interactive Git Commit Graph:** Render a simplified, color-coded Git branch branch chart after Conventional Commits are generated via `gcmt`.
