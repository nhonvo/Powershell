#region PROFILE HELP
# ==============================================================================
#  Exposes interactive help documentation for all custom commands.
# ==============================================================================

class CommandDoc {
    [string]$Alias
    [string]$FullName
    [string]$Desc
    [string]$Command
}

class ProfileHelp {
    static [System.Collections.Generic.Dictionary[string, CommandDoc[]]] GetCommands() {
        $dict = [System.Collections.Generic.Dictionary[string, CommandDoc[]]]::new()

        $dict["Workspace & Navigation"] = @(
            [CommandDoc]@{ Alias = "..";      FullName = "Set-LocationParent";      Desc = "Navigate up one directory level"; Command = "Set-LocationParent" }
            [CommandDoc]@{ Alias = "...";     FullName = "Set-LocationGrandParent"; Desc = "Navigate up two directory levels"; Command = "Set-LocationGrandParent" }
            [CommandDoc]@{ Alias = "proj";     FullName = "Enter-Project";           Desc = "Jump to project workspace (launches TUI on conflict)"; Command = "Enter-Project" }
            [CommandDoc]@{ Alias = "f";       FullName = "Invoke-OpenExplorer";     Desc = "Open current directory in Windows File Explorer"; Command = "Invoke-OpenExplorer" }
            [CommandDoc]@{ Alias = "mkcd";    FullName = "New-DirAndEnter";         Desc = "Create a new directory and navigate into it"; Command = "New-DirAndEnter" }
            [CommandDoc]@{ Alias = "theme";   FullName = "Select-ShellTheme";       Desc = "Interactive Oh My Posh theme switcher TUI"; Command = "Select-ShellTheme" }
            [CommandDoc]@{ Alias = "ide";     FullName = "Invoke-TerminalIde";       Desc = "Launch Micro/NeoVim terminal IDE in current workspace"; Command = "Invoke-TerminalIde" }
            [CommandDoc]@{ Alias = "clone-project"; FullName = "Clone-Project";      Desc = "Clone a repository and register it in the project workspace cache"; Command = "Clone-Project" }
            [CommandDoc]@{ Alias = "new-project";   FullName = "Invoke-ProjectScaffolder"; Desc = "Scaffold a new project from pre-defined templates"; Command = "Invoke-ProjectScaffolder" }
        )

        $dict["System & Network Operations"] = @(
            [CommandDoc]@{ Alias = "go";       FullName = "Reload-Profile";          Desc = "Reload the current PowerShell profile session"; Command = "Reload-Profile" }
            [CommandDoc]@{ Alias = "usage";   FullName = "Get-DiskSpace";           Desc = "Display partition utilization statistics"; Command = "Get-DiskSpace" }
            [CommandDoc]@{ Alias = "kill";    FullName = "Stop-ProcessFriendly";     Desc = "Friendly process termination selector"; Command = "Stop-ProcessFriendly" }
            [CommandDoc]@{ Alias = "myip";    FullName = "Get-PublicIP";            Desc = "Resolve and print current public IPv4 address"; Command = "Get-PublicIP" }
            [CommandDoc]@{ Alias = "tree";    FullName = "Get-FileTree";            Desc = "Display printout of folder tree structure"; Command = "Get-FileTree" }
            [CommandDoc]@{ Alias = "commands"; FullName = "Get-CustomCommands";      Desc = "Access the interactive TUI profile manual"; Command = "Get-CustomCommands" }
            [CommandDoc]@{ Alias = "clh";      FullName = "Clear-ShellHistory";      Desc = "Clear all command history and purge history files"; Command = "Clear-ShellHistory" }
            [CommandDoc]@{ Alias = "ssh-info"; FullName = "Get-SshConnectionInfo";   Desc = "Display Tailscale connection details & active port 22 SSH sessions"; Command = "Get-SshConnectionInfo" }
            [CommandDoc]@{ Alias = "ssh-addkey"; FullName = "Add-SshAuthorizedKey";   Desc = "Authorize a public SSH key for passwordless login"; Command = "Add-SshAuthorizedKey" }
            [CommandDoc]@{ Alias = "killport"; FullName = "Invoke-KillPort";         Desc = "Kill any process listening on a specific local TCP port"; Command = "Invoke-KillPort" }
            [CommandDoc]@{ Alias = "sysmon";   FullName = "Invoke-SystemMonitor";      Desc = "Launch basic system performance and resource monitor"; Command = "Invoke-SystemMonitor" }
            [CommandDoc]@{ Alias = "logstream"; FullName = "Invoke-LogStream";        Desc = "Stream and monitor active log files in real-time"; Command = "Invoke-LogStream" }
            [CommandDoc]@{ Alias = "sec";      FullName = "Invoke-SecretVault";       Desc = "Manage isolated credentials in the local secure vault"; Command = "Invoke-SecretVault" }
            [CommandDoc]@{ Alias = "db-tui";   FullName = "Invoke-DbTui";             Desc = "Start SQLite database Terminal UI file browser"; Command = "Invoke-DbTui" }
            [CommandDoc]@{ Alias = "mobile";   FullName = "Toggle-MobileMode";        Desc = "Toggle Oh My Posh shell configuration mobile layout mode"; Command = "Toggle-MobileMode" }
            [CommandDoc]@{ Alias = "ssh-addkey-mobile"; FullName = "Start-MobileSshKeyReceiver"; Desc = "Start background receiver script to sync mobile keys"; Command = "Start-MobileSshKeyReceiver" }
        )

        $dict["Development Tools"] = @(
            # Git
            [CommandDoc]@{ Alias = "gs";   FullName = "Get-GitStatus";   Desc = "Shows git status"; Command = "Get-GitStatus" }
            [CommandDoc]@{ Alias = "gd";   FullName = "Show-GitDiff";    Desc = "Shows git diff"; Command = "Show-GitDiff" }
            [CommandDoc]@{ Alias = "glo";  FullName = "Get-GitLogGraph";  Desc = "Shows git log graph"; Command = "Get-GitLogGraph" }
            [CommandDoc]@{ Alias = "glog"; FullName = "Get-GitLogPretty"; Desc = "Shows git log prettified list"; Command = "Get-GitLogPretty" }
            [CommandDoc]@{ Alias = "gb";   FullName = "Get-GitBranches";  Desc = "Shows git branches"; Command = "Get-GitBranches" }
            [CommandDoc]@{ Alias = "co";   FullName = "Invoke-GitCheckout"; Desc = "Checkout git branch"; Command = "Invoke-GitCheckout" }
            [CommandDoc]@{ Alias = "co";   FullName = "Invoke-GitBranchCheckout"; Desc = "Checkout branch (runs interactive TUI selection)"; Command = "Invoke-GitBranchCheckout" }
            [CommandDoc]@{ Alias = "cob";  FullName = "New-GitBranch";   Desc = "Checkout new git branch"; Command = "New-GitBranch" }
            [CommandDoc]@{ Alias = "gbd";  FullName = "Remove-GitBranch"; Desc = "Delete git branch"; Command = "Remove-GitBranch" }
            [CommandDoc]@{ Alias = "ga";   FullName = "Invoke-GitAddAll"; Desc = "Stages all files (git add .)"; Command = "Invoke-GitAddAll" }
            [CommandDoc]@{ Alias = "gunstage"; FullName = "Invoke-GitUnstage"; Desc = "Unstage currently staged changes"; Command = "Invoke-GitUnstage" }
            [CommandDoc]@{ Alias = "gcmt"; FullName = "Invoke-GitCommit"; Desc = "Commit changes with message"; Command = "Invoke-GitCommit" }
            [CommandDoc]@{ Alias = "gcmt"; FullName = "Invoke-GitCommitWizard"; Desc = "Commit changes using interactive commit wizard"; Command = "Invoke-GitCommitWizard" }
            [CommandDoc]@{ Alias = "gca";  FullName = "Invoke-GitAmend";  Desc = "Amend modifications into the last commit"; Command = "Invoke-GitAmend" }
            [CommandDoc]@{ Alias = "gundo"; FullName = "Invoke-GitUndo";  Desc = "Undo the last commit (keep changes)"; Command = "Invoke-GitUndo" }
            [CommandDoc]@{ Alias = "gr";   FullName = "Invoke-GitResetSoft"; Desc = "Soft reset to commit"; Command = "Invoke-GitResetSoft" }
            [CommandDoc]@{ Alias = "grh";  FullName = "Invoke-GitResetHard"; Desc = "Hard reset to branch"; Command = "Invoke-GitResetHard" }
            [CommandDoc]@{ Alias = "gf";   FullName = "Invoke-GitFetch";  Desc = "Fetch updates from remote"; Command = "Invoke-GitFetch" }
            [CommandDoc]@{ Alias = "gpu";  FullName = "Invoke-GitPull";   Desc = "Pull updates from remote"; Command = "Invoke-GitPull" }
            [CommandDoc]@{ Alias = "gus";  FullName = "Invoke-GitPush";   Desc = "Push commits to remote"; Command = "Invoke-GitPush" }
            [CommandDoc]@{ Alias = "guf";  FullName = "Invoke-GitPushForce"; Desc = "Force push commits to remote"; Command = "Invoke-GitPushForce" }
            [CommandDoc]@{ Alias = "gms";  FullName = "Invoke-GitMergeSquash"; Desc = "Squash merge target branch"; Command = "Invoke-GitMergeSquash" }
            [CommandDoc]@{ Alias = "gsnap"; FullName = "Invoke-GitStashSnapshot"; Desc = "Create stash snapshot checkpoint"; Command = "Invoke-GitStashSnapshot" }
            [CommandDoc]@{ Alias = "-";   FullName = "Get-GitLog";      Desc = "Shows git log output"; Command = "Get-GitLog" }
            
            # .NET
            [CommandDoc]@{ Alias = "dr";   FullName = "Invoke-DotNetRun";   Desc = "Runs the current .NET project"; Command = "Invoke-DotNetRun" }
            [CommandDoc]@{ Alias = "dw";   FullName = "Invoke-DotNetWatch"; Desc = "Watch project for changes"; Command = "Invoke-DotNetWatch" }
            [CommandDoc]@{ Alias = "db";   FullName = "Invoke-DotNetBuild"; Desc = "Builds the current .NET project"; Command = "Invoke-DotNetBuild" }
            [CommandDoc]@{ Alias = "df";   FullName = "Invoke-DotNetFormat"; Desc = "Formats .NET codebase style rules"; Command = "Invoke-DotNetFormat" }
            [CommandDoc]@{ Alias = "dt";   FullName = "Invoke-DotNetTest";  Desc = "Runs unit tests"; Command = "Invoke-DotNetTest" }
            [CommandDoc]@{ Alias = "wt";   FullName = "Invoke-DotNetWatchTest"; Desc = "Watch project tests for changes"; Command = "Invoke-DotNetWatchTest" }
            [CommandDoc]@{ Alias = "dcl";  FullName = "Invoke-DotNetClean"; Desc = "Run dotnet clean"; Command = "Invoke-DotNetClean" }
            [CommandDoc]@{ Alias = "dres"; FullName = "Invoke-DotNetRestore"; Desc = "Restore project dependencies"; Command = "Invoke-DotNetRestore" }
            [CommandDoc]@{ Alias = "dclean"; FullName = "Remove-BinObj";    Desc = "Remove all bin/ and obj/ folders recursively"; Command = "Remove-BinObj" }
            [CommandDoc]@{ Alias = "da";   FullName = "Add-Migration";      Desc = "Add an Entity Framework Core migration"; Command = "Add-Migration" }
            [CommandDoc]@{ Alias = "du";   FullName = "Update-Database";    Desc = "Apply migrations to database target"; Command = "Update-Database" }
            [CommandDoc]@{ Alias = "dd";   FullName = "Remove-Database";    Desc = "Drop database target"; Command = "Remove-Database" }
            [CommandDoc]@{ Alias = "dremove"; FullName = "Remove-Migration"; Desc = "Rollback last database migration"; Command = "Remove-Migration" }
            [CommandDoc]@{ Alias = "sln";  FullName = "New-Solution";       Desc = "Create a new solution file (.sln)"; Command = "New-Solution" }
            [CommandDoc]@{ Alias = "sln-add"; FullName = "Add-AllProjectsToSolution"; Desc = "Add all .csproj files to solution recursively"; Command = "Add-AllProjectsToSolution" }
            [CommandDoc]@{ Alias = "console"; FullName = "New-ConsoleProject"; Desc = "Create a new .NET Console application"; Command = "New-ConsoleProject" }
            [CommandDoc]@{ Alias = "webapi";  FullName = "New-WebApiProject";  Desc = "Create a new .NET Web API application"; Command = "New-WebApiProject" }

            # Docker
            [CommandDoc]@{ Alias = "dkcl";   FullName = "Get-DockerContainers"; Desc = "Lists all running Docker containers"; Command = "Get-DockerContainers" }
            [CommandDoc]@{ Alias = "dkcl";   FullName = "Invoke-DockerDashboard"; Desc = "Run container dashboard TUI"; Command = "Invoke-DockerDashboard" }
            [CommandDoc]@{ Alias = "dkrmac"; FullName = "Remove-AllDockerContainers"; Desc = "Force delete all containers"; Command = "Remove-AllDockerContainers" }
            [CommandDoc]@{ Alias = "dkstac"; FullName = "Stop-AllDockerContainers"; Desc = "Force stop all running containers"; Command = "Stop-AllDockerContainers" }
            [CommandDoc]@{ Alias = "dkcpu";  FullName = "Invoke-ComposeUp";    Desc = "Runs docker-compose up"; Command = "Invoke-ComposeUp" }
            [CommandDoc]@{ Alias = "dkcpub"; FullName = "Invoke-ComposeUpBuild"; Desc = "Rebuild and run compose container stack"; Command = "Invoke-ComposeUpBuild" }
            [CommandDoc]@{ Alias = "dkcpd";  FullName = "Invoke-ComposeDown";  Desc = "Runs docker-compose down"; Command = "Invoke-ComposeDown" }
            [CommandDoc]@{ Alias = "fix-volume"; FullName = "Remove-UnusedDockerVolumes"; Desc = "Prune all dangling Docker volumes"; Command = "Remove-UnusedDockerVolumes" }
            [CommandDoc]@{ Alias = "fix-image";  FullName = "Remove-UnusedDockerImages";  Desc = "Prune unused Docker images"; Command = "Remove-UnusedDockerImages" }

            # AWS
            [CommandDoc]@{ Alias = "s3ls";     FullName = "Get-S3Buckets";       Desc = "List LocalStack S3 buckets"; Command = "Get-S3Buckets" }
            [CommandDoc]@{ Alias = "lbls";     FullName = "Get-LambdaFunctions"; Desc = "List LocalStack Lambda functions"; Command = "Get-LambdaFunctions" }
            [CommandDoc]@{ Alias = "sqsls";    FullName = "Get-LocalSQSQueues";  Desc = "List LocalStack SQS queues"; Command = "Get-LocalSQSQueues" }
            [CommandDoc]@{ Alias = "s3mb";     FullName = "New-S3Bucket";        Desc = "Create LocalStack S3 bucket"; Command = "New-S3Bucket" }
            [CommandDoc]@{ Alias = "sqsmb";    FullName = "New-LocalSQSQueue";   Desc = "Create LocalStack SQS queue"; Command = "New-LocalSQSQueue" }
            [CommandDoc]@{ Alias = "sqspurge"; FullName = "Clear-LocalSQSQueue"; Desc = "Purge LocalStack SQS queue"; Command = "Clear-LocalSQSQueue" }
            [CommandDoc]@{ Alias = "sqssend";  FullName = "Send-LocalSQSMessage"; Desc = "Send message to LocalStack SQS queue"; Command = "Send-LocalSQSMessage" }
            [CommandDoc]@{ Alias = "sqsrecv";  FullName = "Get-LocalSQSMessage"; Desc = "Receive message from LocalStack SQS queue"; Command = "Get-LocalSQSMessage" }
            [CommandDoc]@{ Alias = "sqsattr";  FullName = "Get-LocalSQSAttributes"; Desc = "Get LocalStack SQS attributes"; Command = "Get-LocalSQSAttributes" }
        )

        $dict["AI & Profile Contexts"] = @(
            [CommandDoc]@{ Alias = "ai";       FullName = "Invoke-MultiAgent";       Desc = "Unified TUI AI Agent Selector menu"; Command = "Invoke-MultiAgent" }
            [CommandDoc]@{ Alias = "claude";   FullName = "Invoke-Claude-By-Ollama";   Desc = "Launch Anthropic Claude Code via local Ollama"; Command = "Invoke-Claude-By-Ollama" }
            [CommandDoc]@{ Alias = "codex";    FullName = "Invoke-Codex-By-Ollama";    Desc = "Launch Codex CLI via local Ollama"; Command = "Invoke-Codex-By-Ollama" }
            [CommandDoc]@{ Alias = "openclaw"; FullName = "Invoke-OpenClaw-By-Ollama"; Desc = "Launch OpenClaw CLI local agent"; Command = "Invoke-OpenClaw-By-Ollama" }
            [CommandDoc]@{ Alias = "clawdbot"; FullName = "Invoke-Clawdbot-By-Ollama"; Desc = "Launch Clawdbot AI helper"; Command = "Invoke-Clawdbot-By-Ollama" }
            [CommandDoc]@{ Alias = "hermes";   FullName = "Invoke-Hermes-By-Ollama";   Desc = "Launch Hermes local reasoning LLM console"; Command = "Invoke-Hermes-By-Ollama" }
            [CommandDoc]@{ Alias = "hermesd";  FullName = "Invoke-HermesDesktop-By-Ollama"; Desc = "Launch Hermes reasoning LLM on Desktop"; Command = "Invoke-HermesDesktop-By-Ollama" }
            [CommandDoc]@{ Alias = "model";    FullName = "Set-OllamaModel";          Desc = "Configure default local Ollama model"; Command = "Set-OllamaModel" }
            [CommandDoc]@{ Alias = "ollama-logs"; FullName = "Invoke-OllamaLogs";     Desc = "Ensure local Ollama server is running and view logs"; Command = "Invoke-OllamaLogs" }
            [CommandDoc]@{ Alias = "agy-account"; FullName = "Invoke-AgyAccount";     Desc = "Manage isolated Antigravity accounts, credentials, and directories"; Command = "Invoke-AgyAccount" }
            [CommandDoc]@{ Alias = "agy";      FullName = "agy";                  Desc = "Invoke agy CLI under isolated context"; Command = "agy" }
            [CommandDoc]@{ Alias = "multigravity"; FullName = "multigravity";     Desc = "Run multigravity multi-profile orchestration CLI"; Command = "multigravity" }
            [CommandDoc]@{ Alias = "mgr";      FullName = "Start-Manager";        Desc = "Check dependencies and launch Antigravity Manager project"; Command = "Start-Manager" }
            [CommandDoc]@{ Alias = "prxy";     FullName = "Start-Proxy";          Desc = "Check dependencies and launch Antigravity Claude Proxy"; Command = "Start-Proxy" }
            [CommandDoc]@{ Alias = "autoswitch"; FullName = "Toggle-AutoSwitch";      Desc = "Toggle automatic account switching on directory change"; Command = "Toggle-AutoSwitch" }
            [CommandDoc]@{ Alias = "acc-sum";  FullName = "Show-AccountsSummary";  Desc = "Display active status and statistics for all accounts"; Command = "Show-AccountsSummary" }
            [CommandDoc]@{ Alias = "ai-dash";  FullName = "Show-AiDashboard";      Desc = "Launch TUI selector for local AI Ollama agents"; Command = "Show-AiDashboard" }
            [CommandDoc]@{ Alias = "ask-ai";   FullName = "Invoke-AskAi";          Desc = "Quick query tool to ask local model questions"; Command = "Invoke-AskAi" }
        )

        return $dict
    }

