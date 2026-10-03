# 🛠️ AGYDOCKER: Centralized Dev Tools Reuse & Auto-Attach Architecture Plan

> **Strategic Architecture & Implementation Blueprint**: Reusable Docker Developer Tools (`pgAdmin 4`, `Mongo Express`, etc.) in `agydocker`  
> **Target Module**: `apps/agydocker`  
> **Reference Centralized Stack**: [`/home/truongnhon/projects/dev-tools/pgadmin`](/home/truongnhon/projects/dev-tools/pgadmin)  
> **Date**: 2026-10-03  
> **Author**: Antigravity Autonomous Pair Programmer  

---

## 1. Executive Summary & Problem Statement

### The Problem in Multi-Project Development
When developers or AI agents work on individual projects across the workstation (e.g. `finance-dashboard`, `InventoryManagementSystem`, `organizeX`, or newly scaffolded repos):
1. **Redundant Container Sprawl**: Every repo attempts to run its own `pgAdmin` or `Mongo Express` in `docker-compose.yml`.
2. **Port Collisions**: Competing containers clash over host ports `:5050`, `:5051`, `:8081`, or `:8082`.
3. **Severe RAM Bloat**: Each `dpage/pgadmin4` instance consumes ~350MB–500MB of resident RAM. Running 3 project-specific instances wastes >1.2GB of WSL2 kernel memory.
4. **Credential Fragmentation**: Operators must repeatedly re-type database credentials, server connection strings, and master passwords.

### The Centralized Solution (`/home/truongnhon/projects/dev-tools`)
The workstation already possesses an elegant, centralized GUI tools stack under `/home/truongnhon/projects/dev-tools` featuring:
* **Single pgAdmin 4 instance** (`dev_tools_pgadmin`) running on `http://localhost:5050` with master password disabled.
* **Pre-configured servers** loaded via `./pgadmin/servers.json` and zero-prompt authentication via `./pgadmin/pgpassfile` (mode `0600`).
* **Single Mongo Express instance** (`dev_tools_mongo_express`) on `http://localhost:8082`.

### The Missing Capability
Connecting a new or existing project's database to this centralized stack currently requires manual configuration:
1. Finding the project's Docker network name.
2. Manually declaring external networks in `dev-tools/docker-compose.yml`.
3. Manually crafting JSON objects inside `pgadmin/servers.json`.
4. Manually appending credentials into `pgadmin/pgpassfile`.
5. Running `docker exec dev_tools_pgadmin /venv/bin/python3 /pgadmin4/setup.py load-servers /pgadmin4/servers.json`.

**Goal**: Build a native, zero-friction **Dev Tools Manager & Auto-Attach Engine** directly inside `agydocker` (CLI + TUI) so any project can instantly reuse the centralized developer tools with a single command or keystroke.

---

## 2. System Architecture & Information Flow

```
   ┌────────────────────────────────────────────────────────┐
   │          Target Development Project Workspace          │
   │  • finance-dashboard   • InventoryManagementSystem      │
   │  • organizeX           • Any new project repo           │
   │  (Runs PostgreSQL:5432, MongoDB:27017 on project net)  │
   └───────────────────────────┬────────────────────────────┘
                               │
                               │ agydocker tools attach (or TUI [A])
                               ▼
   ┌────────────────────────────────────────────────────────┐
   │            AGYDOCKER Dev Tools Engine                  │
   │                                                        │
   │  1. Container & Compose Auto-Detector                  │
   │     • Inspects DB image, port, POSTGRES_DB / USER / PW │
   │     • Resolves container network (e.g. finance-dev-net)│
   │                                                        │
   │  2. Dynamic Docker Network Bridging                    │
   │     • docker network connect <net> dev_tools_pgadmin   │
   │     • Persists external network in compose.yml         │
   │                                                        │
   │  3. Server & Passfile Registry Mutator                 │
   │     • Atomic update of pgadmin/servers.json            │
   │     • Appends entry to pgadmin/pgpassfile (chmod 0600) │
   │                                                        │
   │  4. In-Container Live Hot-Reload                       │
   │     • docker exec dev_tools_pgadmin load-servers       │
   └───────────────────────────┬────────────────────────────┘
                               │
                               ▼
   ┌────────────────────────────────────────────────────────┐
   │          Centralized Stack (:5050 / :8082)             │
   │  • pgAdmin 4 shows newly attached DB under Group Name  │
   │  • Instant query tool with zero password prompt        │
   │  • Shared WSL2 RAM footprint (~400MB total)            │
   └────────────────────────────────────────────────────────┘
```

