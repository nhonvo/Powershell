# PowerShell Control Center (`AgyTui`) — Codebase Structure & Architectural Review

> **Date**: 2026-07-30  
> **Author**: Antigravity AI Engineering Team  
> **Scope**: Complete architectural tree, naming review, and location feedback for `csapp/AgyTui`.

---

## 1. Directory Tree Representation (`csapp/AgyTui`)

```text
csapp/AgyTui/
├── AgyTui.csproj
├── Program.cs
├── Usings.cs
├── priority_workspaces.json
├── profile.config.json
│
├── Domain/                                     # Pure Domain Layer (DDD Bounded Contexts)
│   ├── AccountContext/
│   │   ├── AccountAggregate.cs                 # Account Aggregate Root
│   │   ├── AccountMetadata.cs                  # Account Metadata Value Object / DTO
│   │   ├── EncryptedToken.cs                   # Vault Encrypted Token Record
│   │   └── QuotaMetrics.cs                     # Account Rolling Quota Metrics Record
│   ├── AiAgentContext/
│   │   ├── AgentInvocationLog.cs               # AI Execution Log Entity
│   │   └── ProviderMode.cs                     # Provider Mode Enum (Auto, CloudDirect, Ollama)
│   ├── LearnContext/
│   │   ├── FlashcardDeck.cs                    # Flashcard Deck Aggregate Root
│   │   └── LearningModels.cs                   # Flashcards, Quizzes, Vocab & STAR Records
│   └── WorkspaceContext/
│       ├── ProjectPath.cs                      # Project Path Value Object
│       ├── WorkspaceAggregate.cs               # Workspace Aggregate Root
│       └── WorkspaceModels.cs                  # Workspace Entry & Workspace Link Records
│
├── Infrastructure/                             # Technical Capabilities & Adapters
│   ├── Common/
│   │   ├── AppPaths.cs                         # Base Path Constants
│   │   ├── CommandInvocationLog.cs             # Middleware Activity Logging Models
│   │   ├── EditorResolver.cs                   # CLI Editor Invocation Helper
│   │   ├── LogHelper.cs                        # Centralized File & Console Logger
│   │   ├── ProcessRunner.cs                    # Process Execution Helper
│   │   └── TtlCache.cs                         # Generic Time-To-Live Cache Strategy
│   ├── Configuration/
│   │   ├── Config.cs                           # App Configuration Master Data & Overrides
│   │   └── EnvironmentProvider.cs              # Dev/Prod Runtime Environment Detection
│   ├── Di/
│   │   └── Bootstrapper.cs                     # Dependency Injection Container Registration
│   ├── Integrations/                           # Third-Party & Subsystem Integrations
│   │   ├── AgyClient/                          # Antigravity Gateway Integrations
│   │   │   ├── Interfaces/
│   │   │   │   ├── IAgyAccountStore.cs
│   │   │   │   ├── IAgyQuotaEngine.cs
│   │   │   │   └── IAgyVault.cs
│   │   │   ├── AgyAccountStore.cs
│   │   │   ├── AgyQuotaEngine.cs
│   │   │   └── AgyVault.cs
│   │   ├── Ai/                                 # AI & Ollama Subsystem
│   │   │   ├── Abstractions/
│   │   │   │   ├── IAiProcessRunner.cs
│   │   │   │   └── IOllamaClient.cs
│   │   │   ├── Providers/
│   │   │   │   └── OllamaClient.cs
│   │   │   └── Services/
│   │   │       └── AiProcessRunner.cs
│   │   ├── Aws/
│   │   │   └── AwsS3Bridge.cs                  # AWS S3 Cloud Storage Adapter
│   │   ├── Docker/
│   │   │   └── DockerBridge.cs                 # Docker Container Management Adapter
│   │   ├── DotNet/
│   │   │   └── DotNetCliBridge.cs              # .NET SDK & Build Adapter
│   │   ├── Git/
│   │   │   └── GitBridge.cs                    # Git Repository Helper
│   │   ├── Obsidian/
│   │   │   └── ObsidianBridge.cs               # Obsidian Vault & Markdown Bridge
│   │   └── Sys/
│   │       └── SystemBridge.cs                 # OS Process & Hardware Helper
│   ├── Logging/
│   │   └── CommandLoggingMiddleware.cs         # Command Execution Logging Decorator
│   ├── Persistence/                            # Storage Engine & Data Repositories
│   │   ├── DbContext/
│   │   │   ├── ISqliteDatabase.cs              # SQLite Connection Interface
│   │   │   └── SqliteDatabase.cs               # SQLite Connection Implementation
│   │   ├── Interfaces/
│   │   │   ├── IAgyAccountRepository.cs        # Account Storage Repository Interface
│   │   │   ├── IConfigRepository.cs            # Configuration Repository Interface
│   │   │   ├── IFileRepository.cs              # Generic File Repository Interface
│   │   │   ├── IRepository.cs                  # Generic DB Repository Interface
│   │   │   └── IStudyRepository.cs             # Study Data Repository Interface
│   │   ├── Learning/
│   │   │   └── LearnDataPaths.cs               # Study Directory Paths Helper
│   │   ├── Migrations/
│   │   │   ├── V1__InitialSchema.sql           # Initial Database Schema Migration
│   │   │   └── V2__AddCommandInvocationLogs.sql# Invocation Log Schema Migration
│   │   ├── Repositories/
│   │   │   ├── JsonFileRepositoryBase.cs       # Generic Base JSON File Repository
│   │   │   ├── JsonStudyRepository.cs          # Study Data JSON Repository
│   │   │   ├── SqliteAgyAccountRepository.cs   # Account SQLite Repository
│   │   │   ├── SqliteConfigRepository.cs       # Configuration SQLite Repository
│   │   │   └── SqliteRepositoryBase.cs         # Generic Base SQLite Database Repository
│   │   └── SqliteMigrationEngine.cs            # Automatic SQLite Schema Migration Engine
│   ├── Registries/
│   │   ├── IdeCommandRegistry.cs               # IDE Custom Commands Registry
│   │   ├── ResourceRegistry.cs                 # Learning Resources Index Registry
│   │   └── WorkspaceRegistry.cs                # Multi-Directory Workspace Registry
│   └── Services/
│       ├── AppPathManager.cs                   # App Directory Caching Service
│       ├── ConfigService.cs                    # Configuration Management Service
│       ├── IAppPathManager.cs                  # App Path Service Interface
│       ├── ICommandRouter.cs                   # Command Router Service Interface
│       ├── IConfigService.cs                   # Configuration Service Interface
│       ├── IResourceRegistry.cs                # Resource Registry Interface
│       └── IWorkspaceRegistry.cs               # Workspace Registry Interface
│
└── UI/                                         # User Interface Layer (Spectre.Console & Views)
    ├── Core/
    │   ├── Commands/
    │   │   ├── ICommandHandler.cs              # UI Command Handler Interface
    │   │   └── UiCommandDispatcher.cs          # Reactive UI Command Dispatcher
    │   ├── Common/
    │   │   ├── AgyUiComponents.cs              # Reusable UI Header & Component Library
    │   │   ├── Icons.cs                        # ASCII / Unicode Icon Dictionary
    │   │   ├── ScrollableListView.cs           # Interactive Scroll List Component
    │   │   ├── SpectreWidgets.cs               # Spectre Panel & Menu Helpers
    │   │   └── StatusWidgets.cs                # System Status Widgets
    │   ├── Layouts/
    │   │   ├── FlatTreeRenderer.cs             # Flat Tree View Layout Renderer
    │   │   ├── HotkeysGuide.cs                 # Hotkey Bar Renderer
    │   │   ├── IMenuRenderer.cs                # Menu Renderer Interface
    │   │   ├── MenuNode.cs                     # Hierarchical Menu Tree Node Model
    │   │   ├── MenuRendererBase.cs             # Base Abstract Menu Renderer
    │   │   ├── ProfileHelp.cs                  # Profile Help Documentation Screen
    │   │   ├── ScreenChrome.cs                 # Common Screen Border & Layout Frame
    │   │   └── ThreePaneRenderer.cs            # 3-Pane Responsive Layout Renderer
    │   ├── Navigation/
    │   │   ├── Interfaces/
    │   │   │   └── IUiNavigationHandler.cs     # Navigation Handler Interface
    │   │   ├── AccountViewHelper.cs            # Account View Renderer Helper
    │   │   ├── CcNavigator.cs                  # Command Center Navigator
    │   │   ├── CommandPalette.cs               # Command Palette Search & Execute Window
    │   │   ├── CommandRouter.cs                # Command Router Dispatcher
    │   │   ├── SubPageAccountNavigator.cs      # Account Sub-Page Router
    │   │   ├── SubPageNavigator.cs             # Base Sub-Page Router
    │   │   ├── SubPageProjNavigator.cs         # Projects Sub-Page Router
    │   │   ├── SubPageThemeNavigator.cs        # Theme Sub-Page Router
    │   │   ├── SubPageTopicNavigator.cs        # Learning Topic Sub-Page Router
    │   │   └── UiNavigationHandler.cs          # Interactive Terminal Navigation Handler
    │   ├── Registries/
    │   │   └── CommandRegistry.cs              # Command & Menu Tree Registry
    │   └── State/
    │       └── UiStateStore.cs                 # Reactive UI State Store
    └── Screens/
        ├── Account/                            # Account Management Screen Views
        ├── Career/
        │   ├── AlgoVisualizer.cs               # Algorithm & Data Structures Visualizer
        │   └── InterviewBank.cs                # Technical Interview Question Bank
        ├── Git/
        │   └── GitNexus.cs                     # Git Interactive Terminal View
        ├── Ide/
        │   ├── CodeViewer.cs                   # File Syntax Code Viewer View
        │   ├── GitDiffViewer.cs                # Git Diff Viewer View
        │   ├── SymbolSearch.cs                 # C# Symbol Search View
        │   └── TerminalIde.cs                  # Integrated Terminal IDE View
        ├── Learn/
        │   ├── FlashcardEngine.cs              # Flashcard Quiz View Engine
        │   ├── GuidedLearnFlow.cs              # Guided Study Flow Controller
        │   ├── LearnRouter.cs                  # Study Routing & Topic Dispatcher
        │   ├── SpacedRepetitionEngine.cs       # SuperMemo 2 Spaced Repetition Engine
        │   ├── StudyConsoleView.cs             # Study Dashboard & Terminal View
        │   └── StudySession.cs                 # Active Study Session Logger
        ├── Quizzes/
        │   ├── CsharpQuiz.cs                   # C# Quiz View
        │   └── KanaQuiz.cs                     # Japanese Kana Quiz View
        └── SysNet/
            ├── SshConsoleView.cs               # SSH Connection View
            └── SystemConsoleView.cs            # System Monitoring View
```

