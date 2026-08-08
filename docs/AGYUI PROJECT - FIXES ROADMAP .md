# AGYUI Project - Fixes Roadmap & Implementation Plan

> **Strategic Execution Plan**: Multi-phase roadmap for eliminating technical debt, refactoring core architectural components, establishing automated testing, and scaling system performance in `csapp/AgyTui/`.

---

## 1. Phased Architecture Strategy

```
  +---------------------------------------------------------------------------------+
  | TIER 1: CRITICAL BLOCKING TASKS (Weeks 1 - 3)                                   |
  | Target: Remove Service Locator | Setup xUnit Framework | Implement Command Pattern |
  | Target: Fix Silent Catches | Eliminate N+1 Queries | Convert Static State | IFileSystem|
  +---------------------------------------------------------------------------------+
                                         |
                                         v
  +---------------------------------------------------------------------------------+
  | TIER 2: HIGH PRIORITY STRUCTURAL IMPROVEMENTS (Weeks 4 - 5)                    |
  | Target: Split God Classes | Add Domain Events | Add Concurrent Cache Locks      |
  | Target: Database Query Indexing | Modular Bootstrapper Registrations              |
  +---------------------------------------------------------------------------------+
                                         |
                                         v
  +---------------------------------------------------------------------------------+
  | TIER 3: MEDIUM PRIORITY POLISHING & OPTIMIZATION (Weeks 6 - 7)                  |
  | Target: Unified IStateStore | Split Value Objects | Accessibility Modes             |
  | Target: Complete XML API Docs | O(1) Performance Optimizations & Final Verification|
  +---------------------------------------------------------------------------------+
```

---

## 2. Tier 1: Critical Tasks Roadmap (Weeks 1 to 3)

### Task 1.1: Service Locator Elimination
* **Duration**: 3 - 4 Days
* **Target Files**: 22+ files (`CommandRouter`, `TerminalIde`, `Config`, `ClaudeProvider`, `OllamaClient`, `AgyAccountStore`, `AgyQuotaEngine`, `AgyVault`, `AiProcessRunner`, `IdeCommandRegistry`, etc.).
* **Action Steps**:
  1. Audit occurrences: `grep -r "Bootstrapper.ServiceProvider" csapp/AgyTui/`.
  2. Modify target class constructors to accept dependencies via Constructor Injection.
  3. Update initialization calls and DI container setup in `Bootstrapper.cs`.
  4. Verify occurrence count equals `0`.

---

### Task 1.2: Unit Testing Framework Infrastructure
* **Duration**: 1 - 2 Weeks (Concurrent with T1 tasks)
* **Target Project**: `csapp/AgyTui.Tests`
* **Action Steps**:
  1. Scaffold xUnit project (`dotnet new xunit -n AgyTui.Tests -o csapp/AgyTui.Tests`).
  2. Add packages: `Moq`, `xunit`, `xunit.runner.visualstudio`.
  3. Create directory layout: `Unit/Domain`, `Unit/Infrastructure`, `Unit/UI`, `Integration`, `Mocks`.
  4. Author initial unit tests for domain aggregates (`AccountAggregateTests`).
  5. Configure GitHub Actions workflow (`.github/workflows/tests.yml`).

---

### Task 1.3: Command Pattern Implementation (`CommandRouter` Deconstruction)
* **Duration**: 2 - 3 Days
* **Target File**: `UI/Core/Navigation/CommandRouter.cs` (Original: 1,196 lines).
* **Action Steps**:
  1. Define `ICommand` interface (`Alias`, `ExecuteAsync(string[] args)`).
  2. Implement `CommandRegistry` backed by `Dictionary<string, ICommand>`.
  3. Split 300+ switch cases into 7 dedicated routers:
     * `GitCommandRouter`
     * `AwsCommandRouter`
     * `DockerCommandRouter`
     * `DotNetCommandRouter`
     * `AiCommandRouter`
     * `LearnCommandRouter`
     * `SystemCommandRouter`
  4. Delegate `CommandRouter.Execute` to `CommandRegistry`.
  5. Verify line count is `< 200` lines.

---

### Task 1.4: Silent Exception Handling Elimination
* **Duration**: 1 - 2 Days
* **Target Files**: 36+ files with empty `catch {}` blocks.
* **Action Steps**:
  1. Locate all silent catches: `grep -r "catch\s*{\s*}" csapp/AgyTui/`.
  2. Replace each with `catch (Exception ex) { LogHelper.LogError($"Context: {ex.Message}"); }`.
  3. Verify remaining count equals `0`.

---

### Task 1.5: Fix N+1 Query & I/O Patterns
* **Duration**: 12 Hours total (Parallel execution)
* **Work Packages**:
  * **1.5.1 AgyAccountStore (2 hrs)**: Cache account lists in memory.
  * **1.5.2 AgyQuotaEngine (3 hrs)**: Add JSONL timestamp check prior to re-parsing.
  * **1.5.3 LearningDataSeeder (2 hrs)**: Batch SQL insert statements (100 rows per query).
  * **1.5.4 WorkspaceRegistry (2 hrs)**: Extend cache TTL from 5s to 30min.
  * **1.5.5 N+1 GetBranch (2 hrs)**: Bulk fetch Git branches during workspace queries.

---

### Task 1.6: Static State Conversion to Injected Services
* **Duration**: 2 - 3 Days
* **Target Components**: `Config.Current` (50+ files), `AppPaths.*` (30+ files).
* **Action Steps**:
  1. Create `IConfigService` & `ConfigService`.
  2. Create `IPathProvider` & `PathProvider`.
  3. Register both services as singletons in DI container.
  4. Replace static calls with constructor injected service instances.
  5. Remove `static` modifiers from `Config.cs` and `AppPaths.cs`.