---

## 3. Core Feature Specification

### 3.1 Centralized Stack Lifecycle Control
Commands to manage the centralized stack from anywhere without navigating to `/home/truongnhon/projects/dev-tools`:
* `agydocker tools status`: Checks if `dev_tools_pgadmin` and `dev_tools_mongo_express` are active, shows ports, and lists all registered server databases.
* `agydocker tools up` / `start`: Launches the shared compose stack in detached mode (`-d`) and verifies health.
* `agydocker tools down` / `stop`: Stops the shared stack to reclaim WSL2 memory when database GUI operations are complete.
* `agydocker tools restart`: Restarts the stack and reloads server definitions.
* `agydocker tools open [pgadmin|mongo]`: Automatically opens `http://localhost:5050` or `http://localhost:8082` in the host browser.

### 3.2 Automated Database Discovery & Registration (`attach`)
When run inside a project folder (or provided a path/container):
`agydocker tools attach [path-or-container-name]`

1. **Introspection Pipeline**:
   * Scans project `docker-compose.yml`, `.env`, or live running containers.
   * Extracts:
     - **Database Engine**: PostgreSQL, MongoDB, MySQL, Redis.
     - **Container Name**: e.g. `finance_postgres`, `inventory-db-dev`.
     - **Internal & Mapped Ports**: `5432`, `5433`, `27017`.
     - **Database Name**: from `POSTGRES_DB` or default `postgres`.
     - **User & Password**: from `POSTGRES_USER` / `POSTGRES_PASSWORD` (or prompts if obscure).
     - **Docker Network**: identifies container's primary network name.
2. **Network Bridging**:
   * Immediately invokes `docker network connect <network> dev_tools_pgadmin` (allows DNS resolution on the fly without stopping the container).
   * Updates `dev-tools/docker-compose.yml` to ensure permanence across future restarts.
3. **Registry Mutation**:
   * Appends/updates entry in `pgadmin/servers.json` grouped cleanly by project name (e.g. `Group: "Finance Dashboard"`).
   * Adds credentials to `pgadmin/pgpassfile` with strict `0600` permissions.
4. **Instant In-Container Activation**:
   * Runs `docker exec dev_tools_pgadmin /venv/bin/python3 /pgadmin4/setup.py load-servers /pgadmin4/servers.json`.
   * Prints direct clickable link: `http://localhost:5050` with confirmation.

### 3.3 Auto-Sync Fleet Discovery (`sync`)
`agydocker tools sync`
* Inspects **all running containers** across the Docker daemon.
* Finds all PostgreSQL (`image: *postgres*`) and MongoDB (`image: *mongo*`) containers.
* Compares against current `servers.json`.
* Automatically bridges missing networks and registers new databases in one operation.

### 3.4 Safe Unlinking (`detach`)
`agydocker tools detach <server-id-or-name>`
* Safely removes the database from `servers.json`.
* Cleans up credentials from `pgpassfile`.
* If no other databases share the project network, optionally disconnects `dev_tools_pgadmin` from that network.

---

## 4. TUI Cockpit Integration (`apps/agydocker`)

Extend `agydocker`'s terminal interface with a dedicated 4th tab:

```
⚡ AGYDOCKER - Container Fleet & WSL2 RAM Manager
────────────────────────────────────────────────────────────────────────────────
 [1] 🐳 Containers   [2] 🧠 WSL2 RAM   [3] 💾 Volumes   [4] 🛠️ Dev Tools (GUI)  
────────────────────────────────────────────────────────────────────────────────

 🛠️ Centralized Developer Tools Stack (/home/truongnhon/projects/dev-tools)
   • pgAdmin 4:      🟢 Running (http://localhost:5050) · 382 MB RAM
   • Mongo Express:  🟢 Running (http://localhost:8082) · 94 MB RAM

 Connected Databases in pgAdmin 4:
  ▶  📁 Finance Dashboard
        🐘 Finance DB (DEV - Local)         finance_postgres:5432    [🟢 Bridged]
        🐘 Finance DB (PROD - Neon)         neon.tech:5432           [🌐 Cloud SSL]
     📁 Inventory Management
        🐘 Inventory DB (DEV - Container)   inventory-db-dev:5432    [🟢 Bridged]
        🐘 Inventory DB (LOCAL - Host)      host.docker.internal:5433[💻 Host Gateway]
        🐘 Inventory DB (PROD - Neon)       neon.tech:5432           [🌐 Cloud SSL]

 Active Actions:
   [U] Start Tools    [D] Stop Tools    [A] Auto-Attach DB    [S] Auto-Sync All
   [O] Open pgAdmin   [M] Open Mongo    [R] Reload Servers    [X] Detach Server
────────────────────────────────────────────────────────────────────────────────
 [Tab/1-4] Tabs · [↑/↓] Select · [Enter/O] Open UI · [A] Auto-Attach · [Q] Exit
```

