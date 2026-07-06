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
        return
    }

    # Default TUI Control Center Loop
    $startRow = [Console]::CursorTop
    $startCol = [Console]::CursorLeft

    while ($true) {
        $activeAcc = [AgyAccountManager]::GetActiveAccount()
        if ($null -eq $Global:TuiColors) { [TerminalMenu]::InitializeTuiColors() }
        $headerColor = $Global:TuiColors.Header

        # Move cursor back to starting position to redraw in-place (no full-screen clear!)
        try {
            [Console]::SetCursorPosition($startCol, $startRow)
            $clearStr = " " * ([Console]::WindowWidth - 1)
            for ($i = 0; $i -lt 20; $i++) {
                if ($startRow + $i -lt [Console]::WindowHeight) {
                    [Console]::SetCursorPosition(0, $startRow + $i)
                    Write-Host $clearStr -NoNewline
                }
            }
            [Console]::SetCursorPosition($startCol, $startRow)
        } catch {}

        Write-Host "  ▄████▄   ▄████▄     Powershell Profile CLI v2.0" -ForegroundColor $headerColor
        Write-Host " █▀     ▀ █▀     ▀    System dashboard and control suite." -ForegroundColor $headerColor
        Write-Host " █        █           " -ForegroundColor $headerColor
        Write-Host " █▄     ▄ █▄     ▄    Active Account: $activeAcc" -ForegroundColor Gray
        Write-Host "  ▀████▀   ▀████▀     Time:   $(Get-Date -Format 'yyyy-MM-dd HH:mm')" -ForegroundColor DarkGray
        Write-Host "=============================================" -ForegroundColor $headerColor
        Write-Host ""

        $menuItems = @(
            "[Account]  Manage Antigravity Accounts & Credentials",
            "[AI Agent] Select and run local AI agents (Claude, Codex)",
            "[Project]  Navigate to a registered project workspace",
            "[Manual]   Interactive custom commands help reference",
            "[Theme]    Select and apply Oh My Posh shell theme",
            "[SSH Info] View Tailscale & active port 22 sessions",
            "[Exit]     Exit Control Center"
        )

        $selected = ([type]"TerminalMenu")::Show("Profile Control Center Dashboard", $menuItems, 0)
        if ($selected -lt 0 -or $selected -eq ($menuItems.Count - 1)) {
            # Position cursor cleanly below the menu
            try {
                [Console]::SetCursorPosition(0, $startRow + 20)
                Write-Host ""
            } catch {}
            break
        }

        switch ($selected) {
            0 {
                [AgyAccountManager]::ManageAccountsInteractive()
            }
            1 {
                Invoke-MultiAgent
            }
            2 {
                Write-Host ""
                $q = Read-Host "Enter project name / search query"
                if (-not [string]::IsNullOrWhiteSpace($q)) {
                    Enter-Project $q
                    Write-Host "Press any key to return..." -ForegroundColor Gray
                    [void][Console]::ReadKey($true)
                }
            }
            3 {
                [ProfileHelp]::Show("")
            }
            4 {
                Select-ShellTheme
            }
            5 {
                Clear-Host
                Get-SshConnectionInfo
                Write-Host ""
                Write-Host "Press any key to return to Control Center..." -ForegroundColor Gray
                [void][Console]::ReadKey($true)
            }
        }
    }
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
function Invoke-Npm {
    param([string[]]$ArgsList)
    & npm @ArgsList
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
#endregion



