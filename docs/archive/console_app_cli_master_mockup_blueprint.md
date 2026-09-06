# 🛸 Console App & TUI Architecture Master Blueprint & Complete Mockup Catalog

**Generated At:** 2026-08-11  
**Project Name:** `AgyTui` (Antigravity Terminal User Interface CLI)  
**Target Framework:** .NET 9.0 / C# 13  
**Status:** Exhaustive 39-View ASCII Mockup Catalog & Complete Architectural Specification  

---

## 1. 🏛️ Project Architecture & Framework Setup

- [x] **Target Modern .NET Runtime**: Use .NET 9.0 (`net9.0`) or higher for optimized native trim/single-file publish and performance.
- [x] **Strict Compiler Settings**: Enable `<ImplicitUsings>enable</ImplicitUsings>` and `<Nullable>enable</Nullable>` in `.csproj`.
- [x] **Central Package Management (CPM)**: Use `Directory.Packages.props` at solution root to standardize dependency versions (`Spectre.Console`, `Microsoft.Data.Sqlite`, `Microsoft.Extensions.DependencyInjection`, etc.).
- [x] **Clean Layered Architecture**:
  - `Domain/`: Core business models, context domains (`AccountContext`, `WorkspaceContext`, `LearnContext`), and domain exceptions.
  - `Infrastructure/`: Dependency Injection (`Di/`), Persistence (`Persistence/`), Configuration, Logging, and Middleware.
  - `UI/Core/`: Reusable presentation engines, zero-lag buffers, viewports, command dispatchers, and state containers (`Abstractions/`, `Commands/`, `Components/`, `Layouts/`, `Navigation/`, `State/`).
  - `UI/Screens/`: 12 standardized feature screen view categories (`*Screen.cs` views, `Helpers/`, `Navigators/`).

---

## 2. ⚡ Zero-Lag Scroll Performance & Footer-Title Architecture

> [!IMPORTANT]
> **Footer-Title & Zero-Lag Buffer Specification**:
> 1. Keep top list output 100% header-free so users can drag-select and copy text natively starting from line 1.
> 2. Display the active **Screen Title & Breadcrumb in the Footer Bar** right above the input prompt.
> 3. Build the entire frame in memory using `StringBuilder` or Spectre Grid and flush in a **single ANSI write call (`\x1b[H`)** to completely eliminate redraw lag.

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

## 3. 📐 Exhaustive View Renderer & Layout Catalog (6 Core Engines)

```text
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                        AGY TUI VIEW RENDERER & LAYOUT CATALOG                          │
├──────────────────────┬────────────────────────┬───────────────────┬────────────────────┤
│ View Renderer Engine │ Primary Layout Pattern │ Target Scenarios  │ C# Source Class    │
├──────────────────────┼────────────────────────┼───────────────────┼────────────────────┤
│ ⚡ ZeroLagStreamList │ Single-Column Stream   │ Quick Select Menus│ ScreenChrome.cs    │
│                      │ Header-Free Buffer     │ Prompt Renderers  │ ZeroLagStreamList  │
├──────────────────────┼────────────────────────┼───────────────────┼────────────────────┤
│ 📐 ThreePaneRenderer │ Triple Column Split    │ Control Center    │ ThreePaneRenderer  │
│                      │ (Cat / Items / Details)│ Command Palette   │ MenuNode.cs        │
├──────────────────────┼────────────────────────┼───────────────────┼────────────────────┤
│ 🌳 FlatTreeRenderer  │ Hierarchical Tree      │ Project Switcher  │ FlatTreeRenderer.cs│
│                      │ Indented Expand/Collapse Workspace Tree   │ SubPageProjNav     │
├──────────────────────┼────────────────────────┼───────────────────┼────────────────────┤
│ 💻 DualPaneExplorer  │ Left Explorer Tree     │ Terminal IDE      │ DualPaneExplorer   │
│                      │ Right Code Viewport    │ Code & Symbol View│ TerminalIde.cs     │
├──────────────────────┼────────────────────────┼───────────────────┼────────────────────┤
│ 📑 LogStreamViewport │ Line Paged Stream      │ Application Logs  │ LogStreamViewport  │
│                      │ Auto-Follow & Live Tail│ Docker Logs       │ LogHelper.cs       │
├──────────────────────┼────────────────────────┼───────────────────┼────────────────────┤
│ 🎴 CardFrameEngine   │ Centered Panel Card    │ Flashcards, Quiz, │ CardFrameEngine    │
│                      │ Multiple Choice/Form   │ Interview, STAR   │ FlashcardEngine.cs │
└──────────────────────┴────────────────────────┴───────────────────┴────────────────────┘
```

