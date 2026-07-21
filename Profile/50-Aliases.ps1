#region CENTRALIZED SHELL ALIASES & WRAPPER FUNCTIONS
# ==============================================================================
# Centralized routing layer bridging CLI commands to the static class helpers.
# (Lazy per-file loading via EnsureHelper is gone — everything above is already
# compiled as part of this single script, so there's no separate file to defer.)
# ==============================================================================

# --- Core Aliases ---
Set-Alias -Name ip -Value Get-NetIPConfiguration -Force
Set-Item -Path Alias:\cls -Value Clear-Host -Force -Option AllScope

# --- Navigation & System Wrappers ---
function Set-LocationParent { Set-Location -Path .. }
function Set-LocationGrandParent { Set-Location -Path ..\.. }
function Invoke-OpenExplorer { [AgyTui.SystemHelper]::OpenExplorer() }
function Enter-Project {
 param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Name)
 $query = $Name -join ' '
 $target = [AgyTui.ProfileNavigator]::Navigate($query)
 if ($target) { Set-Location $target }
}
Set-Alias -Name proj -Value Enter-Project -Force
function Invoke-TerminalIde {
 param([string]$Path)
 $targetPath = if ($Path) { $Path } else { Get-Location }
 [AgyTui.TerminalIde]::Open($targetPath)
}
Set-Alias -Name ide -Value Invoke-TerminalIde -Force
function Reload-Profile {
# Must clear the exact guard variable checked at the top of this file (AgyUserProfileLoaded) —
# this used to clear a differently-named AgyProfileLoaded, so `go` re-dot-sourced $PROFILE but
# hit the top-of-file guard and returned immediately without reloading anything.
 $global:AgyUserProfileLoaded = $false
 $global:PoshInitialized = $false
 . $PROFILE
 Write-Host "✅ Profile reloaded." -ForegroundColor Green
}
function New-DirAndEnter {
 param([string]$Path)
 $null = New-Item -ItemType Directory -Path $Path -Force
 Set-Location $Path
}
function Get-DiskSpace { [AgyTui.SystemHelper]::ShowDiskSpace() }
function Get-PublicIP { [AgyTui.SystemHelper]::GetPublicIP() }
function Get-FileTree {
 param([int]$Depth = 2)
 tree.com /f /a | Select-Object -First (50 * $Depth)
}
function Stop-ProcessFriendly {
 param([string]$Name)
 [AgyTui.SystemHelper]::StopProcessFriendly($Name)
}

function Get-SshConnectionInfo { [AgyTui.SshHelper]::GetConnectionInfo() }
function Add-SshAuthorizedKey {
 param([string]$Key, [string]$Account)
 [AgyTui.SshHelper]::AddAuthorizedKey($Key, $Account)
}

# Applies the (returned) theme path picked/computed by AgyTui.ThemeHelper: reload oh-my-posh and
# re-register the prompt hook in-process — this must happen in the live PS1 session, not in C#.
function Apply-ThemePath {
    param([string]$ThemePath)
    if (-not $ThemePath) { return }
    $Global:AgyOriginalPromptCmd = $null
    Remove-Module -Name "oh-my-posh-core" -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "Function:\prompt" -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "Function:\prompt_original" -Force -ErrorAction SilentlyContinue
    oh-my-posh init pwsh --config $ThemePath | Invoke-Expression
    try {
        $type = [Type]"AgyAccountManager"
        if ($type) {
            [AgyAccountManager]::RegisterPromptHook()
        }
    } catch {}
}
function Toggle-MobileMode {
 Apply-ThemePath ([AgyTui.ThemeHelper]::ToggleMobileMode($env:POSH_THEMES_PATH))
}
function Set-DesktopThemeMode {
 Apply-ThemePath ([AgyTui.ThemeHelper]::SetMobileMode($env:POSH_THEMES_PATH, $false))
}
function Set-MobileThemeMode {
 Apply-ThemePath ([AgyTui.ThemeHelper]::SetMobileMode($env:POSH_THEMES_PATH, $true))
}
function Start-MobileSshKeyReceiver { [AgyTui.SshHelper]::StartMobileSshKeyReceiver() }

