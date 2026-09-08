# 🐙 Deep Technical Audit: `agygit` Git Cockpit & Fleet Engine (Go Engine)

> **Category**: Developer Suite Core Audit  
> **Target Subsystem**: `apps/agygit`  
> **Source Files**: [apps/agygit/main.go](../../apps/agygit/main.go) · [apps/agygit/internal/service/gitops/gitops.go](../../apps/agygit/internal/service/gitops/gitops.go) · [apps/agygit/internal/view/app.go](../../apps/agygit/internal/view/app.go)  
> **Comparison Baseline**: C# `GitClient.cs` (`AgyTui`)  
> **Windows UNC Reference**: `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\apps\agygit`

---

## 1. Executive Summary & Architecture

`agygit` manages multi-agent Git workflows, parallel repository fleets across developer workspaces, and isolated worktrees. Key capabilities include:
1. **Parallel Git Fleet Scanner**: Discovers repositories under `~/projects` and displays sync/dirty metrics in a unified dashboard.
2. **Multi-Agent Worktree Isolation**: Spawns isolated Git worktrees into `.worktrees/<branch>` to enable concurrent AI subagents to code on isolated branches without clobbering the main working tree.
3. **Visual Log & Graph**: ASCII Git commit tree visualizer with branch decoration and commit details.
4. **VCS Automation**: Commit, merge, squash-merge, rebase, cherry-pick, stash, branch, and push/pull automation.

```
apps/agygit/
├── go.mod                                   # Go 1.25.0, requires only x/term and x/sys
├── main.go                                  # CLI command router (15 subcommands) & TUI entry
└── internal/
    ├── model/
    │   └── git.go                           # RepoStatus, WorktreeInfo, CommitInfo structs
    ├── service/
    │   └── gitops/
    │       ├── gitops.go                    # Subprocess exec wrappers (593 LOC)
    │       └── gitops_test.go               # Temp repo unit & integration tests
    └── view/
        ├── app.go                           # 958-line monolithic ANSI terminal TUI
        └── view_test.go                     # Render smoke tests
```

---

## 2. Critical Performance & Code Defects