---

### Task 1.7: Filesystem Abstraction Layer (`IFileSystem`)
* **Duration**: 1 Day
* **Target Files**: 15+ files invoking `System.IO.File` and `Directory`.
* **Action Steps**:
  1. Define `IFileSystem` interface.
  2. Implement `FileSystem` wrapper for production.
  3. Create `MockFileSystem` for unit test mocking.
  4. Update file operations across repositories to consume `IFileSystem`.

---

## 3. Tier 2: High Priority Structural Modernization (Weeks 4 to 5)

| Task ID | Task Description | Target Files / Scope | Deliverable | Effort |
| :---: | :--- | :--- | :--- | :---: |
| **2.1** | Break Up God Classes | `TerminalIde`, `FlatTreeRenderer`, `WorkspaceRegistry`, `GitClient` | Split into single-responsibility components (`FileExplorer`, `MenuRendererBase`, etc.) | 2-3 Days |
| **2.2** | Implement Domain Events | Domain aggregates & application pipeline | Add `IDomainEvent` publisher and domain event handlers | 1-2 Days |
| **2.3** | Enforce Thread Safety | `WorkspaceRegistry` cache, `OllamaClient._defaultModel` | Protect static/shared caches with `lock` or `ReaderWriterLockSlim` | 4 Hours |
| **2.4** | Database Query Indexing | SQLite DB repositories | Add database indexes and query caching | 1-2 Days |
| **2.5** | Refactor DI Bootstrapper | `Infrastructure/Di/Bootstrapper.cs` | Organize 120+ registrations into `AddDomainServices`, `AddInfrastructureServices`, `AddUIServices` | 4 Hours |

---

## 4. Tier 3: Medium Priority Polish & Optimization (Weeks 6 to 7)

| Task ID | Task Description | Target Files / Scope | Deliverable | Effort |
| :---: | :--- | :--- | :--- | :---: |
| **3.1** | Unified State Management | UI Screens system-wide | Unify state under `IStateStore` pattern | 1-2 Days |
| **3.2** | Split AccountCredentials | `Domain/AccountContext/` | Split into `AuthTokenCredentials`, `OAuthCredentials`, `EmailCredentials` | 2 Hours |
| **3.3** | Accessibility Features | UI Theme system | Add high-contrast mode, font scaling, emoji ASCII fallbacks | 2-3 Days |
| **3.4** | Complete Documentation | API layer & ADRs | Add XML doc comments and Architecture Decision Records | 2-3 Days |
| **3.5** | Performance Optimization | System critical paths | Convert array lookups to $O(1)$ dictionaries; profile execution | 1-2 Days |

---

## 5. Master 7-Week Execution Schedule

| Week | Phase Focus | Key Deliverables & Milestones | Primary Tasks |
| :---: | :--- | :--- | :--- |
| **Week 1** | Tier 1 Execution | Service locator removal, xUnit test project setup, Command Pattern design | Tasks 1.1, 1.2, 1.3 |
| **Week 2** | Tier 1 Execution | Silent exception logging, N+1 query optimization, batch SQL inserts | Tasks 1.4, 1.5 |
| **Week 3** | Tier 1 Finalization | Static state conversion to services, `IFileSystem` implementation | Tasks 1.6, 1.7 |
| **Week 4** | Tier 2 Execution | Deconstruct `TerminalIde` & renderer god classes, domain event pipeline | Tasks 2.1, 2.2 |
| **Week 5** | Tier 2 Finalization | Thread safety locking, SQLite DB indexing, modular DI Bootstrapper | Tasks 2.3, 2.4, 2.5 |
| **Week 6** | Tier 3 Execution | `IStateStore` implementation, split `AccountCredentials`, accessibility modes | Tasks 3.1, 3.2, 3.3 |
| **Week 7** | Final Polish & Audit | Complete XML documentation, $O(1)$ performance optimization, final validation | Tasks 3.4, 3.5 |

---

## 6. Target Metrics & Verification Commands

To verify completion of each tier, execute the following commands:

### Service Locator Removal Check
```bash
grep -r "Bootstrapper.ServiceProvider" csapp/AgyTui/
# Target Output: 0 matches
```

### Silent Exception Check
```bash
grep -r "catch\s*{\s*}" csapp/AgyTui/
# Target Output: 0 matches
```

### CommandRouter Line Count Check
```bash
wc -l csapp/AgyTui/UI/Core/Navigation/CommandRouter.cs
# Target Output: < 200 lines
```

### Static Config Reference Check
```bash
grep -r "Config\.Current" csapp/AgyTui/
# Target Output: 0 matches
```

### Build & Test Suite Verification
```bash
dotnet build csapp/AgyTui.slnx
dotnet test csapp/AgyTui.Tests --collect:"XPlat Code Coverage"
# Target Coverage: >= 30% after Tier 1; >= 70% after Tier 3
```

---

## 7. Risk Mitigation & Team Governance

> [!WARNING]
> Refactoring core framework code without test validation risks breaking runtime behavior. Strict adherence to safety protocols is mandatory.

* **Test-First Discipline**: Write unit tests for domain aggregates *before* altering internal logic.
* **Feature Branch Isolation**: Execute major refactoring tasks in isolated feature branches (`refactor/service-locator`, `refactor/command-pattern`).
* **Automated CI Validation**: Enforce passing unit tests and static analysis on all Pull Requests.
* **Zero Silent Fallbacks**: Never catch exceptions silently; always log detailed contextual diagnostics.
