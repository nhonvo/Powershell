# ==============================================================================
#  ENHANCED POWERSHELL PROFILE (Microsoft.PowerShell_profile.ps1)
# ==============================================================================

$skipDll = ($null -ne $config.Environment -and $config.Environment.SkipDllLoad -eq $true) -or $env:AGY_SKIP_DLL_LOAD -eq 'true'
$loadDll = ($null -ne $config.Environment -and $config.Environment.LoadDll -eq $true)     -or $env:AGY_LOAD_DLL     -eq 'true'

if ($global:AgyUserProfileLoaded) { return }
$global:AgyUserProfileLoaded = $true
$Global:ProfileRepoRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition

#region 1. CONFIG & ENVIRONMENT
# ==============================================================================
#  Loads profile configuration and sets up environment variables.
# ==============================================================================

$configPath = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "csapp\AgyTui\profile.config.json"
if (-not (Test-Path $configPath)) { $configPath = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "csapp\profile.config.json" }
if (-not (Test-Path $configPath)) { $configPath = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "profile.config.json" }

$config = @{}
if (Test-Path $configPath) {
    try {
        $rawJson = (Get-Content $configPath -Raw) -replace '(?m)^\s*//.*$', '' -replace '\s*//.*$', ''
        $config = $rawJson | ConvertFrom-Json
    } catch {}
}

# Determine Flag State Matrix: Fast Startup vs Normal Load
$fastStartup = ($null -ne $config.Environment -and $config.Environment.EnableFastStartup -eq $true) -or $env:AGY_ENABLE_FAST_STARTUP -eq 'true' -or $env:AGY_SKIP_DLL_LOAD -eq 'true'
$forceLoad   = ($null -ne $config.Environment -and $config.Environment.ForceLoadRedirected -eq $true) -or $env:AGY_FORCE_LOAD_REDIRECTED -eq 'true' -or $env:AGY_LOAD_DLL -eq 'true'

# --- Apply Environment Variables from JSON Config ---
if ($config.Environment) {
    if ($config.Environment.PoshThemesPath) {
        $p = $config.Environment.PoshThemesPath
        $env:POSH_THEMES_PATH = if ([System.IO.Path]::IsPathRooted($p)) { $p } else { Join-Path $Global:ProfileRepoRoot $p }
    } else {
        $env:POSH_THEMES_PATH = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "psapp\asset\powershell-themes"
    }

    if ($config.Environment.PsModulePath) {
        $m = $config.Environment.PsModulePath
        $modDir = if ([System.IO.Path]::IsPathRooted($m)) { $m } else { Join-Path $Global:ProfileRepoRoot $m }
        if ((Test-Path $modDir) -and ($env:PSModulePath -notlike "*$modDir*")) {
            $env:PSModulePath = "$modDir;$env:PSModulePath"
        }
    }

    if ($null -ne $config.Environment.EnableFastStartup)  { $env:AGY_ENABLE_FAST_STARTUP  = if ($config.Environment.EnableFastStartup)  { "true" } else { "false" } }
    if ($null -ne $config.Environment.ForceLoadRedirected) { $env:AGY_FORCE_LOAD_REDIRECTED = if ($config.Environment.ForceLoadRedirected) { "true" } else { "false" } }
    if ($config.Environment.Theme)             { $env:THEME                  = "$($config.Environment.Theme)" }
} else {
    $env:POSH_THEMES_PATH = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "psapp\asset\powershell-themes"
    $localModules = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "psapp\Modules"
    if ((Test-Path $localModules) -and ($env:PSModulePath -notlike "*$localModules*")) {
        $env:PSModulePath = "$localModules;$env:PSModulePath"
    }
}

if ($config.Proxy) {
    if ($config.Proxy.HttpProxy)  { $env:HTTP_PROXY  = "$($config.Proxy.HttpProxy)" }
    if ($config.Proxy.HttpsProxy) { $env:HTTPS_PROXY = "$($config.Proxy.HttpsProxy)" }
    if ($config.Proxy.NoProxy)    { $env:NO_PROXY    = "$($config.Proxy.NoProxy)" }
}
#endregion

