# 🚀 Antigravity TUI (AgyTui) — Comprehensive CLI Clean Architecture Specification

**Generated At:** 2026-08-11  
**Project Name:** `AgyTui` (Antigravity Terminal User Interface CLI)  
**Target Framework:** .NET 9.0 / C# 13  
**Architecture Pattern:** Clean Architecture (Domain-Driven Design + Modular TUI)  

---

## 1. 🏛️ Architectural Overview & Design Principles

The **AgyTui** CLI application is built following **Clean Architecture** principles, strict separation of concerns, and high maintainability. The codebase is partitioned into three core layers:

```text
       ┌─────────────────────────────────────────────────────────┐
       │                   UI LAYER (Presentation)              │
       │  ┌───────────────────────┐   ┌───────────────────────┐  │
       │  │    UI/Core Engine     │   │   UI/Screens Domain   │  │
       │  │ (Layouts/Nav/Comps)   │   │  (12 Screen Subsystems)│  │
       │  └───────────────────────┘   └───────────────────────┘  │
       └────────────────────────────┬────────────────────────────┘
                                    │
                                    ▼
       ┌─────────────────────────────────────────────────────────┐
       │                  INFRASTRUCTURE LAYER                   │
       │  ┌──────────────┐ ┌──────────────┐ ┌─────────────────┐ │
       │  │ Integrations │ │ Persistence  │ │ Dependency Inj. │ │
       │  │ (AI/Git/AWS) │ │(SQLite/JSON) │ │ (Bootstrapper)  │ │
       │  └──────────────┘ └──────────────┘ └─────────────────┘ │
       └────────────────────────────┬────────────────────────────┘
                                    │
                                    ▼
       ┌─────────────────────────────────────────────────────────┐
       │                     DOMAIN LAYER                        │
       │  ┌───────────────────────┐   ┌───────────────────────┐  │
       │  │   Domain Aggregates   │   │     Value Objects     │  │
       │  │ (Account/Learn/Work)  │   │  (Tokens/Quota/Paths) │  │
       │  └───────────────────────┘   └───────────────────────┘  │
       └─────────────────────────────────────────────────────────┘
```

### Core Design Principles:
1. **Dependency Rule**: Outer layers (`UI`, `Infrastructure`) depend inward on the `Domain` layer. The `Domain` layer has zero dependencies on external frameworks or Spectre.Console.
2. **Modular Presentation**: Presentation logic is isolated in `UI/Core/` (reusable engines, viewports, command dispatchers) and `UI/Screens/` (feature views).
3. **Isolated Abstractions**: Interfaces reside inside dedicated `Abstractions/` sub-folders within each core module to prevent mixing contracts with implementations.
4. **Rich Infrastructure Integrations**: Direct wrappers for external tools (Git, Ollama, Claude, AWS, Docker, EF Core, Obsidian) reside in `Infrastructure/Integrations/`.
5. **Persistence Agnosticism**: Data access uses Repository interfaces (`IAgyAccountRepository`, `IWorkspaceRepository`, `IStudyRepository`) with SQLite & JSON file storage providers.

---

## 2. 📂 Full CLI Project File Tree