---

## 4. 🌳 Global Flat Tree Control Center UI Mockups

### 4.1 Level 1: Unexpanded Root Category View (Default Control Center Launch)

```text
> [+] 📂 Favorites                                                                                            
  [+] 📁 Workspace & Dev (cnav / proj)                                                                        
  [+] 🤖 AI Agent & Ollama (cai / ollama)                                                                     
  [+] 👤 AGY Account Switcher (agyswitch / account)                                                           
  [+] 📚 Learn & Study Suite (learn / flashcards)                                                             
  [+] 🎯 Quizzes & Developer Snippets (quiz / snippets)                                                       
  [+] ⚡ Career & Algo Visualizer (career / interview)                                                        
  [+] ☁️ Infrastructure & Vault (aws / obsidian / agy-vault)                                                
  [+] 🎨 Appearance & Theme Settings (theme / topic)                                                          
  [+] 🌐 System & Network Diagnostics (csys / logs)                                                           
  [+] 🛸 Help, Manuals & Docs (help)                                                                          
  ────────────────────────────────────────────────────────────────────────────────────────
  [Exit] Exit Control Center                                                                                  

Title: 🛸 AgyTui Control Center — Global Navigation (cc)
Filter: [ / ] type to filter...  │  Active: 📁 Powershell (C:\Users\TruongNhon\Documents\Powershell)
Nav: [↑/↓ j/k] Move  │  [←/→] Collapse/Expand  │  [Enter] Execute  │  [/] Filter  │  [Esc/q] Exit
Shell Shortcuts: cnav (Workspace) · cai (AI) · gbr (Git) · learn (Learn) · ide (IDE) · agysw (Account)
Select command or type alias: 
```

---

### 4.2 Level 2: Expanded `Workspace & Dev` Category View

```text
  [+] 📂 Favorites                                                                                            
> [-] 📁 Workspace & Dev (cnav / proj)                                                                        
  │  ├── 💻 Terminal IDE Explorer (ide)             — Dual-pane file tree & code viewport                     
  │  ├── 🐙 Git Nexus Live Dashboard (gbr)          — Multi-repo live sync & status monitor                   
  │  ├── 🔨 Project Scaffolder (scaffold)           — Scaffold Web API, Console, React, Worker                
  │  ├── 🔍 Workspace Symbol Indexer (symbol)       — In-memory symbol search overlay                        
  │  ├── 🔎 Pattern Search Engine (ide-search)      — Grep match result list viewer                           
  │  └── 📁 Discover & Register Projects (discover) — Auto-scan unregistered local repos                    
  [+] 🤖 AI Agent & Ollama (cai / ollama)                                                                     
  [+] 👤 AGY Account Switcher (agyswitch / account)                                                           
  [+] 📚 Learn & Study Suite (learn / flashcards)                                                             
  [+] 🎯 Quizzes & Developer Snippets (quiz / snippets)                                                       
  [+] ⚡ Career & Algo Visualizer (career / interview)                                                        
  [+] ☁️ Infrastructure & Vault (aws / obsidian / agy-vault)                                                
  [+] 🎨 Appearance & Theme Settings (theme / topic)                                                          
  [+] 🌐 System & Network Diagnostics (csys / logs)                                                           
  [+] 🛸 Help, Manuals & Docs (help)                                                                          
  ────────────────────────────────────────────────────────────────────────────────────────
  [Exit] Exit Control Center                                                                                  

Title: 📁 Workspace & Dev > Terminal IDE Explorer (ide)
Filter: [ / ] type to filter...  │  Sub-items: 6 available in this branch
Nav: [↑/↓ j/k] Move  │  [←] Collapse Category  │  [Enter] Launch Screen  │  [/] Filter  │  [Esc] Exit
Tip: Press [Enter] or [→] to launch 'Terminal IDE Explorer' (ide)
Select command or type alias: 
```

