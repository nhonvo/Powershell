# 📁 Deep Technical Audit: `agyproj` Project Hub & Workspace Engine (Go Engine)

> **Category**: Developer Suite Core Audit  
> **Target Subsystem**: `apps/agyproj`  
> **Source Files**: [apps/agyproj/main.go](../../apps/agyproj/main.go) · [apps/agyproj/internal/service/detector/detector.go](../../apps/agyproj/internal/service/detector/detector.go) · [apps/agyproj/internal/service/launcher/launcher.go](../../apps/agyproj/internal/service/launcher/launcher.go) · [apps/agyproj/internal/service/registry/registry.go](../../apps/agyproj/internal/service/registry/registry.go) · [apps/agyproj/internal/view/app.go](../../apps/agyproj/internal/view/app.go)  
> **Comparison Baseline**: C# `SqliteWorkspaceRepository.cs` & `WorkspaceAggregate.cs`  
> **Windows UNC Reference**: `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\apps\agyproj`

---

## 1. Executive Summary & Architecture

`agyproj` is the project workspace registry, technology stack detector, and IDE launcher for the Antigravity developer suite:
1. **Multi-Stack Auto-Detection**: Heuristically identifies `.NET / C#` (`.sln`, `.csproj`), `Go` (`go.mod`), `Rust` (`cargo.toml`), `Node/Next/React` (`package.json`), `Python` (`pyproject.toml`), and `Docker`.
2. **Antigravity AI Session Telemetry**: Parses Antigravity transcripts (`~/.gemini/antigravity-cli/brain/*/transcript.jsonl`) to compute session counts, total steps, and accrued USD inference costs per project.
3. **IDE Launcher**: Seamlessly launches VS Code (`code`), Cursor (`cursor`), Neovim (`nvim`), or Antigravity (`agy`) in project directories across WSL2 and Windows boundaries.
4. **Registry & Pinning**: Maintains workspace metadata and favorite project pins (`★`) in `~/.config/antigravity/projects.json`.

```
apps/agyproj/
├── go.mod                                   # Go 1.26.0, requires only x/term and x/sys
├── main.go                                  # CLI commands (ls, scan, add, rm, pin, cd, open, init)
└── internal/
    ├── model/
    │   └── project.go                       # ProjectInfo and RegistryConfig schemas
    ├── service/
    │   ├── detector/
    │   │   ├── detector.go                  # Tech stack, Git telemetry & transcript reader
    │   │   └── detector_test.go             # Unit tests
    │   ├── launcher/
    │   │   ├── launcher.go                  # Process launcher for VS Code, Cursor, Neovim
    │   │   └── launcher_test.go             # Mocked launcher tests
    │   └── registry/
    │       ├── registry.go                  # JSON storage (~/.config/antigravity/projects.json)
    │       └── registry_test.go             # CRUD and scanning tests
    └── view/
        ├── app.go                           # Raw ANSI terminal interface (667 LOC)
        └── app_test.go                      # View render tests
```

---

## 2. Critical Performance & Code Defects