# Navigation & System Aliases
Set-Alias -Name .. -Value Set-LocationParent -Force
Set-Alias -Name ... -Value Set-LocationGrandParent -Force
Set-Alias -Name f -Value Invoke-OpenExplorer -Force
Set-Alias -Name go -Value Reload-Profile -Force
Set-Alias -Name mkcd -Value New-DirAndEnter -Force
Set-Alias -Name usage -Value Get-DiskSpace -Force
Set-Alias -Name disk -Value Get-DiskSpace -Force
Set-Alias -Name myip -Value Get-PublicIP -Force
Set-Alias -Name public-ip -Value Get-PublicIP -Force
Set-Alias -Name tree -Value Get-FileTree -Force
Set-Item -Path Alias:\kill -Value Stop-ProcessFriendly -Force -Option AllScope

Set-Alias -Name ssh-info -Value Get-SshConnectionInfo -Force
Set-Alias -Name ssh-addkey -Value Add-SshAuthorizedKey -Force
Set-Alias -Name mobile -Value Toggle-MobileMode -Force
Set-Alias -Name mobile-setup -Value Toggle-MobileMode -Force
Set-Alias -Name ssh-addkey-mobile -Value Start-MobileSshKeyReceiver -Force

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
Set-Alias -Name gs -Value Get-GitStatus -Force
Set-Alias -Name gd -Value Show-GitDiff -Force
Set-Alias -Name glo -Value Get-GitLogGraph -Force
Set-Alias -Name glg -Value Get-GitLogGraph -Force
Set-Alias -Name glog -Value Get-GitLogPretty -Force
Set-Alias -Name gb -Value Get-GitBranches -Force
Set-Alias -Name gbr -Value Get-GitBranches -Force
Set-Alias -Name co -Value Invoke-GitCheckout -Force
Set-Alias -Name cob -Value New-GitBranch -Force
Set-Alias -Name gbd -Value Remove-GitBranch -Force
Set-Alias -Name ga -Value Invoke-GitAddAll -Force
Set-Alias -Name gunstage -Value Invoke-GitUnstage -Force
Set-Alias -Name gcmt -Value Invoke-GitCommit -Force
Set-Alias -Name gca -Value Invoke-GitAmend -Force
Set-Alias -Name gundo -Value Invoke-GitUndo -Force
Set-Alias -Name git-undo -Value Invoke-GitUndo -Force
Set-Alias -Name gr -Value Invoke-GitResetSoft -Force
Set-Alias -Name grh -Value Invoke-GitResetHard -Force
Set-Alias -Name gf -Value Invoke-GitFetch -Force
Set-Alias -Name gpu -Value Invoke-GitPull -Force
Set-Alias -Name gpull -Value Invoke-GitPull -Force
Set-Alias -Name gus -Value Invoke-GitPush -Force
Set-Alias -Name gpush -Value Invoke-GitPush -Force
Set-Alias -Name guf -Value Invoke-GitPushForce -Force
Set-Alias -Name gms -Value Invoke-GitMergeSquash -Force
Set-Alias -Name gsnap -Value Invoke-GitStashSnapshot -Force

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
Set-Alias -Name dr -Value Invoke-DotNetRun -Force
Set-Alias -Name dw -Value Invoke-DotNetWatch -Force
Set-Alias -Name dwatch -Value Invoke-DotNetWatch -Force
Set-Alias -Name db -Value Invoke-DotNetBuild -Force
Set-Alias -Name dbld -Value Invoke-DotNetBuild -Force
Set-Alias -Name rebuild -Value Invoke-DotNetBuild -Force
Set-Alias -Name df -Value Invoke-DotNetFormat -Force
Set-Alias -Name dt -Value Invoke-DotNetTest -Force
Set-Alias -Name dtst -Value Invoke-DotNetTest -Force
Set-Alias -Name wt -Value Invoke-DotNetWatchTest -Force
Set-Alias -Name dcl -Value Invoke-DotNetClean -Force
Set-Alias -Name dres -Value Invoke-DotNetRestore -Force
Set-Alias -Name drestore -Value Invoke-DotNetRestore -Force
Set-Alias -Name dclean -Value Remove-BinObj -Force
Set-Alias -Name clean-build -Value Remove-BinObj -Force
Set-Alias -Name du -Value Update-Database -Force
Set-Alias -Name update-db -Value Update-Database -Force
Set-Alias -Name da -Value Add-Migration -Force
Set-Alias -Name add-migration -Value Add-Migration -Force
Set-Alias -Name dd -Value Remove-Database -Force
Set-Alias -Name dremove -Value Remove-Migration -Force
Set-Alias -Name sln -Value New-Solution -Force
Set-Alias -Name sln-add -Value Add-AllProjectsToSolution -Force
Set-Alias -Name console -Value New-ConsoleProject -Force
Set-Alias -Name webapi -Value New-WebApiProject -Force

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
Set-Alias -Name dkcl -Value Get-DockerContainers -Force
Set-Alias -Name dkrmac -Value Remove-AllDockerContainers -Force
Set-Alias -Name dkstac -Value Stop-AllDockerContainers -Force
Set-Alias -Name dkcpu -Value Invoke-ComposeUp -Force
Set-Alias -Name dcup -Value Invoke-ComposeUp -Force
Set-Alias -Name dkcpub -Value Invoke-ComposeUpBuild -Force
Set-Alias -Name dkcpd -Value Invoke-ComposeDown -Force
Set-Alias -Name dcdown -Value Invoke-ComposeDown -Force
Set-Alias -Name fix-volume -Value Remove-UnusedDockerVolumes -Force
Set-Alias -Name fix-image -Value Remove-UnusedDockerImages -Force

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
function Invoke-Codex-By-Ollama {
    Load-AgyTuiDll
    if ($null -ne ('AgyTui.AgyAiCore' -as [type])) {
        [AgyTui.AgyAiCore]::InvokeCodex($args)
    } else {
        Invoke-ControlCenter "codex-ollama" $args
    }
}
function Invoke-Claude-By-Ollama {
    Load-AgyTuiDll
    if ($null -ne ('AgyTui.AgyAiCore' -as [type])) {
        [AgyTui.AgyAiCore]::InvokeClaude($args)
    } else {
        Invoke-ControlCenter "claude-ollama" $args
    }
}
function Invoke-OpenClaw-By-Ollama {
    Load-AgyTuiDll
    if ($null -ne ('AgyTui.AgyAiCore' -as [type])) {
        [AgyTui.AgyAiCore]::InvokeOpenClaw($args)
    } else {
        Invoke-ControlCenter "openclaw" $args
    }
}
function Invoke-Clawdbot-By-Ollama {
    Load-AgyTuiDll
    if ($null -ne ('AgyTui.AgyAiCore' -as [type])) {
        [AgyTui.AgyAiCore]::InvokeClawdbot($args)
    } else {
        Invoke-ControlCenter "clawdbot" $args
    }
}
function Invoke-Hermes-By-Ollama {
    Load-AgyTuiDll
    if ($null -ne ('AgyTui.AgyAiCore' -as [type])) {
        [AgyTui.AgyAiCore]::InvokeHermes($args)
    } else {
        Invoke-ControlCenter "hermes" $args
    }
}
function Invoke-HermesDesktop-By-Ollama {
    Load-AgyTuiDll
    if ($null -ne ('AgyTui.AgyAiCore' -as [type])) {
        [AgyTui.AgyAiCore]::InvokeHermesDesktop($args)
    } else {
        Invoke-ControlCenter "hermesd" $args
    }
}
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
function Install-AIIntegrations {
    Load-AgyTuiDll
    if ($null -ne ('AgyTui.AgyAiCore' -as [type])) {
        [AgyTui.AgyAiCore]::InstallAIIntegrations()
    } else {
        Invoke-ControlCenter "install-ai"
    }
}
function Initialize-OllamaServer {
    Load-AgyTuiDll
    if ($null -ne ('AgyTui.AgyAiCore' -as [type])) {
        [AgyTui.AgyAiCore]::InitializeOllamaServer()
    } else {
        Invoke-ControlCenter "init-ollama"
    }
}
function Set-OllamaModel {
    param([string]$ModelName)
    Load-AgyTuiDll
    if ($null -ne ('AgyTui.AgyAiCore' -as [type])) {
        [AgyTui.AgyAiCore]::SetOllamaModel($ModelName)
    } else {
        Invoke-ControlCenter "set-model" $ModelName
    }
}
function Ensure-OllamaServer {
    Load-AgyTuiDll
    if ($null -ne ('AgyTui.AgyAiCore' -as [type])) {
        [AgyTui.AgyAiCore]::EnsureOllamaServer()
    } else {
        Invoke-ControlCenter "ensure-ollama"
    }
}
function Invoke-OllamaLogs {
    Load-AgyTuiDll
    if ($null -ne ('AgyTui.AgyAiCore' -as [type])) {
        [AgyTui.AgyAiCore]::ShowOllamaLogs()
    } else {
        Invoke-ControlCenter "ollama-logs"
    }
}
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
        Load-AgyTuiDll
        if ($null -ne ('AgyTui.AgyAiCore' -as [type])) {
            [AgyTui.AgyAiCore]::InvokeOpenClaw(@())
        } else {
            Invoke-ControlCenter "openclaw"
        }
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
    [AgyTui.AgyAiCore]::InvokeMultiAgent($Query, $PSCmdlet.ParameterSetName, $Model)
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