### 2.1 Catastrophic UI Loop Stutter: 10 Git Processes Per Keystroke
- **Location**: [apps/agygit/internal/view/app.go lines 86–92](../../apps/agygit/internal/view/app.go#L86-L92)
```go
if a.ActiveRepoPath != "" {
    a.cachedStatus, _ = gitops.GetRepoStatus(a.ActiveRepoPath)       // ⚠️ 6 child processes!
    a.cachedCommits, _ = gitops.GetLog(a.ActiveRepoPath, 40)         // ⚠️ 1 child process
    a.cachedGraph, _ = gitops.GetLogGraph(a.ActiveRepoPath, 35)      // ⚠️ 1 child process
    a.cachedBranches, a.activeBranch, _ = gitops.ListBranches(a.ActiveRepoPath) // ⚠️ 1 process
    a.cachedWorktrees, _ = gitops.ListWorktrees(a.ActiveRepoPath)    // ⚠️ 1 process
}
```
- **Defect**: This block was placed **outside** the `if a.needsReload` check.
- **Impact**: On **every single keypress** (navigating up/down, switching tabs, toggling modes), the TUI executes **10 git child processes synchronously**. On large repositories or WSL2 `/mnt/c/` 9P mounts, this causes a **300ms to 1,500ms lag per keystroke**, resulting in severe UI freezing.
- **Remediation**: Move lines 86–92 inside `if a.needsReload` and implement an in-memory 5-second TTL cache for repository status.

### 2.2 Unbounded Concurrency in Fleet Scanning
- **Location**: [apps/agygit/internal/service/gitops/gitops.go lines 65–82](../../apps/agygit/internal/service/gitops/gitops.go#L65-L82)
```go
for _, p := range paths {
    wg.Add(1)
    go func(repoPath string) {
        defer wg.Done()
        st, err := GetRepoStatus(repoPath) // Runs 6 git commands
        ...
    }(p)
}
wg.Wait()
```
- **Defect**: No worker pool or concurrency limiter.
- **Impact**: If a developer has 50 workspaces registered, `agygit` simultaneously spawns $50 \times 6 = 300$ concurrent `git` processes. This exhausts file descriptors, triggers CPU throttling, and can crash Docker or WSL2.
- **Remediation**: Use a worker pool limited to `runtime.NumCPU() * 2` concurrent goroutines using a buffered semaphore channel.

### 2.3 Broken Branch Creation in TUI
- **Location**: [apps/agygit/internal/view/app.go line 423](../../apps/agygit/internal/view/app.go#L423)
```go
_ = gitops.CheckoutBranch(a.ActiveRepoPath, br, false)
```
- **Defect**: The prompt tells the user *"Checkout or Create Branch"*, but `create` is hardcoded to `false`. Entering a new branch name fails silently without creating it (`git checkout <new>` errors because it does not exist).
- **Remediation**: Check if the branch exists in `a.cachedBranches`. If not, call `CheckoutBranch(repo, br, true)` (`git checkout -b <branch>`).

### 2.4 Non-Scrollable Diff Viewer
- **Location**: [apps/agygit/internal/view/app.go lines 434–456](../../apps/agygit/internal/view/app.go#L434-L456)
- **Defect**: Diff viewer runs `git show --stat --color=always <hash>`, dumps the output to terminal stdout, and blocks on `os.Stdin.Read(dummy[:])`.
- **Impact**: For diffs larger than the terminal viewport, lines scroll off-screen and cannot be scrolled back. Working tree uncommitted diffs cannot be viewed at all.

---

## 3. Comparison with Legacy C# System (`GitClient.cs`)

| Feature | Legacy C# `GitClient.cs` | Go `agygit` | Parity Status & Verdict |
| :--- | :--- | :--- | :--- |
| **Conventional Commit Wizard** | Type selector, scope, breaking changes, and **AI commit generator** querying local models on staged diffs. | Simple single-line `bufio.Scanner` string prompt; auto-stages all files. | ❌ **Major Gap in Go**: Lacks AI diff summarization and Conventional Commit enforcement. |
| **Commit Undo Stack** | `InvokeGitUndo()`: shows last commit preview with confirmation, executes `git reset --soft HEAD~1`. | Only unstage (`git reset`). | ❌ **Missing in Go**: No commit undo feature. |
| **Conflict Resolution** | `ShowConflictResolver()`: detects `diff --diff-filter=U`, displays red conflict table, provides 1-click **Accept Ours**, **Accept Theirs**, and `diff --cc`. | Prints `Merge error: exit status 1`. | ❌ **Major Gap in Go**: Zero conflict inspection or resolution tools. |
| **Stash Management** | Table listing stashes with actions: Save, Pop, Apply, Drop, Clear. | Hardcoded single auto-stash `z` and pop `Z`. | 🟡 **Partially Ported**. |
| **Worktree Isolation** | None (manual branches). | Native `.worktrees/<branch>` with 1-tap `agy` subagent launch. | 🟢 **Massive Go Win**: High-leverage multi-agent isolation. |

---

## 4. Prioritized Action Plan & Next Steps

1. **Immediate Performance Hotfix**:
   - Wrap lines 86–92 of `app.go` with `if a.needsReload` to immediately stop the 10 git process invocations on every keystroke.
   - Limit `ScanFleet` concurrency using a semaphore channel of size 8.
2. **Fix Branch Creation**:
   - Update `app.go:L423` to pass `create = true` when branch does not already exist.
3. **Port C# Conflict Resolver**:
   - Add a conflict resolution sub-screen that lists conflicting files with `Accept Ours` and `Accept Theirs` hotkeys.
4. **Add AI Conventional Commit Wizard**:
   - Inspect `git diff --cached` and offer an interactive menu with commit types (`feat`, `fix`, `chore`, `docs`) and auto-generated summaries.
5. **Interactive Viewport Pager**:
   - Implement an ANSI pager with `j`/`k` and `PageUp`/`PageDown` scrolling for viewing commit diffs and working tree diffs.
