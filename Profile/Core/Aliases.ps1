#region CENTRALIZED SHELL ALIASES & WRAPPER FUNCTIONS
# ==============================================================================
#  Centralized routing layer bridging CLI commands to the static class helpers.
# ==============================================================================

# --- Core Aliases ---
Set-Alias -Name ip  -Value Get-NetIPConfiguration -Force
Set-Item -Path Alias:\cls -Value Clear-Host -Force -Option AllScope

# --- Navigation & System Wrappers ---
function Set-LocationParent { [ProfileNavigator]::GoParent() }
function Set-LocationGrandParent { [ProfileNavigator]::GoGrandParent() }
function Invoke-OpenExplorer { [SystemHelper]::OpenExplorer() }
function Enter-Project {
    param([string]$Name)
    [ProfileNavigator]::EnterProject($Name)
}
function Reload-Profile {
    $global:AgyProfileLoaded = $false
    $global:PoshInitialized = $false
    . $PROFILE
    Write-Host "✅ Profile reloaded." -ForegroundColor Green
}
function New-DirAndEnter {
    param([string]$Path)
    $null = New-Item -ItemType Directory -Path $Path -Force
    Set-Location $Path
}
function Get-DiskSpace { [SystemHelper]::GetDiskSpace() | Format-Table -AutoSize }
function Get-PublicIP { [SystemHelper]::GetPublicIP() }
function Get-FileTree {
    param([int]$Depth = 2)
    tree.com /f /a | Select-Object -First (50 * $Depth)
}
function Stop-ProcessFriendly {
    param([string]$Name)
    [SystemHelper]::StopProcessFriendly($Name)
}
function Get-CustomCommands {
    [CmdletBinding(DefaultParameterSetName = "Menu")]
    param(
        [Parameter(ParameterSetName = "Menu")]
        [switch]$Menu,

        [Parameter(ParameterSetName = "Account")]
        [switch]$Account,

        [Parameter(ParameterSetName = "Account")]
        [switch]$Agy,

        [Parameter(ParameterSetName = "Ai")]
        [switch]$Ai,

        [Parameter(ParameterSetName = "Project")]
        [string]$Project,

        [Parameter(ParameterSetName = "Theme")]
        [switch]$Theme,

        [Parameter(ParameterSetName = "Ssh")]
        [switch]$Ssh,

        [Parameter(ParameterSetName = "Manual")]
        [switch]$Manual,

        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$ArgsList
    )

    if ($Account -or $Agy) {
        [AgyAccountManager]::ManageAccountsInteractive()
        return
    }

    if ($Ai) {
        Invoke-MultiAgent
        return
    }

    if ($null -ne $Project -and $Project -ne "") {
        Enter-Project $Project
        return
    }

    if ($Theme) {
        Select-ShellTheme
        return
    }

    if ($Ssh) {
        Get-SshConnectionInfo
        return
    }

    if ($Manual) {
        [ProfileHelp]::Show("")
        return
    }

    if ($ArgsList) {
        $argStr = $ArgsList -join " "
        $matchedProj = [ProfileNavigator]::FindWorkspaces($argStr)
        if ($matchedProj.Count -gt 0) {
            Enter-Project $argStr
        } else {
            [ProfileHelp]::Show($argStr)
        }
        return    }    # Default Command Palette TUI Database setup
    $commandsMap = [ProfileHelp]::GetCommands()
    
    # 1. Main Dashboard TUI Loop
    $defaultIndex = 0
    $cHalf = [char]0x2584
    $cFull = [char]0x2588
    $cTop  = [char]0x2580

    while ($true) {
        if ($null -eq $Global:TuiColors) { [TerminalMenu]::InitializeTuiColors() }
        
        $activeAcc = [AgyAccountManager]::GetActiveAccount()
        $timeStr = Get-Date -Format 'yyyy-MM-dd HH:mm'
        
        $headers = @(
            "  $cHalf$cFull$cFull$cFull$cFull$cHalf   $cHalf$cFull$cFull$cFull$cFull$cHalf     Powershell Profile CLI v2.0",
            " $cFull$cTop     $cTop $cFull$cTop     $cTop    System dashboard and control suite.",
            " $cFull        $cFull           ",
            " $cFull$cHalf     $cHalf $cFull$cHalf     $cHalf    Active Account: $activeAcc",
            "  $cTop$cFull$cFull$cFull$cFull$cTop   $cTop$cFull$cFull$cFull$cFull$cTop     Time:   $timeStr",
            "============================================="
        )

        $menuItems = @(
            "[Account]  Manage Antigravity Accounts & Credentials",
            "[AI Agent] Select and run local AI agents (Claude, Codex)",
            "[Project]  Navigate to a registered project workspace",
            "[Manual]   Interactive custom commands help reference",
            "[Theme]    Select and apply Oh My Posh shell theme",
            "[SSH Info] View Tailscale & active port 22 sessions",
            "[Exit]     Exit Control Center"
        )

        while ([Console]::KeyAvailable) { [void][Console]::ReadKey($true) }

        $selected = [TerminalMenu]::ShowRobust($headers, $menuItems, $defaultIndex, $false, $true)
        if ($selected -lt 0 -or $selected -eq ($menuItems.Count - 1)) {
            break
        }

        # Remember selected item
        $defaultIndex = $selected
        $action = $menuItems[$selected]

        Clear-Host
        if ($action.StartsWith("[Account]")) {
            [AgyAccountManager]::ManageAccountsInteractive()
        }
        elseif ($action.StartsWith("[AI Agent]")) {
            [AiHelper]::ShowAiDashboard()
        }
        elseif ($action.StartsWith("[Project]")) {
            Enter-Project ""
        }
        elseif ($action.StartsWith("[Theme]")) {
            Select-ShellTheme
        }
        elseif ($action.StartsWith("[SSH Info]")) {
            Get-SshConnectionInfo
            Write-Host "`n----------------------------------------------------------" -ForegroundColor Gray
            Write-Host "Press any key to return to Control Center..." -ForegroundColor Yellow
            while ([Console]::KeyAvailable) { [void][Console]::ReadKey($true) }
            [void][Console]::ReadKey($true)
        }
        elseif ($action.StartsWith("[Manual]")) {
            # Open the Category-grouped Manual Sub-menu
            $defaultCategoryIndex = 0
            while ($true) {
                $subHeaders = @(
                    "  $cHalf$cFull$cFull$cFull$cFull$cHalf   $cHalf$cFull$cFull$cFull$cFull$cHalf     Powershell Profile CLI v2.0",
                    " $cFull$cTop     $cTop $cFull$cTop     $cTop    Manual - Command Categories",
                    " $cFull        $cFull           ",
                    " $cFull$cHalf     $cHalf $cFull$cHalf     $cHalf    Select a category to view commands.",
                    "  $cTop$cFull$cFull$cFull$cFull$cTop   $cTop$cFull$cFull$cFull$cFull$cTop     Esc to go back.",
                    "============================================="
                )

                $categories = [System.Collections.Generic.List[string]]::new()
                foreach ($cat in $commandsMap.Keys) {
                    $null = $categories.Add($cat)
                }
                $categoriesList = $categories | Sort-Object
                $catMenuItems = [System.Collections.Generic.List[string]]::new()
                foreach ($cat in $categoriesList) {
                    $null = $catMenuItems.Add("[Category] $cat")
                }
                $null = $catMenuItems.Add("[Back to Dashboard]")

                while ([Console]::KeyAvailable) { [void][Console]::ReadKey($true) }

                $selectedCat = [TerminalMenu]::ShowRobust($subHeaders, $catMenuItems.ToArray(), $defaultCategoryIndex, $false, $true)
                if ($selectedCat -lt 0 -or $selectedCat -eq ($catMenuItems.Count - 1)) {
                    break
                }

                $defaultCategoryIndex = $selectedCat
                $selectedCatName = $categoriesList[$selectedCat]

                # 3. Commands list sub-menu
                $defaultCmdIndex = 0
                while ($true) {
                    $cmdHeaders = @(
                        "  $cHalf$cFull$cFull$cFull$cFull$cHalf   $cHalf$cFull$cFull$cFull$cFull$cHalf     Powershell Profile CLI v2.0",
                        " $cFull$cTop     $cTop $cFull$cTop     $cTop    Category: $selectedCatName",
                        " $cFull        $cFull           ",
                        " $cFull$cHalf     $cHalf $cFull$cHalf     $cHalf    Select a command to run.",
                        "  $cTop$cFull$cFull$cFull$cFull$cTop   $cTop$cFull$cFull$cFull$cFull$cTop     Esc to go back.",
                        "============================================="
                    )

                    $docs = $commandsMap[$selectedCatName] | Sort-Object Alias
                    $cmdLabels = [System.Collections.Generic.List[string]]::new()
                    $cmdDetails = [System.Collections.Generic.List[string]]::new()
                    foreach ($doc in $docs) {
                        $null = $cmdLabels.Add("$($doc.Alias.PadRight(12)) - $($doc.Desc)")
                        $null = $cmdDetails.Add($doc.Command)
                    }
                    $null = $cmdLabels.Add("[Back to Categories]")
                    $null = $cmdDetails.Add("")

                    while ([Console]::KeyAvailable) { [void][Console]::ReadKey($true) }

                    $selectedCmd = [TerminalMenu]::Show($cmdHeaders, $cmdLabels.ToArray(), $cmdDetails.ToArray(), $defaultCmdIndex, $false, $true)
                    if ($selectedCmd -lt 0 -or $selectedCmd -eq ($cmdLabels.Count - 1)) {
                        break
                    }

                    $defaultCmdIndex = $selectedCmd
                    $selectedCmdObj = $docs[$selectedCmd]
                    $cmdToRun = $selectedCmdObj.Command
                    $aliasName = $selectedCmdObj.Alias

                    Clear-Host
                    Write-Host ">>> Running: $aliasName ($cmdToRun)" -ForegroundColor Green
                    Write-Host "----------------------------------------------------------" -ForegroundColor Gray
                    
                    try {
                        Invoke-Expression $aliasName
                    } catch {
                        Write-Error $_
                    }

                    Write-Host "`n----------------------------------------------------------" -ForegroundColor Gray
                    Write-Host "Press any key to return to $selectedCatName..." -ForegroundColor Yellow
                    while ([Console]::KeyAvailable) { [void][Console]::ReadKey($true) }
                    [void][Console]::ReadKey($true)
                }
            }
        }
    }
    Clear-Host
}
function Get-SshConnectionInfo { [SshHelper]::GetConnectionInfo() }
function Add-SshAuthorizedKey {
    param([string]$Key, [string]$Account)
    [SshHelper]::AddAuthorizedKey($Key, $Account)
}
function Toggle-MobileMode { [ThemeHelper]::ToggleMobileMode() }
function Start-MobileSshKeyReceiver { [SshHelper]::StartMobileSshKeyReceiver() }

