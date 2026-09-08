# Non-Blocking UI & Animation Engineering Report
**Components:** `apps/agyswitch` and `apps/agydocker`  
**Engineer Role:** Non-Blocking UI & Animation Engineer  
**Status:** Complete & Verified  

---

## Document References & Dual Links
- **VS Code Clickable:** [AGYSWITCH_AGYDOCKER_NONBLOCKING_REPORT.md](./AGYSWITCH_AGYDOCKER_NONBLOCKING_REPORT.md)
- **Windows UNC Path:** `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\AGYSWITCH_AGYDOCKER_NONBLOCKING_REPORT.md`

---

## 1. Overview & Constraints Compliance

### 1.1 Strict Constraint Adherence
- **Zero modification** to `agyswitch` account management, token swapping, vault credentials, directory mirroring, or account persistence.
- Accounts remain 100% untouched and functional across all commands and UI screens.
- All modifications in `agyswitch` were strictly constrained to the terminal event loop, non-blocking polling, background quota probing goroutines, and animation rendering.

---

## 2. Changes in `apps/agyswitch`

### 2.1 Non-Blocking Polling Architecture
- Created [poll_linux.go](./apps/agyswitch/internal/view/poll_linux.go) using `golang.org/x/sys/unix` with `unix.Poll` to monitor file descriptor 0 (`os.Stdin`) with millisecond timeout precision.
- Created [poll_windows.go](./apps/agyswitch/internal/view/poll_windows.go) for Windows cross-compilation support.

### 2.2 Background Live Quota Probing (`'r'`, `'R'`)
- In [app.go](./apps/agyswitch/internal/view/app.go):
  - Added `probeMu sync.Mutex`, `isProbingQuotas bool`, and `spinnerIdx int` to struct `App`.
  - Replaced the blocking `os.Stdin.Read(buf[:])` call with non-blocking `ready := waitKey(fd, 80)`.
  - When `'r'` or `'R'` is pressed in Tab 0:
    - Launches a background goroutine calling `a.Store.ListAccounts()`.
    - Protects state transitions using `a.probeMu`.
    - UI never blocks or freezes: user can freely move cursor (`j`, `k`, `↑`, `↓`), switch tabs (`1-4`, `Tab`), inspect skills (`v`), inspect rules (`v`), or inspect session trajectories (`v`).
  - Animation & Event Loop:
    - If `waitKey` times out (80ms) and `isProbingQuotas` is active: advances `a.spinnerIdx = (a.spinnerIdx + 1) % len(spinnerFrames)` and triggers `a.Render(accs, sessionsList)`.
    - If user presses keys while probing: advances `spinnerIdx` so the animation keeps rotating smoothly even during fast cursor navigation.
    - Status bar displays animated badge: `[⠋] Probing live CloudCode quotas in background... (Navigate freely)`.
    - Header displays live sync badge: `[⠋ Live Quota Syncing]`.
    - Upon completion: background goroutine updates `a.cachedAccs`, resets `isProbingQuotas = false`, sets `needsReload = true`, and sets `StatusMsg = "\033[32m✔ Successfully updated live quotas in background.\033[0m"`.

### 2.3 Unit Testing & Verification
- Created [app_internal_test.go](./apps/agyswitch/internal/view/app_internal_test.go) verifying `waitKey`, probing state transitions, and animated spinner rendering.
- Test command: `cd apps/agyswitch && go test -v ./...` passed 100% cleanly.

---

## 3. Changes in `apps/agydocker`

### 3.1 Docker Stack & Container Teardown Engine
- In [dockerops.go](./apps/agydocker/internal/service/dockerops/dockerops.go):
  - Added `DownCompose(project string) error`: executes `docker compose -p <project> down`. Validates project name, rejecting empty or `"Standalone"` stacks.
  - Added `DownContainer(id string) error`: executes `docker stop -t 2 <id>` with a 2-second grace period, followed by force removal `docker rm -f <id>`.
- In [dockerops_test.go](./apps/agydocker/internal/service/dockerops/dockerops_test.go):
  - Added unit tests `TestDockerOps_DownCompose` and `TestDockerOps_DownContainer`.

### 3.2 Non-Blocking Event Loop & UI Animation
- In [app.go](./apps/agydocker/internal/view/app.go):
  - Added hotkey `'x'`, `'X'` for "Down Stack":
    - If compose project: triggers `dockerops.DownCompose(proj)` in a background goroutine with `a.setPendingAction(proj, "downing")` and sets `downing` badge on all associated containers.
    - If standalone container: triggers `dockerops.DownContainer(cid)` in a background goroutine with `a.setPendingAction(cid, "downing")`.
  - Smooth animation during rapid navigation:
    - In the event loop, whenever keys are pressed while actions are pending (`a.hasPendingActions() || a.isReloadingActive()`), `a.spinnerIdx = (a.spinnerIdx + 1) % len(spinnerFrames)` is incremented so the animation never stutters during keyboard navigation.
  - Background container reload (`reloadAsync`):
    - Replaced synchronous `docker ps` reloads on the main UI loop with thread-safe `reloadAsync()`, protected by `a.pendingMu` and `a.isReloading` flag.
    - Main event loop never blocks on `docker ps` or docker network calls.
    - Container lists are cloned under read lock (`RLock`), eliminating data races with background reloads.
  - UI Status & Badges:
    - Compose project header displays: `📁 <project> [⠋ DOWNING] (<count> containers)`.
    - Container status badges display: `⠋ [Down]`.
    - Footer updated to include `[X] Down Stack`.

