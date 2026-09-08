# 🛸 Deep Technical Audit: `agyswitch` (Go Engine)

> **Category**: Developer Suite Core Audit  
> **Target Subsystem**: `apps/agyswitch`  
> **Source Files**: [apps/agyswitch/main.go](../../apps/agyswitch/main.go) · [apps/agyswitch/internal/service/store/store.go](../../apps/agyswitch/internal/service/store/store.go) · [apps/agyswitch/internal/service/vault/vault.go](../../apps/agyswitch/internal/service/vault/vault.go) · [apps/agyswitch/internal/service/sessions/sessions.go](../../apps/agyswitch/internal/service/sessions/sessions.go) · [apps/agyswitch/internal/view/app.go](../../apps/agyswitch/internal/view/app.go)  
> **Comparison Baseline**: C# `AgyAccountStore.cs` & `AgyQuotaEngine.cs`  
> **Windows UNC Reference**: `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\apps\agyswitch`

---

## 1. Executive Summary & Component Architecture

`agyswitch` serves as the core identity, vault, quota manager, and session orchestrator of the Antigravity developer suite. It provides:
1. **Multi-Account Context Switching**: Mirrors configuration and credentials between isolated account directories (`~/.gemini_<account>`) and the active runtime context (`~/.gemini`).
2. **Encrypted Token Vault**: Uses AES-256-GCM cipher with PBKDF2 key derivation to safeguard Google OAuth refresh tokens.
3. **Live Quota Engine**: Queries Google CloudCode internal APIs to inspect remaining token quotas across Gemini and Claude models with rolling expiration countdowns.
4. **Agent Sessions & Trajectories**: Discovers Antigravity conversation sessions, step metrics, and calculates estimated inference costs.
5. **Rules & Skills Sync**: Discovers global and workspace-scoped rules and custom skills, synchronizing them across accounts.
6. **Mobile Web Sidecar**: Embedded HTTP API on `:8080` for remote status telemetry and context switching.

```
apps/agyswitch/
├── main.go                       # CLI command router & interactive TUI dispatcher
├── launcher/
│   ├── launcher.go               # Process runner, env scrubber, post-run token sync
│   └── launcher_test.go          # Launcher unit tests
└── internal/
    ├── model/
    │   └── model.go              # Data schemas (AccountInfo, QuotaSummary, SessionInfo)
    ├── service/
    │   ├── quota/                # ⚠️ CRITICAL: EMPTY DIRECTORY (0 files)
    │   ├── rules/                # Rules discovery & MCP server config parser
    │   ├── seeder/               # Canonical template provisioning (~/.gemini_template)
    │   ├── server/               # Mobile Web Sidecar HTTP server (:8080)
    │   ├── sessions/             # Brain log scanner & python-sqlite query engine
    │   ├── skills/               # Skill discovery (~/.gemini/skills, .agents/skills)
    │   ├── store/                # Account registry, directory mirroring & live quota engine
    │   └── vault/                # PBKDF2/AES-256-GCM encryption & Google OAuth refresh
    └── view/
        ├── app.go                # Raw ANSI VT100 terminal interactive TUI (1,198 LOC)
        └── app_test.go           # Render smoke tests
```

---

## 2. Deep-Dive Findings & Code Defects

