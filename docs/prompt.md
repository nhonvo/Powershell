# AGYUI Project - Automated Execution Prompt for AI Agents

> **Execution Directive**: Execute all 17 TIER 1 + TIER 2 + TIER 3 tasks in sequence without requiring user confirmation. Continue until all tasks are verified complete.

---

## 1. Project Reference & Scope

* **Project Root Path**: `csapp/AgyTui/` (Absolute: `C:\Users\TruongNhon\Documents\Powershell\csapp\AgyTui\`)
* **Test Project Path**: `csapp/AgyTui.Tests/`
* **Execution Mode**: Autonomous / Automated (No Manual Prompts Required)

### Reference Documents
* [Agent Consolidated Overview](file:///C:/Users/TruongNhon/Documents/Powershell/docs/agent%20consolidated.md) - Architecture Analysis & Scores
* [All 47 Issues Breakdown](file:///C:/Users/TruongNhon/Documents/Powershell/docs/AGYUI%20PROJECT%20-%20ALL%2047%20ISSUES%20BREAKDOWN.md) - Itemized Catalog of Technical Debt
* [Fixes Roadmap](file:///C:/Users/TruongNhon/Documents/Powershell/docs/AGYUI%20PROJECT%20-%20FIXES%20ROADMAP%20.md) - Phased Implementation Plan & Schedule

---

## 2. Phase 1: Tier 1 Critical Tasks (Blocking - 2 to 3 Weeks)

### Task 1.1: Remove Service Locator Anti-Pattern
* **Goal**: Replace all `Bootstrapper.ServiceProvider.GetRequiredService<T>()` static calls with Constructor Injection across 22+ files.
* **Steps**:
  1. Search for all service locator usages:
     ```bash
     grep -r "Bootstrapper.ServiceProvider.GetRequiredService" csapp/AgyTui/
     ```
  2. For each identified location, update the host class constructor to request the required dependency explicitly via Dependency Injection (DI).
  3. Key files to modify (including but not limited to):
     * `UI/Core/Navigation/CommandRouter.cs` (line 44)
     * `UI/Screens/Ide/TerminalIde.cs` (line 88)
     * `Infrastructure/Configuration/Config.cs` (line 106)
     * `Infrastructure/Integrations/Ai/Providers/ClaudeProvider.cs` (line 17)
     * `Infrastructure/Integrations/Ai/Providers/OllamaClient.cs`
     * `Infrastructure/Integrations/Ai/Providers/HermesProvider.cs`
     * `Infrastructure/Integrations/Ai/Providers/OpenClawProvider.cs`
     * `Infrastructure/Integrations/AgyClient/AgyAccountStore.cs`
     * `Infrastructure/Integrations/AgyClient/AgyQuotaEngine.cs`
     * `Infrastructure/Integrations/AgyClient/AgyVault.cs`
     * `Infrastructure/Integrations/Ai/Services/AiProcessRunner.cs`
     * `Infrastructure/Integrations/Ai/Services/AiDashboardView.cs`
     * `Infrastructure/Registries/IdeCommandRegistry.cs`
     * `Infrastructure/Common/CommandInvocationLog.cs`
     * `Infrastructure/Persistence/DbContext/LearnDataPaths.cs`
     * `UI/Core/Common/StatusWidgets.cs`
     * `UI/Core/Layouts/ScreenChrome.cs`
     * `UI/Core/Navigation/AccountViewHelper.cs`
     * `UI/Core/Navigation/CcNavigator.cs`
     * `UI/Core/Navigation/SubPageAccountNavigator.cs`
     * `UI/Core/Navigation/SubPageNavigator.cs`
     * `UI/Core/Navigation/SubPageThemeNavigator.cs`
  4. Update `Bootstrapper.cs` registrations where factory methods relied on direct static resolution.
  5. **Verification**: `grep -r "Bootstrapper.ServiceProvider" csapp/AgyTui/` must return `0` results (excluding `Program.cs` entry point if applicable).
* **Commit Message**: `refactor: remove service locator anti-pattern throughout codebase`

---

### Task 1.2: Implement Unit Testing Framework & Coverage Infrastructure
* **Goal**: Setup xUnit project, Moq, initial domain tests, and automated test runners targeting >= 10% initial coverage (aiming for 70%+ overall).
* **Steps**:
  1. Create xUnit test project:
     ```bash
     dotnet new xunit -n AgyTui.Tests -o csapp/AgyTui.Tests
     ```
  2. Add test dependencies:
     ```bash
     dotnet add csapp/AgyTui.Tests package Moq
     dotnet add csapp/AgyTui.Tests package xunit
     dotnet add csapp/AgyTui.Tests package xunit.runner.visualstudio
     dotnet add csapp/AgyTui.Tests reference csapp/AgyTui/AgyTui.csproj
     ```
  3. Establish folder hierarchy:
     * `csapp/AgyTui.Tests/Unit/Domain/`
     * `csapp/AgyTui.Tests/Unit/Infrastructure/`
     * `csapp/AgyTui.Tests/Unit/UI/`
     * `csapp/AgyTui.Tests/Integration/`
     * `csapp/AgyTui.Tests/Mocks/`
  4. Write core domain aggregate unit tests:
     * `csapp/AgyTui.Tests/Unit/Domain/AccountAggregateTests.cs` (Testing initialization, active state toggles, invariant validation).
  5. Configure GitHub Actions CI workflow in `.github/workflows/tests.yml` to run tests on all commits.
  6. **Verification**: Execute tests locally and verify coverage report:
     ```bash
     dotnet test csapp/AgyTui.Tests --collect:"XPlat Code Coverage"
     ```
* **Commit Message**: `test: set up xUnit testing framework with initial domain tests`

---

### Task 1.3: Implement Command Pattern to Deconstruct CommandRouter
* **Goal**: Reduce `CommandRouter.cs` from 1,196 lines (300+ switch cases) to < 200 lines by introducing `ICommand` and modular routers.
* **Steps**:
  1. Create command interface `csapp/AgyTui/UI/Core/Commands/ICommand.cs`:
     ```csharp
     namespace AgyTui.UI.Core.Commands;

     public interface ICommand
     {
         string Alias { get; }
         Task ExecuteAsync(string[] args);
     }
     ```
  2. Create registry `csapp/AgyTui/UI/Core/Commands/CommandRegistry.cs` using an $O(1)$ dictionary lookup mechanism.
  3. Extract domain-focused routers:
     * `GitCommandRouter` (`git-*` commands)
     * `AwsCommandRouter` (`aws-*` commands)
     * `DockerCommandRouter` (`docker-*` commands)
     * `DotNetCommandRouter` (`dotnet-*` commands)
     * `AiCommandRouter` (`claude`, `ollama`, `hermes` commands)
     * `LearnCommandRouter` (`learn`, `flashcard` commands)
     * `SystemCommandRouter` (system utility commands)
  4. Register all command handlers and the `CommandRegistry` in `Bootstrapper.cs`.
  5. Delegate `CommandRouter.Execute` to lookup and execute from `CommandRegistry`.
  6. **Verification**: Verify line count of `CommandRouter.cs`:
     ```bash
     wc -l csapp/AgyTui/UI/Core/Navigation/CommandRouter.cs
     ```
     Must be `< 200` lines.
* **Commit Message**: `refactor: implement command pattern, split commandrouter into focused routers`

---

### Task 1.4: Eliminate Silent Exception Handling
* **Goal**: Replace all empty `catch {}` blocks across 36+ files with structured logging.
* **Steps**:
  1. Locate empty catch blocks:
     ```bash
     grep -r "catch\s*{\s*}" csapp/AgyTui/
     ```
  2. Replace silent catches with logged context:
     ```csharp
     // Before
     catch { }

     // After
     catch (Exception ex)
     {
         LogHelper.LogError($"Operation failed: {ex.Message}");
     }
     ```
  3. Critical target files: `ProcessRunner.cs`, `SqliteConfigRepository.cs`, `SqliteWorkspaceRepository.cs`, `OllamaClient.cs`, `TerminalIde.cs`, and 31 additional locations.
  4. **Verification**: `grep -r "catch\s*{\s*}" csapp/AgyTui/` returns `0`.
* **Commit Message**: `fix: add logging to all exception handlers, remove silent catch blocks`

---

### Task 1.5: Fix N+1 Query & I/O Bottlenecks
* **Goal**: Optimize data access, filesystem scans, and seeder loops.
* **Sub-tasks**:
  1. **AgyAccountStore (Task 1.5.1)**: Cache `GetAccounts()` result instead of rescanning directory on every evaluation.
  2. **AgyQuotaEngine (Task 1.5.2)**: Check JSONL file modification timestamp (`LastWriteTime`) before triggering expensive re-parsing.
  3. **LearningDataSeeder (Task 1.5.3)**: Replace `foreach` single-row `INSERT` loops with batch inserts of 100 flashcards per statement.
  4. **WorkspaceRegistry (Task 1.5.4)**: Extend cache TTL from 5 seconds to 30 minutes for directory workspace scans.
  5. **N+1 Branch Queries (Task 1.5.5)**: Include Git branch state within initial workspace fetch queries.
* **Commit Message**: `perf: fix N+1 query patterns, add caching and batching`

---

### Task 1.6: Convert Static Mutable State to Injected Services
* **Goal**: Eliminate static singletons `Config.Current` and `AppPaths.*` in favor of DIP-compliant injected services.
* **Steps**:
  1. Create `IConfigService` (`GetConfig()`, `SetConfig()`, `OnConfigReloaded`) and `ConfigService` implementation.
  2. Create `IPathProvider` (`GetProjectRoot()`, `GetRepoRoot()`, `GetDataDirectory()`) and `PathProvider` implementation.
  3. Register both services as Singletons in `Bootstrapper.cs`.
  4. Replace 50+ instances of `Config.Current` with `_configService.GetConfig()`.
  5. Replace 30+ instances of `AppPaths.*` with `_pathProvider` calls.
  6. Remove `static` accessors from `Config.cs` and `AppPaths.cs`.
  7. **Verification**:
     ```bash
     grep -r "Config\.Current" csapp/AgyTui/
     grep -r "AppPaths\." csapp/AgyTui/Infrastructure/
     ```
     Both must return `0` occurrences.
* **Commit Message**: `refactor: convert Config and AppPaths static state to injected services`

---

### Task 1.7: Introduce FileSystem Abstraction (`IFileSystem`)
* **Goal**: Decouple infrastructure repositories and services from `System.IO.File` and `System.IO.Directory` to allow unit testing with mocks.
* **Steps**:
  1. Create `csapp/AgyTui/Infrastructure/Persistence/IFileSystem.cs` defining:
     * `FileExists(path)`, `DirectoryExists(path)`
     * `ReadAllText(path)`, `WriteAllText(path, content)`
     * `ReadAllLines(path)`, `ReadLines(path)`
     * `GetFiles(path, pattern)`, `GetDirectories(path, pattern)`
     * `GetFileInfo(path)`, `GetDirectoryInfo(path)`
  2. Create `FileSystem.cs` wrapping `System.IO`.
  3. Create `MockFileSystem.cs` in `csapp/AgyTui.Tests/Mocks/` using an in-memory dictionary.
  4. Register `services.AddSingleton<IFileSystem, FileSystem>();` in `Bootstrapper.cs`.
  5. Replace static `File.*` and `Directory.*` calls across 15+ files with `_fileSystem`.
* **Commit Message**: `refactor: create IFileSystem abstraction for testability`

---

## 3. Phase 2: Tier 2 High Priority Tasks (2 Weeks)

### Task 2.1: Deconstruct God Classes
* **Split `TerminalIde.cs` (832 lines)**: Extract `FileExplorer`, `CodeViewer`, `SymbolSearcher`, `IDECommandHandler`.
* **Split Menu Renderers**: Extract common template logic from `FlatTreeRenderer` (665 lines) and `ThreePaneRenderer` (479 lines) into `MenuRendererBase`.
* **Split `WorkspaceRegistry.cs` (684 lines)**: Separate into `WorkspaceDiscoverer`, `WorkspaceCache`, and `WorkspaceValidator`.
* **Split `GitClient.cs` (545 lines)**: Refactor into modular sub-services.
* **Commit Message**: `refactor: break up god classes into focused, single-responsibility classes`

---

### Task 2.2: Implement Domain Events Pipeline
* **Steps**:
  1. Create `IDomainEvent` interface and dispatch infrastructure.
  2. Implement domain events: `AccountActivatedEvent`, `AccountQuotaExceededEvent`, `WorkspaceCreatedEvent`, `FlashcardReviewedEvent`, `StudySessionCompletedEvent`.
  3. Publish domain events during state changes in domain aggregates.
* **Commit Message**: `feat: implement domain events for audit and cross-context communication`

---

### Task 2.3: Ensure Thread Safety across Caches & State
* **Steps**:
  1. Synchronize static cache access (`WorkspaceRegistry` TTL cache, `OllamaClient._defaultModel`) using thread locks or `ReaderWriterLockSlim`.
* **Commit Message**: `fix: add thread safety locks to static caches`

---

### Task 2.4: Optimize Persistence Layer & Database Queries
* **Steps**:
  1. Add database indexing on frequently queried columns in SQLite tables.
  2. Implement query result caching and batch write operations.
* **Commit Message**: `perf: optimize database queries and add indexes`

---

### Task 2.5: Modularize Dependency Injection Bootstrapper
* **Steps**:
  1. Break 120+ manual registrations in `Bootstrapper.cs` into extension modules: `AddDomainServices()`, `AddInfrastructureServices()`, `AddUIServices()`.
* **Commit Message**: `refactor: organize bootstrapper into modular service registration`

---

## 4. Phase 3: Tier 3 Medium Priority Tasks (1 to 2 Weeks)

### Task 3.1: Unified State Management (`IStateStore`)
* Consolidate static state, instance state, and event callbacks into a single `IStateStore` pattern.
* **Commit Message**: `refactor: consolidate state management into unified pattern`

### Task 3.2: Decompose `AccountCredentials` Value Object
* Split bloated single class into `AuthTokenCredentials`, `OAuthCredentials`, and `EmailCredentials`.
* **Commit Message**: `refactor: split AccountCredentials into focused value objects`

### Task 3.3: Accessibility Enhancements
* Add color-blind friendly visual themes, consistent emoji fallback routines, and font scaling options.
* **Commit Message**: `feat: add accessibility modes (color-blind, font scaling)`

### Task 3.4: Comprehensive Documentation & ADRs
* Add XML doc comments to all public APIs and create Architecture Decision Records (ADRs).
* **Commit Message**: `docs: add XML documentation and ADRs`

### Task 3.5: Critical Path Performance Optimization
* Refactor command lookup arrays to $O(1)$ hash maps, enable lazy loading for UI components, and profile critical paths.
* **Commit Message**: `perf: optimize performance critical paths`

---

## 5. Final Automated Verification Criteria

After completing all tasks, run the following verification suite:

```bash
# 1. Verify zero Service Locator calls
grep -r "Bootstrapper.ServiceProvider" csapp/AgyTui/

# 2. Verify zero empty catch blocks
grep -r "catch\s*{\s*}" csapp/AgyTui/

# 3. Verify CommandRouter reduction (< 200 lines)
wc -l csapp/AgyTui/UI/Core/Navigation/CommandRouter.cs

# 4. Verify static Config access removed
grep -r "Config\.Current" csapp/AgyTui/

# 5. Build solution cleanly
dotnet build csapp/AgyTui.slnx

# 6. Execute all unit tests and verify target coverage (>= 70%)
dotnet test csapp/AgyTui.Tests --collect:"XPlat Code Coverage"
```

---

## 6. Final Deliverable & Status Update

Upon completing all verification commands:
1. Create final commit: `chore: complete all tier 1-3 refactoring tasks`
2. Push updates to main repository branch.
3. Update project documentation files with final status report.