### 2.1 Severe Performance Bottleneck: Re-Analyzing All Workspaces on Every Keystroke
- **Location**: [apps/agyproj/internal/view/app.go line 78](../../apps/agyproj/internal/view/app.go#L78) & [apps/agyproj/internal/service/registry/registry.go lines 183–196](../../apps/agyproj/internal/service/registry/registry.go#L183-L196)
```go
// Inside app.go main loop:
for {
    registeredList := a.Registry.ListRegistered() // ⚠️ Called on EVERY keypress!
    ...
}

// Inside registry.go ListRegistered():
for _, p := range cfg.Projects {
    live := m.Detector.Analyze(p.Path) // ⚠️ Performs full file scan, git status, and transcript read!
    ...
}
```
- **Defect**: On **every single keystroke** (scrolling up/down with arrow keys, toggling pins), `ListRegistered()` re-analyzes every registered workspace:
  1. Executes `git status --porcelain` on each project directory.
  2. Walks subdirectories 1 level deep for stack heuristics.
  3. Iterates over all Antigravity session transcripts in `~/.gemini/antigravity-cli/brain/*/`.
- **Impact**: On a system with 15–20 registered projects, especially with some located on `/mnt/c/` Windows mounts, each keystroke incurs a **1,000ms to 3,000ms latency**.
- **Remediation**: Cache `registeredList` in memory inside `App`. Only re-analyze projects on initial startup, explicit manual refresh (`s` key), or when a project is added/removed.

### 2.2 Hardcoded User Path in IDE Launcher
- **Location**: [apps/agyproj/internal/service/launcher/launcher.go lines 34–39](../../apps/agyproj/internal/service/launcher/launcher.go#L34-L39)
```go
case "code", "vscode":
    if _, err := exec.LookPath("code"); err != nil {
        cand := "/mnt/c/Users/TruongNhon/AppData/Local/Programs/Microsoft VS Code/bin/code"
        if fi, errStat := os.Stat(cand); errStat == nil && !fi.IsDir() {
            return l.Runner(cand, projectDir)
        }
    }
    return l.Runner("code", projectDir)
```
- **Defect**: Hardcodes the developer's specific Windows username (`TruongNhon`).
- **Impact**: On any machine with a different Windows username, this fallback fails if `code` is not directly on the WSL2 `$PATH`.
- **Remediation**: Resolve the Windows username dynamically via environment variable `$USERPROFILE` (or `cmd.exe /c "echo %USERPROFILE%"`).

### 2.3 Shell Drop-out Permanently Terminates Application
- **Location**: [apps/agyproj/internal/view/app.go line 263](../../apps/agyproj/internal/view/app.go#L263)
```go
case 't', 'T': // Open Terminal Shell in Tab 0
    ...
    fmt.Printf("\r\n\033[36m[agyproj]\033[0m Dropping into shell in '\033[32m%s\033[0m'...\r\n", sel.Path)
    return a.Launcher.Launch("sh", sel.Path) // ⚠️ TERMINATES TUI!
```
- **Defect**: Uses `return a.Launcher.Launch(...)`, which permanently exits `RunInteractive()`.
- **Impact**: When the user exits the subshell (`exit`), `agyproj` terminates rather than resuming the TUI session.
- **Remediation**: Temporarily restore terminal cooked mode (`term.Restore(fd, oldState)`), execute the shell synchronously, and upon exit re-enter raw mode (`term.MakeRaw(fd)`) and re-render.

### 2.4 Missing Interactive Fuzzy Search
- **Defect**: While CLI subcommands (`cd`, `open`) do a basic `strings.Contains()` check, the interactive TUI has **zero search or text filtering capabilities**. Users must scroll page-by-page through large project registries.

---

## 3. Comparison with Legacy C# System (`SqliteWorkspaceRepository`)

| Dimension | Legacy C# `AgyTui` | Go `agyproj` | Parity Status & Verdict |
| :--- | :--- | :--- | :--- |
| **Storage Engine** | SQLite database (`workspaces` table) with migrations + JSON fallback. | Single flat JSON file (`~/.config/antigravity/projects.json`). | 🟡 **Trade-off**: Go has simpler JSON; C# had transactional integrity and indexes. |
| **Data Attributes** | `AssociatedAccount`, `Tags`, `Alias`, `Links` (`[]WorkspaceLink`), `ParentPath`, `IsRoot`. | `ID`, `Name`, `Path`, `Stack`, `GitBranch`, `IsDirty`, `IsPinned`, `IsActive`, `Cost`. | 🟡 **Mixed**: Go adds AI cost tracking; **lacks workspace URLs/bookmarks, custom aliases, and tags**. |
| **Caching Layer** | `TtlCache` with 5-second TTL on workspace scans. | No caching in `ListRegistered()`. | ❌ **Major Regression in Go**: Heavy disk/git hammering on every keypress. |
| **Scan Depth** | Recursive auto-discovery up to **depth 3**. | Shallow single-level scan (`depth = 1`) of direct children under `~/projects`. | ⚠️ **Degraded in Go**: Cannot find nested workspaces (e.g. `~/projects/clients/app`). |

---

## 4. Prioritized Action Plan & Next Steps

1. **Immediate Performance Hotfix**:
   - Cache `registeredList` in `App` state; eliminate re-analyzing projects in the main event loop.
2. **Fix Launcher Username & Shell Drop-out**:
   - Dynamically resolve VS Code executable path from Windows environment variables.
   - Run subshells without terminating the TUI event loop.
3. **Add Interactive Live Search Filter**:
   - Bind `/` or `s` in the TUI to enter interactive search mode with real-time substring/fuzzy matching.
4. **Restore Project Bookmarks & Links (C# Parity)**:
   - Extend `ProjectInfo` schema with `Links []WorkspaceLink`, allowing developers to press `o` to launch associated URLs (Swagger, staging sites, docs) or open specific subfolders.
5. **Configurable Recursive Scan Depth**:
   - Add `--depth` flag to `agyproj scan` (defaulting to depth 2 or 3) to discover multi-repository monorepos.