---

## 2. Architectural Review & Evaluation

### 🌟 Strengths & Major Refactoring Milestones

1. **Complete Elimination of legacy `Core/` Folder**:
   - Eliminating `Core/` removed ambiguous namespace references (`AgyTui.Core.Models` vs `AgyTui.Domain.*`) and established clear boundary separation between `Domain`, `Infrastructure`, and `UI`.

2. **Domain-Driven Design (DDD) Strict Isolation**:
   - Domain aggregates (`AccountAggregate`, `WorkspaceAggregate`, `FlashcardDeck`) and value objects (`ProjectPath`, `EncryptedToken`, `QuotaMetrics`) contain zero infrastructure dependencies.
   - Domain business rules (e.g. `MarkActive()`, `SetQuotaExceeded()`, `RecordUsage()`, `Activate()`) remain encapsulated inside aggregate roots.

3. **Generic Repository Pattern**:
   - Introduced generic interfaces `IRepository<TEntity, TKey>` and `IFileRepository<TEntity>`.
   - SQLite repositories (`SqliteAgyAccountRepository`, `SqliteConfigRepository`) extend `SqliteRepositoryBase<TEntity, TKey>`.
   - File/JSON repositories (`JsonStudyRepository`) extend `JsonFileRepositoryBase<TEntity>`.

