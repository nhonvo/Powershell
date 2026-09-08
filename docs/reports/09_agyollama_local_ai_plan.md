# 🤖 AGYOLLAMA & Non-Blocking UI Architecture: Technical Plan & Specification

> **Category**: Strategic Plan & Architecture Blueprint  
> **Subsystem**: Local AI Engine (`apps/agyollama`), Non-Blocking Event Loop & UI Animation  
> **Environment**: Ubuntu WSL2 + Windows 11 / VS Code  
> **Date**: September 8, 2026  
> **Status**: In Active Execution  
> **Clickable Reference**: [AGYOLLAMA_LOCAL_AI_PLAN.md](./AGYOLLAMA_LOCAL_AI_PLAN.md)  
> **Windows UNC Path**: `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\AGYOLLAMA_LOCAL_AI_PLAN.md`  
> **Master Gateway**: [docs/README.md](./docs/README.md)  
> **Reports Catalog**: [docs/reports/README.md](./docs/reports/README.md)

---

## 1. Executive Summary

This specification addresses two core engineering frontiers in the **Antigravity Developer Suite**:

1. **Non-Blocking UI Architecture & Continuous Loading Animations**:
   - Long-running operations such as live quota probing (`agyswitch`), Docker stack teardowns/pruning (`agydocker`), Git fleet scanning (`agygit`), and project indexing (`agyproj`) historically caused the terminal event loop to stall or block input reading.
   - We transition all long tasks to background goroutines with state mutexes and non-blocking UNIX `poll()` polling (`waitKey(fd, 80)`).
   - The user's cursor can freely navigate other accounts, containers, tabs, and modals without being stuck or frozen while background tasks proceed. Continuous ANSI spinner animations (`⠋`, `⠙`, `⠹`, `⠸`, `⠼`, `⠴`, `⠦`, `⠧`, `⠇`, `⠏`) render smoothly.

2. **Creation of `agyollama` (Local Ollama & Open LLM AI Cockpit)**:
   - Port the local AI agent, daemon control, and model management capabilities from the legacy C# system (`AgyTui`'s `IOllamaClient`, `OllamaClient.cs`, `OllamaStatusScreen`, `OllamaModelManagerScreen`, and `OllamaBenchmarkScreen`) into a native, high-performance Go micro-application (`apps/agyollama`).
   - Integrated into the `agyx` Master Proxy and 7-tab Cockpit orchestrator.

---

## 2. Architectural Comparison: Legacy C# vs Go `agyollama`

| Capability | Legacy C# (`AgyTui`) | Modern Go (`agyollama`) | Technical Advantage |
| :--- | :--- | :--- | :--- |
| **Runtime & Startup** | .NET 9 JIT / Single-File (`180ms - 320ms`) | Native Go ELF binary (`8ms - 14ms`) | Zero JIT overhead, instant startup |
| **Daemon Probe** | `IPGlobalProperties.GetActiveTcpListeners()` | HTTP `/api/version` + socket check (500ms timeout) | Non-blocking, cross-platform |
| **Model Catalog** | Synchronous `client.GetStringAsync("/api/tags")` | Streaming JSON decoder with structured `ModelInfo` | Accurate byte-to-GB precision & model family |
| **Model Pull** | CLI pass-through or blocking HTTP | Streaming HTTP reader (`/api/pull`) with progress bars | Real-time byte/percent tracking in TUI & CLI |
| **Inference Benchmark** | Basic 5-word prompt latency | Latency (s) + Token/sec + Prompt eval duration | Comprehensive hardware performance evaluation |
| **Interactive Chat** | `AiProcessRunner.RunInteractive("ollama", "run")` | Cooked-mode terminal handover + raw re-entry | Clean PTY restoration, zero visual artifacts |
| **Default Model State** | Plain text file `default_model.txt` | JSON/Config in `~/.config/antigravity/ollama.json` | Backward-compatible fallback |
| **Master Routing** | `CommandRegistry.cs` in monolithic assembly | Unified `agyx` proxy (`agyx ollama`, `agyx ai`) | Isolated memory space, zero assembly locking |

---

## 3. Non-Blocking Event Loop & UI Animation Architecture

### 3.1 The Problem
In raw terminal mode (`golang.org/x/term.MakeRaw(fd)`), invoking `os.Stdin.Read(buf[:])` blocks the OS thread until a byte arrives. If an operation (such as `Store.ListAccounts()` making external HTTP calls or `docker compose down` waiting for container termination) runs on the main thread:
1. The UI completely freezes for 2 to 10 seconds.
2. Keystrokes queue up or drop.
3. The cursor cannot move to other items or tabs.
4. Animated spinners cannot tick while the user is actively navigating.

### 3.2 The Non-Blocking Solution (`poll()` + Background Goroutines)

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Loop as Terminal Event Loop (waitKey 80ms)
    participant Worker as Background Goroutine
    participant Ext as CloudCode API / Docker Daemon
    participant View as ANSI Terminal Buffer

    User->>Loop: Press 'r' (Probe Quotas) or 'x' (Down Stack)
    Loop->>Worker: go func() [Set Pending Flag + Mutex]
    Loop->>View: Render Status: "[⠋] In progress... (Navigate freely)"
    
    par Continuous User Navigation
        User->>Loop: Key 'j' (Cursor Down)
        Loop->>View: SelectedIndex++ (Cursor moves instantly)
        Loop->>View: Update spinner frame (⠙)
    and Background Task Execution
        Worker->>Ext: Long I/O (HTTP Probe / Docker Stop)
        Ext-->>Worker: Response received
        Worker->>Loop: Update Cached State under Mutex
    end

    Loop->>View: Render: "✔ Task completed successfully"
