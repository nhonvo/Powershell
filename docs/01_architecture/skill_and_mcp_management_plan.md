# Comprehensive Architecture & Implementation Plan: Skill, Rule & MCP Management
**Dual-Scope Orchestration (Global vs. Project) for Antigravity (`agy`, `agyswitch`, and `agytui`)**

---

## 1. Executive Summary

As multi-agent workflows scale across diverse repositories (e.g. `finance-dashboard`, `powershell-profile`, `BinhDinhFood`), customizations must be cleanly separated between:
1. **Global Machine Scope (`~/.gemini/`)**: Universal developer preferences, cross-project utility runbooks, credentialed MCP servers, and multi-account sync.
2. **Project Workspace Scope (`<project-root>/.agents/`)**: Repository-specific architecture guidelines, specialized domain skills, and local database/service MCP servers checked into Git.

This plan details the technical architecture, directory layouts, configuration schemas, TUI screens, and implementation milestones for two flagship features:
- **Feature 1: Dual-Scope Skill & Rule Management Engine** (Discovery, Isolation, Scaffolding, and Sync)
- **Feature 2: MCP (Model Context Protocol) Server Management & Health Monitoring** (Global vs. Local configs, dynamic env injection, health probes, and tool inspection)

```
┌──────────────────────────────────────────────────────────────────────────────────────────┐
│                           ANTIGRAVITY CUSTOMIZATION TAXONOMY                             │
├──────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                          │
│  GLOBAL SCOPE (~/.gemini/)                           PROJECT SCOPE (<project>/.agents/)  │
│  ─────────────────────────                           ──────────────────────────────────  │
│  • Global Skills: ~/.gemini/skills/                  • Project Skills: .agents/skills/   │
│    (Git sync, system tools, clean-up runbooks)         (EF seeders, chart audit runbooks)│
│                                                                                          │
│  • Global Rules: ~/.gemini/config/rules/             • Project Rules: .agents/rules/     │
│    (Security policies, concise style guidelines)       (Clean Architecture, DDD, ESLint) │
│                                                                                          │
│  • Global MCP: ~/.gemini/config/mcp_config.json      • Project MCP: .agents/mcp.json     │
│    (Filesystem, GitHub, Brave Search, Notion)          (Postgres DB, Docker, Redis, LSP) │
│                                                                                          │
│  • Mirrored Multi-Account: ~/.gemini_<account>/      • Team Versioned: Checked into Git  │
│                                                                                          │
│                             │                                     │                      │
│                             ▼                                     ▼                      │
│               ┌─────────────────────────────────────────────────────────┐                │
│               │             AGYSWITCH CUSTOMIZATION ENGINE              │                │
│               │  • Precedence Resolver (Project > Explicit > Global)    │                │
│               │  • Health Checker (MCP latency, skill schema linting)   │                │
│               │  • Account Context Mirror (Syncs to active GEMINI_HOME) │                │
│               └─────────────────────────────────────────────────────────┘                │
└──────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Feature 1: Skill & Rule Management (Global vs. Project Separation)

### 2.1 Directory Hierarchy & Resolution Order

Customizations are resolved strictly following Antigravity's progressive disclosure hierarchy:

| Priority | Scope | File Paths | VCS Tracking | Intended Use |
| :--- | :--- | :--- | :--- | :--- |
| **1 (Highest)** | **Project Workspace** | `<root>/.agents/skills/<name>/SKILL.md`<br>`<root>/.agents/rules/*.md` | **Yes (Git)** | Shared runbooks, repo conventions, project seeders |
| **2** | **Project Declared** | `<root>/.agents/skills.json`<br>`<root>/GEMINI.md`, `AGENTS.md` | **Yes (Git)** | Explicitly enabled/disabled project skills |
| **3** | **Global Custom** | `~/.gemini/skills/<name>/SKILL.md`<br>`~/.gemini/config/rules/*.md` | **No (Local)** | Personal productivity runbooks, system rules |
| **4** | **Global Declared** | `~/.gemini/config/skills.json`<br>`~/.gemini/config/rules.json` | **No (Local)** | Machine-wide toggles across all accounts |
| **5 (Lowest)** | **Built-in System** | `~/.gemini/antigravity-cli/builtin/skills/` | **Bundled** | Native CLI capabilities (`agy-customizations`, etc.) |

---

### 2.2 Skill Structure & Progressive Disclosure

Skills prevent prompt bloat through **two-stage progressive disclosure**:
1. **Header Phase (Injected by Default)**: Only YAML frontmatter (`name` + `description`) is injected into the model's system instructions.
2. **Body Phase (On-Demand Activation)**: The model (or user) only invokes `view_file` on `SKILL.md` when a task matches the description.

#### Standard Skill Directory Layout
```text
.agents/skills/csharp-seeder/
├── SKILL.md               # Required: Frontmatter + Step-by-step instructions
├── scripts/               # Optional: Executable shell/powershell helper tools
│   └── seed_demo.ps1
└── references/            # Optional: Sample payloads, SQL schemas, architecture docs
    └── UserSeedData.json
```

#### Standard `SKILL.md` Format
```markdown
---
name: csharp-seeder
description: >-
  Interactive workflow for generating and executing Entity Framework Core seed scripts
  for demo accounts, auth permissions, and database resets in finance-dashboard.
---

# C# EF Core Seeding Runbook
When seeding test entities:
1. Ensure Docker database is healthy: `docker compose ps db`.
2. Inspect `Domain/Entities/` before modifying seed models.
3. Run migrations via `dotnet ef database update --project csharp-sln/Infrastructure`.
```

---

### 2.3 Rule Trigger Modes & Precedence

Rules enforce behavioral constraints and code patterns without requiring manual invocation:

| Trigger Mode | Syntax in Rule Header | Behavior | Example |
| :--- | :--- | :--- | :--- |
| **Always On** | `trigger: always_on` | Loaded unconditionally on every conversation turn. | Security checks, no hardcoded API keys. |
| **Model Decision** | `trigger: model_decision` | Model decides to consult rule based on description. | Performance tuning, refactoring patterns. |
| **Glob Path** | `glob: "**/*.tsx"` | Activated dynamically when the prompt touches matching files. | React memoization, chart optimization rules. |

