# 📱 AGYMOBILE: Mobile Terminal & PWA Cockpit for Antigravity (Tailscale & SSH)

> **Antigravity Mobile Companion (Go Engine)**  
> Ultra-Compact 36-Col Portrait TUI · Lightweight Embedded Mobile PWA · Tailscale Mesh & Resilient SSH Tunneling

---

## 1. Executive Summary & Problem Statement

### The Problem
When developers manage Antigravity workspaces and AI agent workflows remotely from smartphones or tablets (such as over **Tailscale** private mesh VPN):
1. **Screen Layout Mismatch**: Standard terminal interfaces and desktop TUIs require 80–120 columns. On mobile devices in portrait mode, terminals only have **36–45 columns**, causing extreme text wrapping, truncated tabular columns, and illegible output.
2. **Virtual Keyboard Friction**: On-screen mobile keyboards lack dedicated keys (`Esc`, `Ctrl`, `Alt`, arrows) or bury them in submenus. Typing long commands like `agyswitch account switch` or `docker compose restart` on touchscreen is slow and error-prone.
3. **Network Drops on Cellular Handoff**: Moving between cellular (5G/LTE) and Wi-Fi breaks active TCP/SSH connections, terminating running foreground tasks unless wrapped in persistent session managers.
4. **Desktop Monitoring on the Go**: Developers away from their workstation need quick answers to three critical questions:
   - *Is my long-running Antigravity AI subagent finished? What did it conclude?*
   - *Did WSL2 or Docker run out of RAM and freeze the machine?*
   - *What is my active AI account token quota balance?*

### The Solution: `agymobile`
`agymobile` is a purpose-built dual-mode mobile station designed specifically for **Tailscale + SSH remote development**:
- **Mode A: Ultra-Compact Portrait TUI (`agymobile` / `agymobile tui`)**: Formatted strictly for 36–42 column smartphone screens with large numbered quick-actions (1–9) directly accessible on mobile keyboard number rows.
- **Mode B: Zero-Dependency Embedded Mobile Web Cockpit (`agymobile serve`)**: High-performance, lightweight PWA served directly by the Go binary over Tailscale (`100.86.177.57:7890`), providing touch buttons, haptic feedback, live agent monitoring via SSE, and 1-tap controls.
- **Mode C: Tailscale & SSH Station Manager (`agymobile setup`)**: Automatic Tailscale SSH detection, QR code generation for 1-tap mobile terminal connection, and resilient tmux/mosh session wrappers.

---

## 2. System Architecture