```text
csapp/AgyTui/
├── Domain/                              # 📌 1. DOMAIN LAYER (Zero Dependencies)
│   ├── AccountContext/                  # Account Aggregate & Value Objects
│   │   ├── AccountAggregate.cs          # Developer Account Domain Root
│   │   ├── AccountCredentials.cs        # API Keys & Encrypted Credentials
│   │   ├── AccountMetadata.cs           # Email, Tier, Quota Status
│   │   ├── EncryptedToken.cs            # Token Value Object
│   │   └── QuotaMetrics.cs              # Request/Token Quota Metrics
│   │
│   ├── AiAgentContext/                  # AI Provider Domain Context
│   │   ├── AgentInvocationLog.cs        # Agent Execution Audit Record
│   │   └── ProviderMode.cs              # Provider Mode Enum (Claude, Ollama, Hermes, OpenClaw)
│   │
│   ├── LearnContext/                    # Learning & Spaced Repetition Domain
│   │   ├── FlashcardDeck.cs             # Deck Aggregate & Flashcard Entity
│   │   └── LearningModels.cs            # SM-2 Spaced Repetition State & Score
│   │
│   ├── WorkspaceContext/                # Workspace Domain Context
│   │   ├── ProjectPath.cs               # Strongly-Typed Project Path Value Object
│   │   ├── WorkspaceAggregate.cs        # Registered Workspace Aggregate Root
│   │   └── WorkspaceModels.cs           # Workspace Metadata & Status
│   │
│   ├── Common/                          # Shared Domain Utilities
│   │   └── ErrorConstants.cs            # System-wide Error Code Constants
│   │
│   └── Exceptions/                      # Domain Exceptions
│       └── AgyTuiException.cs           # Base Domain Exception
│
├── Infrastructure/                      # 📌 2. INFRASTRUCTURE LAYER (Integrations & Persistence)
│   ├── Di/                              # Dependency Injection
│   │   └── Bootstrapper.cs              # Service Collection & Container Configuration
│   │
│   ├── Configuration/                   # App Configuration & Environment
│   │   ├── Config.cs                    # Strongly-typed App Settings model
│   │   └── EnvironmentProvider.cs       # OS & Environment Var Provider
│   │
│   ├── Integrations/                    # External Tool & Service Integrations
│   │   ├── AgyClient/                   # AGYSWITCH & Vault Integrations
│   │   │   ├── Interfaces/ (IAgyAccountStore, IAgyQuotaEngine, IAgyVault)
│   │   │   ├── AgyAccountStore.cs       # Account Switching Engine
│   │   │   ├── AgyQuotaEngine.cs        # Quota Tracking Engine
│   │   │   └── AgyVault.cs              # Encrypted Secret Vault
│   │   │
│   │   ├── Ai/                          # Multi-Provider AI LLM Engines
│   │   │   ├── Interfaces/ (IClaudeClient, IOllamaClient, IHermesClient, IOpenClawClient)
│   │   │   ├── Providers/               # ClaudeProvider, OllamaClient, HermesProvider, OpenClawProvider
│   │   │   └── Services/                # AiCommitGenerator, AiLearningGenerator, AiProcessRunner
│   │   │
│   │   ├── Aws/                         # AWS Infrastructure Client
│   │   │   ├── Interfaces/ (IAwsClient)
│   │   │   └── AwsClient.cs
│   │   │
│   │   ├── Docker/                      # Docker Engine Client
│   │   │   ├── Interfaces/ (IDockerClient)
│   │   │   └── DockerClient.cs
│   │   │
│   │   ├── DotNet/                      # .NET CLI Client
│   │   │   ├── Interfaces/ (IDotNetClient)
│   │   │   └── DotNetClient.cs
│   │   │
│   │   ├── Git/                         # Git Subprocess Client
│   │   │   ├── Interfaces/ (IGitClient)
│   │   │   └── GitClient.cs
│   │   │
│   │   ├── Obsidian/                    # Obsidian Vault Bridge
│   │   │   ├── Interfaces/ (IObsidianBridge)
│   │   │   └── ObsidianClient.cs
│   │   │
│   │   └── Sys/                         # Subsystem Management Clients
│   │       ├── AntigravityDeckClient.cs
│   │       └── AntigravityManagerClient.cs
│   │
│   ├── Persistence/                     # Database & File Storage Providers
│   │   ├── DbContext/                   # SQLite Connection & Migration Engine
│   │   │   ├── Interfaces/ (ILearningDataSeeder)
│   │   │   ├── LearnDataPaths.cs        # Data Directory Resolution
│   │   │   ├── LearningDataSeeder.cs    # Seeding Engine
│   │   │   └── SqliteDatabase.cs        # Connection Manager
│   │   │
│   │   ├── Repositories/                # Repository Implementations
│   │   │   ├── SqliteAgyAccountRepository.cs
│   │   │   ├── SqliteConfigRepository.cs
│   │   │   ├── SqliteWorkspaceRepository.cs
│   │   │   └── JsonStudyRepository.cs
│   │   │
│   │   ├── Migrations/                  # SQL Schema Version Migrations (V1 to V6)
│   │   └── Seeding/                     # Data Seeders (Master, Account, Learning, Theme)
│   │
│   ├── Common/                          # Infrastructure Helper Utilities
│   │   ├── Interfaces/ (IProcessRunner, IHttpClientProvider, IThemeManager, IEditorResolver)
│   │   ├── AppPaths.cs                  # File System Path Constants
│   │   ├── HttpClientProvider.cs        # Thread-safe Shared HttpClient Instance
│   │   ├── ProcessRunner.cs             # Subprocess Spawner & Output Streamer
│   │   ├── ThemeManager.cs              # Oh-My-Posh Theme Loader
│   │   └── TtlCache.cs                  # Time-To-Live Cache Provider
│   │
│   └── Logging/ & Middleware/           # Logging & Exception Middleware
│       ├── CommandLoggingMiddleware.cs  # Command Execution Logger
│       ├── ExceptionMiddleware.cs       # Global Exception Handler
│       └── FileErrorLogger.cs           # Log File Writer
│
└── UI/                                  # 📌 3. PRESENTATION LAYER (Clean TUI Framework)
    ├── Core/                            # Core Rendering & Navigation Engines
    │   ├── Abstractions/                # Top-Level System Contracts (ICommand, ICommandRouter, IScreenView, ScreenState)
    │   ├── Commands/                    # Static Command Registry & Dispatcher (CommandRegistry, UiCommandDispatcher)
    │   ├── Components/                  # Reusable Atomic Visual Widgets
    │   │   ├── Abstractions/            # Component Interfaces (IAgyUiComponents, ICommandPalette, IIcons, IScrollableListView, ISpectreWidgets, IStatusWidget)
    │   │   ├── AgyUiComponents.cs       # Atomic Component Helpers
    │   │   ├── CommandPalette.cs        # Modal Command Palette Overlay (/cc)
    │   │   ├── FooterTitleBar.cs        # Footer Hotkey Hint Generator
    │   │   ├── Icons.cs                 # Unicode & UTF-8 Icon Registry
    │   │   ├── ScrollableListView.cs    # Viewport Row Bounds Calculator
    │   │   ├── SpectreWidgets.cs        # Standard Spectre Panels & Banners
    │   │   └── StatusWidgets.cs         # Status Bar Widgets & Badges
    │   │
    │   ├── Layouts/                     # 6 Standardized View Renderer Engines
    │   │   ├── Abstractions/            # Layout Engine Interfaces (IMenuNodeBuilder, IMenuRenderer)
    │   │   ├── CardFrameEngineRenderer.cs   # Engine 6: Centered Card Frame Engine
    │   │   ├── DualPaneExplorerRenderer.cs  # Engine 4: Dual Pane IDE Explorer Engine
    │   │   ├── FlatTreeRenderer.cs          # Engine 3: Indented Expandable Tree View Engine
    │   │   ├── LogStreamViewportRenderer.cs # Engine 5: Paged ANSI Log Stream Engine
    │   │   ├── ThreePaneRenderer.cs         # Engine 2: Triple Column Split Engine
    │   │   ├── ZeroLagStreamListRenderer.cs # Engine 1: Single-Column Stream List Engine
    │   │   ├── LayoutCalculator.cs          # Layout Bounds & Padding Calculator
    │   │   ├── MenuNode.cs                  # Hierarchical Menu Node Definition
    │   │   └── ScreenChrome.cs              # Zero-Lag Screen Buffer (\x1b[H)
    │   │
    │   ├── Navigation/                  # Navigation Dispatchers & Routers
    │   │   ├── Abstractions/            # Navigation Contracts (ICcNavigator, ISubPageNavigator, IUiNavigationHandler)
    │   │   ├── Routers/                 # Sub-Domain Routers (AiCommandRouter, GitCommandRouter, LearnCommandRouter, SystemCommandRouter)
    │   │   ├── CcNavigator.cs           # Control Center Global View Runner
    │   │   ├── CommandRouter.cs         # Master CLI Command Router
    │   │   ├── ScreenNavigator.cs       # Stack-Based View Runner
    │   │   └── SubPageNavigator.cs      # Base Sub-Page Navigator Framework
    │   │
    │   └── State/                       # Reactive State Storage
    │       ├── IUiStateStore.cs
    │       └── UiStateStore.cs
    │
    └── Screens/                         # 12 Subsystem Screen View Categories
        ├── Career/ (AlgoVisualizerScreen, InterviewQuestionScreen, StarBuilderScreen)
        ├── Customization/ (AccountManagerScreen, FavoritesManagerScreen, ThemeSelectorScreen, TopicSelectorScreen)
        ├── Database/ (AddMigrationScreen, UpdateDatabaseScreen)
        ├── Diagnostics/ (DockerContainerLogScreen, SystemDiagnosticLogScreen)
        ├── GitNexus/ (CommitStatsChartScreen, MultiRepoSyncScreen, RepoGraphTreeScreen)
        ├── Ide/ (GitDiffViewerScreen, PatternSearchScreen, SymbolSearchOverlayScreen, TerminalIdeExplorerScreen)
        ├── Infrastructure/ (AntigravityDeckScreen, AwsInspectorScreen, ObsidianSyncScreen, SecretVaultScreen)
        ├── Learn/ (FlashcardEngineScreen, MasterLearnHubScreen, StudyStatisticsScreen)
        ├── Ollama/ (OllamaBenchmarkScreen, OllamaModelManagerScreen, OllamaStatusScreen)
        ├── Quizzes/ (CsharpQuizScreen, KanaQuizScreen, SnippetLibraryScreen)
        ├── Scaffolder/ (ScaffoldScreen)
        ├── Workspace/ (WorkspaceDiscoverScreen, WorkspaceNavigatorScreen, WorkspacePruneScreen)
        └── Services/ (ScreenSuiteServices DI Container)
```

