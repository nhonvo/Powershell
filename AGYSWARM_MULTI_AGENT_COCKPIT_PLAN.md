# AGYSWARM: Multi-Agent Terminal Cockpit & Cross-Account Orchestrator Plan

**Application Name:** `agyswarm`  
**Purpose:** Terminal-native multi-child PTY multiplexer that runs multiple AI agents concurrently across different Google accounts and workspaces, tracks real-time agent lifecycle states, enables direct keyboard passthrough, and produces pure Markdown (`.md`) deliverables.  
**Date:** October 3, 2026

---

## 1. Architectural Overview

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                        AGYSWARM TERMINAL COCKPIT (TUI)                                 │
├────────────────────────────────────────────────────────────────────────────────────────┤
│ Active Accounts: nhontruongvo3 (84%), account-2 (92%) · Total Agents: 3 · Swarm: ACTIVE│
├────────────────────────────────────────────────────────────────────────────────────────┤
│ ┌──────────────────────────────────────┐ ┌──────────────────────────────────────────┐ │
│ │ [1] 🤖 Web Researcher (Account A)   │ │ [2] 💻 Code Implementer (Account B)      │ │
│ │ Status: ● WORKING                    │ │ Status: ⚡ NEED INPUT                    │ │
│ │ Workspace: /projects/finance-dash    │ │ Workspace: /projects/finance-dash/ui     │ │
│ │ > Crawling DuckDB docs...            │ │ > [y/N] Apply patch to SignInPage.tsx?   │ │
│ │ > 42 URLs parsed, 12 citations       │ │ >                                        │ │
│ └──────────────────────────────────────┘ └──────────────────────────────────────────┘ │
│ ┌──────────────────────────────────────┐ ┌──────────────────────────────────────────┐ │
│ │ [3] 🧪 Test & Audit (Account C)      │ │ [4] 📊 Swarm Blackboard & Telemetry      │ │
│ │ Status: ✔ DONE                       │ │ Shared Findings: 6 artifacts             │ │
│ │ Workspace: /projects/finance-dash    │ │ Quota Bandwidth: 276% (Combined)         │ │
│ │ > 18 unit tests passed               │ │ Output: ./doc/research/REPORT.md         │ │
│ └──────────────────────────────────────┘ └──────────────────────────────────────────┘ │
├────────────────────────────────────────────────────────────────────────────────────────┤
│ [Enter / i] Attach Keyboard Direct  ·  [Ctrl+]] Detach  ·  [S] Spawn Agent  ·  [M] .md │
│ [K] Kill Agent  ·  [R] Restart  ·  [B] Broadcast  ·  [Tab / ↑↓] Navigate  ·  [Q] Exit │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Core Functional Requirements

### 2.1 Child Terminal Process & PTY Engine (`internal/engine`)
- Uses `github.com/creack/pty` to allocate dedicated pseudo-terminals for each child agent.
- Independent environment variables for each child terminal:
  - `GEMINI_CLI_HOME` pointing to that agent's specific account context from `agyswitch`.
  - Working directory (`Cwd`) pointing to project path from `agyproj` or git worktree.
- Ring buffer for output: stores the last 1,000 lines of terminal output per agent with ANSI formatting preserved.
- **State Heuristic Engine**:
  - `WORKING` (Green): Process is generating output.
  - `NEED_INPUT` (Yellow/Blinking): Output ends with confirmation or input prompts:
    - Patterns: `[y/N]`, `(y/n)`, `? `, `Select an option`, `Press enter`, `password:`, `Approve?`.
  - `DONE` (Cyan): Child process exited with return code 0.
  - `ERROR` (Red): Process exited with non-zero code, or output contains `429`, `Quota exceeded`, `fatal:`.
  - `PAUSED` (Gray): Suspended via `SIGSTOP`.

### 2.2 Direct Interactive Keyboard Pass-through (`internal/engine/attach.go`)
- When the user presses `[Enter]` or `[i]` on an agent pane:
  1. The cockpit switches the OS terminal into raw mode.
  2. Binds `os.Stdin` directly to the child's PTY master file descriptor.
  3. Binds child's PTY output directly to `os.Stdout`.
  4. The user has 100% interactive terminal control (typing responses, navigating TUI prompts, approving diffs).
  5. Pressing **`Ctrl+]`** (0x1D) detaches cleanly, restores cockpit TUI mode, and leaves the agent running smoothly.

### 2.3 Cross-Account Multi-Agent Swarm Coordinator (`internal/swarm`)
- Connects to `agyswitch` store to discover all logged-in accounts and quota fractions.
- When launching a swarm task (e.g. `agyswarm research <topic>`):
  - Spawns Worker 1 on Account 1.
  - Spawns Worker 2 on Account 2.
  - Aggregates CloudCode quota limits (providing 200–300% throughput).

### 2.4 Pure Markdown Deliverable Engine (`internal/markdown`)
- Generates clean, publication-grade GFM Markdown files (`.md` only):
  - Session transcript summary.
  - Tool invocations and results.
  - Final synthesized answer, code diffs, or research dossier.
  - Auto-saved to `./doc/swarm/<timestamp>_<task>.md`.

---

## 3. Directory Layout for `apps/agyswarm`

```
apps/agyswarm/
├── go.mod
├── go.sum
├── main.go                       # CLI Entrypoint (cockpit, spawn, attach, list, research)
├── internal/
│   ├── model/
│   │   ├── agent.go              # AgentSession, Status, SwarmTask, PTYConfig
│   │   └── event.go              # InterAgentEvent, BlackboardRecord
│   ├── engine/
│   │   ├── manager.go            # PTY process supervisor & life-cycle tracker
│   │   ├── pty_linux.go          # creack/pty allocation, resize, and I/O
│   │   ├── heuristics.go         # Prompt detection ([y/N], 429 quota error, etc.)
│   │   └── attach.go             # Raw stdin/stdout interactive pass-through
│   ├── swarm/
│   │   ├── coordinator.go        # Cross-account swarm dispatch & quota balancing
│   │   └── blackboard.go         # Shared inter-agent memory & artifact exchange
│   ├── markdown/
│   │   └── exporter.go           # Pure Markdown report and dossier compiler
│   └── view/
│       ├── cockpit.go            # 2x2 grid / tiled TUI rendering
│       └── tui_test.go           # TUI unit test suite
```

---

## 4. Phased Implementation Steps

1. **Phase 1: Project Setup & PTY Engine**:
   - Initialize `apps/agyswarm` Go module with `github.com/creack/pty` and `golang.org/x/term`.
   - Implement `manager.go` and `pty_linux.go` with ring-buffer stdout/stderr capture and prompt heuristics.
2. **Phase 2: Direct Interactive Pass-through & Cockpit TUI**:
   - Implement `attach.go` with `Ctrl+]` escape sequence.
   - Build Cockpit TUI grid with color-coded status badges, scrolling output panes, and keyboard navigation.
3. **Phase 3: Cross-Account Swarm & Markdown Deliverables**:
   - Integrate with `agyswitch` vault for account assignment and `agyproj` for project root detection.
   - Implement `exporter.go` to generate clean `.md` dossier files.
4. **Phase 4: Makefile Integration & Verification**:
   - Add `agyswarm` target to root `Makefile`.
   - Write comprehensive unit tests and verify build.

---
- **VS Code Clickable:** [AGYSWARM_MULTI_AGENT_COCKPIT_PLAN.md](./AGYSWARM_MULTI_AGENT_COCKPIT_PLAN.md)
- **Windows Path:** `C:\Users\TruongNhon\Documents\Powershell\AGYSWARM_MULTI_AGENT_COCKPIT_PLAN.md`
