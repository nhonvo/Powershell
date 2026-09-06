# 🧪 AgyTui Test Suite Architecture & Rebuild Plan (`AgyTui.Tests`)

**Generated At:** 2026-08-11  
**Target Test Project:** `csapp/AgyTui.Tests/`  
**Target Runtime:** .NET 9.0 (`net9.0`) / xUnit / Moq / FluentAssertions  
**Status:** Architecture Alignment & Test Rebuild Plan  

---

## 1. 🎯 Executive Plan & Architecture Alignment

Following the **Clean UI Architecture Refactoring** of `csapp/AgyTui/`, the test suite in **`csapp/AgyTui.Tests/`** is being reorganized to mirror the production UI project directory structure 1-to-1:

```text
       PRODUCTION CODEBASE (csapp/AgyTui/UI/)          TEST SUITE (csapp/AgyTui.Tests/Unit/UI/)
       ├── Core/                                ──►   ├── Core/
       │   ├── Abstractions/                    ──►   │   ├── Abstractions/
       │   ├── Commands/                        ──►   │   ├── Commands/
       │   ├── Components/                      ──►   │   ├── Components/
       │   ├── Layouts/                         ──►   │   ├── Layouts/
       │   ├── Navigation/                      ──►   │   ├── Navigation/
       │   └── State/                           ──►   │   └── State/
       └── Screens/                             ──►   └── Screens/ (12 Category Test Suites)
```

---

## 2. 📂 Proposed Test Directory Tree (`AgyTui.Tests/Unit/UI/`)

```text
csapp/AgyTui.Tests/Unit/UI/
├── Core/                                # 📌 UI CORE ENGINE UNIT TESTS
│   ├── Abstractions/                    # 🔹 System Contract & State Tests
│   │   ├── ScreenStateTests.cs          # Tests for ScreenState (search filter, selectedIndex bounds)
│   │   └── IScreenViewContractTests.cs  # Contract compliance tests for screen views
│   │
│   ├── Commands/                        # 🔹 Command Catalog & Dispatcher Tests
│   │   ├── CommandRegistryTests.cs      # Validates all 35+ command entries, hotkeys & categories
│   │   └── UiCommandDispatcherTests.cs  # Tests command text string parsing & dispatching
│   │
│   ├── Components/                      # 🔹 Atomic UI Components & Widgets Tests
│   │   ├── CommandPaletteTests.cs       # Interactive modal palette filtering tests
│   │   ├── FooterTitleBarTests.cs       # Footer title bar markup generator tests
│   │   ├── IconsTests.cs                # Unicode & UTF-8 glyph display width tests
│   │   ├── ScrollableListViewTests.cs   # Viewport range calculator tests
│   │   ├── SpectreWidgetsTests.cs       # Spectre panel & banner wrapper tests
│   │   └── StatusWidgetsTests.cs        # Status bar widgets & badges tests
│   │
│   ├── Layouts/                         # 🔹 6 View Renderer Engine Tests
│   │   ├── CardFrameEngineRendererTests.cs   # Engine 6: Centered Spectre Card Frame tests
│   │   ├── DualPaneExplorerRendererTests.cs  # Engine 4: Dual Pane IDE Explorer tests
│   │   ├── FlatTreeRendererTests.cs          # Engine 3: Indented Tree View collapse/expand tests
│   │   ├── LogStreamViewportRendererTests.cs # Engine 5: Paged Log Viewer live tail tests
│   │   ├── ScreenChromeTests.cs              # Zero-Lag ANSI \x1b[H buffer & stdout stream tests
│   │   ├── ThreePaneRendererTests.cs         # Engine 2: Triple Column Split layout tests
│   │   └── ZeroLagStreamListRendererTests.cs # Engine 1: Header-Free Single Column List tests
│   │
│   ├── Navigation/                      # 🔹 Routing & Stack Runner Tests
│   │   ├── CcNavigatorTests.cs          # Global Control Center TUI runner tests
│   │   ├── CommandRouterTests.cs        # Main command router & alias dispatch tests
│   │   ├── ScreenNavigatorTests.cs      # Stack-based screen navigator (Push/Pop) tests
│   │   ├── SubPageNavigatorTests.cs     # Sub-page navigation framework tests
│   │   └── UiNavigationHandlerTests.cs # Navigation event loop handler tests
│   │
│   └── State/                           # 🔹 Reactive TUI State Store Tests
│       └── UiStateStoreTests.cs         # State store mutation & reactive event tests
│
└── Screens/                             # 📌 12 CATEGORY SCREEN VIEW UNIT TESTS
    ├── Career/
    │   ├── AlgoVisualizerScreenTests.cs
    │   ├── InterviewQuestionScreenTests.cs
    │   └── StarBuilderScreenTests.cs
    ├── Customization/
    │   ├── AccountManagerScreenTests.cs
    │   ├── FavoritesManagerScreenTests.cs
    │   ├── ThemeSelectorScreenTests.cs
    │   └── TopicSelectorScreenTests.cs
    ├── Database/
    │   ├── AddMigrationScreenTests.cs
    │   └── UpdateDatabaseScreenTests.cs
    ├── Diagnostics/
    │   ├── DockerContainerLogScreenTests.cs
    │   └── SystemDiagnosticLogScreenTests.cs
    ├── GitNexus/
    │   ├── CommitStatsChartScreenTests.cs
    │   ├── MultiRepoSyncScreenTests.cs
    │   └── RepoGraphTreeScreenTests.cs
    ├── Ide/
    │   ├── GitDiffViewerScreenTests.cs
    │   ├── PatternSearchScreenTests.cs
    │   ├── SymbolSearchOverlayScreenTests.cs
    │   └── TerminalIdeExplorerScreenTests.cs
    ├── Infrastructure/
    │   ├── AntigravityDeckScreenTests.cs
    │   ├── AwsInspectorScreenTests.cs
    │   ├── ObsidianSyncScreenTests.cs
    │   └── SecretVaultScreenTests.cs
    ├── Learn/
    │   ├── FlashcardEngineScreenTests.cs
    │   ├── MasterLearnHubScreenTests.cs
    │   └── StudyStatisticsScreenTests.cs
    ├── Ollama/
    │   ├── OllamaBenchmarkScreenTests.cs
    │   ├── OllamaModelManagerScreenTests.cs
    │   └── OllamaStatusScreenTests.cs
    ├── Quizzes/
    │   ├── CsharpQuizScreenTests.cs
    │   ├── KanaQuizScreenTests.cs
    │   └── SnippetLibraryScreenTests.cs
    ├── Scaffolder/
    │   └── ScaffoldScreenTests.cs
    └── Workspace/
        ├── WorkspaceDiscoverScreenTests.cs
        ├── WorkspaceNavigatorScreenTests.cs
        └── WorkspacePruneScreenTests.cs
```

