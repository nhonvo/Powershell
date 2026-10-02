# Research Report: Existing Solutions for Multi-Agent Terminal Multiplexing & Direct Agent Management

**Date:** October 3, 2026  
**Focus:** Terminal-native tools, multiplexers, and agent orchestrators that run multiple child terminals simultaneously and allow direct human-in-the-loop management.  
**Deliverable File:** Pure Markdown Research Report

---

## 1. Executive Summary & Problem Definition

When running multiple AI coding agents (such as Antigravity `agy`, Claude Code, Aider, or custom autonomous scripts), developers face the **"Orchestrator's Dilemma"**:
- Running agents sequentially is too slow.
- Running agents in separate OS terminal tabs causes context switching fatigue, lost approval prompts, and invisible rate limits.
- Developers need a single **terminal-native cockpit** that can:
  1. Open multiple child terminals (PTYs) in split panes or tiles inside a single terminal window.
  2. Programmatically spawn agents across different directories, accounts, or git worktrees.
  3. **Directly interact** with any child agent (pass keyboard input, approve tool calls, interrupt/resume).
  4. Track lifecycle states (`Working`, `Waiting for Approval`, `Idle`, `Throttled`, `Done`) at a glance.

---

## 2. Taxonomy of Existing Solutions

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                        Multi-Agent Terminal Solution Spectrum                          │
├────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                        │
│  [Traditional Multiplexers]       [Agent-Aware Multiplexers]     [GUI Agent Terminals] │
│   • tmux / tmuxp / tmuxinator      • Herdr (Rust PTY server)      • cmux (Ghostty GUI) │
│   • Zellij (WASM plugins)          • Overmind (tmux supervisor)   • Superset / Termdock│
│   • GNU Screen                     • Custom Go PTY Dashboards                          │
│                                                                                        │
│  ◀── Terminal Native / Low Overhead ──────────────────────── Native OS GUI App ───────▶│
└────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Deep Dive into Leading Existing Solutions

### 3.1 Herdr: The "Agent-Aware" Terminal Multiplexer
**Herdr** is widely considered the pioneer of the *agent-native multiplexer* category.

*   **Architecture**: Client-Server written in Rust. A background daemon owns all child pseudo-terminals (PTYs), while a terminal client renders the TUI.
*   **How it Works**:
    - Creates multiple child panes in your existing terminal.
    - Monitors process names and stdout/stderr output from each child terminal.
    - Uses pattern-matching and hooks to detect whether an agent is actively generating code, stuck on a confirmation prompt (`[y/N]`), or finished.
*   **Direct Agent Management**:
    - **Notification Rings**: Panes that need human input (e.g. tool confirmation) flash in yellow/amber.
    - **Direct Focus / Passthrough**: Pressing hotkeys (e.g., `Alt+1`, `Alt+2`) drops your keyboard directly into that agent's PTY.
    - **Socket API**: Exposes an IPC socket allowing agents to split panes or launch companion agents programmatically.
*   **Pros**: Works over SSH, works inside standard Linux/WSL2/macOS terminals, low memory footprint.
*   **Cons**: Complex to customize custom prompt matchers for non-standard CLI outputs.

---

### 3.2 cmux: The Native macOS Agent Supervisor
**cmux** represents the GUI-first approach to multi-agent terminal management.

*   **Architecture**: Native Swift/AppKit application built directly on the **libghostty** GPU-accelerated terminal engine.
*   **How it Works**:
    - Replaces your terminal emulator rather than running inside it.
    - Organizes agents into vertical workspace tabs, split grids, and approval queues.
*   **Direct Agent Management**:
    - Built-in **Approval Feed**: Automatically intercepts tool execution prompts from child CLIs and shows clickable buttons (Approve / Reject) in a sidebar.
    - Instant hotkey switching between unread agent sessions.
