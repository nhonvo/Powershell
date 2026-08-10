# Console App & CLI Architecture Checklist (C# / AgyTui Pattern)

This checklist provides a standard, production-ready blueprint for building modern C# Console Apps, CLI utilities, and Terminal User Interfaces (TUI), modeled directly on the **[AgyTui](file:///C:/Users/TruongNhon/Documents/Powershell/csapp/AgyTui/AgyTui.csproj)** codebase architecture, the **[UI/Screens](file:///C:/Users/TruongNhon/Documents/Powershell/csapp/AgyTui/UI/Screens)** suite catalog, the **[CommandRegistry](file:///C:/Users/TruongNhon/Documents/Powershell/csapp/AgyTui/UI/Core/Registries/CommandRegistry.cs)** feature tree, and the **AGY Zero-Lag Footer-Title Stream Specification**.

---

## 1. Project Architecture & Framework Setup
- [x] **Target Modern .NET Runtime**: Use .NET 9.0 (`net9.0`) or higher for optimized native trim/single-file publish and performance.
- [x] **Strict Compiler Settings**: Enable `<ImplicitUsings>enable</ImplicitUsings>` and `<Nullable>enable</Nullable>` in `.csproj`.
- [x] **Central Package Management (CPM)**: Use `Directory.Packages.props` at solution root to standardize dependency versions (`Spectre.Console`, `Microsoft.Data.Sqlite`, `Microsoft.Extensions.DependencyInjection`, etc.).
- [x] **Clean Layered Architecture**:
  - `Domain/`: Core business models, context domains (e.g., `AccountContext`, `WorkspaceContext`, `LearnContext`), and domain exceptions.
  - `Infrastructure/`: Dependency Injection (`Di/`), Persistence (`Persistence/`), Registries (`Registries/`), Configuration, Logging, and Middleware.
  - `UI/`: Interactive CLI/TUI components, linear prompt renderers, and ANSI console layouts.
- [x] **Test Assembly Access**: Expose internal members to unit tests via `InternalsVisibleToAttribute` targeting `<_Parameter1>YourApp.Tests</_Parameter1>`.

---

## 2. Zero-Lag Scroll Performance & Footer-Title Architecture

> [!IMPORTANT]
> **Footer-Title & Zero-Lag Buffer**:
> 1. Keep top list output 100% header-free so users can drag-select and copy text natively from line 1.
> 2. Display the active **Screen Title & Breadcrumb in the Footer Bar** right above the input prompt.
> 3. Build the entire frame in memory using `StringBuilder` and flush in a **single `Console.Write` call** to completely eliminate scrolling lag.

### Zero-Lag Scroll Rendering Implementation Pattern:
```csharp
public static void RenderFrameZeroLag(string title, List<string> items, int selectedIdx)
{
    var sb = new StringBuilder(2048);
    
    // 1. Position cursor at home (0,0) without full screen clear
    sb.Append("\x1b[H");

    // 2. Render visible item lines
    for (int i = 0; i < items.Count; i++)
    {
        var pointer = (i == selectedIdx) ? "> " : "  ";
        sb.Append(pointer).Append(items[i]).Append("\x1b[K\n");
    }

    // 3. Render Footer Title & Action Bar at Bottom
    sb.Append("\x1b[K\n");
    sb.Append("Title: 🛸 ").Append(title).Append("\n");
    sb.Append("Nav: [1-9] Jump  │  ↑/↓ Move  │  Enter Select  │  / Search\x1b[K\n");
    sb.Append("Select option: ");

    // 4. Single-call atomic write to console stdout (Zero Redraw Lag)
    Console.Write(sb.ToString());
}
```

---

### 2.1 Exhaustive View Renderer & Layout Architecture Catalog