---

### 4.3 Level 3: Expanded `AI Agent & Ollama` Category View

```text
  [+] 📂 Favorites                                                                                            
  [+] 📁 Workspace & Dev (cnav / proj)                                                                        
> [-] 🤖 AI Agent & Ollama (cai / ollama)                                                                     
  │  ├── 🤖 Interactive AI Assistant (ai / ask-ai)  — Multi-provider LLM prompt terminal                     
  │  ├── 📦 Ollama Model Manager (ollama-models)    — Manage pulled GGUF models & quant tags                 
  │  ├── 🟢 Ollama Daemon Status (ollama-status)    — Service health & active HTTP port                       
  │  ├── 📊 Ollama Model Benchmark (ollama-benchmark)— Tokens/sec evaluation benchmark                     
  │  └── ⚡ AI Codebase Scanner (ai-scan / gcmt-ai) — AST diff scanner & commit message generator             
  [+] 👤 AGY Account Switcher (agyswitch / account)                                                           
  [+] 📚 Learn & Study Suite (learn / flashcards)                                                             
  [+] 🎯 Quizzes & Developer Snippets (quiz / snippets)                                                       
  [+] ⚡ Career & Algo Visualizer (career / interview)                                                        
  [+] ☁️ Infrastructure & Vault (aws / obsidian / agy-vault)                                                
  [+] 🎨 Appearance & Theme Settings (theme / topic)                                                          
  [+] 🌐 System & Network Diagnostics (csys / logs)                                                           
  [+] 🛸 Help, Manuals & Docs (help)                                                                          
  ────────────────────────────────────────────────────────────────────────────────────────
  [Exit] Exit Control Center                                                                                  

Title: 🤖 AI Agent & Ollama > Interactive AI Assistant (ask-ai)
Filter: [ / ] type to filter...  │  Sub-items: 5 available in this branch
Nav: [↑/↓ j/k] Move  │  [←] Collapse Category  │  [Enter] Launch Screen  │  [/] Filter  │  [Esc] Exit
Tip: Press [Enter] or [→] to launch 'Interactive AI Assistant' (ask-ai)
Select command or type alias: 
```

---

### 4.4 Level 4: Live Global Filter Buffer (`/` Query Mode)

```text
> 1) 💻 Terminal IDE Explorer (ide)                 — Workspace & Dev                                         
  2) 🔎 Pattern Search Engine (ide-search)          — Workspace & Dev                                         
  3) 📑 System Diagnostic Log Viewer (logs)         — System & Network Diagnostics                            
  4) 🐳 Docker Container Logs (dlogsu)              — System & Network Diagnostics                            
  ────────────────────────────────────────────────────────────────────────────────────────

Title: 🔍 Global Search Filter > Matched 4 commands for 'log'
Filter: [ log_                                    ] (Press Backspace to edit, Esc to clear)
Nav: [↑/↓] Select Result  │  [Enter] Execute Command  │  [Esc] Clear Search Buffer
Select command [1-4]: 1
```

---

## 5. 📱 Exhaustive Subsystem-by-Subsystem TUI ASCII Mockup Catalog (39 Complete Views)

### 5.1 🔨 Project Scaffolder Subsystem (`scaffold`)

#### View 1: Template Selection View
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

#### View 2: Project Name & Path Input Prompt
```text
  Selected Template: webapi (.NET Web API)

  Enter Project Name:
  > OrderService.API

  Enter Target Parent Directory (default: current workspace):
  > C:\Projects\Microservices

Title: 🔨 Project Scaffolder > Name & Target Directory Input
Input project details and press Enter (or Esc to abort): 
```