# --- AgyAccountManager Legacy Compatibility Layer ---
try {
    Invoke-Expression @'
class AgyAccountManager {
    static [void] RegisterPromptHook() {
        try {
            $promptCmd = Get-Command prompt -ErrorAction SilentlyContinue
            if ($promptCmd -and ($promptCmd.Definition -like "*AgyOriginalPromptCmd*" -or $promptCmd.Definition -like "*prompt_original*")) {
                if (-not $Global:AgyOriginalPromptCmd) {
                    # Allow re-binding if the session global command reference was cleared
                } else {
                    return
                }
            }

            if (Test-Path Function:\prompt_original) {
                Remove-Item Function:\prompt_original -Force -ErrorAction SilentlyContinue
            }

            if (Test-Path Function:\prompt) {
                $Global:AgyOriginalPromptCmd = Get-Command prompt
                $null = New-Item -Path Function:\prompt_original -Value ([ScriptBlock]::Create("if (`$Global:AgyOriginalPromptCmd) { & `$Global:AgyOriginalPromptCmd }")) -Force
                Remove-Item Function:\prompt -Force -ErrorAction SilentlyContinue
            }

            $scriptBlock = {
                try {
                    $activeAccFile = Join-Path -Path $Global:AgySourceHome -ChildPath "active_account.txt"
                    if (Test-Path $activeAccFile) {
                        $content = (Get-Content -Path $activeAccFile -ErrorAction SilentlyContinue)
                        if ($content) {
                            $savedAcc = $content.Trim()
                            $currentHomeName = Split-Path $env:GEMINI_HOME -Leaf
                            $expectedHomeName = if ($savedAcc -eq "default") { ".gemini" } else { ".gemini_$savedAcc" }
                            if ($currentHomeName -ne $expectedHomeName) {
                                $targetPath = if ($savedAcc -eq "default") {
                                    $Global:AgySourceHome
                                } else {
                                    $p = "C:\Users\Public\.$expectedHomeName"
                                    if (-not (Test-Path $p)) { $p = Join-Path -Path $env:USERPROFILE -ChildPath "$expectedHomeName" }
                                    $p
                                }
                                if (Test-Path $targetPath) {
                                    $env:GEMINI_HOME = $targetPath
                                    [AgyTui.AgyAccountCore]::RestoreActiveToken($savedAcc)
                                }
                            }
                        }
                    }
                } catch {}
                try {
                    [AgyAccountManager]::AutoSwitchOnDirectoryChange($pwd.Path)
                } catch {}
                if (Test-Path Function:\prompt_original) {
                    prompt_original
                } else {
                    $acc = [AgyTui.AgyAccountCore]::GetActiveAccount()
                    $tag = ""
                    "PS ($acc)$tag $($pwd.Path)> "
                }
            }
            $null = New-Item -Path Function:\prompt -Value $scriptBlock -Force
        } catch {}
    }

    static [void] AutoSwitchOnDirectoryChange([string]$Path) {
        if (-not [AgyTui.AgyAccountCore]::IsAutoSwitchEnabled()) { return }
        $workspaces = [AgyTui.WorkspaceRegistry]::GetWorkspaces()
        if (-not $workspaces) { return }
        $matchedProject = $workspaces | Where-Object { $_.WorkspacePath -and $Path -like "$($_.WorkspacePath)*" } | Select-Object -First 1
        if ($null -ne $matchedProject -and $matchedProject.AssociatedAccount -and $matchedProject.AssociatedAccount -ne [AgyTui.AgyAccountCore]::GetActiveAccount()) {
            if (-not $Global:AiMode) {
                Write-Host "[Auto-Switch] Changing credentials to: $($matchedProject.AssociatedAccount)" -ForegroundColor Cyan
            }
            [AgyTui.AgyAccountCore]::SetActiveAccount($matchedProject.AssociatedAccount, $true)
        }
    }

    static [void] ToggleAutoSwitch() {
        [AgyTui.AgyAccountCore]::ToggleAutoSwitch()
    }

    static [void] ShowAllAccountsSummary() {
        [AgyTui.AgyAccountCore]::ShowAllAccountsSummary()
    }

    static [string] GetActiveAccount() {
        return [AgyTui.AgyAccountCore]::GetActiveAccount()
    }
}
'@
} catch {
}

