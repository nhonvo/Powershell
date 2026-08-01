 ###  [Favorites] 
  Default favorite aliases set in configuration:
  •  [proj]  Navigate Workspace — Navigate to a registered workspace
  •  [agyswitch]  Switch Active Account — Switch active developer account credentials
  •  [open-term]  Open New Terminal Session — Launch new Windows Terminal ( wt.exe ) or PowerShell window
  •  [ask-ai]  Antigravity AI Agent — Interactive AI pair programming deck & chat 
  •  [vault]  Vault & Account Quotas — Manage tokens, quota, and credentials
  •  [ide]  Terminal IDE — Launch terminal IDE session
  ──────
  ###  [Workspace & Dev] 
  #### Navigation & Workspace
  •  [proj]  Navigate Workspace — Navigate to a registered workspace (Aliases:  p ,  prj )
  •  [cnav]  Registered Workspace Navigator — Interactive selector for all registered workspaces
  •  [go]  Navigate & Launch Workspace — Search and navigate to workspace or launch terminal session
  •  [open-term]  Open New Terminal Session — Launch new Windows Terminal ( wt.exe ) or PowerShell window
  •  [f]  Open Explorer — Open File Explorer in active workspace
  #### IDE & Code Tools
  •  [ide]  Terminal IDE — Launch in-terminal editor workspace ( micro / nvim / vim / nano )
  •  [ide-diff]  Diff Viewer — Full-screen colorized side-by-side / unified git diff viewer
  •  [ide-search]  Search Across Files — Search pattern and symbols across workspace files
  •  [scaffold]  Scaffold New Project — Interactive project boilerplate creator ( webapi ,  mvc ,  react ,  nextjs , etc.)

  #### Git & Repo Tools ( /git-tools )

  •  [gs]  Git Status — Short git status ( --short ) with color coding
  •  [ga]  Git Add All — Stage all modified and new files ( git add . )
  •  [gbr]  Git Branch Manager — Interactive branch manager sorted by commit date (Alias:  gb )
  •  [gcmt]  Conventional Commit — Conventional commit wizard ( feat ,  fix ,  docs , etc.)
  •  [glog]  Git Commit Log — Paged single-repo commit log graph (Aliases:  glo ,  glg )
  •  [gpull]  Git Pull Remote — Execute  git pull  on current branch (Alias:  gpu )
  •  [gpush]  Git Push Remote — Execute  git push  to remote tracking branch (Alias:  gus )
  •  [gf]  Git Fetch Remote — Fetch latest branch references from remote repository
  •  [gd]  Git Diff Viewer — Interactive git diff viewer for modified files
  •  [git-undo]  Git Undo Last Commit — Soft-reset the last local commit (Alias:  gundo )
  •  [nexus]  Repo Nexus Graph — Git Nexus multi-repo dashboard
  •  [repo-graph]  Repository Dependency Graph — Repository dependency graph
  •  [nexus-stats]  Git Nexus Commit Stats — Git Nexus commit stats

  #### .NET Tools ( /dotnet-tools )

  •  [dbld]  Build Project — Execute  dotnet build  in active workspace (Alias:  db )
  •  [dr]  Run Project — Execute  dotnet run  active project in workspace
  •  [dtst]  Test Project — Execute  dotnet test  in active workspace (Alias:  dt )
  •  [df]  Format Code — Execute  dotnet format  code style & linting rules
  •  [dcl]  Clean Solution — Execute  dotnet clean  build output directory
  •  [drestore]  Restore Packages — Execute  dotnet restore  packages in active workspace (Alias:  dres )
  •  [dpublish]  Publish Release — Execute  dotnet publish  release binary in active workspace
  •  [dpack]  Pack NuGet Package — Compiles Release package and outputs  .nupkg 
  •  [dpubpkg]  Publish Package to NuGet — Push  .nupkg  package to NuGet registry
  •  [dwatch]  Watch Live-Reload — Execute  dotnet watch run  continuous dev loop (Alias:  dw )
  •  [rebuild-tui]  Rebuild Control Center TUI — Recompile  AgyTui.csproj  and refresh binary
  •  [clean-build]  Clean Build Artifacts — Remove  bin/  and  obj/  recursively (Alias:  dclean )
  •  [add-migration]  Add EF Migration — EF Core: add migration (Alias:  da )
  •  [update-db]  Update EF Database — EF Core: update database (Alias:  du )
  •  [dotnet-info]  System & SDK Info — Display dotnet environment and SDK version details

  #### Docker Tools ( /docker-tools )

  •  [docker-health]  Docker Health Dashboard — Show container health & resource utilization
  •  [dkcl]  Docker Cleanup — Docker cleanup TUI dashboard
  •  [dkrmac]  Docker Remove All Containers — Stop and remove all Docker containers forcefully
  •  [dkstac]  Docker Stop All Containers — Stop all running Docker containers
  •  [dimg]  Docker Image Manager — List and inspect local Docker images and layer sizes
  •  [dlogs]  Docker Container Logs — Tail output logs for a selected running container
  •  [dcup]  Docker Compose Up —  docker compose up -d  (Alias:  dkcpu )
  •  [dcdown]  Docker Compose Down —  docker compose down  (Alias:  dkcpd )

  #### AWS Tools ( /aws-tools )

  •  [aws-whoami]  AWS Identity Info — Inspect active AWS STS caller identity, profile, and region
  •  [aws-local]  LocalStack Info — LocalStack sandbox diagnostics
  •  [aws-s3]  AWS S3 Buckets — List local or cloud S3 buckets
  •  [aws-sqs]  AWS SQS Queues — List local or cloud SQS queues
  •  [aws-ssm]  AWS SSM Parameter Store — Inspect Parameter Store key-value pairs
  •  [aws-sns]  AWS SNS Topics — Inspect notification topics
  •  [aws-dynamodb]  AWS DynamoDB Tables — Inspect DynamoDB tables
  •  [aws-lambda]  AWS Lambda Functions — Inspect serverless functions
  ──────
  ###  [AI Agent & Ollama] 

  •  [ask-ai]  Antigravity AI Agent — Interactive AI pair programming deck & chat
  •  [openclaw]  OpenClaw Agent — OpenClaw agent launcher
  •  [hermes]  Hermes Agent — Hermes agent launcher (Daemon:  hermesd )
  •  [claude]  Claude Agent — Claude agent launcher (Cloud:  claude-cloud , Ollama:  claude-ollama )
  •  [codex]  Codex Agent — Codex agent launcher (Cloud:  codex-cloud , Ollama:  codex-ollama )
  •  [agy-cli]  Antigravity CLI — Run raw  agy  CLI command
  •  [ai-history]  AI Conversation History — View recent AI agent conversation transcripts

  #### Ollama Tools ( /ollama-tools )

  •  [ollama-status]  Ollama Server Status — Check Ollama service status
  •  [ollama-models]  Ollama Model List — List downloaded Ollama models
  •  [ollama-pull]  Pull Ollama Model — Download LLM model weights via Ollama
  •  [ollama-start]  Start Ollama Service — Launch background Ollama daemon process
  •  [ollama-logs]  Tail Ollama Logs — View background Ollama server logs
  •  [ollama-benchmark]  Ollama Benchmark — Run token generation benchmark
  ──────
  ###  [AGY Account Switch] 

  •  [vault]  Vault & Account Quotas — Manage tokens, quota, and DPAPI credentials
  •  [agyswitch]  Switch Active Account — Switch active developer account credentials
  •  [agyquota]  Account Quota Tree — Interactive account quota & usage viewer

  #### Secret Vault ( /secret-vault )

  •  [secret-set]  Save Secret — Encrypt and save secret key in DPAPI vault
  •  [secret-get]  Get Secret — Decrypt and view stored secret key
  •  [secret-list]  List Secrets — List stored DPAPI secret key aliases
  •  [secret-remove]  Remove Secret — Remove secret key from vault

  #### Quota Views ( /quota-views )

  •  [account-tree]  Account Quota Tree — Interactive account quota & usage viewer
  •  [quota-chart]  Quota Chart — Terminal bar chart of 5-hour and weekly token limits
  •  [live-dashboard]  Live Account Dashboard — Live multi-account quota monitoring table

  #### Account Toggles ( /account-toggles )

  •  [autoswitch]  Toggle Auto-Switch — Toggle automatic account switching on quota exhaustion
  •  [no-auto-commit]  Toggle No-Auto-Commit — Toggle auto-commit behavior
  •  [autocommit]  Toggle Auto-Commit — Enable/disable git auto-commit

  #### Antigravity Deck & Manager ( /antigravity-deck ,  /antigravity-manager )

  •  [deck-status]  Deck Status — Check Antigravity Deck process status
  •  [deck-setup]  Deck Setup — Configure Antigravity Deck environment
  •  [deck-start]  Start Deck — Launch Antigravity Deck process
  •  [deck-online]  Check Deck Online — Verify online status of Antigravity Deck
  •  [mgr-status]  Manager Status — Check Antigravity Manager status
  •  [mgr-setup]  Manager Setup — Configure Antigravity Manager
  •  [mgr-start]  Start Manager — Launch Antigravity Manager process
  ──────
  ###  [Learn & Study] 

  •  [learn]  Learn & Skill Center — Open interactive learning & study console
  •  [learn-gen]  Generate Learning Material — AI generator for study flashcards & quizzes
  •  [guide]  Antigravity Guide — View built-in Antigravity CLI guide
  •  [slash-manual]  Slash Commands Manual — View documentation for  /goal ,  /schedule ,  /grill-me ,  /learn 
  •  [skills]  Custom Skills Directory — Browse active skills in  .gemini/skills/ 

  #### Study Tracking ( /track )

  •  [session]  Start Pomodoro Session — Start a Pomodoro study session
  •  [stats]  Study Statistics — Study statistics and weekly chart
  •  [goals]  Daily Goals — Daily learning goals
  •  [streak]  Study Streak — Study streak display
  •  [due]  Due Reviews — Show due spaced-repetition reviews
  •  [progress]  Progress Dashboard — Progress dashboard (bar chart + tree)
  •  [weak]  Weak Items Queue — Weak items queue (pre-session review)

  #### Obsidian Vault ( /obsidian-vault )

  •  [obsidian]  Obsidian Vault Browser — Search, browse by tag, and view daily notes (Alias:  obs-vault )
  •  [refresh]  Refresh Vault Cache — Re-index Obsidian markdown notes
  •  [vault-open]  Open Vault Folder — Open Obsidian Vault directory in Windows Explorer

  #### Japanese Suite ( /jp-suite )

  •  [kana]  Kana Quiz — Hiragana / Katakana quiz
  •  [kanji]  Kanji Lookup — Kanji lookup & stroke detail
  •  [jlpt]  JLPT Vocab Drill — JLPT vocabulary drill
  •  [grammar]  Grammar Quiz — Japanese grammar quiz

  #### English & Vocab ( /english-vocab )

  •  [word-of-day]  Word of the Day — Show today's vocabulary word
  •  [vocab]  English Vocab Drill — English vocabulary drill
  •  [flashcard]  Flashcard Deck Browser — Open flashcard deck browser

  #### C# & Dev Masterclass ( /csharp-master )

  •  [quiz]  C# Quiz — C# multiple-choice quiz
  •  [snippets]  Code Snippet Library — Code snippet library browser
  •  [sheets]  Cheat Sheet Browser — Cheat-sheet browser ( .txt  files)

  #### DSA & System Design ( /dsa-architect )

  •  [algo]  Algorithm Visualizer — Algorithm visualizer (sort / search)
  •  [complexity]  Big-O Complexity Sheet — Big-O complexity cheat-sheet
  •  [problems]  DSA Problem Tracker — DSA problem tracker

  #### Career & Interview Prep ( /career-interview )

  •  [interview]  Interview Question Bank — Interview question bank
  •  [star]  STAR Answer Builder — STAR answer builder
  •  [mock]  Mock Interview Timer — Mock interview timer
  ──────
  ###  [Obsidian & Resources] 

  •  [obs-graph]  Obsidian Graph View — Visualizes inter-note wikilink relationships in Obsidian vault
  •  [add-resource]  Add Resource — Add a file or URL link to resource registry
  ──────
  ###  [Appearance & Layout] 

  •  [theme]  Theme Manager — Interactive color theme switcher (Dark Modern, Cyberpunk, Forest)
  •  [ui-mode]  UI Mode Switcher — Switch between  three-pane  and  flat-tree  layouts
  •  [density]  Layout Density — Switch between default and compact layout density
  •  [favorite]  Manage Favorites — Add or remove command from favorites category (Alias:  favorites )
  •  [mobile-setup]  Mobile Screen Setup — Toggle compact rendering mode for smaller terminals
  ──────
  ###  [System & Network] 

  •  [config]  System Configuration — Edit base paths, search rules, and hotkeys
  •  [disk]  Disk Usage — Show disk usage and storage health (Alias:  usage )
  •  [public-ip]  Public IP Address — Resolve public IPv4 address (Alias:  myip )
  •  [kill-port]  Kill Port — Kill process occupying a specified TCP port
  •  [ssh-info]  SSH Connection Info — SSH connection summary

  #### SSH & Network Tools ( /ssh-tools )

  •  [tailscale-status]  Tailscale Status — Parse Tailscale status for peer connectivity
  •  [ssh-qr]  SSH Terminal QR Code — Generate terminal QR code for SSH connection parameters

  #### System Reload ( /system-reload )

  •  [system-reload]  System Reload Menu — Interactive menu to reload CC TUI or Terminal profile (Alias:  sys-reload )
  •  [reload-cc]  Reload Control Center TUI — Rebuild code and restart Control Center TUI binary session (Alias:  rcc )
  •  [reload-term]  Reload Terminal Profile — Reload PowerShell profile ( $PROFILE ) (Alias:  rterm )
  •  [reload-all]  Reload Terminal & CC — Full system refresh (reload  $PROFILE , rebuild  AgyTui , restart TUI) (Alias:  rall )
  ──────
  ###  [Help & Docs] 

  •  [cc]  Control Center — Launch main Control Center interface
  •  [help]  Help Documentation — Display command manual and help text
  •  [hotkeys]  Keyboard Hotkeys — Display global keyboard shortcuts cheatsheet
  •  [exit]  Exit Control Center — Clean exit from Control Center TUI

