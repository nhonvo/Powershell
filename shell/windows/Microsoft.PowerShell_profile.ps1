# ==============================================================================
#  ENHANCED POWERSHELL PROFILE (Microsoft.PowerShell_profile.ps1)
# ==============================================================================

$skipDll = ($null -ne $config.Environment -and $config.Environment.SkipDllLoad -eq $true) -or $env:AGY_SKIP_DLL_LOAD -eq 'true' -or $env:AGY_SKIP_DLL_LOAD -eq '1'
$loadDll = ($null -ne $config.Environment -and $config.Environment.LoadDll -eq $true)     -or $env:AGY_LOAD_DLL     -eq 'true' -or $env:AGY_LOAD_DLL     -eq '1'

if ($global:AgyUserProfileLoaded) { return }
$global:AgyUserProfileLoaded = $true
if (-not $Global:ProfileRepoRoot) {
    $profileFile = if ($MyInvocation.MyCommand.Definition) { $MyInvocation.MyCommand.Definition } else { $PSCommandPath }
    $curr = if ($PSScriptRoot) { $PSScriptRoot } else { if ($profileFile) { Split-Path -Parent $profileFile } else { (Get-Location).Path } }
    while ($curr -and (Test-Path $curr) -and -not (Test-Path (Join-Path $curr "apps")) -and -not (Test-Path (Join-Path $curr ".git"))) {
        $parent = Split-Path -Parent -Path $curr
        if ($parent -eq $curr -or [string]::IsNullOrEmpty($parent)) { break }
        $curr = $parent
    }
    $Global:ProfileRepoRoot = $curr
}

$Global:AgyTuiDir = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "archive\agytui\AgyTui"
if (-not (Test-Path $Global:AgyTuiDir)) { $Global:AgyTuiDir = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "apps\agytui\AgyTui" }
if (-not (Test-Path $Global:AgyTuiDir)) { $Global:AgyTuiDir = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "csapp\AgyTui" }

#region 1. CONFIG & ENVIRONMENT
# ==============================================================================
#  Loads profile configuration and sets up environment variables.
# ==============================================================================

$configPath = Join-Path -Path $Global:AgyTuiDir -ChildPath "profile.config.json"
if (-not (Test-Path $configPath)) { $configPath = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "profile.config.json" }

$config = @{}
if (Test-Path $configPath) {
    try {
        $rawJson = (Get-Content $configPath -Raw) -replace '(?m)^\s*//.*$', '' -replace '\s*//.*$', ''
        $config = $rawJson | ConvertFrom-Json
    } catch {}
}

# Determine Flag State Matrix: Fast Startup vs Normal Load
# Fast Startup by default (lazy-loads legacy C# assemblies only on demand) for sub-second startup
$fastStartup = ($env:AGY_LOAD_DLL -ne 'true' -and $env:AGY_LOAD_DLL -ne '1') -and ($null -eq $config.Environment -or $config.Environment.LoadDll -ne $true)
$forceLoad   = ($null -ne $config.Environment -and $config.Environment.ForceLoadRedirected -eq $true) -or $env:AGY_FORCE_LOAD_REDIRECTED -eq 'true' -or $env:AGY_LOAD_DLL -eq 'true' -or $env:AGY_LOAD_DLL -eq '1'

# --- Add Windows Suite Binaries to PATH ---
$pathAdditions = @(
    (Join-Path $HOME ".local\bin"),
    (Join-Path $Global:ProfileRepoRoot "dist\windows")
)
foreach ($pa in $pathAdditions) {
    if ((Test-Path $pa) -and ($env:PATH -notlike "*$pa*")) {
        $env:PATH = "$pa;$env:PATH"
    }
}

# --- Module Search Paths ---
$docPaths = [System.Collections.Generic.List[string]]::new()

# Resolve Documents path via .NET SpecialFolder (handles OneDrive / redirected folders)
try {
    $specialDocs = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::MyDocuments)
    if ($specialDocs -and (Test-Path $specialDocs)) { $docPaths.Add($specialDocs) }
} catch {}