#region 2. ASSEMBLY & TYPE ACCELERATORS LOADER
# ==============================================================================
#  Loads compiled C# assembly (AgyTui.dll) and registers Type Accelerators.
# ==============================================================================

$Global:AgyTuiAppProject = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "csapp\AgyTui\AgyTui.csproj"

function Get-AgyTuiDllPath {
    $releasePath = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "csapp\AgyTui\bin\Release\net9.0\AgyTui.dll"
    if (Test-Path $releasePath) { return $releasePath }

    $debugPath = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "csapp\AgyTui\bin\Debug\net9.0\AgyTui.dll"
    if (Test-Path $debugPath) { return $debugPath }

    $distPath = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "csapp\AgyTui\dist\AgyTui.dll"
    if (Test-Path $distPath) { return $distPath }

    return $null
}

function Load-AgyTuiDll {
    param([bool]$SkipBuildCheck = $true, [bool]$ForceLoad = $false)
    $shouldLoad = (-not $fastStartup) -and ($ForceLoad -or $forceLoad -or (-not [Console]::IsOutputRedirected))
    if (-not $shouldLoad) { return }

    if ($null -eq ([System.AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq "AgyTui" })) {
        $targetDll = Get-AgyTuiDllPath
        $proj = Join-Path -Path $Global:ProfileRepoRoot "csapp\AgyTui\AgyTui.csproj"
        $needsBuild = [string]::IsNullOrEmpty($targetDll)

        if (-not $needsBuild -and -not $SkipBuildCheck -and (Test-Path $proj)) {
            $dllMtime = (Get-Item $targetDll).LastWriteTime
            $newestCs = Get-ChildItem -Path (Join-Path $Global:ProfileRepoRoot "csapp\AgyTui") -Filter "*.cs" -Recurse | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            if ($newestCs -and $newestCs.LastWriteTime -gt $dllMtime) {
                $needsBuild = $true
            }
        }

        if ($needsBuild -and (Test-Path $proj)) {
            try {
                dotnet build "$proj" -p:TreatWarningsAsErrors=true | Out-Null
                $targetDll = Get-AgyTuiDllPath
            } catch {}
        }

        if ($targetDll -and (Test-Path $targetDll)) {
            try {
                $dllFolder = Split-Path $targetDll
                Get-ChildItem -Path $dllFolder -Filter "*.dll" | Where-Object { $_.Name -ne "AgyTui.dll" } | ForEach-Object {
                    try { Add-Type -Path $_.FullName -ErrorAction SilentlyContinue } catch {}
                }
                Add-Type -Path $targetDll -ErrorAction SilentlyContinue
            } catch {}
        }
    }

    try {
        $acc = [psobject].Assembly.GetType('System.Management.Automation.TypeAccelerators')
        $agyAssembly = [System.AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq "AgyTui" } | Select-Object -First 1
        if ($acc -and $agyAssembly) {
            foreach ($type in $agyAssembly.GetExportedTypes()) {
                if ($type.IsClass -and $type.Name -and -not $acc::Get.ContainsKey($type.Name)) {
                    try { $acc::Add($type.Name, $type) } catch {}
                }
            }
            $aliases = @{
                "ObsidianHelper"     = "ObsidianBridge"
                "StudyHelper"        = "LearnRouter"
                "AccountHelper"      = "AgyAccountStore"
                "AgyAccountManager"  = "AgyAccountStore"
                "AiHelper"           = "AgyAiCore"
                "ThemeHelper"        = "ThemeManager"
                "SshHelper"          = "SshConsoleView"
                "SystemHelper"       = "SystemConsoleView"
                "AntigravityManager" = "AntigravityManagerHelper"
                "AntigravityDeck"    = "AntigravityDeckHelper"
            }
            foreach ($alias in $aliases.Keys) {
                $targetClass = $aliases[$alias]
                if (-not $acc::Get.ContainsKey($alias) -and $acc::Get.ContainsKey($targetClass)) {
                    try { $acc::Add($alias, $acc::Get[$targetClass]) } catch {}
                }
            }
        }
    } catch {}
}

if (-not $fastStartup -and -not [Console]::IsOutputRedirected) {
    Load-AgyTuiDll
}
#endregion

