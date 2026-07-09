# Unified Profile Feature Roadmap & Rebuild Plan

This document consolidates and unifies the planned features and architectural updates from `feature_roadmap_decision.md`, `cc_rebuild_plan.md`, and all other plan documents inside the `docs/plan/` directory. All features are categorized under the 4 Big Flows and the main Command Center console loop, formatted using a standardized **Feature - User Story** structure.

---

## 1. Core Control Center Shell Loop (`cc`)

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

---

## 2. Workspace & Code Navigation Flow (Part 1)

### [Feature 2.1] Terminal-Based Sidebar IDE Integration
*   **Feature:** Integrate workspace navigator (`prj`/`proj`) to let developers hop between projects and instantly launch them directly into a terminal-based editor (Micro or Neovim) configured with a file sidebar tree explorer.
*   **User Story:** As a modal or terminal editor user, I want my workspace navigator to offer an action to open the chosen project inside Micro or Neovim, so that I can edit files and browse directory structures without leaving my console window.
*   **Implementation Tasks:**
    - [x] Map priority workspaces and directories dynamically in `Projects.ps1`.
    - [x] Configure Micro editor json settings to auto-install and bind the `filemanager` tree sidebar.
    - [x] Provide a launch script fallback hook to open Neovim with folders explorer support.

### [Feature 2.2] Interactive Workspace Search & Git Status Badges
*   **Feature:** Pull Git branch status summaries and modified file counts asynchronously or via non-blocking queries, rendering them as branch badges directly on project list rows.
*   **User Story:** As a developer hopping between workspace repositories, I want my project selection list to show branch names and pending changes next to each project row, so that I can prioritize work without checking git status manually.
*   **Implementation Tasks:**
    - [x] Check paths and resolve active project lists.
    - [x] Query Git status details for active branches.
    - [x] Append Git status descriptions to navigation labels.
    - [x] Add loading spinner animation to prevent screen freezes during directory scanning.

---

## 3. Multi-Account Management & Local AI Flow (Part 2)

### [Feature 3.1] Unified Account Command & Sandboxing (`acc`)
*   **Feature:** Consolidate keyring credentials and accounts switching under a single `acc` command that supports both interactive TUI toggles and temporary execution sandboxes.
*   **User Story:** As a developer using multiple workspace profiles, I want to dynamically change accounts or run specific scripts temporarily under another profile context, so that my credential settings remain isolated and clean.
*   **Implementation Tasks:**
    - [x] Implement DPAPI secret encryption and active token backup logic.
    - [x] Build the unified interactive account switcher dashboard.
    - [x] Implement the `-temp` switch parameter to run scriptblocks inside an isolated temporary directory sandbox.

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

### [Feature 4.2] System Resources Monitor & Socket Cleaner (`sysmon` / `killport`)
*   **Feature:** Integrate resource inspection meters with process blockers to resolve port collisions from the same screen.
*   **User Story:** As a web developer, I want to see system resource stats and terminate port-hogging processes instantly, so that I can resolve port collisions when launching local web servers.
*   **Implementation Tasks:**
    - [x] Create cpu and memory diagnostics gauges.
    - [x] Implement target port listener checkers and termination processes.
    - [x] Bind process killers directly inside the resource monitor dashboard.

---

## 5. Project Scaffolding & Version Control Flow (Part 4)

### [Feature 5.1] Template-Driven Scaffolding & CI Checks (`new-project`)
*   **Feature:** Bootstrap new projects using configurable layouts including predefined files like default Dockerfiles and database configs, combined with pre-commit CI script checks.
*   **User Story:** As a developer creating new microservices, I want to scaffold projects with prefilled Dockerfiles and configurations, so that my project setup matches local best practices.
*   **Implementation Tasks:**
    - [x] Create project template folders for .NET and frontend frameworks.
    - [x] Implement configuration injections and bootstrap scripts.
    - [x] Incorporate syntax checking and linting triggers on staged files.

### [Feature 5.2] Semantic Git Checker & AI Commit Prefills (`co` / `gcmt`)
*   **Feature:** Interactive Git branch checkouts, Conventional Commit formatting, stage verification checks, and local Ollama-based commit message suggestions.
*   **User Story:** As a developer staging commits, I want a Conventional Commit assistant that validates staged changes and generates semantic summaries using local models, so that my repository logs remain clean.
*   **Implementation Tasks:**
    - [x] Build branch selector andConventional Commit wizards.
    - [x] Block commit helpers if no staged files exist.
    - [x] Wire dynamic tab-completion triggers to send diff contexts to Ollama models.
