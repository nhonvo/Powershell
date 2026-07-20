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
Active learning dashboard, Obsidian vault note sync, language drills, algorithms visualizers, complexity cheat sheets, and technical interview preparation.
*   **Start Learning (auto)** (`learn`) — Launch master learning suite router.
*   **[Obsidian Vault & Sync]** (Grouped Vault commands):
    *   **Obsidian Vault Browser** (`obsidian`) — Search notes by keyword, browse tags, view daily notes, and render graph.
    *   **Rescan & Sync Vault Datasets** (`refresh` / `sync`) — Rescan Obsidian Vault and sync flashcards, quizzes, and cheat sheets to `learn/`.
    *   **Open Vault Folder** (`vault-open`) — Open Obsidian Vault directory in Windows File Explorer.
*   **[Japanese Suite]** (Grouped Japanese tools):
    *   **Kana Quiz** (`kana`) — Japanese hiragana/katakana training.
    *   **Kanji Lookup** (`kanji`) — Search stroke orders, radicals, and meanings.
    *   **JLPT Vocab Drill** (`jlpt`) — JLPT N5 & N4 vocabulary flashcards (1,653 words).
    *   **Grammar Drills** (`grammar`) — N5, N4, N3 Japanese grammar points.
*   **[English & Vocab]** (Grouped English tools):
    *   **Word of the Day** (`word-of-day`) — Display daily developer definition and context.
    *   **English Vocab Drill** (`vocab`) — Interactive intermediate & advanced vocabulary.
    *   **Flashcard Deck Browser** (`flashcard`) — 94 flashcard decks with SM-2 spaced repetition scoring.
    *   **Grammar Drills** (`grammar`) — English tenses, conditionals, and modals.
*   **[C# & Dev Masterclass]** (Grouped C# tools):
    *   **C# Quiz** (`quiz`) — C# features and .NET 9 runtime multiple-choice quiz.
    *   **Code Snippet Library** (`snippets`) — Manage reusable snippets with clipboard copy.
    *   **Cheat Sheet Browser** (`sheets`) — Review 978 developer cheat sheets (.txt files).
*   **[DSA & System Design]** (Grouped DSA tools):
    *   **Algorithm Visualizer** (`algo`) — Step-by-step terminal execution (Bubble, Quick, Merge, BFS, DP).
    *   **Big-O Complexity Sheet** (`complexity`) — Standard algorithms time/space complexity index.
    *   **DSA Problem Tracker** (`problems`) — Track solved LeetCode and HackerRank challenges.
*   **[Career & Interview Prep]** (Grouped Career tools):
    *   **Interview Question Bank** (`interview`) — 34 Behavioral and technical questions.
    *   **STAR Answer Builder** (`star`) — Practice framing STAR format answers.
    *   **Mock Interview Timer** (`mock`) — Countdown timer for coding interview sessions.

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

## ⚙️ 8. `[Theme & Settings]`
Profile themes, command palettes, and documentation help browser.
*   **Command Palette** (`cc`) — Action switcher command palette.
*   **Help Browser** (`help`) — In-terminal documentation reader.
*   **Select Shell Theme** (`theme`) — Interactive oh-my-posh prompt theme selector.
*   **Toggle Auto-Commit** (`no-auto-commit` / `autocommit`) — Toggle automatic git commits during multi-agent tasks.

---

## 🚪 9. `[Exit]`
*   **[Exit] Exit Control Center** — Return to standard PowerShell prompt.
