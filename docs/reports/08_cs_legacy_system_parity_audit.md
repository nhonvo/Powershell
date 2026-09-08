# 🏛️ Comprehensive Audit & Parity Report: Legacy C# System (`AgyTui`) vs. Go Engine Suite

> **Category**: Historical System Audit & Architectural Parity  
> **Target Legacy Codebase**: `apps/agytui/AgyTui` & `apps/agytui/AgyTui.Tests`  
> **Source Files**: [apps/agytui/AgyTui/Program.cs](../../apps/agytui/AgyTui/Program.cs) · [apps/agytui/AgyTui/Infrastructure/Di/Bootstrapper.cs](../../apps/agytui/AgyTui/Infrastructure/Di/Bootstrapper.cs) · [apps/agytui/AgyTui/Infrastructure/Persistence/DbContext/SqliteDatabase.cs](../../apps/agytui/AgyTui/Infrastructure/Persistence/DbContext/SqliteDatabase.cs) · [apps/agytui/AgyTui/Infrastructure/Integrations/AgyClient/AgyVault.cs](../../apps/agytui/AgyTui/Infrastructure/Integrations/AgyClient/AgyVault.cs) · [apps/agytui/AgyTui/UI/Core/Navigation/CommandRouter.cs](../../apps/agytui/AgyTui/UI/Core/Navigation/CommandRouter.cs)  
> **Windows UNC Reference**: `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\apps\agytui`

---

## 1. Executive Summary

Before the Antigravity developer suite was re-engineered into native Go micro-tools (`agyswitch`, `agyproj`, `agygit`, `agydocker`, `agyterm`, `agyx`, `agymobile`), the developer ecosystem was powered by **`AgyTui`**: a monolithic .NET 9.0 console control center comprising **over 200 C# source files**, **272 registered command aliases**, a unified **relational SQLite database (migrations V1 through V7)**, and an extensive test suite of **261 xUnit unit, integration, and architecture tests**.

While the migration to Go achieved **sub-15ms execution speeds** and eradicated PowerShell assembly file-lock hazards, the Go suite omitted or fragmented several mature subsystems from C#. This audit details the C# system's architecture, security model, capabilities, performance bottlenecks, and provides an itemized parity matrix.

```
                      C# AgyTui Architecture (Monolith)
┌────────────────────────────────────────────────────────────────────────┐
│ UI: Spectre.Console · 3-Pane Layout · CommandRouter (272 Aliases)      │
├────────────────────────────────────────────────────────────────────────┤
│ Infrastructure: Bootstrapper (60+ DI Services) · SqliteDatabase (WAL)  │
│ Migrations V1-V7 · DPAPI / Credential Manager · LocalStack · DotNet EF │
├────────────────────────────────────────────────────────────────────────┤
│ Domain: Pure Business Aggregates (Account, Workspace, FlashcardDeck)   │
└────────────────────────────────────────────────────────────────────────┘

              Converted To: Go Micro-App Suite (7 Binaries)
┌────────────┬────────────┬────────────┬────────────┬────────────┬───────┐
│ agyswitch  │  agyproj   │   agygit   │ agydocker  │  agyterm   │ agyx  │
└────────────┴────────────┴────────────┴────────────┴────────────┴───────┘
```

---

## 2. Architecture & Design of Legacy C# System

### 2.1 Clean Architecture Enforcement
The C# codebase strictly enforced Clean Architecture principles verified by reflection tests (`ArchitectureTests.cs`):
1. **Domain Layer (`AgyTui.Domain`)**: Pure business logic with zero framework dependencies. Contained aggregates:
   - `AccountAggregate`: Developer account lifecycle transitions, usage tracking, and quota limits.
   - `WorkspaceAggregate`: Project path invariants, accounts association, and custom aliases.
   - `FlashcardDeck`: SuperMemo-2 spaced repetition state, repetition intervals, and ease factors.
2. **Infrastructure Layer (`AgyTui.Infrastructure`)**:
   - `Bootstrapper.cs`: Centralized DI container registering over 60 services and repositories using `Microsoft.Extensions.DependencyInjection`.
   - `CommandLoggingMiddleware.cs`: Automatic audit interceptor logging alias invocations, durations, and account context to SQLite.
3. **UI Layer (`AgyTui.UI`)**:
   - `ThreePaneRenderer.cs`: Left pane (categories), middle pane (expandable command tree with live search), right pane (dynamic widgets and live telemetry).
   - `ScreenChrome.cs`: Frame-buffered ANSI renderer eliminating screen flicker.