# --- Help shortcuts ---
function Invoke-ControlCenter {
    $debugExe = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "AgyTuiApp\bin\Debug\net10.0\AgyTuiApp.exe"
    $releaseExe = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "AgyTuiApp\dist\AgyTuiApp.exe"
    if (Test-Path $debugExe) {
        & $debugExe @args
    } elseif (Test-Path $releaseExe) {
        & $releaseExe @args
    } else {
        dotnet run --project (Join-Path $Global:ProfileRepoRoot "AgyTuiApp\AgyTuiApp.csproj") -- $args
    }
    $agyHome = if ($Global:AgySourceHome) { $Global:AgySourceHome } else { Join-Path $env:USERPROFILE ".gemini" }
    $selectedProjFile = Join-Path -Path $agyHome -ChildPath "selected_project.txt"
    if ($selectedProjFile -and (Test-Path $selectedProjFile -ErrorAction SilentlyContinue)) {
        $projPath = Get-Content $selectedProjFile -Raw -ErrorAction SilentlyContinue
        if ($projPath -and (Test-Path $projPath.Trim() -ErrorAction SilentlyContinue)) {
            Write-Host "🛸 Navigating to selected workspace: $($projPath.Trim())" -ForegroundColor Cyan
            Set-Location -Path $($projPath.Trim())
        }
        Remove-Item $selectedProjFile -Force -ErrorAction SilentlyContinue
    }
    $selectedThemeFile = Join-Path -Path $agyHome -ChildPath "selected_theme.txt"
    if ($selectedThemeFile -and (Test-Path $selectedThemeFile -ErrorAction SilentlyContinue)) {
        $themePath = Get-Content $selectedThemeFile -Raw -ErrorAction SilentlyContinue
        if ($themePath -and (Test-Path $themePath.Trim() -ErrorAction SilentlyContinue)) {
            Write-Host "🎨 Applying selected theme: $(Split-Path $themePath.Trim() -Leaf)" -ForegroundColor Cyan
            Apply-ThemePath $themePath.Trim()
        }
        Remove-Item $selectedThemeFile -Force -ErrorAction SilentlyContinue
    }
}
Set-Alias -Name cc -Value Invoke-ControlCenter -Force

