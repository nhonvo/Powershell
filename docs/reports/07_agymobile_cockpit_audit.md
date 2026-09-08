# 📱 Deep Technical Audit: `agymobile` Mobile Cockpit & Remote Station (Go Engine)

> **Category**: Developer Suite Core Audit  
> **Target Subsystem**: `apps/agymobile`  
> **Source Files**: [apps/agymobile/main.go](../../apps/agymobile/main.go) · [apps/agymobile/internal/web/server.go](../../apps/agymobile/internal/web/server.go) · [apps/agymobile/internal/service/hostops/host.go](../../apps/agymobile/internal/service/hostops/host.go) · [apps/agymobile/internal/service/agentops/agent.go](../../apps/agymobile/internal/service/agentops/agent.go) · [apps/agymobile/internal/service/tailscaleops/tailscale.go](../../apps/agymobile/internal/service/tailscaleops/tailscale.go) · [apps/agymobile/internal/view/app.go](../../apps/agymobile/internal/view/app.go)  
> **Comparison Baseline**: [AGYMOBILE_PLAN.md](../../AGYMOBILE_PLAN.md) & [docs/01_architecture/agymobile_tailscale_ssh_plan.md](../../docs/01_architecture/agymobile_tailscale_ssh_plan.md)  
> **Windows UNC Reference**: `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\apps\agymobile`

---

## 1. Executive Summary & Architecture

`agymobile` provides remote mobile control over Antigravity AI agent sessions, host resources, and containers over Tailscale mesh networks:
1. **Mode A: 38-Column Portrait Terminal TUI**: Strictly bounded to $\le 38$ columns for smartphone terminals (Termux, ConnectBot, Termius) with number-row hotkeys (`1` through `9`).
2. **Mode B: Embedded Mobile Web Cockpit**: Serves a dark-mode mobile web interface on port 7890 with live progress bars and touch actions.
3. **Host Optimization**: 1-tap memory reclamation via kernel cache drop (`/proc/sys/vm/drop_caches`).
4. **Agent Telemetry**: Reads active AI agent session thoughts and steps from `~/.gemini/antigravity-cli/brain/*/transcript.jsonl`.

```
apps/agymobile/
├── go.mod                                   # Go 1.22.0, requires only x/term and x/sys
├── main.go                                  # CLI entrypoint (status, serve, flush)
└── internal/
    ├── model/
    │   └── mobile.go                        # MobileDashboard schemas
    ├── service/
    │   ├── agentops/
    │   │   └── agent.go                     # Agent transcript parser
    │   ├── hostops/
    │   │   └── host.go                      # Host RAM & drop_caches
    │   └── tailscaleops/
    │       └── tailscale.go                 # Tailscale IP & MagicDNS detector
    ├── view/
    │   └── app.go                           # 38-column portrait TUI (450 LOC)
    └── web/
        └── server.go                        # HTTP server & embedded HTML (337 LOC)
```

---

## 2. Critical Security Vulnerabilities & Code Defects

> [!CAUTION]
> **Severe Network Exposure & Remote Execution Risks**:

