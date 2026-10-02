# Deep Architectural Brainstorm: Cross-Account Multi-Agent Swarm & Headless Telegram Markdown Gateway

**Date:** October 3, 2026  
**Platform Base:** Antigravity Suite (`agyswitch` v2.0 & `agyproj` v2.0)  
**Deliverable File:** Pure Markdown Design Document

---

## Executive Summary & Foundation

The current Antigravity Developer Suite has established two mission-critical pillars:
1. **`agyswitch`**: Manages the multi-account credential vault, live CloudCode quota telemetry, token rotating, and session trajectory history.
2. **`agyproj`**: Manages workspace discovery, git tracking, project metadata registry, and IDE environment orchestration.

### The Two Missing Capabilities
To scale from single-operator CLI sessions into an autonomous, remotely controllable intelligence network, two major capabilities are missing:
1. **Cross-Account Multi-Agent Collaboration & Deep Web Research Engine (`agyswarm`)**: Subagents currently run sequentially under a single Google account and single session. When complex web research or heavy coding tasks are initiated, quota is drained rapidly and subagents cannot exchange context across different sessions or accounts.
2. **Headless Telegram Conversational Gateway with Pure Markdown Outputs (`agytg`)**: A mobile/remote interface that enables commanding Antigravity from Telegram, with automatic quota-aware account rotation, workspace binding, and a strict **Markdown-Only Artifact Pipeline** (bypassing Telegram chat character limits by generating and sending structured `.md` files directly).

```
                      ┌──────────────────────────────────────────┐
                      │              Telegram User               │
                      └────────────────────┬─────────────────────┘
                                           │ Mobile prompts & queries
                                           ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                        PROJECT 2: agytg (Telegram Markdown Gateway)                     │
│  • Headless Daemon & Telegram Long-Polling / Webhook                                   │
│  • Session & Project Binding (via agyproj)                                             │
│  • Pure Markdown Output Compiler (.md artifact generator + Telegram Doc Uploader)      │
└──────────────────────────────────────────┬─────────────────────────────────────────────┘
                                           │ Tasks & Research Requests
                                           ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│               PROJECT 1: agyswarm (Cross-Account Multi-Agent & Research Swarm)         │
│  • Account-Pool Orchestrator (distributes agents across Account 1, 2, 3 via agyswitch)  │
│  • Blackboard Inter-Agent IPC (Shared context, memory, and artifact exchange)          │
│  • Autonomous Internet Research Engine (Web crawling, scraping, synthesis)             │
│  • Output: Structured Research Dossier & Code Change Plans in pure Markdown (.md)      │
└────────────────────────────────────┬───────────────────────────────────────────────────┘
                                     │
            ┌────────────────────────┴────────────────────────┐
            ▼                                                 ▼
┌───────────────────────────────┐                 ┌───────────────────────────────┐
│     agyswitch (Vault & Quota) │                 │   agyproj (Workspace Hub)     │
│  • Multi-Account Pool         │                 │  • Project Roots              │
│  • Auto Quota Balancing       │                 │  • Git Branches & Env         │
└───────────────────────────────┘                 └───────────────────────────────┘
```

---

## Project 1: `agyswarm` - Cross-Account Multi-Agent Swarm & Web Research Engine

### 1.1 The Core Problems Solved
1. **Quota Bottleneck**: A single account running a deep research task easily exhausts its hourly/weekly Gemini & Claude CloudCode quota buckets.
2. **Subagent Isolation**: Native CLI subagents cannot easily communicate with agents running in other terminals, background tasks, or workspaces.
3. **Manual Internet Research**: Searching, reading documentation, extracting GitHub issues, and synthesizing technical reports currently requires manual developer prompts.

### 1.2 Core Architecture & Functional Modules

#### A. Multi-Account Agent Worker Pool (`internal/pool`)
- Interfaces directly with `agyswitch` account storage (`~/.gemini_*` or account directories).
- **Quota-Aware Agent Dispatcher**: When a complex task is launched, `agyswarm` partitions the job into specialized roles and assigns each role to an account with optimal available quota:
  - **Account A (Best Gemini Quota)**: Assigned to **Lead Researcher & Web Synthesizer** (large context window for processing raw HTML/docs).
  - **Account B (Best Claude Quota)**: Assigned to **Code Architect & Auditor** (high-precision logic & refactoring).
  - **Account C (Standby)**: Assigned to **Verification & Test Runner**.
- If an agent detects a 429 Rate Limit or low quota during execution, `agyswarm` seamlessly pauses that worker, checkpoints its state, switches context via `agyswitch`, and resumes without losing progress.

#### B. Inter-Agent Blackboard Message Bus (`internal/bus`)
- An ultra-fast, local SQLite-backed WAL (Write-Ahead Logging) or Unix domain socket event bus.
- Agents publish events:
  - `RESEARCH_FINDING`: Topic, source URL, extracted facts, code snippets.
  - `TASK_DELEGATION`: Sub-goal assigned to another specialized agent.
  - `ARTIFACT_COMMITTED`: A generated `.md` plan, diff, or audit report.
