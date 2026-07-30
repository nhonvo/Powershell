# Master Generalized Autonomous Execution Prompt for PowerShell Control Center (`AgyTui`)

> **Instruction**: Copy and send this prompt to your AI coding agent (or execute it sequentially) to autonomously implement, test, and commit all 8 architectural steps in `master_architectural_plan.md` in a continuous general loop without breaking context.

---

```markdown
You are an expert lead software engineer executing the Master Architectural Refactoring Plan for the PowerShell Control Center codebase (`AgyTui`).

### 🎯 Goal
Autonomously loop through and implement Phase 1 through Phase 8 of `master_architectural_plan.md`. After EACH phase:
1. Run `dotnet test csapp/AgyTui.Tests/AgyTui.Tests.csproj -c Debug` and assert a 100% PASS rate (zero test failures).
2. Commit code changes to Git with a conventional commit message.
3. Automatically loop to the next phase without breaking turn or waiting for user intervention unless a blocking error occurs.

---

### 🔄 General Execution Loop Sequence

#### 🛠️ Phase 1: Solution DI Audit, Prohibition of `new ServiceObject()`, & `ServiceTestFixture`
1. Audit all C# classes across `csapp/AgyTui`. Eliminate direct instantiations of service objects using `new ServiceObject()`.
2. Refactor fallback constructors and static call sites (`AiProcessRunner.RunInteractiveStatic`, `AgySecretVault`, `TokenVault`, `StatusWidgets`, `ScreenChrome`, `SubPageNavigator`, `AiDashboardView`, `LearnDataPaths`) to use constructor DI or top-level `Func<T>` factory delegates.
3. Create `ServiceTestFixture.cs` in `csapp/AgyTui.Tests/Fixtures/` providing isolated `IServiceProvider` mocking (`InMemoryAgyAccountRepository`, `FakeSqliteDatabase`).
4. Run `dotnet test csapp/AgyTui.Tests/AgyTui.Tests.csproj -c Debug`. Ensure 100% pass rate.
5. Commit: `refactor(di): audit solution DI, prohibit new ServiceObject() and add ServiceTestFixture`

---

#### 🚀 Phase 2: Path Resolution Caching (`IAppPathManager`)
1. Create `IAppPathManager.cs` interface and `AppPathManager.cs` implementation in `csapp/AgyTui/Core/Services/`.
2. Implement thread-safe path caching (`ConcurrentDictionary`) for `GeminiHome`, `AccountPrefix`, and `GetAccountDirectory(accountName)`. Include `InvalidateCache()` method.
3. Register `services.AddSingleton<IAppPathManager, AppPathManager>();` in `Bootstrapper.cs`.
4. Update `AgyAccountStore.cs` to consume `IAppPathManager`.
5. Run `dotnet test csapp/AgyTui.Tests/AgyTui.Tests.csproj -c Debug`. Ensure all tests pass.
6. Commit: `feat(core): implement IAppPathManager thread-safe path resolution caching`

---

#### 🏗️ Phase 3: Domain-Driven Design (DDD) Bounded Context Restructuring
1. Restructure domain entities into `AgyTui.Domain` bounded contexts (`AccountContext`, `WorkspaceContext`, `AiAgentContext`, `LearnContext`).
2. Implement `AccountAggregate`, `QuotaMetrics` (Value Object), `WorkspaceAggregate`, and `AgentInvocationLog`.
3. Run `dotnet test csapp/AgyTui.Tests/AgyTui.Tests.csproj -c Debug`. Ensure all tests pass.
4. Commit: `refactor(ddd): restructure domain layer into bounded contexts and value objects`

---

#### 🗄️ Phase 4: SQLite Migration Engine (`SqliteMigrationEngine`)
1. Create `SqliteMigrationEngine.cs` in `csapp/AgyTui/Infrastructure/Persistence/`.
2. Add embedded SQL DDL scripts (`V1__InitialSchema.sql`, `V2__AddCommandInvocationLogs.sql`).
3. Support environment isolation: use `agytui.dev.db` when `ENVIRONMENT=Development` and `agytui.db` when `ENVIRONMENT=Production`.
4. Register `SqliteMigrationEngine` in `Bootstrapper.cs` and trigger `ApplyMigrations()` during application startup.
5. Create `SqlitePersistenceTests.cs` in `csapp/AgyTui.Tests/Integration/`.
6. Run `dotnet test csapp/AgyTui.Tests/AgyTui.Tests.csproj -c Debug`. Ensure all tests pass.
7. Commit: `feat(persistence): implement SqliteMigrationEngine for versioned DDL schema migrations`

---

#### 🛠️ Phase 5: PowerShell Profile Bridge & Parity Tests (`ProfileAliasParityTests`)
1. Extract wrapper functions from `Microsoft.PowerShell_profile.ps1` (`Show-GitStatus`, `Show-GitDiff`, `Invoke-ConventionalCommit`, `Reset-AgyAccountData`, `Show-DockerHealth`) into compiled C# clients (`GitClient`, `DotNetClient`, `DockerClient`).
2. Update `Microsoft.PowerShell_profile.ps1` to delegate function calls directly to `AgyTui <alias>`.
3. Create `ProfileAliasParityTests.cs` in `csapp/AgyTui.Tests/Parity/` asserting that every alias in `CommandRegistry.cs` has a registered handler in `CommandRouter.cs`.
4. Run `dotnet test csapp/AgyTui.Tests/AgyTui.Tests.csproj -c Debug`. Ensure 100% pass rate.
5. Commit: `refactor(profile): delegate PS1 aliases to AgyTui C# engine and add ProfileAliasParityTests`

