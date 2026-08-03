# 🌿 Git Command Architecture & Dual-Tier Enhancement Pattern

## 1. Executive Summary & Design Blueprint

The **Dual-Tier CLI / TUI Architecture** separates standard CLI commands from rich interactive TUI views. 

```
                               ┌─────────────────────────────────────────┐
                               │           PowerShell Profile            │
                               │  (Microsoft.PowerShell_profile.ps1)     │
                               └────────────────────┬────────────────────┘
                                                    │
                      ┌─────────────────────────────┴─────────────────────────────┐
                      ▼                                                           ▼
         [Standard Native CLI Tier]                                  [Custom Spectre TUI Tier (✨)]
   Executes raw native CLI directly                             Delegates to AgyTui.dll via CommandRouter
      (Fast, pipeable, standard stdout)                            (Rich tables, interactive pickers, AI)
                      │                                                           │
                      ▼                                                           ▼
         `git status`, `git branch`, etc.                            `GitClient.cs` Spectre TUI Views
```

---

## 2. Naming & Routing Conventions

1. **Native CLI Command (`alias`)**:
   - Directly executes standard native `git` CLI with passed `@args`.
   - Standard output is piped cleanly to the shell console.
   - Example: `gs` $\rightarrow$ `git status @args`, `gb` $\rightarrow$ `git branch @args`.

2. **Custom Spectre TUI Command (`alias + u` / `✨`)**:
   - Marked with **`✨`** in menus and command palette displays.
   - Delegates to `AgyTui.dll` via `[CommandRouter]::Route("alias")`.
   - Renders interactive Spectre.Console UI elements (tables, pickers, commit wizards, AI diff drafting).
   - Example: `gsu` $\rightarrow$ `✨ Git Status (Custom TUI Table)`, `gbr` $\rightarrow$ `✨ Git Branch Manager`.

---

## 3. Git Command Alignment Matrix

| Native Command (CLI) | Execution Action | Custom TUI Command (`✨`) | TUI Feature & Behavior |
| :--- | :--- | :--- | :--- |
| **`gs`** | `git status @args` | **`gsu`** / **`gsi`** | **`✨ Git Status Table`**: Color-coded Spectre table for modified, staged, untracked files. |
| **`gb`** | `git branch @args` | **`gbr`** / **`gbu`** | **`✨ Git Branch Manager`**: Interactive TUI selector listing branches sorted by commit date. |
| **`gcommit`** | `git commit -m "..."` | **`gcmt`** | **`✨ Conventional Commit`**: Interactive prompt wizard + local AI diff drafting. |
| **`glo`** / **`glg`** | `git log --graph` | **`glou`** | **`✨ Git Commit Log`**: Interactive scrollable Spectre Pager log viewer. |
| **`gd`** | `git diff @args` | **`gdu`** | **`✨ Git Diff Viewer`**: Interactive full-screen Spectre diff viewer. |
| **`gr`** | `git reset --soft HEAD~1` | **`git-undo`** | **`✨ Git Undo Last Commit`**: Confirmation-guarded soft reset with preview. |
| **`gclone`** | `git clone <url>` | **`gcloneu`** | **`✨ Clone Project`**: Interactive URL prompt with auto-destination resolution in `~/Documents`. |
| N/A | Native CLI | **`nexus`** | **`✨ Repo Nexus Graph`**: Workspace multi-repository dashboard. |
| N/A | Native CLI | **`repo-graph`** | **`✨ Repository Dependency Graph`**: Visual inter-project tree links. |
| N/A | Native CLI | **`nexus-stats`** | **`✨ Git Nexus Stats`**: Commit velocity, active authors, and code churn analytics. |

---

## 4. TUI Menu Tree Folder Mapping

All Git commands are grouped under **`📂 Git & Repo Tools`** in [CommandRegistry.cs](file:///C:/Users/TruongNhon/Documents/Powershell/csapp/AgyTui/UI/Core/Registries/CommandRegistry.cs):

```text
─ [-] 📂 Git & Repo Tools
     ├── 🌿 /gs — Git Status (Native)
     ├── 📄 /gsu — ✨ Git Status (Custom TUI Table)
     ├── 📄 /ga — Git Add All (Native)
     ├── 🌿 /gb — Git Branch (Native)
     ├── 🌿 /gbr — ✨ Git Branch Manager
     ├── 🌿 /co — Git Checkout (Native)
     ├── 🌿 /cob — New Git Branch (Native)
     ├── 🌿 /gbd — Delete Git Branch (Native)
     ├── 💬 /gcommit — Git Commit (Native)
     ├── 💬 /gcmt — ✨ Conventional Commit
     ├── 📜 /glo — Git Commit Log Graph (Native)
     ├── 📜 /glog — Git Commit Log Pretty (Native)
     ├── 📜 /glou — ✨ Git Commit Log Pager
     ├── ⬇ /gpull — Git Pull Remote (Native)
     ├── ⬆ /gpush — Git Push Remote (Native)
     ├── ⬆ /guf — Git Push Force (Native)
     ├── 📄 /gf — Git Fetch Remote (Native)
     ├── 📄 /gd — Git Diff (Native)
     ├── ↩ /gr — Git Reset Soft (Native)
     ├── ↩ /grh — Git Reset Hard (Native)
     ├── ↩ /git-undo — ✨ Git Undo Last Commit
     ├── 🚀 /gclone — Git Clone Project (Native)
     ├── 🚀 /gcloneu — ✨ Clone Project Assistant
     ├── 🕸 /nexus — ✨ Repo Nexus Graph
     ├── 🕸 /repo-graph — ✨ Repository dependency graph
     └── 🕸 /nexus-stats — ✨ Git Nexus commit stats
```
