# AgyTuiApp Directory Structure Refactor Plan

This document outlines the current file layout of the `AgyTuiApp` codebase and proposes a clean, modular structure following Clean Architecture principles for C# console applications.

---

## 🌳 1. Current File Tree

Below is the complete recursive file tree of `csapp/AgyTuiApp`:

```
csapp/AgyTuiApp/
├── AgyTuiApp.csproj
├── Program.cs
├── CommandRegistry.cs
├── ThemeHelper.cs
├── Services/
│   ├── AccountHelper.cs
│   ├── AgySecretVault.cs
│   ├── AiHelper.cs
│   ├── AntigravityDeckHelper.cs
│   ├── AntigravityManagerHelper.cs
│   ├── AwsService.cs
│   ├── DatabaseHelper.cs
│   ├── DockerService.cs
│   ├── DotNetService.cs
│   ├── FlashcardEngine.cs
│   ├── GitService.cs
│   ├── GuidedLearnFlow.cs
│   ├── ObsidianHelper.cs
│   ├── OllamaHelper.cs
│   ├── Projects.cs
│   ├── SkillService.cs
│   ├── SpacedRepetitionEngine.cs
│   ├── SshHelper.cs
│   ├── StudyHelper.cs
│   ├── StudySession.cs
│   ├── SystemHelper.cs
│   ├── Domain/
│   │   ├── AccountRepository.cs
│   │   ├── AiLearningGenerator.cs
│   │   ├── Config.cs
│   │   ├── IdeCommandRegistry.cs
│   │   ├── LearnRouter.cs
│   │   ├── ProjectScaffolder.cs
│   │   ├── QuotaTracker.cs
│   │   ├── ResourceExtractor.cs
│   │   ├── ResourceRegistry.cs
│   │   ├── TokenVault.cs
│   │   └── WorkspaceRegistry.cs
│   ├── Infra/
│   │   ├── EditorResolver.cs
│   │   ├── HttpClientProvider.cs
│   │   ├── ProcessRunner.cs
│   │   └── TtlCache.cs
│   └── Infrastructure/
│       ├── AgyServices.cs
│       ├── CliToolWrapper.cs
│       ├── HelperCompatibility.cs
│       ├── IAccountRepository.cs
│       ├── IStudyRepository.cs
│       ├── JsonAccountRepository.cs
│       └── JsonStudyRepository.cs
└── Views/
    ├── AccountViewHelper.cs
    ├── AgyHeader.cs
    ├── CcNavigator.cs
    ├── CommandPalette.cs
    ├── FlatTreeRenderer.cs
    ├── HotkeysGuide.cs
    ├── IMenuRenderer.cs
    ├── MenuNode.cs
    ├── ProfileHelp.cs
    ├── ThreePaneRenderer.cs
    ├── Components/
    │   ├── Icons.cs
    │   ├── ScreenChrome.cs
    │   ├── ScrollableListView.cs
    │   ├── SpectreWidgets.cs
    │   ├── StatusWidgets.cs
    │   ├── SubPageAccountNavigator.cs
    │   ├── SubPageNavigator.cs
    │   ├── SubPageProjNavigator.cs
    │   ├── SubPageThemeNavigator.cs
    │   └── SubPageTopicNavigator.cs
    ├── Renderers/
    │   └── MenuRendererBase.cs
    └── Screens/
        ├── Career/
        │   └── InterviewBank.cs
        ├── Dsa/
        │   └── AlgoVisualizer.cs
        ├── Git/
        │   └── GitNexus.cs
        ├── Ide/
        │   ├── CodeViewer.cs
        │   ├── FileExplorer.cs
        │   ├── GitDiffViewer.cs
        │   ├── SymbolSearch.cs
        │   └── TerminalIde.cs
        └── Quizzes/
            ├── CsharpQuiz.cs
            └── KanaQuiz.cs
```

---

## 🛠 2. Proposed Clean Architecture Structure

To separate concerns, remove duplicate folders (like `Infra` vs `Infrastructure`), and group business logic separately from view renderers, we propose refactoring into three main high-level layers: **Core**, **Infrastructure**, and **UI**.