#### View 3: Scaffolding Execution Progress
```text
  ✔ Scaffolding .NET Web API 'OrderService.API'...
  ✔ Initializing git repository...
  ✔ Registering workspace in AgyTui...

  Project created successfully at: C:\Projects\Microservices\OrderService.API

Title: 🔨 Project Scaffolder > Execution Complete
Press any key to open in Terminal IDE or navigate...
```

---

### 5.2 💼 Workspace Discovery & Pruning Subsystem (`discover-workspaces` / `prune-workspaces` / `proj`)

#### View 4: Auto-Discover Unregistered Projects (`discover-workspaces`)
```text
  Scanning container path 'C:\Projects'... Found 3 unregistered projects:

  [1] 📦 CustomerPortal (React / TypeScript — C:\Projects\CustomerPortal)
  [2] 📦 NotificationService (.NET Worker — C:\Projects\NotificationService)
  [3] 📦 PaymentGateway (.NET Web API — C:\Projects\PaymentGateway)

Title: 💼 Workspace Manager > Auto-Discover Projects (discover-workspaces)
Actions: [1-3] Register Selected Project  │  [a] Register All  │  Esc Cancel
Select option: 
```

#### View 5: Prune Stale Workspaces (`prune-workspaces`)
```text
  Checking registered workspace paths for missing directories...

  • [MISSING] Legacy-API (Path no longer exists: C:\OldProjects\Legacy-API)
  • [VALID]   Powershell (Path verified: C:\Users\TruongNhon\Documents\Powershell)

Title: 💼 Workspace Manager > Prune Stale Workspaces (prune-workspaces)
Actions: [y] Confirm Prune Missing Paths  │  Esc Keep All
Select option: y
```

#### View 6: Registered Workspace Navigator View (`proj` / `cnav`)
```text
  1) 📁 Powershell                  — C:\Users\TruongNhon\Documents\Powershell (Active)
  2) 📁 OrderService.API            — C:\Projects\Microservices\OrderService.API
  3) 📁 CustomerPortal              — C:\Projects\CustomerPortal
  4) 📁 NotificationService         — C:\Projects\NotificationService

Title: 💼 Workspace Manager > Registered Workspace Navigator (proj)
Nav: ↑/↓ Navigate  │  Enter Switch Workspace  │  [a] Add  │  [d] Delete  │  Esc Back
Select option: 1
```

---

### 5.3 📑 System Diagnostics & Docker Log Viewers (`logs` / `dlogsu`)

#### View 7: Control Center System Diagnostic Log Viewer (`/log` / `LogHelper.cs`)
```text
  [2026-08-11 00:00:01] INFO  [Bootstrapper] ServiceProvider initialized in 42ms.
  [2026-08-11 00:00:02] DEBUG [Config] Loaded profile.config.json. UI.Mode = "simple-cli".
  [2026-08-11 00:02:15] WARN  [AccountStore] Account 'work-prod' token expired.
  [2026-08-11 00:05:00] ERROR [GitClient] Command 'git fetch' returned exit code 128 (Network timeout).

Title: 📑 AgyTui System Diagnostic Log Viewer (logs/app.log)
Nav: ↑/↓ Scroll Logs  │  [f] Follow Live Logs  │  [c] Clear Log File  │  Esc Back
```

#### View 8: Docker Container Log Tailer (`dlogsu` / `DockerClient.cs`)
```text
  [Container: localstack | http://localhost:4566]
  2026-08-11 00:01:10 INFO  Ready. Available services: s3, sqs, ssm, dynamodb, lambda.
  2026-08-11 00:03:00 INFO  POST / HTTP/1.1 200 - S3 ListBuckets

Title: 🐳 Docker Container Logs > localstack (dlogsu)
Nav: ↑/↓ Scroll Logs  │  [f] Follow  │  Esc Back
```

---

### 5.4 🗄️ EF Core Database Migration Subsystem (`add-migration` / `update-db`)

#### View 9: Add EF Core Migration (`add-migration` / `da`)
```text
  Target DbContext: ApplicationDbContext
  Enter Migration Name:
  > AddUserPreferencesTable

  Executing `dotnet ef migrations add AddUserPreferencesTable`...
  ✔ Migration '20260811000600_AddUserPreferencesTable' generated in ./Migrations.

Title: 🗄️ EF Core > Add Migration (add-migration)
Actions: [u] Apply Migration to Database Now  │  Esc Done
```

