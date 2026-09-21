# 🛸 Antigravity Developer Suite — Documentation Gateway & Master Index

> **Category**: Documentation Gateway & System Sitemap  
> **Subsystem**: Antigravity Go Engine Suite & Developer Tooling  
> **Environment**: Ubuntu WSL2 + Windows 11 / VS Code  
> **Date**: September 2026  
> **Status**: Production / Active  
> **Master Strategic Roadmap**: [00_agy_system_audit_and_roadmap.md](reports/00_agy_system_audit_and_roadmap.md)  
> **Audit Catalog**: [docs/reports/README.md](reports/README.md)  
> **Windows UNC Reference**: `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\docs\README.md`

---

## Executive Summary

Welcome to the centralized documentation gateway for the **Antigravity Developer Suite**. 

The Antigravity ecosystem has evolved from an earlier monolithic .NET 9.0 console application (`AgyTui`) into a **modular, high-speed Go Engine suite comprising 8 native micro-applications** + integrated cloud tooling. These tools deliver instant execution (cold-start latency under 15ms), lightweight memory footprints (6MB–14MB RAM), and zero PowerShell DLL locking hazards, while providing rich terminal dashboards and cross-platform automation across Ubuntu WSL2 and Windows 11.

This master gateway provides top-level architectural topologies, component catalogs, user navigation, developer guides, and links to all forensic deep-dive audit reports.

---

## 1. System Architecture & Topology

The modern Antigravity suite is structured around **`agyx`** as the master proxy and cockpit orchestrator, with specialized Go micro-tools handling dedicated operational domains:

```mermaid
graph TD
    User([Developer / Shell User]) -->|Shell Triggers & Aliases| Shell[PowerShell 7+ / Zsh Profiles]
    
    %% Master Gateway
    Shell -->|agyx / x| AGYX[⚡ agyx: Master Orchestrator & Cockpit]
    
    %% Micro-tools direct or proxied
    Shell -->|agysw / agys| AGYSWITCH[🛸 agyswitch: Vault, Identity & Quota]
    Shell -->|agyp / proj| AGYPROJ[📁 agyproj: Workspace & Tech Stack Hub]
    Shell -->|agyg| AGYGIT[🐙 agygit: Multi-Agent Git & Worktrees]
    Shell -->|agyd| AGYDOCKER[🐳 agydocker: Container Fleet & RAM Guard]
    Shell -->|agyt| AGYTERM[🎨 agyterm: Terminal Themes & WinTerm JSON]
    Shell -->|agym| AGYMOBILE[📱 agymobile: Mobile Cockpit & Web :7890]
    Shell -->|agyo / ai| AGYOLLAMA[🤖 agyollama: Local AI & Ollama Daemon]
    
    %% Master proxy delegation
    AGYX -.->|Proxy Delegation / PTY| AGYSWITCH
    AGYX -.->|Proxy Delegation / PTY| AGYPROJ
    AGYX -.->|Proxy Delegation / PTY| AGYGIT
    AGYX -.->|Proxy Delegation / PTY| AGYDOCKER
    AGYX -.->|Proxy Delegation / PTY| AGYTERM
    AGYX -.->|Proxy Delegation / PTY| AGYMOBILE
    AGYX -.->|Proxy Delegation / PTY| AGYOLLAMA
    AGYX -.->|Diagnostic Telemetry| LocalStack[☁️ AWS / LocalStack]
    
    %% External and host interfaces
    AGYSWITCH -->|AES-256 Keyring / Mirroring| ContextDirs["Context Stores (~/.gemini_*)"]
    AGYSWITCH -->|Live Quota HTTPS Probes| GoogleCloudCode[Google CloudCode API]
    AGYSWITCH -->|Subprocess Launcher| AGYCLI[Google Antigravity CLI]
    
    AGYPROJ -->|Telemetry & Cost Analytics| BrainLogs["Transcripts (~/.gemini/.../brain)"]
    AGYGIT -->|Isolated Git Worktrees| AgentWorktrees[".worktrees/<branch> (Concurrent Agents)"]
    AGYDOCKER -->|Kernel Memory Watchdog| ProcMem["WSL2 /proc/meminfo (RAM/Swap)"]
    AGYTERM -->|Direct JSON AST Mutation| WinTermSettings["Windows Terminal (settings.json)"]
    AGYMOBILE -->|Encrypted Remote Access| Tailscale["Tailscale Mesh / Mobile Web :7890"]
    AGYOLLAMA -->|REST API / CLI Fallback| OllamaDaemon["Ollama Daemon (:11434)"]
    
    %% Legacy System
    Shell -.->|cc / ccd (Historical Baseline)| CSharpLegacy["🏛️ C# AgyTui (.NET 9.0 Monolith)"]
    CSharpLegacy -->|WAL Relational Storage| SqliteDB[("SQLite (agytui.db V1-V7)")]
    CSharpLegacy -->|SM-2 Engine| LearningDecks["Flashcards & Curriculums"]
```

