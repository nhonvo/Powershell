# AGYGIT & AGYPROJ Performance, Bug, and UX Fix Report

## Overview
This report documents the performance optimizations, bug fixes, and UX enhancements implemented across `agygit` and `agyproj`. All changes eliminate UI latencies, avoid excessive process spawning, provide robust Windows/WSL interoperability, and add essential developer features like soft undo and interactive search filtering.

---

## 1. AGYGIT Changes & Optimizations

### 1.1 Eliminated 10-Child-Process Navigation Lag
* **File:** [app.go](./apps/agygit/internal/view/app.go)
* **Problem:** In each iteration of the main TUI loop, lines 86-92 unconditionally executed 6 Git commands for `GetRepoStatus`, 1 for `GetLog`, 1 for `GetLogGraph`, 1 for `ListBranches`, and 1 for `ListWorktrees` (plus `GetStatusSummary` during rendering). On every arrow key navigation (`Up`/`Down`/`j`/`k`), the TUI stalled for hundreds of milliseconds while spawning over 10 child processes.
* **Fix:**
  - Gated the execution of Git status queries inside `if a.needsReload || a.cachedStatus == nil`.
  - Added `cachedSummary` to `App` struct to cache `GetStatusSummary`, avoiding duplicate Git calls during screen re-rendering.
  - Set `a.needsReload = false` only after loading cached state.
  - Set `a.needsReload = true` and reset `cachedStatus = nil` upon active repo changes, mutations (commit, stage, unstage, stash, checkout, undo), and manual refresh (`r`).
  - Arrow key navigation and tab browsing are now instantaneous.

### 1.2 Concurrency Throttling in `ScanFleet`
* **File:** [gitops.go](./apps/agygit/internal/service/gitops/gitops.go)
* **Problem:** `ScanFleet` spawned unbounded goroutines for every detected repository, which could launch over 300 Git processes concurrently and exhaust OS process limits / CPU resources.
* **Fix:**
  - Introduced a semaphore channel `sem := make(chan struct{}, 8)` to limit concurrency to at most 8 parallel workers.
  - Safely controls resource utilization while maintaining rapid fleet discovery.

### 1.3 Branch Checkout & Creation Fix
* **File:** [app.go](./apps/agygit/internal/view/app.go)
* **Problem:** When typing a new branch name in Tab 2 (`b` key), `gitops.CheckoutBranch` was always called with `create = false`, failing with an error if the branch did not already exist.
* **Fix:**
  - Checks if the entered branch exists in `a.cachedBranches`.
  - Passes `create = !exists`, ensuring `git checkout -b <branch>` runs when creating a new branch and regular `git checkout <branch>` runs when switching to an existing branch.
  - Also bound `Enter` on Tab 2 to directly checkout the highlighted branch.

### 1.4 Git Undo Feature (`git reset --soft HEAD~1`)
* **Files:**
  - [gitops.go](./apps/agygit/internal/service/gitops/gitops.go): Added `GitUndo(repoPath string) error` running `git reset --soft HEAD~1`.
  - [main.go](./apps/agygit/main.go): Exposed CLI subcommand `agygit undo`.
  - [app.go](./apps/agygit/internal/view/app.go): Added `U` hotkey in Tab 0 with an interactive confirmation prompt (`Undo last commit (git reset --soft HEAD~1)? Changes will remain staged. (y/N)`). Updated status footer and help screen.
  - [gitops_test.go](./apps/agygit/internal/service/gitops/gitops_test.go): Added `TestGitOps_GitUndo` unit test verifying soft reset and preservation of staged changes.

---

## 2. AGYPROJ Changes & Optimizations

### 2.1 Workspace Caching (Eliminated 1-3s Lag per Keystroke)
* **File:** [app.go](./apps/agyproj/internal/view/app.go)
* **Problem:** In every loop iteration, `a.Registry.ListRegistered()` was called, invoking `m.Detector.Analyze` for every project on disk and blocking the event loop for 1-3 seconds per keystroke.
* **Fix:**
  - Added `registeredCache []model.ProjectInfo` and `needsReload bool` to `App` struct.
  - Initialized `needsReload: true` in `NewApp`.
  - Only calls `a.Registry.ListRegistered()` when `a.needsReload || a.registeredCache == nil`, then clears `a.needsReload = false`.
  - Triggers reload only on mutations (add project, delete project, pin/unpin, set active, register from discovery) and manual refresh (`r`).
  - Typing and arrow key navigation are now completely lag-free.

### 2.2 Dynamic Windows User Profile Discovery
* **File:** [launcher.go](./apps/agyproj/internal/service/launcher/launcher.go)
* **Problem:** Line 35 contained a hardcoded path `/mnt/c/Users/TruongNhon/...`, which fails for any other user or system.
* **Fix:**
  - Implemented dynamic lookup using:
    1. `os.Getenv("LOCALAPPDATA")`
    2. `os.Getenv("USERPROFILE")`
    3. `cmd.exe /c echo %LOCALAPPDATA%`
    4. `cmd.exe /c echo %USERPROFILE%`
    5. Fallback glob `/mnt/c/Users/*/AppData/Local/Programs/Microsoft VS Code/bin/code`
  - Added `windowsToWslPath` supporting `wslpath -u` and drive-letter mapping fallback (`C:\...` -> `/mnt/c/...`).
  - Added unit test in [launcher_test.go](./apps/agyproj/internal/service/launcher/launcher_test.go).