---

### 2.4 Skill & Rule Management Capabilities in `agyswitch`

1. **Isolation & Scoping**:
   - Explicit badges in TUI: `[Global]` (green) vs `[Project]` (magenta).
   - Filter views: `[All]`, `[Global Only]`, `[Project Only]`.
2. **Cross-Account Sync (`[S]` key)**:
   - When a global skill is modified in `~/.gemini/skills/`, `agyswitch` syncs it atomically across all account directories (`~/.gemini_fptvttnhon2020`, `~/.gemini_fptvttnhon2026`, etc.).
3. **Scaffolding (`[N]` key)**:
   - Interactive modal to scaffold new skills: prompts for Name, Scope (Global/Project), and Description, then generates valid `SKILL.md`.
4. **Promotion / Forking**:
   - `Promote to Project`: Copies a global skill into `.agents/skills/` so it can be customized and checked into Git for the team.
   - `Publish to Global`: Exports a successful project skill to `~/.gemini/skills/` for personal use across other codebases.

---

## 3. Feature 2: MCP (Model Context Protocol) Server Management

### 3.1 Dual-Scope MCP Configuration Architecture

MCP servers connect Antigravity agents to external tools (Database explorers, Browser automation, Docker daemon, GitHub, Jira).

```
~/.gemini/config/mcp_config.json        <project-root>/.agents/mcp.json
       [GLOBAL MCP SERVERS]                   [PROJECT MCP SERVERS]
       • Brave Search API                     • Local Postgres DB Explorer
       • GitHub Global CLI                    • Docker Daemon Controller
       • Filesystem Inspector                 • Project Roslyn Language Server
                │                                      │
                └──────────────────┬───────────────────┘
                                   ▼
                       ┌───────────────────────┐
                       │ Merged MCP Toolset    │
                       │ (Project overrides    │
                       │  global servers with  │
                       │  same identifier)     │
                       └───────────────────────┘
```