---

#### 🖥️ Phase 6: UI Command Handlers & Smooth Rendering Engine
1. Implement `UiStateStore.cs` for reactive, immutable UI state management.
2. Implement `IUiCommandDispatcher` and WebAPI-style `ICommandHandler<TCommand>` handlers.
3. Refactor `ThreePaneRenderer` and `FlatTreeRenderer` into pure render functions evaluating `(UiState) => IRenderable` via Spectre `LiveDisplay` diffing without screen flicker.
4. Run `dotnet test csapp/AgyTui.Tests/AgyTui.Tests.csproj -c Debug`. Ensure all tests pass.
5. Commit: `feat(ui): implement smooth flicker-free rendering engine and UiCommandDispatcher`

---

#### 📦 Phase 7: Dev vs Release Setup & Onboarding Script
1. Create `script/Install-AgyEnvironment.ps1` setup script supporting fresh computer onboarding:
   - Audit and install .NET 9 SDK via `winget` if missing.
   - Provision `~/.gemini/` directories (`logs/`, `history/`, `data/`).
   - Link `$PROFILE` to `Microsoft.PowerShell_profile.ps1`.
   - Restore and compile `AgyTui.csproj`.
   - Execute initial SQLite schema migrations and OAuth login.
2. Create `build-release.ps1` for local single-file Native AOT publish (`PublishSingleFile=true`).
3. Create `.github/workflows/release.yml` GitHub Actions CI/CD workflow.
4. Run `dotnet test csapp/AgyTui.Tests/AgyTui.Tests.csproj -c Debug`.
5. Commit: `feat(ci): create Install-AgyEnvironment.ps1 onboarding script and release workflow`

---

#### 📚 Phase 8: Multi-Layered Testing & Knowledge Base Sync
1. Finalize integration test suite and memory diagnoser benchmarks (`PathResolutionBenchmarkTests`).
2. Create `docs/guides/testing_and_ci.md` developer guide.
3. Verify Obsidian dataset sync engine via `refresh` command.
4. Run `dotnet test csapp/AgyTui.Tests/AgyTui.Tests.csproj -c Debug`.
5. Commit: `docs(testing): complete multi-layered test suite and knowledge base documentation`

---

### 🛡️ Safety & Quality Rules
- **No Diagnostics Assumptions**: Run `dotnet test` after every step and inspect output directly.
- **No Swallowing Exceptions**: Maintain strict error propagation.
- **Keep Tests Passing**: Never proceed to the next phase if a unit test is failing.
- **Concise Reporting**: After each phase commit, output a concise status summary highlighting passed test counts and git commit hashes.
```