*   **Pros**: Ultra-smooth 120fps rendering, polished UI, notifications integrated with macOS.
*   **Cons**: macOS only (not compatible with pure Linux or WSL2 CLI environments), cannot be run over a headless SSH connection.

---

### 3.3 tmux + tmuxp / tmuxinator + Git Worktrees (The Classic Power Setup)
The battle-tested standard among terminal purists.

*   **Architecture**: POSIX terminal multiplexer running client-server.
*   **How it Works**:
    - Uses declarative YAML configuration (`tmuxp` or `tmuxinator`) to spawn pre-configured layouts (e.g. 2x2 grid with 4 agents).
    - Pairs with **Git Worktrees** (`git worktree add ../feature-branch`) to isolate each agent into its own clean directory.
*   **Direct Agent Management**:
    - Navigate panes with `Ctrl+b` + arrow keys (or Vim `h/j/k/l`).
    - Programmatic injection via `tmux send-keys -t session:0.1 "command" Enter`.
    - Broadcast mode via `setw synchronize-panes on` to send identical commands to all agents.
*   **Pros**: Ubiquitous on every Linux/WSL2 machine, zero installation overhead, rock-solid stability.
*   **Cons**: Completely blind to agent lifecycle state. You have to visually scan all panes to notice if an agent is blocked on a prompt.

---

### 3.4 Zellij: Modern Terminal Workspace with WASM Plugins
**Zellij** is a modern Rust-based terminal multiplexer that bridges the gap between tmux and graphical IDEs.

*   **Architecture**: Multi-threaded terminal workspace with floating panes, tiled layouts, and a WebAssembly (WASM) plugin system.
*   **How it Works**:
    - Defines custom layouts via KDL layout files.
    - Supports floating terminal windows on top of tiled agent panes.
*   **Direct Agent Management**:
    - Built-in pane naming and status bar.
    - Plugins like `zellij-agent-tracker` can query the Zellij plugin API to display unread badges on inactive panes when output changes.
*   **Pros**: Exceptional out-of-the-box keyboard UX, floating modal panes, fast Rust performance.
*   **Cons**: WASM plugin ecosystem is still maturing; requires learning Zellij-specific keybindings.

---

### 3.5 Overmind / Hivemind: Process Managers with Interactive Attach
Originally designed for web development (Procfile), these tools are increasingly used for multi-agent process supervision.

*   **Architecture**: Go daemon managing subprocesses inside hidden tmux windows.
*   **How it Works**:
    - Spawns agents defined in a `Procfile`:
      ```procfile
      researcher: agy --prompt "Research OAuth2"
      coder:      agy --prompt "Implement Auth"
      auditor:    agy --prompt "Run Security Audit"
      ```
    - Shows a unified, interleaved terminal log.
*   **Direct Agent Management**:
    - Run `overmind connect coder`: instantly drops your terminal into the interactive stdin/stdout of the coder agent.
    - Detach with `Ctrl+b d` and return to the main dashboard.
*   **Pros**: Simple Procfile syntax, crash restart policies, easy to daemonize.
*   **Cons**: Interleaved log can be chaotic; doesn't provide visual split panes by default.

---

## 4. Comprehensive Comparison Matrix

| Feature | Herdr | cmux | tmux / tmuxp | Zellij | Overmind |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Interface** | Terminal TUI | macOS GUI (Ghostty) | Terminal TUI | Terminal TUI | Terminal CLI |
| **OS Compatibility** | Linux, macOS, WSL2 | macOS only | Linux, macOS, WSL2 | Linux, macOS, WSL2 | Linux, macOS, WSL2 |
| **Multi-Pane Split** | Yes (Dynamic Grid) | Yes (Tiled & Tabs) | Yes (Any layout) | Yes (Tiled & Float) | Hidden (connect on-demand) |
| **Direct Stdin Typing** | Yes (Instant switch) | Yes (Direct) | Yes (Pane focus) | Yes (Pane focus) | Yes (`overmind connect`) |
| **Agent State Awareness** | ✅ High (Working/Blocked) | ✅ High (Approval feed) | ❌ None (Pure terminal) | ⚠️ Partial (via WASM) | ⚠️ Partial (Process exit only) |
| **Programmatic API** | Unix Socket | AppleScript / CLI | CLI (`send-keys`) | WASM / CLI | CLI / Signals |
| **SSH / Remote Ready** | ✅ Yes | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes |
| **Resource Footprint** | Low (Rust) | Medium (GUI) | Minimal (C) | Low (Rust) | Low (Go) |

