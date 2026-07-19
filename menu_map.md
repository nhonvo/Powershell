# 🛸 PowerShell Profile Control Center v3.0 — Menu Map

This document outlines the consolidated, clean, and redesigned menu structure of the profile Control Center (`cc`). All redundancies, overlapping categories, and mixed items have been resolved into a single, cohesive 10-section hierarchy.

---

## 📁 1. `[Workspace & Dev]`
Project workspace navigation, terminal IDE integration, .NET build/test/clean actions, EF migrations, and git workflows.
*   **Navigate Workspace** (`proj` / `p`) — Jump to a registered workspace directory from `priority_workspaces.json`.
*   **Terminal IDE** (`ide`) — Launch terminal IDE session inside the active directory.
*   **Diff Viewer** (`ide-diff`) — Visual git diff viewer for staging.
*   **Search Across Files** (`ide-search`) — Find text patterns recursively in files.
*   **[.NET Project Tools]** (Grouped .NET commands):
    *   **Build Project** (`dbld`) — Run `dotnet build` inside the active workspace.
    *   **Test Project** (`dtst`) — Run `dotnet test` inside the active workspace.
    *   **Clean Build Artifacts** (`clean-build`) — Recursively purge `bin/` and `obj/` directories.
    *   **Add EF Migration** (`add-migration`) — Scaffold a new Entity Framework Core migration.
    *   **Update EF Database** (`update-db`) — Apply EF Core migrations to database.
*   **Scaffold New Project** (`scaffold`) — Interactive boilerplate creator (webapi, blazor, console).
*   **Git Status** (`gs`) — Color-coded short git status overview.
*   **Conventional Commit** (`gcmt`) — Commit wizard following standard conventions.
*   **Git Undo Last Commit** (`git-undo`) — Soft-reset the last commit while keeping staged changes.
*   **Repo Nexus Graph** (`nexus`) — Visualize branch relationships and repository graphs.

---

## 🤖 2. `[AI Agent & Ollama]`
AI developer assistants and local large language models (LLMs). Automatically queries the local daemon status and lists pulled models in the Details pane.
*   **Claude Code (Cloud)** (`claude-cloud`) — Launch Anthropic's Claude Code CLI utilizing cloud APIs.
*   **Claude Code (Ollama)** (`claude-ollama`) — Run Claude Code routed locally via Ollama.
*   **Codex (Cloud)** (`codex-cloud`) — Launch Gemini's Codex CLI utilizing cloud APIs.
*   **Codex (Ollama)** (`codex-ollama`) — Run Codex locally routed via Ollama.
*   **OpenClaw (Ollama)** (`openclaw`) — Execute local OpenClaw task routines.
*   **Hermes3 (Ollama)** (`hermes`) — Run Hermes3 local model reasoning.
*   **Ollama: Manage Models** (`ollama-models`) — List pulled local models, view details/info, or delete models.
*   **Ollama: Pull New Model** (`ollama-pull`) — Fetch/download a new model from Ollama registry.
*   **Ollama: Start Daemon** (`ollama-start`) — Boot up the background Ollama service daemon.
*   **Ollama: View Server Logs** (`ollama-logs`) — Show last 50 lines of local Ollama server logs.
*   **Antigravity Deck: Setup/Initialize** (`deck-setup`) — Run `npm run setup` in `C:\Users\TruongNhon\AppData\Local\AntigravityDeck`.
*   **Antigravity Deck: Start Local** (`deck-start`) — Run `npm run dev` to start local dev server.
*   **Antigravity Deck: Go Online (Tunnel)** (`deck-online`) — Run `npm run online` to expose local dev via Cloudflare Tunnel.
*   **Launch Antigravity CLI (agy)** (`agy-cli`) — Launch the google antigravity CLI tool terminal.

---

## 🔑 3. `[AGY Account Switch]`
Antigravity accounts, credentials switching, and rolling token quota tracking.
*   **Select Active Account** (`agyswitch`) — Toggle between active profiles with login status indicators (✔/✘) and real Gmail name resolution.
*   **View All Accounts** (`agyquota`) — Print token usage quotas and credentials paths.
*   **Account Tree** (`account-tree`) — View conversation counts, private sizes, and weekly usage.
*   **Quota Bar Chart** (`quota-chart`) — Horizontal bar chart displaying remaining 5H and weekly quotas.
*   **Live Dashboard** (`live-dashboard`) — Tabular status screen of all credentials in the keyring.
*   **Toggle Auto-Switch** (`autoswitch`) — Toggle automatic credential switching on directory change.