# Navigation & System Aliases
Set-Alias -Name ..          -Value Set-LocationParent          -Force
Set-Alias -Name ...         -Value Set-LocationGrandParent     -Force
Set-Alias -Name f           -Value Invoke-OpenExplorer         -Force
Set-Alias -Name prj         -Value Enter-Project               -Force
Set-Alias -Name proj        -Value Enter-Project               -Force
Set-Alias -Name go          -Value Reload-Profile              -Force
Set-Alias -Name mkcd        -Value New-DirAndEnter             -Force
Set-Alias -Name usage       -Value Get-DiskSpace               -Force
Set-Alias -Name myip        -Value Get-PublicIP                -Force
Set-Alias -Name tree        -Value Get-FileTree                -Force
Set-Item -Path Alias:\kill -Value Stop-ProcessFriendly -Force -Option AllScope
Set-Alias -Name commands    -Value Get-CustomCommands          -Force
Set-Alias -Name ssh-info    -Value Get-SshConnectionInfo       -Force
Set-Alias -Name ssh-addkey  -Value Add-SshAuthorizedKey        -Force
Set-Alias -Name mobile             -Value Toggle-MobileMode            -Force
Set-Alias -Name ssh-addkey-mobile  -Value Start-MobileSshKeyReceiver    -Force

