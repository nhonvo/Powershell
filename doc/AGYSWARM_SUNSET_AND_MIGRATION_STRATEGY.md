# AGYSWARM Sunset, Technical Audit & Migration Strategy

**Date:** 2026-10-23  
**Status:** SUNSET / DEPRECATED (Moved from `apps/agyswarm` to `tools/agyswarm`)  
**Target:** Multi-Agent Terminal Cockpit & Child PTY Orchestrator  

---

## 1. Executive Summary & Context

`agyswarm` was conceived as a multi-child terminal agent orchestrator and PTY cockpit designed to run parallel agent workers across multiple Google/Gemini quota identities. In practice, the feature proved non-functional, confusing to users, and architecturally misaligned with modern LLM agent orchestration.

As instructed, **`agyswarm` has been moved out of the primary suite applications into [tools/agyswarm](./tools/agyswarm)**. This document provides an evidence-based technical audit explaining why the swarm feature failed, details the code defects discovered, and outlines a clear sunset and transition roadmap.

---

## 2. Root Cause Audit & Failure Mechanisms

```
┌────────────────────────────────────────────────────────────────────────┐
│                   agyswarm Architecture Breakdown                      │
├────────────────────────────────┬───────────────────────────────────────┤
│ Concept                        │ Reality in Codebase                   │
├────────────────────────────────┼───────────────────────────────────────┤
│ "Autonomous Multi-Agent Swarm" │ Mock bash/powershell echo sleep loop  │
│ "Interactive PTY Cockpit"      │ Stdin race condition & output corrupt │
│ "Multi-Account Workforce"      │ Symlink collision in ~/.gemini_swarm  │
└────────────────────────────────┴───────────────────────────────────────┘
```

