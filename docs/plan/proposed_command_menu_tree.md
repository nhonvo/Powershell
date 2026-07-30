# Proposed Command Menu Tree Structure for AgyTui `CommandRegistry.cs`

This document details the complete, expanded hierarchical menu tree proposed for `CommandRegistry.cs`. All current existing menu items have been preserved, and missing actions (such as `cnav`, `reset-agy`, `purge-accounts`, `dotnet-info`) have been added.

```
📁 AgyTui Command Menu Tree
├── 🛠️ [Workspace & Dev]
│   ├── 📁 Navigation
│   │   ├── proj (p, prj) — Interactive Workspace Navigator
│   │   ├── cnav          — Registered Workspace Navigator
│   │   ├── go            — Navigate & Launch Terminal Session
│   │   ├── open-term     — Open New Terminal Session in CWD
│   │   └── f             — Open Windows File Explorer in CWD
│   ├── 💻 Terminal IDE & Scaffolding
│   │   ├── ide           — Interactive Terminal IDE
│   │   ├── ide-diff      — Side-by-side Git Diff Viewer
│   │   ├── ide-search    — Workspace Code & Symbol Search
│   │   └── scaffold      — Project Boilerplate Wizard
│   ├── 🔷 .NET Developer Suite
│   │   ├── dbld (db)     — dotnet build
│   │   ├── dr            — dotnet run
│   │   ├── dtst (dt)     — dotnet test
│   │   ├── df            — dotnet format
│   │   ├── dcl           — dotnet clean
│   │   ├── drestore (dres)— dotnet restore
│   │   ├── dpublish      — dotnet publish -c Release
│   │   ├── dpack         — dotnet pack (.nupkg)
│   │   ├── dpubpkg       — Publish .nupkg to NuGet
│   │   ├── dwatch (dw)   — dotnet watch run
│   │   ├── clean-build   — Delete bin/ & obj/ recursively
│   │   ├── add-migration — EF Core: add migration
│   │   ├── update-db     — EF Core: update database
│   │   ├── dotnet-info   — dotnet --info environment diagnostics
│   │   └── rebuild-tui   — Rebuild Control Center TUI binary
│   ├── 🌿 Git & Repository Tools
│   │   ├── gs            — git status (--short)
│   │   ├── ga            — git add .
│   │   ├── gbr (gb)      — Git Branch Manager & Checkout
│   │   ├── gcmt          — Conventional Commit Wizard
│   │   ├── glog (glo, glg)— Git Commit Graph Log
│   │   ├── gpull (gpu)   — git pull
│   │   ├── gpush (gus)   — git push
│   │   ├── gf            — git fetch
│   │   ├── gd            — Git Diff Viewer
│   │   ├── git-undo      — git reset --soft HEAD~1
│   │   ├── nexus         — Multi-repo Nexus Dashboard
│   │   ├── repo-graph    — Repo Dependency Graph
│   │   └── nexus-stats   — Nexus Commit Stats Summary
│   ├── 🐳 Docker Suite
│   │   ├── docker-health — Container Health & Resources
│   │   ├── dkcl          — Docker Cleanup Dashboard
│   │   ├── dimg          — Docker Image Manager
│   │   ├── dlogs         — Container Log Tail
│   │   ├── dcup (dkcpu)  — docker compose up -d
│   │   ├── dcdown (dkcpd)— docker compose down
│   │   ├── dkstac        — Stop All Containers
│   │   └── dkrmac        — Remove All Containers
│   └── ☁️ AWS & LocalStack Suite
│       ├── aws-whoami    — AWS STS Caller Identity
│       ├── aws-local     — LocalStack Sandbox Status (port 4566)
│       ├── aws-s3        — List S3 Buckets
│       ├── aws-sqs       — List SQS Queues
│       ├── aws-ssm       — Inspect Parameter Store
│       ├── aws-sns       — List SNS Topics
│       ├── aws-dynamodb  — List DynamoDB Tables
│       └── aws-lambda    — List Lambda Functions
│
├── 🔑 [AGY Account Switch]
│   ├── 👤 Account Context Manager
│   │   ├── agyswitch     — Select Active Account Context
│   │   ├── agyquota      — Multi-Account Quota Summary
│   │   ├── account-tree  — Active Account Hierarchy Tree
│   │   ├── quota-chart   — Bar Chart of Account Limits
│   │   ├── live-dashboard— Live Metrics Monitoring Table
│   │   ├── reset-agy     — Reset & Purge All Account Data
│   │   └── purge-accounts— Purge Custom Accounts & Reset to Default
│   ├── ⚙️ Account Controls & Toggles
│   │   ├── autoswitch    — Toggle Auto-Switching Candidate Accounts
│   │   ├── no-auto-commit— Toggle Multi-Agent Auto-Commit Mode
│   │   └── autocommit    — Alias for no-auto-commit toggle
│   ├── 🔒 Secret Vault
│   │   ├── secret-set    — Save Encrypted Key-Value Secret
│   │   ├── secret-get    — Read & Decrypt Secret Value
│   │   ├── secret-list   — List Encrypted Secrets
│   │   └── secret-remove — Delete Encrypted Secret
│   ├── 🖥️ Antigravity Deck (Desk)
│   │   ├── deck-status   — Check Deck Server Status (port 3000)
│   │   ├── deck-setup    — Initialize Deck Environment
│   │   ├── deck-start    — Launch Local Deck App
│   │   └── deck-online   — Expose Deck via Tunnel
│   └── 🏢 Antigravity Manager
│       ├── mgr-status    — Check Manager Backend (port 8045)
│       ├── mgr-setup     — Setup Manager Dependencies
│       └── mgr-start     — Boot Manager Desktop Application
│
├── 🤖 [AI Agent & Ollama]
│   ├── 🧠 AI Agents & Invocations
│   │   ├── agy-cli       — Launch Antigravity CLI executable
│   │   ├── claude        — Claude Code CLI (Auto Mode)
│   │   ├── claude-cloud  — Claude Code CLI (Force Direct Cloud)
│   │   ├── claude-ollama — Claude Code CLI (Force Local Ollama)
│   │   ├── codex         — Codex CLI (Auto Mode)
│   │   ├── codex-cloud   — Codex CLI (Force Direct Cloud)
│   │   ├── codex-ollama  — Codex CLI (Force Local Ollama)
│   │   ├── openclaw      — OpenClaw Agent
│   │   ├── hermes        — Hermes3 Reasoning Model
│   │   ├── hermesd       — Hermes3 Debug Mode
│   │   ├── ai-history    — AI Invocations Audit Log Ledger
│   │   └── ai-mode-check — Check Resolved AI Provider Mode
│   └── 🦙 Ollama Local LLM Suite
│       ├── ollama-status — Check Daemon Status (port 11434)
│       ├── ollama-models — Model Management Dashboard
│       ├── ollama-pull   — Download New Model
│       ├── ollama-start  — Boot Background Daemon (`ollama serve`)
│       ├── ollama-logs   — Tail Server Logs
│       └── ollama-benchmark— Benchmark Evaluation Speed (t/s)
│
├── 🌐 [System & Network]
│   ├── 📊 Diagnostics & System
│   │   ├── disk (usage)  — Disk Usage & Free Space Ratios
│   │   ├── public-ip (myip)— Resolve External Public IP
│   │   └── kill-port     — Kill Process Listening on Port
│   ├── 🔌 Network & SSH
│   │   ├── ssh-info      — SSH Connection Summary & Local IPs
│   │   ├── tailscale-status— Mesh Peer Connectivity Status
│   │   └── ssh-qr        — Generate Terminal QR Code for SSH
│   └── 🔄 Reload & Environment
│       ├── system-reload — Interactive Reload Options Menu
│       ├── reload-cc     — Rebuild & Restart Control Center TUI
│       ├── reload-term   — Reload PowerShell $PROFILE
│       └── reload-all    — Full Refresh ($PROFILE + TUI Rebuild)
│
├── 📚 [Learn & Study]
│   ├── 🎯 Study Hub & AI Generation
│   │   ├── learn         — Interactive Topic Learning Router
│   │   └── learn-gen     — AI Content & Deck Generator
│   ├── ⏱️ Session & Progress Tracking
│   │   ├── session       — Start 25-Min Pomodoro Session
│   │   ├── stats         — Weekly Study Volume & Stats
│   │   ├── goals         — Daily Learning Targets
│   │   ├── streak        — Consecutive Study Streak Counter
│   │   ├── due           — Spaced Repetition (SM-2) Due Reviews
│   │   ├── progress      — Visual Progress Bar Charts
│   │   └── weak          — Pre-session Weak Items Queue
│   ├── 📓 Obsidian Vault & Knowledge Base
│   │   ├── obsidian      — Note & Tag Browser
│   │   ├── obs-graph     — Inter-Note Wikilink Graph
│   │   ├── refresh       — Rescan Vault & Sync Datasets
│   │   ├── vault-open    — Open Vault Folder in Explorer
│   │   └── add-resource  — Register File / URL Resource
│   └── 🎓 Domain Suites
│       ├── 🇯🇵 Japanese   : kana, kanji, jlpt, grammar
│       ├── 🇬🇧 English    : word-of-day, vocab, flashcard
│       ├── 💻 C# / .NET   : quiz, snippets, sheets
│       ├── 🧮 DSA        : algo, complexity, problems
│       └── 💼 Career     : interview, star, mock
│
├── 🎨 [Appearance & Layout]
│   ├── theme             — Interactive Oh-My-Posh Theme Picker
│   ├── mobile-setup      — Toggle Mobile Setup (Mobile Mode + Compact)
│   ├── ui-mode           — Toggle Layout Mode (Three-Pane vs Flat-Tree)
│   ├── density           — Toggle Display Density (Comfortable vs Compact)
│   ├── favorite          — Pin/Unpin Command Alias to Favorites
│   └── favorites         — List Pinned Favorite Commands
│
└── ❓ [Help & Docs]
    ├── cc                — Command Palette
    ├── help              — Interactive Profile Help Browser
    └── hotkeys           — Grouped Keyboard Shortcuts Guide
```