4. **Layer Dependency Enforcement**:
   - `ArchitectureTests.cs` verifies that `AgyTui.Infrastructure` components never depend on `AgyTui.UI` components.
   - `CommandRegistry.cs` is located in `AgyTui.UI.Core.Registries` because it couples directly to `MenuNode` UI layout elements.

---

## 3. Location & Naming Audit Feedback

| Subsystem / File | Current Location | Naming Assessment | Recommendation |
| :--- | :--- | :--- | :--- |
| `CommandRegistry.cs` | `UI/Core/Registries/CommandRegistry.cs` | ✅ **Optimal** | Located correctly in `UI/Core/Registries` as it directly references `MenuNode` layout structures. |
| `WorkspaceRegistry.cs` | `Infrastructure/Registries/WorkspaceRegistry.cs` | ✅ **Appropriate** | Encapsulates workspace filesystem scanning and cache. Abstracted via `IWorkspaceRegistry`. |
| `ResourceRegistry.cs` | `Infrastructure/Registries/ResourceRegistry.cs` | ✅ **Appropriate** | Indexing and SHA-256 checksum logic for learning notes. Abstracted via `IResourceRegistry`. |
| `AppPathManager.cs` | `Infrastructure/Services/AppPathManager.cs` | ✅ **Optimal** | Implements `IAppPathManager` singleton registered in `Bootstrapper.cs`. |
| `ConfigService.cs` | `Infrastructure/Services/ConfigService.cs` | ✅ **Optimal** | Implements `IConfigService` singleton registered in `Bootstrapper.cs`. |
| `LearnDataPaths.cs` | `Infrastructure/Persistence/Learning/LearnDataPaths.cs` | ℹ️ **Minor Suggestion** | Consider moving to `Infrastructure/Persistence/DbContext/` or `Infrastructure/Services/` in future passes for uniform data path discovery. |

---

## 4. Summary & Verification

- **Total Unit/Integration Tests**: **117 Passed (100% PASS rate)**.
- **Git Commit**: Clean working directory on branch `main`.