# --- Git Wrappers ---
function Get-GitStatus { [GitHelper]::Status($args) }
function Show-GitDiff { [GitHelper]::Diff() }
function Get-GitLogGraph { [GitHelper]::LogGraph() }
function Get-GitLogPretty { [GitHelper]::LogPretty() }
function Get-GitLog { [GitHelper]::Log() }
function Get-GitBranches { [GitHelper]::GetBranches($args) }
function Invoke-GitCheckout {
    param([string]$branchName)
    [GitHelper]::Checkout($branchName)
}
function New-GitBranch {
    param([string]$branchName)
    [GitHelper]::NewBranch($branchName)
}
function Remove-GitBranch {
    param([string]$branchName)
    [GitHelper]::RemoveBranch($branchName)
}
function Invoke-GitAddAll { [GitHelper]::AddAll() }
function Invoke-GitUnstage { [GitHelper]::Unstage() }
function Invoke-GitCommit {
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Message)
    [GitHelper]::Commit($Message)
}
function Invoke-GitAmend { [GitHelper]::Amend($args) }
function Invoke-GitUndo { [GitHelper]::Undo() }
function Invoke-GitResetSoft { [GitHelper]::ResetSoft() }
function Invoke-GitResetHard { [GitHelper]::ResetHard() }
function Invoke-GitFetch { [GitHelper]::Fetch() }
function Invoke-GitPull { [GitHelper]::Pull($args) }
function Invoke-GitPush { [GitHelper]::Push($args) }
function Invoke-GitPushForce { [GitHelper]::PushForce($args) }
function Invoke-GitMergeSquash {
    param([string]$BranchName)
    [GitHelper]::MergeSquash($BranchName)
}
function Invoke-GitStashSnapshot {
    param([string]$Message)
    [GitHelper]::StashSnapshot($Message)
}

