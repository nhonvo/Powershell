# 🚀 AGYBOT: Proxy Integration, Advanced Features & Operator Guide

## 📌 Document Metadata
- **Application:** `agybot` (9th Micro-App) & `agyx` (Master Proxy Orchestrator)
- **Primary Source Code:** [apps/agybot](./apps/agybot) and [apps/agyx](./apps/agyx)
- **Binary Targets:** `~/.local/bin/agybot` and `~/.local/bin/agyx`
- **Config Storage:** `~/.config/antigravity/bot.env` (highest priority) and `.env`
- **Daemon Files:** `~/.config/antigravity/agybot.pid` and `~/.config/antigravity/agybot.log`
- **Dual Link Reference:**
  - **VS Code Clickable (Recommended):** [AGYBOT_PROXY_AND_ADVANCED_FEATURES_GUIDE.md](./AGYBOT_PROXY_AND_ADVANCED_FEATURES_GUIDE.md)
  - **Windows UNC Path:** `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\AGYBOT_PROXY_AND_ADVANCED_FEATURES_GUIDE.md`

---

## 1. Unified Proxy Integration (`agyx bot`)

`agybot` is registered as a first-class citizen in the **`agyx` Master Proxy & Orchestrator**. You can invoke any bot function directly via `agyx`:

```bash
# Display AgyBot Cockpit Overview via proxy
agyx bot

# Background Daemon Lifecycle Controls via proxy
agyx bot start             # Start detached Telegram server daemon
agyx bot status            # Check live daemon state, PID & host telemetry
agyx bot logs [n]          # Tail the last n log lines (default 30)
agyx bot restart           # Gracefully restart the background daemon
agyx bot stop              # Terminate background daemon process

# Check or update configuration via proxy
agyx bot config
agyx bot config set DEFAULT_MODEL "Claude Sonnet 4.6"

# List recent Antigravity AI brain sessions via proxy
agyx bot sessions
```

### Registered Aliases in `agyx`:
- `agyx bot`
- `agyx b`
- `agyx telegram`
- `agyx tg`
- `agyx agentbot`

---

## 2. Interactive TUI Master Cockpit (`agyx`) Integration

`agyx` includes `agybot` in its full-screen interactive TUI Cockpit:

1. Launch master cockpit:
   ```bash
   agyx
   ```
2. Press `[6]` to navigate to the **🛠️ Tools Drawer** (Utilities & Cloud Services).
3. The drawer displays all four developer utilities:
   - `[T] 🎨 Terminal Themes & Fonts (agyterm)`
   - `[M] 📱 Mobile Cockpit & Remote Station (agymobile)`
   - `[A] ☁️ AWS Cloud & LocalStack Diagnostics (aws)`
   - `[B] 🤖 Antigravity Telegram Controller & Daemon (agybot)`
4. **Live Daemon State Indicator:** Item `[B]` renders real-time daemon state:
   - `🟢 Running (PID: <pid>) · Press [S] to Stop`
   - `⚪ Stopped · Press [S] to Start`
5. **Interactive Controls & Hotkeys in Tab 6:**
   - **`[↑/↓]` or `[j/k]`**: Move selection between utilities.
   - **`[b] / [B]`**: Jump directly to `agybot`.
   - **`[s] / [S]`**: Toggle background daemon (Starts if stopped, stops if running) with live status banner.
   - **`[Enter]`**: Launch full interactive `agybot` console overview.
   - **`[Q] / [Esc]`**: Exit cockpit.

---

## 3. Background Daemon Lifecycle (`agybot start | stop | restart | logs | status`)

The `agybot` daemon is engineered to run decoupled from the active terminal, allowing it to survive terminal disconnections and WSL2 background lifecycle:

### How It Works:
- **Detached Session:** Uses Linux `syscall.SysProcAttr{Setsid: true}` so terminal closure does not send `SIGHUP`.
- **Process ID Tracking:** Persists PID to `~/.config/antigravity/agybot.pid`. Probing uses `syscall.Signal(0)` to verify the process is truly alive and cleans up stale PID files automatically.
- **Detached Output:** All standard output and error streams are redirected to `~/.config/antigravity/agybot.log`.
- **Graceful Teardown:** Sends `SIGTERM` first, allowing Telegram bot long-polling loops to close cleanly, followed by a 3-second timeout and force-kill fallback if needed.

### CLI Daemon Commands:
```bash
# 1. Start the daemon in the background
agybot start

# 2. Probe daemon status and host telemetry
agybot status

# 3. Stream real-time logs
agybot logs 50

# 4. Restart daemon after updating bot.env
agybot restart

# 5. Stop the background daemon
agybot stop
```

---

## 4. CLI Configuration Management (`agybot config`)

Configuration can be viewed, updated, and validated from the command line without opening text editors. Settings are persisted directly to `~/.config/antigravity/bot.env` with strict permissions (`0600`).