#region 3. PSREADLINE & PROMPT THEME ENGINE
# ==============================================================================
#  Configures PSReadLine options, keybindings, and Oh My Posh theme prompt.
# ==============================================================================

class ProfileEnvironment {
    static [void] ConfigurePSReadLine() {
        Set-PSReadLineOption -EditMode Windows
        $psReadLineCmd = Get-Command Set-PSReadLineOption -ErrorAction SilentlyContinue
        if ($psReadLineCmd -and $psReadLineCmd.Parameters.ContainsKey('PredictionSource')) {
            try {
                $supportsVt = $global:Host.UI.SupportsVirtualTerminal -and -not [Console]::IsOutputRedirected
                if ($supportsVt) {
                    Set-PSReadLineOption -PredictionSource History
                    Set-PSReadLineOption -PredictionViewStyle ListView
                } else {
                    Set-PSReadLineOption -PredictionSource None
                }
            } catch {
                Set-PSReadLineOption -PredictionSource None
            }
        }
        Set-PSReadLineOption -BellStyle None

        $psReadlineColors = @{
            "Command"   = [ConsoleColor]::Green
            "Parameter" = [ConsoleColor]::Gray
            "Operator"  = [ConsoleColor]::Magenta
            "Variable"  = [ConsoleColor]::Yellow
            "String"    = [ConsoleColor]::Cyan
            "Number"    = [ConsoleColor]::White
            "Type"      = [ConsoleColor]::Blue
            "Comment"   = [ConsoleColor]::DarkGreen
            "Keyword"   = [ConsoleColor]::DarkYellow
            "Error"     = [ConsoleColor]::Red
        }
        if ($psReadLineCmd -and $psReadLineCmd.Parameters.ContainsKey('PredictionSource')) {
            $psReadlineColors["InlinePrediction"] = '#70A99F'
        }

        try {
            Set-PSReadlineOption -Color $psReadlineColors
        } catch {}

        if ($global:Host.Name -eq 'ConsoleHost' -and (Get-Command Set-PSReadLineKeyHandler -ErrorAction SilentlyContinue)) {
            Set-PSReadLineKeyHandler -Key UpArrow -Function HistorySearchBackward
            Set-PSReadLineKeyHandler -Key DownArrow -Function HistorySearchForward
            Set-PSReadLineKeyHandler -Chord 'Ctrl+Spacebar' -Function Complete
            Set-PSReadLineKeyHandler -Key F7 -ScriptBlock {
                $command = Get-History | Out-GridView -Title 'Command History' -PassThru
                if ($command) {
                    $pr = [Type]"Microsoft.PowerShell.PSConsoleReadLine"
                    if ($pr) {
                        $pr::RevertLine()
                        $pr::Insert($command.CommandLine)
                    }
                }
            }
        }
    }

