# Powershell Custom Commands Reference

This document lists all the custom functions and short aliases available in the environment, parsed directly from [Aliases.ps1](file:///C:/Users/TruongNhon/Documents/Powershell/Profile/Core/Aliases.ps1).

> [!NOTE]
> You can run these commands using either their short **Alias** or their full descriptive **PowerShell Function** name.

## Visual Menu and Submenu Tree

```
Profile CLI Menu
|__ 1. Workspace & Navigation
|   |__ Shortcuts
|   |   |__ .. (Set-LocationParent) - Navigate up one directory level
|   |   |__ ... (Set-LocationGrandParent) - Navigate up two directory levels
|   |   |__ f (Invoke-OpenExplorer) - Open current directory in Windows File Explorer
|   |   |__ mkcd (New-DirAndEnter) - Create a new directory and navigate into it
|   |__ Launchers
|   |   |__ - (Set-DesktopThemeMode) - PowerShell helper function
|   |   |__ - (Set-MobileThemeMode) - PowerShell helper function
|   |   |__ clone-project (Clone-Project) - Clone a repository and register it in the project workspace cache
|   |   |__ ide (Invoke-TerminalIde) - Launch Micro/NeoVim terminal IDE in current workspace
|   |   |__ new-project (Invoke-ProjectScaffolder) - Scaffold a new project from pre-defined templates
|   |   |__ proj (Enter-Project) - Jump to project workspace (launches TUI on conflict)
|   |   |__ theme (Select-ShellTheme) - Interactive Oh My Posh theme switcher TUI
|__ 2. Development Tools
|   |__ Git Version Control
|   |   |__ - (Get-GitLog) - Shows git log output
|   |   |__ co (Invoke-GitBranchCheckout) - Checkout branch (runs interactive TUI selection)
|   |   |__ co (Invoke-GitCheckout) - Checkout git branch
|   |   |__ cob (New-GitBranch) - Checkout new git branch
|   |   |__ ga (Invoke-GitAddAll) - Stages all files (git add .)
|   |   |__ gb (Get-GitBranches) - Shows git branches
|   |   |__ gbd (Remove-GitBranch) - Delete git branch
|   |   |__ gca (Invoke-GitAmend) - Amend modifications into the last commit
|   |   |__ gcmt (Invoke-GitCommit) - Commit changes with message
|   |   |__ gcmt (Invoke-GitCommitWizard) - Commit changes using interactive commit wizard
|   |   |__ gd (Show-GitDiff) - Shows git diff
|   |   |__ gf (Invoke-GitFetch) - Fetch updates from remote
|   |   |__ glg (Get-GitLogGraph) - Shows git log graph
|   |   |__ glog (Get-GitLogPretty) - Shows git log prettified list
|   |   |__ gms (Invoke-GitMergeSquash) - Squash merge target branch
|   |   |__ gpu (Invoke-GitPull) - Pull updates from remote
|   |   |__ gr (Invoke-GitResetSoft) - Soft reset to commit
|   |   |__ grh (Invoke-GitResetHard) - Hard reset to branch
|   |   |__ gs (Get-GitStatus) - Shows git status
|   |   |__ gsnap (Invoke-GitStashSnapshot) - Create stash snapshot checkpoint
|   |   |__ guf (Invoke-GitPushForce) - Force push commits to remote
|   |   |__ gundo (Invoke-GitUndo) - Undo the last commit (keep changes)
|   |   |__ gunstage (Invoke-GitUnstage) - Unstage currently staged changes
|   |   |__ gus (Invoke-GitPush) - Push commits to remote
|   |__ .NET SDK Tools
|   |   |__ console (New-ConsoleProject) - Create a new .NET Console application
|   |   |__ da (Add-Migration) - Add an Entity Framework Core migration
|   |   |__ db (Invoke-DotNetBuild) - Builds the current .NET project
|   |   |__ dcl (Invoke-DotNetClean) - Run dotnet clean
|   |   |__ dclean (Remove-BinObj) - Remove all bin/ and obj/ folders recursively
|   |   |__ dd (Remove-Database) - Drop database target
|   |   |__ df (Invoke-DotNetFormat) - Formats .NET codebase style rules
|   |   |__ dr (Invoke-DotNetRun) - Runs the current .NET project
|   |   |__ dremove (Remove-Migration) - Rollback last database migration
|   |   |__ dres (Invoke-DotNetRestore) - Restore project dependencies
|   |   |__ dt (Invoke-DotNetTest) - Runs unit tests
|   |   |__ du (Update-Database) - Apply migrations to database target
|   |   |__ dw (Invoke-DotNetWatch) - Watch project for changes
|   |   |__ sln (New-Solution) - Create a new solution file (.sln)
|   |   |__ sln-add (Add-AllProjectsToSolution) - Add all .csproj files to solution recursively
|   |   |__ webapi (New-WebApiProject) - Create a new .NET Web API application
|   |   |__ wt (Invoke-DotNetWatchTest) - Watch project tests for changes
|   |__ Docker Stacks
|   |   |__ dkcl (Get-DockerContainers) - Lists all running Docker containers
|   |   |__ dkcl (Invoke-DockerDashboard) - Run container dashboard TUI
|   |   |__ dkcpd (Invoke-ComposeDown) - Runs docker-compose down
|   |   |__ dkcpu (Invoke-ComposeUp) - Runs docker-compose up
|   |   |__ dkcpub (Invoke-ComposeUpBuild) - Rebuild and run compose container stack
|   |   |__ dkrmac (Remove-AllDockerContainers) - Force delete all containers
|   |   |__ dkstac (Stop-AllDockerContainers) - Force stop all running containers
|   |   |__ fix-image (Remove-UnusedDockerImages) - Prune unused Docker images
|   |   |__ fix-volume (Remove-UnusedDockerVolumes) - Prune all dangling Docker volumes
|   |__ AWS LocalStack
|   |   |__ lbls (Get-LambdaFunctions) - List LocalStack Lambda functions
|   |   |__ s3ls (Get-S3Buckets) - List LocalStack S3 buckets
|   |   |__ s3mb (New-S3Bucket) - Create LocalStack S3 bucket
|   |   |__ sqsattr (Get-LocalSQSAttributes) - Get LocalStack SQS attributes
|   |   |__ sqsls (Get-LocalSQSQueues) - List LocalStack SQS queues
|   |   |__ sqsmb (New-LocalSQSQueue) - Create LocalStack SQS queue
|   |   |__ sqspurge (Clear-LocalSQSQueue) - Purge LocalStack SQS queue
|   |   |__ sqsrecv (Get-LocalSQSMessage) - Receive message from LocalStack SQS queue
|   |   |__ sqssend (Send-LocalSQSMessage) - Send message to LocalStack SQS queue
|__ 3. System & Network Operations
|   |__ System Administration
|   |   |__ - (caws) - Quick category reference guide for AWS commands
|   |   |__ - (cdk) - Quick category reference guide for Docker commands
|   |   |__ - (cg) - Quick category reference guide for Git commands
|   |   |__ - (cnav) - Quick category reference guide for Navigation commands
|   |   |__ - (cnet) - Quick category reference guide for .NET commands
|   |   |__ - (csys) - Quick category reference guide for System Utilities
|   |   |__ - (Invoke-CcSearch) - PowerShell helper function
|   |   |__ - (Invoke-Npm) - Wrapper running local npm client
|   |   |__ ai (Invoke-MultiAgent) - Unified TUI AI Agent Selector menu
|   |   |__ cc (Get-CustomCommands) - Access the interactive TUI profile manual
|   |   |__ clh (Clear-ShellHistory) - Clear all command history and purge history files
|   |   |__ db-tui (Invoke-DbTui) - Start SQLite database Terminal UI file browser
|   |   |__ go (Reload-Profile) - Reload the current PowerShell profile session
|   |   |__ kill (Stop-ProcessFriendly) - Friendly process termination selector
|   |   |__ killport (Invoke-KillPort) - Kill any process listening on a specific local TCP port
|   |   |__ logstream (Invoke-LogStream) - Stream and monitor active log files in real-time
|   |   |__ mobile (Toggle-MobileMode) - Toggle Oh My Posh shell configuration mobile layout mode
|   |   |__ myip (Get-PublicIP) - Resolve and print current public IPv4 address
|   |   |__ sec (Invoke-SecretVault) - Manage isolated credentials in the local secure vault
|   |   |__ sysmon (Invoke-SystemMonitor) - Launch basic system performance and resource monitor
|   |   |__ tree (Get-FileTree) - Display printout of folder tree structure
|   |   |__ usage (Get-DiskSpace) - Display partition utilization statistics
|   |__ SSH Management
|   |   |__ - (cssh) - Quick category reference guide for SSH commands
|   |   |__ ssh-addkey (Add-SshAuthorizedKey) - Authorize a public SSH key for passwordless login
|   |   |__ ssh-addkey-mobile (Start-MobileSshKeyReceiver) - Start background receiver script to sync mobile keys
|   |   |__ ssh-info (Get-SshConnectionInfo) - Display Tailscale connection details & active port 22 SSH sessions
|__ 4. AI & Profile Contexts
|   |__ AI Interface (Chat & Query)
|   |   |__ - (cai) - Quick category reference guide for AI Tools
|   |   |__ - (Install-AIIntegrations) - Setup tool installing local LLM wrappers and settings
|   |   |__ - (Invoke-ChatGPT) - Launch ChatGPT CLI (routes to OpenClaw if not installed)
|   |   |__ - (Invoke-CopilotExplain) - Query GitHub Copilot CLI helper to explain commands
|   |   |__ ai-dash (Show-AiDashboard) - Launch TUI selector for local AI Ollama agents
|   |   |__ ask-ai (Invoke-AskAi) - Quick query tool to ask local model questions
|   |__ AI Ollama Assistants
|   |   |__ - (Ensure-OllamaServer) - Internal hook checking and prompting for Ollama service
|   |   |__ - (Initialize-OllamaServer) - Internal initialization routine starting local Ollama
|   |   |__ claude (Invoke-Claude-By-Ollama) - Launch Anthropic Claude Code via local Ollama
|   |   |__ clawdbot (Invoke-Clawdbot-By-Ollama) - Launch Clawdbot AI helper
|   |   |__ codex (Invoke-Codex-By-Ollama) - Launch Codex CLI via local Ollama
|   |   |__ hermes (Invoke-Hermes-By-Ollama) - Launch Hermes local reasoning LLM console
|   |   |__ hermesd (Invoke-HermesDesktop-By-Ollama) - Launch Hermes reasoning LLM on Desktop
|   |   |__ model (Set-OllamaModel) - Configure default local Ollama model
|   |   |__ ollama-logs (Invoke-OllamaLogs) - Ensure local Ollama server is running and view logs
|   |   |__ openclaw (Invoke-OpenClaw-By-Ollama) - Launch OpenClaw CLI local agent
|   |__ Antigravity Contexts
|   |   |__ - (agy) - Invoke agy CLI under isolated context
|   |   |__ - (multigravity) - Run multigravity multi-profile orchestration CLI
|   |   |__ acc (Invoke-AccountSession) - Select/run commands under isolated account environment
|   |   |__ acc-sum (Show-AccountsSummary) - Display active status and statistics for all accounts
|   |   |__ agy-acc (Invoke-AgyAccount) - Manage isolated Antigravity accounts, credentials, and directories
|   |   |__ agy-m (Invoke-AgyMenu) - Manage Antigravity Accounts & Credentials TUI menu
|   |   |__ autoswitch (Toggle-AutoSwitch) - Toggle automatic account switching on directory change
|   |   |__ mgr (Start-Manager) - Check dependencies and launch Antigravity Manager project
|   |   |__ prxy (Start-Proxy) - Check dependencies and launch Antigravity Claude Proxy
```

## Category: 1. Workspace & Navigation - Shortcuts

| Alias | PowerShell Function (Full Name) | Description |
|---|---|---|
| `..` | `Set-LocationParent` | Navigate up one directory level |
| `...` | `Set-LocationGrandParent` | Navigate up two directory levels |
| `f` | `Invoke-OpenExplorer` | Open current directory in Windows File Explorer |
| `mkcd` | `New-DirAndEnter` | Create a new directory and navigate into it |

## Category: 1. Workspace & Navigation - Launchers

| Alias | PowerShell Function (Full Name) | Description |
|---|---|---|
| `-` | `Set-DesktopThemeMode` | PowerShell helper function |
| `-` | `Set-MobileThemeMode` | PowerShell helper function |
| `clone-project` | `Clone-Project` | Clone a repository and register it in the project workspace cache |
| `ide` | `Invoke-TerminalIde` | Launch Micro/NeoVim terminal IDE in current workspace |
| `new-project` | `Invoke-ProjectScaffolder` | Scaffold a new project from pre-defined templates |
| `proj` | `Enter-Project` | Jump to project workspace (launches TUI on conflict) |
| `theme` | `Select-ShellTheme` | Interactive Oh My Posh theme switcher TUI |

## Category: 2. Development Tools - Git Version Control

| Alias | PowerShell Function (Full Name) | Description |
|---|---|---|
| `-` | `Get-GitLog` | Shows git log output |
| `co` | `Invoke-GitBranchCheckout` | Checkout branch (runs interactive TUI selection) |
| `co` | `Invoke-GitCheckout` | Checkout git branch |
| `cob` | `New-GitBranch` | Checkout new git branch |
| `ga` | `Invoke-GitAddAll` | Stages all files (git add .) |
| `gb` | `Get-GitBranches` | Shows git branches |
| `gbd` | `Remove-GitBranch` | Delete git branch |
| `gca` | `Invoke-GitAmend` | Amend modifications into the last commit |
| `gcmt` | `Invoke-GitCommit` | Commit changes with message |
| `gcmt` | `Invoke-GitCommitWizard` | Commit changes using interactive commit wizard |
| `gd` | `Show-GitDiff` | Shows git diff |
| `gf` | `Invoke-GitFetch` | Fetch updates from remote |
| `glg` | `Get-GitLogGraph` | Shows git log graph |
| `glog` | `Get-GitLogPretty` | Shows git log prettified list |
| `gms` | `Invoke-GitMergeSquash` | Squash merge target branch |
| `gpu` | `Invoke-GitPull` | Pull updates from remote |
| `gr` | `Invoke-GitResetSoft` | Soft reset to commit |
| `grh` | `Invoke-GitResetHard` | Hard reset to branch |
| `gs` | `Get-GitStatus` | Shows git status |
| `gsnap` | `Invoke-GitStashSnapshot` | Create stash snapshot checkpoint |
| `guf` | `Invoke-GitPushForce` | Force push commits to remote |
| `gundo` | `Invoke-GitUndo` | Undo the last commit (keep changes) |
| `gunstage` | `Invoke-GitUnstage` | Unstage currently staged changes |
| `gus` | `Invoke-GitPush` | Push commits to remote |

## Category: 2. Development Tools - .NET SDK Tools

| Alias | PowerShell Function (Full Name) | Description |
|---|---|---|
| `console` | `New-ConsoleProject` | Create a new .NET Console application |
| `da` | `Add-Migration` | Add an Entity Framework Core migration |
| `db` | `Invoke-DotNetBuild` | Builds the current .NET project |
| `dcl` | `Invoke-DotNetClean` | Run dotnet clean |
| `dclean` | `Remove-BinObj` | Remove all bin/ and obj/ folders recursively |
| `dd` | `Remove-Database` | Drop database target |
| `df` | `Invoke-DotNetFormat` | Formats .NET codebase style rules |
| `dr` | `Invoke-DotNetRun` | Runs the current .NET project |
| `dremove` | `Remove-Migration` | Rollback last database migration |
| `dres` | `Invoke-DotNetRestore` | Restore project dependencies |
| `dt` | `Invoke-DotNetTest` | Runs unit tests |
| `du` | `Update-Database` | Apply migrations to database target |
| `dw` | `Invoke-DotNetWatch` | Watch project for changes |
| `sln` | `New-Solution` | Create a new solution file (.sln) |
| `sln-add` | `Add-AllProjectsToSolution` | Add all .csproj files to solution recursively |
| `webapi` | `New-WebApiProject` | Create a new .NET Web API application |
| `wt` | `Invoke-DotNetWatchTest` | Watch project tests for changes |

## Category: 2. Development Tools - Docker Stacks

| Alias | PowerShell Function (Full Name) | Description |
|---|---|---|
| `dkcl` | `Get-DockerContainers` | Lists all running Docker containers |
| `dkcl` | `Invoke-DockerDashboard` | Run container dashboard TUI |
| `dkcpd` | `Invoke-ComposeDown` | Runs docker-compose down |
| `dkcpu` | `Invoke-ComposeUp` | Runs docker-compose up |
| `dkcpub` | `Invoke-ComposeUpBuild` | Rebuild and run compose container stack |
| `dkrmac` | `Remove-AllDockerContainers` | Force delete all containers |
| `dkstac` | `Stop-AllDockerContainers` | Force stop all running containers |
| `fix-image` | `Remove-UnusedDockerImages` | Prune unused Docker images |
| `fix-volume` | `Remove-UnusedDockerVolumes` | Prune all dangling Docker volumes |

## Category: 2. Development Tools - AWS LocalStack

| Alias | PowerShell Function (Full Name) | Description |
|---|---|---|
| `lbls` | `Get-LambdaFunctions` | List LocalStack Lambda functions |
| `s3ls` | `Get-S3Buckets` | List LocalStack S3 buckets |
| `s3mb` | `New-S3Bucket` | Create LocalStack S3 bucket |
| `sqsattr` | `Get-LocalSQSAttributes` | Get LocalStack SQS attributes |
| `sqsls` | `Get-LocalSQSQueues` | List LocalStack SQS queues |
| `sqsmb` | `New-LocalSQSQueue` | Create LocalStack SQS queue |
| `sqspurge` | `Clear-LocalSQSQueue` | Purge LocalStack SQS queue |
| `sqsrecv` | `Get-LocalSQSMessage` | Receive message from LocalStack SQS queue |
| `sqssend` | `Send-LocalSQSMessage` | Send message to LocalStack SQS queue |

## Category: 3. System & Network Operations - System Administration

| Alias | PowerShell Function (Full Name) | Description |
|---|---|---|
| `-` | `caws` | Quick category reference guide for AWS commands |
| `-` | `cdk` | Quick category reference guide for Docker commands |
| `-` | `cg` | Quick category reference guide for Git commands |
| `-` | `cnav` | Quick category reference guide for Navigation commands |
| `-` | `cnet` | Quick category reference guide for .NET commands |
| `-` | `csys` | Quick category reference guide for System Utilities |
| `-` | `Invoke-CcSearch` | PowerShell helper function |
| `-` | `Invoke-Npm` | Wrapper running local npm client |
| `ai` | `Invoke-MultiAgent` | Unified TUI AI Agent Selector menu |
| `cc` | `Get-CustomCommands` | Access the interactive TUI profile manual |
| `clh` | `Clear-ShellHistory` | Clear all command history and purge history files |
| `db-tui` | `Invoke-DbTui` | Start SQLite database Terminal UI file browser |
| `go` | `Reload-Profile` | Reload the current PowerShell profile session |
| `kill` | `Stop-ProcessFriendly` | Friendly process termination selector |
| `killport` | `Invoke-KillPort` | Kill any process listening on a specific local TCP port |
| `logstream` | `Invoke-LogStream` | Stream and monitor active log files in real-time |
| `mobile` | `Toggle-MobileMode` | Toggle Oh My Posh shell configuration mobile layout mode |
| `myip` | `Get-PublicIP` | Resolve and print current public IPv4 address |
| `sec` | `Invoke-SecretVault` | Manage isolated credentials in the local secure vault |
| `sysmon` | `Invoke-SystemMonitor` | Launch basic system performance and resource monitor |
| `tree` | `Get-FileTree` | Display printout of folder tree structure |
| `usage` | `Get-DiskSpace` | Display partition utilization statistics |

## Category: 3. System & Network Operations - SSH Management

| Alias | PowerShell Function (Full Name) | Description |
|---|---|---|
| `-` | `cssh` | Quick category reference guide for SSH commands |
| `ssh-addkey` | `Add-SshAuthorizedKey` | Authorize a public SSH key for passwordless login |
| `ssh-addkey-mobile` | `Start-MobileSshKeyReceiver` | Start background receiver script to sync mobile keys |
| `ssh-info` | `Get-SshConnectionInfo` | Display Tailscale connection details & active port 22 SSH sessions |

## Category: 4. AI & Profile Contexts - AI Interface (Chat & Query)

| Alias | PowerShell Function (Full Name) | Description |
|---|---|---|
| `-` | `cai` | Quick category reference guide for AI Tools |
| `-` | `Install-AIIntegrations` | Setup tool installing local LLM wrappers and settings |
| `-` | `Invoke-ChatGPT` | Launch ChatGPT CLI (routes to OpenClaw if not installed) |
| `-` | `Invoke-CopilotExplain` | Query GitHub Copilot CLI helper to explain commands |
| `ai-dash` | `Show-AiDashboard` | Launch TUI selector for local AI Ollama agents |
| `ask-ai` | `Invoke-AskAi` | Quick query tool to ask local model questions |

## Category: 4. AI & Profile Contexts - AI Ollama Assistants

| Alias | PowerShell Function (Full Name) | Description |
|---|---|---|
| `-` | `Ensure-OllamaServer` | Internal hook checking and prompting for Ollama service |
| `-` | `Initialize-OllamaServer` | Internal initialization routine starting local Ollama |
| `claude` | `Invoke-Claude-By-Ollama` | Launch Anthropic Claude Code via local Ollama |
| `clawdbot` | `Invoke-Clawdbot-By-Ollama` | Launch Clawdbot AI helper |
| `codex` | `Invoke-Codex-By-Ollama` | Launch Codex CLI via local Ollama |
| `hermes` | `Invoke-Hermes-By-Ollama` | Launch Hermes local reasoning LLM console |
| `hermesd` | `Invoke-HermesDesktop-By-Ollama` | Launch Hermes reasoning LLM on Desktop |
| `model` | `Set-OllamaModel` | Configure default local Ollama model |
| `ollama-logs` | `Invoke-OllamaLogs` | Ensure local Ollama server is running and view logs |
| `openclaw` | `Invoke-OpenClaw-By-Ollama` | Launch OpenClaw CLI local agent |

## Category: 4. AI & Profile Contexts - Antigravity Contexts

| Alias | PowerShell Function (Full Name) | Description |
|---|---|---|
| `-` | `agy` | Invoke agy CLI under isolated context |
| `-` | `multigravity` | Run multigravity multi-profile orchestration CLI |
| `acc` | `Invoke-AccountSession` | Select/run commands under isolated account environment |
| `acc-sum` | `Show-AccountsSummary` | Display active status and statistics for all accounts |
| `agy-acc` | `Invoke-AgyAccount` | Manage isolated Antigravity accounts, credentials, and directories |
| `agy-m` | `Invoke-AgyMenu` | Manage Antigravity Accounts & Credentials TUI menu |
| `autoswitch` | `Toggle-AutoSwitch` | Toggle automatic account switching on directory change |
| `mgr` | `Start-Manager` | Check dependencies and launch Antigravity Manager project |
| `prxy` | `Start-Proxy` | Check dependencies and launch Antigravity Claude Proxy |