function cg { Invoke-ControlCenter "gs" }
function cnet { Invoke-ControlCenter "public-ip" }
function csys { Invoke-ControlCenter "disk" }
function cdk { Invoke-ControlCenter "dkcl" }
function cai { Invoke-ControlCenter "claude" }
function caws { Invoke-ControlCenter "aws-local" }
function cnav { Invoke-ControlCenter "proj" }
function cssh { Invoke-ControlCenter "ssh-info" }

# Theme Switcher
function Select-ShellTheme {
 Apply-ThemePath ([AgyTui.ThemeHelper]::SelectThemeInteractive($env:POSH_THEMES_PATH, $env:THEME))
}
Set-Alias -Name theme -Value Select-ShellTheme -Force

function Clone-Project {
 param(
 [Parameter(Mandatory=$true, Position=0)][string]$Url,
 [Parameter(Position=1)][string]$DestName
 )

 $baseDir = $Global:ProjectsBaseDir
 if (-not (Test-Path $baseDir)) {
 $baseDir = Join-Path $env:USERPROFILE "Documents"
 }

 if (-not $DestName) {
 if ($Url -match '/([^/]+)\.git$') {
 $DestName = $Matches[1]
 } elseif ($Url -match '/([^/]+)$') {
 $DestName = $Matches[1]
 } else {
 $DestName = "cloned-project-" + (Get-Random)
 }
 }

 $targetPath = Join-Path $baseDir $DestName
 Write-Host "Cloning project from $Url into $targetPath..." -ForegroundColor Cyan
 git clone $Url $targetPath

 if ($LASTEXITCODE -eq 0 -and (Test-Path $targetPath)) {
 $cacheFile = Join-Path $env:USERPROFILE ".gemini\antigravity\workspace_cache.json"
 Remove-Item $cacheFile -Force -ErrorAction SilentlyContinue
 Write-Host "Project successfully cloned and registered to workspace cache!" -ForegroundColor Green
 } else {
 Write-Error "Failed to clone repository."
 }
}
# Named "gclone", not "clone-project": PowerShell command names are case-insensitive, so an
# alias named "clone-project" collides with the "Clone-Project" function it points to — aliases
# take precedence over functions, so invoking either name tried to resolve the alias again and
# failed outright with "term 'clone-project' is not recognized" (verified via isolated repro).
Set-Alias -Name gclone -Value Clone-Project -Force