#### View 10: Update Database Schema (`update-db` / `du`)
```text
  Applying pending migrations to local SQLite database...

  • Applying migration '20260811000600_AddUserPreferencesTable'...
  ✔ Database schema updated successfully.

Title: 🗄️ EF Core > Update Database (update-db)
Press any key to return...
```

---

### 5.5 🤖 Ollama LLM Suite Views (`ollama-status` / `ollama-models` / `ollama-benchmark`)

#### View 11: Ollama Daemon Health Status (`ollama-status`)
```text
  • [1] llama3.2:latest (3.8 GB · Q4_K_M)
  • [2] hermes3:8b (4.7 GB · Q4_K_M)
  • [3] codex-local:latest (6.1 GB · Q5_K_M)

Title: 🤖 Ollama Daemon Status — ONLINE (http://localhost:11434)
Actions: [p] Pull Model  │  [b] Benchmark Models  │  [l] View Server Logs  │  [m] Manage
Select option: 
```

#### View 12: Ollama Interactive Model Manager (`ollama-models`)
```text
  1) 🤖 llama3.2:latest — 3.8 GB (Family: llama · Quant: Q4_K_M)
  2) 🤖 hermes3:8b — 4.7 GB (Family: llama · Quant: Q4_K_M)
  3) 🤖 codex-local:latest — 6.1 GB (Family: qwen · Quant: Q5_K_M)

Title: 🤖 Ollama > Pulled Model Manager
Actions: [1-3] Select Model  │  [r] Remove Model  │  [c] Copy Model Tag  │  Esc Back
Select model: 1
```

#### View 13: Ollama Model Benchmark Evaluator (`ollama-benchmark`)
```text
  Model Benchmark Results (Prompt: 512 tokens / Generation: 128 tokens):

  • llama3.2:latest   │ Eval Speed: 42.8 tokens/sec │ Warmup: 110ms
  • hermes3:8b        │ Eval Speed: 31.4 tokens/sec │ Warmup: 180ms
  • codex-local:latest│ Eval Speed: 24.1 tokens/sec │ Warmup: 240ms

Title: 🤖 Ollama > Performance Benchmark Evaluator
Press any key to return to model manager...
```

---

### 5.6 💻 Terminal IDE Feature Suite Views (`ide` / `symbol` / `ide-search` / `ide-diff`)

#### View 14: Terminal IDE Main Explorer View (`ide`)
```text
  EXPLORER (Left Pane)           │ FILE VIEWPORT: Program.cs (Right Pane)
  ▼ AgyTui                       │ 1: namespace AgyTui;
    ▶ Domain                     │ 2: public static class Program
    ▶ Infrastructure             │ 3: {
    ▼ UI                         │ 4:     public static int Main(string[] args)
      ▼ Core                     │ 5:     {
        ▼ Layouts                │ 6:         return RunAppZeroLag(args);
          📄 ScreenChrome.cs     │ 7:     }
      📄 Program.cs              │ 8: }

Title: 💻 Terminal IDE — C:\Users\TruongNhon\Documents\Powershell\csapp\AgyTui
Nav: ↑/↓/←/→ File Tree  │  Enter Open  │  / Symbol Search  │  Esc Close IDE
```

#### View 15: Workspace Symbol Indexer View (`symbol` / `SymbolSearch.cs`)
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

#### View 16: Workspace File Pattern Search View (`ide-search` / `IdeSearchService.cs`)
```text
  Search Pattern Match Results for 'RenderFrameZeroLag':

  1) UI/Core/Layouts/SimpleCliMenuRenderer.cs:L52 — ScreenChrome.RenderFrameZeroLag(...)
  2) UI/Core/Layouts/MenuRendererBase.cs:L88 — public static void RenderFrameZeroLag(...)
  3) UI/Screens/Workspace/ProjectScreen.cs:L31 — RenderFrameZeroLag(() => ...)

Title: 💻 Terminal IDE > Workspace Pattern Search (ide-search)
Nav: [1-3] Open File at Line  │  Esc Cancel
```