The system provides 6 standardized view rendering engines to handle all interactive console UI scenarios:

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                        AGY TUI VIEW RENDERER & LAYOUT CATALOG                          │
├──────────────────────┬────────────────────────┬───────────────────┬────────────────────┤
│ View Renderer Engine │ Primary Layout Pattern │ Target Scenarios  │ C# Source Class    │
├──────────────────────┼────────────────────────┼───────────────────┼────────────────────┤
│ ⚡ ZeroLagStreamList │ Single-Column Stream   │ Quick Select Menus│ ScreenChrome.cs    │
│                      │ Header-Free Buffer     │ Prompt Renderers  │ SimpleCliRenderer  │
├──────────────────────┼────────────────────────┼───────────────────┼────────────────────┤
│ 📐 ThreePaneRenderer │ Triple Column Split    │ Control Center    │ ThreePaneRenderer  │
│                      │ (Cat / Items / Details)│ Command Palette   │ MenuRendererBase   │
├──────────────────────┼────────────────────────┼───────────────────┼────────────────────┤
│ 🌳 FlatTreeRenderer  │ Hierarchical Tree      │ Project Switcher  │ FlatTreeRenderer.cs│
│                      │ Indented Expand/Collapse Workspace Tree   │ SubPageProjNav     │
├──────────────────────┼────────────────────────┼───────────────────┼────────────────────┤
│ 💻 DualPaneExplorer  │ Left Explorer Tree     │ Terminal IDE      │ TerminalIde.cs     │
│                      │ Right Code Viewport    │ Code & Symbol View│ CodeViewer.cs      │
├──────────────────────┼────────────────────────┼───────────────────┼────────────────────┤
│ 📑 LogStreamViewport │ Line Paged Stream      │ Application Logs  │ LogHelper.cs       │
│                      │ Auto-Follow & Live Tail│ Docker Logs       │ DockerClient.cs    │
├──────────────────────┼────────────────────────┼───────────────────┼────────────────────┤
│ 🎴 CardFrameEngine   │ Centered Panel Card    │ Flashcards, Quiz, │ FlashcardEngine.cs │
│                      │ Multiple Choice/Form   │ Interview, STAR   │ InterviewBank.cs   │
└──────────────────────┴────────────────────────┴───────────────────┴────────────────────┘
```

#### View Renderer Specifications:

1. **ZeroLagStreamList View Renderer (`ScreenChrome.cs` / `RenderFrameZeroLag`)**:
   - **Characteristics**: 100% header-free top list output for clean mouse text selection; Footer title bar with breadcrumbs and navigation hints right above the input prompt.
   - **Cursor Strategy**: Hides cursor via ANSI `\x1b[?25l` during render; uses home positioning `\x1b[H` without clearing screen to prevent flicker.

2. **ThreePaneRenderer View Engine (`ThreePaneRenderer.cs`)**:
   - **Characteristics**: Three column layout with Categories (Left), Items (Center), and Details/Help (Right).
   - **Search & Filter**: Real-time fuzzy filter buffer with `Ctrl+W` word deletion (`DeletePreviousWord`) and smooth mouse scroll wheel tracking.

3. **FlatTreeRenderer Tree View Engine (`FlatTreeRenderer.cs`)**:
   - **Characteristics**: Indented expandable/collapsible tree view for nested project workspaces and file directories.
   - **Interactions**: Arrow keys (`←` collapse, `→` expand, `Enter` activate).

4. **DualPaneExplorer View Engine (`TerminalIde.cs`)**:
   - **Characteristics**: Left-side tree menu + Right-side line-numbered code viewer viewport (`1: namespace AgyTui...`).
   - **Interactions**: `/` triggers in-memory workspace symbol search overlay.

5. **LogStreamViewport Log Engine (`LogHelper.cs`)**:
   - **Characteristics**: Stream viewer supporting `[f]` live follow, ANSI color-coded log levels (INFO=cyan, WARN=yellow, ERROR=red), and line truncation clearing `\x1b[K`.

6. **CardFrameEngine Study Card Renderer (`FlashcardEngine.cs` / `InterviewBank.cs`)**:
   - **Characteristics**: Centered rounded Spectre panel cards for SM-2 spaced repetition grades (1-5), STAR method forms, and algorithm array step visualizers.

---

## 3. Exhaustive Screen & Suite Architecture Catalog (`UI/Screens`)

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                        AGY TUI SCREEN SUITE CATALOG MATRIX                             │
├─────────────────┬──────────────────────┬──────────────────────────┬────────────────────┤
│ Suite Interface │ Implementation Class │ Feature Files            │ Key Actions & CRUD │
├─────────────────┼──────────────────────┼──────────────────────────┼────────────────────┤
│ 📖 ILearnSuite  │ LearnSuiteService    │ LearnRouter, Flashcard,  │ SM-2 Study Grade,  │
│                 │                      │ StudySession, StudyStats │ Streak, Pomodoro   │
├─────────────────┼──────────────────────┼──────────────────────────┼────────────────────┤
│ 💼 IScreenView  │ ProjectScreen (proj) │ SubPageProjNavigator     │ Flat tree expand,  │
│                 │ AccountScreen (agysw)│ SubPageAccountNavigator  │ Token Auth Guard,  │
│                 │ ThemeScreen (theme)  │ SubPageThemeNavigator    │ Posh theme apply,  │
│                 │ TopicScreen (topic)  │ SubPageTopicNavigator    │ Custom topic input │
├─────────────────┼──────────────────────┼──────────────────────────┼────────────────────┤
│ 🐙 IGitNexusSuit│ GitNexusSuiteService │ GitNexus, GitDiffViewer, │ Live Sync, gbr,    │
│                 │                      │ RepoGraph, GitClient     │ gcmt, gconflict    │
├─────────────────┼──────────────────────┼──────────────────────────┼────────────────────┤
│ 💻 IIdeSuite    │ IdeSuiteService      │ TerminalIde, CodeViewer, │ Dual pane explorer,│
│                 │                      │ SymbolSearch, IdeSearch  │ Symbol lookup /    │
├─────────────────┼──────────────────────┼──────────────────────────┼────────────────────┤
│ 🎯 Quizzes      │ Quizzes Directory    │ CsharpQuiz, KanaQuiz,    │ Multiple choice 1-4│
│                 │                      │ SnippetLibrary           │ Clipboard copy     │
├─────────────────┼──────────────────────┼──────────────────────────┼────────────────────┤
│ ⚡ ICareerSuite │ CareerSuiteService   │ InterviewBank,           │ STAR question card,│
│                 │                      │ AlgoVisualizer           │ Array mutation viz │
├─────────────────┼──────────────────────┼──────────────────────────┼────────────────────┤
│ ☁️ IAwsClient   │ AwsClient            │ AwsClient, LocalStack    │ S3, SQS, Lambda    │
├─────────────────┼──────────────────────┼──────────────────────────┼────────────────────┤
│ 📓 IObsidianBrid│ ObsidianBridge       │ ObsidianClient, Graph    │ Vault, Daily Note  │
├─────────────────┼──────────────────────┼──────────────────────────┼────────────────────┤
│ 🔒 IAgyVault    │ AgyVault             │ AgyVault, KeyringHelper  │ DPAPI Secret Store │
├─────────────────┼──────────────────────┼──────────────────────────┼────────────────────┤
│ 🌌 IDeckClient  │ AntigravityDeckClient│ AntigravityDeckClient    │ Node server, Tunnel│
├─────────────────┼──────────────────────┼──────────────────────────┼────────────────────┤
│ 🤖 IAiProvider  │ ClaudeProvider, etc. │ Claude, Hermes, OpenClaw │ Multi-LLM status   │
├─────────────────┼──────────────────────┼──────────────────────────┼────────────────────┤
│ 🔍 IAiScanner   │ AiProjectScanner     │ AiProjectScanner, Commit │ AST Scan, Auto Commit
└─────────────────┴──────────────────────┴──────────────────────────┴────────────────────┘
```

---

### 3.1 Exhaustive Feature-to-View Recommendation & Gap-Filling Map

Every feature across all 12 subsystems has a suggested UI view pattern, CLI keyword trigger, footer title format, primary user actions, and underlying C# logic file:

| Feature Subsystem | Trigger Keyword | Recommended UI View Pattern | Footer Title Format | Key Actions & Hotkeys | C# Logic File |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Project Scaffolder** | `scaffold` | `ZeroLagSingleColumnList` + `InputPrompt` | `🔨 Project Scaffolder > {Step}` | `[1-6]` Select Template, `Enter` Submit | `ProjectScaffolder.cs` |
| **Workspace Discovery** | `discover-workspaces` | `ZeroLagSingleColumnList` (Batch) | `💼 Workspace Manager > Auto-Discover` | `[1-N]` Register, `[a]` Register All | `WorkspaceRegistry.cs` |
| **Workspace Pruning** | `prune-workspaces` | `StatusCheckList` (Guard) | `💼 Workspace Manager > Prune Stale` | `[y]` Confirm, `Esc` Cancel | `WorkspaceRegistry.cs` |
| **Workspace Navigator** | `proj` / `cnav` | `FlatTreeExpandableList` | `💼 Workspace Manager > Navigator` | `↑/↓` Move, `Enter` Switch, `[a]` Add | `SubPageProjNavigator.cs` |
| **System Diagnostics** | `/log` / `log` | `PagedLogViewer` (Follow mode) | `📑 AgyTui System Diagnostic Log Viewer` | `↑/↓` Scroll, `[f]` Live Follow, `[c]` Clear | `LogHelper.cs` |
| **Docker Log Tailer** | `dlogsu` | `ContainerLogTailer` | `🐳 Docker Container Logs > {name}` | `↑/↓` Scroll, `[f]` Follow, `Esc` Exit | `DockerClient.cs` |
| **Add EF Migration** | `add-migration` | `InputAndExecuteView` | `🗄️ EF Core > Add Migration` | `[u]` Apply to DB Now, `Esc` Done | `SqliteMigrationEngine.cs` |
| **Update Database** | `update-db` | `MigrationApplyProgressView` | `🗄️ EF Core > Update Database` | `Enter` Return to Menu | `SqliteMigrationEngine.cs` |
| **Ollama Daemon Status**| `ollama-status` | `ServiceStatusDashboard` | `🤖 Ollama Daemon Status — ONLINE` | `[p]` Pull, `[b]` Benchmark, `[l]` Logs | `OllamaClient.cs` |
| **Ollama Models** | `ollama-models` | `ModelListWithMetadata` | `🤖 Ollama > Pulled Model Manager` | `[1-N]` Select, `[r]` Remove, `[c]` Copy | `OllamaClient.cs` |
| **Ollama Benchmark** | `ollama-benchmark` | `BenchmarkTableViewer` | `🤖 Ollama > Performance Benchmark` | `Enter` Return | `OllamaClient.cs` |
| **Terminal IDE Explorer**| `ide` | `DualPaneSplitExplorer` | `💻 Terminal IDE — {workspace_path}` | `↑/↓/←/→` Tree, `Enter` Open, `/` Search | `TerminalIde.cs` |
| **Workspace Symbols** | `symbol` | `LiveFilterSymbolList` | `💻 Terminal IDE > Workspace Symbol Search`| `[1-N]` Jump, Type filter, `Esc` Back | `SymbolSearch.cs` |
| **Pattern Search** | `ide-search` | `GrepMatchResultList` | `💻 Terminal IDE > Workspace Pattern Search`| `[1-N]` Open Line, `Esc` Back | `IdeFileSearchService.cs` |
| **Git Diff Viewer** | `ide-diff` | `ColorizedDiffPager` | `💻 Terminal IDE > Git Diff Viewer` | `↑/↓` Scroll, `[n]` Next File, `[p]` Prev | `GitDiffViewer.cs` |
| **Account Switcher** | `agysw` | `AccountListWithAuthBadge` | `💼 Account Manager > AGYSWITCH` | `↑/↓` Move, `Enter` Switch, `[l]` Login | `AccountScreen.cs` |
| **Theme Selector** | `theme` | `ThemePreviewList` | `🎨 System Settings > Theme Appearance` | `↑/↓` Move, `Enter` Apply, `/` Search | `ThemeScreen.cs` |
| **Topic Selector** | `topic` | `DomainTopicList` | `🎯 Learning Suite > AI Learning Topic` | `↑/↓` Move, `Enter` Select, `/` Search | `TopicScreen.cs` |
| **Master Learn Hub** | `learn auto` | `DomainSelectionHub` | `🎓 Antigravity Master Learning Hub` | `[1-6]` Select Domain, `Esc` Exit | `LearnRouter.cs` |
| **Flashcard Engine** | `flashcards` | `CardQuestionAnswerFlipView` | `🎴 Flashcard Engine > Grade Input` | `[1-5]` Recall Grade (SM-2 Algorithm) | `FlashcardEngine.cs` |
| **Study Statistics** | `study-stats` | `PomodoroStreakDashboard` | `📊 Study Console > Daily Dashboard` | `Enter` Return | `StudyConsoleView.cs` |
| **Vocab Drill Engine** | `vocab` | `TwoStepRevealWordView` | `📖 Vocab Drill > {Level}` | `Enter` Reveal, `[y/n]` Knew it? | `VocabDrill` in `InterviewBank.cs`|
| **Multi-Repo Sync** | `gbr` | `LiveTableDashboard` (30s refresh) | `🐙 Git Nexus > Multi-Workspace Live Sync`| `↑/↓` Scroll, `[r]` Refresh, `Esc` Exit | `GitNexus.cs` |
| **Commit Bar Chart** | `gstats` | `BarChartAndTreeVisualizer` | `📊 Git Nexus > Commit Frequency & Branch`| `Enter` Return | `GitNexusStats.cs` |
| **Repo Dependency Tree**| `repograph` | `DependencyTreeGraph` | `🕸️ Repo Graph > Multi-Project Dependency`| `Enter` Return | `RepoGraph.cs` |
| **C# Quiz Engine** | `quiz-cs` | `MultipleChoiceQuizCard` | `🎯 Interactive Quiz > C# Knowledge` | `[1-4]` Select Option, `Enter` Next | `CsharpQuiz.cs` |
| **Kana Practice Quiz** | `kana-quiz` | `SingleInputPromptQuiz` | `🌸 Japanese Suite > Kana Practice` | Type Romaji + `Enter`, `Esc` Abort | `KanaQuiz.cs` |
| **Snippet Library** | `snippets` | `SnippetCodeCardWithClipboard` | `⚡ Snippet Library > {Lang}/{Category}` | `[c]` Copy Clipboard, `[n]` Next | `SnippetLibrary` in `CsharpQuiz.cs`|
| **Interview Bank** | `interview` | `QuestionCardWithHints` | `💼 Career Suite > Interview Question` | `[n]` Next, `[h]` Hints, `[s]` STAR | `InterviewBank.cs` |
| **STAR Builder** | `star-builder` | `MultiStepFormCard` | `⭐ STAR Builder > Structured Response` | `[s]` Save to DB, `[e]` Edit | `StarBuilder` in `InterviewBank.cs`|
| **Algo Visualizer** | `algo-viz` | `StepByStepArrayFrameViewer` | `🧩 Algo Visualizer > {Algorithm} Trace` | `Enter` Next Step, `[a]` Auto-Play | `AlgoVisualizer.cs` |
| **AWS Cloud Inspector**| `aws-status` | `CloudResourceTabbedInspector` | `☁️ AWS Infrastructure > S3/SQS/Lambda` | `[1]` S3, `[2]` SQS, `[3]` Lambda, `[4]` SSM | `AwsClient.cs` |
| **Obsidian Bridge** | `obsidian` | `VaultActionHubAndNotePager` | `📓 Obsidian Bridge > Vault Management` | `[1-5]` Select Action, `Esc` Exit | `ObsidianClient.cs` |
| **DPAPI Secret Vault** | `agy-vault` | `MaskedSecretListWithKeyring` | `🔒 AGY Vault > Encrypted Credentials` | `[a]` Add, `[r]` Retrieve, `[d]` Delete | `AgyVault.cs` |
| **Antigravity Deck** | `agy-deck` | `ProcessManagerWithTunnelSummary` | `🌌 Antigravity Deck > Micro-Server` | `[1]` Setup, `[2]` Local, `[3]` Tunnel | `AntigravityDeckClient.cs` |
| **AI Multi-Providers** | `ai-providers` | `ProviderLatencyPingDashboard` | `🤖 AI Core > Multi-LLM Provider Monitor` | `[p]` Ping, `[c]` Config, `[m]` Set Model| `ClaudeProvider.cs` / `OllamaClient.cs`|
| **AI AST Scanner** | `ai-scan` | `AiCommitDiffReviewView` | `🤖 AI Assistant > AST Scan & Commit` | `[c]` Commit AI Msg, `[e]` Edit Msg | `AiProjectScanner.cs` |

## 4. Single-Column Layout Mockups by Feature Subsystem

Top list output is 100% header-free for clean mouse copying. **Screen Title & Navigation Hints are positioned in the Footer**:

### 4.1 🔨 Project Scaffolder Views (`scaffold`)

#### Step 1: Template Selection View
```text
  1) webapi      — .NET Web API with Controller / Minimal API setup
  2) console     — Modern .NET Console App with Dependency Injection
  3) react       — React + TypeScript Web App (scaffolds via Vite)
  4) blazorwasm  — Blazor WebAssembly Standalone Application
  5) classlib    — .NET Class Library for reusable NuGet packages
  6) worker      — Background Worker Service process template

Title: 🔨 Project Scaffolder > Select Boilerplate Template (scaffold)
Select template [1-6] or Esc Cancel: 1
```

#### Step 2: Project Name & Path Input Prompt
```text
  Selected Template: webapi (.NET Web API)

  Enter Project Name:
  > OrderService.API

  Enter Target Parent Directory (default: current workspace):
  > C:\Projects\Microservices

Title: 🔨 Project Scaffolder > Name & Target Directory Input
Input project details and press Enter (or Esc to abort): 
```

#### Step 3: Scaffolding Execution Progress
```text
  ✔ Scaffolding .NET Web API 'OrderService.API'...
  ✔ Initializing git repository...
  ✔ Registering workspace in AgyTui...

  Project created successfully at: C:\Projects\Microservices\OrderService.API

Title: 🔨 Project Scaffolder > Execution Complete
Press any key to open in Terminal IDE or navigate...
```

---

### 4.2 💼 Workspace Discovery & Pruning Views (`discover-workspaces` / `prune-workspaces`)

#### View 1: Auto-Discover Unregistered Projects (`discover-workspaces`)
```text
  Scanning container path 'C:\Projects'... Found 3 unregistered projects:

  [1] 📦 CustomerPortal (React / TypeScript — C:\Projects\CustomerPortal)
  [2] 📦 NotificationService (.NET Worker — C:\Projects\NotificationService)
  [3] 📦 PaymentGateway (.NET Web API — C:\Projects\PaymentGateway)

Title: 💼 Workspace Manager > Auto-Discover Projects (discover-workspaces)
Actions: [1-3] Register Selected Project  │  [a] Register All  │  Esc Cancel
Select option: 
```

#### View 2: Prune Stale Workspaces (`prune-workspaces`)
```text
  Checking registered workspace paths for missing directories...

  • [MISSING] Legacy-API (Path no longer exists: C:\OldProjects\Legacy-API)
  • [VALID]   Powershell (Path verified: C:\Users\TruongNhon\Documents\Powershell)

Title: 💼 Workspace Manager > Prune Stale Workspaces (prune-workspaces)
Actions: [y] Confirm Prune Missing Paths  │  Esc Keep All
Select option: y
```

---

### 4.3 📑 System Diagnostic & Container Log Viewers (`dlogsu` / `ollama-logs` / `log`)

#### View 1: Control Center Diagnostic Log Viewer (`/log` / `LogHelper`)
```text
  [2026-08-11 00:00:01] INFO  [Bootstrapper] ServiceProvider initialized in 42ms.
  [2026-08-11 00:00:02] DEBUG [Config] Loaded profile.config.json. UI.Mode = "simple-cli".
  [2026-08-11 00:02:15] WARN  [AccountStore] Account 'work-prod' token expired.
  [2026-08-11 00:05:00] ERROR [GitClient] Command 'git fetch' returned exit code 128 (Network timeout).

Title: 📑 AgyTui System Diagnostic Log Viewer (logs/app.log)
Nav: ↑/↓ Scroll Logs  │  [f] Follow Live Logs  │  [c] Clear Log File  │  Esc Back
```

#### View 2: Docker Container Log Tailer (`dlogsu`)
```text
  [Container: localstack | http://localhost:4566]
  2026-08-11 00:01:10 INFO  Ready. Available services: s3, sqs, ssm, dynamodb, lambda.
  2026-08-11 00:03:00 INFO  POST / HTTP/1.1 200 - S3 ListBuckets

Title: 🐳 Docker Container Logs > localstack (dlogsu)
Nav: ↑/↓ Scroll Logs  │  [f] Follow  │  Esc Back
```

---

### 4.4 🗄️ EF Core Database Migration & SQLite State Views (`add-migration` / `update-db`)

#### View 1: Add EF Core Migration (`add-migration` / `da`)
```text
  Target DbContext: ApplicationDbContext
  Enter Migration Name:
  > AddUserPreferencesTable

  Executing `dotnet ef migrations add AddUserPreferencesTable`...
  ✔ Migration '20260811000600_AddUserPreferencesTable' generated in ./Migrations.

Title: 🗄️ EF Core > Add Migration (add-migration)
Actions: [u] Apply Migration to Database Now  │  Esc Done
```

#### View 2: Update Database Schema (`update-db` / `du`)
```text
  Applying pending migrations to local SQLite database...

  • Applying migration '20260811000600_AddUserPreferencesTable'...
  ✔ Database schema updated successfully.

Title: 🗄️ EF Core > Update Database (update-db)
Press any key to return...
```

---

### 4.5 🤖 Ollama Feature Suite Views

#### View 1: Ollama Daemon Status (`ollama-status`)
```text
  • [1] llama3.2:latest (3.8 GB · Q4_K_M)
  • [2] hermes3:8b (4.7 GB · Q4_K_M)
  • [3] codex-local:latest (6.1 GB · Q5_K_M)

Title: 🤖 Ollama Daemon Status — ONLINE (http://localhost:11434)
Actions: [p] Pull Model  │  [b] Benchmark Models  │  [l] View Server Logs  │  [m] Manage
Select option: 
```

#### View 2: Ollama Interactive Model Manager (`ollama-models`)
```text
  1) 🤖 llama3.2:latest — 3.8 GB (Family: llama · Quant: Q4_K_M)
  2) 🤖 hermes3:8b — 4.7 GB (Family: llama · Quant: Q4_K_M)
  3) 🤖 codex-local:latest — 6.1 GB (Family: qwen · Quant: Q5_K_M)

Title: 🤖 Ollama > Pulled Model Manager
Actions: [1-3] Select Model  │  [r] Remove Model  │  [c] Copy Model Tag  │  Esc Back
Select model: 1
```

#### View 3: Ollama Model Benchmark Evaluator (`ollama-benchmark`)
```text
  Model Benchmark Results (Prompt: 512 tokens / Generation: 128 tokens):

  • llama3.2:latest   │ Eval Speed: 42.8 tokens/sec │ Warmup: 110ms
  • hermes3:8b        │ Eval Speed: 31.4 tokens/sec │ Warmup: 180ms
  • codex-local:latest│ Eval Speed: 24.1 tokens/sec │ Warmup: 240ms

Title: 🤖 Ollama > Performance Benchmark Evaluator
Press any key to return to model manager...
```

---

### 4.6 💻 Terminal IDE Feature Suite Views

#### View 4: Terminal IDE Main Explorer View (`ide`)
```text
  EXPLORER (Left Pane)           │ FILE VIEWPORT: Program.cs (Right Pane)
  ▼ AgyTui                       │ 1: namespace AgyTui;
    ▶ Domain                     │ 2: public static class Program
    ▶ Infrastructure             │ 3: {
    ▼ UI                         │ 4:     public static int Main(string[] args)
      ▼ Core                     │ 5:     {
        ▼ Layouts                │ 6:         return RunAppZeroLag(args);
          📄 SimpleCliRenderer.cs│ 7:     }
      📄 Program.cs              │ 8: }

Title: 💻 Terminal IDE — C:\Users\TruongNhon\Documents\Powershell\csapp\AgyTui
Nav: ↑/↓/←/→ File Tree  │  Enter Open  │  / Symbol Search  │  Esc Close IDE
```

#### View 5: Workspace Symbol Indexer View (`symbol` / `SymbolSearch.cs`)
```text
  Workspace Code Symbol Index:

  1) 🔧 Program.Main(string[] args) — Program.cs:L8
  2) 🔧 SimpleCliMenuRenderer.Run(MenuNode root) — SimpleCliMenuRenderer.cs:L15
  3) 🔧 CommandRegistry.AssertSwitchCases() — CommandRegistry.cs:L661
  4) 🔧 Bootstrapper.Initialize() — Bootstrapper.cs:L12

Title: 💻 Terminal IDE > Workspace Symbol Search (/symbol)
Filter: Main_
Nav: [1-4] Jump to Code Symbol  │  Type to filter  │  Esc Cancel
```

#### View 6: Workspace File Pattern Search View (`ide-search` / `IdeSearchService.cs`)
```text
  Search Pattern Match Results for 'RenderFrameZeroLag':

  1) UI/Core/Layouts/SimpleCliMenuRenderer.cs:L52 — ScreenChrome.RenderFrameZeroLag(...)
  2) UI/Core/Layouts/MenuRendererBase.cs:L88 — public static void RenderFrameZeroLag(...)
  3) UI/Screens/Workspace/ProjectScreen.cs:L31 — RenderFrameZeroLag(() => ...)

Title: 💻 Terminal IDE > Workspace Pattern Search (ide-search)
Nav: [1-3] Open File at Line  │  Esc Cancel
```

#### View 7: Colorized Git Diff Viewer (`ide-diff` / `GitDiffViewer.cs`)
```text
  Modified File (1/3): csapp/AgyTui/Program.cs
  @@ -8,3 +8,3 @@
  - return RunApp(args);
  + return RunAppZeroLag(args);

Title: 💻 Terminal IDE > Git Diff Viewer (ide-diff)
Nav: ↑/↓ Scroll Diff  │  [n] Next Modified File  │  [p] Previous File  │  Esc Exit
```

---

### 4.7 💼 Workspace & System Customization Screens (`IScreenView` suite)

#### View 1: Registered Workspace Navigator View (`proj` / `cnav` / `SubPageProjNavigator.cs`)
```text
  1) 📁 Powershell                  — C:\Users\TruongNhon\Documents\Powershell (Active)
  2) 📁 OrderService.API            — C:\Projects\Microservices\OrderService.API
  3) 📁 CustomerPortal              — C:\Projects\CustomerPortal
  4) 📁 NotificationService         — C:\Projects\NotificationService

Title: 💼 Workspace Manager > Registered Workspace Navigator (proj)
Nav: ↑/↓ Navigate  │  Enter Switch Workspace  │  [a] Add  │  [d] Delete  │  Esc Back
Select option: 1
```

#### View 2: AGY Account Switcher & Auth Guard View (`agysw` / `AccountScreen.cs`)
```text
> 1) work-prod (work@corp.com) (✔ Logged In · Key: Key-9f8a) (Active)
  2) dev-personal (dev@personal.io) (✔ Logged In · Key: Key-1b4c)
  3) sandbox-test (test@sandbox.net) (✘ Logged Out · Key: None)

Title: 💼 Account Manager > AGYSWITCH Account Manager (agysw)
Nav: ↑/↓ Navigate  │  Enter Switch  │  [a] Create  │  [l] Auth Login  │  [d] Delete  │  [o] Logout
Select option: 
```

#### View 3: Visual Theme Selector View (`theme` / `ThemeScreen.cs`)
```text
> 1) Catppuccin Mocha (Active)
  2) Tokyo Night
  3) One Dark Pro
  4) Dracula
  5) Gruvbox Dark

Title: 🎨 System Settings > Theme & Visual Appearance (theme)
Nav: ↑/↓ Navigate  │  Enter Apply Theme  │  / Search  │  Esc Cancel
Select theme: 
```

#### View 4: AI Learning Topic Selector View (`topic` / `TopicScreen.cs`)
```text
> 1) jp          — Japanese Language Suite (JLPT, Kana, Grammar)
  2) en          — English Vocabulary & Grammar Drill
  3) cs          — C# & .NET Masterclass
  4) dsa         — Data Structures & Algorithms
  5) interview   — Behavioral & Technical Interview Questions
  6) [Type Custom Topic...]

Title: 🎯 Learning Suite > AI Learning & Topic Selector (topic)
Nav: ↑/↓ Navigate  │  Enter Select Topic  │  / Search  │  Esc Cancel
Select topic [1-6]: 
```

---

### 4.8 🎓 Master Learning Suite Views (`learn` / `ILearnSuite`)

#### View 1: Antigravity Master Learning Suite Hub (`learn auto` / `LearnRouter.cs`)
```text
  1) 🎌 Japanese Language Suite (Kana, Kanji, JLPT)
  2) 📖 English & Vocabulary (Vocab Drill, Word of Day, Flashcards)
  3) 💻 C# & .NET Masterclass (Quiz, Snippets, Cheat Sheets)
  4) 🧩 DSA & System Architecture (Algo Visualizer, Big-O, Tracker)
  5) 💼 Career & Technical Interview (Questions, STAR Builder, Mock)
  6) 📊 Progress & Spaced Repetition Queue

Title: 🎓 Antigravity Master Learning Suite Hub (learn)
Nav: [1-6] Launch Domain Suite  │  ↑/↓ Navigate  │  Enter Select  │  Esc Exit
Select domain: 
```

#### View 2: Spaced Repetition Flashcard Engine (`flashcards` / `FlashcardEngine.cs`)
```text
  CARD 3 / 15  │ Deck: C# Memory Management & GC Internals
  ────────────────────────────────────────────────────────────────────────────
  Q: What is the primary difference between Gen 0, Gen 1, and Gen 2 in .NET GC?

  A: Gen 0 holds short-lived objects (e.g. temporary variables); Gen 1 serves as 
     a buffer between short and long-lived objects; Gen 2 holds long-lived objects 
     (e.g. static data, singletons) and LOH (Large Object Heap).

  Recall Evaluation Grade:
  [1] Blackout / Failed  │  [2] Wrong / Hard  │  [3] Good / Recalled  │  [4] Perfect  │  [5] Easy

Title: 🎴 Flashcard Engine > Spaced Repetition Grade Input (SM-2 Algorithm)
Grade card [1-5] or Esc Exit: 
```

#### View 3: Study Session & Pomodoro Statistics View (`study-stats` / `StudyConsoleView.cs`)
```text
  TODAY'S STUDY SESSION SUMMARY
  ────────────────────────────────────────────────────────────────────────────
  • Total Time Focused  : 45 mins (2 Pomodoro cycles)
  • Flashcards Reviewed : 32 cards (28 correct · 87.5% retention)
  • Weak Queue Items    : Span<T> vs Memory<T>, LOH Compaction
  • Current Streak      : 🔥 7 Days Streak

Title: 📊 Study Console > Daily Progress & Spaced Repetition Dashboard
Press any key to return to Master Learning Hub...
```

---

### 4.9 🐙 Git Nexus Multi-Repo Suite Views (`git-nexus` / `IGitNexusSuite`)

#### View 1: Live Multi-Repo Status Dashboard (`gbr` / `GitNexus.cs`)
```text
  Repo                 Branch            Sync         Dirty    Last Commit
  ────────────────────────────────────────────────────────────────────────────
  Powershell           main              [green]sync[/]         [dim]0[/]      a1b2c3d Fix TUI scroll zero-lag
  OrderService.API     feature/auth      [yellow]↑2[/]         [yellow]3[/]      f9e8d7c Add JWT middleware
  CustomerPortal       main              [cyan]↓1[/]         [dim]0[/]      b4c5d6e Bump Vite to v5.2

Title: 🐙 Git Nexus > Multi-Workspace Live Sync Dashboard (gbr)
Nav: Auto-refreshes (30s)  │  ↑/↓ Scroll  │  [r] Refresh Now  │  Esc Exit Dashboard
```

#### View 2: Commit Frequency Bar Chart & Branch Tree (`gstats` / `GitNexusStats.cs`)
```text
  COMMITS THIS WEEK BY REPOSITORY:
  Powershell       ██████████████████████████████ (18 commits)
  OrderService.API ██████████████ (9 commits)
  CustomerPortal   ██████ (4 commits)

  BRANCH STRUCTURE:
  ▼ Powershell
    * main
  ▼ OrderService.API
    * feature/auth
      main

Title: 📊 Git Nexus > Commit Frequency & Branch Structure Visualizer
Press any key to return to Git Nexus...
```

#### View 3: Workspace Project Dependency Tree View (`repograph` / `RepoGraph.cs`)
```text
  WORKSPACES DEPENDENCY GRAPH:
  ▼ OrderService.API (csproj)
    → OrderService.Domain
    → OrderService.Infrastructure
  ▼ CustomerPortal (npm)
    → react
    → typescript

Title: 🕸️ Repo Graph > Multi-Project Dependency Analyzer
Press any key to return...
```

---

### 4.10 🎯 Interactive Quizzes & Dev Tools Views (`CsharpQuiz` / `KanaQuiz` / `SnippetLibrary`)

#### View 1: C# & .NET Interactive Knowledge Quiz (`quiz-cs` / `CsharpQuiz.cs`)
```text
  QUESTION 4 / 10  │ Topic: C# Memory Management & Performance
  ────────────────────────────────────────────────────────────────────────────
  Q: Which keyword prevents a struct from being copied when passed as a method parameter?

  1) readonly ref
  2) in
  3) out
  4) static

  ✔ Correct! 'in' passes a struct by read-only reference, avoiding expensive copies.

Title: 🎯 Interactive Quiz > C# & .NET Knowledge Assessment
Actions: [1-4] Select Option  │  Enter Next Question  │  Esc Exit
```

#### View 2: Japanese Hiragana & Katakana Script Quiz (`kana-quiz` / `KanaQuiz.cs`)
```text
  SCORE: 12 / 12  │ Streak: 🔥 12  │ Script: Hiragana Drill
  ────────────────────────────────────────────────────────────────────────────
  Prompt Character:
                            き ょ う

  Enter Romaji equivalent:
  > kyou

  ✔ Perfect! きょ う = kyou (Today)

Title: 🌸 Japanese Suite > Kana & Pronunciation Practice Quiz
Type answer and press Enter (or Esc to abort): 
```

#### View 3: Developer Code Snippet Inspector (`snippets` / `SnippetLibrary.cs`)
```text
  C# SNIPPET: High-Performance Zero-Allocation Span Splitter
  Category: Performance · Difficulty: 3
  ────────────────────────────────────────────────────────────────────────────
  public static void SplitSpan(ReadOnlySpan<char> source, char delimiter)
  {
      int index;
      while ((index = source.IndexOf(delimiter)) != -1)
      {
          var segment = source.Slice(0, index);
          // Process segment without heap allocation
          source = source.Slice(index + 1);
      }
  }

  Explanation: Uses ReadOnlySpan<char> to parse strings without GC allocations.
  Use case: Log parsing and high-throughput network packet header parsing.

Title: ⚡ Snippet Library > C# / Performance / SpanSplitter
Actions: [c] Copy Code to Clipboard  │  [n] Next Snippet  │  Esc Back
Select action: c
```

---

### 4.11 ⚡ Career & Algorithm Visualizer Suite Views (`career` / `ICareerSuite`)

#### View 1: Technical & Behavioral Interview Question Bank (`interview` / `InterviewBank.cs`)
```text
  QUESTION ID: int_042  │ Category: System Design · Difficulty: Hard
  ────────────────────────────────────────────────────────────────────────────
  Q: Design a distributed rate limiter that handles 100,000 requests/sec with low latency.

  Format: System Design Architecture Diagram & Trade-offs Discussion
  
  Hints:
   • Consider Token Bucket vs Leaky Bucket vs Sliding Window Counter.
   • Use Redis with Lua scripts or local in-memory cache + sync.

  Companies: Google, AWS, Microsoft, Uber

Title: 💼 Career Suite > Technical Interview Question Card
Actions: [n] Next Question  │  [h] Toggle Hints  │  [s] Build STAR Answer  │  Esc Back
Select action: 
```

#### View 2: STAR Method Answer Construction View (`star-builder` / `StarBuilder.cs`)
```text
  INTERVIEW QUESTION: Describe a time you resolved a major production database bottleneck.
  ────────────────────────────────────────────────────────────────────────────
  S (Situation) : Order API response latency spiked to 4.5s during Black Friday sales peak.
  T (Task)      : Identify root cause and optimize query execution time to under 100ms.
  A (Action)    : Analyzed SQLite execution plan, added missing composite index on (UserId, CreatedAt),
                  and introduced in-memory LRU caching for hot account profiles.
  R (Result)    : P99 latency dropped from 4.5s to 24ms, CPU utilization decreased by 65%.
  Metric        : 99.4% reduction in query latency, 0 dropped transactions.

Title: ⭐ STAR Builder > Verified Structured Response Card
Actions: [s] Save STAR Answer to DB  │  [e] Edit Response  │  Esc Cancel
Select option: s
```

#### View 3: Algorithm Step-by-Step Array Mutation Visualizer (`algo-viz` / `AlgoVisualizer.cs`)
```text
  ALGORITHM VISUALIZER: Bubble Sort — Step 14 / 28
  ────────────────────────────────────────────────────────────────────────────
  Step 14 · Comparisons: 18 · Swaps: 6

  ┌────┬────┬────┬────┬────┬────┬────┬────┐
  │ 3  │ 7  │ 12 │ 15 │ 19 │ 22 │ 41 │ 88 │
  └────┴────┴────┴────┴────┴────┴────┴────┘
             ↑    ↑
      Comparing indices 2–3 ([12] vs [15]) — No swap needed

Title: 🧩 Algo Visualizer > Bubble Sort Interactive Trace
Actions: Enter Next Step  │  [a] Auto-Play (500ms)  │  Esc Stop Trace
```

---

### 4.12 🏗️ Infrastructure & Integration Subsystem Views

#### View 1: AWS Cloud & LocalStack Integration Inspector (`aws-status` / `AwsClient.cs`)
```text
  AWS IDENTITY: arn:aws:iam::123456789012:user/developer (Region: us-east-1)
  LOCALSTACK ENDPOINT: http://localhost:4566 (ONLINE)

  • S3 Buckets      : s3://app-assets-prod, s3://user-backups-localstack
  • SQS Queues      : https://sqs.us-east-1.amazonaws.com/123456789012/order-events
  • Lambda Functions: ProcessOrderFunction, GeneratePdfReportFunction
  • SSM Parameters  : /app/prod/db_connection_string, /app/prod/api_key

Title: ☁️ AWS Infrastructure > S3 / SQS / Lambda / LocalStack Inspector (aws-status)
Actions: [1] S3 Buckets  │  [2] SQS Queues  │  [3] Lambda  │  [4] SSM Params  │  Esc Back
Select option: 
```

#### View 2: Obsidian Vault & Knowledge Sync Bridge (`obsidian` / `ObsidianClient.cs`)
```text
  ACTIVE VAULT: C:\Users\TruongNhon\Documents\ObsidianVault (Verified · 142 notes)

  1) 🔍 Search Vault Notes (142 markdown files)
  2) 🏷️ Browse Notes by Tag (#csharp, #dotnet, #system-design, #japanese)
  3) 📅 Today's Daily Note (2026-08-11.md)
  4) 🕸️ Interactive Note Link Graph Visualizer (ObsidianGraph.cs)
  5) ⚙️ Reconfigure Vault Directory Path

Title: 📓 Obsidian Bridge > Vault Management & Daily Note Sync (obsidian)
Nav: [1-5] Select Action  │  ↑/↓ Navigate  │  Enter Execute  │  Esc Exit
Select option: 
```

#### View 3: AGY DPAPI Encrypted Secret Vault (`agy-vault` / `AgyVault.cs`)
```text
  STORAGE ENGINE: Windows DPAPI (DataProtectionScope.CurrentUser) + Windows Keyring

  • SECRET_OPENAI_API_KEY      [Protected · Base64 Signature: a8f9...4b1c]
  • SECRET_DATABASE_PASSPHRASE [Protected · Base64 Signature: c1d2...9e8f]
  • SECRET_GITHUB_PAT          [Protected · Base64 Signature: e5f6...7a8b]

Title: 🔒 AGY Vault > Encrypted Credentials & Secret Store (agy-vault)
Actions: [a] Add Secret  │  [r] Retrieve Secret  │  [d] Delete Secret  │  Esc Back
Select option: 
```

#### View 4: Antigravity Deck Micro-Server & Tunnel Visualizer (`agy-deck` / `AntigravityDeckClient.cs`)
```text
  DECK APP PATH: C:\Users\TruongNhon\.gemini\antigravity-deck
  PORT BINDINGS: FE: 3000 (React)  │  BE: 3500 (Node.js)  │  Deck: 18789

  1) ⚡ Setup & Install Dependencies (npm run setup)
  2) 🚀 Launch Local Dev Server (http://127.0.0.1:18789)
  3) 🌐 Launch Cloudflare Tunnel (https://antigravity-deck.trycloudflare.com)
  4) 🛑 Kill Active Deck Ports (9808, 9807, 3500, 3000, 18789)

Title: 🌌 Antigravity Deck > Micro-Server Process Controller (agy-deck)
Nav: [1-4] Select Action  │  Esc Back
Select option: 
```

#### View 5: Multi-AI LLM Provider Connectivity Status (`ai-providers` / `ClaudeProvider.cs` / `HermesProvider.cs`)
```text
  CONNECTED PROVIDERS:
  • 🟢 Claude 3.5 Sonnet   │ Online · API: Anthropic v1 · Latency: 140ms
  • 🟢 Hermes 3 (Local)    │ Online · Endpoint: http://localhost:11434 · Latency: 18ms
  • 🟢 OpenClaw Engine     │ Online · Local IPC Socket · Latency: 8ms
  • 🔴 GPT-4o               │ Offline (Invalid API Key in profile.config.json)

Title: 🤖 AI Core > Multi-LLM Provider Status & Latency Monitor (ai-providers)
Actions: [p] Test Ping  │  [c] Configure API Keys  │  [m] Set Primary Model  │  Esc Back
Select option: 
```

#### View 6: AI Codebase Scanner & Automated Commit Generator (`ai-scan` / `AiProjectScanner.cs`)
```text
  ANALYZING WORKSPACE: C:\Users\TruongNhon\Documents\Powershell\csapp\AgyTui
  Modified Files (3): Program.cs, AgyVault.cs, AwsClient.cs

  AI SUGGESTED COMMIT MESSAGE:
  "feat(infrastructure): Add AWS LocalStack client, Obsidian vault sync, and DPAPI secret vault"

Title: 🤖 AI Assistant > Codebase AST Scanner & Commit Generator (gcmt-ai)
Actions: [c] Commit with AI Message  │  [e] Edit Message  │  Esc Cancel
Select action: c
```

---

## 5. Keybinding & Interaction Matrix

| Action | Primary Key | Alternative Hotkeys | Behavioral Description |
| :--- | :---: | :---: | :--- |
| **Quick Select** | `1` .. `9` | N/A | Types option index number into linear prompt |
| **Back / Exit** | `0` | `Esc`, `q`, `b` | Returns to parent menu level or exits sub-screen |
| **Navigate Up** | `↑` | `k` | Moves selection index up in memory buffer |
| **Navigate Down** | `↓` | `j` | Moves selection index down in memory buffer |
| **Select / Execute**| `Enter` | `Space` | Activates selected option or launches child view |
| **Global Search** | `/` | `Ctrl+F` | Enters single-column live search mode |

---

## 6. Console UX & Signal Safety
- [x] **UTF-8 Console Encoding**: Set explicit console encodings at launch (`Console.OutputEncoding = Encoding.UTF8`).
- [x] **SIGINT / SIGTERM Interception**: Register `Console.CancelKeyPress` to gracefully flush SQLite transactions without leaving orphan locks.