### 3.3 CLI Subcommand
- In [main.go](./apps/agydocker/main.go):
  - Added subcommand `agydocker down <project-or-id>` which tears down compose project stacks or stops and force-removes individual containers.
  - Updated `printHelp()` documentation.

### 3.4 Unit Testing & Verification
- Updated [view_test.go](./apps/agydocker/internal/view/view_test.go) with `TestApp_DownActionAndAsyncReload`.
- Test command: `cd apps/agydocker && go test -v ./...` passed 100% cleanly.

---

## 4. Test Suite Execution Logs

### 4.1 `agyswitch` Test Output
```
=== RUN   TestApp_NonInteractivePrintStatus
--- PASS: TestApp_NonInteractivePrintStatus (0.01s)
=== RUN   TestApp_WaitKey
--- PASS: TestApp_WaitKey (0.00s)
=== RUN   TestApp_ProbingQuotasRendering
--- PASS: TestApp_ProbingQuotasRendering (0.01s)
=== RUN   TestApp_RenderTabs
--- PASS: TestApp_RenderTabs (0.00s)
PASS
ok      agyswitch/internal/view 0.023s
=== RUN   TestLauncher_CleanArgs
--- PASS: TestLauncher_CleanArgs (0.00s)
=== RUN   TestLauncher_FindAgyBinCandidates
--- PASS: TestLauncher_FindAgyBinCandidates (0.00s)
PASS
ok      agyswitch/launcher      0.008s
```

### 4.2 `agydocker` Test Output
```
=== RUN   TestDockerOps_GetMemoryInfo
--- PASS: TestDockerOps_GetMemoryInfo (0.00s)
=== RUN   TestDockerOps_ListContainers
--- PASS: TestDockerOps_ListContainers (0.02s)
=== RUN   TestDockerOps_PruneVolumes
--- PASS: TestDockerOps_PruneVolumes (0.02s)
=== RUN   TestDockerOps_KillContainer
--- PASS: TestDockerOps_KillContainer (0.02s)
=== RUN   TestDockerOps_RemoveContainer
--- PASS: TestDockerOps_RemoveContainer (0.02s)
=== RUN   TestDockerOps_DownCompose
--- PASS: TestDockerOps_DownCompose (0.08s)
=== RUN   TestDockerOps_DownContainer
--- PASS: TestDockerOps_DownContainer (0.02s)
PASS
ok      agydocker/internal/service/dockerops    0.207s
=== RUN   TestApp_PrintStatus
--- PASS: TestApp_PrintStatus (0.01s)
=== RUN   TestApp_Render
--- PASS: TestApp_Render (0.00s)
=== RUN   TestApp_PendingActions
--- PASS: TestApp_PendingActions (0.00s)
=== RUN   TestApp_DockerDaemonWarning
--- PASS: TestApp_DockerDaemonWarning (0.01s)
=== RUN   TestApp_DownActionAndAsyncReload
--- PASS: TestApp_DownActionAndAsyncReload (0.00s)
PASS
ok      agydocker/internal/view 0.040s
```

---

## 5. Verification Summary Table

| Requirement | Implementation File(s) | Status |
|---|---|---|
| `waitKey(fd, timeoutMs)` Linux (`unix.Poll`) | [poll_linux.go](./apps/agyswitch/internal/view/poll_linux.go) | Verified |
| `waitKey(fd, timeoutMs)` Windows | [poll_windows.go](./apps/agyswitch/internal/view/poll_windows.go) | Verified |
| Non-blocking quota probe on 'r' / 'R' | [app.go](./apps/agyswitch/internal/view/app.go) | Verified |
| Non-freezing cursor navigation during probe | [app.go](./apps/agyswitch/internal/view/app.go) | Verified |
| Spinner animation badge during probe | [app.go](./apps/agyswitch/internal/view/app.go) | Verified |
| Quota probe completion state & reload | [app.go](./apps/agyswitch/internal/view/app.go) | Verified |
| Account data/vault 100% untouched | N/A (zero diff to account files) | Verified |
| `DownCompose(project)` | [dockerops.go](./apps/agydocker/internal/service/dockerops/dockerops.go) | Verified |
| `DownContainer(id)` | [dockerops.go](./apps/agydocker/internal/service/dockerops/dockerops.go) | Verified |
| Hotkey 'x', 'X' for Down Stack | [app.go](./apps/agydocker/internal/view/app.go) | Verified |
| Smooth spinner animation during rapid nav | [app.go](./apps/agydocker/internal/view/app.go) | Verified |
| Async container reload (`reloadAsync`) | [app.go](./apps/agydocker/internal/view/app.go) | Verified |
| Free cursor/tab nav during container actions | [app.go](./apps/agydocker/internal/view/app.go) | Verified |
| CLI subcommand `down <project-or-id>` | [main.go](./apps/agydocker/main.go) | Verified |
| Full unit test suites passing | `go test -v ./...` in both modules | Verified (100% Pass) |
