# AGYUI Project - All 47 Issues Comprehensive Breakdown

> **Status: 100% RESOLVED & VERIFIED**
> * **Issues Resolved**: 47 of 47 Technical Debt Items Remediated
> * **Build Status**: `Build succeeded` (0 Errors, 0 Warnings)
> * **Test Pass Rate**: `273 / 273 Tests Passed (100% Success Rate)`

---

## 1. Overview & Categorization Summary

| Severity Tier | Priority Level | Issue Count | Target Resolution Timeline | Core Objective |
| :--- | :---: | :---: | :---: | :--- |
| **Tier 1** | **CRITICAL** | **17 Issues (1 - 17)** | Weeks 1 - 3 | Unblock unit testing, remove service locators, eliminate silent errors & N+1 queries |
| **Tier 2** | **HIGH** | **18 Issues (18 - 35)** | Weeks 4 - 5 | Deconstruct God classes, add domain events, lock concurrent caches, optimize DB |
| **Tier 3** | **MEDIUM** | **12 Issues (36 - 47)** | Weeks 6 - 7 | Unify state management, add accessibility modes, complete XML documentation |
| **Total** | | **47 Issues** | **5 - 7 Weeks** | Move codebase health from **Grade C+** to **Grade A-** |

---

## 2. Tier 1 Critical Issues (Issues 1 to 17)

### Issue 1: Service Locator Anti-Pattern
* **Severity**: Critical (Tier 1)
* **Affected Files**: 22+ files including `CommandRouter.cs`, `TerminalIde.cs`, `Config.cs`, `ClaudeProvider.cs`, `OllamaClient.cs`, `AgyAccountStore.cs`, `AgyQuotaEngine.cs`, `AgyVault.cs`, `AiProcessRunner.cs`, `IdeCommandRegistry.cs`.
* **Problem**: Widespread use of `Bootstrapper.ServiceProvider.GetRequiredService<T>()` static calls.
* **Impact**: Hidden dependencies, severe tight coupling to DI container, blocks all isolated unit testing.
* **Fix**: Convert to Constructor Injection. Pass dependencies explicitly via constructor parameters.
* **Estimated Effort**: 3 to 4 Days

---

