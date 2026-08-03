# ==============================================================================
#  ENHANCED POWERSHELL PROFILE (Microsoft.PowerShell_profile.ps1)
# ==============================================================================

$skipDll = ($null -ne $config.Environment -and $config.Environment.SkipDllLoad -eq $true) -or $env:AGY_SKIP_DLL_LOAD -eq 'true' -or $env:AGY_SKIP_DLL_LOAD -eq '1'
$loadDll = ($null -ne $config.Environment -and $config.Environment.LoadDll -eq $true)     -or $env:AGY_LOAD_DLL     -eq 'true' -or $env:AGY_LOAD_DLL     -eq '1'

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
$fastStartup = ($null -ne $config.Environment -and $config.Environment.EnableFastStartup -eq $true) -or $env:AGY_ENABLE_FAST_STARTUP -eq 'true' -or $env:AGY_ENABLE_FAST_STARTUP -eq '1' -or $env:AGY_SKIP_DLL_LOAD -eq 'true' -or $env:AGY_SKIP_DLL_LOAD -eq '1'
$forceLoad   = ($null -ne $config.Environment -and $config.Environment.ForceLoadRedirected -eq $true) -or $env:AGY_FORCE_LOAD_REDIRECTED -eq 'true' -or $env:AGY_FORCE_LOAD_REDIRECTED -eq '1' -or $env:AGY_LOAD_DLL -eq 'true' -or $env:AGY_LOAD_DLL -eq '1'

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
                    try {
                        $bytes = [System.IO.File]::ReadAllBytes($_.FullName)
                        [System.Reflection.Assembly]::Load($bytes) | Out-Null
                    } catch {}
                }
                $bytes = [System.IO.File]::ReadAllBytes($targetDll)
                [System.Reflection.Assembly]::Load($bytes) | Out-Null
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

if ($forceLoad -or (-not $fastStartup -and -not [Console]::IsOutputRedirected)) {
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

#region 4. DOCKER & CONTAINERS INTEGRATION
# ==============================================================================
function Invoke-DockerDashboard { Load-AgyTuiDll; [CommandRouter]::Route("dkcl") }
function Invoke-DockerHealth { Load-AgyTuiDll; [CommandRouter]::Route("docker-health") }
function Get-DockerContainers { docker ps @args }
function Get-DockerContainersUI { Load-AgyTuiDll; [CommandRouter]::Route("dku", $args) }
function Get-DockerImages { docker images @args }
function Get-DockerImagesUI { Load-AgyTuiDll; [CommandRouter]::Route("dimgu", $args) }
function Get-DockerLogs { docker logs @args }
function Get-DockerLogsUI { Load-AgyTuiDll; [CommandRouter]::Route("dlogsu", $args) }
function Remove-AllDockerContainers { Load-AgyTuiDll; [CommandRouter]::Route("dkrmac") }
function Stop-AllDockerContainers { Load-AgyTuiDll; [CommandRouter]::Route("dkstac") }
function Invoke-ComposeUp { Load-AgyTuiDll; [CommandRouter]::Route("dcup", $args) }
function Invoke-ComposeUpBuild { Load-AgyTuiDll; [CommandRouter]::Route("dcupb", $args) }
function Invoke-ComposeDown { Load-AgyTuiDll; [CommandRouter]::Route("dcdown", $args) }
function Remove-UnusedDockerVolumes { Load-AgyTuiDll; [CommandRouter]::Route("dkprunev", $args) }
function Remove-UnusedDockerImages { Load-AgyTuiDll; [CommandRouter]::Route("dkprunei", $args) }

Set-Alias -Name dk -Value Get-DockerContainers -Force
Set-Alias -Name dku -Value Get-DockerContainersUI -Force
Set-Alias -Name dki -Value Get-DockerContainersUI -Force
Set-Alias -Name dimg -Value Get-DockerImages -Force
Set-Alias -Name dimgu -Value Get-DockerImagesUI -Force
Set-Alias -Name dlogs -Value Get-DockerLogs -Force
Set-Alias -Name dlogsu -Value Get-DockerLogsUI -Force
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

function Invoke-GitStatus { git status @args }
function Invoke-GitStatusUI { Load-AgyTuiDll; [CommandRouter]::Route("gsu", $args) }
function Show-GitDiff { Load-AgyTuiDll; [CommandRouter]::Route("gd", $args) }
function Get-GitLogGraph { Load-AgyTuiDll; [CommandRouter]::Route("glg", $args) }
function Get-GitLogPretty { Load-AgyTuiDll; [CommandRouter]::Route("glog", $args) }
function Get-GitLog { Load-AgyTuiDll; [CommandRouter]::Route("glo", $args) }
function Get-GitBranches { git branch @args }
function Get-GitBranchesUI { Load-AgyTuiDll; [CommandRouter]::Route("gbr", $args) }
function Invoke-GitCheckout { param([string]$branchName) Load-AgyTuiDll; [CommandRouter]::Route("co", $branchName) }
function New-GitBranch { Load-AgyTuiDll; [CommandRouter]::Route("cob", $args) }
function Remove-GitBranch { Load-AgyTuiDll; [CommandRouter]::Route("gbd", $args) }
function Invoke-GitAddAll { Load-AgyTuiDll; [CommandRouter]::Route("ga", $args) }
function Invoke-GitUnstage { Load-AgyTuiDll; [CommandRouter]::Route("gunstage", $args) }
function Invoke-GitCommit { param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Message) if ($Message) { git commit -m ($Message -join " ") } else { Load-AgyTuiDll; [CommandRouter]::Route("gcmt") } }
function Invoke-GitAmend { Load-AgyTuiDll; [CommandRouter]::Route("gca", $args) }
function Invoke-GitUndo { Load-AgyTuiDll; [CommandRouter]::Route("git-undo", $args) }
function Invoke-GitResetSoft { Load-AgyTuiDll; [CommandRouter]::Route("gr", $args) }
function Invoke-GitResetHard { Load-AgyTuiDll; [CommandRouter]::Route("grh", $args) }
function Invoke-GitFetch { Load-AgyTuiDll; [CommandRouter]::Route("gf", $args) }
function Invoke-GitPull { Load-AgyTuiDll; [CommandRouter]::Route("gpull", $args) }
function Invoke-GitPush { Load-AgyTuiDll; [CommandRouter]::Route("gpush", $args) }
function Invoke-GitPushForce { Load-AgyTuiDll; [CommandRouter]::Route("guf", $args) }
function Invoke-GitCommitWizard { param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Message) Load-AgyTuiDll; $msg = $Message -join ' '; [CommandRouter]::Route("gcmt", $msg) }
function Clone-Project { Load-AgyTuiDll; [CommandRouter]::Route("gclone", $args) }
function Get-GitRemotes { git remote -v @args }
function Get-GitRemotesUI { Load-AgyTuiDll; [CommandRouter]::Route("gremoteu", $args) }
function Invoke-GitCheckoutRemote { param([string]$remoteBranch) Load-AgyTuiDll; [CommandRouter]::Route("gco-remote", $remoteBranch) }
function Invoke-GitMerge { param([string]$branchName) Load-AgyTuiDll; [CommandRouter]::Route("gmerge", $branchName) }
function Invoke-GitMergeUI { Load-AgyTuiDll; [CommandRouter]::Route("gmergeu", $args) }
function Invoke-GitConflictResolver { Load-AgyTuiDll; [CommandRouter]::Route("gconflict", $args) }
function Invoke-GitStashManager { Load-AgyTuiDll; [CommandRouter]::Route("gstash", $args) }
function Invoke-GitRebase { param([string]$branchName) Load-AgyTuiDll; [CommandRouter]::Route("grebase", $branchName) }