# Operations Dashboards & Shortcuts
function Invoke-DockerDashboard { [DockerHelper]::Dkcl() }
Set-Alias -Name dkcl -Value Invoke-DockerDashboard -Force

function Invoke-KillPort {
 param([Parameter(Mandatory=$true, Position=0)][int]$Port)
 [AgyTui.SystemHelper]::KillPort($Port)
}
Set-Alias -Name killport -Value Invoke-KillPort -Force

function Invoke-SystemMonitor { [AgyTui.SystemHelper]::SystemMonitor() }
Set-Alias -Name sysmon -Value Invoke-SystemMonitor -Force

# Scaffolding Shortcuts
function Invoke-ProjectScaffolder { [AgyTui.ProjectScaffolder]::Scaffold() }
Set-Alias -Name new-project -Value Invoke-ProjectScaffolder -Force

# Git TUI Shortcuts
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
 if (-not (Test-AgyAiGate)) { return }
 $q = $Query -join ' '
# $Global:Error is PS1 session state — resolve the "explain my last error" fallback here,
# then hand the C# side a plain resolved string to query the local Ollama model with.
 if ([string]::IsNullOrWhiteSpace($q)) {
 if ($Global:Error -and $Global:Error.Count -gt 0) {
 $lastErr = $Global:Error[0]
 $q = "Last Shell Error:`n$($lastErr | Format-List * -Force | Out-String)"
 if ($lastErr.InvocationInfo) {
 $q += "`nInvocation Line: $($lastErr.InvocationInfo.Line)"
 }
 }
 }
 [AgyTui.AgyAiCore]::AskAi($q)
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
 [AgyTui.AgySecretVault]::SetSecret($Key, $Value)
 }
 "get" {
 if (-not $Key) {
 Write-Error "Usage: sec get <key>"
 return
 }
 $val = [AgyTui.AgySecretVault]::GetSecret($Key)
 if ($val) { Write-Host $val }
 }
 "list" {
 [AgyTui.AgySecretVault]::ListSecrets()
 }
 { $_ -in "remove", "rm" } {
 if (-not $Key) {
 Write-Error "Usage: sec remove <key>"
 return
 }
 [AgyTui.AgySecretVault]::RemoveSecret($Key)
 }
 }
}
Set-Alias -Name sec -Value Invoke-SecretVault -Force