---

## 5. Technical Implementation Plan & Package Structure

```text
apps/agydocker/
├── main.go                               # Add 'tools' CLI router command
├── internal/
│   ├── model/
│   │   ├── container.go
│   │   ├── devtools.go                   # DevToolsStack, DBServerEntry, PassFileEntry
│   │   └── mem.go
│   ├── service/
│   │   ├── devtools/
│   │   │   ├── config.go                 # Stack path resolver & validation
│   │   │   ├── detector.go               # Inspects projects/containers for DB engines
│   │   │   ├── bridge.go                 # Dynamic Docker network attachment
│   │   │   ├── pgadmin.go                # servers.json & pgpassfile mutator + setup.py reload
│   │   │   ├── mongo.go                  # Mongo Express connection config
│   │   │   └── devtools_test.go          # Unit tests for JSON/passfile mutations
│   │   └── dockerops/
│   └── view/
│       ├── app.go                        # Add ActiveTab 3 (Dev Tools Tab)
│       └── devtools_tab.go               # Render centralized tools status & DB table
```

---

## 6. Phased Implementation Roadmap

| Phase | Milestone | Scope & Deliverables |
| :---: | :--- | :--- |
| **Phase 1** | **DevTools Model & Config Resolver** | Implement `devtools.go` and `config.go`. Supports path auto-discovery (`/home/truongnhon/projects/dev-tools`, `$AGY_DEV_TOOLS_PATH`), validation, and compose status inspection. |
| **Phase 2** | **pgAdmin Registry Mutator** | Implement `pgadmin.go` to parse, serialize, and mutate `servers.json` and `pgpassfile` with atomic writes and strict `0600` permissions. Unit tested. |
| **Phase 3** | **Network Bridging & Container Detector** | Implement `bridge.go` (calls Docker API/CLI `docker network connect`) and `detector.go` (extracts DB info from `docker-compose.yml`, `.env`, and container env vars). |
| **Phase 4** | **In-Container Hot-Reload & Sync Engine** | Implement `load-servers` invocation via `docker exec dev_tools_pgadmin` and implement `agydocker tools sync` across all running containers. |
| **Phase 5** | **CLI Subcommand Suite** | Integrate subcommands into `main.go`: `agydocker tools [status|up|down|attach|detach|sync|open]`. |
| **Phase 6** | **TUI Cockpit Tab 4** | Add Tab `[4] 🛠️ Dev Tools (GUI)` to `agydocker` interactive TUI with server tree, network status badges, and one-key shortcuts (`[U]`, `[D]`, `[A]`, `[S]`, `[O]`, `[M]`). |
| **Phase 7** | **Verification & Suite Integration** | Run tests (`make test`), verify zero regression across all 12 apps, update `agyx` shortcuts, and cross-compile. |

---

## 7. Document Reference Links

* **VS Code Clickable Link (Recommended):**
  * Architecture Plan: [AGYDOCKER_DEV_TOOLS_REUSE_PLAN.md](./AGYDOCKER_DEV_TOOLS_REUSE_PLAN.md)
  * Centralized Stack Reference: [`/home/truongnhon/projects/dev-tools/README.md`](/home/truongnhon/projects/dev-tools/README.md)
  * Centralized pgAdmin Config: [`/home/truongnhon/projects/dev-tools/pgadmin/servers.json`](/home/truongnhon/projects/dev-tools/pgadmin/servers.json)
  * Existing agydocker View: [apps/agydocker/internal/view/app.go](./apps/agydocker/internal/view/app.go)
* **Windows UNC Path:**
  * `\\wsl.localhost\Ubuntu\mnt\c\Users\TruongNhon\Documents\Powershell\AGYDOCKER_DEV_TOOLS_REUSE_PLAN.md`
  * `\\wsl.localhost\Ubuntu\home\truongnhon\projects\dev-tools\pgadmin\servers.json`