Set-Alias -Name gs -Value Invoke-GitStatus -Force
Set-Alias -Name gsu -Value Invoke-GitStatusUI -Force
Set-Alias -Name gsi -Value Invoke-GitStatusUI -Force
Set-Alias -Name gd -Value Show-GitDiff -Force
Set-Alias -Name glo -Value Get-GitLogGraph -Force
Set-Alias -Name glg -Value Get-GitLogGraph -Force
Set-Alias -Name glog -Value Get-GitLogPretty -Force
Set-Alias -Name gb -Value Get-GitBranches -Force
Set-Alias -Name gbr -Value Get-GitBranchesUI -Force
Set-Alias -Name gbu -Value Get-GitBranchesUI -Force
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
Set-Alias -Name gremote -Value Get-GitRemotes -Force
Set-Alias -Name grt -Value Get-GitRemotes -Force
Set-Alias -Name gremoteu -Value Get-GitRemotesUI -Force
Set-Alias -Name grtu -Value Get-GitRemotesUI -Force
Set-Alias -Name gco-remote -Value Invoke-GitCheckoutRemote -Force
Set-Alias -Name cor -Value Invoke-GitCheckoutRemote -Force
Set-Alias -Name gmerge -Value Invoke-GitMerge -Force
Set-Alias -Name gm -Value Invoke-GitMerge -Force
Set-Alias -Name gmergeu -Value Invoke-GitMergeUI -Force
Set-Alias -Name gmi -Value Invoke-GitMergeUI -Force
Set-Alias -Name gconflict -Value Invoke-GitConflictResolver -Force
Set-Alias -Name gcf -Value Invoke-GitConflictResolver -Force
Set-Alias -Name gconflictu -Value Invoke-GitConflictResolver -Force
Set-Alias -Name gcfu -Value Invoke-GitConflictResolver -Force
Set-Alias -Name gstash -Value Invoke-GitStashManager -Force
Set-Alias -Name gst -Value Invoke-GitStashManager -Force
Set-Alias -Name gstashu -Value Invoke-GitStashManager -Force
Set-Alias -Name gstu -Value Invoke-GitStashManager -Force
Set-Alias -Name grebase -Value Invoke-GitRebase -Force
Set-Alias -Name grb -Value Invoke-GitRebase -Force
Set-Alias -Name grebaseu -Value Invoke-GitRebase -Force
Set-Alias -Name grbu -Value Invoke-GitRebase -Force
#endregion