# Git Aliases
Set-Alias -Name gs        -Value Get-GitStatus        -Force
Set-Alias -Name gd        -Value Show-GitDiff         -Force
Set-Alias -Name glo       -Value Get-GitLogGraph      -Force
Set-Alias -Name glg       -Value Get-GitLogGraph      -Force
Set-Alias -Name glog      -Value Get-GitLogPretty     -Force
Set-Alias -Name gb        -Value Get-GitBranches      -Force
Set-Alias -Name co        -Value Invoke-GitCheckout   -Force
Set-Alias -Name cob       -Value New-GitBranch        -Force
Set-Alias -Name gbd       -Value Remove-GitBranch     -Force
Set-Alias -Name ga        -Value Invoke-GitAddAll     -Force
Set-Alias -Name gunstage  -Value Invoke-GitUnstage    -Force
Set-Alias -Name gcmt      -Value Invoke-GitCommit     -Force
Set-Alias -Name gca       -Value Invoke-GitAmend      -Force
Set-Alias -Name gundo     -Value Invoke-GitUndo       -Force
Set-Alias -Name gr        -Value Invoke-GitResetSoft  -Force
Set-Alias -Name grh       -Value Invoke-GitResetHard  -Force
Set-Alias -Name gf        -Value Invoke-GitFetch      -Force
Set-Alias -Name gpu       -Value Invoke-GitPull       -Force
Set-Alias -Name gus       -Value Invoke-GitPush       -Force
Set-Alias -Name guf       -Value Invoke-GitPushForce  -Force
Set-Alias -Name gms       -Value Invoke-GitMergeSquash -Force
Set-Alias -Name gsnap     -Value Invoke-GitStashSnapshot -Force

# --- .NET Development Wrappers ---
function Invoke-DotNetRun { [DotNetHelper]::Run($args) }
function Invoke-DotNetWatch { [DotNetHelper]::Watch($args) }
function Invoke-DotNetBuild { [DotNetHelper]::Build($args) }
function Invoke-DotNetFormat { [DotNetHelper]::Format($args) }
function Invoke-DotNetTest { [DotNetHelper]::Test($args) }
function Invoke-DotNetWatchTest { [DotNetHelper]::WatchTest($args) }
function Invoke-DotNetClean { [DotNetHelper]::Clean($args) }
function Invoke-DotNetRestore { [DotNetHelper]::Restore($args) }
function Remove-BinObj { [DotNetHelper]::CleanBinObj() }
function Update-Database {
    param([string]$Context)
    [DotNetHelper]::UpdateDatabase($Context)
}
function Add-Migration {
    param([string]$MigrationName, [string]$Context)
    [DotNetHelper]::AddMigration($MigrationName, $Context)
}
function Remove-Database {
    param([string]$Context)
    [DotNetHelper]::RemoveDatabase($Context)
}
function Remove-Migration {
    param([string]$Context)
    [DotNetHelper]::RemoveMigration($Context)
}
function New-Solution {
    param([string]$Name)
    [DotNetHelper]::NewSolution($Name)
}
function Add-AllProjectsToSolution { [DotNetHelper]::AddAllProjectsToSolution() }
function New-ConsoleProject {
    param([string]$Name)
    [DotNetHelper]::NewConsole($Name)
}
function New-WebApiProject {
    param([string]$Name)
    [DotNetHelper]::NewWebApi($Name)
}