function Invoke-DbTui {
 param([Parameter(Mandatory=$true, Position=0)][string]$DbPath)
 $resolved = Resolve-Path $DbPath -ErrorAction SilentlyContinue
 if (-not $resolved) {
 Write-Error "Database file not found: $DbPath"
 return
 }
 [AgyTui.DatabaseHelper]::ShowDatabaseTui($resolved.Path)
}
Set-Alias -Name db-tui -Value Invoke-DbTui -Force

function Invoke-LogStream {
 param([Parameter(Position=0)][string]$LogPath)
 [AgyTui.LogHelper]::StreamLogs($LogPath)
}
Set-Alias -Name logstream -Value Invoke-LogStream -Force

# --- Antigravity Projects Wrappers ---
function Start-Manager { Start-AgyManager }
Set-Alias -Name mgr -Value Start-Manager -Force

function Start-Proxy { Start-AgyProxy }
Set-Alias -Name prxy -Value Start-Proxy -Force

# --- Antigravity Account Management Wrappers ---
function Toggle-AutoSwitch { [AgyAccountManager]::ToggleAutoSwitch() }
Set-Alias -Name autoswitch -Value Toggle-AutoSwitch -Force

function Show-AccountsSummary { [AgyAccountManager]::ShowAllAccountsSummary() }
Set-Alias -Name acc-sum -Value Show-AccountsSummary -Force

# --- AI Session Dashboard Wrappers ---
function Show-AiDashboard { if (-not (Test-AgyAiGate)) { return }; [AgyTui.AgyAiCore]::ShowAiDashboard() }
Set-Alias -Name ai-dash -Value Show-AiDashboard -Force

# --- AWS LocalStack Aliases ---
Set-Alias -Name s3ls -Value Get-S3Buckets -Force
Set-Alias -Name s3mb -Value New-S3Bucket -Force
Set-Alias -Name lbls -Value Get-LambdaFunctions -Force
Set-Alias -Name sqsls -Value Get-LocalSQSQueues -Force
Set-Alias -Name sqsmb -Value New-LocalSQSQueue -Force
Set-Alias -Name sqspurge -Value Clear-LocalSQSQueue -Force
Set-Alias -Name sqssend -Value Send-LocalSQSMessage -Force
Set-Alias -Name sqsrecv -Value Get-LocalSQSMessage -Force
Set-Alias -Name sqsattr -Value Get-LocalSQSAttributes -Force

# --- System History Wrapper ---
function Clear-ShellHistory { [SystemHelper]::ClearHistory() }
Set-Alias -Name clh -Value Clear-ShellHistory -Force

#endregion

# ==============================================================================
# Startup complete
# ==============================================================================
try {
 [AgyTui.LogHelper]::Log("Enhanced PowerShell Profile loaded successfully. (AiMode = $Global:AiMode)")
} catch {}

if (-not $Global:AiMode) {
 if ($Global:VerboseStartup) {
 Write-Host "🛸 Enhanced PowerShell Profile loaded." -ForegroundColor Green
 } else {
 $acc = "default"
 try {
 $acc = [AgyAccountManager]::GetActiveAccount()
 } catch {}
 Write-Host "🛸 Enhanced Profile Loaded | Account: $acc" -ForegroundColor Green
 }
}