#region 6. DOTNET SDK INTEGRATION
# ==============================================================================
#  Shortcuts and tool wrappers for .NET development.
# ==============================================================================

function Invoke-DotNetRun { dotnet run @args }
function Invoke-DotNetRunUI { Load-AgyTuiDll; [CommandRouter]::Route("dru", $args) }
function Invoke-DotNetWatch { dotnet watch @args }
function Invoke-DotNetBuild { dotnet build @args }
function Invoke-DotNetBuildUI { Load-AgyTuiDll; [CommandRouter]::Route("dbldu", $args) }
function Invoke-DotNetFormat { dotnet format @args }
function Invoke-DotNetTest { dotnet test @args }
function Invoke-DotNetTestUI { Load-AgyTuiDll; [CommandRouter]::Route("dtstu", $args) }
function Invoke-DotNetWatchTest { dotnet watch test @args }
function Invoke-DotNetClean { dotnet clean @args }
function Invoke-DotNetRestore { dotnet restore @args }
function Remove-BinObj { Load-AgyTuiDll; [CommandRouter]::Route("dclean", $args) }
function Update-Database { Load-AgyTuiDll; [CommandRouter]::Route("update-db", $args) }
function Add-Migration { Load-AgyTuiDll; [CommandRouter]::Route("add-migration", $args) }
function Remove-Database { Load-AgyTuiDll; [CommandRouter]::Route("dd", $args) }
function Remove-Migration { Load-AgyTuiDll; [CommandRouter]::Route("dremove", $args) }
function New-Solution { Load-AgyTuiDll; [CommandRouter]::Route("sln", $args) }
function Add-AllProjectsToSolution { Load-AgyTuiDll; [CommandRouter]::Route("sln-add", $args) }
function New-ConsoleProject { Load-AgyTuiDll; [CommandRouter]::Route("console", $args) }
function New-WebApiProject { Load-AgyTuiDll; [CommandRouter]::Route("webapi", $args) }
function dpack { dotnet pack @args }
function dpubpkg { Load-AgyTuiDll; [CommandRouter]::Route("dpubpkg", $args) }

Set-Alias -Name dr -Value Invoke-DotNetRun -Force
Set-Alias -Name dru -Value Invoke-DotNetRunUI -Force
Set-Alias -Name dw -Value Invoke-DotNetWatch -Force
Set-Alias -Name dwatch -Value Invoke-DotNetWatch -Force
Set-Alias -Name db -Value Invoke-DotNetBuild -Force
Set-Alias -Name dbld -Value Invoke-DotNetBuild -Force
Set-Alias -Name dbldu -Value Invoke-DotNetBuildUI -Force
Set-Alias -Name dbu -Value Invoke-DotNetBuildUI -Force
Set-Alias -Name rebuild -Value Invoke-DotNetBuild -Force
Set-Alias -Name df -Value Invoke-DotNetFormat -Force
Set-Alias -Name dt -Value Invoke-DotNetTest -Force
Set-Alias -Name dtst -Value Invoke-DotNetTest -Force
Set-Alias -Name dtstu -Value Invoke-DotNetTestUI -Force
Set-Alias -Name dtu -Value Invoke-DotNetTestUI -Force
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

function Get-AWSWhoAmI { aws sts get-caller-identity @args }
function Get-AWSWhoAmIUI { Load-AgyTuiDll; [CommandRouter]::Route("aws-whoamiu", $args) }
function Get-S3Buckets { aws s3 ls @args }
function Get-S3BucketsUI { Load-AgyTuiDll; [CommandRouter]::Route("aws-s3u", $args) }
function New-S3Bucket { Load-AgyTuiDll; [CommandRouter]::Route("s3mb", $args) }
function Get-LambdaFunctions { Load-AgyTuiDll; [CommandRouter]::Route("aws-local", $args) }
function Get-LocalSQSQueues { Load-AgyTuiDll; [CommandRouter]::Route("aws-sqs", $args) }
function New-LocalSQSQueue { Load-AgyTuiDll; [CommandRouter]::Route("sqsmb", $args) }
function Clear-LocalSQSQueue { Load-AgyTuiDll; [CommandRouter]::Route("sqspurge", $args) }
function Send-LocalSQSMessage { Load-AgyTuiDll; [CommandRouter]::Route("sqssend", $args) }
function Get-LocalSQSMessage { Load-AgyTuiDll; [CommandRouter]::Route("sqsrecv", $args) }
function Get-LocalSQSAttributes { Load-AgyTuiDll; [CommandRouter]::Route("sqsattr", $args) }