# .NET Aliases
Set-Alias -Name dr     -Value Invoke-DotNetRun       -Force
Set-Alias -Name dw     -Value Invoke-DotNetWatch     -Force
Set-Alias -Name db     -Value Invoke-DotNetBuild     -Force
Set-Alias -Name df     -Value Invoke-DotNetFormat    -Force
Set-Alias -Name dt     -Value Invoke-DotNetTest      -Force
Set-Alias -Name wt     -Value Invoke-DotNetWatchTest -Force
Set-Alias -Name dcl    -Value Invoke-DotNetClean     -Force
Set-Alias -Name dres   -Value Invoke-DotNetRestore   -Force
Set-Alias -Name dclean -Value Remove-BinObj          -Force
Set-Alias -Name du      -Value Update-Database   -Force
Set-Alias -Name da      -Value Add-Migration     -Force
Set-Alias -Name dd      -Value Remove-Database   -Force
Set-Alias -Name dremove -Value Remove-Migration  -Force
Set-Alias -Name sln     -Value New-Solution              -Force
Set-Alias -Name sln-add -Value Add-AllProjectsToSolution -Force
Set-Alias -Name console -Value New-ConsoleProject        -Force
Set-Alias -Name webapi  -Value New-WebApiProject         -Force

# --- Docker Wrappers ---
function Get-DockerContainers {
    param([switch]$All)
    [DockerHelper]::GetContainers($All)
}
function Remove-AllDockerContainers { [DockerHelper]::RemoveAllContainers() }
function Stop-AllDockerContainers { [DockerHelper]::StopAllContainers() }
function Invoke-ComposeUp { [DockerHelper]::ComposeUp($args) }
function Invoke-ComposeUpBuild { [DockerHelper]::ComposeUpBuild($args) }
function Invoke-ComposeDown { [DockerHelper]::ComposeDown($args) }
function Remove-UnusedDockerVolumes { [DockerHelper]::RemoveUnusedVolumes() }
function Remove-UnusedDockerImages { [DockerHelper]::RemoveUnusedImages() }

# Docker Aliases
Set-Alias -Name dkcl       -Value Get-DockerContainers       -Force
Set-Alias -Name dkrmac     -Value Remove-AllDockerContainers -Force
Set-Alias -Name dkstac     -Value Stop-AllDockerContainers   -Force
Set-Alias -Name dkcpu      -Value Invoke-ComposeUp           -Force
Set-Alias -Name dkcpub     -Value Invoke-ComposeUpBuild      -Force
Set-Alias -Name dkcpd      -Value Invoke-ComposeDown         -Force
Set-Alias -Name fix-volume -Value Remove-UnusedDockerVolumes -Force
Set-Alias -Name fix-image  -Value Remove-UnusedDockerImages  -Force

# --- AWS LocalStack Wrappers ---
function Get-S3Buckets { [AwsHelper]::GetS3Buckets() }
function New-S3Bucket {
    param([string]$Name)
    [AwsHelper]::NewS3Bucket($Name)
}
function Get-LambdaFunctions { [AwsHelper]::GetLambdaFunctions() }
function Get-LocalSQSQueues { [AwsHelper]::GetLocalSQSQueues() }
function New-LocalSQSQueue {
    param([string]$QueueName)
    [AwsHelper]::NewLocalSQSQueue($QueueName)
}
function Clear-LocalSQSQueue {
    param([string]$QueueUrl)
    [AwsHelper]::ClearLocalSQSQueue($QueueUrl)
}
function Send-LocalSQSMessage {
    param([string]$QueueUrl, [string]$MessageBody, [string]$GroupId)
    [AwsHelper]::SendLocalSQSMessage($QueueUrl, $MessageBody, $GroupId)
}
function Get-LocalSQSMessage {
    param([string]$QueueUrl)
    [AwsHelper]::GetLocalSQSMessage($QueueUrl)
}
function Get-LocalSQSAttributes {
    param([string]$QueueUrl)
    [AwsHelper]::GetLocalSQSAttributes($QueueUrl)
}

# --- AI Tools Wrappers ---
function Invoke-Codex-By-Ollama { [AiHelper]::InvokeCodex($args) }
function Invoke-Claude-By-Ollama { [AiHelper]::InvokeClaude($args) }
function Invoke-OpenClaw-By-Ollama { [AiHelper]::InvokeOpenClaw($args) }
function Invoke-Clawdbot-By-Ollama { [AiHelper]::InvokeClawdbot($args) }
function Invoke-Hermes-By-Ollama { [AiHelper]::InvokeHermes($args) }
function Invoke-HermesDesktop-By-Ollama { [AiHelper]::InvokeHermesDesktop($args) }
function Invoke-CopilotExplain {
    param([string]$Command)
    if (Get-Command gh -ErrorAction SilentlyContinue) {
        $extensions = gh extension list -ErrorAction SilentlyContinue
        if ($extensions -notmatch "gh-copilot") {
            Write-Host "Installing github/gh-copilot extension..." -ForegroundColor Cyan
            gh extension install github/gh-copilot
        }
        gh copilot explain $Command
    } else {
        Write-Error "GitHub CLI (gh) is not installed. Please install it from https://cli.github.com/"
    }
}
function Install-AIIntegrations { [AiHelper]::InstallAIIntegrations() }
function Initialize-OllamaServer { [AiHelper]::InitializeOllamaServer() }
function Set-OllamaModel {
    param([string]$ModelName)
    [AiHelper]::SetOllamaModel($ModelName)
}
function Ensure-OllamaServer { [AiHelper]::EnsureOllamaServer() }
function Invoke-OllamaLogs { [AiHelper]::ShowOllamaLogs() }
function Invoke-Npm {
    param([string[]]$ArgsList)
    & npm @ArgsList
}