---

## 3. 🔄 System Execution Flow & Data Pipeline

The CLI follows an **Event-Driven Dispatch Pipeline**:

```text
 ┌──────────────┐     ┌─────────────────────┐     ┌─────────────────────┐
 │ User Command │ ──► │  UiCommandDispatcher│ ──► │    CommandRouter    │
 │ (e.g. "ide") │     │ (Parses Alias/Args) │     │(Maps to ScreenKey)  │
 └──────────────┘     └─────────────────────┘     └──────────┬──────────┘
                                                             │
                                                             ▼
 ┌──────────────┐     ┌─────────────────────┐     ┌─────────────────────┐
 │    Output    │ ◄── │ View Renderer Engine│ ◄── │   IScreenView       │
 │   (Terminal) │     │ (Spectre.Console)   │     │ (TerminalIdeExplorer│
 └──────────────┘     └─────────────────────┘     └──────────┬──────────┘
                                                             │ (Fetches Data)
                                                             ▼
                                                  ┌─────────────────────┐
                                                  │ Integration/Repo    │
                                                  │ (GitClient/Sqlite)  │
                                                  └─────────────────────┘
```

1. **Invocation**: User types a command alias in the CLI (e.g. `ide`, `gbr`, `agysw`).
2. **Dispatching**: `UiCommandDispatcher` resolves alias against `CommandRegistry`.
3. **Routing**: `CommandRouter` directs call to the target `IScreenView` in `UI/Screens/<Category>/`.
4. **Execution & Data Fetching**: Screen View uses DI-injected Infrastructure Services (`IGitClient`, `IOllamaClient`, `IAgyAccountRepository`) to fetch domain models.
5. **Rendering**: Screen View renders via one of the 6 standardized **View Renderer Engines** in `UI/Core/Layouts/`.
6. **Double Buffering**: Output is streamed to terminal stdout via `ScreenChrome` zero-lag buffer (`\x1b[H`).