### 2.1 Unauthenticated Remote Endpoints
- **Location**: [apps/agymobile/internal/web/server.go lines 20–24](../../apps/agymobile/internal/web/server.go#L20-L24)
- **Defect**: Neither the web dashboard (`/`) nor the REST API endpoints (`/api/stats`, `/api/action/flush-ram`, `/api/action/docker-stop-all`) implement any authentication, session cookie, or bearer token check.

### 2.2 Broad Interface Binding (`0.0.0.0`)
- **Location**: [apps/agymobile/internal/web/server.go line 18](../../apps/agymobile/internal/web/server.go#L18)
```go
addr := fmt.Sprintf("0.0.0.0:%d", port)
```
- **Defect**: Binds to `0.0.0.0` rather than the Tailscale IP (`100.x.y.z`) or loopback (`127.0.0.1`).
- **Impact**: The dashboard and action endpoints are exposed to **all** network interfaces, including local Wi-Fi LANs, public hotspot networks, and WSL2 host bridges.

### 2.3 Unauthenticated Destructive POST Endpoint
- **Location**: [apps/agymobile/internal/web/server.go lines 63–77](../../apps/agymobile/internal/web/server.go#L63-L77)
```go
func handleDockerStopAll(w http.ResponseWriter, r *http.Request) {
    ...
    out, _ := exec.Command("docker", "ps", "-q").Output()
    ids := strings.Fields(string(out))
    if len(ids) > 0 {
        args := append([]string{"stop"}, ids...)
        _ = exec.Command("docker", args...).Run() // ⚠️ ANYONE CAN TERMINATE ALL CONTAINERS!
    }
}
```
- **Impact**: Any device on the same local network can trigger `POST /api/action/docker-stop-all` and kill all running containers on the developer workstation.

---

## 3. Implementation Reality vs. Architectural Blueprint (`AGYMOBILE_PLAN.md`)

| Blueprint Specification | [AGYMOBILE_PLAN.md](../../AGYMOBILE_PLAN.md) | Actual Code in `apps/agymobile` | Gap & Severity |
| :--- | :--- | :--- | :--- |
| **SSE Live Stream** | Server-Sent Events (`/api/events`) | Client-side `setInterval(fetchStats, 3000)` | ⚠️ **Medium**: Inefficient 3s AJAX polling instead of live push. |
| **WebSockets & Web PTY** | In-browser xterm.js terminal via WebSocket | Absent (rely on native SSH clients) | ❌ **High**: Cannot run terminal commands from phone browser. |
| **ANSI QR Code Generator** | QR code in terminal for 1-tap phone pairing | `internal/service/tailscaleops/qr.go` missing | ❌ **High**: Planned QR code generator was not implemented. |
| **Web Asset Management** | Dedicated files in `internal/web/assets/` | Hardcoded 230-line string in `server.go` | ⚠️ **Medium**: Hard to maintain, no CSS minification or caching. |
| **PWA Compliance** | `manifest.json` and service worker (`sw.js`) | Plain HTML page | ⚠️ **Low**: Operates as a bookmark, not an installable PWA. |
| **Two-Way Agent Prompting**| Queue steering prompts from phone | Read-only transcript monitor | ❌ **High**: Cannot steer agent tasks while away from desk. |

---

## 4. Prioritized Action Plan & Next Steps

### Phase 1: Security Lockdown (Immediate Priority)
1. **Bind Strictly to Tailscale IP**:
   - Update `StartServer()` to resolve the Tailscale IPv4 address (`tsInfo.IPv4`) and bind exclusively to that interface:
   ```go
   bindIP := tsInfo.IPv4
   if bindIP == "" || bindIP == "127.0.0.1" {
       bindIP = "127.0.0.1"
   }
   addr := fmt.Sprintf("%s:%d", bindIP, port)
   ```
2. **Bearer Token Authentication**:
   - Generate a random 32-character token on first run, saved to `~/.config/antigravity/mobile_token.secret`.
   - Require `?token=` query parameter or `Authorization: Bearer <token>` header on all requests.

### Phase 2: Feature Parity with Blueprint (Short-Term)
1. **Implement Terminal QR Code Generator**:
   - Add `internal/service/tailscaleops/qr.go` using `skip2/go-qrcode` to render an ANSI QR code pairing terminal clients to `ssh://<user>@<tailscale-ip>`.
2. **Migrate to Server-Sent Events (SSE)**:
   - Implement `/api/events` with `text/event-stream` pushing updates immediately when host memory changes or agent steps complete.
3. **Extract Assets via `//go:embed`**:
   - Move HTML, CSS, and JS into `internal/web/assets/` and use Go 1.16+ `embed.FS`.

### Phase 3: Advanced Mobile Capabilities (Medium-Term)
1. **Web Terminal via WebSocket**:
   - Implement `/ws/terminal` using `creack/pty` and `gorilla/websocket` with xterm.js in the browser.
2. **Remote Agent Steering Prompt**:
   - Add an input card allowing developers to send instructions or answers to waiting subagents from their phone.
