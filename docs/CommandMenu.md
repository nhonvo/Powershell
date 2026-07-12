# Powershell Custom Commands Reference

This document lists all the custom functions and short aliases available in the environment, parsed directly from [Aliases.ps1](file:///C:/Users/TruongNhon/Documents/Powershell/Profile/Core/Aliases.ps1).

> [!NOTE]
> You can run these commands using either their short **Alias** or their full descriptive **PowerShell Function** name.

## Visual Menu and Submenu Tree (Regrouped)

```
Profile CLI Menu
|__ 1. Workspace & Navigation
|   |__ Shortcuts
|   |   |__ .. (Set-LocationParent) - Navigate up one directory level
|   |   |__ ... (Set-LocationGrandParent) - Navigate up two directory levels
|   |   |__ f (Invoke-OpenExplorer) - Open current directory in Windows File Explorer
|   |   |__ mkcd (New-DirAndEnter) - Create a new directory and navigate into it
|   |__ Launchers
|   |   |__ proj / prj (Enter-Project) - Jump to project workspace
|   |   |__ ide (Invoke-TerminalIde) - Launch Micro/NeoVim terminal IDE
|   |   |__ theme (Select-ShellTheme) - Interactive Oh My Posh theme switcher TUI
|   |   |__ clone-project (Clone-Project) - Clone a repository and register it in the project workspace cache
|   |   |__ new-project (Invoke-ProjectScaffolder) - Scaffold a new project from pre-defined templates
|__ 2. Development Tools
|   |__ Git Version Control
|   |   |__ gs (Get-GitStatus) - Shows git status
|   |   |__ gd (Show-GitDiff) - Shows git diff
|   |   |__ glo / glg (Get-GitLogGraph) - Shows git log graph
|   |   |__ glog (Get-GitLogPretty) - Shows git log prettified list
|   |   |__ gb (Get-GitBranches) - Shows git branches
|   |   |__ co (Invoke-GitBranchCheckout) - Checkout branch (runs interactive TUI selection)
|   |   |__ co (Invoke-GitCheckout) - Checkout git branch
|   |   |__ cob (New-GitBranch) - Checkout new git branch
|   |   |__ gbd (Remove-GitBranch) - Delete git branch
|   |   |__ ga (Invoke-GitAddAll) - Stages all files (git add .)
|   |   |__ gunstage (Invoke-GitUnstage) - Unstage currently staged changes
|   |   |__ gcmt (Invoke-GitCommit) - Commit changes with message
|   |   |__ gcmt (Invoke-GitCommitWizard) - Commit changes using interactive commit wizard
|   |   |__ gca (Invoke-GitAmend) - Amend modifications into the last commit
|   |   |__ gundo (Invoke-GitUndo) - Undo the last commit (keep changes)
|   |   |__ gr (Invoke-GitResetSoft) - Soft reset to commit
|   |   |__ grh (Invoke-GitResetHard) - Hard reset to branch
|   |   |__ gf (Invoke-GitFetch) - Fetch updates from remote
|   |   |__ gpu (Invoke-GitPull) - Pull updates from remote
|   |   |__ gus (Invoke-GitPush) - Push commits to remote
|   |   |__ guf (Invoke-GitPushForce) - Force push commits to remote
|   |   |__ gms (Invoke-GitMergeSquash) - Squash merge target branch
|   |   |__ gsnap (Invoke-GitStashSnapshot) - Create stash snapshot checkpoint
|   |   |__ - (Get-GitLog) - Shows git log output
|   |__ .NET SDK Tools
|   |   |__ dr (Invoke-DotNetRun) - Runs the current .NET project
|   |   |__ dw (Invoke-DotNetWatch) - Watch project for changes
|   |   |__ db (Invoke-DotNetBuild) - Builds the current .NET project
|   |   |__ df (Invoke-DotNetFormat) - Formats .NET codebase style rules
|   |   |__ dt (Invoke-DotNetTest) - Runs unit tests
|   |   |__ wt (Invoke-DotNetWatchTest) - Watch project tests for changes
|   |   |__ dcl (Invoke-DotNetClean) - Run dotnet clean
|   |   |__ dres (Invoke-DotNetRestore) - Restore project dependencies
|   |   |__ dclean (Remove-BinObj) - Remove all bin/ and obj/ folders recursively
|   |   |__ da (Add-Migration) - Add an Entity Framework Core migration
|   |   |__ du (Update-Database) - Apply migrations to database target
|   |   |__ dd (Remove-Database) - Drop database target
|   |   |__ dremove (Remove-Migration) - Rollback last database migration
|   |   |__ sln (New-Solution) - Create a new solution file (.sln)
|   |   |__ sln-add (Add-AllProjectsToSolution) - Add all .csproj files to solution recursively
|   |   |__ console (New-ConsoleProject) - Create a new .NET Console application
|   |   |__ webapi (New-WebApiProject) - Create a new .NET Web API application
|   |__ Docker Stacks
|   |   |__ dkcl (Get-DockerContainers) - Lists all running Docker containers
|   |   |__ dkcl (Invoke-DockerDashboard) - Run container dashboard TUI
|   |   |__ dkrmac (Remove-AllDockerContainers) - Force delete all containers
|   |   |__ dkstac (Stop-AllDockerContainers) - Force stop all running containers
|   |   |__ dkcpu (Invoke-ComposeUp) - Runs docker-compose up
|   |   |__ dkcpub (Invoke-ComposeUpBuild) - Rebuild and run compose container stack
|   |   |__ dkcpd (Invoke-ComposeDown) - Runs docker-compose down
|   |   |__ fix-volume (Remove-UnusedDockerVolumes) - Prune all dangling Docker volumes
|   |   |__ fix-image (Remove-UnusedDockerImages) - Prune unused Docker images
|   |__ AWS LocalStack
|   |   |__ s3ls (Get-S3Buckets) - List LocalStack S3 buckets
|   |   |__ lbls (Get-LambdaFunctions) - List LocalStack Lambda functions
|   |   |__ sqsls (Get-LocalSQSQueues) - List LocalStack SQS queues
|   |   |__ s3mb (New-S3Bucket) - Create LocalStack S3 bucket
|   |   |__ sqsmb (New-LocalSQSQueue) - Create LocalStack SQS queue
|   |   |__ sqspurge (Clear-LocalSQSQueue) - Purge LocalStack SQS queue
|   |   |__ sqssend (Send-LocalSQSMessage) - Send message to LocalStack SQS queue
|   |   |__ sqsrecv (Get-LocalSQSMessage) - Receive message from LocalStack SQS queue
|   |   |__ sqsattr (Get-LocalSQSAttributes) - Get LocalStack SQS attributes
|__ 3. System & Network Operations
|   |__ System Administration
|   |   |__ go (Reload-Profile) - Reload the current PowerShell profile session
|   |   |__ usage (Get-DiskSpace) - Display partition utilization statistics
|   |   |__ kill (Stop-ProcessFriendly) - Friendly process termination selector
|   |   |__ myip (Get-PublicIP) - Resolve and print current public IPv4 address
|   |   |__ tree (Get-FileTree) - Display printout of folder tree structure
|   |   |__ commands / cc (Get-CustomCommands) - Access the interactive TUI profile manual
|   |   |__ clh (Clear-ShellHistory) - Clear all command history and purge history files
|   |   |__ killport (Invoke-KillPort) - Kill any process listening on a specific local TCP port
|   |   |__ sysmon (Invoke-SystemMonitor) - Launch basic system performance and resource monitor
|   |   |__ mobile (Toggle-MobileMode) - Toggle Oh My Posh shell configuration mobile layout mode
|   |   |__ db-tui (Invoke-DbTui) - Start SQLite database Terminal UI file browser
|   |   |__ logstream (Invoke-LogStream) - Stream and monitor active log files in real-time
|   |   |__ sec (Invoke-SecretVault) - Manage isolated credentials in the local secure vault
|   |   |__ - (Ensure-OllamaServer) - Internal hook checking and prompting for Ollama service
|   |   |__ - (Initialize-OllamaServer) - Internal initialization routine starting local Ollama
|   |   |__ - (Install-AIIntegrations) - Setup tool installing local LLM wrappers and settings
|   |   |__ - (Invoke-CopilotExplain) - Query GitHub Copilot CLI helper to explain commands
|   |   |__ - (Invoke-Npm) - Wrapper running local npm client
|   |   |__ - (cai / caws / cdk / cg / cnav / cnet / csys) - Quick category manual filters
|   |__ SSH Management
|   |   |__ ssh-info (Get-SshConnectionInfo) - Display Tailscale connection details & active port 22 SSH sessions
|   |   |__ ssh-addkey (Add-SshAuthorizedKey) - Authorize a public SSH key for passwordless login
|   |   |__ ssh-addkey-mobile (Start-MobileSshKeyReceiver) - Start background receiver script to sync mobile keys
|   |   |__ - (cssh) - Quick category manual filter for SSH commands
|__ 4. AI & Profile Contexts
|   |__ AI Interface (Chat & Query)
|   |   |__ ai (Invoke-MultiAgent) - Unified TUI AI Agent Selector menu
|   |   |__ ask-ai (Invoke-AskAi) - Quick query tool to ask local model questions
|   |   |__ openclaw (Invoke-ChatGPT) - Launch ChatGPT CLI (routes to OpenClaw if not installed)
|   |   |__ openclaw (Invoke-OpenClaw-By-Ollama) - Launch OpenClaw CLI local agent
|   |__ AI Ollama Assistants
|   |   |__ claude (Invoke-Claude-By-Ollama) - Launch Anthropic Claude Code via local Ollama
|   |   |__ codex (Invoke-Codex-By-Ollama) - Launch Codex CLI via local Ollama
|   |   |__ clawdbot (Invoke-Clawdbot-By-Ollama) - Launch Clawdbot AI helper
|   |   |__ hermes (Invoke-Hermes-By-Ollama) - Launch Hermes local reasoning LLM console
|   |   |__ hermesd (Invoke-HermesDesktop-By-Ollama) - Launch Hermes reasoning LLM on Desktop
|   |   |__ model (Set-OllamaModel) - Configure default local Ollama model
|   |   |__ ollama-logs (Invoke-OllamaLogs) - Ensure local Ollama server is running and view logs
|   |   |__ ai-dash (Show-AiDashboard) - Launch TUI selector for local AI Ollama agents
|   |__ Antigravity Contexts
|   |   |__ agy-account / agy-acc (Invoke-AgyAccount) - Manage isolated Antigravity accounts, credentials, and directories
|   |   |__ agy-m (Invoke-AgyMenu) - Manage Antigravity Accounts & Credentials TUI menu
|   |   |__ acc (Invoke-AccountSession) - Select/run commands under isolated account environment
|   |   |__ agy (agy) - Invoke agy CLI under isolated context
|   |   |__ multigravity (multigravity) - Run multigravity multi-profile orchestration CLI
|   |   |__ mgr (Start-Manager) - Check dependencies and launch Antigravity Manager project
|   |   |__ prxy (Start-Proxy) - Check dependencies and launch Antigravity Claude Proxy
|   |   |__ autoswitch (Toggle-AutoSwitch) - Toggle automatic account switching on directory change
|   |   |__ acc-sum (Show-AccountsSummary) - Display active status and statistics for all accounts
```

## Category: Workspace & Navigation

| Alias | PowerShell Function (Full Name) | Description | In TUI Menu |
|---|---|---|---|
| `..` | `Set-LocationParent` | Navigate up one directory level | Yes |
| `...` | `Set-LocationGrandParent` | Navigate up two directory levels | Yes |
| `clone-project` | `Clone-Project` | Clone a repository and register it in the project workspace cache | Yes |
| `f` | `Invoke-OpenExplorer` | Open current directory in Windows File Explorer | Yes |
| `ide` | `Invoke-TerminalIde` | Launch Micro/NeoVim terminal IDE in current workspace | Yes |
| `mkcd` | `New-DirAndEnter` | Create a new directory and navigate into it | Yes |
| `new-project` | `Invoke-ProjectScaffolder` | Scaffold a new project from pre-defined templates | Yes |
| `proj` | `Enter-Project` | Jump to project workspace (launches TUI on conflict) | Yes |
| `theme` | `Select-ShellTheme` | Interactive Oh My Posh theme switcher TUI | Yes |

## Category: Development Tools

| Alias | PowerShell Function (Full Name) | Description | In TUI Menu |
|---|---|---|---|
| `-` | `Get-GitLog` | Shows git log output | Yes |
| `co` | `Invoke-GitBranchCheckout` | Checkout branch (runs interactive TUI selection) | Yes |
| `co` | `Invoke-GitCheckout` | Checkout git branch | Yes |
| `cob` | `New-GitBranch` | Checkout new git branch | Yes |
| `console` | `New-ConsoleProject` | Create a new .NET Console application | Yes |
| `da` | `Add-Migration` | Add an Entity Framework Core migration | Yes |
| `db` | `Invoke-DotNetBuild` | Builds the current .NET project | Yes |
| `dcl` | `Invoke-DotNetClean` | Run dotnet clean | Yes |
| `dclean` | `Remove-BinObj` | Remove all bin/ and obj/ folders recursively | Yes |
| `dd` | `Remove-Database` | Drop database target | Yes |
| `df` | `Invoke-DotNetFormat` | Formats .NET codebase style rules | Yes |
| `dkcl` | `Get-DockerContainers` | Lists all running Docker containers | Yes |
| `dkcl` | `Invoke-DockerDashboard` | Run container dashboard TUI | Yes |
| `dkcpd` | `Invoke-ComposeDown` | Runs docker-compose down | Yes |
| `dkcpu` | `Invoke-ComposeUp` | Runs docker-compose up | Yes |
| `dkcpub` | `Invoke-ComposeUpBuild` | Rebuild and run compose container stack | Yes |
| `dkrmac` | `Remove-AllDockerContainers` | Force delete all containers | Yes |
| `dkstac` | `Stop-AllDockerContainers` | Force stop all running containers | Yes |
| `dr` | `Invoke-DotNetRun` | Runs the current .NET project | Yes |
| `dremove` | `Remove-Migration` | Rollback last database migration | Yes |
| `dres` | `Invoke-DotNetRestore` | Restore project dependencies | Yes |
| `dt` | `Invoke-DotNetTest` | Runs unit tests | Yes |
| `du` | `Update-Database` | Apply migrations to database target | Yes |
| `dw` | `Invoke-DotNetWatch` | Watch project for changes | Yes |
| `fix-image` | `Remove-UnusedDockerImages` | Prune unused Docker images | Yes |
| `fix-volume` | `Remove-UnusedDockerVolumes` | Prune all dangling Docker volumes | Yes |
| `ga` | `Invoke-GitAddAll` | Stages all files (git add .) | Yes |
| `gb` | `Get-GitBranches` | Shows git branches | Yes |
| `gbd` | `Remove-GitBranch` | Delete git branch | Yes |
| `gca` | `Invoke-GitAmend` | Amend modifications into the last commit | Yes |
| `gcmt` | `Invoke-GitCommit` | Commit changes with message | Yes |
| `gcmt` | `Invoke-GitCommitWizard` | Commit changes using interactive commit wizard | Yes |
| `gd` | `Show-GitDiff` | Shows git diff | Yes |
| `gf` | `Invoke-GitFetch` | Fetch updates from remote | Yes |
| `glg` | `Get-GitLogGraph` | Shows git log graph | Yes |
| `glog` | `Get-GitLogPretty` | Shows git log prettified list | Yes |
| `gms` | `Invoke-GitMergeSquash` | Squash merge target branch | Yes |
| `gpu` | `Invoke-GitPull` | Pull updates from remote | Yes |
| `gr` | `Invoke-GitResetSoft` | Soft reset to commit | Yes |
| `grh` | `Invoke-GitResetHard` | Hard reset to branch | Yes |
| `gs` | `Get-GitStatus` | Shows git status | Yes |
| `gsnap` | `Invoke-GitStashSnapshot` | Create stash snapshot checkpoint | Yes |
| `guf` | `Invoke-GitPushForce` | Force push commits to remote | Yes |
| `gundo` | `Invoke-GitUndo` | Undo the last commit (keep changes) | Yes |
| `gunstage` | `Invoke-GitUnstage` | Unstage currently staged changes | Yes |
| `gus` | `Invoke-GitPush` | Push commits to remote | Yes |
| `lbls` | `Get-LambdaFunctions` | List LocalStack Lambda functions | Yes |
| `s3ls` | `Get-S3Buckets` | List LocalStack S3 buckets | Yes |
| `s3mb` | `New-S3Bucket` | Create LocalStack S3 bucket | Yes |
| `sln` | `New-Solution` | Create a new solution file (.sln) | Yes |
| `sln-add` | `Add-AllProjectsToSolution` | Add all .csproj files to solution recursively | Yes |
| `sqsattr` | `Get-LocalSQSAttributes` | Get LocalStack SQS attributes | Yes |
| `sqsls` | `Get-LocalSQSQueues` | List LocalStack SQS queues | Yes |
| `sqsmb` | `New-LocalSQSQueue` | Create LocalStack SQS queue | Yes |
| `sqspurge` | `Clear-LocalSQSQueue` | Purge LocalStack SQS queue | Yes |
| `sqsrecv` | `Get-LocalSQSMessage` | Receive message from LocalStack SQS queue | Yes |
| `sqssend` | `Send-LocalSQSMessage` | Send message to LocalStack SQS queue | Yes |
| `webapi` | `New-WebApiProject` | Create a new .NET Web API application | Yes |
| `wt` | `Invoke-DotNetWatchTest` | Watch project tests for changes | Yes |

## Category: System & Network Operations

| Alias | PowerShell Function (Full Name) | Description | In TUI Menu |
|---|---|---|---|
| `-` | `cssh` | Quick category reference guide for SSH commands | No |
| `cc` | `Get-CustomCommands` | Access the interactive TUI profile manual | Yes |
| `clh` | `Clear-ShellHistory` | Clear all command history and purge history files | Yes |
| `db-tui` | `Invoke-DbTui` | Start SQLite database Terminal UI file browser | Yes |
| `go` | `Reload-Profile` | Reload the current PowerShell profile session | Yes |
| `kill` | `Stop-ProcessFriendly` | Friendly process termination selector | Yes |
| `killport` | `Invoke-KillPort` | Kill any process listening on a specific local TCP port | Yes |
| `logstream` | `Invoke-LogStream` | Stream and monitor active log files in real-time | Yes |
| `mobile` | `Toggle-MobileMode` | Toggle Oh My Posh shell configuration mobile layout mode | Yes |
| `myip` | `Get-PublicIP` | Resolve and print current public IPv4 address | Yes |
| `sec` | `Invoke-SecretVault` | Manage isolated credentials in the local secure vault | Yes |
| `ssh-addkey` | `Add-SshAuthorizedKey` | Authorize a public SSH key for passwordless login | Yes |
| `ssh-addkey-mobile` | `Start-MobileSshKeyReceiver` | Start background receiver script to sync mobile keys | Yes |
| `ssh-info` | `Get-SshConnectionInfo` | Display Tailscale connection details & active port 22 SSH sessions | Yes |
| `sysmon` | `Invoke-SystemMonitor` | Launch basic system performance and resource monitor | Yes |
| `tree` | `Get-FileTree` | Display printout of folder tree structure | Yes |
| `usage` | `Get-DiskSpace` | Display partition utilization statistics | Yes |
| `-` | `cai` | Quick category reference guide for AI Tools | No |
| `-` | `caws` | Quick category reference guide for AWS commands | No |
| `-` | `cdk` | Quick category reference guide for Docker commands | No |
| `-` | `cg` | Quick category reference guide for Git commands | No |
| `-` | `cnav` | Quick category reference guide for Navigation commands | No |
| `-` | `cnet` | Quick category reference guide for .NET commands | No |
| `-` | `csys` | Quick category reference guide for System Utilities | No |
| `-` | `Ensure-OllamaServer` | Internal hook checking and prompting for Ollama service | No |
| `-` | `Initialize-OllamaServer` | Internal initialization routine starting local Ollama | No |
| `-` | `Install-AIIntegrations` | Setup tool installing local LLM wrappers and settings | No |
| `-` | `Invoke-CopilotExplain` | Query GitHub Copilot CLI helper to explain commands | No |
| `-` | `Invoke-Npm` | Wrapper running local npm client | No |

## Category: AI & Profile Contexts

| Alias | PowerShell Function (Full Name) | Description | In TUI Menu |
|---|---|---|---|
| `acc-sum` | `Show-AccountsSummary` | Display active status and statistics for all accounts | Yes |
| `agy` | `agy` | Invoke agy CLI under isolated context | Yes |
| `agy-acc` | `Invoke-AgyAccount` | Manage isolated Antigravity accounts, credentials, and directories | Yes |
| `ai` | `Invoke-MultiAgent` | Unified TUI AI Agent Selector menu | Yes |
| `ai-dash` | `Show-AiDashboard` | Launch TUI selector for local AI Ollama agents | Yes |
| `ask-ai` | `Invoke-AskAi` | Quick query tool to ask local model questions | Yes |
| `autoswitch` | `Toggle-AutoSwitch` | Toggle automatic account switching on directory change | Yes |
| `claude` | `Invoke-Claude-By-Ollama` | Launch Anthropic Claude Code via local Ollama | Yes |
| `clawdbot` | `Invoke-Clawdbot-By-Ollama` | Launch Clawdbot AI helper | Yes |
| `codex` | `Invoke-Codex-By-Ollama` | Launch Codex CLI via local Ollama | Yes |
| `hermes` | `Invoke-Hermes-By-Ollama` | Launch Hermes local reasoning LLM console | Yes |
| `hermesd` | `Invoke-HermesDesktop-By-Ollama` | Launch Hermes reasoning LLM on Desktop | Yes |
| `mgr` | `Start-Manager` | Check dependencies and launch Antigravity Manager project | Yes |
| `model` | `Set-OllamaModel` | Configure default local Ollama model | Yes |
| `multigravity` | `multigravity` | Run multigravity multi-profile orchestration CLI | Yes |
| `ollama-logs` | `Invoke-OllamaLogs` | Ensure local Ollama server is running and view logs | Yes |
| `openclaw` | `Invoke-OpenClaw-By-Ollama` | Launch OpenClaw CLI local agent | Yes |
| `prxy` | `Start-Proxy` | Check dependencies and launch Antigravity Claude Proxy | Yes |
| `-` | `Invoke-ChatGPT` | Launch ChatGPT CLI (routes to OpenClaw if not installed) | No |
| `acc` | `Invoke-AccountSession` | Select/run commands under isolated account environment | No |
| `agy-m` | `Invoke-AgyMenu` | Manage Antigravity Accounts & Credentials TUI menu | No |