---

## 4. 🖼️ The 6 Standardized Presentation Renderer Engines

All 35 screen views in `UI/Screens/` derive their layout from one of 6 core presentation renderer engines:

| Engine | Name | Primary Use Case | Output Visual Structure |
| :--- | :--- | :--- | :--- |
| **Engine 1** | `ZeroLagStreamListRenderer` | Single-column selectable lists, quick search screens | Header-free single-column item list + Footer hint bar |
| **Engine 2** | `ThreePaneRenderer` | Multi-category Explorer (Git Nexus, AWS, Benchmarks) | Triple column split (Categories \| Items \| Details) |
| **Engine 3** | `FlatTreeRenderer` | Workspace & Global View Tree Navigators (`cnav`) | Indented expandable tree view with hotkey badges |
| **Engine 4** | `DualPaneExplorerRenderer` | Terminal IDE Explorer & File Code Viewer (`ide`) | Dual column (File Explorer Tree \| Paged Code Viewport) |
| **Engine 5** | `LogStreamViewportRenderer` | System Diagnostic Logs & Docker Tail (`/log`, `dlogsu`) | Paged ANSI log stream viewer with live tailing |
| **Engine 6** | `CardFrameEngineRenderer` | Quizzes, STAR Builder, Flashcards & Stats | Centered Spectre Card Frame with grade indicators |

---

## 5. 🛠️ Technology Stack & Dependencies

- **Runtime**: .NET 9.0 (C# 13)
- **TUI Renderer**: Spectre.Console
- **Database / Storage**: SQLite, Entity Framework Core 9.0, System.Text.Json
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Subprocess Runner**: System.Diagnostics.Process with Stream Buffering
- **AI Integrations**: Claude Code CLI API, Ollama REST API, Hermes API, OpenClaw Protocol

---

## 6. ✅ Architectural Quality Checklist

- [x] **Zero Circular Dependencies**: Inward dependency rule (`UI` ➔ `Infrastructure` ➔ `Domain`).
- [x] **Isolated Interface Contracts**: Every Core & Subsystem directory isolates contracts in `Abstractions/`.
- [x] **12 Category Subsystems**: `UI/Screens/` cleanly categorizes all 35 TUI screen views into 12 domain subdirectories with `Helpers/` and `Navigators/`.
- [x] **Single Command Registry**: `CommandRegistry.cs` is the single source of truth for command metadata, hotkeys, categories, and aliases.