- Cross-session memory allows an agent in `finance-dashboard` to instantly read findings produced by an agent that previously researched `auth-service`.

#### C. Autonomous Internet Deep Research Pipeline (`internal/research`)
A 4-stage pipeline that operates in the background:
1. **Query Decomposition**: Takes a prompt (e.g. *"Analyze best practices for WSL2 file bridge with Go and C# .NET 9"*) and generates 5–8 targeted search queries.
2. **Concurrent Web Fetch & Markdown Conversion**: Fetches Google/DuckDuckGo results, scrapes web pages, strips scripts/CSS, and converts HTML into clean GFM Markdown.
3. **Fact Verification & Hallucination Filter**: Cross-checks facts across at least 2 distinct URLs.
4. **Structured Markdown Dossier Generation**: Automatically generates a single consolidated, executive-grade Markdown deliverable (e.g. `./doc/research/2026-10-03_NET9_WSL2_RESEARCH.md`).

### 1.3 Command-Line & Headless Interface
```bash
# Run multi-agent research across 3 accounts concurrently
agyswarm research "OAuth2 token refresh patterns in Go" \
  --project finance-dashboard \
  --accounts auto-pool \
  --workers 3 \
  --output ./doc/research/OAUTH2_RESEARCH.md

# Start inter-agent mesh daemon
agyswarm daemon start

# Check active swarm agents & quota distribution
agyswarm status
```

---

## Project 2: `agytg` - Telegram Conversational Gateway with Pure Markdown Output

### 2.1 The Core Problems Solved
1. **Remote Agent Control**: Developers want to prompt Antigravity, check agent progress, or trigger overnight research from mobile via Telegram.
2. **Telegram Message Limits**: Telegram imposes a 4,096-character limit on regular messages, often breaking large code blocks, tables, and reports.
3. **Ephemeral Noise**: Chat messages disappear or get lost. Developers require durable, version-controlled `.md` files stored directly in the project workspace.

### 2.2 Core Architecture & Functional Modules

#### A. Headless Telegram Sidecar Daemon (`internal/bot`)
- Implements high-throughput Telegram Bot API polling or webhook receiver in pure Go.
- **Strict Security Guard**:
  - Whitelist of allowed Telegram User IDs (`ALLOWED_TELEGRAM_USERS`).
  - Scrypt-hashed PIN authentication required before session unlock.
  - Rate-limiting and command sanitization against prompt injection.

#### B. Context & Workspace Binding (`internal/session`)
- Directly leverages `agyproj` to discover registered repositories.
- Commands:
  - `/proj finance-dashboard`: Binds the active Telegram thread to that workspace.
  - `/acc nhontruongvo3`: Sets the active Antigravity account context.
  - `/status`: Returns live quota percentage and current git branch.
  - `/sessions`: Shows recent sessions matching `agyswitch` Tab 4.

#### C. Pure Markdown-Only Output Engine (`internal/output`)
Every interaction adheres to the **Markdown Deliverable Standard**:
1. When the user sends a command or query via Telegram (e.g., *"Research database migration strategies and draft a plan"*):
   - The bot acknowledges with a quick reaction and status message: `⚡ Task launched under finance-dashboard (Workers: 2)...`.
   - The agent executes headless, generating a comprehensive Markdown deliverable.
2. The bot writes the full output file to the workspace:
   `./doc/telegram/2026-10-03_DB_MIGRATION_PLAN.md`
3. The bot sends the file directly to the Telegram user as a **Native Document File (`.md`)**.
4. In the Telegram message body, it only sends a 3–5 line TL;DR summary:
   ```markdown
   ✅ Task Complete: Database Migration Plan
   📁 Project: finance-dashboard
   📄 Deliverable: 2026-10-03_DB_MIGRATION_PLAN.md (14.2 KB)
   📊 Steps: 34 · Time: 42s · Cost: $0.0170
   ```
5. If the user clicks the attached `.md` file on phone or tablet, it opens instantly in any mobile markdown reader with perfect formatting, full tables, and mermaid diagrams.

---

## End-to-End Synergy Flow: The Unified Workflow