    static [void] Show([string]$CategoryFilter) {
        $cmds = [ProfileHelp]::GetCommands()

        # Dynamic TUI category menu using global TerminalMenu
        $categories = [string[]]($cmds.Keys | Sort-Object)

        # Pre-build flat list of all commands for global search matching
        $allCommands = @()
        foreach ($cat in $categories) {
            foreach ($cmd in $cmds[$cat]) {
                $allCommands += $cmd
            }
        }

        $resolver = {
            param([string]$filterText)
            $results = [System.Collections.Generic.List[MenuItem]]::new()

            if ([string]::IsNullOrWhiteSpace($filterText)) {
                # Show top-level categories
                foreach ($cat in $categories) {
                    $item = [MenuItem]@{
                        Label = "$cat ($($cmds[$cat].Count) commands)"
                        Type = "Category"
                        Value = $cat
                    }
                    $null = $results.Add($item)
                }
            } else {
                # Perform global flat child command search
                foreach ($cmd in $allCommands) {
                    if ($cmd.Alias -like "*$filterText*" -or $cmd.Desc -like "*$filterText*" -or $cmd.Command -like "*$filterText*") {
                        $item = [MenuItem]@{
                            Label = "{0,-10} - {1}" -f $cmd.Alias, $cmd.Desc
                            Type = "Command"
                            Value = $cmd
                        }
                        $null = $results.Add($item)
                    }
                }
            }
            return $results.ToArray()
        }

        while ($true) {
            $selected = [TerminalMenu]::ShowDynamic("Select Help Category", $resolver, 0, $CategoryFilter)
            if ($null -eq $selected) { return }

            # Reset the filter once a selection is made or on loop return so it doesn't get locked
            $CategoryFilter = ""

            if ($selected.Type -eq "Command") {
                # Select command from global search result and run directly
                $cmdObj = $selected.Value
                Clear-Host
                Write-Host ""
                Write-Host "  Running: $($cmdObj.Alias) ($($cmdObj.Command))" -ForegroundColor Green
                Write-Host ""

                try {
                    Invoke-Expression $cmdObj.Alias | Out-Host
                } catch {
                    Write-Error "Execution failed: $_"
                }

                Write-Host ""
                Write-Host "  [Press any key to return to help menu]" -ForegroundColor DarkGray
                $null = [Console]::ReadKey($true)
            }
            elseif ($selected.Type -eq "Category") {
                $catName = $selected.Value
                $catCmds = $cmds[$catName]

                while ($true) {
                    $subResolver = {
                        param([string]$subFilter)
                        $subResults = [System.Collections.Generic.List[MenuItem]]::new()
                        foreach ($cmd in $catCmds) {
                            if ([string]::IsNullOrWhiteSpace($subFilter) -or $cmd.Alias -like "*$subFilter*" -or $cmd.Desc -like "*$subFilter*" -or $cmd.Command -like "*$subFilter*") {
                                $item = [MenuItem]@{
                                    Label = "{0,-10} - {1}" -f $cmd.Alias, $cmd.Desc
                                    Type = "Command"
                                    Value = $cmd
                                }
                                $null = $subResults.Add($item)
                            }
                        }
                        return $subResults.ToArray()
                    }

                    $selectedSub = [TerminalMenu]::ShowDynamic("Category: $catName", $subResolver, 0)
                    if ($null -eq $selectedSub) { break }

                    $cmdObj = $selectedSub.Value
                    Clear-Host
                    Write-Host ""
                    Write-Host "  Running: $($cmdObj.Alias) ($($cmdObj.Command))" -ForegroundColor Green
                    Write-Host ""

                    try {
                        Invoke-Expression $cmdObj.Alias | Out-Host
                    } catch {
                        Write-Error "Execution failed: $_"
                    }

                    Write-Host ""
                    Write-Host "  [Press any key to return to category menu]" -ForegroundColor DarkGray
                    $null = [Console]::ReadKey($true)
                }
            }
        }
    }
}
#endregion