function Invoke-ChatGPT {
    param([string]$Query)
    if (Get-Command chatgpt -ErrorAction SilentlyContinue) {
        if ($Query) { chatgpt $Query } else { chatgpt }
    } else {
        Write-Warning "ChatGPT CLI command 'chatgpt' is not installed. Routing to local OpenClaw instead."
        [AiHelper]::InvokeOpenClaw(@())
    }
}

# Unified AI Agent Selector
function Invoke-MultiAgent {
    [CmdletBinding(DefaultParameterSetName="Menu")]
    param(
        [Parameter(Position=0)][string]$Query,
        [Parameter(ParameterSetName="Gemini")][Alias("g")][switch]$UseGemini,
        [Parameter(ParameterSetName="Copilot")][Alias("c")][switch]$UseCopilot,
        [Parameter(ParameterSetName="Ollama")][Alias("o")][switch]$UseOllama,
        [Parameter(ParameterSetName="Claude")][switch]$UseClaude,
        [Parameter(ParameterSetName="Local")][switch]$UseLocal,
        [Parameter(ParameterSetName="ChatGPT")][Alias("gpt")][switch]$UseChatGPT,
        [Parameter(ParameterSetName="Codex")][Alias("cx")][switch]$UseCodex,
        [string]$Model
    )
    [AiHelper]::InvokeMultiAgent($Query, $PSCmdlet.ParameterSetName, $Model)
}

# AI Tools Aliases
Set-Alias -Name ai          -Value Invoke-MultiAgent        -Force
Set-Alias -Name codex       -Value Invoke-Codex-By-Ollama   -Force
Set-Alias -Name claude      -Value Invoke-Claude-By-Ollama  -Force
Set-Alias -Name openclaw    -Value Invoke-OpenClaw-By-Ollama -Force
Set-Alias -Name clawdbot    -Value Invoke-Clawdbot-By-Ollama -Force
Set-Alias -Name hermes      -Value Invoke-Hermes-By-Ollama   -Force
Set-Alias -Name hermesd     -Value Invoke-HermesDesktop-By-Ollama -Force
Set-Alias -Name model       -Value Set-OllamaModel          -Force
Set-Alias -Name ollama-logs -Value Invoke-OllamaLogs         -Force

# --- Antigravity Multi-Account Manager Wrappers ---
function Invoke-AgyAccount {
    [CmdletBinding()]
    param(
        [Parameter(Position=0)]
        [ValidateSet("list", "lst", "show", "use", "add", "create", "remove", "delete", "del", "run", "exec", "logout", "signout", "login", "signin", "interactive", "select")]
        [string]$Action = "interactive",
        [Parameter(Position=1)]
        [string]$Account,
        [Parameter(Position=2, ValueFromRemainingArguments=$true)]
        [string[]]$ExtraArgs,
        [Parameter()][switch]$Temporary
    )
    switch ($Action) {
        "interactive" { [AgyAccountManager]::SelectAccountInteractive() }
        "select"      { [AgyAccountManager]::SelectAccountInteractive() }
        { $_ -in "list", "lst", "show" } { [AgyAccountManager]::ShowAccounts() }
        "use" {
            if ([string]::IsNullOrWhiteSpace($Account)) {
                [AgyAccountManager]::SelectAccountInteractive()
                return
            }
            [AgyAccountManager]::SetActiveAccount($Account, $Temporary)
        }
        { $_ -in "add", "create" } {
            if ([string]::IsNullOrWhiteSpace($Account)) {
                Write-Error "Please specify an account name to add."
                return
            }
            [AgyAccountManager]::AddAccount($Account)
        }
        { $_ -in "remove", "delete", "del" } {
            if ([string]::IsNullOrWhiteSpace($Account)) {
                Write-Error "Please specify an account name to remove."
                return
            }
            [AgyAccountManager]::RemoveAccount($Account)
        }
        { $_ -in "logout", "signout" } {
            $target = if ($Account) { $Account } else { [AgyAccountManager]::GetActiveAccount() }
            [AgyAccountManager]::ResetCredentials($target)
        }
        { $_ -in "login", "signin" } {
            $target = if ($Account) { $Account } else { [AgyAccountManager]::GetActiveAccount() }
            [AgyAccountManager]::ResetCredentials($target)
            [AgyAccountManager]::InvokeWithAccount($target, @())
        }
        { $_ -in "run", "exec" } {
            if ([string]::IsNullOrWhiteSpace($Account)) {
                Write-Error "Please specify an account name to run under."
                return
            }
            [AgyAccountManager]::InvokeWithAccount($Account, $ExtraArgs)
        }
    }
}
Set-Alias -Name agy-account -Value Invoke-AgyAccount -Force
Set-Alias -Name agy-acc     -Value Invoke-AgyAccount -Force