### Proposed Layout Tree:

```
csapp/AgyTuiApp/
├── AgyTuiApp.csproj
├── Program.cs                           # App Entry Point
├── Core/                                # Domain Entities & Business Logic (Pure C#)
│   ├── Models/                          # Data Models
│   │   ├── Account.cs
│   │   ├── Workspace.cs
│   │   ├── CommandEntry.cs
│   │   └── Quiz.cs
│   ├── Registries/                      # Core Registries
│   │   ├── CommandRegistry.cs
│   │   └── StatusWidgetRegistry.cs
│   └── Interfaces/                      # Contracts for Data Access
│       ├── IAccountRepository.cs
│       └── IStudyRepository.cs
├── Infrastructure/                      # Persistence, Utilities, and Integrations
│   ├── Persistence/                     # JSON storage & local settings vaults
│   │   ├── JsonAccountRepository.cs
│   │   ├── JsonStudyRepository.cs
│   │   ├── TokenVault.cs
│   │   └── AgySecretVault.cs
│   ├── Common/                          # Shared OS Helpers
│   │   ├── ProcessRunner.cs
│   │   ├── HttpClientProvider.cs
│   │   ├── TtlCache.cs
│   │   └── ThemeHelper.cs
│   └── Integrations/                    # Wrappers for external command lines/tools
│       ├── Git/                         # GitService, GitHelper
│       ├── Docker/                      # DockerService, DockerHelper
│       ├── Aws/                         # AwsService, AwsHelper
│       ├── DotNet/                      # DotNetService, DotNetHelper
│       ├── Ollama/                      # OllamaHelper, AiHelper
│       └── System/                      # SystemHelper, SshHelper
└── UI/                                  # Console Presentation (Spectre.Console View Layer)
    ├── Core/                            # Base layout elements and components
    │   ├── Navigation/                  # CcNavigator, SubPageNavigator, etc.
    │   ├── Layouts/                     # ScreenChrome, FlatTreeRenderer, ThreePaneRenderer
    │   └── Common/                      # ScrollableListView, Icons, SpectreWidgets
    └── Screens/                         # Modular interactive views
        ├── Ide/                         # TerminalIde, CodeViewer, FileExplorer, GitDiffViewer, SymbolSearch
        ├── Learn/                       # SpacedRepetition, StudySession, StudyHelper, GuidedLearnFlow
        ├── Quizzes/                     # CsharpQuiz, KanaQuiz
        └── Career/                      # InterviewBank, AlgoVisualizer (DSA)
```

---

## 🏷 3. Naming Convention Enhancements (Dropping "_Helper")

Currently, many classes are named with the generic `*Helper` or `*Service` suffix (e.g. `GitHelper` and `GitService`). Having both is confusing, and using `Helper` is a code smell that leads to bloated classes with mixed concerns.

To make the codebase self-documenting and separate low-level logic from UI presentation, we will rename these integrations using clear, specific architectural suffixes:

### 🔌 A. Low-Level Integrations: Use `Client`
For classes whose primary job is interfacing with external tools, APIs, or command lines (spawning processes, making HTTP calls, reading JSON), we drop `Helper` and `Service` and use **`Client`**.
- `GitService` / `GitHelper` (core execution) $\rightarrow$ **`GitClient`**
- `DockerService` / `DockerHelper` (core execution) $\rightarrow$ **`DockerClient`**
- `AwsService` / `AwsHelper` (core execution) $\rightarrow$ **`AwsClient`**
- `DotNetService` / `DotNetHelper` (core execution) $\rightarrow$ **`DotNetClient`**
- `OllamaHelper` / `AiHelper` (core execution) $\rightarrow$ **`OllamaClient`**

### 🖥 B. High-Level Console Presenters: Use `ConsoleView`
For classes whose primary job is executing a workflow and rendering Spectre.Console layout tables, graphs, progress bars, or prompts in the console window, we rename them to **`ConsoleView`** (placed in the `UI` layer).
- `GitHelper` (interactive UI commands) $\rightarrow$ **`GitConsoleView`**
- `DockerHelper` (interactive dashboard UI) $\rightarrow$ **`DockerConsoleView`**
- `SystemHelper` (interactive disk tables / port killing UI) $\rightarrow$ **`SystemConsoleView`**
- `SshHelper` (interactive connection logs / QR presentation UI) $\rightarrow$ **`SshConsoleView`**
- `AccountHelper` / `AccountViewHelper` (interactive accounts list UI) $\rightarrow$ **`AccountConsoleView`**