### 2.3 Non-Exiting Shell Drop-Out (`t` Key)
* **File:** [app.go](./apps/agyproj/internal/view/app.go)
* **Problem:** Line 263 executed `return a.Launcher.Launch("sh", sel.Path)`, which terminated `Run()` and dropped out of the application entirely.
* **Fix:**
  - Restores terminal cooked mode with `term.Restore(fd, oldState)`.
  - Runs the shell synchronously with `a.Launcher.Launch("sh", sel.Path)`.
  - Upon return from shell, re-enters raw terminal mode `oldState, _ = term.MakeRaw(fd)`.
  - Sets `a.needsReload = true` and `a.tabSwitched = true` to refresh Git state and repaint the UI.

### 2.4 Interactive Search Filter (`/` Key)
* **File:** [app.go](./apps/agyproj/internal/view/app.go)
* **Implementation:**
  - Pressing `/` in Tab 0 enters interactive search mode (`inSearchMode = true`).
  - Real-time keystroke filtering by workspace `name`, `stack`, `git branch`, or `path`.
  - Pressing `Enter` locks the search filter and returns focus to workspace selection.
  - Pressing `Backspace` removes characters from the query.
  - Pressing `Esc` clears the filter and restores full workspace listing.
  - Added visual filter counters (e.g. `Filter: "go" (1/3 matches)`) in header and footer.
  - Added unit test `TestApp_SearchFilter` in [app_test.go](./apps/agyproj/internal/view/app_test.go).

---

## 3. Test Verification Results

### agygit Test Suite
Command: `cd apps/agygit && go test -count=1 -v ./...`
```
=== RUN   TestGitOps_IsGitRepo_And_GetStatus
--- PASS: TestGitOps_IsGitRepo_And_GetStatus (0.04s)
=== RUN   TestGitOps_Worktrees
--- PASS: TestGitOps_Worktrees (0.02s)
=== RUN   TestGitOps_Commit_And_Graph
--- PASS: TestGitOps_Commit_And_Graph (0.02s)
=== RUN   TestGitOps_Branch_Merge_Squash
--- PASS: TestGitOps_Branch_Merge_Squash (0.02s)
=== RUN   TestGitOps_CherryPick
--- PASS: TestGitOps_CherryPick (0.02s)
=== RUN   TestGitOps_GitUndo
--- PASS: TestGitOps_GitUndo (0.02s)
PASS
ok  	agygit/internal/service/gitops	0.151s
=== RUN   TestApp_PrintStatus
--- PASS: TestApp_PrintStatus (0.08s)
=== RUN   TestApp_Render
--- PASS: TestApp_Render (0.00s)
PASS
ok  	agygit/internal/view	0.084s
```

### agyproj Test Suite
Command: `cd apps/agyproj && go test -count=1 -v ./...`
```
=== RUN   TestDetector_Analyze
--- PASS: TestDetector_Analyze (0.00s)
PASS
ok  	agyproj/internal/service/detector	0.005s
=== RUN   TestLauncher_Launch
--- PASS: TestLauncher_Launch (0.08s)
=== RUN   TestLauncher_WindowsPathConversion
--- PASS: TestLauncher_WindowsPathConversion (0.06s)
PASS
ok  	agyproj/internal/service/launcher	0.140s
=== RUN   TestRegistry_CRUD
--- PASS: TestRegistry_CRUD (0.00s)
=== RUN   TestRegistry_ScanDirectory
--- PASS: TestRegistry_ScanDirectory (0.00s)
PASS
ok  	agyproj/internal/service/registry	0.008s
=== RUN   TestApp_Render
--- PASS: TestApp_Render (0.00s)
=== RUN   TestApp_SearchFilter
--- PASS: TestApp_SearchFilter (0.00s)
PASS
ok  	agyproj/internal/view	0.004s
```

### Binary Compilation
Both `agygit` and `agyproj` compile cleanly:
```bash
(cd apps/agygit && go build -o /dev/null .) && (cd apps/agyproj && go build -o /dev/null .)
# Exited 0 with no errors
```

---

## 4. Compliance with Critical Rules
- **agyswitch Untouched:** Zero modifications made to `agyswitch` accounts, tokens, credentials, or config files.
- **Dual Link Reference:**
  - **VS Code Clickable:** [AGYGIT_AGYPROJ_PERF_UX_REPORT.md](./AGYGIT_AGYPROJ_PERF_UX_REPORT.md)
  - **Windows UNC Path:** `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\AGYGIT_AGYPROJ_PERF_UX_REPORT.md`