function Invoke-AgyMenu {
    [AgyAccountManager]::ManageAccountsInteractive()
}
Set-Alias -Name agy-m -Value Invoke-AgyMenu -Force

function Invoke-AccountSession {
    [CmdletBinding()]
    param(
        [Parameter(Position=0)][string]$Account,
        [Parameter(Position=1)][ScriptBlock]$Command,
        [Parameter()][switch]$Temp,
        [Parameter()][switch]$Status
    )

    if ($Status) {
        $active = [AgyAccountManager]::GetActiveAccount()
        $quota = [AgyAccountManager]::CalculateRollingQuotas($active)
        if ($Global:AiMode) {
            Write-Host "Account:$active;Weekly:$($quota.RemainingWeekly)%;5H:$($quota.Remaining5H)%;Count5H:$($quota.Count5H)"
        } else {
            Write-Host "=============================================" -ForegroundColor Cyan
            Write-Host " QUOTA STATUS: $active" -ForegroundColor Cyan
            Write-Host "=============================================" -ForegroundColor Cyan
            Write-Host "  * Weekly Quota:   $($quota.RemainingWeekly)% remaining ($($quota.CountWeekly)/1000)" -ForegroundColor Gray
            Write-Host "  * Five Hour Limit: $($quota.Remaining5H)% remaining ($($quota.Count5H)/50)" -ForegroundColor Gray
            Write-Host "=============================================" -ForegroundColor Cyan
        }
        return
    }

    if ([string]::IsNullOrWhiteSpace($Account)) {
        if ($Global:AiMode) {
            $accounts = [AgyAccountManager]::GetAccounts()
            foreach ($acc in $accounts) {
                $meta = [AgyAccountManager]::GetAccountMetadata($acc)
                $tokenFile = Join-Path ([AgyAccountManager]::GetAccountDirectory($acc)) "keyring_token.txt"
                $status = if (Test-Path $tokenFile) { "LoggedIn" } else { "LoggedOut" }
                Write-Host "$acc,$status,$($meta.UsageCount)"
            }
        } else {
            [AgyAccountManager]::ManageAccountsInteractive()
        }
        return
    }

    if ($Command) {
        $targetPath = [AgyAccountManager]::GetAccountDirectory($Account)
        if (-not (Test-Path $targetPath)) {
            Write-Error "Account '$Account' does not exist."
            return
        }
        $oldHome = $env:GEMINI_HOME
        try {
            $env:GEMINI_HOME = $targetPath
            [AgyAccountManager]::RestoreActiveToken($Account)
            $Command.Invoke()
        } finally {
            $env:GEMINI_HOME = $oldHome
            $priorActive = [AgyAccountManager]::GetActiveAccount()
            [AgyAccountManager]::RestoreActiveToken($priorActive)
        }
        return
    }

    [AgyAccountManager]::SetActiveAccount($Account, $Temp)
}
Set-Alias -Name acc -Value Invoke-AccountSession -Force


function agy {
    [CmdletBinding()]
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$PassThruArgs)
    [AgyAccountManager]::InvokeAgy($PassThruArgs)
}

function multigravity {
    [CmdletBinding()]
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$PassThruArgs)
    [AgyAccountManager]::InvokeMultigravity($PassThruArgs)
}