    static [void] LoadModules() {
        [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
        $modules = @(
            @{ Name = "PSReadLine";                         Description = "Core CLI Experience" }
            @{ Name = "Terminal-Icons";                     Description = "Rich File Icons" }
            @{ Name = "posh-git";                           Description = "Git Status in Prompt" }
            @{ Name = "Microsoft.PowerShell.ConsoleGuiTools"; Description = "Terminal UI" }
        )
        foreach ($mod in $modules) {
            try {
                Import-Module $mod.Name -ErrorAction Stop
            } catch {
                try {
                    Install-Module $mod.Name -Scope CurrentUser -Force -AllowClobber -SkipPublisherCheck -ErrorAction Stop
                    Import-Module $mod.Name -ErrorAction SilentlyContinue
                } catch {}
            }
        }
    }
}

[ProfileEnvironment]::ConfigurePSReadLine()

# Initialize Oh My Posh Theme
$themePath = Join-Path -Path $env:POSH_THEMES_PATH -ChildPath "$($env:THEME).omp.json"
if ((Test-Path $themePath) -and (Get-Command oh-my-posh -ErrorAction SilentlyContinue)) {
    if (-not $global:PoshInitialized) {
        try {
            oh-my-posh --init --shell pwsh --config $themePath | Invoke-Expression
            $global:PoshInitialized = $true
        } catch {
            Write-Warning "Failed to initialize oh-my-posh: $_"
        }
    }
}
#endregion

#region 4. DOCKER & CONTAINER INTEGRATION
# ==============================================================================
#  Shortcuts and TUI dashboards for Docker and Docker Compose.
# ==============================================================================

function Invoke-DockerDashboard { Load-AgyTuiDll; [CommandRouter]::Route("dkcl") }
function Invoke-DockerHealth { Load-AgyTuiDll; [CommandRouter]::Route("docker-health") }
function Get-DockerContainers { param([switch]$All) if ($All) { docker ps -a } else { docker ps } }
function Remove-AllDockerContainers { Load-AgyTuiDll; [CommandRouter]::Route("dkrmac") }
function Stop-AllDockerContainers { Load-AgyTuiDll; [CommandRouter]::Route("dkstac") }
function Invoke-ComposeUp { Load-AgyTuiDll; [CommandRouter]::Route("dcup", $args) }
function Invoke-ComposeUpBuild { docker-compose up --build $args }
function Invoke-ComposeDown { Load-AgyTuiDll; [CommandRouter]::Route("dcdown", $args) }
function Remove-UnusedDockerVolumes { docker volume prune -f }
function Remove-UnusedDockerImages { docker image prune -af }

Set-Alias -Name dkcl -Value Invoke-DockerDashboard -Force
Set-Alias -Name docker-health -Value Invoke-DockerHealth -Force
Set-Alias -Name dps -Value Get-DockerContainers -Force
Set-Alias -Name containers -Value Get-DockerContainers -Force
Set-Alias -Name dkcpu -Value Invoke-ComposeUp -Force
Set-Alias -Name dcup -Value Invoke-ComposeUp -Force
Set-Alias -Name dkcpub -Value Invoke-ComposeUpBuild -Force
Set-Alias -Name dkcpd -Value Invoke-ComposeDown -Force
Set-Alias -Name dcdown -Value Invoke-ComposeDown -Force
Set-Alias -Name fix-volume -Value Remove-UnusedDockerVolumes -Force
Set-Alias -Name fix-image -Value Remove-UnusedDockerImages -Force
#endregion

#region 5. GIT & VCS INTEGRATION
# ==============================================================================
#  Shortcuts and interactive commit/checkout wizards for Git.
# ==============================================================================

function Invoke-GitStatus { git status $args }
function Show-GitDiff { git diff $args }
function Get-GitLogGraph { git log --graph --oneline --decorate --all }
function Get-GitLogPretty { git log --pretty=format:"%h - %an, %ar : %s" }
function Get-GitLog { Load-AgyTuiDll; [CommandRouter]::Route("glo", $args) }
function Get-GitBranches { Load-AgyTuiDll; [CommandRouter]::Route("gb", $args) }
function Invoke-GitCheckout { param([string]$branchName) Load-AgyTuiDll; [CommandRouter]::Route("co", $branchName) }
function New-GitBranch { param([string]$branchName) git checkout -b $branchName }
function Remove-GitBranch { param([string]$branchName) git branch -d $branchName }
function Invoke-GitAddAll { Load-AgyTuiDll; [CommandRouter]::Route("ga", $args) }
function Invoke-GitUnstage { git restore --staged . }
function Invoke-GitCommit { param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Message) if ($Message) { git commit -m ($Message -join " ") } else { Load-AgyTuiDll; [CommandRouter]::Route("gcmt") } }
function Invoke-GitAmend { git commit --amend $args }
function Invoke-GitUndo { Load-AgyTuiDll; [CommandRouter]::Route("git-undo", $args) }
function Invoke-GitResetSoft { git reset --soft HEAD~1 }
function Invoke-GitResetHard { git reset --hard }
function Invoke-GitFetch { Load-AgyTuiDll; [CommandRouter]::Route("gf", $args) }
function Invoke-GitPull { Load-AgyTuiDll; [CommandRouter]::Route("gpull", $args) }
function Invoke-GitPush { Load-AgyTuiDll; [CommandRouter]::Route("gpush", $args) }
function Invoke-GitPushForce { git push --force $args }
function Invoke-GitCommitWizard { param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Message) Load-AgyTuiDll; $msg = $Message -join ' '; [CommandRouter]::Route("gcmt", $msg) }

