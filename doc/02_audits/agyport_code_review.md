# AGYPORT Code & Architecture Review Report

**Date:** October 5, 2026  
**Application:** `agyport` (Port Manager & RAM Optimizer Go Engine)  
**Binary Location:** [dist/windows/agyport.exe](./dist/windows/agyport.exe)  
**Web Engine:** `http://127.0.0.1:5999/`

---

## 1. Executive Summary

This code review and system audit addresses critical process control issues on Windows, corrects TCP `netstat` state parsing, eliminates RAM suggestion clutter, and adds developer-focused sorting, grouping, pagination, and system port filtering to both the Web Dashboard (`http://127.0.0.1:5999/`) and CLI engine.

---

## 2. Severity Taxonomy & Identified Findings

### [P0 - CRITICAL] Process Tree Kill Failures on Windows (Resolved)
- **Location:** [apps/agyport/internal/service/portops/killer.go:70-76](./apps/agyport/internal/service/portops/killer.go#L70-L76)
- **Failure Mechanism:** On Windows, developer web applications (e.g. Node.js, Vite, Next.js) spawn child worker processes (`node.exe` -> `esbuild`/`swc`). Calling `taskkill /PID <pid> /T` without `/F` fails with exit code `128` ("process can only be terminated forcefully (with /F option)").
- **Fix Applied:** Modified `killer.go` to unconditionally include `/T /F` flags in Windows `taskkill` calls (`exec.Command("taskkill", "/PID", pidStr, "/T", "/F")`), guaranteeing clean process tree termination without dangling child sockets.

```go
if runtime.GOOS == "windows" {
    // Always use /T (tree kill) and /F (force) on Windows to ensure child processes are terminated
    cmd := exec.Command("taskkill", "/PID", strconv.Itoa(pid), "/T", "/F")
    if out, err := cmd.CombinedOutput(); err != nil {
        return fmt.Errorf("taskkill error: %s (%v)", strings.TrimSpace(string(out)), err)
    }
    return nil
}
```

---

### [P1 - HIGH] Netstat TCP Listening State Parsing Index Shift (Resolved)
- **Location:** [apps/agyport/internal/service/portops/collector.go:210-230](./apps/agyport/internal/service/portops/collector.go#L210-L225)
- **Failure Mechanism:** Windows `netstat -ano` output format is:
  `TCP    0.0.0.0:3000    0.0.0.0:0    LISTENING    21588`
  The code previously checked `fields[2] == "LISTENING"`, which evaluated `0.0.0.0:0` (Foreign Address). This caused active TCP dev ports (such as 3000, 4321, 5432, 6379) to be dropped from listening port listings.
- **Fix Applied:** Updated field index check to `fields[3] == "LISTENING"` and parsed PID from `fields[4]`.

---

### [P2 - MEDIUM] System Socket Misclassification as Custom Dev Servers (Resolved)
- **Location:** [apps/agyport/internal/service/portops/collector.go:345-385](./apps/agyport/internal/service/portops/collector.go#L345-L385)
- **Failure Mechanism:** High system sockets (`svchost.exe`, `System`, `dosvc`, `lsass`, `msedge.exe`, `chrome.exe`) on ports > 1024 were falling through to default `Custom Dev Server`.
- **Fix Applied:**
  1. Added explicit `Browser` detection category for `msedge.exe`, `chrome.exe`, `firefox.exe`, `brave.exe`.
  2. Identified system background daemons (`svchost`, `System`, `lsass`, `services`, `dosvc`, `sshd`, `tailscaled`, `adb`, `hasplms`, `sqlservr`) and flagged `is_system_port = true`.
  3. Formulated priority sorting (`getCategoryPriority`) placing Dev Servers (Priority #1) at the VERY TOP of port listings.

---

### [P3 - LOW] Removal of Smart RAM Suggestions Box (Resolved)
- **Location:** [apps/agyport/internal/service/ramops/ram.go:75-80](./apps/agyport/internal/service/ramops/ram.go#L75-L80) & [apps/agyport/internal/web/server.go](./apps/agyport/internal/web/server.go)
- **Rationale:** Per user feedback ("remove the suggset part thes so dumb"), removed suggestion banners from the Web UI and emptied the backend generator `GenerateRAMSuggestions` to return 0 suggestions.

---

### [P4 - INFO / ENHANCEMENT] Web Dashboard Enhancements (Resolved)
- **Location:** [apps/agyport/internal/web/server.go](./apps/agyport/internal/web/server.go)
- **Features Implemented:**
  1. **🛡️ Hide System Ports Toggle:** Checked by default to declutter developer workspace.
  2. **Group By Dropdown:** Supports `None (Flat List)`, `Framework`, and `Category` with collapsible section headers.
  3. **Interactive Column Sorting:** Clickable column headers (`PORT`, `PROTO`, `PID`, `PROCESS / PROJECT`, `FRAMEWORK`, `RAM USAGE`) with `▲`/`▼` indicators.
  4. **Table Pagination:** Customizable page size (`15`, `25`, `50`, `100`, `All`) with `Prev`/`Next` navigation and total count displays.
  5. **Direct Server Access:** Added `🌐 Open` button for instant dev server navigation (`http://localhost:<port>`).

---

## 3. Verification & Test Suite

All unit tests pass cleanly:
```bash
wsl bash -c "cd apps/agyport && go test -v ./..."
# Result: PASS (ok agyport/internal/model, portops, ramops, view, web)
```
Cross-compiled binary verified on Windows:
```powershell
dist\windows\agyport.exe ls --json
```

---

*Report prepared by Antigravity AI Code Review Engine.*