---

## 🐳 4. `[Docker & Databases]`
Container runtime control, local cloud mock sandboxes, and SQLite database browsers.
*   **Docker Cleanup** (`dkcl`) — TUI utility to prune networks, dangling layers, and unused volumes.
*   **Docker Compose Up** (`dcup`) — Spawn docker containers in background.
*   **Docker Compose Down** (`dcdown`) — Spin down container compose groups.
*   **LocalStack Info** (`aws-local`) — Query active LocalStack S3/SQS/Lambda services.
*   **SQLite Browser** (`db-tui`) — Open SQLite file in interactive terminal data viewer.

---

## 🌐 5. `[System & Network]`
Local system health diagnostics, network settings, and network socket management.
*   **Disk Usage** (`disk`) — Partitions space allocation and warning status.
*   **Public IP Address** (`public-ip`) — Query external IPv4 address.
*   **SSH Connection Info** (`ssh-info`) — View Tailscale IP and active inbound SSH connections.
*   **Kill Port** (`kill-port`) — Terminate process binding a specific port.

---

## 📖 6. `[Learn & Study]`
Active learning dashboard, language drills, algorithms complexity cheat sheets, and technical interview preparation.
*   **Start Learning (auto)** (`learn`) — Launch flashcard drills and algorithm reviews.
*   **Flashcard Deck Browser** (`flashcard`) — Browse and study flashcard decks.
*   **English Vocab Drill** (`vocab`) — Interactive vocabulary training.
*   **Kana Quiz** (`kana`) — Japanese hiragana/katakana training.
*   **Kanji Lookup** (`kanji`) — Search stroke orders and meanings of characters.
*   **JLPT Vocab Drill** (`jlpt`) — JLPT vocabulary flashcards.
*   **Algorithm Visualizer** (`algo`) — Sorting/searching algorithm execution.
*   **Big-O Complexity Sheet** (`complexity`) — Standard algorithms complexity index.
*   **DSA Problem Tracker** (`problems`) — Track solved LeetCode and HackerRank challenges.
*   **Code Snippet Library** (`snippets`) — Manage reusable snippets.
*   **Cheat Sheet Browser** (`sheets`) — Review dev cheat sheets.
*   **C# Quiz** (`quiz`) — C# features and dotnet runtime quiz questions.
*   **Interview Question Bank** (`interview`) — Behavioral and technical question pool.
*   **STAR Answer Builder** (`star`) — Practice framing STAR format answers.
*   **Mock Interview Timer** (`mock`) — Countdown timer for coding interviews.
*   **Word of the Day** (`word-of-day`) — Display daily definition, pronunciation, and example.

---

## 📈 7. `[Track & Progress]`
Session management, goals, streaks, progress analytics, and spaced-repetition due reviews.
*   **Start Pomodoro Session** (`session`) — Run custom focus session interval timer.
*   **Study Statistics** (`stats`) — Charts showing daily study hours and solved count.
*   **Daily Goals** (`goals`) — Track learning target completions.
*   **Study Streak** (`streak`) — Active study streaks calendar.
*   **Due Reviews** (`due`) — Spaced repetition flashcards due today.
*   **Progress Dashboard** (`progress`) — Aggregated analytics overview.
*   **Weak Items Queue** (`weak`) — Review incorrect/difficult quiz cards.

---

## 🕸️ 8. `[Obsidian & Resources]`
Obsidian knowledge base syncing, graph visualization, and reference material indexes.
*   **Obsidian Vault Config** (`obsidian`) — Update vault paths and configurations.
*   **Obsidian Graph View** (`obs-graph`) — Display wikilinks network in active workspace.
*   **Refresh Learning Data** (`refresh`) — Reload study files from directories.
*   **Add Resource** (`add-resource`) — Register reference links and files.

---

## ⚙️ 9. `[Theme & Settings]`
Profile themes, command palettes, and documentation help browser.
*   **Command Palette** (`cc`) — Action switcher command palette.
*   **Help Browser** (`help`) — In-terminal documentation reader.
*   **Select Shell Theme** (`theme`) — Interactive oh-my-posh prompt theme selector.

---

## 🚪 10. `[Exit]`
*   **[Exit] Exit Control Center** — Return to standard PowerShell prompt.