### 🧠 C. Domain Engine Components: Keep `Engine` / `Flow`
For classes that manage core calculations, state-machine tracking, or learning logic, we use clear domain suffixes:
- `SpacedRepetitionEngine` (SM-2 calculations) $\rightarrow$ **`SpacedRepetitionEngine`** (Domain Engine)
- `FlashcardEngine` (Vocabulary matching) $\rightarrow$ **`FlashcardEngine`** (Domain Engine)
- `GuidedLearnFlow` (Study session state) $\rightarrow$ **`GuidedLearnFlow`** (Domain Flow)

---

## 📈 4. Mapping of Relocated Files

Here is how the main source files will map to the new modular structure:

| Original File Path | Proposed File Path | Layer |
| :--- | :--- | :--- |
| `CommandRegistry.cs` | `Core/Registries/CommandRegistry.cs` | **Core** |
| `Services/Infrastructure/IAccountRepository.cs` | `Core/Interfaces/IAccountRepository.cs` | **Core** |
| `Services/Infrastructure/IStudyRepository.cs` | `Core/Interfaces/IStudyRepository.cs` | **Core** |
| `Services/Infrastructure/JsonAccountRepository.cs` | `Infrastructure/Persistence/JsonAccountRepository.cs` | **Infrastructure** |
| `Services/Infrastructure/JsonStudyRepository.cs` | `Infrastructure/Persistence/JsonStudyRepository.cs` | **Infrastructure** |
| `Services/AgySecretVault.cs` | `Infrastructure/Persistence/AgySecretVault.cs` | **Infrastructure** |
| `Services/Domain/TokenVault.cs` | `Infrastructure/Persistence/TokenVault.cs` | **Infrastructure** |
| `Services/Infra/ProcessRunner.cs` | `Infrastructure/Common/ProcessRunner.cs` | **Infrastructure** |
| `Services/Infra/HttpClientProvider.cs` | `Infrastructure/Common/HttpClientProvider.cs` | **Infrastructure** |
| `Services/Infra/TtlCache.cs` | `Infrastructure/Common/TtlCache.cs` | **Infrastructure** |
| `ThemeHelper.cs` | `Infrastructure/Common/ThemeHelper.cs` | **Infrastructure** |
| `Services/GitService.cs` | `Infrastructure/Integrations/Git/GitService.cs` | **Infrastructure** |
| `Services/DockerService.cs` | `Infrastructure/Integrations/Docker/DockerService.cs` | **Infrastructure** |
| `Services/AwsService.cs` | `Infrastructure/Integrations/Aws/AwsService.cs` | **Infrastructure** |
| `Services/DotNetService.cs` | `Infrastructure/Integrations/DotNet/DotNetService.cs` | **Infrastructure** |
| `Services/OllamaHelper.cs` | `Infrastructure/Integrations/Ollama/OllamaHelper.cs` | **Infrastructure** |
| `Services/AiHelper.cs` | `Infrastructure/Integrations/Ollama/AiHelper.cs` | **Infrastructure** |
| `Services/SystemHelper.cs` | `Infrastructure/Integrations/System/SystemHelper.cs` | **Infrastructure** |
| `Services/SshHelper.cs` | `Infrastructure/Integrations/System/SshHelper.cs` | **Infrastructure** |
| `Views/FlatTreeRenderer.cs` | `UI/Core/Layouts/FlatTreeRenderer.cs` | **UI** |
| `Views/ThreePaneRenderer.cs` | `UI/Core/Layouts/ThreePaneRenderer.cs` | **UI** |
| `Views/CcNavigator.cs` | `UI/Core/Navigation/CcNavigator.cs` | **UI** |
| `Views/Components/SubPageNavigator.cs` | `UI/Core/Navigation/SubPageNavigator.cs` | **UI** |
| `Views/Screens/Ide/TerminalIde.cs` | `UI/Screens/Ide/TerminalIde.cs` | **UI** |
| `Services/SpacedRepetitionEngine.cs` | `UI/Screens/Learn/SpacedRepetitionEngine.cs` | **UI** |
| `Services/StudySession.cs` | `UI/Screens/Learn/StudySession.cs` | **UI** |
| `Services/GuidedLearnFlow.cs` | `UI/Screens/Learn/GuidedLearnFlow.cs` | **UI** |
| `Views/Screens/Quizzes/CsharpQuiz.cs` | `UI/Screens/Quizzes/CsharpQuiz.cs` | **UI** |
| `Views/Screens/Quizzes/KanaQuiz.cs` | `UI/Screens/Quizzes/KanaQuiz.cs` | **UI** |
| `Views/Screens/Career/InterviewBank.cs` | `UI/Screens/Career/InterviewBank.cs` | **UI** |
| `Views/Screens/Dsa/AlgoVisualizer.cs` | `UI/Screens/Career/AlgoVisualizer.cs` | **UI** |