function Clone-Project {
    param(
        [Parameter(Mandatory=$true, Position=0)][string]$Url,
        [Parameter(Position=1)][string]$DestName
    )
    $baseDir = Join-Path $env:USERPROFILE "Documents"
    if (-not $DestName) {
        if ($Url -match '/([^/]+)\.git$') { $DestName = $Matches[1] }
        elseif ($Url -match '/([^/]+)$') { $DestName = $Matches[1] }
        else { $DestName = "cloned-project-" + (Get-Random) }
    }
    $targetPath = Join-Path $baseDir $DestName
    Write-Host "Cloning project from $Url into $targetPath..." -ForegroundColor Cyan
    git clone $Url $targetPath
    if ($LASTEXITCODE -eq 0 -and (Test-Path $targetPath)) {
        Write-Host "Project successfully cloned!" -ForegroundColor Green
    } else {
        Write-Error "Failed to clone repository."
    }
}

Set-Alias -Name gs -Value Invoke-GitStatus -Force
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
Set-Alias -Name gcommit -Value Invoke-GitCommit -Force
Set-Alias -Name gcmt -Value Invoke-GitCommitWizard -Force
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
Set-Alias -Name gclone -Value Clone-Project -Force
#endregion

#region 6. DOTNET SDK INTEGRATION
# ==============================================================================
#  Shortcuts and tool wrappers for .NET development.
# ==============================================================================

function Invoke-DotNetRun { Load-AgyTuiDll; [CommandRouter]::Route("dr", $args) }
function Invoke-DotNetWatch { Load-AgyTuiDll; [CommandRouter]::Route("dw", $args) }
function Invoke-DotNetBuild { Load-AgyTuiDll; [CommandRouter]::Route("db", $args) }
function Invoke-DotNetFormat { Load-AgyTuiDll; [CommandRouter]::Route("df", $args) }
function Invoke-DotNetTest { Load-AgyTuiDll; [CommandRouter]::Route("dt", $args) }
function Invoke-DotNetWatchTest { Load-AgyTuiDll; [CommandRouter]::Route("dwatch", $args) }
function Invoke-DotNetClean { Load-AgyTuiDll; [CommandRouter]::Route("dcl", $args) }
function Invoke-DotNetRestore { Load-AgyTuiDll; [CommandRouter]::Route("dres", $args) }
function Remove-BinObj { Load-AgyTuiDll; [CommandRouter]::Route("dclean", $args) }
function Update-Database { Load-AgyTuiDll; [CommandRouter]::Route("update-db", $args) }
function Add-Migration { Load-AgyTuiDll; [CommandRouter]::Route("add-migration", $args) }
function Remove-Database { Load-AgyTuiDll; [CommandRouter]::Route("dd", $args) }
function Remove-Migration { Load-AgyTuiDll; [CommandRouter]::Route("dremove", $args) }
function New-Solution { param([string]$Name) dotnet new sln -n $Name }
function Add-AllProjectsToSolution { Get-ChildItem -Recurse -Filter "*.csproj" | ForEach-Object { dotnet sln add $_.FullName } }
function New-ConsoleProject { param([string]$Name) dotnet new console -n $Name }
function New-WebApiProject { param([string]$Name) dotnet new webapi -n $Name }
function dpack { Load-AgyTuiDll; [CommandRouter]::Route("dpack", $args) }
function dpubpkg { Load-AgyTuiDll; [CommandRouter]::Route("dpubpkg", $args) }

Set-Alias -Name dr -Value Invoke-DotNetRun -Force
Set-Alias -Name dw -Value Invoke-DotNetWatch -Force
Set-Alias -Name dwatch -Value Invoke-DotNetWatch -Force
Set-Alias -Name db -Value Invoke-DotNetBuild -Force
Set-Alias -Name dbld -Value Invoke-DotNetBuild -Force
Set-Alias -Name rebuild -Value Invoke-DotNetBuild -Force
Set-Alias -Name df -Value Invoke-DotNetFormat -Force
Set-Alias -Name dt -Value Invoke-DotNetTest -Force
Set-Alias -Name dtst -Value Invoke-DotNetTest -Force
Set-Alias -Name dwt -Value Invoke-DotNetWatchTest -Force
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
#endregion

