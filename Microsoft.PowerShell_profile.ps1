# ==============================================================================
#  ENHANCED POWERSHELL PROFILE (Microsoft.PowerShell_profile.ps1)
# ==============================================================================

$skipDll = ($null -ne $config.Environment -and $config.Environment.SkipDllLoad -eq $true) -or $env:AGY_SKIP_DLL_LOAD -eq 'true' -or $env:AGY_SKIP_DLL_LOAD -eq '1'
$loadDll = ($null -ne $config.Environment -and $config.Environment.LoadDll -eq $true)     -or $env:AGY_LOAD_DLL     -eq 'true' -or $env:AGY_LOAD_DLL     -eq '1'

if ($global:AgyUserProfileLoaded) { return }
$global:AgyUserProfileLoaded = $true
$profileFile = if ($MyInvocation.MyCommand.Definition) { $MyInvocation.MyCommand.Definition } else { $PSCommandPath }
if ($profileFile -and (Test-Path $profileFile -PathType Leaf)) {
    $Global:ProfileRepoRoot = Split-Path -Parent -Path $profileFile
} else {
    $curr = if ($PSScriptRoot) { $PSScriptRoot } else { Get-Location }
    while ($curr -and (Test-Path $curr) -and -not (Test-Path (Join-Path $curr "csapp"))) {
        $parent = Split-Path -Parent -Path $curr
        if ($parent -eq $curr) { break }
        $curr = $parent
    }
    $Global:ProfileRepoRoot = $curr
}

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
    $candidates = @(
        (Join-Path -Path $Global:ProfileRepoRoot -ChildPath "csapp\AgyTui\dist\AgyTui.dll"),
        (Join-Path -Path $Global:ProfileRepoRoot -ChildPath "csapp\AgyTui\bin\Release\net9.0\AgyTui.dll"),
        (Join-Path -Path $Global:ProfileRepoRoot -ChildPath "csapp\AgyTui\bin\Release\net10.0\AgyTui.dll"),
        (Join-Path -Path $Global:ProfileRepoRoot -ChildPath "csapp\AgyTui\bin\Debug\net9.0\AgyTui.dll"),
        (Join-Path -Path $Global:ProfileRepoRoot -ChildPath "csapp\AgyTui\bin\Debug\net10.0\AgyTui.dll")
    )
    foreach ($cand in $candidates) {
        if (Test-Path $cand) { return $cand }
    }
    return $null
}

if ($null -eq $global:AgyAssemblyResolverRegistered) {
    $global:AgyAssemblyResolverRegistered = $true
    try {
        [System.AppDomain]::CurrentDomain.add_AssemblyResolve({
            param($sender, $eventArgs)
            try {
                $asmName = (New-Object System.Reflection.AssemblyName($eventArgs.Name)).Name
                $root = $Global:ProfileRepoRoot
                if (-not $root) { return $null }
                $dirs = @(
                    (Join-Path $root "csapp\AgyTui\dist"),
                    (Join-Path $root "csapp\AgyTui\bin\Release\net9.0"),
                    (Join-Path $root "csapp\AgyTui\bin\Release\net10.0"),
                    (Join-Path $root "csapp\AgyTui\bin\Debug\net9.0"),
                    (Join-Path $root "csapp\AgyTui\bin\Debug\net10.0")
                )
                foreach ($d in $dirs) {
                    $cand = Join-Path $d "$asmName.dll"
                    if (Test-Path $cand) {
                        $b = [System.IO.File]::ReadAllBytes($cand)
                        return [System.Reflection.Assembly]::Load($b)
                    }
                }
            } catch {}
            return $null
        })
    } catch {}
}