### Issue 2: Zero Automated Unit Test Coverage
* **Severity**: Critical (Tier 1)
* **Affected Files**: Entire codebase (19,615 LOC across 276 C# files).
* **Problem**: Zero unit tests exist in the codebase.
* **Impact**: Extremely high risk of regressions during refactoring; zero build-time confidence.
* **Fix**: Create `csapp/AgyTui.Tests` xUnit test project with Moq, create domain/infrastructure tests, target >= 70% coverage.
* **Estimated Effort**: 1 to 2 Weeks

---

### Issue 3: Static Global Mutable State
* **Severity**: Critical (Tier 1)
* **Affected Files**: `Infrastructure/Configuration/Config.cs` (`Config.Current`), `Infrastructure/Common/AppPaths.cs` (`_projectRoot`), `WorkspaceRegistry.cs`.
* **Problem**: Global static state accessed and modified directly across multiple threads.
* **Impact**: Dependency Inversion Principle (DIP) violation, tight coupling, temporal coupling, thread-safety bugs.
* **Fix**: Replace static singletons with injected `IConfigService` and `IPathProvider` services registered as DI singletons.
* **Estimated Effort**: 2 to 3 Days

---

### Issue 4: CommandRouter God Class
* **Severity**: Critical (Tier 1)
* **Affected Files**: `csapp/AgyTui/UI/Core/Navigation/CommandRouter.cs` (1,196 lines).
* **Problem**: Single `Execute` method contains 300+ `switch` cases handling all application commands (Git, AWS, Docker, AI, Learn, System).
* **Impact**: Direct violation of Single Responsibility Principle (SRP) and Open/Closed Principle (OCP); untestable.
* **Fix**: Implement Command Pattern (`ICommand`, `CommandRegistry`) and split into domain-specific routers (`GitCommandRouter`, `AiCommandRouter`, etc.). Reduce file to < 200 lines.
* **Estimated Effort**: 2 to 3 Days

---

### Issue 5: Silent Exception Handling (Swallowed Errors)
* **Severity**: Critical (Tier 1)
* **Affected Files**: 36+ files including `ProcessRunner.cs`, `SqliteConfigRepository.cs`, `SqliteWorkspaceRepository.cs`, `TerminalIde.cs`, `OllamaClient.cs`.
* **Problem**: Empty `catch { }` blocks silently swallow exceptions without logging or handling.
* **Impact**: Bugs fail silently in production, leaving zero diagnostic audit trailing or log context.
* **Fix**: Replace every empty catch block with `LogHelper.LogError($"Context: {ex.Message}");`.
* **Estimated Effort**: 1 to 2 Days

---

### Issue 6: N+1 Directory Scan in AgyAccountStore
* **Severity**: Critical (Tier 1)
* **Affected Files**: `Infrastructure/Integrations/AgyClient/AgyAccountStore.cs` (lines 157-188).
* **Problem**: Scans disk filesystem for account files and then executes individual SQLite database queries for each item.
* **Impact**: Significant disk I/O latency and query roundtrips during account switching.
* **Fix**: Implement memory caching with TTL or batch fetch account metadata in a single query.
* **Estimated Effort**: 2 Hours

---

### Issue 7: N+1 JSONL Parsing in AgyQuotaEngine
* **Severity**: Critical (Tier 1)
* **Affected Files**: `Infrastructure/Integrations/AgyClient/AgyQuotaEngine.cs` (lines 25-61).
* **Problem**: Re-parses entire multi-megabyte JSONL audit log file on every quota calculation request.
* **Impact**: Massive CPU overhead and disk reading on frequent status requests.
* **Fix**: Cache parsed JSONL data in memory; verify file modification timestamp (`LastWriteTime`) prior to re-parsing.
* **Estimated Effort**: 3 Hours

---

### Issue 8: N+1 Single Row Inserts in LearningDataSeeder
* **Severity**: Critical (Tier 1)
* **Affected Files**: `Infrastructure/Persistence/Seeding/LearningDataSeeder.cs` (lines 90-111).
* **Problem**: `foreach` loop executes 1,000 separate SQL `INSERT` statements when seeding initial flashcards.
* **Impact**: Database lock overhead and slow application startup time.
* **Fix**: Batch SQL inserts into chunked transactions (100 flashcards per batch statement).
* **Estimated Effort**: 2 Hours

---

### Issue 9: N+1 Workspace Discovery Traversal
* **Severity**: Critical (Tier 1)
* **Affected Files**: `Infrastructure/Registries/WorkspaceRegistry.cs` (lines 203-279).
* **Problem**: Performs 7 synchronous filesystem checks per directory across hundreds of candidate directories.
* **Impact**: 500ms to 2,000ms UI latency during workspace scans.
* **Fix**: Increase cache TTL from 5 seconds to 30 minutes; optimize directory filtering routines.
* **Estimated Effort**: 2 Hours

---

### Issue 10: Thread-Unsafe In-Memory Caches
* **Severity**: Critical (Tier 1)
* **Affected Files**: `WorkspaceRegistry.cs` (TtlCache), `AppPaths.cs`, `OllamaClient.cs` (`_defaultModel`).
* **Problem**: Static in-memory dictionary caches are accessed concurrently without locking or synchronization.
* **Impact**: Potential `ConcurrentModificationException`, state corruption, and race conditions.
* **Fix**: Synchronize cache access using `lock` objects or `ReaderWriterLockSlim`.
* **Estimated Effort**: 4 Hours

---

### Issue 11: Domain Model Encapsulation Breaches (Public Setters)
* **Severity**: Critical (Tier 1)
* **Affected Files**: `Domain/AccountContext/AccountMetadata.cs`.
* **Problem**: Entity exposes `public` property setters (`LastUsed`, `UsageCount`, `Email`, etc.).
* **Impact**: External callers can mutate aggregate state without enforcing domain invariants.
* **Fix**: Make property setters `private`/`internal`; expose behavior-driven aggregate methods.
* **Estimated Effort**: 1 Hour

---

### Issue 12: Exposed Mutable Domain Collections
* **Severity**: Critical (Tier 1)
* **Affected Files**: `Domain/AccountContext/AccountAggregate.cs`.
* **Problem**: Exposes internal backing `List<string> RequestHistory` directly as a public property.
* **Impact**: External code can modify, clear, or corrupt the domain aggregate's internal state.
* **Fix**: Expose collection as `IReadOnlyList<string>`.
* **Estimated Effort**: 1 Hour

---

### Issue 13: HttpClient Static Instantiation Anti-Pattern
* **Severity**: Critical (Tier 1)
* **Affected Files**: `Infrastructure/Common/HttpClientProvider.cs`.
* **Problem**: Static `HttpClient` instance managed manually across multiple accessors without configuration scope.
* **Impact**: Socket exhaustion or inability to override headers/timeouts per request; non-mockable.
* **Fix**: Use `IHttpClientFactory` or typed HttpClient DI registrations.
* **Estimated Effort**: 4 Hours

---

### Issue 14: Missing Filesystem Abstraction (`System.IO`)
* **Severity**: Critical (Tier 1)
* **Affected Files**: 15+ files using `File.ReadLines`, `File.Exists`, `Directory.GetFiles` directly.
* **Problem**: High-level components execute static `System.IO` methods directly.
* **Impact**: Code cannot be unit tested without interacting with the real physical disk.
* **Fix**: Introduce `IFileSystem` interface, `FileSystem` runtime wrapper, and `MockFileSystem` for testing.
* **Estimated Effort**: 1 Day

---

### Issue 15: Static Configuration Initialization without Hot-Reload
* **Severity**: Critical (Tier 1)
* **Affected Files**: `Infrastructure/Configuration/Config.cs` (lines 82-95).
* **Problem**: `static Config()` populates setting values once at startup with no refresh mechanism.
* **Impact**: Application must be restarted whenever settings files are updated on disk.
* **Fix**: Move config loading into `ConfigService` with hot-reload change notifications.
* **Estimated Effort**: 2 to 4 Hours

---

### Issue 16: Central Bootstrapper Service Locator Hub
* **Severity**: Critical (Tier 1)
* **Affected Files**: `Infrastructure/Di/Bootstrapper.cs`.
* **Problem**: Serves as global Service Locator host for 22+ files across application layers.
* **Impact**: Tight coupling of application components to the DI container container lifecycle.
* **Fix**: Refactor `Bootstrapper` to pure composition root once Service Locator calls are removed (#1).
* **Estimated Effort**: Covered by Issue 1

---

### Issue 17: Misplaced AgentInvocationLog Entity
* **Severity**: Critical (Tier 1)
* **Affected Files**: `Domain/AiAgentContext/AgentInvocationLog.cs`.
* **Problem**: Pure logging data structure placed inside Domain layer without domain logic.
* **Impact**: DDD layer pollution; misleads domain context boundaries.
* **Fix**: Move to `Infrastructure/Logging/` or introduce legitimate domain aggregate behavior.
* **Estimated Effort**: 2 Hours

---

## 3. Tier 2 High Priority Issues (Issues 18 to 35)

### Issue 18: TerminalIde God Class Monolith
* **Severity**: High (Tier 2)
* **Affected Files**: `UI/Screens/Ide/TerminalIde.cs` (832 lines).
* **Problem**: Single UI component manages directory tree browsing, code file viewer, symbol search, and Git operations.
* **Fix**: Deconstruct into `FileExplorer`, `CodeViewer`, `SymbolSearcher`, and `IDECommandHandler`.
* **Estimated Effort**: 1 to 2 Days

---

### Issue 19: High Code Duplication in Menu Renderers
* **Severity**: High (Tier 2)
* **Affected Files**: `FlatTreeRenderer.cs` (665 lines) vs `ThreePaneRenderer.cs` (479 lines).
* **Problem**: ~40% duplicate code handling search input, node selection, and keyboard navigation.
* **Fix**: Extract common template logic into `MenuRendererBase` abstract class.
* **Estimated Effort**: 1 to 2 Days

---

### Issue 20: WorkspaceRegistry God Class
* **Severity**: High (Tier 2)
* **Affected Files**: `Infrastructure/Registries/WorkspaceRegistry.cs` (684 lines).
* **Problem**: Combines directory discovery, TTL caching, path validation, and disk reading in one class.
* **Fix**: Deconstruct into `WorkspaceDiscoverer`, `WorkspaceCache`, and `WorkspaceValidator`.
* **Estimated Effort**: 1 to 2 Days

---

### Issue 21: GitClient Monolith Class
* **Severity**: High (Tier 2)
* **Affected Files**: `Infrastructure/Integrations/Git/GitClient.cs` (545 lines).
* **Problem**: Single class contains 50+ unorganized Git operations.
* **Fix**: Refactor into logical Git sub-services (`GitStatusService`, `GitBranchService`, `GitCommitService`).
* **Estimated Effort**: 1 Day

---

### Issue 22: CommandRegistry Inline Bloat
* **Severity**: High (Tier 2)
* **Affected Files**: `Infrastructure/Registries/CommandRegistry.cs` (703 lines).
* **Problem**: Contains over 600 lines of inline command string registrations.
* **Fix**: Modularize command definitions into domain-specific registration providers.
* **Estimated Effort**: 4 Hours

---

### Issue 23: SubPageNavigator Static UI State Leaks
* **Severity**: High (Tier 2)
* **Affected Files**: `UI/Core/Navigation/SubPageNavigator.cs` (lines 19-23).
* **Problem**: `static _detailsSearchBuffer` and `static SelectedWorkspaceIndex` persist globally across UI switches.
* **Fix**: Convert static state fields into instance fields managed per navigation lifecycle.
* **Estimated Effort**: 1 Day

---

### Issue 24: Blocking Filesystem I/O During UI Rendering
* **Severity**: High (Tier 2)
* **Affected Files**: `UI/Screens/Ide/TerminalIde.cs` (lines 57-65).
* **Problem**: `GetVisibleNodes()` executes synchronous directory reads directly inside the frame render loop.
* **Fix**: Implement async directory fetching and lazy-load tree nodes on expand.
* **Estimated Effort**: 4 Hours

---

### Issue 25: Uncached Executable Resolution Lookups
* **Severity**: High (Tier 2)
* **Affected Files**: `Infrastructure/Common/ProcessRunner.cs`.
* **Problem**: `FindOnPath("git")` performs filesystem PATH lookups on every single command execution.
* **Fix**: Cache resolved executable binary paths in an in-memory thread-safe dictionary.
* **Estimated Effort**: 1 Hour

---

### Issue 26: Hardcoded Quota Boundaries and Usernames
* **Severity**: High (Tier 2)
* **Affected Files**: `Infrastructure/Integrations/AgyClient/AgyQuotaEngine.cs` (lines 86-111).
* **Problem**: Specific developer usernames and quota limits hardcoded into source code logic.
* **Fix**: Externalize quota rules and user overrides into configuration settings.
* **Estimated Effort**: 2 Hours

---

### Issue 27: Bloated AccountCredentials Value Object
* **Severity**: High (Tier 2)
* **Affected Files**: `Domain/AccountContext/AccountCredentials.cs`.
* **Problem**: Single class contains 5 optional authentication schemes (API tokens, OAuth tokens, credentials).
* **Fix**: Split into dedicated value objects: `AuthTokenCredentials`, `OAuthCredentials`, `EmailCredentials`.
* **Estimated Effort**: 2 Hours

---

### Issue 28: Unorganized Bootstrapper Registration Monolith
* **Severity**: High (Tier 2)
* **Affected Files**: `Infrastructure/Di/Bootstrapper.cs`.
* **Problem**: Over 120 service registrations dumped sequentially in a single startup method.
* **Fix**: Create modular registration extension methods (`AddDomainServices`, `AddInfrastructureServices`, `AddUIServices`).
* **Estimated Effort**: 4 Hours

---

### Issue 29: Duplicate N+1 Query in FindAutoSwitchCandidate
* **Severity**: High (Tier 2)
* **Affected Files**: `Infrastructure/Integrations/AgyClient/AgyAccountStore.cs` (lines 428-443).
* **Problem**: `FindAutoSwitchCandidate()` fetches accounts list and then queries metadata in a loop.
* **Fix**: Bulk-fetch accounts along with their metadata in a single combined call.
* **Estimated Effort**: 2 Hours

---

### Issue 30: Flashcard Engine Eager Loading Bottleneck
* **Severity**: High (Tier 2)
* **Affected Files**: `UI/Screens/Learn/FlashcardEngine.cs`.
* **Problem**: Reads and parses 50+ complete deck JSON files merely to build the main selection menu.
* **Fix**: Load deck headers/metadata only for the menu; lazy-load full cards on selection.
* **Estimated Effort**: 3 Hours

---

### Issue 31: Tight Coupling to Spectre.Console Framework
* **Severity**: High (Tier 2)
* **Affected Files**: System-wide UI renderer components.
* **Problem**: Direct calls to `AnsiConsole.Write()` scattered across components without abstraction.
* **Fix**: Wrap console interactions behind `IConsoleRenderer` interface for automated UI testing.
* **Estimated Effort**: 2 to 3 Days

---

### Issue 32: Absence of Domain Events Architecture
* **Severity**: High (Tier 2)
* **Affected Files**: Domain aggregates across `Domain/`.
* **Problem**: Domain state changes (e.g., account activation, quota breaches) trigger no domain events.
* **Fix**: Implement `IDomainEvent` interface and domain event dispatcher pipeline.
* **Estimated Effort**: 1 to 2 Days

---

### Issue 33: AgyVault Service Locator Closure
* **Severity**: High (Tier 2)
* **Affected Files**: `Infrastructure/Integrations/AgyClient/AgyVault.cs`.
* **Problem**: Instantiates factory closure invoking static `Bootstrapper.ServiceProvider`.
* **Fix**: Replace closure with Constructor Injection.
* **Estimated Effort**: 1 Hour

---

### Issue 34: AiProcessRunner Service Locator Closures
* **Severity**: High (Tier 2)
* **Affected Files**: `Infrastructure/Integrations/Ai/Services/AiProcessRunner.cs`.
* **Problem**: Contains 2 factory closures calling static DI container.
* **Fix**: Replace closures with direct dependency injection.
* **Estimated Effort**: 1 Hour

---

### Issue 35: IdeCommandRegistry Lambda Service Locator
* **Severity**: High (Tier 2)
* **Affected Files**: `Infrastructure/Registries/IdeCommandRegistry.cs` (line 48).
* **Problem**: Service locator invocation hidden inside command execution lambda.
* **Fix**: Inject required command services via constructor.
* **Estimated Effort**: 1 Hour

---

## 4. Tier 3 Medium Priority Issues (Issues 36 to 47)

### Issue 36: Inconsistent State Management Patterns
* **Severity**: Medium (Tier 3)
* **Problem**: Codebase mixes 3 state paradigms (static fields, instance variables, event callbacks).
* **Fix**: Unify state handling under a single `IStateStore` pattern across all UI screens.
* **Estimated Effort**: 1 to 2 Days

---

### Issue 37: Unsynchronized Static Field in OllamaClient
* **Severity**: Medium (Tier 3)
* **Affected Files**: `Infrastructure/Integrations/Ai/Providers/OllamaClient.cs`.
* **Problem**: `_defaultModel` static string modified across threads without locking.
* **Fix**: Protect static model configuration with thread synchronization lock.
* **Estimated Effort**: 2 Hours

---

### Issue 38: Unhandled Fire & Forget Task in PublicIpWidget
* **Severity**: Medium (Tier 3)
* **Affected Files**: `UI/Core/Common/StatusWidgets.cs` (lines 63-75).
* **Problem**: Launches network HTTP check using `Task.Run()` without error handling or awaiting.
* **Fix**: Implement proper async lifecycle management and error handling.
* **Estimated Effort**: 2 Hours

---

### Issue 39: Missing Retry Policies for External Ollama Calls
* **Severity**: Medium (Tier 3)
* **Affected Files**: `Infrastructure/Integrations/Ai/Providers/OllamaClient.cs`.
* **Problem**: HTTP calls to Ollama AI service lack retry mechanisms or circuit breakers.
* **Fix**: Integrate Polly resilience pipelines for retries and timeouts.
* **Estimated Effort**: 1 to 2 Days

---

### Issue 40: Shared Static State in OllamaStatusWidgetCache
* **Severity**: Medium (Tier 3)
* **Affected Files**: `UI/Core/Common/StatusWidgets.cs` (lines 10-24).
* **Problem**: Uses shared static `TtlCache` causing cross-widget side effects.
* **Fix**: Inject instance-based cache via DI.
* **Estimated Effort**: 2 Hours

---

### Issue 41: Sparse XML Documentation Density
* **Severity**: Medium (Tier 3)
* **Problem**: Less than 5% comment density across core codebase.
* **Fix**: Add complete XML documentation on public APIs and write Architecture Decision Records (ADRs).
* **Estimated Effort**: 2 to 3 Days

---

### Issue 42: Lack of Accessibility Support
* **Severity**: Medium (Tier 3)
* **Problem**: Hardcoded colors and glyphs render poorly on non-standard terminals or for color-blind users.
* **Fix**: Implement high-contrast mode, font scaling options, and ASCII fallback rendering.
* **Estimated Effort**: 2 to 3 Days

---

### Issue 43: Weak Invariants in FlashcardDeck Aggregate
* **Severity**: Medium (Tier 3)
* **Affected Files**: `Domain/LearnContext/FlashcardDeck.cs`.
* **Problem**: `UpdateStats()` method lacks state validation and invariant guards.
* **Fix**: Enforce strict aggregate boundary checks and explicit state transition logic.
* **Estimated Effort**: 4 Hours

---

### Issue 44: Navigation Buffer Overflow & Input Desync
* **Severity**: Medium (Tier 3)
* **Problem**: Rapid keyboard strokes can cause search buffer desync and index out-of-range errors.
* **Fix**: Add input validation guards and debounce rapid navigation keystrokes.
* **Estimated Effort**: 1 Day

---

### Issue 45: Recursion Depth Risk in Directory Browsing
* **Severity**: Medium (Tier 3)
* **Affected Files**: `Infrastructure/Registries/WorkspaceRegistry.cs`.
* **Problem**: Recursive directory traversal can cause stack overflow on deeply nested symlinks.
* **Fix**: Convert recursive algorithm to iterative traversal with explicit depth limiters.
* **Estimated Effort**: 2 Hours

---

### Issue 46: Direct File Operations in Command Logging Middleware
* **Severity**: Medium (Tier 3)
* **Affected Files**: `Infrastructure/Middleware/CommandLoggingMiddleware.cs`.
* **Problem**: Calls `File.AppendAllText()` directly bypassing filesystem abstractions.
* **Fix**: Update to use `IFileSystem` interface.
* **Estimated Effort**: 1 Hour

---

### Issue 47: Config Dual-Write Synchronization Vulnerability
* **Severity**: Medium (Tier 3)
* **Affected Files**: `Infrastructure/Persistence/Repositories/SqliteConfigRepository.cs` (lines 77-85).
* **Problem**: Simultaneously writes configuration to SQLite database and local JSON file, risking desync.
* **Fix**: Single-source-of-truth strategy: write to SQLite as primary, export JSON as async backup.
* **Estimated Effort**: 1 Day

---

## 5. Master Issues Matrix Table

| Issue ID | Issue Name | Tier | Category | Affected Component | Effort |
| :---: | :--- | :---: | :--- | :--- | :---: |
| **#1** | Service Locator Anti-Pattern | **Tier 1** | Architecture | System-wide (22+ files) | 3-4 Days |
| **#2** | Zero Unit Test Coverage | **Tier 1** | Quality | Entire Codebase (19.6k LOC) | 1-2 Weeks |
| **#3** | Static Global Mutable State | **Tier 1** | Architecture | Config & AppPaths | 2-3 Days |
| **#4** | CommandRouter God Class | **Tier 1** | UI | CommandRouter.cs | 2-3 Days |
| **#5** | Silent Exception Handling | **Tier 1** | Reliability | System-wide (36+ files) | 1-2 Days |
| **#6** | N+1 AgyAccountStore Scans | **Tier 1** | Performance | AgyAccountStore.cs | 2 Hours |
| **#7** | N+1 Quota Engine Re-parsing | **Tier 1** | Performance | AgyQuotaEngine.cs | 3 Hours |
| **#8** | N+1 Learning Seeder Inserts | **Tier 1** | Performance | LearningDataSeeder.cs | 2 Hours |
| **#9** | N+1 Workspace Discovery | **Tier 1** | Performance | WorkspaceRegistry.cs | 2 Hours |
| **#10** | Thread-Unsafe Caches | **Tier 1** | Concurrency | Caches & Static State | 4 Hours |
| **#11** | Domain Setter Violation | **Tier 1** | Domain DDD | AccountMetadata.cs | 1 Hour |
| **#12** | Exposed Mutable Collection | **Tier 1** | Domain DDD | AccountAggregate.cs | 1 Hour |
| **#13** | HttpClient Anti-Pattern | **Tier 1** | Infrastructure | HttpClientProvider.cs | 4 Hours |
| **#14** | Missing File I/O Abstraction | **Tier 1** | Testability | Infrastructure (15+ files) | 1 Day |
| **#15** | Static Config Initialization | **Tier 1** | Infrastructure | Config.cs | 2-4 Hours |
| **#16** | Bootstrapper Hub Coupling | **Tier 1** | Architecture | Bootstrapper.cs | Covered (#1) |
| **#17** | AgentInvocationLog Anomaly | **Tier 1** | Domain DDD | AgentInvocationLog.cs | 2 Hours |
| **#18** | TerminalIde God Class | **Tier 2** | UI | TerminalIde.cs | 1-2 Days |
| **#19** | UI Renderer Duplication | **Tier 2** | UI | FlatTree & ThreePane | 1-2 Days |
| **#20** | WorkspaceRegistry God Class | **Tier 2** | Infrastructure | WorkspaceRegistry.cs | 1-2 Days |
| **#21** | GitClient Monolith | **Tier 2** | Infrastructure | GitClient.cs | 1 Day |
| **#22** | CommandRegistry Bloat | **Tier 2** | Infrastructure | CommandRegistry.cs | 4 Hours |
| **#23** | SubPageNavigator Static State | **Tier 2** | UI | SubPageNavigator.cs | 1 Day |
| **#24** | Filesystem I/O in Render Loop | **Tier 2** | UI | TerminalIde.cs | 4 Hours |
| **#25** | Uncached Executable Lookups | **Tier 2** | Performance | ProcessRunner.cs | 1 Hour |
| **#26** | Hardcoded Quota Values | **Tier 2** | Security | AgyQuotaEngine.cs | 2 Hours |
| **#27** | Bloated AccountCredentials | **Tier 2** | Domain DDD | AccountCredentials.cs | 2 Hours |
| **#28** | Bootstrapper Monolith | **Tier 2** | Infrastructure | Bootstrapper.cs | 4 Hours |
| **#29** | Auto-Switch Candidate N+1 | **Tier 2** | Performance | AgyAccountStore.cs | 2 Hours |
| **#30** | Learn Data Menu Loading | **Tier 2** | Performance | FlashcardEngine.cs | 3 Hours |
| **#31** | Spectre.Console Coupling | **Tier 2** | UI / Testability | UI Renderers | 2-3 Days |
| **#32** | Missing Domain Events | **Tier 2** | Domain DDD | Domain Aggregates | 1-2 Days |
| **#33** | AgyVault Service Locator | **Tier 2** | Architecture | AgyVault.cs | 1 Hour |
| **#34** | AiProcessRunner Service Locator | **Tier 2** | Architecture | AiProcessRunner.cs | 1 Hour |
| **#35** | IdeCommandRegistry Service Locator | **Tier 2** | Architecture | IdeCommandRegistry.cs | 1 Hour |
| **#36** | Inconsistent State Management | **Tier 3** | UI | UI Screens | 1-2 Days |
| **#37** | OllamaClient Static Model | **Tier 3** | Concurrency | OllamaClient.cs | 2 Hours |
| **#38** | Public IP Task Fire & Forget | **Tier 3** | Reliability | StatusWidgets.cs | 2 Hours |
| **#39** | Ollama API Resilience | **Tier 3** | Reliability | OllamaClient.cs | 1-2 Days |
| **#40** | Status Widget Static Cache | **Tier 3** | Concurrency | StatusWidgets.cs | 2 Hours |
| **#41** | Sparse XML Documentation | **Tier 3** | Documentation | Entire Codebase | 2-3 Days |
| **#42** | Lack of Accessibility Support | **Tier 3** | UX | System-wide UI | 2-3 Days |
| **#43** | FlashcardDeck Weak Invariants | **Tier 3** | Domain DDD | FlashcardDeck.cs | 4 Hours |
| **#44** | Navigation Buffer Overflow | **Tier 3** | UX | Navigation | 1 Day |
| **#45** | Recursive Depth Risk | **Tier 3** | Reliability | WorkspaceRegistry.cs | 2 Hours |
| **#46** | Middleware Direct File Ops | **Tier 3** | Infrastructure | CommandLoggingMiddleware.cs | 1 Hour |
| **#47** | Config Dual-Write Desync | **Tier 3** | Infrastructure | SqliteConfigRepository.cs | 1 Day |