#region 7. AWS LOCALSTACK INTEGRATION
# ==============================================================================
#  Shortcuts and wrappers for AWS LocalStack (S3, SQS, Lambda).
# ==============================================================================

function Get-S3Buckets { Load-AgyTuiDll; [CommandRouter]::Route("aws-s3", $args) }
function New-S3Bucket { param([string]$Name) awslocal s3 mb "s3://$Name" }
function Get-LambdaFunctions { Load-AgyTuiDll; [CommandRouter]::Route("aws-local", $args) }
function Get-LocalSQSQueues { Load-AgyTuiDll; [CommandRouter]::Route("aws-sqs", $args) }
function New-LocalSQSQueue { param([string]$QueueName) awslocal sqs create-queue --queue-name=$QueueName }
function Clear-LocalSQSQueue { param([string]$QueueUrl) awslocal sqs purge-queue --queue-url $QueueUrl }
function Send-LocalSQSMessage { param([string]$QueueUrl, [string]$MessageBody, [string]$GroupId) $gid = if ($GroupId) { $GroupId } else { "default-group" }; awslocal sqs send-message --queue-url $QueueUrl --message-body $MessageBody --message-group-id $gid }
function Get-LocalSQSMessage { param([string]$QueueUrl) awslocal sqs receive-message --queue-url $QueueUrl }
function Get-LocalSQSAttributes { param([string]$QueueUrl) awslocal sqs get-queue-attributes --queue-url $QueueUrl --attribute-names All }

Set-Alias -Name s3ls -Value Get-S3Buckets -Force
Set-Alias -Name s3mb -Value New-S3Bucket -Force
Set-Alias -Name lbls -Value Get-LambdaFunctions -Force
Set-Alias -Name sqsls -Value Get-LocalSQSQueues -Force
Set-Alias -Name sqsmb -Value New-LocalSQSQueue -Force
Set-Alias -Name sqspurge -Value Clear-LocalSQSQueue -Force
Set-Alias -Name sqssend -Value Send-LocalSQSMessage -Force
Set-Alias -Name sqsrecv -Value Get-LocalSQSMessage -Force
Set-Alias -Name sqsattr -Value Get-LocalSQSAttributes -Force
#endregion

#region 8. AI & MULTI-AGENT SHORTCUTS
# ==============================================================================
#  Shortcuts for AI agent sessions and routing.
# ==============================================================================

function Invoke-MultiAgent { param([string]$Query) Load-AgyTuiDll; [CommandRouter]::Route("ai", $Query) }
function Invoke-ControlCenter {
    param([string]$CmdAlias, [object[]]$PassArgs)
    $env:ENVIRONMENT = "Production"
    if (-not $CmdAlias -or $CmdAlias -eq "cc") {
        $tuiExe = Join-Path $Global:ProfileRepoRoot "csapp\AgyTui\bin\Release\net9.0\AgyTui.exe"
        if (Test-Path $tuiExe) {
            & $tuiExe
            return
        }
    }
    Load-AgyTuiDll
    [CommandRouter]::Route($CmdAlias, $PassArgs)
}

function Invoke-ControlCenterDev {
    param([string]$CmdAlias, [object[]]$PassArgs)
    $env:ENVIRONMENT = "Development"
    $tuiDevExe = Join-Path $Global:ProfileRepoRoot "csapp\AgyTui\bin\Debug\net9.0\AgyTui.exe"
    if (Test-Path $tuiDevExe) {
        Write-Host "🚀 Launching AgyTui [DEVELOPMENT MODE]..." -ForegroundColor Cyan
        & $tuiDevExe
        return
    }
    Write-Host "🔨 Building & Launching AgyTui [DEVELOPMENT MODE]..." -ForegroundColor Cyan
    Push-Location (Join-Path $Global:ProfileRepoRoot "csapp\AgyTui")
    dotnet run -c Debug -- @PassArgs
    Pop-Location
}