#### View 17: Colorized Git Diff Viewer (`ide-diff` / `GitDiffViewer.cs`)
```text
  Modified File (1/3): csapp/AgyTui/Program.cs
  @@ -8,3 +8,3 @@
  - return RunApp(args);
  + return RunAppZeroLag(args);

Title: 💻 Terminal IDE > Git Diff Viewer (ide-diff)
Nav: ↑/↓ Scroll Diff  │  [n] Next Modified File  │  [p] Previous File  │  Esc Exit
```

---

### 5.7 💼 Workspace Customization Views (`agysw` / `theme` / `topic`)

#### View 18: AGY Account Switcher & Auth Guard (`agysw` / `AccountScreen.cs`)
```text
> 1) work-prod (work@corp.com) (✔ Logged In · Key: Key-9f8a) (Active)
  2) dev-personal (dev@personal.io) (✔ Logged In · Key: Key-1b4c)
  3) sandbox-test (test@sandbox.net) (✘ Logged Out · Key: None)

Title: 💼 Account Manager > AGYSWITCH Account Manager (agysw)
Nav: ↑/↓ Navigate  │  Enter Switch  │  [a] Create  │  [l] Auth Login  │  [d] Delete  │  [o] Logout
Select option: 
```

#### View 19: Visual Theme Selector View (`theme` / `ThemeScreen.cs`)
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

#### View 20: AI Learning Topic Selector View (`topic` / `TopicScreen.cs`)
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

### 5.8 🎓 Master Learning Suite Views (`learn` / `flashcards` / `study-stats` / `vocab`)

#### View 21: Antigravity Master Learning Suite Hub (`learn auto` / `LearnRouter.cs`)
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

#### View 22: Spaced Repetition Flashcard Engine (`flashcards` / `FlashcardEngine.cs`)
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

#### View 23: Study Session & Pomodoro Statistics (`study-stats` / `StudyConsoleView.cs`)
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

#### View 24: Two-Step Word Reveal Drill (`vocab` / `InterviewBank.cs`)
```text
  PROMPT WORD (JLPT N2): 曖昧 (あいまい)
  ────────────────────────────────────────────────────────────────────────────
  Press [Enter] to reveal definition...

  [REVEALED]
  Definition: Vague, ambiguous, unclear.
  Example   : 彼の返事は曖昧だった。 (His answer was vague.)

Title: 📖 Vocab Drill > JLPT N2 Flashcard Drill
Did you recall this word? [y] Recalled  │  [n] Missed  │  Esc Exit
```

---

### 5.9 🐙 Git Nexus Multi-Repo Suite Views (`gbr` / `gstats` / `repograph`)

#### View 25: Live Multi-Repo Status Dashboard (`gbr` / `GitNexus.cs`)
```text
  Repo                 Branch            Sync         Dirty    Last Commit
  ────────────────────────────────────────────────────────────────────────────
  Powershell           main              [green]sync[/]         [dim]0[/]      a1b2c3d Fix TUI scroll zero-lag
  OrderService.API     feature/auth      [yellow]↑2[/]         [yellow]3[/]      f9e8d7c Add JWT middleware
  CustomerPortal       main              [cyan]↓1[/]         [dim]0[/]      b4c5d6e Bump Vite to v5.2

Title: 🐙 Git Nexus > Multi-Workspace Live Sync Dashboard (gbr)
Nav: Auto-refreshes (30s)  │  ↑/↓ Scroll  │  [r] Refresh Now  │  Esc Exit Dashboard
```

#### View 26: Commit Frequency Bar Chart & Branch Structure (`gstats` / `GitNexusStats.cs`)
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

#### View 27: Workspace Project Dependency Tree View (`repograph` / `RepoGraph.cs`)
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

### 5.10 🎯 Interactive Quizzes & Dev Tools Views (`quiz-cs` / `kana-quiz` / `snippets`)

#### View 28: C# & .NET Knowledge Assessment Quiz (`quiz-cs` / `CsharpQuiz.cs`)
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

