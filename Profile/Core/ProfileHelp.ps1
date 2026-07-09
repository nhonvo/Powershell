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

        $dict["Project Navigation"] = @(
            [CommandDoc]@{ Alias = "..";      FullName = "..";      Desc = "Navigate up one directory level"; Command = "Set-LocationParent" }
            [CommandDoc]@{ Alias = "...";     FullName = "...";     Desc = "Navigate up two directory levels"; Command = "Set-LocationGrandParent" }
            [CommandDoc]@{ Alias = "proj";     FullName = "proj";     Desc = "Jump to project workspace (launches TUI on conflict)"; Command = "Enter-Project" }
            [CommandDoc]@{ Alias = "f";       FullName = "f";       Desc = "Open current directory in Windows File Explorer"; Command = "Invoke-OpenExplorer" }
            [CommandDoc]@{ Alias = "mkcd";    FullName = "mkcd";    Desc = "Create a new directory and navigate into it"; Command = "New-DirAndEnter" }
            [CommandDoc]@{ Alias = "theme";   FullName = "theme";   Desc = "Interactive Oh My Posh theme switcher TUI"; Command = "Select-ShellTheme" }
        )

        $dict["System Utilities"] = @(
            [CommandDoc]@{ Alias = "go";       FullName = "go";       Desc = "Reload the current PowerShell profile session"; Command = "Reload-Profile" }
            [CommandDoc]@{ Alias = "usage";   FullName = "usage";   Desc = "Display partition utilization statistics"; Command = "Get-DiskSpace" }
            [CommandDoc]@{ Alias = "kill";    FullName = "kill";    Desc = "Friendly process termination selector"; Command = "Stop-ProcessFriendly" }
            [CommandDoc]@{ Alias = "myip";    FullName = "myip";    Desc = "Resolve and print current public IPv4 address"; Command = "Get-PublicIP" }
            [CommandDoc]@{ Alias = "tree";    FullName = "tree";    Desc = "Display printout of folder tree structure"; Command = "Get-FileTree" }
            [CommandDoc]@{ Alias = "commands"; FullName = "commands"; Desc = "Access the interactive TUI profile manual"; Command = "Get-CustomCommands" }
        )

        $dict["SSH"] = @(
            [CommandDoc]@{ Alias = "ssh-info"; FullName = "ssh-info"; Desc = "Display Tailscale connection details & active port 22 SSH sessions"; Command = "Get-SshConnectionInfo" }
            [CommandDoc]@{ Alias = "ssh-addkey"; FullName = "ssh-addkey"; Desc = "Authorize a public SSH key for passwordless login"; Command = "Add-SshAuthorizedKey" }
        )

        $dict["Git"] = @(
            [CommandDoc]@{ Alias = "gs";   FullName = "gs";   Desc = "Shows git status"; Command = "Get-GitStatus" }
            [CommandDoc]@{ Alias = "gd";   FullName = "gd";   Desc = "Shows git diff"; Command = "Show-GitDiff" }
            [CommandDoc]@{ Alias = "glo";  FullName = "glo";  Desc = "Shows git log graph"; Command = "Get-GitLogGraph" }
            [CommandDoc]@{ Alias = "glog"; FullName = "glog"; Desc = "Shows git log prettified list"; Command = "Get-GitLogPretty" }
            [CommandDoc]@{ Alias = "gb";   FullName = "gb";   Desc = "Shows git branches"; Command = "Get-GitBranches" }
            [CommandDoc]@{ Alias = "co";   FullName = "co";   Desc = "Checkout git branch"; Command = "Invoke-GitCheckout" }
            [CommandDoc]@{ Alias = "cob";  FullName = "cob";  Desc = "Checkout new git branch"; Command = "New-GitBranch" }
            [CommandDoc]@{ Alias = "gbd";  FullName = "gbd";  Desc = "Delete git branch"; Command = "Remove-GitBranch" }
            [CommandDoc]@{ Alias = "ga";   FullName = "ga";   Desc = "Stages all files (git add .)"; Command = "Invoke-GitAddAll" }
            [CommandDoc]@{ Alias = "gunstage"; FullName = "gunstage"; Desc = "Unstage currently staged changes"; Command = "Invoke-GitUnstage" }
            [CommandDoc]@{ Alias = "gcmt"; FullName = "gcmt"; Desc = "Commit changes with message"; Command = "Invoke-GitCommit" }
            [CommandDoc]@{ Alias = "gca";  FullName = "gca";  Desc = "Amend modifications into the last commit"; Command = "Invoke-GitAmend" }
            [CommandDoc]@{ Alias = "gundo"; FullName = "gundo"; Desc = "Undo the last commit (keep changes)"; Command = "Invoke-GitUndo" }
            [CommandDoc]@{ Alias = "gr";   FullName = "gr";   Desc = "Soft reset to commit"; Command = "Invoke-GitResetSoft" }
            [CommandDoc]@{ Alias = "grh";  FullName = "grh";  Desc = "Hard reset to branch"; Command = "Invoke-GitResetHard" }
            [CommandDoc]@{ Alias = "gf";   FullName = "gf";   Desc = "Fetch updates from remote"; Command = "Invoke-GitFetch" }
            [CommandDoc]@{ Alias = "gpu";  FullName = "gpu";  Desc = "Pull updates from remote"; Command = "Invoke-GitPull" }
            [CommandDoc]@{ Alias = "gus";  FullName = "gus";  Desc = "Push commits to remote"; Command = "Invoke-GitPush" }
            [CommandDoc]@{ Alias = "guf";  FullName = "guf";  Desc = "Force push commits to remote"; Command = "Invoke-GitPushForce" }
            [CommandDoc]@{ Alias = "gms";  FullName = "gms";  Desc = "Squash merge target branch"; Command = "Invoke-GitMergeSquash" }
            [CommandDoc]@{ Alias = "gsnap"; FullName = "gsnap"; Desc = "Create stash snapshot checkpoint"; Command = "Invoke-GitStashSnapshot" }
        )

        $dict[".NET"] = @(
            [CommandDoc]@{ Alias = "dr";   FullName = "dr";   Desc = "Runs the current .NET project"; Command = "Invoke-DotNetRun" }
            [CommandDoc]@{ Alias = "dw";   FullName = "dw";   Desc = "Watch project for changes"; Command = "Invoke-DotNetWatch" }
            [CommandDoc]@{ Alias = "db";   FullName = "db";   Desc = "Builds the current .NET project"; Command = "Invoke-DotNetBuild" }
            [CommandDoc]@{ Alias = "df";   FullName = "df";   Desc = "Formats .NET codebase style rules"; Command = "Invoke-DotNetFormat" }
            [CommandDoc]@{ Alias = "dt";   FullName = "dt";   Desc = "Runs unit tests"; Command = "Invoke-DotNetTest" }
            [CommandDoc]@{ Alias = "wt";   FullName = "wt";   Desc = "Watch project tests for changes"; Command = "Invoke-DotNetWatchTest" }
            [CommandDoc]@{ Alias = "dcl";  FullName = "dcl";  Desc = "Run dotnet clean"; Command = "Invoke-DotNetClean" }
            [CommandDoc]@{ Alias = "dres"; FullName = "dres"; Desc = "Restore project dependencies"; Command = "Invoke-DotNetRestore" }
            [CommandDoc]@{ Alias = "dclean"; FullName = "dclean"; Desc = "Remove all bin/ and obj/ folders recursively"; Command = "Remove-BinObj" }
            [CommandDoc]@{ Alias = "da";   FullName = "da";   Desc = "Add an Entity Framework Core migration"; Command = "Add-Migration" }
            [CommandDoc]@{ Alias = "du";   FullName = "du";   Desc = "Apply migrations to database target"; Command = "Update-Database" }
            [CommandDoc]@{ Alias = "dd";   FullName = "dd";   Desc = "Drop database target"; Command = "Remove-Database" }
            [CommandDoc]@{ Alias = "dremove"; FullName = "dremove"; Desc = "Rollback last database migration"; Command = "Remove-Migration" }
            [CommandDoc]@{ Alias = "sln";  FullName = "sln";  Desc = "Create a new solution file (.sln)"; Command = "New-Solution" }
            [CommandDoc]@{ Alias = "sln-add"; FullName = "sln-add"; Desc = "Add all .csproj files to solution recursively"; Command = "Add-AllProjectsToSolution" }
            [CommandDoc]@{ Alias = "console"; FullName = "console"; Desc = "Create a new .NET Console application"; Command = "New-ConsoleProject" }
            [CommandDoc]@{ Alias = "webapi";  FullName = "webapi";  Desc = "Create a new .NET Web API application"; Command = "New-WebApiProject" }
        )

        $dict["Docker"] = @(
            [CommandDoc]@{ Alias = "dkcl";   FullName = "dkcl";   Desc = "Lists all running Docker containers"; Command = "Get-DockerContainers" }
            [CommandDoc]@{ Alias = "dkrmac"; FullName = "dkrmac"; Desc = "Force delete all containers"; Command = "Remove-AllDockerContainers" }
            [CommandDoc]@{ Alias = "dkstac"; FullName = "dkstac"; Desc = "Force stop all running containers"; Command = "Stop-AllDockerContainers" }
            [CommandDoc]@{ Alias = "dkcpu";  FullName = "dkcpu";  Desc = "Runs docker-compose up"; Command = "Invoke-ComposeUp" }
            [CommandDoc]@{ Alias = "dkcpub"; FullName = "dkcpub"; Desc = "Rebuild and run compose container stack"; Command = "Invoke-ComposeUpBuild" }
            [CommandDoc]@{ Alias = "dkcpd";  FullName = "dkcpd";  Desc = "Runs docker-compose down"; Command = "Invoke-ComposeDown" }
            [CommandDoc]@{ Alias = "fix-volume"; FullName = "fix-volume"; Desc = "Prune all dangling Docker volumes"; Command = "Remove-UnusedDockerVolumes" }
            [CommandDoc]@{ Alias = "fix-image";  FullName = "fix-image";  Desc = "Prune unused Docker images"; Command = "Remove-UnusedDockerImages" }
        )

        $dict["AI Tools"] = @(
            [CommandDoc]@{ Alias = "ai";       FullName = "ai";       Desc = "Unified TUI AI Agent Selector menu"; Command = "Invoke-MultiAgent" }
            [CommandDoc]@{ Alias = "claude";   FullName = "claude";   Desc = "Launch Anthropic Claude Code via local Ollama"; Command = "Invoke-Claude-By-Ollama" }
            [CommandDoc]@{ Alias = "codex";    FullName = "codex";    Desc = "Launch Codex CLI via local Ollama"; Command = "Invoke-Codex-By-Ollama" }
            [CommandDoc]@{ Alias = "openclaw"; FullName = "openclaw"; Desc = "Launch OpenClaw CLI local agent"; Command = "Invoke-OpenClaw-By-Ollama" }
            [CommandDoc]@{ Alias = "clawdbot"; FullName = "clawdbot"; Desc = "Launch Clawdbot AI helper"; Command = "Invoke-Clawdbot-By-Ollama" }
            [CommandDoc]@{ Alias = "hermes";   FullName = "hermes";   Desc = "Launch Hermes local reasoning LLM console"; Command = "Invoke-Hermes-By-Ollama" }
            [CommandDoc]@{ Alias = "hermesd";  FullName = "hermesd";  Desc = "Launch Hermes reasoning LLM on Desktop"; Command = "Invoke-HermesDesktop-By-Ollama" }
            [CommandDoc]@{ Alias = "model";    FullName = "model";    Desc = "Configure default local Ollama model"; Command = "Set-OllamaModel" }
            [CommandDoc]@{ Alias = "ollama-logs"; FullName = "ollama-logs"; Desc = "Ensure local Ollama server is running and view logs"; Command = "Invoke-OllamaLogs" }
            [CommandDoc]@{ Alias = "agy-account"; FullName = "agy-account"; Desc = "Manage isolated Antigravity accounts, credentials, and directories"; Command = "Invoke-AgyAccount" }
            [CommandDoc]@{ Alias = "agy";      FullName = "agy";      Desc = "Invoke agy CLI under isolated context"; Command = "agy" }
            [CommandDoc]@{ Alias = "multigravity"; FullName = "multigravity"; Desc = "Run multigravity multi-profile orchestration CLI"; Command = "multigravity" }
        )

        $dict["AWS"] = @(
            [CommandDoc]@{ Alias = "Get-S3Buckets";       FullName = "Get-S3Buckets";       Desc = "List LocalStack S3 buckets"; Command = "Get-S3Buckets" }
            [CommandDoc]@{ Alias = "Get-LambdaFunctions"; FullName = "Get-LambdaFunctions"; Desc = "List LocalStack Lambda functions"; Command = "Get-LambdaFunctions" }
            [CommandDoc]@{ Alias = "Get-LocalSQSQueues";  FullName = "Get-LocalSQSQueues";  Desc = "List LocalStack SQS queues"; Command = "Get-LocalSQSQueues" }
            [CommandDoc]@{ Alias = "New-S3Bucket";        FullName = "New-S3Bucket";        Desc = "Create LocalStack S3 bucket"; Command = "New-S3Bucket" }
            [CommandDoc]@{ Alias = "New-LocalSQSQueue";   FullName = "New-LocalSQSQueue";   Desc = "Create LocalStack SQS queue"; Command = "New-LocalSQSQueue" }
            [CommandDoc]@{ Alias = "Clear-LocalSQSQueue"; FullName = "Clear-LocalSQSQueue"; Desc = "Purge LocalStack SQS queue"; Command = "Clear-LocalSQSQueue" }
            [CommandDoc]@{ Alias = "Send-LocalSQSMessage"; FullName = "Send-LocalSQSMessage"; Desc = "Send message to LocalStack SQS queue"; Command = "Send-LocalSQSMessage" }
            [CommandDoc]@{ Alias = "Get-LocalSQSMessage";  FullName = "Get-LocalSQSMessage";  Desc = "Receive message from LocalStack SQS queue"; Command = "Get-LocalSQSMessage" }
            [CommandDoc]@{ Alias = "Get-LocalSQSAttributes"; FullName = "Get-LocalSQSAttributes"; Desc = "Get LocalStack SQS attributes"; Command = "Get-LocalSQSAttributes" }
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



