# AGYPORT Code Review & Fix Report: RAM Metrics & Console TUI Navigation

## Executive Summary

This report documents the findings, root cause analysis, and resolution for two critical UX and data accuracy issues in `agyport`:
1. **Misleading RAM Metrics Display**: Summing per-process RSS memory across multiple open ports of the same PID in Web Dashboard metrics and group aggregations.
2. **Console TUI Navigation Lag**: 800ms input poll blocking delay on Windows and continuous 800ms background socket scanning on Linux/Unix during keyboard navigation.

---

## Findings & Audit Taxonomy

### 1. [P1 - HIGH] Misleading Multi-Port Process RAM Aggregation in Web Dashboard

* **File Link**: [`server.go:578-588`](file:///C:/Users/TruongNhon/Documents/Powershell/apps/agyport/internal/web/server.go#L578-L588)
* **Offending Snippet**:
  ```javascript
  var devServers = 0;
  var reclaimBytes = 0;
  ports.forEach(function(p) {
    if (!p.is_system_port && p.pid > 0) {
      devServers++;
      reclaimBytes += p.memory_bytes;
    }
  });
  ```
* **Failure Mechanism**: When a single dev process (e.g. PID `1234`) listens on 4 ports (e.g. `3000`, `3001`, `3002`, `3003`) and consumes 1.5 GB RSS, `ports.forEach` loops over all 4 port entries. Adding `p.memory_bytes` 4 times resulted in `1.5 GB × 4 = 6.0 GB`, double-counting RAM and misrepresenting each port as costing 1.5 GB independently.
* **Resolution**: Replaced naive array iteration with PID-level deduplication (`devPids[p.pid]`). Updated table column headers to `RAM USAGE (PID Total)` and added a `(Shared across N ports)` indicator badge.

---

### 2. [P1 - HIGH] 800ms Input Blocking Delay & Background Polling Lag in Console TUI

* **File Link**: [`tui.go:119-123`](file:///C:/Users/TruongNhon/Documents/Powershell/apps/agyport/internal/view/tui.go#L119-L123)
* **Offending Snippet**:
  ```go
  // Poll input with timeout for live background refresh
  if !waitKey(fd, 800) {
      a.ReloadAsync()
      continue
  }
  ```
* **Failure Mechanism**:
  1. On **Windows**, `waitKey(fd, 800)` introduced a mandatory 800ms sleep loop before reading stdin, causing noticeable key input lag during menu/list navigation.
  2. On **Unix/Linux**, `unix.Poll(pfd, 800)` timed out every 800ms and launched `a.ReloadAsync()`, spawning `lsof`/`ss` background process scans every 800ms while navigating or reading.
* **Resolution**: Removed `waitKey(fd, 800)` from the raw terminal input loop. Terminal input reads directly from stdin with 0ms latency. Data scanning is executed on startup and explicitly when the user presses **`R` / `r`**. Added `[r] Refresh Metrics` to the hotkey bar.

---

## Verification & Testing Results

All unit test suites across all packages passed successfully:

```text
ok  	agyport/internal/model     (cached)
ok  	agyport/internal/service/portops
ok  	agyport/internal/service/ramops
ok  	agyport/internal/view      0.058s
ok  	agyport/internal/web       0.077s
```