#### Global Configuration (`~/.gemini/config/mcp_config.json`)
```json
{
  "mcpServers": {
    "brave-search": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-brave-search"],
      "env": {
        "BRAVE_API_KEY": "${BRAVE_API_KEY}"
      }
    },
    "filesystem": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-filesystem", "/home/truongnhon"]
    }
  }
}
```

#### Project Configuration (`<project-root>/.agents/mcp.json`)
```json
{
  "mcpServers": {
    "project-database": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-postgres", "postgresql://dev:pass@localhost:5432/finance_db"]
    },
    "docker": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-docker"]
    }
  }
}
```

---

### 3.2 Real-Time Health & Latency Probing

Currently, MCP servers display mock connectivity. The enhanced MCP engine implements **active IPC probes**:

1. **STDIO Ping**:
   - Sends a lightweight JSON-RPC `{"jsonrpc": "2.0", "id": 1, "method": "ping"}` over stdin.
   - Measures response time in milliseconds (`LatencyMs`).
2. **Health Classifications**:
   - `● Connected (8ms)`: Normal operating condition.
   - `▲ Degraded (>500ms)`: Server is running but experiencing high IPC latency.
   - `○ Offline (Exit 1)`: Process failed to start or crashed.
   - `🔑 Missing Env`: Required environment variables (e.g. `${POSTGRES_PASSWORD}`) are unresolved.
3. **Auto-Recovery**:
   - Automatic restart with exponential backoff if a server process terminates unexpectedly.

---

## 4. Enhanced TUI Design (Tabs 2 & 3 in `agyswitch`)

### 4.1 Tab 2: Skills Hub (Scope Filtering & Promotion)

```
🛸 AGYSWITCH - Dedicated Antigravity Control Center (Go Engine v2.0)
──────────────────────────────────────────────────────────────────────────────────────────────────
 [1] 🔑 Vault & Quota   [2] 🧩 Skills Hub   [3] 📜 Rules & MCP   [4] 📊 Fleets & Sessions 
──────────────────────────────────────────────────────────────────────────────────────────────────
 Filter: [A] All (8) · [G] Global (5) · [P] Project: finance-dashboard (3)

 🧩 Discovered Antigravity Skills:
    1. 🌐 [Global]   agy-customizations   Comprehensive guide & reference for Antigravity rules/skills
    2. 🌐 [Global]   antigravity-guide    CLI commands, slash triggers, shortcuts & sidecar guide
    3. 🌐 [Global]   git-fleet-cleanup    Automated stale worktree pruner & branch garbage collection
  ▶ 4. 📁 [Project]  csharp-sln-seeder    EF Core test entity & demo auth seeder for finance-dashboard
       Path:        /home/truongnhon/projects/finance-dashboard/.agents/skills/csharp-sln-seeder
       Trigger:     On-Demand Progressive Disclosure · Scripts: [seed_demo.ps1]
       Overrides:   None (Workspace Unique)
    5. 📁 [Project]  chart-audit-rules    Recharts memory leak detection & performance benchmarks
    6. 📁 [Project]  docker-rebuild-all   Local docker-compose rebuild and container health verifier

──────────────────────────────────────────────────────────────────────────────────────────────────
 [Tab/1-4] Switch Tab · [↑/↓ j/k] Nav · [Enter/V] View Skill · [N] New Skill · [P] Promote/Publish
 [S] Sync Global to Vaults · [D] Delete · [Q/Esc] Exit
```

---

### 4.2 Tab 3: Rules & MCP Servers (Live Health & Secret Status)

