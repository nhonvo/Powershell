# Proposed Terminal Features & Token-Saving AI Agent Enhancements

This document details new features, enhancements, and a **Dual-Mode Terminal Profile** specification. It is designed to save context window tokens when an AI agent (such as Claude Code, Codex, or Gemini) is executing commands or running scripts in the terminal, while preserving rich visual aesthetics for human developers.

---

## 1. Core Architecture: Dual-Mode Terminal (`Human` vs. `AI Agent`)

PowerShell will dynamically detect if the current session is being run by an AI Agent (by checking environment variables or a profile flag) and swap between two modes:

1.  **Human Mode (Rich Visuals):** Prints ASCII art, decorative borders, progress bars, emoji icons, and interactive TUI menus.
2.  **AI Mode (Token-Saving):** Removes all drawings, logos, and blank spacers. Formats outputs as compact key-value lines or minimal JSON to reduce token usage in the AI's prompt context.

### Detection Mechanism:
Add a loader check at the top of the PowerShell profile:
```powershell
# Auto-detect AI Agent runner context
$Global:AiMode = $false
if ($env:AI_MODE -eq "true" -or 
    $env:TERM_PROGRAM -eq "anthropic-code" -or 
    $env:GEMINI_API_KEY -ne $null -and ($env:TERM -eq "dumb" -or $env:PAGER -eq "cat")) {
    $Global:AiMode = $true
}
```

---

## 2. Feature Breakdown: Human vs. AI Agent Modes

This section lists both existing and proposed features, detailing their behavior in both modes to show how they save tokens.

| Feature / Command | Human Mode (Rich TUI) | AI Agent Mode (Token-Saving) | Token Savings |
| :--- | :--- | :--- | :--- |
| **Profile Load Banner** | Prints large ASCII logo (`▄████▄`) and welcome messages. | Completely silent. No banner or greeting printed. | ~300 tokens |
| **Project Selector (`prj`)** | Renders full interactive TUI menu (`TerminalMenu`) with borders. | Skips TUI. If no project name is passed, prints a clean, flat list of project paths (one per line). | ~500 tokens |
| **Terminal IDE (`ide`)** | Launches full split-pane text editor with tree sidebar and border lines. | Launches `micro` or `neovim` silently, or prints file tree as a flat list if requested by terminal scan. | ~1000+ tokens |
| **Account Manager (`acc`)** | Renders detailed table showing last used dates, folder sizes, and statuses. | Prints a clean CSV list: `Account,Status,UsageCount` with zero padding. | ~400 tokens |
| **System Monitor (`sysmon`)** | Renders real-time gauge bars using blocks (`████░░░`). | Prints single line key-values: `CPU:40.2%;RAM:60.5%;Disk:88%`. | ~250 tokens |
| **Git Selector (`co`)** | Interactive tree menu showing git branches to check out. | Outputs a plain text list of local branches. | ~200 tokens |
| **Port Inspector (`killport`)** | Multi-line warning dialog and prompt options. | Prints raw data: `PID:14210;Process:node;Port:8080`. | ~150 tokens |

---

## 3. Specific Feature Specifications (Agent-Friendly Design)

### A. Token-Saving Project Navigator (`prj`)
*   **Human:** Runs `Enter-Project` and renders the list using `TerminalMenu.ps1` with styling.
*   **AI Agent:** If the agent runs `prj` without arguments to check available workspaces, instead of rendering a menu (which depends on keystrokes and loops indefinitely in a non-interactive shell), it prints a simple list of registered workspaces:
    ```text
    Powershell | C:\Users\...\Powershell
    finance-dashboard | C:\Users\...\finance-dashboard
    ```
    This allows the AI agent to immediately read and parse the list in one step.

### B. Isolated Codex CLI Execution
*   When running local Codex CLI sessions, pointing `$env:CODEX_HOME` to a clean sandbox directory disables global skills and MCP. 
*   **Token Savings:** Prevents Codex from dumping large numbers of registered tools and helper descriptions into the prompt context on startup, saving **2,000+ tokens** per invocation.

### C. Compact Quota Reporting
*   **Human:** Renders progress bars, limit groups, and countdown timers.
*   **AI Agent:** Renders a compact summary.
    *   *Human Output:*
        ```text
        GEMINI MODELS
          Weekly Limit
            [████████████████████████████████████████░░░░░░░░░░] 80.24%
            80% remaining · Refreshes in 142h 20m
        ```
    *   *AI Agent Output:*
        ```text
        Gemini: Weekly=80.24%, 5H=85.26%
        Claude: Weekly=100.00%, 5H=100.00%
        ```
        This removes the graphical block characters and spacing lines entirely.

---

## 4. Timeline & Verification Plan for AI Mode

1.  **Agent Simulation Test:**
    Create a script in the `Tests` directory that executes profile commands with `$env:AI_MODE = "true"`. Verify that the standard output contains no ANSI box drawings, ASCII logos, or interactive prompt blocks.
2.  **Token Budget Audit:**
    Measure the character length of output returned by commands under both modes to guarantee at least a **70% reduction in console output size** when in AI Agent mode.

---

## 5. Feature Implementation Roadmap & Sequencing
To manage development dependencies and complexity, features will be implemented in the following sequence:

1.  **Dual-Mode Terminal Profile:** Sets up the basic framework for human vs. AI detection and output formatting.
2.  **Terminal IDE & Control Center TUI:** Integrates the menu interface and terminal-based file tree editors.
3.  **Antigravity Multi-Account (Agy) Switcher & Quotas:** Standardizes credential toggles and request logs.
4.  **Local AI Integration:** Updates Ollama, Claude, Hermes, and Codex sandboxes.
5.  **Docker Dashboard TUI:** Implements interactive container list and action menus.
6.  **System Utilities:** Adds port inspector, fuzzy history search, and resource monitors.
7.  **Project Scaffolder:** Implements the `new-project` bootstrapping templates.
8.  **Git TUI Helpers:** Adds branch checkout and Conventional Commit builder wizard.
9.  **Code Style & Refactoring Assistant:** (Scheduled after all core features) Automates style formatting and monolithic file extraction.
10. **Unit Test Enhancement:** (Scheduled after Code Style/Refactoring) Expands Pester test coverages across all features.

---

## 6. Tasks
- [x] Scan and catalog all planned CLI commands and TUI features.
- [x] Formulate human-vs-agent requirements table.
- [x] Establish standard token budget target rules (e.g. 70% reduction in output size).
- [x] Build verification simulator script for AI Mode terminal environments.
- [x] Perform final audit measuring character count reductions across all features.