-> add these function to the menu if needed then update the status these unuse function

  ### 1. System, Network & Process Helpers ( SystemHelper ,  ProcessRunner ,  ThemeManager )

   Class          | Method Signature        | File Location                                | Purpose / Functionality
  ----------------|-------------------------|----------------------------------------------|-----------------------------------------------
    SystemHelper  |  ShowDiskSpace()        |  Infrastructure/Common/SystemHelper.cs:L65   | Displays disk usage table for drive volumes
    SystemHelper  |  GetPublicIP()          |  Infrastructure/Common/SystemHelper.cs:L88   | Resolves public IPv4 address via HTTP
    SystemHelper  |  KillPort(int port)     |  Infrastructure/Common/SystemHelper.cs:L105  | Kills process listening on given TCP port
    SystemHelper  |  ShowNetworkInterfaces()|  Infrastructure/Common/SystemHelper.cs:L122  | Lists active network interfaces & IP
                  |                         |                                              | addresses
    SystemHelper  |  ShowProcessList()      |  Infrastructure/Common/SystemHelper.cs:L145  | Displays top CPU/Memory process table
    ThemeManager  |  ResolveStartupTheme(str|  Infrastructure/Common/ThemeManager.cs:L93   | Resolves theme color palette at boot
                  | ing)                    |                                              |
    ThemeManager  |  SetMobileMode(string?) |  Infrastructure/Common/ThemeManager.cs:L55   | Toggles compact rendering layout for
                  |                         |                                              | mobile/small screens
  ──────
  ### 2. Workspace & Registry Management ( WorkspaceRegistry ,  ProfileNavigator )

   Class               | Method Signature         | File Location                             | Purpose / Functionality
  ---------------------|--------------------------|-------------------------------------------|--------------------------------------------
    WorkspaceRegistry  |  SyncAllProjects()       |  Infrastructure/Registries/WorkspaceRegist| Scans root search paths and updates
                       |                          | ry.cs:L320                                | workspace cache
    WorkspaceRegistry  |  PruneWorkspaces()       |  Infrastructure/Registries/WorkspaceRegist| Removes non-existent paths from workspace
                       |                          | ry.cs:L106                                | registry
    WorkspaceRegistry  |  AutoDiscoverWorkspaces()|  Infrastructure/Registries/WorkspaceRegist| Discovers unregistered projects in 
                       |                          | ry.cs:L203                                | $env:PROJECT_BASE_DIR 
    WorkspaceRegistry  |  ManageWorkspaceLinks()  |  Infrastructure/Registries/WorkspaceRegist| Interactive editor for project URL/file
                       |                          | ry.cs:L549                                | bookmarks
    ProfileNavigator   |  OpenProfileDirectory()  |  Infrastructure/Registries/WorkspaceRegist| Opens PowerShell profile directory in
                       |                          | ry.cs:L637                                | Explorer
  ──────
  ### 3. Vault & Account Store Operations ( AgyVault ,  AgyAccountStore ,  SubPageAccountNavigator )

   Class                     | Method Signature          | File Location                          | Purpose / Functionality
  ---------------------------|---------------------------|----------------------------------------|----------------------------------------
    AgyVault                 |  ListSecrets()            |  Infrastructure/Integrations/AgyClient/| Lists all stored DPAPI secrets
                             |                           | AgyVault.cs:L241                       |
    AgyVault                 |  SyncActiveAccountWithKeyr|  Infrastructure/Integrations/AgyClient/| Synchronizes active account token with
                             | ing()                     | AgyVault.cs:L116                       | OS keyring
    AgyAccountStore          |  ExportAccountData()      |  Infrastructure/Integrations/AgyClient/| Exports account metadata & request
                             |                           | AgyAccountStore.cs:L180                | history to JSON
    SubPageAccountNavigator  |  LoginAccount()           |  UI/Core/Navigation/SubPageAccountNavig| Interactive login flow for new
                             |                           | ator.cs:L99                            | developer account
    SubPageAccountNavigator  |  LogoutAccount()          |  UI/Core/Navigation/SubPageAccountNavig| Purges credentials for active account
                             |                           | ator.cs:L129                           |
  ──────
  ### 4. Learning, Study & Obsidian Engine ( ObsidianClient ,  StudyConsoleView ,  SpacedRepetitionEngine )

   Class              | Method Signature    | File Location                                      | Purpose / Functionality
  --------------------|---------------------|----------------------------------------------------|-----------------------------------------
    ObsidianClient    |  ShowDailyNote()    |  Infrastructure/Integrations/Obsidian/ObsidianClien| Opens today's Obsidian daily note
                      |                     | t.cs:L124                                          |
    ObsidianClient    |  ListByTag()        |  Infrastructure/Integrations/Obsidian/ObsidianClien| Filters Obsidian markdown notes by
                      |                     | t.cs:L135                                          | #tag 
    ObsidianClient    |  ShowOrphans()      |  Infrastructure/Integrations/Obsidian/ObsidianClien| Finds notes with zero inbound/outbound
                      |                     | t.cs:L276                                          | wikilinks
    StudyConsoleView  |  ShowWeeklyChart()  |  UI/Screens/Learn/StudyConsoleView.cs:L29          | Renders ASCII bar chart of weekly study
                      |                     |                                                    | hours
    StudyConsoleView  |  ShowMasteryTree()  |  UI/Screens/Learn/StudyConsoleView.cs:L332         | Displays topic mastery tree view
    WeakItemsQueue    |  ClearWeakItems()   |  UI/Screens/Learn/StudyConsoleView.cs:L411         | Resets queue of weak study items
  ──────
  ### 5. Git, Repository & IDE Components ( GitNexus ,  TerminalIde ,  SymbolSearch )

   Class          | Method Signature       | File Location                        | Purpose / Functionality
  ----------------|------------------------|--------------------------------------|--------------------------------------------------------
    GitNexus      |  ShowBranchTree(workspa|  UI/Screens/Git/GitNexus.cs:L172     | Displays multi-repository branch tree graph
                  | ces)                   |                                      |
    TerminalIde   |  UpdateAgyContext()    |  UI/Screens/Ide/TerminalIde.cs:L424  | Updates  .agy-context.md  with active workspace
                  |                        |                                      | context
    SymbolSearch  |  BrowseSymbols()       |  UI/Screens/Ide/SymbolSearch.cs:L7   | Interactive symbol browser across  .cs / .ps1  files
    CodeViewer    |  ViewFile(path)        |  UI/Screens/Ide/CodeViewer.cs:L6     | Full-screen syntax-highlighted code file viewer

──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
>