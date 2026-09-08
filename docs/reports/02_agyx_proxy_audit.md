# ⚡ Deep Technical Audit: `agyx` Master Proxy & Orchestrator (Go Engine)

> **Category**: Developer Suite Core Audit  
> **Target Subsystem**: `apps/agyx`  
> **Source Files**: [apps/agyx/main.go](../../apps/agyx/main.go) · [apps/agyx/internal/proxy/proxy.go](../../apps/agyx/internal/proxy/proxy.go) · [apps/agyx/internal/view/cockpit.go](../../apps/agyx/internal/view/cockpit.go)  
> **Comparison Baseline**: C# `CommandRouter.cs` (1,468 LOC, 272 switch cases)  
> **Windows UNC Reference**: `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\apps\agyx`

---

## 1. Executive Summary & Architecture

`agyx` is the unified gateway, master proxy, and orchestrator for the Antigravity developer suite. Rather than maintaining a monolithic application or forcing developers to memorize dozens of isolated CLI binaries, `agyx` provides:
1. **Interactive Master Cockpit**: A keyboard-driven TUI displaying status summaries across all suite modules.
2. **Subcommand Routing & Proxying**: Direct CLI delegation to `agyswitch`, `agyproj`, `agygit`, `agydocker`, and `agyterm`.
3. **PTY Terminal Handover**: Clean raw-mode suspension and restoration when delegating to child interactive TUIs.
4. **Shell Profile Integration**: Command generators for Zsh, Bash, and PowerShell (`agyx init`).

```
                              ┌───────────────────────────────────┐
                              │            AGYX CLI / TUI         │
                              └─────────────────┬─────────────────┘
                                                │
         ┌──────────────────┬───────────────────┼───────────────────┬──────────────────┐
         ▼                  ▼                   ▼                   ▼                  ▼
    agyswitch            agyproj             agygit              agydocker          agyterm
  (Vault/Quota)       (Workspaces)        (Git Fleet)          (Containers)         (Themes)
```

---

## 2. Code Defects & Implementation Gaps

### 2.1 Critical Omission: `agymobile` Missing from Suite Registry
- **Location**: [apps/agyx/internal/proxy/proxy.go lines 20–53](../../apps/agyx/internal/proxy/proxy.go#L20-L53)
- **Defect**: `GetRegisteredTools()` registers only 5 tools:
  - `switch` (`agyswitch`)
  - `proj` (`agyproj`)
  - `git` (`agygit`)
  - `docker` (`agydocker`)
  - `term` (`agyterm`)
- **Impact**:
  - Running `agyx mobile` or `agyx m` yields `Unknown suite command 'mobile'`.
  - The Cockpit TUI ([cockpit.go](../../apps/agyx/internal/view/cockpit.go#L108-L115)) has hardcoded tabs 0 to 4, completely omitting `agymobile`.
- **Remediation**: Add `mobile` (`agymobile`, aliases: `m`, `remote`, `phone`, `pwa`) to `GetRegisteredTools()` and expand Cockpit tab navigation from 5 to 6 tabs.

### 2.2 Subprocess Exit Code Truncation Bug
- **Location**: [apps/agyx/main.go lines 46–51](../../apps/agyx/main.go#L46-L51)
```go
tool := proxy.ResolveTool(cmd)
if tool != nil {
    args := os.Args[2:]
    if err := proxy.Execute(tool.BinaryName, args); err != nil {
        // If error is an ExitError, preserve the exit code
        os.Exit(1) // ⚠️ DEFECT: Hardcodes exit code 1!
    }
    return
}
```
- **Defect**: The comment says `"preserve the exit code"`, but the code unconditionally executes `os.Exit(1)`.
- **Impact**: Any non-zero exit code from a child process (e.g., `git` exit code 128, SIGINT 130, or custom tool errors) is erased and returned as `1` to calling shell scripts or CI/CD pipelines.
- **Remediation**:
```go
if err := proxy.Execute(tool.BinaryName, args); err != nil {
    if exitErr, ok := err.(*exec.ExitError); ok {
        os.Exit(exitErr.ExitCode())
    }
    os.Exit(1)
}
```

### 2.3 Absence of Inter-Process Communication (IPC)
- **Current State**: `agyx` does not maintain a daemon, shared memory, or Unix domain socket.
- **Limitation**: Every sub-tool invocation (`agydocker`, `agyswitch`) requires spawning a brand-new OS process that re-reads disk state from scratch. There is no real-time telemetry pipeline (e.g. streaming Docker RAM or active AI session progress directly onto the `agyx` dashboard).
- **Remediation**: Introduce a lightweight Unix Domain Socket (`~/.local/state/agyx/daemon.sock`) for low-latency status queries across tools.

---

## 3. Comparison: Go `agyx` vs Legacy C# `CommandRouter`

| Architectural Aspect | Legacy C# `CommandRouter` | Go Engine `agyx` | Evaluation |
| :--- | :--- | :--- | :--- |
| **Execution Architecture** | In-process assembly execution via reflection / PowerShell. | Subprocess proxy with native OS execution. | 🟢 **Go Wins**: Clean process isolation; zero memory leaks or DLL locking. |
| **Binary Startup Time** | 150ms–300ms (CLR tiered JIT). | < 10ms (compiled native Go). | 🟢 **Go Wins**: Instantaneous execution. |
| **Command Scope** | 272 switch cases covering all system functions. | Modally delegated to dedicated binaries. | 🟢 **Go Wins**: Modular UNIX philosophy; single-responsibility binaries. |
| **Safety Invariants** | Reflection tests asserting all 270+ aliases have router cases. | Dynamic alias string slice mapping. | 🟡 **C# Advantage**: C# statically proved zero orphaned commands via unit tests. |

---

## 4. Prioritized Action Plan & Next Steps

1. **Register `agymobile`**:
   - Update `proxy.go` with `agymobile` definition and aliases (`m`, `remote`).
   - Add Tab `[6] 📱 Mobile` in [cockpit.go](../../apps/agyx/internal/view/cockpit.go).
2. **Fix Exit Code Propagation**:
   - Inspect `*exec.ExitError` in `main.go` and return native exit status.
3. **Cockpit Live Metrics Preview**:
   - Instead of static descriptive text in Cockpit tabs, fetch live data:
     - Tab `Switch`: Display active account and Gemini/Claude quota bar.
     - Tab `Docker`: Display WSL2 RAM gauge from `/proc/meminfo`.
     - Tab `Git`: Display dirty repo count across projects.
