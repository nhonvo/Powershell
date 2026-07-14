# 🛸 Consolidated Reference Guide: Legacy PowerShell Profile Logic

This document details all features, commands, aliases, and execution logic contained within the retired `.ps1` files in the `Profile/` environment. It serves as a blueprint for any future migration or feature parity implementation in the new C# DLL-backed profile.

---

## 📂 Core Environment Configurations (`Profile/Core/`)

### 1. `TerminalMenu.ps1` (TUI Selector Engine)
- **Interactive Selector (`[TerminalMenu]::Show`)**: Rendered in-place interactive menu using key interception (`[Console]::ReadKey($true)`).
- **Search-on-Type Filtering**: Dynamic item filtering matching user keystrokes in real-time.
- **Scrollable Pager (`[TerminalMenu]::ShowScrollableContent`)**: Handled text wrapping, custom viewport height, and keyboard pagination (`Up`/`Down` arrows, `PageUp`/`PageDown`, `Home`/`End`, and `Escape` to close).
- **Theme Color Extraction**: Dynamically extracted segment colors from the active Oh My Posh JSON theme configuration to automatically apply consistent hex highlight styles to the TUI.

### 2. `ProfileEnvironment.ps1` (Shell Environment & KeyHandlers)
- **PSReadLine Bindings**: Registered standard keystroke actions like `Tab` autocomplete, history search (`Up`/`Down`), and custom keystrokes.
- **Session Boot Diagnostics**: Read environment variables like `$env:AI_MODE` and `$env:TERM_PROGRAM` to toggle plain-text stream logs vs. rich TUI layouts.
- **Verbose Startup Splashes**: Output colorized loading logs or a clean single-line status splash depending on configuration.

### 3. `Aliases.ps1` (Command Routing Engine)
- **Control Center Palette (`cc`)**: Prompted a full-screen, categories-grouped Command Palette indexing all available commands in the environment.
- **Shortcut Bindings**: Centralized registration of short aliases mapping to static class helpers (e.g., `gs` -> git status, `proj` -> navigate workspace).
- **Tab Autocompletion Registers**: Registered native argument auto-completers for commands like `multigravity`.

### 4. `Projects.ps1` (Workspace Context Registry)
- **Workspace Schemas**: Maintained an array of workspace hashtables storing paths, short names, and `AssociatedAccount` details.
- **Workspace Groups / Pinning**: Grouped workspaces dynamically for batch operations.

### 5. `ProfileHelp.ps1` (Interactive Help Browser)
- **Help Menu**: Opened an interactive selection hierarchy listing command descriptions, syntax parameters, and help logs.

---

## 🛠️ Specialized Helpers (`Profile/Helpers/`)

### 6. `ProfileNavigator.ps1` (Workspace Hopper)
- **Command**: `proj <query>` (alias `p`)
- **Logic**: Performed case-insensitive regex searches on registered workspace directories.
- **Conflict Resolution**: If the query matched multiple workspaces, it opened a `TerminalMenu` for interactive selection. If it matched exactly one, it jumped immediately.

### 7. `SystemHelper.ps1` (Diagnostics & Monitor Tools)
- **Disk Usage Tracker (`Get-DiskSpace`)**: Printed disk partitions, free space ratios, and health statuses.
- **Public IP Resolver (`Get-PublicIP`)**: Fetched external IPv4 addresses via REST endpoints, catching networking fallbacks gracefully.
- **Port Terminator (`Kill-Port <number>`)**: Located the Process ID (PID) listening on the specified TCP port and forcefully terminated it (`Stop-Process -Force`).
- **Connection Summary (`Get-SshConnectionInfo`)**: Rendered local IPv4s, active Tailscale addresses, and active inbound SSH connections.

### 8. `SshHelper.ps1` (Secure Remote Access)
- **Command**: `ssh-info`
- **Headless Key Receiver**: Started a temporary listener to receive and authorize public keys for passwordless logins from mobile/Termux terminals.

### 9. `DotNetHelper.ps1` (.NET Build & Db Tools)
- **Deep Clean (`Remove-BinObj`)**: Recursively swept workspace folders and forcefully purged `bin/` and `obj/` compile artifacts.
- **Build / Test Runners**: Shortcut cmdlets `dbld` (`dotnet build`) and `dtst` (`dotnet test`) that execute in the context of the active dotnet workspace.
- **EF Core Migrations**: Wrappers to create migrations (`Add-Migration`) and push schema updates to databases.

### 10. `GitHelper.ps1` (Conventional Commit Wizard)
- **Commands**: `gs` (status summary), `gcmt` (conventional commits wizard)
- **Wizard Logic**: Prompted the user sequentially for:
  1. Commit Type (Select from: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`, `ci`).
  2. Scope (Optional text input).
  3. Short Description (Validated character count).
  4. Breaking Changes / Issues closed.
  - Automatically constructed and executed the `git commit -m` statement.
- **Undo Last Commit (`Invoke-GitUndo`)**: Soft-reset the last local commit while keeping the edited changes in the working directory.

### 11. `DockerHelper.ps1` (Container Management & Cleanup TUI)
- **Command**: `dkcl` (Docker Cleanup TUI dashboard)
- **Cleanup Dashboard Logic**: Opened a menu allowing selective cleanups:
  - Stop and remove all running containers.
  - Prune unused images and dangling layers.
  - Delete unused container volumes and networks.
- **Compose Shortcuts**: Commands `dcup` and `dcdown` to orchestrate multi-container Docker Compose environments.

### 12. `AwsHelper.ps1` (LocalStack Sandbox Diagnostic)
- **Command**: `aws-local`
- **Logic**: Checked emulated local S3 buckets, SQS queues, and running Lambda functions.

### 13. `AiHelper.ps1` (Ollama Local LLM Integrations)
- **Commands**: `claude`, `codex`, `openclaw`, `hermes`, `hermesd`
- **Daemon Manager**: Monitored port `11434` to ensure Ollama local service was active, starting it in the background if offline.
- **Local TUI Routers**: Configured configurations and environment flags before launching CLI agents (e.g., configuring Node runtime environment variables for Claude Code).

### 14. `AgyAccountManager.ps1` (Legacy Multi-Account Context Switcher)
- **Directory Isolation**: Handled credentials isolation under `$env:GEMINI_HOME = C:\Users\Public\.gemini_<name>`.
- **Self-Healing Junctions**: Verified and dynamically recreated directory junctions for `config` (skills) and `antigravity` (conversation transcript logs) folders.
- **Auto-Switching Hook**: Contextual hook checked active path changes against `Projects.ps1` configuration to automatically load the correct developer account when traversing workspaces.

### 15. `AgySecretVault.ps1` (DPAPI Secure Keyring)
- **Encrypted Stores**: Serialized secrets to file using native Windows Data Protection API (DPAPI) and secure string formats.

### 16. `DatabaseHelper.ps1` (SQLite Schema Viewer)
- **Command**: `db-tui`
- **Logic**: Read SQLite schemas and output database structure details in an interactive TUI window.

### 17. `ProjectScaffolder.ps1` (Boilerplate Creator)
- **Scaffolder Logic**: Prompted the user to select a template (Web API, Console app, React, Vite) and scaffolded folder directories and projects.

### 18. `LogHelper.ps1` (Multiplexed Log & Spin Engine)
- **Spinner Wrapper**: Wrapped long-running execution blocks with an in-place loading spinner.