#### View 29: Japanese Kana & Pronunciation Practice Quiz (`kana-quiz` / `KanaQuiz.cs`)
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

#### View 30: Developer Code Snippet Inspector (`snippets` / `SnippetLibrary.cs`)
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

### 5.11 ⚡ Career & Algorithm Visualizer Suite Views (`interview` / `star-builder` / `algo-viz`)

#### View 31: Technical Interview Question Card (`interview` / `InterviewBank.cs`)
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

#### View 32: STAR Method Answer Construction View (`star-builder` / `StarBuilder.cs`)
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

#### View 33: Algorithm Array Mutation Visualizer (`algo-viz` / `AlgoVisualizer.cs`)
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

### 5.12 🏗️ Infrastructure & Integrations Subsystem Views (`aws-status` / `obsidian` / `agy-vault` / `agy-deck` / `ai-providers` / `ai-scan`)

#### View 34: AWS Infrastructure & LocalStack Inspector (`aws-status` / `AwsClient.cs`)
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

#### View 35: Obsidian Vault Sync & Note Graph (`obsidian` / `ObsidianClient.cs`)
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

#### View 36: DPAPI Secret Vault Inspector (`agy-vault` / `AgyVault.cs`)
```text
  STORAGE ENGINE: Windows DPAPI (DataProtectionScope.CurrentUser) + Windows Keyring

  • SECRET_OPENAI_API_KEY      [Protected · Base64 Signature: a8f9...4b1c]
  • SECRET_DATABASE_PASSPHRASE [Protected · Base64 Signature: c1d2...9e8f]
  • SECRET_GITHUB_PAT          [Protected · Base64 Signature: e5f6...7a8b]

Title: 🔒 AGY Vault > Encrypted Credentials & Secret Store (agy-vault)
Actions: [a] Add Secret  │  [r] Retrieve Secret  │  [d] Delete Secret  │  Esc Back
Select option: 
```

#### View 37: Antigravity Deck Micro-Server Controller (`agy-deck` / `AntigravityDeckClient.cs`)
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

#### View 38: Multi-AI LLM Provider Latency Inspector (`ai-providers` / `ClaudeProvider.cs`)
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

#### View 39: AI Codebase AST Scanner & Commit Generator (`ai-scan` / `AiProjectScanner.cs`)
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

## 6. ⌨️ Interactive Keybinding & Action Matrix

| Action | Primary Key | Alternative Hotkeys | Behavioral Description |
| :--- | :---: | :---: | :--- |
| **Quick Select** | `1` .. `9` | N/A | Types option index number into linear prompt |
| **Back / Exit** | `0` | `Esc`, `q`, `b` | Returns to parent menu level or exits sub-screen |
| **Navigate Up** | `↑` | `k` | Moves selection index up in memory buffer |
| **Navigate Down** | `↓` | `j` | Moves selection index down in memory buffer |
| **Page Up / Down** | `PgUp` / `PgDn` | N/A | Scrolls viewport by page size (`Console.WindowHeight - 19`) |
| **Expand / Collapse** | `←` / `→` | N/A | Expands or collapses category/group tree branch |
| **Select / Execute**| `Enter` | `Space` | Activates selected option or launches child view |
| **Global Search** | `/` | `Ctrl+F` | Enters single-column live search mode |
| **Delete Word** | `Ctrl+W` | `Backspace` | Deletes last character or last word in search buffer |
| **Copy Shortcut** | `c` / `y` | N/A | Copies selected command alias (e.g. `/ide`) to OS clipboard |

---

## 7. 🔒 Console UX & Signal Safety Checklist

- [x] **UTF-8 Console Encoding**: Set explicit console encodings at launch (`Console.OutputEncoding = Encoding.UTF8`).
- [x] **SIGINT / SIGTERM Interception**: Register `Console.CancelKeyPress` to gracefully flush SQLite transactions without leaving orphan locks.
- [x] **Zero-Lag Smooth Flushing**: Use single-call stdout buffer flushing (`\x1b[H`) in `ScreenChrome.cs` to eliminate terminal scrolling flicker.