---

## 3. 🧪 Key Unit Test Targets & Assertions

| Test Category | Target Class | Key Assertions & Scenarios |
| :--- | :--- | :--- |
| **Zero-Lag Stream** | `ScreenChrome` | Asserts `\x1b[H` escape sequence is prepended to buffer; verifies no scroll flicker occurs during render. |
| **Flat Tree Engine**| `FlatTreeRenderer` | Asserts category node collapse (`-`) and expansion (`+`); verifies fuzzy search filter matching. |
| **Command Router** | `CommandRouter` | Asserts all 35+ command aliases resolve to non-null screen handlers; verifies feature flags block unauthorized execution. |
| **Screen Stack** | `ScreenNavigator` | Asserts `PushState` pushes screen to stack and `PopState` pops back to parent screen without losing state. |
| **Viewport Math** | `ScrollableListView` | Asserts `ComputeViewport(totalCount, selectedIdx, maxRows)` handles zero-length arrays and out-of-bounds selection safely. |
| **Screen Views** | `IScreenView` implementations | Asserts `GetItemCount(filter)` handles empty and non-empty filters; verifies `HandleInput` handles `Enter` and `Esc` navigation results. |

---

## 4. 📅 Implementation Phases

- [x] **Phase 1: Namespace Synchronization**: Updated legacy `using AgyTui.UI.Core.Registries;` and `using AgyTui.UI.Core.Common;` imports across all test files to match new core namespaces.
- [ ] **Phase 2: Directory Restructuring**: Move test files in `AgyTui.Tests/Unit/UI/` into `Core/` (`Abstractions/`, `Commands/`, `Components/`, `Layouts/`, `Navigation/`, `State/`) and `Screens/` subfolders.
- [ ] **Phase 3: Screen View Coverage Expansion**: Add dedicated `*ScreenTests.cs` unit tests for each of the 35 screen view classes.
- [ ] **Phase 4: Continuous Test Run Verification**: Ensure `dotnet test` executes with 100% pass rate.