### 2.2 Relational SQLite Engine & Migrations (V1 to V7)
- **Path**: `~/.gemini/agytui.db` (or `agytui.dev.db` in development mode).
- **Concurrency PRAGMAs**: Configured with Write-Ahead Logging (`PRAGMA journal_mode=WAL;`), `PRAGMA synchronous=NORMAL;`, and `PRAGMA busy_timeout=5000;`.
- **Native DLL Resolver**: Registered dynamic Win32/Linux P/Invoke resolvers in `SqliteDatabase.cs` to resolve `e_sqlite3.dll` regardless of PowerShell working directory.
- **Migration Pipeline (`SqliteMigrationEngine.cs`)**:
  - `V1`: `app_config`, `accounts`, `system_state`.
  - `V2`: `command_invocation_logs` audit trail.
  - `V3`: `workspaces`, `flashcard_decks`, `flashcards` with relational foreign keys.
  - `V4`: `themes`, `ai_invocation_logs`.
  - `V5`: `resources`, `skills`.
  - `V6`: `quiz_questions` with company interview taxonomy.
  - `V7`: Encrypted credential storage columns on `accounts`.

### 2.3 Security & Credential Isolation
- **Windows DPAPI Integration**: Encrypted OAuth refresh tokens and credentials via `ProtectedData.Protect(data, entropy, DataProtectionScope.CurrentUser)` salted with `"AgyTui_Secure_Entropy_v1"`.
- **Windows Credential Manager P/Invoke**: Direct native Win32 calls to `advapi32.dll` (`CredReadW`, `CredWriteW`, `CredDeleteW`) for target `"gemini:antigravity"`.
- **Atomic Context Switching**: Pre-switch token backup, full bidirectional directory mirroring excluding locks/databases, NTFS junction verification, and garbage collection / connection pool clearing (`SqliteConnection.ClearAllPools()`) across 5 retries.

---

## 3. Comprehensive Feature Catalog of C# System

### 3.1 Account & Rolling Quota Engine
- Dual rolling quota windows: 5-hour window (50-request limit) and 7-day window (1,000-request limit).
- Time-to-exhaustion forecasting and 15-minute release projection (`ForecastQuotaRelease`).
- `AutoSwitchOnQuotaExceeded()`: Automatic failover to the healthiest backup account when an active account hits a rate limit.
- Low-quota webhook alerts (`TriggerLowQuotaWebhookAsync`).

### 3.2 DotNet SDK & EF Core Automation
- `dclean`: Recursive traversal and purging of `bin` and `obj` folders across entire solutions.
- Entity Framework Core migration tools: `add-migration`, `update-db`, `dremove`, `dd`.
- Solution management: `sln-add` (automatically discovers and attaches all projects to a `.sln`).
- Interactive NuGet package build and publishing wizard (`dpack`, `dpubpkg`).

### 3.3 Advanced Git Client (`GitClient.cs`)
- Conventional Commit Wizard with AI-assisted draft generator from `git diff --cached`.
- `git-undo`: Soft-reset uncommit (`git reset --soft HEAD~1`) with commit preview.
- Interactive Conflict Resolution Assistant (`ShowConflictResolver`): Visual conflict table with 1-click `Accept Ours`, `Accept Theirs`, and `diff --cc`.
- Stash manager, rebase wizard, and branch sorting by committer date.

### 3.4 AWS LocalStack Cloud Suite (`AwsClient.cs`)
- Unified querying and manipulation of local cloud sandboxes on port 4566:
  - S3: Bucket listing and creation.
  - SQS: Queue creation, purge, message send/receive with FIFO deduplication.
  - DynamoDB: Table discovery and attribute inspection.
  - Lambda: Function inventory.
  - SSM & SNS: Parameter store and topic enumeration.

### 3.5 Spaced Repetition Learning Suite (SM-2)
- Complete implementation of the SuperMemo-2 (SM-2) spaced repetition algorithm.
- Interactive review TUI with due date filtering (`IsDueToday`), ease factor tracking, and mastered status.
- Rich developer curriculums:
  - **Japanese**: Kana (Hiragana & Katakana), Kanji (stroke count, On/Kun, mnemonics), JLPT N5 vocab.
  - **English**: Intermediate vocabulary with IPA pronunciation, antonyms, synonyms, and examples.
  - **C# / .NET 9**: Deep technical quizzes with explanations.
  - **DSA**: Complexity cheat sheets and Big-O algorithm cards.
  - **Career & System Design**: Behavioral questions categorized by FAANG company and STAR framework templates.
- **Obsidian Sync Bridge**: Automatic markdown export of study sessions into local Obsidian vaults.

### 3.6 AI Agent Integrations
- CLI orchestration abstractions for Claude, Ollama (port 11434 with auto-start `ollama serve`), Hermes, and OpenClaw (port 18789 gateway).
- AI project scanner categorizing workspaces based on `CLAUDE.md`, `Modelfile`, `AGY.md`.
- AI learning generator synthesizing flashcard decks via automated CLI prompts.

---

## 4. Performance Comparison: Why Go Was Built