### 2.1 Architectural Anomaly: Empty `internal/service/quota` Directory
- **Path**: [apps/agyswitch/internal/service/quota](../../apps/agyswitch/internal/service/quota)
- **Defect**: The directory is completely empty (0 bytes, 0 files).
- **Root Cause**: During development, all quota logic (`ProbeQuotaStatus`, `FetchUserQuotaSummary`, `ExtractGroupQuotas`, `ExtractDetailedModelBuckets`) was implemented directly inside [apps/agyswitch/internal/service/store/store.go](../../apps/agyswitch/internal/service/store/store.go#L340-L655).
- **Remediation**: Extract all quota probing, API fetching, and bucket parsing out of `store.go` into `internal/service/quota/quota.go` to restore package boundary integrity.

### 2.2 Brittle External Runtime: Python Subprocess in `sessions.go`
- **Location**: [apps/agyswitch/internal/service/sessions/sessions.go line 131](../../apps/agyswitch/internal/service/sessions/sessions.go#L131)
```go
cmd := exec.Command("python3", "-c", script, dbPath)
out, err := cmd.Output()
```
- **Defect**: Instead of querying Antigravity's `conversation_summaries.db` SQLite database using a native Go SQLite driver, `sessions.go` executes an embedded inline Python script via `exec.Command("python3", ...)`.
- **Impact**:
  1. **Latency**: Spawns an external Python runtime process incurring a 50ms–120ms startup penalty on every session inspection.
  2. **Dependency Risk**: Fails completely on environments where `python3` is not installed or not in `$PATH`.
- **Remediation**: Replace with pure-Go, zero-CGO SQLite driver (`modernc.org/sqlite`) to query `conversation_summaries.db` natively in sub-millisecond memory.

### 2.3 Mocked / Stubbed MCP Server Healthchecks
- **Location**: [apps/agyswitch/internal/service/rules/rules.go lines 103–108](../../apps/agyswitch/internal/service/rules/rules.go#L103-L108)
```go
status := model.MCPServerStatus{
    ServerName: name,
    Command:    cmdStr,
    IsRunning:  true,
    LatencyMs:  12, // ⚠️ HARDCODED MOCK DATA
}
```
- **Defect**: MCP server status is completely hardcoded. It marks all parsed MCP servers as `IsRunning: true` with a static `LatencyMs: 12`.
- **Impact**: When an MCP server crashes or misconfigures, `agyswitch` falsely displays `● Connected` in green.
- **Remediation**: Implement an active probe: check if the MCP process is alive in the process table or perform a lightweight JSON-RPC `{"jsonrpc":"2.0","method":"ping","id":1}` handshake via standard I/O pipes.

### 2.4 Cryptographic Posture: Static Entropy Salt in Vault
- **Location**: [apps/agyswitch/internal/service/vault/vault.go lines 25–31](../../apps/agyswitch/internal/service/vault/vault.go#L25-L31)
```go
const SaltString = "AgySwitch_Secure_Entropy_v2"
```
- **Defect**: Uses a static, compile-time string as the salt for PBKDF2 key derivation across all users and all machines.
- **Comparison with C#**: C# `AgyVault` used Windows DPAPI (`ProtectedData.Protect` tied to user OS credentials) or derived dynamic machine-level seeds (`$"{Environment.UserName}@{Environment.MachineName}"`).
- **Remediation**: Derive key entropy dynamically from `/etc/machine-id` (or `/var/lib/dbus/machine-id`) combined with the active Linux user UID.

### 2.5 UI Framework Reality: Raw ANSI Event Loop vs Bubbletea
- **Location**: [apps/agyswitch/internal/view/app.go](../../apps/agyswitch/internal/view/app.go)
- **Defect**: `agyswitch` does **not** use Charm Bubbletea or Lipgloss. It is a 1,198-line custom ANSI terminal application using `golang.org/x/term`.
- **Consequences**:
  1. **Main Thread Blocking**: Network calls (such as pressing `R` to refresh live quotas across all accounts) run synchronously or block the main input loop until `store.ListAccounts()` completes.
  2. **No Visual Spinner Feedback**: Users experience a frozen screen during multi-account network requests.
  3. **No Window Resize (`SIGWINCH`) Handler**: Resizing the terminal window during an active session does not adapt the layout until the next keypress triggers `term.GetSize()`.

---

## 3. Comparison with Legacy C# System (`AgyAccountStore` & `AgyQuotaEngine`)

| Feature | Legacy C# `AgyTui` | Go `agyswitch` | Parity Status & Verdict |
| :--- | :--- | :--- | :--- |
| **Storage Architecture** | Centralized SQLite database (`agytui.db`) with schema migrations V1–V7. | Flat JSON registry (`agyswitch_accounts.json`) + directory mirrors. | 🟡 **Trade-off**: Go avoids SQLite database locks; C# had ACID transactional guarantees. |
| **Credential Encryption** | Windows DPAPI (`ProtectedDataScope.CurrentUser`) + entropy. | AES-256-GCM + PBKDF2 with static salt. | ⚠️ **Degraded in Go**: No Windows DPAPI integration on Windows hosts. |
| **Keyring Interop** | Win32 native P/Invoke (`advapi32.dll` `CredReadW`, `CredWriteW`). | File-based token storage (`keyring_token.txt`) + `cmdkey.exe`. | ⚠️ **Degraded in Go**: C# had direct Win32 credential isolation. |
| **Live Quota Engine** | Local calculation from `ai_activity_log.jsonl` with hardcoded thresholds. | Real-time live Google CloudCode API (`retrieveUserQuotaSummary`). | 🟢 **Superior in Go**: Accurate per-model buckets and reset countdowns. |
| **Auto Quota Failover** | `AutoSwitchOnQuotaExceeded()` switches account upon 429 automatically. | Manual account switch or CLI `launch-quota`. | ❌ **Missing in Go**: No automatic runtime failover during agent execution. |
| **Commit Protection** | `IsNoAutoCommitEnabled()` injects `--no-auto-commit` to prevent AI commits. | Omitted in launcher. | ❌ **Missing in Go**: AI agent can commit unwanted code. |
| **Startup Overhead** | 200ms–500ms (.NET CLR JIT compilation). | < 15ms (native Go binary). | 🟢 **Massive Go Win**: Instantaneous execution. |

---

## 4. Prioritized Action Plan & New Features

### Phase 1: Engine Hardening (Immediate)
1. **Refactor Quota Service**:
   - Create [apps/agyswitch/internal/service/quota/quota.go](../../apps/agyswitch/internal/service/quota) and migrate `FetchUserQuotaSummary`, `ProbeQuotaStatus`, `ExtractGroupQuotas`, and `ExtractDetailedModelBuckets` from `store.go`.
2. **Native SQLite for Sessions**:
   - Replace `exec.Command("python3", ...)` in [sessions.go](../../apps/agyswitch/internal/service/sessions/sessions.go#L131) with `modernc.org/sqlite` queries.
3. **Reintroduce `--no-auto-commit` Guard**:
   - In [apps/agyswitch/launcher/launcher.go](../../apps/agyswitch/launcher/launcher.go), add an argument flag `--no-auto-commit` toggleable in the Vault TUI tab.

### Phase 2: Loading & UI Modernization (Short-Term)
1. **Async Non-Blocking Quota Refresh**:
   - Run live quota fetching in background goroutines communicating via channels, rendering an animated ANSI spinner (`⠋ ⠙ ⠹ ⠸ ⠼ ⠴ ⠦ ⠧ ⠇ ⠏`) in the status footer.
2. **Terminal Resize Handling**:
   - Register a `os/signal` channel listening on `syscall.SIGWINCH` to trigger immediate frame re-renders when the terminal window is resized.

### Phase 3: High-Value New Features (Medium-Term)
1. **Automated Runtime Quota Failover**:
   - Wrap `agy` executions with exit code / output interception. When a `429 Rate Limit` or quota exhaustion error is detected, automatically trigger `SelectBestQuotaAccount()` and prompt: `[agyswitch] Quota depleted on account A. Press Enter to resume under account B...`.
2. **Real-Time Token Burn Rate Graph**:
   - Render a mini ASCII Sparkline chart (` ▂▃▅▇`) in Tab 3 showing hourly token consumption and cost accrual per project.