# Fallbacks for OneDrive and standard USERPROFILE / HOME Documents
if ($env:OneDrive -and (Test-Path (Join-Path $env:OneDrive "Documents"))) {
    $odDocs = Join-Path $env:OneDrive "Documents"
    if (-not $docPaths.Contains($odDocs)) { $docPaths.Add($odDocs) }
}
$stdDocs = if ($env:USERPROFILE) { Join-Path $env:USERPROFILE "Documents" } else { Join-Path $HOME "Documents" }
if ($stdDocs -and (Test-Path $stdDocs) -and (-not $docPaths.Contains($stdDocs))) {
    $docPaths.Add($stdDocs)
}

# Candidate module directories
$modCandidates = [System.Collections.Generic.List[string]]::new()
foreach ($doc in $docPaths) {
    $modCandidates.Add((Join-Path $doc "PowerShell\Modules"))
    $modCandidates.Add((Join-Path $doc "WindowsPowerShell\Modules"))
}

# Local AppData module directory (PowerShell 7+ / PSResourceGet)
if ($env:LOCALAPPDATA) {
    $modCandidates.Add((Join-Path $env:LOCALAPPDATA "PowerShell\Modules"))
}

# Cross-platform user modules (Linux / WSL pwsh)
if ($HOME) {
    $modCandidates.Add((Join-Path $HOME ".local\share\powershell\Modules"))
}

# Repository local modules (modern layout: shell/modules or modules)
if ($Global:ProfileRepoRoot) {
    $modCandidates.Add((Join-Path $Global:ProfileRepoRoot "shell\modules"))
    $modCandidates.Add((Join-Path $Global:ProfileRepoRoot "modules"))
}

# Register existing module paths into PSModulePath
$sep = [System.IO.Path]::PathSeparator
$existingPaths = ($env:PSModulePath -split [System.Text.RegularExpressions.Regex]::Escape($sep)) | Where-Object { $_ }
foreach ($mc in $modCandidates) {
    if ((Test-Path $mc) -and ($existingPaths -notcontains $mc)) {
        $env:PSModulePath = "$mc$sep$env:PSModulePath"
        $existingPaths += $mc
    }
}

# --- Apply Environment Variables & Themes ---
$themesDir = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "shell\assets\powershell-themes"
if (-not (Test-Path $themesDir) -and (Test-Path (Join-Path $Global:ProfileRepoRoot "shell\assets"))) {
    $themesDir = Join-Path $Global:ProfileRepoRoot "shell\assets"
}
if ($config.Environment -and $config.Environment.PoshThemesPath) {
    $p = $config.Environment.PoshThemesPath
    $env:POSH_THEMES_PATH = if ([System.IO.Path]::IsPathRooted($p)) { $p } else { Join-Path $Global:ProfileRepoRoot $p }
} else {
    $env:POSH_THEMES_PATH = $themesDir
}

if (-not $env:THEME) {
    $themeCandidates = @(
        (Join-Path $HOME ".config\selected_posh_theme.txt"),
        "\\wsl.localhost\Ubuntu\home\truongnhon\.config\selected_posh_theme.txt",
        (Join-Path $HOME ".gemini\selected_theme.txt")
    )
    foreach ($tc in $themeCandidates) {
        if (Test-Path $tc) {
            $tVal = (Get-Content $tc -Raw -ErrorAction SilentlyContinue).Trim()
            if ($tVal) { $env:THEME = $tVal; break }
        }
    }
    if (-not $env:THEME) {
        if ($config.Environment -and $config.Environment.Theme) {
            $env:THEME = "$($config.Environment.Theme)"
        } else {
            $env:THEME = "catppuccin"
        }
    }
}

if ($null -ne $config.Environment.EnableFastStartup)  { $env:AGY_ENABLE_FAST_STARTUP  = if ($config.Environment.EnableFastStartup)  { "true" } else { "false" } }
if ($null -ne $config.Environment.ForceLoadRedirected) { $env:AGY_FORCE_LOAD_REDIRECTED = if ($config.Environment.ForceLoadRedirected) { "true" } else { "false" } }

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

$Global:AgyTuiAppProject = Join-Path -Path $Global:AgyTuiDir -ChildPath "AgyTui.csproj"