```
                               PERFORMANCE BENCHMARK
┌───────────────────────────────┬──────────────────────┬──────────────────────┐
│ Metric                        │ Legacy C# (AgyTui)   │ Go Micro-Tools Suite │
├───────────────────────────────┼──────────────────────┼──────────────────────┤
│ Cold-Start Execution Time     │ 150ms – 300ms        │ 8ms – 18ms           │
│ Stale Build Penalty           │ 2,000ms – 5,000ms    │ 0ms (Precompiled)    │
│ Memory Footprint per Session  │ 45MB – 85MB          │ 6MB – 14MB           │
│ Process / DLL Locking         │ Win32 file lock risk │ Zero persistent locks│
│ Architecture Coupling         │ Single Monolith      │ 7 Focused Micro-Apps │
└───────────────────────────────┴──────────────────────┴──────────────────────┘
```

The C# system suffered from two critical operational flaws in PowerShell:
1. **PowerShell Profile Startup Hook**: Opening a new terminal tab executed `Load-AgyTuiDll`, checking file modification times across all `.cs` files and triggering a `dotnet build` in-line if source files changed, causing a 2-5 second stall.
2. **Assembly File Locking**: In-process DLL loading locked binary files on disk, requiring complex reflection hacks (`[System.IO.File]::ReadAllBytes`) to avoid locking files during development.

---

## 5. Comprehensive Parity Gap Matrix

| Subsystem / Capability | Legacy C# `AgyTui` | Go Engine Suite | Parity Verdict |
| :--- | :--- | :--- | :--- |
| **Startup Speed & RAM** | 150–300ms, 45–85MB RAM. | 8–18ms, 6–14MB RAM. | 🟢 **Major Go Victory** |
| **Unified Database** | SQLite (`agytui.db`) with migrations V1–V7. | Fragmented flat JSON files. | ❌ **Major Regression in Go** |
| **Windows Credential Vault**| DPAPI + Win32 `advapi32.dll` P/Invoke. | AES-256-GCM + file tokens. | ⚠️ **Degraded in Go** |
| **Live Quota Probing** | Local calculations from activity log. | Real-time Google CloudCode API. | 🟢 **Go Victory** |
| **Automatic Quota Failover** | Auto-switches account on 429 errors. | Manual switch only. | ❌ **Missing in Go** |
| **Spaced Repetition (SM-2)** | Full SM-2 engine & multi-deck curriculum. | None. | ❌ **100% Omitted in Go** |
| **AWS LocalStack Integration**| S3, SQS, DynamoDB, Lambda, SSM, SNS. | None. | ❌ **100% Omitted in Go** |
| **DotNet SDK & EF Core Tools**| `dclean`, migrations, solution, NuGet wizard.| None. | ❌ **100% Omitted in Go** |
| **AI Commit Wizard** | AI draft generation from cached diff. | Basic string prompt. | ❌ **Missing in Go** |
| **Git Conflict Resolver** | Ours/Theirs visual resolution table. | Raw CLI error dump. | ❌ **Missing in Go** |
| **Git Undo Stack** | `InvokeGitUndo()` (`reset --soft HEAD~1`). | Basic unstage only. | ❌ **Missing in Go** |
| **Multi-Agent Worktrees** | None (manual branches). | Native `.worktrees/<branch>` in `agygit`.| 🟢 **Major Go Victory** |
| **WSL2 Kernel RAM Guard** | None. | `/proc/meminfo` bar meters in `agydocker`.| 🟢 **Major Go Victory** |
| **Mobile Tailscale Cockpit** | None. | 38-col portrait TUI & PWA in `agymobile`.| 🟢 **Major Go Victory** |
| **Test Coverage & Harness** | 261 xUnit tests (Architecture & Parity). | ~10 package unit tests. | ❌ **Major Gap in Go** |

---

## 6. Strategic Recommendations for the Go Suite

1. **Adopt a Pure-Go Centralized SQLite Database**:
   Replace scattered JSON files (`agyswitch_accounts.json`, `projects.json`, `active_account.txt`) with a unified SQLite database managed via `modernc.org/sqlite` (pure Go, zero CGO). This restores schema migrations, cross-app transactional queries, and ACID safety without reintroducing native DLL lockups.
2. **Port the Spaced Repetition Suite as `agylearn`**:
   The SM-2 flashcard engine and pre-seeded curriculums (C#, Japanese, DSA, STAR interview bank) represent a massive intellectual investment that should be ported to an independent native Go binary (`agylearn`).
3. **Port Git Conflict Resolver & AI Commit Wizard to `agygit`**:
   Incorporate visual ours/theirs conflict resolution and AI-generated commit messages into `agygit`.
4. **Port DotNet SDK & Cloud Tools as `agydotnet` / `agycloud`**:
   Implement `dclean` and LocalStack dashboard tools as lightweight Go micro-utilities or retain thin PowerShell profile functions.