```

### 3.3 Implementation Details
1. **`poll_linux.go`**:
   ```go
   //go:build !windows
   package view

   import "golang.org/x/sys/unix"

   func waitKey(fd int, timeoutMs int) bool {
       pfd := []unix.PollFd{{Fd: int32(fd), Events: unix.POLLIN}}
       n, err := unix.Poll(pfd, timeoutMs)
       return err == nil && n > 0
   }
   ```
2. **`agyswitch` Quota Probing**:
   - Triggered by `r`/`R` in Tab 0.
   - Mutex `probeMu` guards `isProbingQuotas bool` and `cachedAccs`.
   - In the event loop: when `!ready && a.isProbingQuotas`, advance `spinnerIdx` and re-render every 80ms.
   - Keystrokes (`j`, `k`, `1`-`4`, `v`, `Enter`) process immediately.
   - Safety Invariant: Vault credentials, tokens, account swapping, and directory mirroring are **100% untouched**.
3. **`agydocker` Container Lifecycle & Down**:
   - Adds `dockerops.DownCompose(project)` and `dockerops.DownContainer(id)`.
   - Hotkey `x`/`X` invokes Down Stack in background with animated spinner.
   - Post-completion reload runs asynchronously in background, preventing `docker ps` from stalling the frame loop.

---

## 4. `apps/agyollama` Application Design

### 4.1 Component Topology
```
apps/agyollama/
├── go.mod                                   # Module agyollama (Go 1.23)
├── main.go                                  # CLI Command Dispatcher & TUI Launcher
├── internal/
│   ├── model/
│   │   └── model.go                         # DaemonStatus, ModelInfo, BenchmarkResult, PullProgress
│   ├── service/
│   │   └── ollamaops/
│   │       ├── client.go                    # HTTP API Client targeting 127.0.0.1:11434
│   │       └── client_test.go               # Mock HTTP unit tests (100% pass)
│   └── view/
│       ├── app.go                           # 4-Tab ANSI VT100 Interactive Cockpit
│       ├── poll_linux.go                    # Linux unix.Poll non-blocking polling
│       ├── poll_windows.go                  # Windows fallback polling
│       └── view_test.go                     # UI rendering and non-interactive status tests
```

### 4.2 TUI 4-Tab Cockpit Layout
1. **Tab `[1] 🤖 Status & Daemon`**:
   - Live daemon status (`ONLINE` / `OFFLINE`).
   - Host binding (`http://127.0.0.1:11434`), server version, and uptime.
   - Active default model and VRAM / RAM metrics.
   - Quick actions: `[s]` Start Daemon, `[d]` Set Default, `[l]` View Logs.
2. **Tab `[2] 📦 Models`**:
   - Model table: Index, Model Name, Size (GB), Family, Parameter Size, Modified.
   - Interactive actions: `Enter` to select default model, `r` to run interactive chat, `v` to view detailed modelcard (`/api/show`), `d` to delete model, `p` to pull model with streaming download progress bar.
3. **Tab `[3] ⚡ Benchmark`**:
   - One-key latency & throughput benchmark (`b`).
   - Dispatches standardized inference query to installed models.
   - Displays real-time evaluation duration, prompt eval latency, and tokens/second.
4. **Tab `[4] 📜 Logs`**:
   - Live server log viewer inspecting Ollama daemon logs.

### 4.3 CLI Command Suite
```bash
agyollama status             # Check daemon health & active model
agyollama ls                 # List installed local models
agyollama pull <model>       # Download new model with streaming progress
agyollama run [model]        # Interactive chat with local model
agyollama benchmark          # Measure latency & throughput across installed models
agyollama start              # Boot background daemon (ollama serve)
agyollama delete <model>     # Remove local model from storage
agyollama info <model>       # Inspect model architecture and parameters
agyollama default [model]    # Get or set default system model
agyollama logs               # Show last 50 lines of daemon log
agyollama                    # (No args) Launch interactive 4-tab TUI Cockpit
```

---

## 5. Master Proxy (`agyx`) & Build Integration

1. **Proxy Registration (`apps/agyx/internal/proxy/proxy.go`)**:
   ```go
   {
       Name:        "ollama",
       BinaryName:  "agyollama",
       Aliases:     []string{"ai", "llm", "localai", "model"},
       Description: "Local Ollama daemon, model manager, benchmarking & AI agent cockpit",
   }
   ```
2. **Master Cockpit Expansion (`apps/agyx/internal/view/cockpit.go`)**:
   - Expands Master Cockpit from 6 to 7 tabs:
     `[1] 🔄 Switch` `[2] 📁 Proj` `[3] 📦 Git` `[4] 🐳 Docker` `[5] 🎨 Term` `[6] 📱 Mobile` `[7] 🤖 Ollama`
3. **Makefile Integration**:
   - `APPS := agyswitch agyproj agygit agydocker agyterm agyx agymobile agyollama`
   - Target `make ollama` for standalone build and installation.

---

## 6. Verification Criteria & Test Protocol

1. **Safety Verification**:
   - Verify `git diff apps/agyswitch/internal/service/vault` and `store` remain completely empty.
2. **Unit Test Verification**:
   - `cd apps/agyswitch && go test -count=1 ./...`
   - `cd apps/agydocker && go test -count=1 ./...`
   - `cd apps/agyollama && go test -count=1 ./...`
   - `cd apps/agyx && go test -count=1 ./...`
   - Master suite: `make test` (100% green across all 8 applications).
3. **Compilation & CLI Test**:
   - `make build` -> 8 binaries in `./bin/`.
   - Test all new commands (`./bin/agyollama status`, `./bin/agyollama ls`, `./bin/agyx ls`, etc.).
4. **Documentation Compliance**:
   - All links must be workspace-relative (`./...`).
   - Zero `file:///home/` or broken absolute paths.