# --- Tab Autocompleters ---
if (Get-Command multigravity -ErrorAction SilentlyContinue) {
    Register-ArgumentCompleter -Native -CommandName multigravity -ScriptBlock {
        param($wordToComplete, $commandAst, $cursorPosition)
        $opts = @('new', 'list', 'status', 'rename', 'delete', 'clone', 'template', 'export', 'import', 'update', 'doctor', 'stats', 'completion', 'help')
        $profiles = if (Test-Path (Join-Path $env:USERPROFILE "AntigravityProfiles")) {
            Get-ChildItem -Directory -Path (Join-Path $env:USERPROFILE "AntigravityProfiles") | Select-Object -ExpandProperty Name
        } else { @() }
        ($opts + $profiles) | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
            [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
        }
    }
}

# --- Help shortcuts ---
Set-Alias -Name cc -Value Get-CustomCommands -Force
function cg   { [ProfileHelp]::Show("Git") }
function cnet { [ProfileHelp]::Show("Net") }
function csys { [ProfileHelp]::Show("Sys") }
function cdk  { [ProfileHelp]::Show("Docker") }
function cai  { [ProfileHelp]::Show("AI") }
function caws { [ProfileHelp]::Show("AWS") }
function cnav { [ProfileHelp]::Show("Navigation") }
function cssh { [ProfileHelp]::Show("SSH") }

# Theme Switcher
function Select-ShellTheme { [ThemeHelper]::SelectThemeInteractive() }
Set-Alias -Name theme -Value Select-ShellTheme -Force
# Operations Dashboards & Shortcuts
function Invoke-DockerDashboard { [DockerHelper]::Dkcl() }
Set-Alias -Name dkcl -Value Invoke-DockerDashboard -Force

function Invoke-KillPort {
    param([Parameter(Mandatory=$true, Position=0)][int]$Port)
    [SystemHelper]::KillPort($Port)
}
Set-Alias -Name killport -Value Invoke-KillPort -Force

function Invoke-SystemMonitor { [SystemHelper]::SystemMonitor() }
Set-Alias -Name sysmon -Value Invoke-SystemMonitor -Force

# Scaffolding & Git TUI Shortcuts
function Invoke-ProjectScaffolder {
    param([string]$Template, [string]$Name, [int]$Port)
    [ProjectScaffolder]::ScaffoldProject($Template, $Name, $Port)
}
Set-Alias -Name new-project -Value Invoke-ProjectScaffolder -Force

function Invoke-GitBranchCheckout { [GitHelper]::BranchCheckoutTui() }
Set-Alias -Name co -Value Invoke-GitBranchCheckout -Force

function Invoke-GitCommitWizard {
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Message)
    $msg = $Message -join ' '
    [GitHelper]::Gcmt($msg)
}
Set-Alias -Name gcmt -Value Invoke-GitCommitWizard -Force

function Invoke-AskAi {
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Query)
    $q = $Query -join ' '
    [AiHelper]::AskAi($q)
}
Set-Alias -Name ask-ai -Value Invoke-AskAi -Force

function Invoke-SecretVault {
    [CmdletBinding()]
    param(
        [Parameter(Position=0)]
        [ValidateSet("set", "get", "list", "remove", "rm")]
        [string]$Action = "list",
        [Parameter(Position=1)]
        [string]$Key,
        [Parameter(Position=2)]
        [string]$Value
    )

    switch ($Action) {
        "set" {
            if (-not $Key -or -not $Value) {
                Write-Error "Usage: sec set <key> <value>"
                return
            }
            [AgySecretVault]::SetSecret($Key, $Value)
        }
        "get" {
            if (-not $Key) {
                Write-Error "Usage: sec get <key>"
                return
            }
            $val = [AgySecretVault]::GetSecret($Key)
            if ($val) { Write-Host $val }
        }
        "list" {
            [AgySecretVault]::ListSecrets()
        }
        { $_ -in "remove", "rm" } {
            if (-not $Key) {
                Write-Error "Usage: sec remove <key>"
                return
            }
            [AgySecretVault]::RemoveSecret($Key)
        }
    }
}
Set-Alias -Name sec -Value Invoke-SecretVault -Force
function Invoke-DbTui {
    param([Parameter(Mandatory=$true, Position=0)][string]$DbPath)
    [DatabaseHelper]::DbTui($DbPath)
}
Set-Alias -Name db-tui -Value Invoke-DbTui -Force

function Invoke-LogStream {
    param([Parameter(Position=0)][string]$LogPath)
    [LogHelper]::StreamLogs($LogPath)
}
Set-Alias -Name logstream -Value Invoke-LogStream -Force

#endregion



