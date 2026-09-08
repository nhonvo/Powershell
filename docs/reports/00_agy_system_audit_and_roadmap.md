# 🛸 Antigravity Developer Suite: Comprehensive Audit & Strategic Roadmap

> **Category**: System Architecture, Code Audit & Product Roadmap  
> **Environment**: Ubuntu WSL2 + Windows 11 / VS Code  
> **Date**: September 2026  
> **Audited Applications**: `agyswitch`, `agyproj`, `agygit`, `agydocker`, `agyterm`, `agyx`, `agymobile`  
> **Comparative Baseline**: Legacy C# Control Center (`apps/agytui/AgyTui` & `AgyTui.Tests`)  
> **Deliverable Index**: [Detailed Reports Catalog](#7-index-of-specialized-audit-reports)  
> **Windows UNC Reference**: `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\AGY_SYSTEM_AUDIT_AND_ROADMAP.md`

---

## 1. Executive Summary

A comprehensive, multi-agent forensic audit was conducted across the entire Antigravity developer ecosystem. The system recently transitioned from a monolithic .NET 9.0 C# console application (`apps/agytui/AgyTui`) to a modular Go micro-tool engine (`apps/agyswitch`, `apps/agyproj`, `apps/agygit`, `apps/agydocker`, `apps/agyterm`, `apps/agyx`, `apps/agymobile`).

### Key Audit Findings at a Glance:
1. **Performance Transformation (Success)**:
   - Startup cold-start dropped from **150ms–300ms (.NET CLR/JIT)** to **8ms–18ms (compiled native Go)**.
   - Memory footprint dropped from **45MB–85MB** to **6MB–14MB** per session.
   - Elimination of the PowerShell profile inline build stall (**2,000ms–5,000ms delay**) and Win32 DLL file-locking hazards.
2. **UI Framework Reality Check**:
   - **Zero Charm Bubbletea / Lipgloss**: Despite earlier architectural assumptions, **none** of the Go apps use Charm Bubbletea or Lipgloss. All 7 apps use **hand-rolled, raw ANSI VT100 terminal event loops** on top of `golang.org/x/term` and `golang.org/x/sys`.
   - **Synchronous Subprocess UI Freezing**: Because raw terminal loops execute blocking subprocess calls directly on the UI event thread:
     - `agygit` synchronously spawns **10 git child processes on every keystroke** outside reload guards (300ms–1500ms lag).
     - `agyproj` runs a full file scan, `git status`, and session cost calculation across **all registered projects on every single frame/keystroke** (1000ms–3000ms lag on WSL2 `/mnt/c/`).
3. **Severe Security Hazard in `agymobile`**:
   - Binds to `0.0.0.0:7890` across all public and Wi-Fi network interfaces with **zero authentication**, exposing an unauthenticated destructive endpoint (`POST /api/action/docker-stop-all`) that allows any device on the network to kill host containers.
4. **Architectural & Code Defects Discovered**:
   - **Empty Directories**: [apps/agyswitch/internal/service/quota](./apps/agyswitch/internal/service/quota) and [apps/agyterm/internal/service/linuxterm](./apps/agyterm/internal/service/linuxterm) are completely empty (0 files).
   - **Brittle Python Subprocess**: `agyswitch/sessions.go` invokes `exec.Command("python3", ...)` to query SQLite transcripts.
   - **Hardcoded Path & Username**: `agyproj/launcher.go` hardcodes the author's Windows username (`/mnt/c/Users/TruongNhon/...`).
   - **Control Flow Exit Bugs**: In `agyproj`, pressing `t` terminates the TUI; in `agyx`, child process exit codes are erased (`os.Exit(1)`); in `agygit`, branch creation has `create=false` hardcoded.
   - **Volume Prune Bug**: `agydocker` executes `docker system prune -f` instead of `docker volume prune -f`.
5. **Major Parity Regressions vs. C#**:
   - **SuperMemo-2 (SM-2) Spaced Repetition Suite**: Entirely omitted from the Go engine (Kana, Kanji, JLPT, English vocab, C# .NET 9, DSA, STAR behavioral interview bank).
   - **Git Advanced Tools**: Omitted AI Conventional Commit Generator, Git Undo stack (`reset --soft HEAD~1`), and visual Conflict Resolution Assistant (`ShowConflictResolver`).
   - **Unified Relational Database**: Replaced C#'s SQLite WAL engine (migrations V1–V7) with disconnected flat JSON files.
   - **AWS LocalStack & DotNet Tools**: S3, SQS, DynamoDB, Lambda, and `dclean` tools were completely dropped.

---

## 2. Comparative Matrix: Go Engine Suite vs. Legacy C# System

| Architectural & Functional Dimension | Legacy C# System (`AgyTui`) | Go Engine Suite (7 Apps) | Status & Parity Evaluation |
| :--- | :--- | :--- | :--- |
| **Startup Latency** | 150ms–300ms (CLR tiered JIT) | **8ms–18ms** (native static binary) | 🟢 **Major Go Victory** |
| **Terminal Integration** | Monolithic in-process DLL load via PowerShell (`[CommandRouter]::Route`) | Standalone micro-binaries aliased in Zsh/PS (`agys`, `agyg`, `agyd`, `agyp`, `agyt`, `agyx`) | 🟢 **Major Go Victory** |
| **Memory Footprint** | 45MB–85MB RAM per open terminal | **6MB–14MB RAM** per execution | 🟢 **Major Go Victory** |
| **Persistence Model** | Unified SQLite (`agytui.db`) with WAL, foreign keys, migrations V1–V7 | Disjointed flat JSON files (`projects.json`, `active_account.txt`) | ❌ **Major Regression in Go**: Prone to write-races, no transactions. |
| **Credential Security** | Windows DPAPI (`CurrentUser`) + Win32 `advapi32.dll` P/Invoke Credential Manager | AES-256-GCM with static PBKDF2 salt string | ⚠️ **Degraded in Go**: No DPAPI or native Credential Manager. |
| **OAuth Token Refresh** | External CLI invocation | Native Google OAuth HTTP refresh in Go | 🟢 **Go Victory** |
| **Live Quota Probing** | Local calculations from activity log | Real-time live Google CloudCode API (`retrieveUserQuotaSummary`) | 🟢 **Major Go Victory** |
| **Automatic Quota Failover** | `AutoSwitchOnQuotaExceeded()` switches on 429 | Manual account switch or CLI `launch-quota` | ❌ **Missing in Go**: No runtime auto-failover during agent execution. |
| **Multi-Agent Git Worktrees** | None (manual branches) | Native `.worktrees/<branch>` with 1-tap `agy` subagent launch | 🟢 **Major Go Victory** |
| **Git Conflict Resolver** | Ours/Theirs visual resolution table (`diff --cc`) | Raw CLI error dump | ❌ **Missing in Go**: No conflict resolution wizard. |
| **AI Commit Wizard** | AI draft generation from `git diff --cached` | Basic string prompt | ❌ **Missing in Go**: No AI commit summarization. |
| **Git Undo Stack** | `InvokeGitUndo()` (`reset --soft HEAD~1`) | Unstage only | ❌ **Missing in Go**: No commit undo feature. |
| **WSL2 RAM & Swap Guard** | None | Real-time `/proc/meminfo` bar gauges & `drop_caches` | 🟢 **Major Go Victory** |
| **Mobile Remote Cockpit** | None | 38-column portrait TUI & embedded Web Cockpit on `:7890` | 🟢 **Major Go Victory** |
| **Spaced Repetition (SM-2)** | Full SM-2 engine & 6 curated curriculums | None | ❌ **100% Missing in Go** |
| **AWS LocalStack Cloud** | S3, SQS, DynamoDB, Lambda, SSM, SNS | None | ❌ **100% Missing in Go** |
| **DotNet SDK & EF Core** | `dclean`, migrations, solution, NuGet wizard | None | ❌ **100% Missing in Go** |
| **Obsidian Vault Bridge** | Markdown note sync & orphan note detection | None | ❌ **100% Missing in Go** |
| **Test Coverage & Quality** | 261 xUnit tests (Architecture rules, invariants) | ~10 package unit tests | ❌ **Major Gap in Go** |

---

## 3. Comprehensive Analysis of Performance, UI & Loading Behaviors

### 3.1 The "Bubbletea Myth" & Reality of Raw ANSI Terminal Loops
During the migration, it was conceptualized that the Go applications would run on Charm Bubbletea (`tea.Model`) and Lipgloss.
- **The Reality**: None of the 7 Go applications import `github.com/charmbracelet/bubbletea` or `github.com/charmbracelet/lipgloss`.
- **The Architecture**: Every app implements an immediate-mode raw terminal loop:
  1. `term.MakeRaw(fd)` to disable canonical input line buffering.
  2. `\033[?1049h\033[?25l` to enter the Alternate Screen Buffer and hide the cursor.
  3. `for { os.Stdin.Read(buf) ... Render() }` loop parsing raw escape sequences (`[A` Up, `[B` Down, `[C` Right, `[D` Left).
  4. Deferred `\033[?25h\033[?1049l` and `term.Restore()` on shutdown.

### 3.2 UI Freezing & Event Loop Latency Root Causes
Because there is no Elm-style background command scheduler (`tea.Cmd`), operations that execute blocking I/O run directly on the UI thread:

```
[Keystroke: Arrow Down]
       │
       ▼
[agygit Main Loop] ──► GetRepoStatus()  (6 git child processes)  ──┐
                   ──► GetLog()         (1 git child process)    ──┼─► 10 Synchronous Process
                   ──► GetLogGraph()    (1 git child process)    ──┤   Spawns: 300ms–1500ms Freeze!
                   ──► ListBranches()   (1 git child process)    ──┤
                   ──► ListWorktrees()  (1 git child process)    ──┘
```

1. **`agygit`**: In [apps/agygit/internal/view/app.go lines 86–92](./apps/agygit/internal/view/app.go#L86-L92), status and log fetching occurs **outside** the `if a.needsReload` guard. Every single arrow keypress launches 10 child processes.
2. **`agyproj`**: In [apps/agyproj/internal/view/app.go line 78](./apps/agyproj/internal/view/app.go#L78), `registeredList := a.Registry.ListRegistered()` executes inside the render loop without caching. Every keypress re-runs file scanners, `git status`, and transcript analyzers across all registered workspaces.
3. **`agyswitch`**: Pressing `R` (refresh live quotas) blocks the UI thread until Google CloudCode APIs respond for all accounts.

### 3.3 Loading States, Spinners & Flicker Management
- **Screen Double-Buffering**: The Go apps successfully prevent screen tearing by rendering entire frames into pre-allocated `strings.Builder` buffers (4096 bytes) and emitting a single `os.Stdout.WriteString()` starting at home `\033[H`.
- **Spinner Implementation**: Only `agydocker` implements an animated spinner (`⠋ ⠙ ⠹ ⠸ ⠼ ⠴ ⠦ ⠧ ⠇ ⠏`), driven by non-blocking `unix.Poll` with a 150ms timeout. `agyswitch`, `agygit`, and `agyproj` lack animated spinners and appear frozen during long operations.
- **Window Resizing (`SIGWINCH`)**: None of the Go apps listen for `syscall.SIGWINCH`. Terminal resize events are ignored until the user presses a key.

---

## 4. Itemized Code Defects & Vulnerabilities

```
┌────────────────────────────────────────────────────────────────────────────┐
│                    CRITICAL DEFECTS & ACTION ITEMS                         │
├─────────────────┬───────────────────────────────────┬──────────────────────┤
│ Application     │ Code Defect / Vulnerability       │ Impact Severity      │
├─────────────────┼───────────────────────────────────┼──────────────────────┤
│ agymobile       │ Unauthenticated 0.0.0.0:7890      │ 🚨 CRITICAL SECURITY │
│ agymobile       │ Destructive POST /docker-stop-all │ 🚨 CRITICAL SECURITY │
│ agygit          │ 10 git commands per keypress      │ 🔴 HIGH PERFORMANCE │
│ agyproj         │ Re-analyzing all repos per frame  │ 🔴 HIGH PERFORMANCE │
│ agyswitch       │ Empty internal/service/quota dir  │ 🟠 ARCHITECTURE      │
│ agyterm         │ Empty internal/service/linuxterm  │ 🟠 ARCHITECTURE      │
│ agyswitch       │ python3 subprocess in sessions.go │ 🟠 RELIABILITY       │
│ agyproj         │ Hardcoded username "TruongNhon"   │ 🟠 PORTABILITY       │
│ agygit          │ Hardcoded create=false on branch  │ 🟡 FUNCTIONAL BUG    │
│ agyproj         │ Shell 't' terminates application  │ 🟡 FUNCTIONAL BUG    │
│ agydocker       │ System prune instead of volume    │ 🟡 FUNCTIONAL BUG    │
│ agyx            │ Erased child exit codes (Exit(1)) │ 🟡 FUNCTIONAL BUG    │
│ agyx            │ agymobile omitted from registry   │ 🟡 FUNCTIONAL BUG    │
│ agyswitch       │ Hardcoded MCP mock health status  │ 🟡 ACCURACY          │
└─────────────────┴───────────────────────────────────┴──────────────────────┘
```

---

## 5. Strategic Engineering Roadmap

```
  Phase 1 (Immediate)      Phase 2 (Short-Term)       Phase 3 (Medium-Term)      Phase 4 (Long-Term)
┌──────────────────────┐  ┌───────────────────────┐  ┌───────────────────────┐  ┌───────────────────┐
│ • Security Lockdown  │  │ • 5s In-Memory TTL    │  │ • Port SM-2 Learning  │  │ • Bubbletea /     │
│   (Tailscale IP auth)│  │   Caches (Git/Proj)   │  │   as 'agylearn'       │  │   Lipgloss TUI    │
│ • Fix 10-Process Lag │  │ • Semaphore Worker    │  │ • Port Git Conflict   │  │ • Unified Unix    │
│ • Fix Branch Create  │  │   Pools for Fleet     │  │   Resolver & AI Commit│  │   Domain Socket   │
│ • Fix Volume Prune   │  │ • Terminal SIGWINCH   │  │ • Pure-Go SQLite      │  │   IPC Daemon      │
│ • Fix Hardcoded Paths│  │   Resize Handlers     │  │   (modernc.org/sqlite)│  │ • Mobile WebSocket│
│ • Register agymobile │  │ • Animated Spinners   │  │ • Auto Quota Failover │  │   PTY Terminal    │
└──────────────────────┘  └───────────────────────┘  └───────────────────────┘  └───────────────────┘
```

### Phase 1: Security Lockdown & Critical Bug Fixes (Immediate Priority)
1. **`agymobile` Security Lockdown**:
   - Bind HTTP server strictly to Tailscale IP: `addr := fmt.Sprintf("%s:%d", tsInfo.IPv4, port)`.
   - Implement bearer token validation via `~/.config/antigravity/mobile_token.secret`.
2. **Eliminate `agygit` Keystroke Freeze**:
   - Move lines 86–92 of `agygit/internal/view/app.go` inside `if a.needsReload`.
3. **Eliminate `agyproj` Frame-Render Freeze**:
   - Cache `registeredList` in `App` state; only re-analyze on manual refresh (`s`) or mutation.
4. **Fix Broken Branch Creation in `agygit`**:
   - Check if branch exists; pass `create = true` (`git checkout -b`) if new.
5. **Fix `agydocker` Volume Pruning**:
   - Change Tab 2 `P` key to execute `docker volume prune -f`.
6. **Fix `agyproj` Username Hardcoding & Shell Drop-Out**:
   - Resolve `$USERPROFILE` dynamically; resume TUI upon subshell exit.
7. **Fix `agyx` Proxy Registry & Exit Codes**:
   - Register `agymobile` in `proxy.go` and Cockpit tabs; preserve child `*exec.ExitError` codes.

### Phase 2: Performance, Caching & UX Modernization (Short-Term Priority)
1. **Thread-Safe In-Memory TTL Caching**:
   - Implement a 5-second TTL cache for `RepoStatus` in `agygit` and `ProjectInfo` in `agyproj`.
2. **Worker Pool Concurrency Throttling**:
   - Replace unbounded goroutines in `agygit.ScanFleet()` with a bounded worker pool (`runtime.NumCPU() * 2`).
3. **Interactive Fuzzy Search in `agyproj`**:
   - Bind `/` in `agyproj` to filter registered workspaces in real-time.
4. **Asynchronous Non-Blocking Quota Refresh**:
   - Run `agyswitch` quota refreshes in background goroutines with animated ANSI spinners.
5. **Terminal Window Resize Signals**:
   - Register `syscall.SIGWINCH` signal listeners across all 7 TUIs for immediate re-rendering on window resize.

### Phase 3: Core Feature Parity with Legacy C# (Medium-Term Priority)
1. **Port Spaced Repetition Suite as `agylearn`**:
   - Create a dedicated Go application `apps/agylearn` hosting the SM-2 algorithm, flashcard flip UI, and pre-seeded curriculums (C#, Japanese, DSA, STAR interview bank).
2. **Port Advanced Git Tools to `agygit`**:
   - **Git Conflict Resolver**: Visual ours/theirs resolution table for `diff --diff-filter=U`.
   - **AI Conventional Commit Wizard**: Read `git diff --cached` and query local models.
   - **Git Undo Stack**: `git reset --soft HEAD~1` with commit preview.
3. **Adopt Pure-Go Centralized SQLite**:
   - Integrate `modernc.org/sqlite` (pure Go, zero CGO) to replace flat JSON files with a unified relational database, restoring schema migrations and transactional safety.
4. **Automated Runtime Quota Failover**:
   - Intercept `429 Rate Limit` exit codes from `agy`, automatically switch to the account with highest quota (`SelectBestQuotaAccount()`), and prompt to resume.

### Phase 4: Long-Term Architecture Evolution
1. **Charm Bubbletea & Lipgloss Migration**:
   - Refactor raw ANSI loops to `tea.Model` for standardized async I/O (`tea.Cmd`) and responsive viewports.
2. **Unified `agyx` IPC Daemon**:
   - Establish a local Unix Domain Socket (`~/.local/state/agyx/daemon.sock`) for instant telemetry sharing across CLI tools.
3. **Mobile Web PTY Terminal in `agymobile`**:
   - Back `/ws/terminal` with `creack/pty` and xterm.js to enable full browser terminal execution from mobile devices.

---

## 6. Proposed New Features for the Antigravity Suite

1. **`agyswitch`: Automatic Quota Failover Interceptor**:
   A wrapper command `agyswitch run <cmd>` that executes `agy`. If `agy` exits due to model rate limits, it automatically hot-swaps to the next available account and re-launches seamlessly.
2. **`agyproj`: Multi-Repo Workspace Bundles**:
   Allow grouping multiple repositories under a single unified "Project Bundle" (e.g. `frontend` + `backend` + `infra`), enabling 1-tap IDE launch of multi-root workspaces.
3. **`agydocker`: WSL2 Memory Auto-Compactor**:
   Background watchdog that automatically invokes memory compaction when `/proc/meminfo` reports $>85\%$ RAM pressure.
4. **`agymobile`: 2-Way Voice/Text Prompt Queue**:
   A mobile PWA card allowing developers to speak or type steering prompts directly into active Antigravity subagent sessions over Tailscale.
5. **`agylearn`: Standalone Developer Mastery TUI**:
   A standalone Go binary bringing back the complete SM-2 learning suite with Obsidian vault markdown synchronization.

---

## 7. Index of Specialized Audit Reports

For granular, line-by-line technical analyses, consult the dedicated reports in `./docs/reports/`:

| Report File | Topic & Scope | Windows UNC Path |
| :--- | :--- | :--- |
| **[01_agyswitch_deep_audit.md](./docs/reports/01_agyswitch_deep_audit.md)** | Quota engine, AES vault, Python subprocess, MCP mock | `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\docs\reports\01_agyswitch_deep_audit.md` |
| **[02_agyx_proxy_audit.md](./docs/reports/02_agyx_proxy_audit.md)** | Master orchestrator, PTY handover, exit code bug, IPC | `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\docs\reports\02_agyx_proxy_audit.md` |
| **[03_agygit_fleet_audit.md](./docs/reports/03_agygit_fleet_audit.md)** | Multi-agent fleet, 10-process lag, branch bug, diffs | `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\docs\reports\03_agygit_fleet_audit.md` |
| **[04_agyproj_workspace_audit.md](./docs/reports/04_agyproj_workspace_audit.md)** | Stack detector, frame lag, hardcoded user, fuzzy search | `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\docs\reports\04_agyproj_workspace_audit.md` |
| **[05_agydocker_container_audit.md](./docs/reports/05_agydocker_container_audit.md)** | Compose grouping, WSL2 RAM guard, volume prune bug | `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\docs\reports\05_agydocker_container_audit.md` |
| **[06_agyterm_theme_audit.md](./docs/reports/06_agyterm_theme_audit.md)** | WinTerm JSON editor, Oh-My-Posh previews, empty linuxterm | `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\docs\reports\06_agyterm_theme_audit.md` |
| **[07_agymobile_cockpit_audit.md](./docs/reports/07_agymobile_cockpit_audit.md)** | 38-col TUI, security holes, missing SSE/PTY/QR | `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\docs\reports\07_agymobile_cockpit_audit.md` |
| **[08_cs_legacy_system_parity_audit.md](./docs/reports/08_cs_legacy_system_parity_audit.md)** | C# Clean Architecture, SQLite V1-V7, SM-2, parity gaps | `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\docs\reports\08_cs_legacy_system_parity_audit.md` |