function Load-AgyTuiDll {
    param([bool]$SkipBuildCheck = $true, [bool]$ForceLoad = $false)
    $isLoaded = $null -ne ([System.AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq "AgyTui" })
    $shouldLoad = $ForceLoad -or $forceLoad -or (-not $isLoaded)
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
                dotnet build "$proj" -c Release | Out-Null
                $targetDll = Get-AgyTuiDllPath
                if ($targetDll) {
                    $distFolder = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "csapp\AgyTui\dist"
                    if (-not (Test-Path $distFolder)) { New-Item -ItemType Directory -Path $distFolder -Force | Out-Null }
                    $dllFolder = Split-Path $targetDll
                    if ($dllFolder -ne $distFolder) {
                        Get-ChildItem -Path $dllFolder -Filter "*.dll" | ForEach-Object {
                            Copy-Item -Path $_.FullName -Destination $distFolder -Force -ErrorAction SilentlyContinue
                        }
                        $distDll = Join-Path -Path $distFolder -ChildPath "AgyTui.dll"
                        if (Test-Path $distDll) { $targetDll = $distDll }
                    }
                }
            } catch {}
        }

        if ($targetDll -and (Test-Path $targetDll)) {
            try {
                $dllFolder = Split-Path $targetDll
                $searchFolders = @(
                    $dllFolder,
                    (Join-Path -Path $Global:ProfileRepoRoot -ChildPath "csapp\AgyTui\bin\Release\net9.0"),
                    (Join-Path -Path $Global:ProfileRepoRoot -ChildPath "csapp\AgyTui\bin\Debug\net9.0"),
                    (Join-Path -Path $Global:ProfileRepoRoot -ChildPath "csapp\AgyTui\dist")
                ) | Select-Object -Unique | Where-Object { Test-Path $_ }

                foreach ($folder in $searchFolders) {
                    Get-ChildItem -Path $folder -Filter "*.dll" | Where-Object { $_.Name -ne "AgyTui.dll" } | ForEach-Object {
                        try {
                            $bytes = [System.IO.File]::ReadAllBytes($_.FullName)
                            [System.Reflection.Assembly]::Load($bytes) | Out-Null
                        } catch {}
                    }
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
            $exportedTypes = $null
            try { $exportedTypes = $agyAssembly.GetExportedTypes() } catch {
                try { $exportedTypes = $agyAssembly.GetTypes() } catch {}
            }
            if ($exportedTypes) {
                foreach ($type in $exportedTypes) {
                    if ($type.IsClass -and $type.Name -and -not $acc::Get.ContainsKey($type.Name)) {
                        try { $acc::Add($type.Name, $type) } catch {}
                    }
                }
            }
            $aliases = @{
                "ObsidianHelper"     = "ObsidianBridge"
                "StudyHelper"        = "LearnRouter"
                "AccountHelper"      = "AgyAccountStore"
                "AgyAccountManager"  = "AgyAccountStore"
                "AiHelper"           = "AiDashboardView"
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

function Get-AgyType {
    param([string]$TypeName)
    Load-AgyTuiDll
    $t = $TypeName -as [type]
    if ($null -eq $t) {
        $acc = [psobject].Assembly.GetType('System.Management.Automation.TypeAccelerators')
        if ($acc -and $acc::Get.ContainsKey($TypeName)) {
            $t = $acc::Get[$TypeName]
        }
    }
    if ($null -eq $t) {
        $agyAssy = [System.AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq "AgyTui" } | Select-Object -First 1
        if ($agyAssy) {
            try {
                $t = $agyAssy.GetTypes() | Where-Object { $_.Name -eq $TypeName } | Select-Object -First 1
            } catch {
                try {
                    $t = $agyAssy.GetExportedTypes() | Where-Object { $_.Name -eq $TypeName } | Select-Object -First 1
                } catch {}
            }
        }
    }
    return $t
}

function Invoke-AgyRoute {
    param([string]$Alias, $RouteArgs = $null)

    $tuiExe = Join-Path $Global:ProfileRepoRoot "csapp\AgyTui\dist\AgyTui.exe"
    if (-not (Test-Path $tuiExe)) { $tuiExe = Join-Path $Global:ProfileRepoRoot "csapp\AgyTui\bin\Release\net9.0\AgyTui.exe" }
    if (-not (Test-Path $tuiExe)) { $tuiExe = Join-Path $Global:ProfileRepoRoot "csapp\AgyTui\bin\Debug\net9.0\AgyTui.exe" }

    $flatArgs = @()
    if ($RouteArgs) {
        if ($RouteArgs -is [string]) {
            if ($RouteArgs.Trim()) { $flatArgs += $RouteArgs }
        }
        elseif ($RouteArgs -is [System.Collections.IEnumerable]) {
            foreach ($item in $RouteArgs) {
                if ($null -ne $item -and [string]$item -ne "") { $flatArgs += [string]$item }
            }
        } else {
            $flatArgs += [string]$RouteArgs
        }
    }

    if (Test-Path $tuiExe) {
        if ($flatArgs.Count -gt 0) {
            & $tuiExe $Alias @flatArgs
        } else {
            & $tuiExe $Alias
        }
        return
    }

    $proj = Join-Path $Global:ProfileRepoRoot "csapp\AgyTui\AgyTui.csproj"
    if (Test-Path $proj) {
        if ($flatArgs.Count -gt 0) {
            dotnet run --project "$proj" -c Release -- $Alias @flatArgs
        } else {
            dotnet run --project "$proj" -c Release -- $Alias
        }
        return
    }

    $routerType = Get-AgyType "CommandRouter"
    if ($null -ne $routerType) {
        if ($null -ne $RouteArgs) {
            return $routerType::Route($Alias, $RouteArgs)
        } else {
            return $routerType::Route($Alias)
        }
    } else {
        Write-Error "AgyTui binary/component [CommandRouter] could not be resolved."
    }
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
function Invoke-DockerDashboard { Invoke-AgyRoute "dkcl" }
function Invoke-DockerHealth { Invoke-AgyRoute "docker-health" }
function Get-DockerContainers { docker ps @args }
function Get-DockerContainersUI { Invoke-AgyRoute "dku" $args }
function Get-DockerImages { docker images @args }
function Get-DockerImagesUI { Invoke-AgyRoute "dimgu" $args }
function Get-DockerLogs { docker logs @args }
function Get-DockerLogsUI { Invoke-AgyRoute "dlogsu" $args }
function Remove-AllDockerContainers { Invoke-AgyRoute "dkrmac" }
function Stop-AllDockerContainers { Invoke-AgyRoute "dkstac" }
function Invoke-ComposeUp { Invoke-AgyRoute "dcup" $args }
function Invoke-ComposeUpBuild { Invoke-AgyRoute "dcupb" $args }
function Invoke-ComposeDown { Invoke-AgyRoute "dcdown" $args }
function Remove-UnusedDockerVolumes { Invoke-AgyRoute "dkprunev" $args }
function Remove-UnusedDockerImages { Invoke-AgyRoute "dkprunei" $args }

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
function Invoke-GitStatusUI { if ($args) { git status @args } else { Invoke-AgyRoute "gsu" } }
function Show-GitDiff { if ($args) { git diff @args } else { Invoke-AgyRoute "gd" } }
function Get-GitLogGraph { if ($args) { git log --graph --oneline --decorate @args } else { Invoke-AgyRoute "glg" } }
function Get-GitLogPretty { if ($args) { git log --pretty=format:"%h - %an, %ar : %s" @args } else { Invoke-AgyRoute "glog" } }
function Get-GitLog { git log @args }
function Get-GitBranches { git branch @args }
function Get-GitBranchesUI { if ($args) { git branch @args } else { Invoke-AgyRoute "gbr" } }
function Invoke-GitCheckout { if ($args) { git checkout @args } else { Invoke-AgyRoute "co" } }
function New-GitBranch { if ($args) { git checkout -b @args } else { Invoke-AgyRoute "cob" } }
function Remove-GitBranch { if ($args) { git branch -d @args } else { Invoke-AgyRoute "gbd" } }
function Invoke-GitAddAll { if ($args) { git add @args } else { git add . } }
function Invoke-GitUnstage { if ($args) { git restore --staged @args } else { git restore --staged . } }
function Invoke-GitCommit { param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Message) if ($Message) { git commit -m ($Message -join " ") } else { Invoke-AgyRoute "gcmt" } }
function Invoke-GitAmend { if ($args) { git commit --amend @args } else { Invoke-AgyRoute "gca" } }
function Invoke-GitUndo { if ($args) { git reset --soft @args } else { git reset --soft HEAD~1 } }
function Invoke-GitResetSoft { if ($args) { git reset --soft @args } else { git reset --soft HEAD~1 } }
function Invoke-GitResetHard { if ($args) { git reset --hard @args } else { git reset --hard } }
function Invoke-GitFetch { git fetch @args }
function Invoke-GitPull { git pull @args }
function Invoke-GitPush { git push @args }
function Invoke-GitPushForce { git push --force-with-lease @args }
function Invoke-GitCommitWizard { param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Message) if ($Message) { git commit -m ($Message -join " ") } else { Invoke-AgyRoute "gcmt" } }
function Clone-Project { if ($args) { git clone @args } else { Invoke-AgyRoute "gclone" } }
function Get-GitRemotes { git remote -v @args }
function Get-GitRemotesUI { if ($args) { git remote -v @args } else { Invoke-AgyRoute "gremoteu" } }
function Invoke-GitCheckoutRemote { if ($args) { git checkout -t @args } else { Invoke-AgyRoute "gco-remote" } }
function Invoke-GitMerge { if ($args) { git merge @args } else { Invoke-AgyRoute "gmergeu" } }
function Invoke-GitMergeUI { if ($args) { git merge @args } else { Invoke-AgyRoute "gmergeu" } }
function Invoke-GitConflictResolver { Invoke-AgyRoute "gconflict" $args }
function Invoke-GitStashManager { if ($args) { git stash @args } else { Invoke-AgyRoute "gstash" } }
function Invoke-GitRebase { if ($args) { git rebase @args } else { Invoke-AgyRoute "grebase" } }

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
function Invoke-DotNetRunUI { Invoke-AgyRoute "dru" $args }
function Invoke-DotNetWatch { dotnet watch @args }
function Invoke-DotNetBuild { dotnet build @args }
function Invoke-DotNetBuildUI { Invoke-AgyRoute "dbldu" $args }
function Invoke-DotNetFormat { dotnet format @args }
function Invoke-DotNetTest { dotnet test @args }
function Invoke-DotNetTestUI { Invoke-AgyRoute "dtstu" $args }
function Invoke-DotNetWatchTest { dotnet watch test @args }
function Invoke-DotNetClean { dotnet clean @args }
function Invoke-DotNetRestore { dotnet restore @args }
function Remove-BinObj { Invoke-AgyRoute "dclean" $args }
function Update-Database { Invoke-AgyRoute "update-db" $args }
function Add-Migration { Invoke-AgyRoute "add-migration" $args }
function Remove-Database { Invoke-AgyRoute "dd" $args }
function Remove-Migration { Invoke-AgyRoute "dremove" $args }
function New-Solution { Invoke-AgyRoute "sln" $args }
function Add-AllProjectsToSolution { Invoke-AgyRoute "sln-add" $args }
function New-ConsoleProject { Invoke-AgyRoute "console" $args }
function New-WebApiProject { Invoke-AgyRoute "webapi" $args }
function dpack { dotnet pack @args }
function dpubpkg { Invoke-AgyRoute "dpubpkg" $args }

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
function Get-AWSWhoAmIUI { Invoke-AgyRoute "aws-whoamiu" $args }
function Get-S3Buckets { aws s3 ls @args }
function Get-S3BucketsUI { Invoke-AgyRoute "aws-s3u" $args }
function New-S3Bucket { Invoke-AgyRoute "s3mb" $args }
function Get-LambdaFunctions { Invoke-AgyRoute "aws-local" $args }
function Get-LocalSQSQueues { Invoke-AgyRoute "aws-sqs" $args }
function New-LocalSQSQueue { Invoke-AgyRoute "sqsmb" $args }
function Clear-LocalSQSQueue { Invoke-AgyRoute "sqspurge" $args }
function Send-LocalSQSMessage { Invoke-AgyRoute "sqssend" $args }
function Get-LocalSQSMessage { Invoke-AgyRoute "sqsrecv" $args }
function Get-LocalSQSAttributes { Invoke-AgyRoute "sqsattr" $args }

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
#  Delegates AI agent routing and Control Center TUI execution to C# engine.
# ==============================================================================

function Invoke-MultiAgent { param([string]$Query) Invoke-AgyRoute "ai" $Query }

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
    Invoke-AgyRoute $CmdAlias $PassArgs
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
function Invoke-OpenExplorer { Invoke-AgyRoute "f" }
function Invoke-WorkspaceNavigator { param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Name) Invoke-AgyRoute "proj" $Name }
function Invoke-TerminalIde {
    param([string]$Path)
    $targetPath = if ($Path) { $Path } else { Get-Location }
    $ideType = Get-AgyType "TerminalIde"
    if ($ideType) { $ideType::Open($targetPath) }
}
function Reload-Profile { . $PROFILE; Write-Host "✅ Profile reloaded." -ForegroundColor Green }

function open-term {
    if ($args) {
        Start-Process wt.exe -ArgumentList $args
    } else {
        $sysType = Get-AgyType "SystemHelper"
        if ($sysType) { $sysType::OpenNewTerminalSession($pwd.Path, [string]$null, $true) }
    }
}

function Select-ShellTheme {
    $themeType = Get-AgyType "ThemeHelper"
    if ($themeType) { Apply-ThemePath ($themeType::SelectThemeInteractive($env:POSH_THEMES_PATH, $env:THEME)) }
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

#region 11. LINUX CLI WORKSPACE HELPERS
# ==============================================================================
#  Linux-style file inspection, opener, search, and viewing tools.
# ==============================================================================

function Invoke-ViewFile {
    param([string]$Path, [int]$MaxLines = 100)
    if (-not $Path) { Write-Host "Usage: view <filepath> [maxLines]" -ForegroundColor Yellow; return }
    if (-not (Test-Path -LiteralPath $Path)) { Write-Host "File not found: $Path" -ForegroundColor Red; return }

    $fi = Get-Item -LiteralPath $Path
    $lines = Get-Content -LiteralPath $Path -TotalCount $MaxLines
    $totalLines = (Get-Content -LiteralPath $Path | Measure-Object -Line).Lines

    Write-Host "📄 $Path ($totalLines lines · $([math]::Round($fi.Length / 1KB, 1)) KB)" -ForegroundColor Cyan
    Write-Host ("─" * 80) -ForegroundColor DarkGray
    $lineNum = 1
    foreach ($l in $lines) {
        $numStr = "{0:D3}" -f $lineNum
        Write-Host "$numStr │ " -NoNewline -ForegroundColor DarkGray
        Write-Host $l
        $lineNum++
    }
    if ($totalLines -gt $MaxLines) {
        Write-Host "... (showing $MaxLines of $totalLines lines, use 'ide $Path' for full IDE view)" -ForegroundColor DarkGray
    }
    Write-Host ("─" * 80) -ForegroundColor DarkGray
}

function Invoke-OpenFile {
    param([string]$Target)
    if (-not $Target) { $Target = "." }
    if ($Target -match '^https?://') {
        Start-Process $Target
    } elseif (Test-Path -LiteralPath $Target -PathType Container) {
        Invoke-Item -LiteralPath $Target
    } elseif (Test-Path -LiteralPath $Target -PathType Leaf) {
        $ext = [System.IO.Path]::GetExtension($Target).ToLower()
        if ($ext -in @('.txt', '.md', '.json', '.cs', '.ps1', '.py', '.js', '.ts', '.html', '.css', '.yaml', '.yml')) {
            Invoke-TerminalIde -Path $Target
        } else {
            Invoke-Item -LiteralPath $Target
        }
    } else {
        Write-Host "Target not found: $Target" -ForegroundColor Red
    }
}

function Invoke-HeadFile {
    param([string]$Path, [int]$n = 20)
    if (-not $Path) { Write-Host "Usage: head <file> [-n 20]" -ForegroundColor Yellow; return }
    if (-not (Test-Path -LiteralPath $Path)) { Write-Host "File not found: $Path" -ForegroundColor Red; return }
    $lines = Get-Content -LiteralPath $Path -TotalCount $n
    $lineNum = 1
    foreach ($l in $lines) {
        Write-Host ("{0:D3} │ " -f $lineNum) -NoNewline -ForegroundColor DarkGray
        Write-Host $l
        $lineNum++
    }
}

function Invoke-TailFile {
    param([string]$Path, [int]$n = 20)
    if (-not $Path) { Write-Host "Usage: tail <file> [-n 20]" -ForegroundColor Yellow; return }
    if (-not (Test-Path -LiteralPath $Path)) { Write-Host "File not found: $Path" -ForegroundColor Red; return }
    $lines = Get-Content -LiteralPath $Path -Tail $n
    foreach ($l in $lines) {
        Write-Host "│ " -NoNewline -ForegroundColor DarkGray
        Write-Host $l
    }
}

function Invoke-FindFile {
    param([string]$Pattern = "*")
    Get-ChildItem -Recurse -File -Filter "*$Pattern*" -Exclude bin,obj,.git | Select-Object -First 50 | ForEach-Object {
        $rel = Resolve-Path -Relative $_.FullName
        Write-Host "📄 $rel" -ForegroundColor Cyan
    }
}

function Invoke-GrepFile {
    param([string]$Pattern)
    if (-not $Pattern) { Write-Host "Usage: gf <pattern>" -ForegroundColor Yellow; return }
    Get-ChildItem -Recurse -File -Exclude bin,obj,.git | Select-Object -First 300 | Select-String -Pattern $Pattern | ForEach-Object {
        $rel = Resolve-Path -Relative $_.Path
        Write-Host "$rel`:$($_.LineNumber)" -NoNewline -ForegroundColor Yellow
        Write-Host " │ $($_.Line.Trim())"
    }
}

Set-Alias -Name view -Value Invoke-ViewFile -Force
Set-Alias -Name cat-file -Value Invoke-ViewFile -Force
Set-Alias -Name open -Value Invoke-OpenFile -Force
Set-Alias -Name head -Value Invoke-HeadFile -Force
Set-Alias -Name tail -Value Invoke-TailFile -Force
Set-Alias -Name ff -Value Invoke-FindFile -Force
Set-Alias -Name gf -Value Invoke-GrepFile -Force
#endregion

#region 12. SYSTEM UTILITIES & HISTORY
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