```
🛸 AGYSWITCH - Dedicated Antigravity Control Center (Go Engine v2.0)
──────────────────────────────────────────────────────────────────────────────────────────────────
 [1] 🔑 Vault & Quota   [2] 🧩 Skills Hub   [3] 📜 Rules & MCP   [4] 📊 Fleets & Sessions 
──────────────────────────────────────────────────────────────────────────────────────────────────
 📜 Active Customization Rules:
    1. 🌐 [Global]   SECURITY_BASELINE.md   Always On     Zero token/credential leakage policy
    2. 🌐 [Global]   CONCISE_OUTPUT.md      Always On     Github-style markdown links & symbol refs
  ▶ 3. 📁 [Project]  CLEAN_ARCH_NET8.md     Model Dec.    DDD domain separation & MediatR handlers
       Path: /home/truongnhon/projects/finance-dashboard/.agents/rules/CLEAN_ARCH_NET8.md

 🔌 Configured MCP Tool Providers:
    • 🌐 filesystem         [cmd: npx @mcp/server-filesystem]       ● Connected (8ms)
    • 🌐 brave-search       [cmd: npx @mcp/server-brave-search]     ● Connected (14ms)
    • 📁 project-database   [cmd: npx @mcp/server-postgres]         ● Connected (12ms · 18 tables)
    • 📁 docker-controller  [cmd: npx @mcp/server-docker]           ○ Offline (Docker socket denied)

──────────────────────────────────────────────────────────────────────────────────────────────────
 [Tab/1-4] Switch Tab · [↑/↓ j/k] Nav · [V] Inspect Rule · [N] New Rule · [R] Ping MCP Servers
 [T] Test Server Connection · [E] Edit Config · [Q/Esc] Exit
```

---

## 5. Command-Line Interface (`agyswitch` CLI)

Add the following subcommands to `agyswitch` for rapid scripting and CI/CD validation:

```bash
# Skills Management
agyswitch skills list                           # List skills across all scopes
agyswitch skills list --project                 # List project skills only
agyswitch skills new <name> --scope=project     # Scaffold new skill in .agents/skills/
agyswitch skills sync                           # Mirror global skills to all ~/.gemini_* vaults
agyswitch skills promote <name>                 # Copy global skill into current project

# Rules Management
agyswitch rules list                            # List all active rules
agyswitch rules new <name> --trigger=always     # Create rule in ~/.gemini/config/rules/
agyswitch rules lint                            # Validate markdown headers and glob syntax

# MCP Server Management
agyswitch mcp status                            # Probe and print status of all MCP servers
agyswitch mcp ping <server-name>                # Ping specific server and report JSON-RPC latency
agyswitch mcp add <name> <cmd> [args...]        # Register new MCP server (global or --project)
agyswitch mcp remove <name>                     # Unregister MCP server
```

---

## 6. Implementation Roadmap

### Phase 1: Core Scoping & Discovery Engine (Week 1)
- [ ] Extend `internal/model/model.go` with `ScopeType` (`ScopeGlobal`, `ScopeProject`) and `MCPHealthStatus`.
- [ ] Update `skills.DiscoverSkills(workspaceDir)` to return both Global and Project skills without discarding Project skills that share names with Global ones (mark project ones as overrides).
- [ ] Implement `rules.DiscoverRules(workspaceDir)` with support for `.agents/rules/`, `GEMINI.md`, and `AGENTS.md`.

### Phase 2: Active Health Checker & Secret Injection for MCP (Week 2)
- [ ] Replace static mock latency in `rules.CheckMCPServerStatus` with real `exec.Command` spawn and JSON-RPC ping probe.
- [ ] Implement `${ENV_VAR}` expansion in `ParseMCPConfig` using `os.Getenv` and `.env` discovery.
- [ ] Add dual config merging: `~/.gemini/config/mcp_config.json` + `<workspace>/.agents/mcp.json`.

### Phase 3: Interactive TUI Enhancements (Week 3)
- [ ] Add Scope Filter keys (`[A]` All, `[G]` Global, `[P]` Project) in Tab 2.
- [ ] Add Skill Scaffolding modal (`[N]` key in Tab 2) to generate `SKILL.md` template with frontmatter.
- [ ] Add MCP Server connection test key (`[T]` in Tab 3) that displays live stdout/stderr logs if a server fails to start.

### Phase 4: Web Sidecar REST Endpoints (Week 4)
- [ ] Expose `GET /api/v1/skills?workspace=...` (returns JSON list with scope and override status).
- [ ] Expose `GET /api/v1/mcp/status` (returns live server ping latencies and connection states).
- [ ] Expose `POST /api/v1/skills/sync` (triggers global skill mirroring across account directories).