```
[User on Mobile / Telegram]
       │
       │ 1. Sends: "/research Benchmark DuckDB vs SQLite for session storage"
       ▼
┌────────────────────────────────────────────────────────┐
│  agytg (Telegram Gateway Daemon)                        │
│  • Authenticates Telegram User ID                      │
│  • Resolves active workspace from agyproj              │
│  • Dispatches task to agyswarm                         │
└──────────────────────────┬─────────────────────────────┘
                           │
                           │ 2. Dispatches task with project context
                           ▼
┌────────────────────────────────────────────────────────┐
│  agyswarm (Multi-Agent Swarm Orchestrator)             │
│  • Inspects agyswitch: Selects Account 1 & Account 2   │
│  • Worker 1 (Account 1): Queries Google/DuckDuckGo     │
│  • Worker 2 (Account 2): Clones benchmarks & tests     │
│  • Inter-Agent Bus: Workers exchange benchmark results │
│  • Synthesizes single consolidated Markdown report     │
└──────────────────────────┬─────────────────────────────┘
                           │
                           │ 3. Writes deliverable file into workspace root
                           ▼
┌────────────────────────────────────────────────────────┐
│  Workspace Repository (e.g., ./finance-dashboard/doc/) │
│  • DUCKDB_VS_SQLITE_BENCHMARK_20261003.md              │
└──────────────────────────┬─────────────────────────────┘
                           │
                           │ 4. Streams file back to user
                           ▼
┌────────────────────────────────────────────────────────┐
│  agytg Telegram Delivery                               │
│  • Sends summary message to Telegram chat              │
│  • Sends DUCKDB_VS_SQLITE_BENCHMARK_20261003.md as doc │
└────────────────────────────────────────────────────────┘
```

---

## Proposed Project Structure & Code Layout

### 1. `apps/agyswarm` (Multi-Agent Swarm & Web Research)
```
apps/agyswarm/
├── go.mod
├── main.go                       # CLI entrypoint (research, swarm, daemon, status)
├── internal/
│   ├── model/
│   │   ├── agent.go              # AgentWorker, Role, Task, QuotaBudget
│   │   └── research.go           # ResearchDossier, WebSource, FactRecord
│   ├── pool/
│   │   ├── pool.go               # Account-aware worker dispatcher
│   │   └── balance.go            # Quota load-balancer across agyswitch accounts
│   ├── bus/
│   │   ├── bus.go                # Blackboard WAL event broker
│   │   └── memory.go             # Cross-session shared knowledge store
│   ├── research/
│   │   ├── crawl.go              # Concurrent web scraper & reader
│   │   ├── markdown.go           # Clean HTML-to-GFM Markdown transformer
│   │   └── synthesize.go         # Research report compiler
│   └── view/
│       └── tui.go                # Live terminal monitoring dashboard
```

### 2. `apps/agytg` (Telegram Markdown Gateway)
```
apps/agytg/
├── go.mod
├── main.go                       # Daemon entrypoint (start, stop, status, set-pin)
├── internal/
│   ├── bot/
│   │   ├── bot.go                # Telegram Bot API client & webhook/poll loop
│   │   ├── router.go             # Command & message dispatch router
│   │   └── handlers.go           # /research, /task, /status, /proj handlers
│   ├── session/
│   │   ├── binding.go            # User -> Project & Account context map
│   │   └── runner.go             # Headless agy / agyswarm invocation wrapper
│   ├── markdown/
│   │   ├── compiler.go           # Enforces GFM compliance, headers & tables
│   │   └── uploader.go           # Sends .md file as native Telegram document
│   └── security/
│       ├── auth.go               # User whitelist & PIN auth manager
│       └── guard.go              # Sanitization & safe sandbox validation
```

---

## Phased Implementation Plan

### Phase 1: `agyswarm` Web Research & Multi-Account Dispatcher
1. Implement `pool` module interfacing with `agyswitch` vault to spawn parallel subagent workers on separate accounts.
2. Build the autonomous web fetcher and HTML-to-Markdown cleaner in `research/crawl.go`.
3. Implement `agyswarm research "<query>"` producing standalone `.md` reports.

### Phase 2: Inter-Agent Blackboard Bus
1. Implement the local SQLite WAL message bus (`internal/bus`).
2. Add cross-session event publishing and artifact subscription.
3. Test parallel multi-account research with 2 accounts running concurrently.

### Phase 3: `agytg` Telegram Headless Bot & Pure-Markdown Exporter
1. Build `agytg` daemon with Telegram long-polling and user whitelist authentication.
2. Integrate `agyproj` project selection via Telegram commands (`/proj`).
3. Enforce **Markdown Document Delivery**: every interaction generates a `.md` artifact saved to the workspace and uploaded directly to Telegram.

---

## Reference Links (WSL2 + Windows Environment)
- **VS Code Clickable (Recommended):** [AGY_MULTI_AGENT_AND_TELEGRAM_PLATFORM_BRAINSTORM.md](./AGY_MULTI_AGENT_AND_TELEGRAM_PLATFORM_BRAINSTORM.md)
- **Windows Path:** `C:\Users\TruongNhon\Documents\Powershell\AGY_MULTI_AGENT_AND_TELEGRAM_PLATFORM_BRAINSTORM.md`
- **Related Platforms:** [agyswitch](./apps/agyswitch/internal/view/app.go) · [agyproj](./apps/agyproj/internal/view/app.go)