---

## 2. Modern Go Engine Suite (10 Applications)

The modern suite decomposes developer operations into 10 focused, independently testable, single-binary Go applications:

| Application | Path | Aliases | Primary Role & Capabilities | Architecture & Guide |
| :--- | :--- | :--- | :--- | :--- |
| **`agyx`** | [apps/agyx/](../apps/agyx) | `agyx`, `x` | **Master Proxy & Unified Cockpit**: Single entrypoint delegating CLI commands and hosting an interactive cockpit across core modules and tools. | [02_agyx_proxy_audit.md](reports/02_agyx_proxy_audit.md) |
| **`agyswitch`** | [apps/agyswitch/](../apps/agyswitch) | `agysw`, `agys`, `switch` | **Identity, Vault & Quota Engine**: AES-256 encrypted credential storage, automated OAuth token refresh, non-blocking background quota probing, instant credential switching, and isolated `GEMINI_HOME` subprocess launching. | [01_agyswitch_deep_audit.md](reports/01_agyswitch_deep_audit.md) |
| **`agyproj`** | [apps/agyproj/](../apps/agyproj) | `agyproj`, `agyp`, `proj` | **Project Hub & Workspace Registry**: Heuristic technology stack detection (C#, Go, Rust, Node, Python, Docker), Antigravity AI session inference cost/step calculations, and 1-tap IDE launch. | [04_agyproj_workspace_audit.md](reports/04_agyproj_workspace_audit.md) |
| **`agygit`** | [apps/agygit/](../apps/agygit) | `agygit`, `agyg` | **Git Fleet & Worktree Orchestrator**: Multi-repo status fleet scanner across `~/projects`, interactive selective staging, file rejection, 3-way conflict resolver, and multi-agent `.worktrees/` isolation. | [03_agygit_fleet_audit.md](reports/03_agygit_fleet_audit.md) |
| **`agydocker`** | [apps/agydocker/](../apps/agydocker) | `agydocker`, `agyd` | **Container Fleet & WSL2 RAM Guard**: Container lifecycle and Compose stack down/kill/prune management, non-blocking async operations, direct `/proc/meminfo` kernel RAM/Swap gauges. | [05_agydocker_container_audit.md](reports/05_agydocker_container_audit.md) |
| **`agyterm`** | [apps/agyterm/](../apps/agyterm) | `agyterm`, `agyt` | **Terminal Themes & Diagnostics**: Direct JSON AST mutation of Windows Terminal `settings.json` from WSL2 with safety backups, Oh-My-Posh dynamic color previewer, and 9-subsystem shell health diagnostics. | [06_agyterm_theme_audit.md](reports/06_agyterm_theme_audit.md) |
| **`agymobile`** | [apps/agymobile/](../apps/agymobile) | `agymobile`, `agym` | **Mobile Cockpit & Remote Station**: Ultra-compact 38-column smartphone portrait TUI, embedded Web Cockpit on port 7890 over Tailscale mesh network, host optimization, and AI thought stream telemetry. | [07_agymobile_cockpit_audit.md](reports/07_agymobile_cockpit_audit.md) |
| **`agyollama`** | [apps/agyollama/](../apps/agyollama) | `agyollama`, `agyo`, `ai` | **Local AI & Ollama Cockpit**: Daemon lifecycle, model manager (pull/delete/default), hardware inference benchmark, non-blocking animated TUI, and subshell AI pair programming. | [09_agyollama_local_ai_plan.md](reports/09_agyollama_local_ai_plan.md) |
| **`agybot`** | [apps/agybot/](../apps/agybot) | `agybot`, `bot` | **Remote Telegram Controller & Research Daemon**: Secure 2FA Telegram control, multi-project execution, research delegation, auto-lock timeouts, and background daemon lifecycle. | [agybot_system_architecture.md](01_architecture/agybot_system_architecture.md) |
| **`agyport`** | [apps/agyport/](../apps/agyport) | `agyport`, `port` | **Active Port & RAM Leverage Manager**: Real-time listening port inspector, selective/all dev server port killer, framework detection (Vite, Next, Express, .NET), and RAM optimizer. | [agyport_system_architecture.md](01_architecture/agyport_system_architecture.md) |

> [!NOTE]
> **🛠️ Streamlined Cockpit Architecture**: The `agyx` TUI organizes developer workflows into daily drivers and utilities. For LocalStack recipes and AWS cloud CLI helpers, see [docs/04_command_enhancements/04_aws_localstack_cheatsheet.md](04_command_enhancements/04_aws_localstack_cheatsheet.md). For authentication and multi-account vault architecture, see [docs/01_architecture/agyswitch_engine.md](01_architecture/agyswitch_engine.md).

---

## 3. 📋 Audit & Deep-Dive Reports

In September 2026, an exhaustive forensic code and architecture audit was executed across the suite. The findings, benchmarks, security reviews, and parity comparisons are published in the dedicated reports catalog:

* **Reports Catalog Index**: **[reports/README.md](reports/README.md)**
* **Master System Audit & Product Roadmap**: **[00_agy_system_audit_and_roadmap.md](reports/00_agy_system_audit_and_roadmap.md)**
* **Local AI & Ollama Architecture Specification**: **[09_agyollama_local_ai_plan.md](reports/09_agyollama_local_ai_plan.md)**
* **Agygit Interactive Staging & Conflict Plan**: **[10_agygit_staging_reject_conflict_plan.md](reports/10_agygit_staging_reject_conflict_plan.md)**

### Detailed Reports Index (00 – 18):
0. **[00_agy_system_audit_and_roadmap.md](reports/00_agy_system_audit_and_roadmap.md)**: Master forensic system audit, vulnerability matrix, legacy C# parity checklist, and 4-phase transformation roadmap.
1. **[01_agyswitch_deep_audit.md](reports/01_agyswitch_deep_audit.md)**: AES-256 token vault, OAuth refresh, CloudCode quota probing, empty `service/quota` package, and brittle Python CLI transcript parser.
2. **[02_agyx_proxy_audit.md](reports/02_agyx_proxy_audit.md)**: Master proxy architecture, Cockpit navigation, child process exit code erasure bug (`os.Exit(1)`), missing `agymobile` routing, and Unix Domain Socket IPC plans.
3. **[03_agygit_fleet_audit.md](reports/03_agygit_fleet_audit.md)**: Parallel fleet scanner, multi-agent worktrees, 10-subprocess per keystroke UI freeze, and branch checkout creation bug (`create=false`).
4. **[04_agyproj_workspace_audit.md](reports/04_agyproj_workspace_audit.md)**: Stack auto-detector, transcript AI cost/step analytics, uncached frame-render lag, and hardcoded Windows username path in `launcher.go`.
5. **[05_agydocker_container_audit.md](reports/05_agydocker_container_audit.md)**: Container/Compose management, `/proc/meminfo` WSL2 RAM/Swap gauges, volume pruning bug (`docker system prune` instead of `docker volume prune`), and async Braille spinner.
6. **[06_agyterm_theme_audit.md](reports/06_agyterm_theme_audit.md)**: Windows Terminal `settings.json` mutation from WSL2, Oh-My-Posh ANSI color previewer, empty `service/linuxterm` package, and 9 shell health checks.
7. **[07_agymobile_cockpit_audit.md](reports/07_agymobile_cockpit_audit.md)**: 38-column portrait TUI, embedded Web Cockpit (:7890), severe unauthenticated endpoint vulnerability (`docker-stop-all`), and Tailscale integration.
8. **[08_cs_legacy_system_parity_audit.md](reports/08_cs_legacy_system_parity_audit.md)**: Historical baseline of the monolithic C# system (`apps/agytui`), SQLite V1–V7 WAL persistence, SM-2 Spaced Repetition suite loss, AWS LocalStack, and the Go parity restoration roadmap.
9. **[09_agyollama_local_ai_plan.md](reports/09_agyollama_local_ai_plan.md)**: Dedicated Go Local AI & Ollama micro-app porting C# IOllamaClient/screens: REST client, streaming pull, hardware benchmark, non-blocking UI.
10. **[10_agygit_staging_reject_conflict_plan.md](reports/10_agygit_staging_reject_conflict_plan.md)**: Interactive porcelain status, single/all file change rejection, selective staging/unstaging, 3-way merge conflict resolution, diff inspector.
11. **[11_agyswitch_agydocker_nonblocking_report.md](reports/11_agyswitch_agydocker_nonblocking_report.md)**: Non-blocking UI implementation: UNIX `poll` 80ms loop, background quota probing, async stack/container down, continuous Braille spinners.
12. **[12_agyollama_implementation_report.md](reports/12_agyollama_implementation_report.md)**: Complete delivery and verification report for local Ollama & AI engine: 15/15 passing tests, models manager, benchmark, TUI.
13. **[13_agyx_agymobile_security_routing_report.md](reports/13_agyx_agymobile_security_routing_report.md)**: Fix report: Secure bearer token authentication, QR pairing, subshell terminal handoff, transparent proxy routing, 21 unit tests.
14. **[14_agyswitch_session_and_ollama_fix_report.md](reports/14_agyswitch_session_and_ollama_fix_report.md)**: Fix report: Ollama WSL2 path resolution, session persistence & consolidation across accounts, instant account switching, UI active dot fix.
15. **[15_agygit_agyproj_perf_ux_report.md](reports/15_agygit_agyproj_perf_ux_report.md)**: Fix report: Eliminating 10-subprocess per keystroke freeze in agygit, project caching, dynamic username resolution, interactive subshells.
16. **[16_agydocker_agyterm_fix_report.md](reports/16_agydocker_agyterm_fix_report.md)**: Fix report: Safe Docker volume pruning, empty package scaffolding (`service/linuxterm`), dynamic theme/font preview, Windows Terminal sync.
17. **[17_agymobile_plan.md](reports/17_agymobile_plan.md)**: Agymobile remote cockpit engineering specification, Tailscale mesh network routing, mobile PWA, and session inspection.
18. **[18_suite_verification_and_test_report.md](reports/18_suite_verification_and_test_report.md)**: Complete 8-app ecosystem verification, automated test suites (100+ unit tests passing), end-to-end integration validation.

---

## 4. Legacy C# Control Center (`AgyTui`) Historical Role

Prior to the Go micro-tool architecture, the developer environment was driven by **`AgyTui`**: a monolithic .NET 9.0 console application now archived in [archive/agytui/AgyTui/](../archive/agytui/AgyTui).

### Why the Suite Migrated to Go:
- **Startup Speed**: Cold-start dropped from **150ms–300ms (.NET CLR/JIT)** to **8ms–18ms (compiled native Go)**.
- **Memory Footprint**: Memory usage decreased from **45MB–85MB** to **6MB–14MB** per session.
- **Elimination of Shell Locking**: Loading .NET assemblies in-process inside PowerShell caused Win32 DLL file locks that blocked iterative rebuilds. Go standalone executables run out-of-process with zero locks.

### The Historical Value of the C# Codebase:
The C# codebase is **fully preserved in `archive/agytui/`** as an architectural reference and feature gold-standard. It contains **200+ C# files**, **272 registered command handlers**, **60+ DI services**, a unified **relational SQLite database with migrations V1 through V7**, and **261 passing xUnit tests**.

Critical features originally implemented in C# that are slated for parity restoration in Go include:
- **SuperMemo-2 (SM-2) Spaced Repetition Suite**: Japanese (Kana, Kanji, JLPT), English vocabulary, C# .NET 9, DSA, and STAR behavioral interview question banks (slated for new Go app `agylearn`).
- **Relational Transactional Persistence**: Migration from disconnected flat JSON files back to an embedded relational SQLite engine (via pure-Go `modernc.org/sqlite`).
- **Advanced Git Tooling**: AI Conventional Commit generator, visual ours/theirs conflict resolver, and `InvokeGitUndo()` commit undo stack.
- **AWS LocalStack Cloud Explorer**: S3, SQS, DynamoDB, Lambda, SSM parameter manager.

For a full technical analysis of the C# architecture and feature gaps, consult **[08_cs_legacy_system_parity_audit.md](reports/08_cs_legacy_system_parity_audit.md)**.

---

## 5. Centralized Documentation Sitemap

### 🏛️ 01. System Architecture (`docs/01_architecture/`)
* **Go Engine & Modern Architecture**:
  * [agyswitch_engine.md](01_architecture/agyswitch_engine.md): Go v2.0 engine, multi-account context isolation, AES-256 vault encryption, quota probing, and subprocess launcher.
  * [agymobile_tailscale_ssh_plan.md](01_architecture/agymobile_tailscale_ssh_plan.md): Remote mobile cockpit, Tailscale mesh networking, SSH port forwarding, and lightweight web sidecar.
  * [orca_multi_agent_ade_design.md](01_architecture/orca_multi_agent_ade_design.md): Worktree-first Agent Development Environment (ADE), inspired by Orca, enabling parallel AI coding subagents.
  * [skill_and_mcp_management_plan.md](01_architecture/skill_and_mcp_management_plan.md): Dual-scope orchestration (global `~/.gemini/` vs project `.agents/`) for skills, rules, and MCP servers.
* **Core Reference & C# Architectural Foundation**:
  * [system_overview_and_file_tree.md](01_architecture/system_overview_and_file_tree.md): Repository topology, file tree catalog, and subsystem interactions.
  * [overview.md](01_architecture/overview.md): Clean Architecture principles, Onion layer boundaries, and dependency inversion rules.
  * [ddd_bounded_contexts.md](01_architecture/ddd_bounded_contexts.md): Domain aggregate roots (Account, Workspace, AiAgent, LearnContext).
  * [database_persistence.md](01_architecture/database_persistence.md): Relational SQLite schema design, WAL mode, and Migrations V1–V7.
  * [seeding_pipeline.md](01_architecture/seeding_pipeline.md): MasterSeeder modular data ingestion pipeline for accounts, workspaces, and learning decks.

### 👤 02. User Guide (`docs/02_user_guide/`)
* [agyswitch_cli_tui.md](02_user_guide/agyswitch_cli_tui.md): Interactive hotkeys, headless CLI commands, quota checking, and account switching.
* [powershell_profile_shortcuts.md](02_user_guide/powershell_profile_shortcuts.md): Terminal navigation, Git helpers, and launcher aliases (`cc`, `cnav`, `agysw`, `proj`).
* [onboarding_and_setup.md](02_user_guide/onboarding_and_setup.md): Automated environment provisioning via `Install-AgyEnvironment.ps1` (Windows) and `setup-ubuntu.sh` (WSL/Linux).
* [tui_screen_catalog.md](02_user_guide/tui_screen_catalog.md): Spectre.Console interactive 3-pane dashboard layout and screen workflows.

### 🛠️ 03. Developer Guide (`docs/03_developer_guide/`)
* [dual_environment_workflow.md](03_developer_guide/dual_environment_workflow.md): Isolated Dev sandbox database (`agytui.dev.db`) vs. Production (`agytui.db`).
* [testing_and_architecture_rules.md](03_developer_guide/testing_and_architecture_rules.md): Reflection architecture tests, domain invariant checks, and unit test suites.
* [release_publishing.md](03_developer_guide/release_publishing.md): Standalone single-file binary compilation via `build-release.ps1`.

### 🚀 04. Command Enhancements (`docs/04_command_enhancements/`)
* [01_git_enhancement.md](04_command_enhancements/01_git_enhancement.md): Git status enhancements, branch management, repo nexus graph, and commit analytics.
* [02_dotnet_enhancement.md](04_command_enhancements/02_dotnet_enhancement.md): .NET build progress, test runner results tables, and lock-free cleanup.
* [03_docker_enhancement.md](04_command_enhancements/03_docker_enhancement.md): Container dashboards, image cleanup wizards, log pagers, and Compose helpers.
* [04_aws_enhancement.md](04_command_enhancements/04_aws_enhancement.md): AWS identity inspection, LocalStack status checks, S3 bucket browser, and SQS queues.
* [05_linux_neovim_ide_flow.md](04_command_enhancements/05_linux_neovim_ide_flow.md): Neovim IDE workflow, terminal multiplexing, and editor integration.
* [06_linux_cli_tools.md](04_command_enhancements/06_linux_cli_tools.md): High-speed modern CLI utilities (`fzf`, `eza`, `zoxide`, `bat`, `fd`, `ripgrep`).

### 🗄️ Archive (`docs/archive/`)
* [archive/](archive/): Historical task plans, mockup blueprints, interim refactoring blueprints, and audit reports preserved for full traceability.

---

## 6. Technology Stack & Comparison

| Subsystem | Technology | Execution Speed | Memory | Primary Purpose | Parity Status |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Orchestrator (`agyx`)** | Go 1.25 (Native) | <10ms | ~6MB | Unified CLI proxy, multi-module TUI cockpit | 🟢 Active |
| **Identity/Vault (`agyswitch`)** | Go 1.23 (Native) | ~12ms | ~8MB | Multi-account isolation, AES-256 vault, live quota probing | 🟢 Active |
| **Workspaces (`agyproj`)** | Go 1.25 (Native) | ~15ms | ~10MB | Stack detector, AI session analytics, IDE launcher | 🟢 Active |
| **Git Fleet (`agygit`)** | Go 1.25 (Native) | ~14ms | ~10MB | Parallel fleet status, `.worktrees/` agent isolation | 🟢 Active |
| **Containers (`agydocker`)** | Go 1.25 (Native) | ~15ms | ~11MB | Docker lifecycle, WSL2 `/proc/meminfo` RAM guard | 🟢 Active |
| **Terminal (`agyterm`)** | Go 1.25 (Native) | ~10ms | ~7MB | Windows Terminal JSON editor, Oh-My-Posh previews | 🟢 Active |
| **Mobile Remote (`agymobile`)** | Go 1.22 (Native) | ~15ms | ~12MB | 38-col smartphone TUI, Web Cockpit (:7890), Tailscale | 🟢 Active |
| **Local AI (`agyollama`)** | Go 1.25 (Native) | ~10ms | ~9MB | Local Ollama daemon manager, model pull/delete & benchmarks | 🟢 Active |
| **Cloud Explorer (`aws`)** | `agyx` Proxy + Profile | Instant | In-process / Go | LocalStack health probe (:4566), S3/SQS/STS CLI diagnostics | 🟢 Active |
| **Legacy Control Center (`AgyTui`)** | .NET 9.0 C# | 150ms–300ms | 45MB–85MB | Monolithic 3-pane UI, SQLite WAL database, SM-2 learning, AWS | 🏛️ Archived Reference |
| **Shell Integration** | PowerShell 7+ & Zsh | Instant | In-process | Prompt theming, path shortcuts, cross-platform aliases | 🟢 Active |

---

## 7. Cross-Platform Linking Standards (WSL2 + Windows VS Code)

All documentation in this repository strictly adheres to the workspace link standard:
1. **Workspace-Relative Markdown Links (Mandatory)**: All file and folder links use workspace-relative paths starting with `./` or `../`. These links are intercepted directly by VS Code's editor buffer and open in a new editor tab upon `Ctrl+Click`.
2. **Zero `file:///` URLs**: Absolute `file:///` URLs are strictly forbidden as they trigger Windows external file association errors.
3. **Dual Link References for Major Deliverables**: Significant documents provide both the clickable workspace link and the Windows UNC path (`\\wsl.localhost\Ubuntu\...`).