function Get-AgyTuiDllPath {
    $candidates = @(
        (Join-Path -Path $Global:AgyTuiDir -ChildPath "dist\AgyTui.dll"),
        (Join-Path -Path $Global:AgyTuiDir -ChildPath "bin\Release\net9.0\AgyTui.dll"),
        (Join-Path -Path $Global:AgyTuiDir -ChildPath "bin\Release\net10.0\AgyTui.dll"),
        (Join-Path -Path $Global:AgyTuiDir -ChildPath "bin\Debug\net9.0\AgyTui.dll"),
        (Join-Path -Path $Global:AgyTuiDir -ChildPath "bin\Debug\net10.0\AgyTui.dll")
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
                $dir = $Global:AgyTuiDir
                if (-not $dir) { return $null }
                $dirs = @(
                    (Join-Path $dir "dist"),
                    (Join-Path $dir "bin\Release\net9.0"),
                    (Join-Path $dir "bin\Release\net10.0"),
                    (Join-Path $dir "bin\Debug\net9.0"),
                    (Join-Path $dir "bin\Debug\net10.0")
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
        $proj = $Global:AgyTuiAppProject
        $needsBuild = [string]::IsNullOrEmpty($targetDll)

        if (-not $needsBuild -and -not $SkipBuildCheck -and (Test-Path $proj)) {
            $dllMtime = (Get-Item $targetDll).LastWriteTime
            $newestCs = Get-ChildItem -Path $Global:AgyTuiDir -Filter "*.cs" -Recurse | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            if ($newestCs -and $newestCs.LastWriteTime -gt $dllMtime) {
                $needsBuild = $true
            }
        }

        if ($needsBuild -and (Test-Path $proj)) {
            try {
                dotnet build "$proj" -c Release | Out-Null
                $targetDll = Get-AgyTuiDllPath
                if ($targetDll) {
                    $distFolder = Join-Path -Path $Global:AgyTuiDir -ChildPath "dist"
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
                    (Join-Path -Path $Global:AgyTuiDir -ChildPath "bin\Release\net9.0"),
                    (Join-Path -Path $Global:AgyTuiDir -ChildPath "bin\Debug\net9.0"),
                    (Join-Path -Path $Global:AgyTuiDir -ChildPath "dist")
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

if ($forceLoad) {
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

function Invoke-GoApp {
    param([string]$AppName, [object[]]$AppArgs)
    $binPath = Join-Path $HOME ".local\bin\$AppName.exe"
    if (-not (Test-Path $binPath) -and $Global:ProfileRepoRoot) {
        $binPath = Join-Path $Global:ProfileRepoRoot "dist\windows\$AppName.exe"
    }
    if (Test-Path $binPath) {
        if ($AppArgs -and $AppArgs.Count -gt 0) { & $binPath @AppArgs } else { & $binPath }
        return
    }
    if (Get-Command $AppName -ErrorAction SilentlyContinue) {
        if ($AppArgs -and $AppArgs.Count -gt 0) { & $AppName @AppArgs } else { & $AppName }
        return
    }
    if (Get-Command wsl -ErrorAction SilentlyContinue) {
        if ($AppArgs -and $AppArgs.Count -gt 0) { wsl $AppName @AppArgs } else { wsl $AppName }
        return
    }
    Write-Host "⚠️ Go engine [$AppName] is not found in PATH, dist\windows, or WSL." -ForegroundColor Yellow
}

function Invoke-AgyRoute {
    param([string]$Alias, $RouteArgs = $null)

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

    $routeKey = if ($Alias) { $Alias.ToLowerInvariant() } else { "" }
    switch ($routeKey) {
        # --- Docker Routes -> agydocker ---
        "dlogsu" {
            if ($flatArgs.Count -gt 0) { Invoke-GoApp "agydocker" @("logs", $flatArgs[0]) }
            else { Invoke-GoApp "agydocker" }
        }
        { $_ -in "dku", "dki", "dkcl" } {
            Invoke-GoApp "agydocker"
        }
        "dimgu" {
            Invoke-GoApp "agydocker"
        }
        "docker-health" {
            Invoke-GoApp "agydocker" @("ram")
        }
        "dkrmac" {
            Invoke-GoApp "agydocker" @("prune")
        }
        "dkstac" {
            $running = docker ps -q 2>$null
            if ($running) { docker stop $running } else { Write-Host "No running containers." -ForegroundColor Green }
        }
        "dcup" {
            docker compose up @flatArgs
        }
        "dcupb" {
            docker compose up --build @flatArgs
        }
        "dcdown" {
            if ($flatArgs.Count -gt 0) { Invoke-GoApp "agydocker" @("down", $flatArgs[0]) }
            else { docker compose down }
        }
        { $_ -in "dkprunev", "dkprunei" } {
            Invoke-GoApp "agydocker" @("prune")
        }

        # --- Git Routes -> agygit ---
        "gsu" {
            Invoke-GoApp "agygit"
        }
        "gd" {
            if ($flatArgs.Count -gt 0) { git diff @flatArgs } else { git diff }
        }
        "glg" {
            Invoke-GoApp "agygit" @("graph")
        }
        "glog" {
            Invoke-GoApp "agygit" @("log")
        }
        { $_ -in "gbr", "gbu" } {
            Invoke-GoApp "agygit"
        }
        "co" {
            git checkout @flatArgs
        }
        "cob" {
            git checkout -b @flatArgs
        }
        "gbd" {
            git branch -d @flatArgs
        }
        "gcmt" {
            if ($flatArgs.Count -gt 0) {
                Invoke-GoApp "agygit" @("commit", ($flatArgs -join " "))
            } else {
                Invoke-GoApp "agygit" @("commit")
            }
        }
        "gca" {
            git commit --amend @flatArgs
        }
        "gclone" {
            git clone @flatArgs
        }
        "gremoteu" {
            git remote -v
        }
        "gco-remote" {
            git checkout -t @flatArgs
        }
        "gmergeu" {
            if ($flatArgs.Count -gt 0) { Invoke-GoApp "agygit" @("merge", $flatArgs[0]) }
            else { Invoke-GoApp "agygit" }
        }
        "gconflict" {
            git diff --name-only --diff-filter=U
        }
        "gstash" {
            git stash @flatArgs
        }
        "grebase" {
            git rebase @flatArgs
        }

        "gundo" {
            Invoke-GoApp "agygit" @("undo")
        }

        # --- Workspace & Projects -> agyproj ---
        { $_ -in "proj", "projects", "cnav" } {
            Invoke-GoApp "agyproj" @flatArgs
        }

        # --- Cockpit / Control Center -> agyx ---
        { $_ -in "cc", "ai", "cockpit" } {
            Invoke-GoApp "agyx" @flatArgs
        }

        # --- .NET SDK Direct Commands ---
        "dru" { dotnet run @flatArgs }
        "dbldu" { dotnet build @flatArgs }
        "dtstu" { dotnet test @flatArgs }
        "dclean" {
            Get-ChildItem -Path . -Include bin,obj -Recurse -Directory -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "🧹 Cleaned all bin/ and obj/ folders." -ForegroundColor Green
        }
        "update-db" { dotnet ef database update @flatArgs }
        "add-migration" { dotnet ef migrations add @flatArgs }
        "dd" { dotnet ef database drop @flatArgs }
        "dremove" { dotnet ef migrations remove @flatArgs }
        "sln" { dotnet new sln @flatArgs }
        "sln-add" { dotnet sln add (Get-ChildItem -Path . -Filter *.csproj -Recurse | Select-Object -ExpandProperty FullName) }
        "console" { dotnet new console @flatArgs }
        "webapi" { dotnet new webapi @flatArgs }
        "dpubpkg" { dotnet pack -c Release @flatArgs }

        # --- AWS / LocalStack Direct Commands ---
        "aws-whoamiu" { aws sts get-caller-identity @flatArgs }
        "aws-s3u" { aws s3 ls @flatArgs }
        "s3mb" { aws s3 mb @flatArgs }
        "aws-local" { aws lambda list-functions @flatArgs }
        "aws-sqs" { aws sqs list-queues @flatArgs }
        "sqsmb" { aws sqs create-queue @flatArgs }
        "sqspurge" { aws sqs purge-queue @flatArgs }
        "sqssend" { aws sqs send-message @flatArgs }
        "sqsrecv" { aws sqs receive-message @flatArgs }
        "sqsattr" { aws sqs get-queue-attributes @flatArgs }

        default {
            if (Get-Command agyx -ErrorAction SilentlyContinue) {
                & agyx $Alias @flatArgs
                return
            }
            $tuiExe = Join-Path $Global:AgyTuiDir "dist\AgyTui.exe"
            if (Test-Path $tuiExe) {
                if ($flatArgs.Count -gt 0) { & $tuiExe $Alias @flatArgs } else { & $tuiExe $Alias }
                return
            }
            Invoke-GoApp "agyx" @flatArgs
        }
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
        if (-not (Get-Module -Name "Terminal-Icons")) {
            Import-Module "Terminal-Icons" -ErrorAction SilentlyContinue
        }
    }
}

[ProfileEnvironment]::ConfigurePSReadLine()
[ProfileEnvironment]::LoadModules()

function Apply-ThemePath {
    param([string]$ThemeName)
    $themeFile = Join-Path $env:POSH_THEMES_PATH "$ThemeName.omp.json"
    if ((Test-Path $themeFile) -and (Get-Command oh-my-posh -ErrorAction SilentlyContinue)) {
        try {
            oh-my-posh init pwsh --config $themeFile | Invoke-Expression
        } catch {
            try {
                oh-my-posh --init --shell pwsh --config $themeFile | Invoke-Expression
            } catch {
                Write-Warning "Failed to initialize oh-my-posh: $_"
            }
        }
    }
}

# Initialize Oh My Posh Theme
Apply-ThemePath $env:THEME
#endregion

#region 4. DOCKER & CONTAINER INTEGRATION
# ==============================================================================
#  Shortcuts and TUI dashboards for Docker and Docker Compose.
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
        $activeAccFile = Join-Path $env:USERPROFILE ".gemini\active_account.txt"
        if (Test-Path -LiteralPath $activeAccFile) {
            $accName = (Get-Content -LiteralPath $activeAccFile -Raw).Trim()
            if ($accName -and $accName -ne "default") {
                $targetHome = Join-Path $env:USERPROFILE ".gemini_$accName"
                if (Test-Path -LiteralPath $targetHome) {
                    $env:GEMINI_HOME = $targetHome
                    try { [System.Environment]::SetEnvironmentVariable("GEMINI_HOME", $targetHome, "User") } catch {}
                }
            } elseif ($accName -eq "default") {
                $targetHome = Join-Path $env:USERPROFILE ".gemini"
                $env:GEMINI_HOME = $targetHome
                try { [System.Environment]::SetEnvironmentVariable("GEMINI_HOME", $targetHome, "User") } catch {}
            }
        } else {
            $userVal = [System.Environment]::GetEnvironmentVariable("GEMINI_HOME", "User")
            if ($userVal -and (Test-Path $userVal)) {
                $env:GEMINI_HOME = $userVal
            }
        }
        $agyHome = if ($env:GEMINI_HOME) { $env:GEMINI_HOME } else { Join-Path $env:USERPROFILE ".gemini" }
        if ($agyHome -ne (Join-Path $env:USERPROFILE ".gemini")) {
            Remove-Item Env:\GEMINI_CLI_IDE_AUTH_TOKEN -ErrorAction SilentlyContinue
            Remove-Item Env:\GEMINI_CLI_IDE_SERVER_PORT -ErrorAction SilentlyContinue
        }

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
    if ($CmdAlias) {
        Invoke-AgyRoute $CmdAlias $PassArgs
    } else {
        Invoke-GoApp "agyx"
    }
    Sync-ActiveAgyEnvironment
}

function Invoke-ControlCenterDev {
    param([string]$CmdAlias, [object[]]$PassArgs)
    if ($CmdAlias) {
        Invoke-AgyRoute $CmdAlias $PassArgs
    } else {
        Invoke-GoApp "agyx"
    }
    Sync-ActiveAgyEnvironment
}

function Invoke-AgyAccount {
    param(
        [Parameter(Position=0)]
        [string]$SubCommand,
        [Parameter(Position=1)]
        [string]$TargetAccount,
        [switch]$Temporary
    )
    $exe = Join-Path $HOME ".local\bin\agyswitch.exe"
    if (-not (Test-Path $exe) -and $Global:ProfileRepoRoot) { $exe = Join-Path $Global:ProfileRepoRoot "dist\windows\agyswitch.exe" }
    if (-not (Test-Path $exe)) {
        $cmd = Get-Command agyswitch.exe -CommandType Application -ErrorAction SilentlyContinue
        if ($cmd) { $exe = $cmd.Source }
    }

    if (Test-Path $exe) {
        $argsList = @()
        if ($SubCommand) {
            switch ($SubCommand.ToLowerInvariant()) {
                "use" { if ($TargetAccount) { $argsList += @("switch", $TargetAccount) } }
                "list" { $argsList += "status" }
                "ls" { $argsList += "status" }
                default {
                    $argsList += $SubCommand
                    if ($TargetAccount) { $argsList += $TargetAccount }
                }
            }
        }
        & $exe @argsList
        return
    }

    if (Get-Command wsl -ErrorAction SilentlyContinue) {
        $wslCmd = "agyswitch"
        if ($SubCommand) { $wslCmd += " $SubCommand" }
        if ($TargetAccount) { $wslCmd += " $TargetAccount" }
        wsl bash -c "$wslCmd"
        return
    }
    Write-Host "⚠️ agyswitch CLI is not found in PATH." -ForegroundColor Yellow
}

function Reset-AgyAccountData { 
    Invoke-AgyAccount status
}
function Invoke-ControlCenterNavigator { Invoke-ControlCenter "cnav" @args }
function Purge-AgyAccounts { 
    Invoke-AgyAccount status
}
function Show-DotNetInfo { Invoke-ControlCenter "dotnet-info" @args }

Set-Alias -Name ai -Value Invoke-MultiAgent -Force
Set-Alias -Name cai -Value Invoke-MultiAgent -Force
Set-Alias -Name cc -Value Invoke-ControlCenter -Force
Set-Alias -Name ccd -Value Invoke-ControlCenterDev -Force
Set-Alias -Name cnav -Value Invoke-ControlCenterNavigator -Force
Set-Alias -Name agy-account -Value Invoke-AgyAccount -Force
Set-Alias -Name agy-acc -Value Invoke-AgyAccount -Force
Set-Alias -Name agyswitch -Value Invoke-AgyAccount -Force
Set-Alias -Name agysw -Value Invoke-AgyAccount -Force

function agyx {
    $bin = Join-Path $HOME ".local\bin\agyx.exe"
    if (-not (Test-Path $bin)) { $bin = Join-Path $Global:ProfileRepoRoot "dist\windows\agyx.exe" }
    if (Test-Path $bin) { & $bin @args; return }
    if (Get-Command wsl -ErrorAction SilentlyContinue) { wsl agyx @args; return }
    Write-Host "❌ agyx binary not found. Please compile via 'make windows'" -ForegroundColor Red
}
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
    $targetPath = if ($Path) { $Path } else { (Get-Location).Path }
    $agyProj = Join-Path $HOME ".local\bin\agyproj.exe"
    if (-not (Test-Path $agyProj)) { $agyProj = Join-Path $Global:ProfileRepoRoot "dist\windows\agyproj.exe" }
    if (Test-Path $agyProj) {
        & $agyProj open $targetPath
        return
    }
    if (Get-Command agyproj -ErrorAction SilentlyContinue) {
        & agyproj open $targetPath
        return
    }
    if (Get-Command code -ErrorAction SilentlyContinue) {
        code $targetPath
        return
    }
    $ideType = Get-AgyType "TerminalIde"
    if ($ideType) { $ideType::Open($targetPath) }
}
function Reload-Profile { . $PROFILE; Write-Host "✅ Profile reloaded." -ForegroundColor Green }

function open-term {
    if ($args) {
        Start-Process wt.exe -ArgumentList $args
    } else {
        if (Get-Command wt.exe -ErrorAction SilentlyContinue) {
            Start-Process wt.exe -ArgumentList "-d `"$($pwd.Path)`""
            return
        }
        $sysType = Get-AgyType "SystemHelper"
        if ($sysType) { $sysType::OpenNewTerminalSession($pwd.Path, [string]$null, $true) }
    }
}

function Set-ShellTheme {
    param([string]$ThemeName)
    $themesDir = $env:POSH_THEMES_PATH
    if (-not $ThemeName) {
        $termBin = Join-Path $HOME ".local\bin\agyterm.exe"
        if (-not (Test-Path $termBin)) { $termBin = Join-Path $Global:ProfileRepoRoot "dist\windows\agyterm.exe" }
        if (Test-Path $termBin) {
            & $termBin
            return
        }
        if (Get-Command agyterm -ErrorAction SilentlyContinue) {
            & agyterm
            return
        }
        if (Get-Command Out-ConsoleGridView -ErrorAction SilentlyContinue) {
            $sel = Get-ChildItem -Path $themesDir -Filter "*.omp.json" | ForEach-Object { $_.BaseName } | Out-ConsoleGridView -Title "Select Oh My Posh Theme (Type to search)" -OutputMode Single
            if ($sel) { Set-ShellTheme $sel }
            return
        }
        Write-Host "🎨 Current Theme: $env:THEME" -ForegroundColor Cyan
        Write-Host "Usage: theme <theme-name>" -ForegroundColor Yellow
        Write-Host "Available themes in your repository:" -ForegroundColor Cyan
        if (Test-Path $themesDir) {
            Get-ChildItem -Path $themesDir -Filter "*.omp.json" | ForEach-Object { $_.BaseName } | Format-Wide -Column 4
        }
        return
    }
    $themeFile = Join-Path $themesDir "$ThemeName.omp.json"
    if (-not (Test-Path $themeFile)) {
        Write-Host "❌ Theme not found: $ThemeName" -ForegroundColor Red
        return
    }
    $env:THEME = $ThemeName
    $configDir = Join-Path $HOME ".config"
    if (-not (Test-Path $configDir)) { New-Item -ItemType Directory -Path $configDir -Force | Out-Null }
    Set-Content -Path (Join-Path $configDir "selected_posh_theme.txt") -Value $ThemeName -Force
    $wslConfigDir = "\\wsl.localhost\Ubuntu\home\truongnhon\.config"
    if (Test-Path $wslConfigDir) {
        try { Set-Content -Path (Join-Path $wslConfigDir "selected_posh_theme.txt") -Value $ThemeName -Force } catch {}
    }
    Apply-ThemePath $ThemeName
    Write-Host "✨ Applied Oh My Posh theme: $ThemeName" -ForegroundColor Green
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
Set-Alias -Name theme -Value Set-ShellTheme -Force
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
    if (-not $Pattern) { Write-Host 'Usage: gf <pattern>' -ForegroundColor Yellow; return }
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

#region 13. AGYX DEVELOPER SUITE — GO DYNAMIC INITIALIZER (OPTION A)
$agyxBin = Join-Path $HOME ".local\bin\agyx.exe"
if (-not (Test-Path $agyxBin) -and $Global:ProfileRepoRoot) {
    $agyxBin = Join-Path $Global:ProfileRepoRoot "dist\windows\agyx.exe"
}
if (Test-Path $agyxBin) {
    $initScript = & $agyxBin init powershell 2>$null
    if ($initScript -and $initScript -notlike "*[agyswitch]*") {
        $sb = [ScriptBlock]::Create($initScript)
        . $sb
    }
} elseif (Get-Command wsl -ErrorAction SilentlyContinue) {
    try {
        $initScript = wsl agyx init powershell 2>$null
        if ($initScript -and $initScript -notlike "*[agyswitch]*") {
            $sb = [ScriptBlock]::Create($initScript)
            . $sb
        }
    } catch {}
}
#endregion

if (-not [Console]::IsOutputRedirected -and [Environment]::UserInteractive) {
    Write-Host "🛸 Enhanced PowerShell Profile Loaded" -ForegroundColor Green
}
#endregion