Set-Alias -Name aws-whoami -Value Get-AWSWhoAmI -Force
Set-Alias -Name aws-whoamiu -Value Get-AWSWhoAmIUI -Force
Set-Alias -Name aws-s3 -Value Get-S3Buckets -Force
Set-Alias -Name aws-s3u -Value Get-S3BucketsUI -Force
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

#region 8. AI & MULTI-AGENT SHORTCUTS
# ==============================================================================
#  Delegates AI agent routing and Control Center TUI execution to C# engine.
# ==============================================================================

function Invoke-MultiAgent { param([string]$Query) Load-AgyTuiDll; [CommandRouter]::Route("ai", $Query) }

function Sync-ActiveAgyEnvironment {
    try {
        $userVal = [System.Environment]::GetEnvironmentVariable("GEMINI_HOME", "User")
        if ($userVal -and (Test-Path $userVal)) { $env:GEMINI_HOME = $userVal }
        $agyHome = if ($env:GEMINI_HOME) { $env:GEMINI_HOME } else { Join-Path $env:USERPROFILE ".gemini" }

        $projFile = Join-Path $agyHome "selected_project.txt"
        if (Test-Path -LiteralPath $projFile) {
            $targetProj = (Get-Content -LiteralPath $projFile -Raw).Trim()
            Remove-Item -LiteralPath $projFile -Force -ErrorAction SilentlyContinue
            if ($targetProj -and (Test-Path -LiteralPath $targetProj)) { Set-Location -LiteralPath $targetProj; Write-Host "📂 Switched workspace directory to: $targetProj" -ForegroundColor Green }
        }

        $themeFile = Join-Path $agyHome "selected_theme.txt"
        if (Test-Path -LiteralPath $themeFile) {
            $targetTheme = (Get-Content -LiteralPath $themeFile -Raw).Trim()
            Remove-Item -LiteralPath $themeFile -Force -ErrorAction SilentlyContinue
            if ($targetTheme) { $env:THEME = $targetTheme; Apply-ThemePath $targetTheme }
        }
    } catch {}
}

function Invoke-ControlCenter {
    param([string]$CmdAlias, [object[]]$PassArgs)
    $env:ENVIRONMENT = "Production"
    $tuiExe = Join-Path $Global:ProfileRepoRoot "csapp\AgyTui\dist\AgyTui.exe"
    if (-not (Test-Path $tuiExe)) { $tuiExe = Join-Path $Global:ProfileRepoRoot "csapp\AgyTui\bin\Release\net9.0\AgyTui.exe" }
    if (Test-Path $tuiExe) {
        if ($CmdAlias) { & $tuiExe $CmdAlias @PassArgs } else { & $tuiExe }
        Sync-ActiveAgyEnvironment
        return
    }
    Load-AgyTuiDll
    [CommandRouter]::Route($CmdAlias, $PassArgs)
    Sync-ActiveAgyEnvironment
}

function Invoke-ControlCenterDev {
    param([string]$CmdAlias, [object[]]$PassArgs)
    $env:ENVIRONMENT = "Development"
    $tuiDevExe = Join-Path $Global:ProfileRepoRoot "csapp\AgyTui\bin\Debug\net9.0\AgyTui.exe"
    if (Test-Path $tuiDevExe) {
        Write-Host "🚀 Launching AgyTui [DEVELOPMENT MODE]..." -ForegroundColor Cyan
        if ($CmdAlias) { & $tuiDevExe $CmdAlias @PassArgs } else { & $tuiDevExe }
        Sync-ActiveAgyEnvironment
        return
    }
    Write-Host "🔨 Building & Launching AgyTui [DEVELOPMENT MODE]..." -ForegroundColor Cyan
    Push-Location (Join-Path $Global:ProfileRepoRoot "csapp\AgyTui")
    if ($CmdAlias) { dotnet run -c Debug -- $CmdAlias @PassArgs } else { dotnet run -c Debug }
    Pop-Location
    Sync-ActiveAgyEnvironment
}

function Reset-AgyAccountData { Invoke-ControlCenter "reset-agy" @args }
function Invoke-ControlCenterNavigator { Invoke-ControlCenter "cnav" @args }
function Purge-AgyAccounts { Invoke-ControlCenter "purge-accounts" @args }
function Show-DotNetInfo { Invoke-ControlCenter "dotnet-info" @args }

Set-Alias -Name ai -Value Invoke-MultiAgent -Force
Set-Alias -Name cai -Value Invoke-MultiAgent -Force
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