---

## 5. Architectural Blueprint for the Antigravity Suite

Given our existing ecosystem in **WSL2 + Windows VSCode** with **`agyswitch`** (multi-account management) and **`agyproj`** (workspace management), we have two optimal implementation paths:

### Option A: The "Cockpit" Mode in `agyterm` / `agyswarm` (Pure Go PTY Engine)
Build a native agent-aware terminal multiplexer inside `apps/agyterm` or `apps/agyswarm`:
1. **PTY Engine**: Use `github.com/creack/pty` in Go to spawn and own child agent processes.
2. **Terminal Grid Layout**:
   - Split 1x2 or 2x2 viewport panes inside the existing Go TUI.
   - Pane 1: Worker 1 (Account A - Research Agent)
   - Pane 2: Worker 2 (Account B - Code Implementation)
   - Pane 3: Worker 3 (Account C - Test & Review Agent)
3. **Focus Routing**:
   - In **Overview Mode**: Keystrokes navigate the agent dashboard (`↑/↓/Tab`).
   - In **Interactive Mode**: Pressing `Enter` or `1-4` attaches raw `os.Stdin` directly to that child agent's PTY, allowing direct typing and prompt approval.
   - Pressing `Ctrl+]` or `Ctrl+\` detaches back to the Overview Cockpit.
4. **Lifecycle Detection**:
   - Scans child PTY output for known Antigravity prompt tokens (e.g. `[y/N]`, `Select an option`, `Error: 429`).
   - Displays real-time status badges: `● RUNNING`, `⚡ NEED INPUT`, `✔ DONE`.

### Option B: The Automated `tmux` Engine (`agyswarm tmux`)
Instead of building a full terminal emulator from scratch, generate programmatic tmux sessions:
1. `agyswarm` issues shell commands to configure a dedicated `antigravity-swarm` tmux session.
2. Automatically creates git worktrees per agent to avoid merge conflicts.
3. Automatically sets environment variables (`GEMINI_CLI_HOME`, account directory) per pane.
4. The user attaches with a single command: `tmux attach -t antigravity-swarm`.

---

## 6. Recommendations & Next Steps

1. **For Immediate Use**:
   - Use **Herdr** if you want an off-the-shelf, terminal-native agent multiplexer that works seamlessly in Linux/WSL2 with state tracking.
   - Use **tmux + tmuxp** if you want zero external dependencies and maximum scriptability.
2. **For Antigravity Suite Evolution**:
   - Adopt **Option A (Native Go PTY Cockpit)** inside `apps/agyswarm` or `apps/agyterm`. This allows native integration with `agyswitch`'s account rotation and `agyproj`'s workspace paths, delivering a unified, zero-dependency Go binary.

---

## Reference Links (WSL2 + Windows Environment)
- **VS Code Clickable (Recommended):** [MULTI_AGENT_TERMINAL_SOLUTIONS_RESEARCH.md](./MULTI_AGENT_TERMINAL_SOLUTIONS_RESEARCH.md)
- **Windows Path:** `C:\Users\TruongNhon\Documents\Powershell\MULTI_AGENT_TERMINAL_SOLUTIONS_RESEARCH.md`
- **Related Brainstorm Document:** [AGY_MULTI_AGENT_AND_TELEGRAM_PLATFORM_BRAINSTORM.md](./AGY_MULTI_AGENT_AND_TELEGRAM_PLATFORM_BRAINSTORM.md)