---

## 💻 5. Structuring as a Modern CLI Project

Currently, `AgyTuiApp` handles command-line arguments via manual array parsing and a massive `switch(alias)` block in **[Program.cs](file:///C:/Users/TruongNhon/Documents/PowerShell/csapp/AgyTuiApp/Program.cs)**. 

To turn this into a professional CLI tool, we can transition to **Spectre.Console.Cli** (already included in the Spectre.Console library).

### 🛠 Proposed Command Pattern Design

Under this pattern:
1. **Interactive TUI Mode**: Executed when the tool is run with zero arguments (launches the main dashboard).
2. **Subcommand Routing**: Executed when subcommands are provided (e.g. `agy disk` or `agy git diff`).
3. **No Giant Switch Blocks**: Each command is encapsulated into a separate testable class.

### Directory Structure Additions:
```
csapp/AgyTuiApp/
├── Core/
│   └── Commands/                        # Encapsulated command routes
│       ├── Base/                        # Shared settings and TUI router
│       ├── Git/                         # GitStatusCmd, GitDiffCmd
│       ├── System/                      # DiskUsageCmd, PublicIpCmd
│       ├── Workspace/                   # ProjNavigatorCmd, IdeCmd
│       └── Learn/                       # PomodoroSessionCmd, StatsCmd
```

### 📝 Example CLI Command Implementation

Here is how a single command like `disk` is refactored into a class:

```csharp
using System.ComponentModel;
using Spectre.Console.Cli;

namespace AgyTui.Core.Commands.System;

public sealed class DiskUsageCommand : Command<DiskUsageCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-j|--json")]
        [Description("Output raw disk metrics in JSON format")]
        public bool OutputJson { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        if (settings.OutputJson)
        {
            // Output computer-readable disk JSON data
            Console.WriteLine("[ { \"drive\": \"C:\", \"free_gb\": 142.5 } ]");
            return 0;
        }

        // Standard colored console output
        Infrastructure.Integrations.System.SystemHelper.ShowDiskSpace();
        return 0;
    }
}
```

### 🔗 Wiring up Program.cs via CommandApp

Using **Spectre.Console.Cli**'s `CommandApp`, the entry point is simplified to a declarative routing system:

```csharp
using Spectre.Console.Cli;
using AgyTui.Core.Commands.System;
using AgyTui.Core.Commands.Workspace;

public static class Program
{
    public static int Main(string[] args)
    {
        // 1. Direct interactive dashboard launch if no args
        if (args.Length == 0)
        {
            UI.Core.Navigation.CcNavigator.Run();
            return 0;
        }

        // 2. Command routing for CLI commands
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.SetApplicationName("agy");

            // Register system commands
            config.AddCommand<DiskUsageCommand>("disk")
                  .WithDescription("Show local drive capacities and partition health");

            // Register project commands
            config.AddCommand<WorkspaceNavigatorCommand>("proj")
                  .WithAlias("p")
                  .WithDescription("Select and open development project workspace");
        });

        return app.Run(args);
    }
}
```

### 🌟 Key Benefits of the CLI Refactoring:
1. **Self-Documenting Help Screens**: Automatically generates beautiful `--help` screens showing descriptions, commands, and parameter options.
2. **Type Safety & Flags**: Handles option parsing (e.g. `--json`, `-d "path"`) and type binding automatically.
3. **Decoupled Business Logic**: Testing a specific command only requires executing its class without loading the entire TUI render cycle.

---

## 🧪 6. Structuring the Test Project (`AgyTuiApp.Tests`)

To maintain clean architecture and ensure long-term testability, the test project **`AgyTuiApp.Tests`** should be structured to mirror the production codebase directory layout. This makes finding, writing, and executing tests highly predictable.

### Proposed Test Project Layout:

```
csapp/AgyTuiApp.Tests/
├── AgyTuiApp.Tests.csproj
├── Mocks/                              # Shared Test Fakes & Mock Providers
│   ├── FakeAccountRepository.cs         # In-memory account repo mock
│   ├── FakeStudyRepository.cs           # In-memory study repo mock
│   └── TestResources/                   # JSON/TSV mock raw files for tests
├── Unit/                               # Isolated Unit Tests (Mirroring production Core/Infra/UI)
│   ├── Core/
│   │   ├── Registries/
│   │   │   └── CommandRegistryTests.cs
│   │   └── Services/
│   │       ├── SpacedRepetitionTests.cs
│   │       └── WeakItemsQueueTests.cs
│   ├── Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── AccountServiceTests.cs
│   │   │   └── ConfigTests.cs
│   │   └── Common/
│   │       └── TtlCacheTests.cs
│   └── UI/
│       ├── Layouts/
│       │   └── FlatTreeRendererTests.cs
│       └── Components/
│           └── ScreenChromeTests.cs
└── Integration/                        # Integration Tests (collaborating parts, filesystem, CLI commands)
    ├── ResourceDiscoveryTests.cs       # Verifies TSV extractor/parser behaves correctly on actual files
    └── LearningDataTests.cs            # Verifies JSON writing and config saving on local disk
```

### 📈 Test File Relocation Mapping

Here is how the existing test files map to the new structured layout:

| Original Test File | Proposed Test File | Layer |
| :--- | :--- | :--- |
| `Unit/CommandRegistryTests.cs` | `Unit/Core/Registries/CommandRegistryTests.cs` | **Unit (Core)** |
| `Unit/SpacedRepetitionTests.cs` | `Unit/Core/Services/SpacedRepetitionTests.cs` | **Unit (Core)** |
| `Unit/WeakItemsQueueTests.cs` | `Unit/Core/Services/WeakItemsQueueTests.cs` | **Unit (Core)** |
| `Unit/AccountServiceTests.cs` | `Unit/Infrastructure/Persistence/AccountServiceTests.cs` | **Unit (Infra)** |
| `Unit/ConfigTests.cs` | `Unit/Infrastructure/Persistence/ConfigTests.cs` | **Unit (Infra)** |
| `Unit/ConfigServiceTests.cs` | `Unit/Infrastructure/Persistence/ConfigServiceTests.cs` | **Unit (Infra)** |
| `Unit/TtlCacheTests.cs` | `Unit/Infrastructure/Common/TtlCacheTests.cs` | **Unit (Infra)** |
| `Unit/FlatTreeRendererTests.cs` | `Unit/UI/Layouts/FlatTreeRendererTests.cs` | **Unit (UI)** |
| `Unit/ScreenChromeTests.cs` | `Unit/UI/Components/ScreenChromeTests.cs` | **Unit (UI)** |
| `Unit/TsvExtractorTests.cs` | `Integration/TsvExtractorTests.cs` | **Integration** |
| `Unit/LearningDataTests.cs` | `Integration/LearningDataTests.cs` | **Integration** |
| `Unit/QuotaMetricsTests.cs` | `Integration/QuotaMetricsTests.cs` | **Integration** |
| `Integration/ResourceDiscoveryTests.cs` | `Integration/ResourceDiscoveryTests.cs` | **Integration** |