function Reset-AgyAccountData {
    [CmdletBinding()]
    param()

    Write-Host "⚠️ Purging all AGY account data, custom account directories, and token credentials..." -ForegroundColor Yellow
    Invoke-ControlCenter "reset-agy"
}

function Invoke-ControlCenterNavigator { Invoke-ControlCenter "cnav" }
function Purge-AgyAccounts { Invoke-ControlCenter "purge-accounts" }
function Show-DotNetInfo { Invoke-ControlCenter "dotnet-info" }

Set-Alias -Name ai -Value Invoke-MultiAgent -Force
Set-Alias -Name cai -Value Invoke-MultiAgent -Force
Set-Alias -Name claude -Value Invoke-MultiAgent -Force
Set-Alias -Name cc -Value Invoke-ControlCenter -Force
Set-Alias -Name ccd -Value Invoke-ControlCenterDev -Force
Set-Alias -Name cnav -Value Invoke-ControlCenterNavigator -Force
Set-Alias -Name reset-agy -Value Reset-AgyAccountData -Force
Set-Alias -Name purge-accounts -Value Purge-AgyAccounts -Force
Set-Alias -Name dotnet-info -Value Show-DotNetInfo -Force
#endregion

#region 9. NAVIGATION & SYSTEM WRAPPERS
# ==============================================================================
#  Core navigation shortcuts, terminal launchers, and theme switcher.
# ==============================================================================

function Set-LocationParent { Set-Location .. }
function Set-LocationGrandParent { Set-Location ..\.. }
function Invoke-OpenExplorer { Load-AgyTuiDll; [CommandRouter]::Route("f") }
function Invoke-WorkspaceNavigator { param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Name) Load-AgyTuiDll; [CommandRouter]::Route("proj", $Name) }
function Invoke-TerminalIde { param([string]$Path) Load-AgyTuiDll; $targetPath = if ($Path) { $Path } else { Get-Location }; [TerminalIde]::Open($targetPath) }
function Reload-Profile { . $PROFILE; Write-Host "✅ Profile reloaded." -ForegroundColor Green }

function open-term {
    if ($args) {
        Start-Process wt.exe -ArgumentList $args
    } else {
        Load-AgyTuiDll
        [SystemHelper]::OpenNewTerminalSession($pwd.Path, [string]$null, $true)
    }
}

function Select-ShellTheme {
    Load-AgyTuiDll
    Apply-ThemePath ([ThemeHelper]::SelectThemeInteractive($env:POSH_THEMES_PATH, $env:THEME))
}

Set-Alias -Name ip -Value Get-NetIPConfiguration -Force
Set-Item -Path Alias:\cls -Value Clear-Host -Force -Option AllScope
Set-Alias -Name proj -Value Invoke-WorkspaceNavigator -Force
Set-Alias -Name ide -Value Invoke-TerminalIde -Force
Set-Alias -Name .. -Value Set-LocationParent -Force
Set-Alias -Name ... -Value Set-LocationGrandParent -Force
Set-Alias -Name f -Value Invoke-OpenExplorer -Force
Set-Alias -Name go -Value Reload-Profile -Force
Set-Alias -Name term -Value open-term -Force
Set-Alias -Name wt -Value open-term -Force
Set-Alias -Name theme -Value Select-ShellTheme -Force
#endregion

#region 10. SYSTEM UTILITIES & HISTORY
# ==============================================================================
#  System history cleanup and shell startup completion banner.
# ==============================================================================

function Clear-ShellHistory {
    Clear-Host
    Remove-Item (Get-PSReadlineOption).HistorySavePath -ErrorAction SilentlyContinue
    $prType = [Type]"Microsoft.PowerShell.PSConsoleReadLine"
    if ($prType) { $prType::ClearHistory() }
    Clear-History
    Write-Host "🧹 All command history has been cleared." -ForegroundColor Yellow
}
Set-Alias -Name clh -Value Clear-ShellHistory -Force

if (-not [Console]::IsOutputRedirected -and [Environment]::UserInteractive) {
    Write-Host "🛸 Enhanced PowerShell Profile Loaded" -ForegroundColor Green
}
#endregion