```
                                  Tailscale Encrypted Mesh
  📱 Mobile Device (Android / iOS) ═════════════════════════╗
  ├── Termux / ConnectBot / Termius (SSH)                  ║
  └── Mobile Safari / Chrome PWA (Web UI)                   ║
                                                            ║
                                                            ▼
┌────────────────────────────────────────────────────────────────────────┐
│ WSL2 / Linux Host (100.86.177.57 · truongnhon-1)                       │
│                                                                        │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                    AGYMOBILE (Go Engine)                         │  │
│  ├────────────────────────────────┬─────────────────────────────────┤  │
│  │ Mode 1: Mobile Portrait TUI    │ Mode 2: Mobile Web & PWA        │  │
│  │ • 36–42 Col Adaptive Layout    │ • Embedded Go net/http (No Node)│  │
│  │ • Number-Row Hotkeys (1-9)     │ • Touch-First Responsive PWA    │  │
│  │ • Zero ANSI Horizontal Clip    │ • Server-Sent Events (SSE) Live │  │
│  │ • Tmux/Mosh Session Guard      │ • Tailscale-Bound Auth (:7890)  │  │
│  └────────────────┬───────────────┴─────────────────┬───────────────┘  │
│                   │                                 │                  │
│                   ▼                                 ▼                  │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │               Antigravity Core Suite Integration                 │  │
│  │  • agyswitch: Quota balances, active vault profile, token burn   │  │
│  │  • agydocker: 1-Tap RAM flush, container start/stop/restart      │  │
│  │  • agygit:    Quick branch sync, status summary, commit review   │  │
│  │  • agyx:      Agent trajectory transcripts, live subagent states │  │
│  │  • Host OS:   WSL2 memory (/proc/meminfo), load avg, uptime      │  │
│  └──────────────────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 3. UI/UX Design & Mockups

### Mode A: 38-Column Portrait Terminal TUI Mockup

Designed to display cleanly on small screens without line breaks or clipping:

```
┌──────────────────────────────────────┐
│ 📱 AGYMOBILE · Remote Cockpit (WSL)  │
│ 🌐 Tailscale: 100.86.177.57 [Online] │
├──────────────────────────────────────┤
│ 🧠 RAM: 4.2 / 15.6 GB (27% · Normal) │
│ 👤 AI: Work Pro ($3.98 · 42k tokens) │
│ 🐳 Docker: 2 / 19 Up (dev-tools)     │
│ 🤖 Agent: 1 Active (Subagent #4)     │
├──────────────────────────────────────┤
│ ⚡ QUICK ACTIONS (Press 1-9):         │
│                                      │
│  [1] 🤖 AI Agents & Live Tasks       │
│  [2] 👤 Switch AI Account            │
│  [3] 🐳 Docker Containers & Compose  │
│  [4] 🧠 Flush WSL2 RAM (Drop Cache)  │
│  [5] 🐙 Git Status & Sync            │
│  [6] 📁 Open Workspace in Tmux       │
│  [7] 📲 Tailscale QR & Remote Info   │
│  [8] 🌐 Start Mobile Web UI (:7890)  │
│  [9] 📜 Recent Agent Transcript Log  │
├──────────────────────────────────────┤
│ [r] Refresh · [q] Exit · [?] Help    │
└──────────────────────────────────────┘
```

### Sub-View Example: 38-Column Container Manager
```
┌──────────────────────────────────────┐
│ 🐳 DOCKER CONTAINERS (19 Total)      │
├──────────────────────────────────────┤
│ 1. [UP]  dev_tools_pgadmin           │
│ 2. [UP]  dev_tools_mongo_express     │
│ 3. [EXT] inventory-ui-dev            │
│ 4. [EXT] inventory-api-dev           │
│ 5. [EXT] binhdinhfood_postgres       │
├──────────────────────────────────────┤
│ ▶ Selected: dev_tools_pgadmin        │
│   Ports: 5050->80/tcp                │
│                                      │
│ [s] Toggle · [a] Stack · [l] Logs    │
│ [n/p] Page 1/4 · [b] Back            │
└──────────────────────────────────────┘
```

### Mode B: Mobile Web PWA Cockpit Mockup (HTML5 / CSS Touch Interface)

When accessed via phone browser at `http://100.86.177.57:7890`:
- **Dark Mode Modern Aesthetic**: Sleek Obsidian/Emerald theme matching Antigravity CLI.
- **Top Status Bar**: Live Tailscale ping, WSL2 RAM progress bar, active AI profile badge.
- **Touch Action Cards**:
  - `[ 🚀 1-Tap RAM Clean ]`: Frees cached Linux kernel buffer memory instantly.
  - `[ 🔄 Switch Account ]`: Dropdown to toggle Personal / Work accounts with 1 tap.
  - `[ 🐳 Containers ]`: Simple card toggles to Start/Stop stacks with status pills (Green `Up`, Red `Exited`).
  - `[ 🤖 Live Agent Stream ]`: Real-time text stream of current Antigravity agent thoughts and recent commands.
  - `[ 💬 Quick Prompt ]`: Input field with microphone/dictation support to queue commands directly to the agent.

---

## 4. Key Feature Matrix

| Feature | Mobile TUI (`agymobile`) | Web PWA (`:7890`) | Description |
| :--- | :---: | :---: | :--- |
| **Tailscale Auto-Detection** | ✅ | ✅ | Discovers Tailscale IPv4/IPv6, MagicDNS, and node status. |
| **38-Col Portrait Adaptation** | ✅ | N/A | Guaranteed zero text clipping on smartphone terminals. |
| **Number-Row Hotkeys** | ✅ | N/A | All major actions bound to `1` through `9` for touchscreen keyboards. |
| **WSL2 RAM Emergency Guard** | ✅ | ✅ | Real-time RAM monitor and 1-tap memory reclamation (`drop_caches`). |
| **Container & Stack Control** | ✅ | ✅ | Start, stop, and restart individual containers or compose stacks. |
| **AI Vault & Quota Switching** | ✅ | ✅ | Seamless integration with `agyswitch` profiles and token balances. |
| **Agent Trajectory Monitor** | ✅ | ✅ | Read live steps and outputs from ongoing AI conversations. |
| **Tailscale Connect QR Code** | ✅ | ✅ | Generates ANSI QR code in terminal for instant mobile SSH pairing. |
| **Mosh & Tmux Integration** | ✅ | N/A | Spawns or attaches to persistent remote sessions resilient to cell drops. |
| **Zero External Runtime** | ✅ | ✅ | Single static Go binary without requiring Node.js, Python, or npm. |

---

## 5. Tailscale & SSH Connection Workflow

### 1. Connecting via Tailscale SSH (Direct Terminal)
1. Ensure Tailscale is running on the workstation (`tailscale status`).
2. On the mobile device (Termux, Termius, or ConnectBot), run:
   ```bash
   ssh truongnhon@100.86.177.57
   # or with MagicDNS:
   ssh truongnhon@truongnhon-1
   ```
3. Launch mobile cockpit:
   ```bash
   agymobile
   ```

### 2. Connecting via Embedded Web Cockpit
1. Start the mobile web server:
   ```bash
   agymobile serve
   # Binds to 100.86.177.57:7890 (Tailscale mesh only)
   ```
2. Open mobile browser to:
   `http://100.86.177.57:7890` (or `http://truongnhon-1:7890`)
3. Tap "Add to Home Screen" to install as a standalone PWA.

---

## 6. Project Directory Structure (`apps/agymobile`)

```text
apps/agymobile/
├── cmd/
│   └── agymobile/
│       └── main.go                 # CLI entrypoint (subcommands: tui, serve, status, qr)
├── internal/
│   ├── model/
│   │   └── mobile.go               # Mobile dashboard & host stats models
│   ├── service/
│   │   ├── tailscaleops/
│   │   │   ├── tailscale.go        # Tailscale CLI wrapper & IP/MagicDNS resolver
│   │   │   └── qr.go               # QR code generator for SSH connection string
│   │   ├── hostops/
│   │   │   └── host.go             # WSL2 RAM, load avg, and drop_caches executor
│   │   └── agentops/
│   │       └── agent.go            # Reads transcript.jsonl & active subagents
│   ├── web/
│   │   ├── server.go               # Pure Go HTTP server with SSE stream
│   │   └── assets/
│   │       ├── index.html          # Responsive touch-first mobile dashboard
│   │       ├── app.js              # SSE client & touch event handlers
│   │       └── style.css           # Modern dark-mode mobile styling
│   └── view/
│       ├── app.go                  # 38-column adaptive TUI engine
│       └── view_test.go            # Portrait rendering unit tests
├── go.mod
└── main.go                         # Root package launcher
```

---

## 7. Implementation Milestones

- [ ] **Phase 1**: Architecture blueprint document & user approval (`AGYMOBILE_PLAN.md`).
- [ ] **Phase 2**: Scaffolding `apps/agymobile` Go module and domain models.
- [ ] **Phase 3**: Host, Tailscale, Docker, and Agent inspection services.
- [ ] **Phase 4**: 38-column compact mobile TUI engine with number-row hotkeys.
- [ ] **Phase 5**: Embedded mobile Web PWA server with live SSE metrics.
- [ ] **Phase 6**: Master Makefile integration (`make mobile`) and end-to-end verification.