### Finding 1: [P0 - CRITICAL] `runSwarmTask` Is a Dummy Echo Script Without Agent Logic
- **File Reference:** [tools/agyswarm/main.go:166-174](./tools/agyswarm/main.go#L166-L174)
- **Offending Code:**
```go
		// Command to run: powershell or bash
		cmd := "bash"
		var agentArgs []string
		if runtime.GOOS == "windows" {
			cmd = "powershell.exe"
			agentArgs = []string{"-NoProfile", "-Command", fmt.Sprintf("Write-Host 'Agent %s starting task on account %s...'; Start-Sleep -Seconds 1; Write-Host 'Finished research subtask %d.'", workerName, acc, i+1)}
		} else {
			agentArgs = []string{"-c", fmt.Sprintf("echo 'Agent %s starting task on account %s...'; sleep 1; echo 'Finished research subtask %d.'; exit 0", workerName, acc, i+1)}
		}
```
- **Failure Mechanism:** The command `agyswarm run --task "..."` claimed to execute an autonomous multi-agent swarm across accounts. In reality, it only spawned shells running `echo` and `sleep 1`. It never called the Antigravity CLI (`agy`), never invoked an LLM API, and never performed actual problem-solving. Users running this observed trivial dummy outputs that generated fake markdown summaries.

---

### Finding 2: [P0 - CRITICAL] Concurrent `os.Stdin` Read Contention Causing 50% Keystroke Dropping
- **File Reference:** [tools/agyswarm/internal/view/cockpit.go:73-89](./tools/agyswarm/internal/view/cockpit.go#L73-L89) & [tools/agyswarm/internal/engine/manager.go:345-360](./tools/agyswarm/internal/engine/manager.go#L345-L360)
- **Offending Code:**
In `cockpit.Run()`:
```go
	// Background keyboard reader in Cockpit
	go func() {
		buf := make([]byte, 16)
		for {
			n, err := os.Stdin.Read(buf)
            ...
			keyChan <- data
		}
	}()
```
In `Manager.Attach()`:
```go
	for {
		select {
		case <-done:
			return nil
		default:
			n, err := os.Stdin.Read(buf)
            ...
			if _, err := ptmx.Write(buf[:n]); err != nil {
				return nil
			}
		}
	}
```
- **Failure Mechanism:** When the user pressed `[Enter]` to attach to an agent pane, `Manager.Attach()` opened a read loop on `os.Stdin`. However, the background goroutine in `cockpit.Run()` was **never paused or stopped**. Both goroutines competed to read from the single file descriptor `os.Stdin`. Approximately half of the keystrokes were captured by the background cockpit thread and buffered into `keyChan`, while only the remaining half reached the agent PTY. Upon detaching with `Ctrl+]`, the queued keystrokes executed random commands in Cockpit navigation.

---

### Finding 3: [P1 - HIGH] PTY Stdout Split Between `bufio.Scanner` and `io.Copy`
- **File Reference:** [tools/agyswarm/internal/engine/manager.go:180-184](./tools/agyswarm/internal/engine/manager.go#L180-L184) & [tools/agyswarm/internal/engine/manager.go:334](./tools/agyswarm/internal/engine/manager.go#L334)
- **Offending Code:**
In `readPTYOutput`:
```go
func (m *Manager) readPTYOutput(id string, ptmx io.ReadWriteCloser, cmd *exec.Cmd) {
	scanner := bufio.NewScanner(ptmx)
	for scanner.Scan() {
		line := scanner.Text()
        ...
```
In `Attach`:
```go
	go func() {
		_, _ = io.Copy(os.Stdout, ptmx)
		close(done)
	}()
```
- **Failure Mechanism:** A child terminal process has a single pseudo-terminal master file descriptor (`ptmx`). `readPTYOutput` continuously scanned `ptmx` to update the Cockpit's recent log preview. When the user attached to the pane, `io.Copy(os.Stdout, ptmx)` was launched to stream child terminal output directly to the screen. Because both were reading concurrently from the same unbuffered stream, stream chunks were split unpredictably. The interactive terminal showed corrupted, half-missing lines.

---

### Finding 4: [P2 - MEDIUM] Flawed Symlink Isolation for Account Directories
- **File Reference:** [tools/agyswarm/internal/engine/manager.go:111-117](./tools/agyswarm/internal/engine/manager.go#L111-L117)
- **Offending Code:**
```go
		agentHome := filepath.Join(m.UserHome, ".gemini_swarm", "agents", acc)
		_ = os.MkdirAll(agentHome, 0755)
		agentGeminiLink := filepath.Join(agentHome, ".gemini")
		if _, err := os.Lstat(agentGeminiLink); err == nil {
			_ = os.Remove(agentGeminiLink)
		}
		_ = os.Symlink(accountDir, agentGeminiLink)
```
- **Failure Mechanism:** On Windows (without Developer Mode or elevated privileges), `os.Symlink` fails with `ERROR_PRIVILEGE_NOT_HELD`. Furthermore, concurrent workers spawned for the same account alias raced on removing and recreating the same symlink path (`agentGeminiLink`), leading to intermittent IO errors.

---

## 3. Why the Multi-Child Terminal Swarm Concept Failed

1. **Terminal Multiplexing is Already Solved:** Tools like `tmux`, `zellij`, and native IDE split terminals solve terminal management far better than a custom Go TUI. Reimplementing a terminal emulator inside a CLI tool introduced massive complexity (raw mode, ANSI escapes, resize signals, PTY allocation).
2. **LLM Agents Require Structured IPC, Not Raw PTYs:** Modern multi-agent architectures (such as AutoGen, CrewAI, or Antigravity Subagents) communicate via structured JSON messages, tools, and shared state, not by typing keystrokes into interactive Bash sessions.
3. **High Cognitive Overhead:** Users reported having no idea what `agyswarm` was doing, because watching 4 raw terminal shells execute concurrently in 6-line truncated viewports provides zero actionable insight compared to a structured dashboard.

---

## 4. Sunset & Deprecation Roadmap

```
Horizon 1 (Current - Sprint 1)        Horizon 2 (Sprint 2)             Horizon 3 (Final State)
┌─────────────────────────────────┐   ┌────────────────────────────┐   ┌────────────────────────────┐
│ • Move apps/agyswarm -> tools/  │   │ • Deprecate CLI flags      │   │ • Full archive to legacy   │
│ • Remove from master APPS list  │──>│ • Extract Markdown Dossier │──>│ • Native Antigravity agent │
│ • Keep buildable as legacy tool │   │   exporter into agyreview  │   │   delegation via agybot    │
└─────────────────────────────────┘   └────────────────────────────┘   └────────────────────────────┘
```

### Salvageable Components
| Component | Location | Destination / Purpose |
| :--- | :--- | :--- |
| **Markdown Exporter** | [tools/agyswarm/internal/markdown/exporter.go](./tools/agyswarm/internal/markdown/exporter.go) | Can be reused in `agyreview` for exporting audit dossiers. |
| **Heuristics Parser** | [tools/agyswarm/internal/engine/heuristics.go](./tools/agyswarm/internal/engine/heuristics.go) | Can be adapted in `agybot` to detect when a command is prompting for user input. |

### Recommended Replacement
Instead of standalone pseudo-terminal swarms, multi-agent workflows should be driven through:
- **`agybot`**: Top-level dispatcher with `/research` and autonomous task planning.
- **`agyreview`**: Multi-repo static analysis and autonomous PR remediation sentinel.
- **Antigravity CLI Subagents**: Native `invoke_subagent` and structured task delegation.