```bash
# 1. View all configuration keys and source paths
agybot config

# 2. Update a configuration setting
agybot config set DEFAULT_EFFORT high
agybot config set DEFAULT_MODEL "Gemini 3.7 Flash"
agybot config set TASK_TIMEOUT 900
agybot config set AUTO_LOCK_MINUTES 45

# 3. Manage Allowed Telegram User Whitelist
agybot config whitelist list
agybot config whitelist add 8343607963
agybot config whitelist rm 123456789

# 4. Generate a new Scrypt PIN Hash for security authentication
agybot set-pin 987654
```

---

## 5. Multi-Project Creation & Deep Management

`agybot` allows developers to create, scaffold, and switch between codebases on the fly, directly synchronized with `agyproj` (`~/.config/antigravity/projects.json`).

### 5.1 Create Projects from Telegram
Send command to the bot in Telegram:
```text
/newproj payment-gateway go
```
**Supported Stacks:**
- `go`: Scaffolds `go.mod`, `main.go`, `README.md`, and runs `git init`.
- `node` / `ts` / `react`: Scaffolds `package.json`, `README.md`, and runs `git init`.
- `python` / `py`: Scaffolds `requirements.txt`, `main.py`, `README.md`, and runs `git init`.
- `dotnet` / `csharp`: Prepares `.NET` structure with `README.md` and runs `git init`.

*Result:* The project is created under `/home/truongnhon/projects/<name>`, registered in `agyproj`, and your active Telegram session switches to it automatically.

### 5.2 Create Projects from CLI
```bash
agybot newproj analytics-service go
```

### 5.3 Switch Between Workspaces
- In Telegram: `/cd finance-dashboard` or `/proj` (interactive list)
- Quick Shortcut: `/finance` (locks context to `finance-dashboard`)
- In CLI: `agybot projects`

---

## 6. Deep Research Engine (`/research`)

In addition to direct coding tasks, `agybot` supports deep technological, architectural, and codebase research powered by Google Antigravity AI (`Gemini 3.7 Flash` / `Claude Sonnet 4.6`).

### 6.1 From Telegram
Send the `/research` command with your topic or question:
```text
/research Best practices for distributed locking in PostgreSQL and Redis under high-concurrency fintech workloads
```

**Antigravity Autonomous Research Pipeline:**
1. **Autonomous Investigation:** Examines technical documentation, web sources, and code examples.
2. **Structured Analysis:** Formats the output into:
   - *Executive Summary*
   - *Architectural Analysis & Key Patterns*
   - *Trade-offs, Failure Modes & Risk Matrix*
   - *Concrete Implementation Steps*
   - *Strategic Recommendations*
3. **Live Streaming Progress:** Live updates (`🔍 Inspecting codebase...`, `🌐 Web research...`) stream directly into the Telegram message bubbles.

### 6.2 From CLI
```bash
agybot research "Comparative audit of gRPC vs Connect-RPC for Go microservices"
```

---

## 7. Multi-Session & Conversation Continuity

`agybot` provides access to the historical and active conversation brain of Google Antigravity (`~/.gemini/antigravity-cli/brain/`).

### 7.1 Telegram Session Commands

| Command | Action & Purpose |
| :--- | :--- |
| **`/sessions`** | Lists the 8 most recent sessions with timestamp, step count, and estimated cost. |
| **`/resume <conversation_id>`** | Resumes an existing conversation. All subsequent prompts will continue with the previous context intact. |
| **`/session`** | Displays the active conversation ID, current workspace, model, and active account. |
| **`/new`** or **`/reset`** | Starts a clean, unlinked conversation session. |

### 7.2 CLI Session Commands
```bash
# List all recent AI sessions across all workspaces
agybot sessions

# Resume a specific session and pass a follow-up prompt
agybot resume 566def5b-8195-4e61-845b-52eb08b52b16 "Refactor the authentication middleware to use scrypt"
```

---

## 8. Complete Telegram Command Matrix

```text
── Navigation & Projects ──────────────────────────────────────────
/proj, /projects           List all registered workspaces in agyproj
/cd <project_or_path>      Switch active workspace
/newproj <name> [stack]    Create and scaffold new project repository
/finance                   Quick switch to finance-dashboard workspace
/ls [subpath]              Explore workspace directory files
/view <file>, /cat <file>  Read text file contents inside workspace

── AI Engine & Research ───────────────────────────────────────────
<any text prompt>          Dispatch coding prompt to Google Antigravity
/research <topic>          Conduct deep architectural & library research
/sessions                  List recent Antigravity conversation sessions
/resume <conv_id>          Resume previous AI session context
/session                   Show active conversation ID and settings
/new, /reset               Start fresh AI conversation session
/models                    List available Google Antigravity AI models

── Account Context & Quota (agyswitch) ────────────────────────────
/account                   Inspect active account, tokens & rolling quota
/switch <account_name>     Switch active Antigravity account context

── Security & Host Health ─────────────────────────────────────────
/status                    View host CPU, RAM, Disk & Docker health
/config                    View current AgyBot system configuration
/pin, /unlock              Submit security PIN to unlock controller
/lock                      Lock controller session immediately
```

---

## 9. Verification & Build Commands

```bash
# Recompile agybot and agyx
make bot
make proxy

# Execute all 9 unit test suites across the monorepo
make test
```
