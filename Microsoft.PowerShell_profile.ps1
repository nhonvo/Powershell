
  if ($global:AgyUserProfileLoaded) { return }
$global:AgyUserProfileLoaded = $true
$Global:ProfileRepoRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition

# --- Load configuration from profile.config.json ---
$configPath = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "csapp\profile.config.json"
if (-not (Test-Path $configPath)) { $configPath = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "profile.config.json" }
if (-not (Test-Path $configPath)) {
    $defaultConfig = [ordered]@{
        "_Note_AiMode" = "Terminal mode override: 'auto' (scans env), 'true' (force AI mode), or 'false' (force interactive TUI)."
        "AiMode" = "auto"
        
        "_Note_VerboseStartup" = "Set to true to show verbose load status messages during shell startup."
        "VerboseStartup" = $false
        
        "_Note_StartupLogFile" = "Path to the startup performance log. Leave empty for default: ~/.gemini/antigravity/profile.log"
        "StartupLogFile" = ""
        
        "_Note_PoshThemesPath" = "Path to your oh-my-posh themes folder. Leave empty for default: <RepoRoot>/asset/powershell-themes"
        "PoshThemesPath" = ""
        
        "_Note_ProjectsBaseDir" = "Base directory for development projects. Leave empty to use 'C:\Users\sshuser\project' or '~/Desktop/project'."
        "ProjectsBaseDir" = ""
        
        "_Note_AgySourceHome" = "Base directory for Antigravity settings and accounts. Leave empty for default: C:\Users\Public\.gemini"
        "AgySourceHome" = ""
        
        "_Note_GlobalBinDir" = "Directory containing active Antigravity CLI binary symlinks. Leave empty for default: C:\ProgramData\agy\bin"
        "GlobalBinDir" = ""
    }
    try {
        $defaultConfig | ConvertTo-Json -Depth 4 | Set-Content -Path $configPath -Force
    } catch {}
}

$config = @{}
if (Test-Path $configPath) {
    try {
        $rawJson = (Get-Content $configPath -Raw) -replace '(?m)^\s*//.*$', '' -replace '\s*//.*$', ''
        $config = $rawJson | ConvertFrom-Json
    } catch {}
}

# Apply environment variable POSH_THEMES_PATH
if ($config -and $config.PoshThemesPath) {
    if ([System.IO.Path]::IsPathRooted($config.PoshThemesPath)) {
        $env:POSH_THEMES_PATH = $config.PoshThemesPath
    } else {
        $env:POSH_THEMES_PATH = Join-Path -Path $Global:ProfileRepoRoot -ChildPath $config.PoshThemesPath
    }
} else {
    $env:POSH_THEMES_PATH = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "psapp\asset\powershell-themes"
}

# Add local profile modules directory to PSModulePath
$localModules = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "psapp\Modules"
if ((Test-Path $localModules) -and ($env:PSModulePath -notlike "*$localModules*")) {
    $env:PSModulePath = "$localModules;$env:PSModulePath"
}

# Apply global startup log file path
if ($config -and $config.StartupLogFile) {
    if ([System.IO.Path]::IsPathRooted($config.StartupLogFile)) {
        $Global:AgyStartupLogFile = $config.StartupLogFile
    } else {
        if ($config.StartupLogFile -like "~*") {
            $Global:AgyStartupLogFile = Join-Path -Path $env:USERPROFILE -ChildPath $config.StartupLogFile.Substring(1).TrimStart('\', '/')
        } else {
            $Global:AgyStartupLogFile = Join-Path -Path $Global:ProfileRepoRoot -ChildPath $config.StartupLogFile
        }
    }
} else {
    $Global:AgyStartupLogFile = Join-Path $env:USERPROFILE ".gemini\antigravity\profile.log"
}

$Global:AgyStartupStart = Get-Date

# Apply VerboseStartup
if ($config -and $null -ne $config.VerboseStartup) {
    $Global:VerboseStartup = [System.Convert]::ToBoolean($config.VerboseStartup)
} else {
    $Global:VerboseStartup = $false
}

# Apply AiMode
if ($config -and $null -ne $config.AiMode -and "$($config.AiMode)" -ne "auto") {
    $Global:AiMode = [System.Convert]::ToBoolean($config.AiMode)
} else {
    $Global:AiMode = $false
    $aiMarkers = @(
        "anthropic-code",   # Claude Code
        "vscode-copilot",   # GitHub Copilot CLI
        "codex-agent",      # Codex Agent
        "cursor-terminal"   # Cursor AI terminal
    )
    if ($env:AI_MODE -eq "true" -or
        $env:TERM_PROGRAM -in $aiMarkers -or
        ($null -ne $env:GEMINI_API_KEY -and ($env:TERM -eq "dumb" -or $env:PAGER -eq "cat"))) {
        $Global:AiMode = $true
    }
}

# Apply ProjectsBaseDir
if ($config -and $config.ProjectsBaseDir) {
    $Global:ProjectsBaseDir = $config.ProjectsBaseDir
} else {
    $Global:ProjectsBaseDir = if (Test-Path "$env:USERPROFILE\Desktop\project") { "$env:USERPROFILE\Desktop\project" } elseif (Test-Path "$env:USERPROFILE\project") { "$env:USERPROFILE\project" } else { Join-Path $env:USERPROFILE "Documents" }
}

# Apply AgySourceHome
if ($config -and $config.AgySourceHome) {
    $Global:AgySourceHome = $config.AgySourceHome
} else {
    $Global:AgySourceHome = Join-Path $env:USERPROFILE ".gemini"
}

# Apply GlobalBinDir
if ($config -and $config.GlobalBinDir) {
    $Global:GlobalBinDir = $config.GlobalBinDir
} else {
    $Global:GlobalBinDir = "C:\ProgramData\agy\bin"
}

# --- Startup checkpoint logging ---
# Temporary diagnostic for the "profile loads too long / freezes" reports — appends a
# timestamped, elapsed-ms line to profile.log at each major startup phase. Plain file I/O
# (no dependency on AgyTuiApp.dll, which hasn't loaded yet at the top of this file) so it
# works even if a later phase never returns. Check the tail of the log after a slow/frozen
# session to see exactly which phase was last reached.
function Write-AgyStartupCheckpoint {
    param([string]$Label)
    try {
        $elapsedMs = [Math]::Round(((Get-Date) - $Global:AgyStartupStart).TotalMilliseconds)
        $dir = Split-Path $Global:AgyStartupLogFile
        if (-not (Test-Path $dir)) { $null = New-Item -ItemType Directory -Path $dir -Force }
        Add-Content -Path $Global:AgyStartupLogFile -Value "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] [STARTUP] +$($elapsedMs)ms  $Label"
    } catch {}
}
Write-AgyStartupCheckpoint "script start"
 
# ==============================================================================
# ==============================================================================
#  AGY TUI — compiled C# Spectre.Console application (AgyTuiApp)
# ==============================================================================
$Global:AgyTuiAppProject = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "csapp\AgyTuiApp\AgyTuiApp.csproj"
function Load-AgyTuiDll {
    if ($null -eq ([System.AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq "AgyTuiApp" })) {
        $targetDll = Join-Path -Path $Global:ProfileRepoRoot "csapp\AgyTuiApp\bin\Debug\net10.0\AgyTuiApp.dll"
        if (-not (Test-Path $targetDll)) {
            $targetDll = Join-Path -Path $Global:ProfileRepoRoot "csapp\AgyTuiApp\dist\AgyTuiApp.dll"
        }
        if (-not (Test-Path $targetDll)) {
            $proj = Join-Path -Path $Global:ProfileRepoRoot "csapp\AgyTuiApp\AgyTuiApp.csproj"
            if (Test-Path $proj) {
                dotnet build "$proj" -p:TreatWarningsAsErrors=true | Out-Null
            }
        }
        if (Test-Path $targetDll) {
            try {
                Get-ChildItem -Path (Split-Path $targetDll) -Filter "*.dll" | Where-Object { $_.Name -ne "AgyTuiApp.dll" } | ForEach-Object {
                    try { Add-Type -Path $_.FullName -ErrorAction SilentlyContinue } catch {}
                }
                Add-Type -Path $targetDll -ErrorAction SilentlyContinue
            } catch {}
        }
    }
    
    # Register PowerShell Type Accelerators for shorthand C# helper calls
    try {
        $acc = [psobject].Assembly.GetType('System.Management.Automation.TypeAccelerators')
        if ($acc) {
            foreach ($type in @('GitHelper', 'DotNetHelper', 'DockerHelper', 'SystemHelper', 'AwsHelper', 'ObsidianHelper', 'StudyHelper', 'AccountHelper', 'AiHelper')) {
                $full = "AgyTui.$type" -as [type]
                if ($full -and -not ($acc::Get.ContainsKey($type))) {
                    $acc::Add($type, $full)
                }
            }
        }
    } catch {}
}
Load-AgyTuiDll

function Test-AgyAiGate {
    Load-AgyTuiDll
    if ($null -ne ('AgyTui.AgyAiCore' -as [type])) {
        return [AgyTui.AgyAiCore]::IsAiOllamaEnabled() -or [AgyTui.AgyAiCore]::IsAgyEnabled()
    }
    return $true
}

function Start-AgyManager {
    Load-AgyTuiDll
    if ($null -ne ('AgyTui.Projects' -as [type])) {
        [AgyTui.Projects]::StartManager()
    } elseif ($null -ne ('Projects' -as [type])) {
        [Projects]::StartManager()
    } else {
        Write-Warning "Projects manager not loaded."
    }
}

function Start-AgyProxy {
    Load-AgyTuiDll
    if ($null -ne ('AgyTui.Projects' -as [type])) {
        [AgyTui.Projects]::StartProxy()
    } elseif ($null -ne ('Projects' -as [type])) {
        [Projects]::StartProxy()
    } else {
        Write-Warning "Projects proxy not loaded."
    }
}

Write-AgyStartupCheckpoint "AgyTuiApp subprocess mode ready"
 
# ==============================================================================
#  Enhanced PowerShell Profile — single-file build
#  (Everything below used to live in Profile\Core\*.ps1 / Profile\Helpers\*.ps1,
#   dot-sourced from a loader loop. PowerShell compiles an entire script file as
#   one unit before running any top-level statement, so classes defined anywhere
#   below are visible to each other regardless of order — the old envFile/projFile-
#   first / alphabetical dot-source ordering trick is no longer needed. Only the
#   relative order of top-level *executable* statements with side effects still
#   matters, and that order is preserved exactly as it ran before: ProfileEnvironment
#   init -> oh-my-posh theme -> Projects workspace load -> Helper classes ->
#   AgyAccountManager init -> ProfileHelp -> Aliases -> final banner.)
# ==============================================================================
 
#region PROFILE ENVIRONMENT
# ==============================================================================
#  Shell environment setup, PSReadLine settings, and community modules loading.
# ==============================================================================
 
class ProfileEnvironment {
    static [void] InitializeSession() {
        # Ensure UTF8 for Icons
        [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
 
        if (-not $Global:AiMode -and $Global:VerboseStartup) {
            Write-Host "[*] Loading Enhanced PowerShell Profile... (Core)" -ForegroundColor Cyan
        }
 
 
 
        # --- Module Loading & Auto-Healing ---
        $modules = @(
            @{ Name = "PSReadLine";                         Description = "Core CLI Experience" }
            @{ Name = "z";                                  Description = "Smart Directory Navigation" }
        )
        if (-not $Global:AiMode) {
            $modules += @(
                @{ Name = "Terminal-Icons";                     Description = "Rich File Icons" }
                @{ Name = "posh-git";                           Description = "Git Status in Prompt" }
                @{ Name = "Microsoft.PowerShell.ConsoleGuiTools"; Description = "Terminal UI (Out-ConsoleGridView)" }
                @{ Name = "BurntToast";                         Description = "Windows Notifications" }
            )
        }
 
        foreach ($mod in $modules) {
            # Auto-Install if missing (only in interactive console)
            if (-not (Get-Module -ListAvailable -Name $mod.Name)) {
                if ($Global:AiMode -or [Console]::IsOutputRedirected -or -not [Environment]::UserInteractive) {
                    if (-not $Global:AiMode) {
                        Write-Warning "[!] Module $($mod.Name) is missing and console is non-interactive. Skipping installation."
                    }
                    continue
                }
                Write-Host "[+] Installing $($mod.Name) ($($mod.Description))..." -ForegroundColor Cyan
                try {
                    Install-Module $mod.Name -Scope CurrentUser -Force -AllowClobber -SkipPublisherCheck -ErrorAction Stop
                } catch {
                    Write-Warning "[!] Failed to install $($mod.Name). Skipping."
                    continue
                }
            }
 
            # Safe Import
            try {
                if ($mod.Name -eq "Terminal-Icons") {
                    Import-Module $mod.Name -Force -ErrorAction SilentlyContinue
                } else {
                    Import-Module $mod.Name -ErrorAction SilentlyContinue
                }
            } catch {
                if (-not $Global:AiMode) {
                    Write-Warning "[x] Error loading $($mod.Name): $_"
                }
            }
        }
 
        # --- PSReadLine Options ---
        Set-PSReadLineOption -EditMode Windows
        $psReadLineCmd = Get-Command Set-PSReadLineOption -ErrorAction SilentlyContinue
        if ($psReadLineCmd -and $psReadLineCmd.Parameters.ContainsKey('PredictionSource')) {
            try {
                $supportsVt = -not $Global:AiMode -and $global:Host.UI.SupportsVirtualTerminal -and -not [Console]::IsOutputRedirected
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
 
        # Define colors compatible with both older and newer PSReadLine versions
        $psReadlineColors = @{
            "Command"          = [ConsoleColor]::Green
            "Parameter"        = [ConsoleColor]::Gray
            "Operator"         = [ConsoleColor]::Magenta
            "Variable"         = [ConsoleColor]::Yellow
            "String"           = [ConsoleColor]::Cyan
            "Number"           = [ConsoleColor]::White
            "Type"             = [ConsoleColor]::Blue
            "Comment"          = [ConsoleColor]::DarkGreen
            "Keyword"          = [ConsoleColor]::DarkYellow
            "Error"            = [ConsoleColor]::Red
        }
        if ($psReadLineCmd -and $psReadLineCmd.Parameters.ContainsKey('PredictionSource')) {
            $psReadlineColors["InlinePrediction"] = '#70A99F'
        }
 
        try {
            Set-PSReadlineOption -Color $psReadlineColors
        } catch {}
 
        # --- PSReadLine Key Bindings ---
        if (-not $Global:AiMode -and $global:Host.Name -eq 'ConsoleHost' -and (Get-Command Set-PSReadLineKeyHandler -ErrorAction SilentlyContinue)) {
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
        # .NET Hotkeys
        if (-not $Global:AiMode) {
            Set-PSReadLineKeyHandler -Key 'Ctrl+Shift+b' -ScriptBlock {
                $pr = [Type]"Microsoft.PowerShell.PSConsoleReadLine"
                if ($pr) {
                    $pr::RevertLine()
                    $pr::Insert('db')
                    $pr::AcceptLine()
                }
            }
            Set-PSReadLineKeyHandler -Key 'Ctrl+Shift+t' -ScriptBlock {
                $pr = [Type]"Microsoft.PowerShell.PSConsoleReadLine"
                if ($pr) {
                    $pr::RevertLine()
                    $pr::Insert('dt')
                    $pr::AcceptLine()
                }
            }
            # Launches the C# Control Center TUI directly (same target as the `cc` alias) —
            # kept as a hotkey per the decision to keep the alias surface but add a fast path in.
            Set-PSReadLineKeyHandler -Key 'Ctrl+Shift+c' -ScriptBlock {
                $pr = [Type]"Microsoft.PowerShell.PSConsoleReadLine"
                if ($pr) {
                    $pr::RevertLine()
                    $pr::Insert('cc')
                    $pr::AcceptLine()
                }
            }
        }
    }
}
 
[ProfileEnvironment]::InitializeSession()
Write-AgyStartupCheckpoint "ProfileEnvironment.InitializeSession (modules, PSReadLine) done"
 
# --- Oh My Posh Theme (Initialized in global script scope to bypass class method scoping constraints) ---
if (-not $Global:AiMode) {
    # Theme name resolution (+ legacy active_theme.txt migration) lives in
    # [AgyTui.ThemeHelper]::ResolveStartupTheme() now — pure file/JSON logic, no PS1-only dependency.
    # This runs unconditionally on every shell startup (not deferred inside a method like every
    # other AgyTui call site), so if AgyTui.dll isn't loaded yet it must not break oh-my-posh
    # entirely — fall back to a bare default rather than let the whole file abort.
    try {
        $configPath = Join-Path -Path $env:POSH_THEMES_PATH -ChildPath "config.json"
        $legacyPath = Join-Path -Path $env:POSH_THEMES_PATH -ChildPath "active_theme.txt"
        if (Test-Path $configPath) {
            $cfg = Get-Content $configPath -Raw -ErrorAction SilentlyContinue | ConvertFrom-Json -ErrorAction SilentlyContinue
            if ($cfg -and $cfg.active_theme) {
                $env:THEME = $cfg.active_theme
            } else {
                $env:THEME = "neko"
            }
        } elseif (Test-Path $legacyPath) {
            $env:THEME = (Get-Content $legacyPath -Raw -ErrorAction SilentlyContinue).Trim()
        } else {
            $env:THEME = "neko"
        }
    } catch {
        $env:THEME = "neko"
    }
    $themePath = Join-Path -Path $env:POSH_THEMES_PATH -ChildPath "$($env:THEME).omp.json"
    if (Test-Path $themePath) {
        if (-not $global:PoshInitialized) {
            try {
                oh-my-posh --init --shell pwsh --config $themePath | Invoke-Expression
                $global:PoshInitialized = $true
            } catch {
                Write-Warning "Failed to initialize oh-my-posh: $_"
            }
        }
    } else {
        Write-Warning "Oh My Posh theme '$($env:THEME)' not found at '$themePath'."
    }
}
 
Write-AgyStartupCheckpoint "oh-my-posh init block done"
#endregion

#region GIT HELPER
# ==============================================================================
#  Shortcuts and wrappers for common Git operations.
#
#  Embedded via Invoke-Expression on an escaped-from-parsing string, not defined
#  directly in this script's own AST: BranchCheckoutTui/Gcmt/GenerateAiCommitMessage
#  reference [AgyTui.*] types inside the class body, and PowerShell resolves class-body
#  type refs at parse time. If this class were a literal part of this file, an
#  unresolvable type (pre-PowerShell-7.6, before AgyTuiApp.dll's .NET 10 types can
#  load) would abort parsing of the ENTIRE file — verified empirically: nothing in the
#  whole script would run, not even code physically before the failing class.
#  Invoke-Expression gives this class its own independent parse pass, wrapped in
#  try/catch, so a failure here only disables the git aliases instead of all ~100.
# ==============================================================================
try {
    Load-AgyTuiDll
    Invoke-Expression @'
class GitHelper {
    static [void] Status([string[]]$PassThruArgs) {
        Write-Host "[Git] Status" -ForegroundColor Yellow
        if ($PassThruArgs) { git status $PassThruArgs | Out-Default } else { git status | Out-Default }
    }
 
    static [void] Undo() {
        Write-Host "[Git] Undoing last commit (keeping changes)..." -ForegroundColor Yellow
        git reset --soft HEAD~1 | Out-Default
    }
 
    static [void] Unstage() {
        Write-Host "[Git] Unstaging changes..." -ForegroundColor Yellow
        git restore --staged . | Out-Default
    }
 
    static [void] StashSnapshot([string]$Message) {
        $msg = if ($Message) { $Message } else { "snapshot" }
        git stash push -m "$msg" | Out-Default
        git stash apply 0 | Out-Default
        Write-Host "[Git] Snapshot stashed: $msg" -ForegroundColor Green
    }
 
    static [void] Diff() {
        git diff | Out-Default
    }
 
    static [void] AddAll() {
        Write-Host "[Git] Staging all changes..." -ForegroundColor Green
        git add . | Out-Default
    }
 
    static [void] Commit([string[]]$Message) {
        $commitMessage = $Message -join ' '
        Write-Host "[Git] Committing with message: '$commitMessage'" -ForegroundColor Cyan
        git commit -m "$commitMessage" | Out-Default
    }
 
    static [void] Amend([string[]]$PassThruArgs) {
        Write-Host "[Git] Amending previous commit..." -ForegroundColor Cyan
        if ($PassThruArgs) { git commit --amend $PassThruArgs | Out-Default } else { git commit --amend | Out-Default }
    }
 
    static [void] Checkout([string]$branchName) {
        Write-Host "Check out branch: $branchName" -ForegroundColor Green
        git checkout $branchName | Out-Default
    }
 
    static [void] NewBranch([string]$branchName) {
        Write-Host "Check out and create a new branch: $branchName" -ForegroundColor Green
        git checkout -b $branchName | Out-Default
    }
 
    static [void] LogGraph() {
        git log --graph --oneline --decorate --all | Out-Default
    }
 
    static [void] LogPretty() {
        git log --pretty=format:"%C(yellow)%h%Creset -%C(red)%d%Creset %s %C(green)(%cr) %C(bold blue)<%an>%Creset" --abbrev-commit | Out-Default
    }
 
    static [void] Log() {
        git log | Out-Default
    }
 
    static [void] Pull([string[]]$PassThruArgs) {
        Write-Host "[Git] Pulling changes from remote..." -ForegroundColor Blue
        if ($PassThruArgs) { git pull $PassThruArgs | Out-Default } else { git pull | Out-Default }
    }
 
    static [void] Push([string[]]$PassThruArgs) {
        Write-Host "[Git] Pushing changes to remote..." -ForegroundColor Blue
        if ($PassThruArgs) { git push $PassThruArgs | Out-Default } else { git push | Out-Default }
    }
 
    static [void] PushForce([string[]]$PassThruArgs) {
        Write-Host "[Git] Force pushing changes..." -ForegroundColor Red
        if ($PassThruArgs) { git push --force $PassThruArgs | Out-Default } else { git push --force | Out-Default }
    }
 
    static [void] Fetch() {
        Write-Host "[Git] Fetching from all remotes..." -ForegroundColor Blue
        git fetch --all --prune | Out-Default
    }
 
    static [void] GetBranches([string[]]$PassThruArgs) {
        Write-Host "[Git] Branches:" -ForegroundColor Green
        if ($PassThruArgs) { git branch $PassThruArgs | Out-Default } else { git branch | Out-Default }
    }
 
    static [void] RemoveBranch([string]$branchName) {
        git branch -d $branchName | Out-Default
    }
 
    static [void] MergeSquash([string]$BranchName) {
        Write-Host "Squash merging branch: $BranchName" -ForegroundColor Yellow
        git merge --squash $BranchName | Out-Default
        Write-Host "Commit the squashed changes!" -ForegroundColor Cyan
    }
 
    static [void] ResetSoft() {
        git reset HEAD~ | Out-Default
    }
 
    static [void] ResetHard() {
        git reset --hard | Out-Default
    }
 
    static [void] BranchCheckoutTui() {
        if (-not (Test-Path ".git")) {
            Write-Error "Not a git repository."
            return
        }
        $branches = @()
        try {
            $raw = git branch --format="%(refname:short)" 2>$null
            if ($null -ne $raw) {
                foreach ($b in $raw) {
                    if (-not [string]::IsNullOrWhiteSpace($b)) {
                        $branches += $b.Trim()
                    }
                }
            }
        } catch {}
 
        if ($Global:AiMode) {
            foreach ($b in $branches) {
                Write-Host $b
            }
            return
        }
 
        if ($branches.Count -eq 0) {
            Write-Host "No branches found." -ForegroundColor Yellow
            return
        }
 
        $selected = [AgyTui.SpectreMenu]::Show("Select Git Branch to Checkout", $branches, 0)
        if ($selected -ge 0) {
            $bName = $branches[$selected]
            Write-Host "Checking out branch: $bName" -ForegroundColor Green
            git checkout $bName
        } else {
            Write-Host "Cancelled." -ForegroundColor DarkGray
        }
    }
 
    static [string] GenerateAiCommitMessage() {
        if (-not (Test-AgyAiGate)) { return "" }
        $diff = git diff --cached
        if (-not $diff) { return "" }
 
        $prompt = "Generate a concise, one-line conventional commit description (excluding type/scope prefix) based on the following git diff. Output ONLY the description:`n`n$diff"
 
        $body = @{
            model = [AgyTui.AgyAiCore]::OllamaDefaultModel
            prompt = $prompt
            stream = $false
        } | ConvertTo-Json
 
        try {
            $res = Invoke-RestMethod -Method Post -Uri "http://127.0.0.1:11434/api/generate" -Body $body -ContentType "application/json" -TimeoutSec 5
            if ($res.response) {
                $desc = $res.response.Trim()
                $desc = $desc -replace '^(feat|fix|docs|style|refactor|test|chore)(\(.*?\))?:\s*', ''
                return $desc
            }
        } catch {
            Write-Warning "Failed to contact Ollama for AI commit suggestion: $_"
        }
        return ""
    }
 
    static [void] Gcmt([string]$DirectMessage) {
        $staged = git diff --cached --name-only
        if (-not $staged) {
            Write-Warning "No staged changes found. Run git add first."
            return
        }
 
        if ($Global:AiMode -and -not $DirectMessage) {
            Write-Host "Usage: gcmt <message>"
            Write-Host "Supported Commit Types: feat, fix, docs, style, refactor, test, chore"
            return
        }
 
        if ($DirectMessage) {
            Write-Host "Committing with message: '$DirectMessage'" -ForegroundColor Cyan
            git commit -m "$DirectMessage"
            return
        }
 
        $types = @(
            "feat     (New feature)",
            "fix      (Bug fix)",
            "docs     (Documentation changes)",
            "style    (Formatting, missing semi colons, etc)",
            "refactor (Code restructuring without behavior changes)",
            "test     (Adding missing tests)",
            "chore    (Maintenance/dependencies)"
        )
        $sel = [AgyTui.SpectreMenu]::Show("Select Commit Type", $types, 0)
        if ($sel -lt 0) { return }
 
        $type = switch ($sel) {
            0 { "feat" }
            1 { "fix" }
            2 { "docs" }
            3 { "style" }
            4 { "refactor" }
            5 { "test" }
            6 { "chore" }
        }
 
        $scope = Read-Host "Enter commit scope (optional, press Enter to skip)"
        $scope = $scope.Trim()
 
        $desc = Read-Host "Enter commit description (or type 'ai' to auto-generate)"
        if ($desc -eq "ai") {
            Write-Host "[Ollama] Querying Ollama for commit suggestion..." -ForegroundColor Yellow
            $desc = [GitHelper]::GenerateAiCommitMessage()
            if (-not $desc) {
                $desc = Read-Host "Ollama failed to generate message. Please enter description manually"
            } else {
                Write-Host "[Ollama] Generated: $desc" -ForegroundColor Green
            }
        }
 
        if ([string]::IsNullOrWhiteSpace($desc)) {
            Write-Warning "Commit description cannot be empty."
            return
        }
 
        $finalMsg = if ($scope) { "${type}($scope): $desc" } else { "${type}: $desc" }
        Write-Host ""
        Write-Host "Generated commit message: '$finalMsg'" -ForegroundColor Cyan
        $confirm = Read-Host "Confirm commit? (Y/N)"
        if ($confirm -match "^[Yy]") {
            git commit -m "$finalMsg"
        } else {
            Write-Host "Commit cancelled." -ForegroundColor DarkGray
        }
    }
}
'@
} catch {
}
#endregion
 
 

#region DOTNET HELPER
# ==============================================================================
#  Shortcuts and migrations tool wrappers for the .NET SDK.
# ==============================================================================
 
class DotNetHelper {
    static [void] Run([string[]]$PassThruArgs) {
        Write-Host "🚀 Running project..." -ForegroundColor Green
        if ($PassThruArgs) { dotnet run $PassThruArgs | Out-Default } else { dotnet run | Out-Default }
    }
 
    static [void] CleanBinObj() {
        Write-Host "💥 Destroying bin/ and obj/ folders..." -ForegroundColor Red
        $dirs = Get-ChildItem -Path . -Depth 3 -Directory -Force -ErrorAction SilentlyContinue |
            Where-Object { ($_.Name -eq 'bin' -or $_.Name -eq 'obj') -and $_.FullName -notmatch '\\(node_modules|\.git|\.vs)\\' }
        if ($dirs) {
            foreach ($d in $dirs) {
                if (Test-Path $d.FullName) {
                    try {
                        Remove-Item -Path $d.FullName -Recurse -Force -ErrorAction SilentlyContinue
                    } catch {}
                }
            }
        }
        Write-Host "✅ Clean complete." -ForegroundColor Green
    }
 
    static [void] NewSolution([string]$Name) {
        dotnet new sln -n $Name | Out-Default
    }
 
    static [void] AddAllProjectsToSolution() {
        $projects = Get-ChildItem -Recurse -Filter "*.csproj"
        foreach ($p in $projects) {
            dotnet sln add $p.FullName | Out-Default
        }
    }
 
    static [void] WatchTest([string[]]$PassThruArgs) {
        Write-Host "👀 Watching Tests..." -ForegroundColor Yellow
        if ($PassThruArgs) { dotnet watch test $PassThruArgs | Out-Default } else { dotnet watch test | Out-Default }
    }
 
    static [void] Watch([string[]]$PassThruArgs) {
        Write-Host "👀 Watching for changes..." -ForegroundColor Cyan
        if ($PassThruArgs) { dotnet watch $PassThruArgs | Out-Default } else { dotnet watch | Out-Default }
    }
 
    static [void] Build([string[]]$PassThruArgs) {
        Write-Host "🔨 Building project..." -ForegroundColor Blue
        if ($PassThruArgs) { dotnet build $PassThruArgs | Out-Default } else { dotnet build | Out-Default }
    }
 
    static [void] Format([string[]]$PassThruArgs) {
        Write-Host "💅 Formatting code..." -ForegroundColor Magenta
        if ($PassThruArgs) { dotnet format $PassThruArgs | Out-Default } else { dotnet format | Out-Default }
    }
 
    static [void] Test([string[]]$PassThruArgs) {
        Write-Host "🧪 Running tests..." -ForegroundColor Yellow
        if ($PassThruArgs) { dotnet test $PassThruArgs | Out-Default } else { dotnet test | Out-Default }
    }
 
    static [void] Clean([string[]]$PassThruArgs) {
        Write-Host "🧹 Cleaning project..." -ForegroundColor Yellow
        if ($PassThruArgs) { dotnet clean $PassThruArgs | Out-Default } else { dotnet clean | Out-Default }
    }
 
    static [void] Restore([string[]]$PassThruArgs) {
        Write-Host "📦 Restoring packages..." -ForegroundColor Magenta
        if ($PassThruArgs) { dotnet restore $PassThruArgs | Out-Default } else { dotnet restore | Out-Default }
    }
 
    static [void] UpdateDatabase([string]$Context) {
        Write-Host "📈 Updating database..." -ForegroundColor Green
        $params = @("ef", "database", "update")
        if ($Context) { $params += @("--context", $Context) }
        dotnet $params | Out-Default
    }
 
    static [void] AddMigration([string]$MigrationName, [string]$Context) {
        Write-Host "➕ Adding migration: $MigrationName" -ForegroundColor Cyan
        $params = @("ef", "migrations", "add", $MigrationName)
        if ($Context) { $params += @("--context", $Context) }
        dotnet $params | Out-Default
    }
 
    static [void] RemoveDatabase([string]$Context) {
        Write-Host "🔥 Dropping database..." -ForegroundColor Red
        if ((Read-Host "Are you sure? (y/N)") -eq "y") {
            $params = @("ef", "database", "drop", "--force")
            if ($Context) { $params += @("--context", $Context) }
            dotnet $params | Out-Default
            Write-Host "Database dropped."
        } else {
            Write-Host "Cancelled."
        }
    }
 
    static [void] RemoveMigration([string]$Context) {
        Write-Host "⏪ Removing last migration..." -ForegroundColor Yellow
        $params = @("ef", "migrations", "remove")
        if ($Context) { $params += @("--context", $Context) }
        dotnet $params | Out-Default
    }
 
    static [void] NewConsole([string]$Name) {
        dotnet new console -n $Name | Out-Default
    }
 
    static [void] NewWebApi([string]$Name) {
        dotnet new webapi -n $Name | Out-Default
    }
}
#endregion
 


 #region DOCKER HELPER
# ==============================================================================
# Shortcuts and prune utility wrappers for Docker and Docker Compose.
#
# Embedded via Invoke-Expression for the same reason as GitHelper above: Dkcl()
# references [AgyTui.*] types inside the class body.
# ==============================================================================
try {
    Load-AgyTuiDll
    Invoke-Expression @'
class DockerHelper {
 static [void] GetContainers([bool]$All) {
 Write-Host "[Docker] Containers:" -ForegroundColor Blue
 if ($All) { docker container ls -a | Out-Default } else { docker container ls | Out-Default }
 }

 static [void] RemoveAllContainers() {
 Write-Host "[Prune] Removing ALL containers..." -ForegroundColor Red
 if ((Read-Host "This will remove ALL containers. Are you sure? (y/N)") -eq 'y') {
 $c = docker ps -aq
 if ($c) { docker rm $c | Out-Default }
 Write-Host "All containers removed."
 } else {
 Write-Host "Cancelled."
 }
 }

 static [void] StopAllContainers() {
 Write-Host "[Stop] Stopping ALL running containers..." -ForegroundColor Yellow
 $c = docker ps -q
 if ($c) { docker stop $c | Out-Default }
 Write-Host "All containers stopped."
 }

 static [void] ComposeUp([string[]]$PassThruArgs) {
 Write-Host "[Compose] Starting Docker Compose..." -ForegroundColor Green
 if ($PassThruArgs) { docker-compose up $PassThruArgs | Out-Default } else { docker-compose up | Out-Default }
 }

 static [void] ComposeUpBuild([string[]]$PassThruArgs) {
 Write-Host "[Compose] Building and starting Docker Compose... " -ForegroundColor Blue
 if ($PassThruArgs) { docker-compose up --build $PassThruArgs | Out-Default } else { docker-compose up --build | Out-Default }
 }

 static [void] ComposeDown([string[]]$PassThruArgs) {
 Write-Host "[Compose] Stopping Docker Compose..." -ForegroundColor Yellow
 if ($PassThruArgs) { docker-compose down $PassThruArgs | Out-Default } else { docker-compose down | Out-Default }
 }

 static [void] RemoveUnusedVolumes() {
 Write-Host "[Prune] Pruning Docker volumes..." -ForegroundColor Magenta
 docker volume prune | Out-Default
 }

 static [void] RemoveUnusedImages() {
 Write-Host "[Prune] Pruning Docker images..." -ForegroundColor Magenta
 docker image prune | Out-Default
 }

 static [void] Dkcl() {
 $containers = @()
 try {
 $open = "{" + "{"
 $close = "}" + "}"
 $fmt = "$open.Names$close:::$open.State$close:::$open.Image$close:::$open.Label 'com.docker.compose.project'$close"
 $raw = docker ps -a --format $fmt 2>$null
 if ($null -ne $raw) {
 foreach ($line in $raw) {
 if ([string]::IsNullOrWhiteSpace($line)) { continue }
 $parts = $line -split ':::'
 $proj = "(Standalone)"
 if ($parts[3]) { $proj = $parts[3] }
 $containers += [PSCustomObject]@{
 Name = $parts[0]
 State = $parts[1]
 Image = $parts[2]
 Project = $proj
 }
 }
 }
 } catch {}

 if ($Global:AiMode) {
 foreach ($c in $containers) {
 Write-Host "$($c.Name),$($c.State),$($c.Image)"
 }
 return
 }

 if ($containers.Count -eq 0) {
 Write-Host "No Docker containers found." -ForegroundColor Yellow
 return
 }

 while ($true) {
 $containers = [AgyTui.SpectreProgress]::SpinnerResult("[Docker] Querying active container configurations...", {
 $cList = @()
 try {
 $open = "{" + "{"
 $close = "}" + "}"
 $fmt = "$open.Names$close:::$open.State$close:::$open.Image$close:::$open.Label 'com.docker.compose.project'$close"
 $raw = docker ps -a --format $fmt 2>$null
 if ($null -ne $raw) {
 foreach ($line in $raw) {
 if ([string]::IsNullOrWhiteSpace($line)) { continue }
 $parts = $line -split ':::'
 $proj = "(Standalone)"
 if ($parts[3]) { $proj = $parts[3] }
 $cList += [PSCustomObject]@{
 Name = $parts[0]
 State = $parts[1]
 Image = $parts[2]
 Project = $proj
 }
 }
 }
 } catch {}
 return $cList
 })

 $grouped = $containers | Group-Object Project
 $menuItems = @()
 $itemMapping = @()
 foreach ($group in $grouped) {
 $menuItems += "[$($group.Name)]"
 $itemMapping += $null
 foreach ($c in $group.Group) {
 $statusIcon = "[-]"
 if ($c.State -eq "running") { $statusIcon = "[+]" }
 $menuItems += " $statusIcon $($c.Name) ($($c.State)) - $($c.Image)"
 $itemMapping += $c
 }
 }
 $menuItems += "[x] Exit Dashboard"
 $itemMapping += $null

 $selected = [AgyTui.SpectreMenu]::Show("Docker Containers Dashboard (dkcl)", $menuItems, 0)
 if ($selected -lt 0 -or $selected -eq ($menuItems.Count - 1)) {
 break
 }

 $c = $itemMapping[$selected]
 if ($null -eq $c) {
 continue
 }

 $subItems = @(
 "[Start] Start Container",
 "[Stop] Stop Container",
 "[Restart] Restart Container",
 "[Logs] View Logs (tail 50)",
 "[Back] Return"
 )
 $subSel = [AgyTui.SpectreMenu]::Show("Manage Container: $($c.Name)", $subItems, 0)
 if ($subSel -eq 0) {
 Write-Host "Starting $($c.Name)..." -ForegroundColor Green
 docker start $c.Name | Out-Null
 }
 elseif ($subSel -eq 1) {
 Write-Host "Stopping $($c.Name)..." -ForegroundColor Yellow
 docker stop $c.Name | Out-Null
 }
 elseif ($subSel -eq 2) {
 Write-Host "Restarting $($c.Name)..." -ForegroundColor Cyan
 docker restart $c.Name | Out-Null
 }
 elseif ($subSel -eq 3) {
 Write-Host "Fetching logs for $($c.Name)..." -ForegroundColor Blue
 $logs = docker logs --tail 50 $c.Name 2>&1
 [AgyTui.SpectrePager]::Show("Logs: $($c.Name)", $logs)
 }
 }
 }
}
'@
} catch {
}
#endregion

#region AWS HELPER
# ==============================================================================
# AWS LocalStack commands and S3/SQS utility wrappers.
# ==============================================================================

class AwsHelper {
 static [string]$LocalStackUrl = "http://localhost:4566"

 static [void] GetS3Buckets() {
 awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) s3 ls | Out-Default
 }

 static [void] NewS3Bucket([string]$Name) {
 awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) s3 mb s3://$Name | Out-Default
 }

 static [void] GetLambdaFunctions() {
 awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) lambda list-functions | Out-Default
 }

 static [void] GetLocalSQSQueues() {
 awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs list-queues | Out-Default
 }

 static [void] NewLocalSQSQueue([string]$QueueName) {
 awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs create-queue --queue-name=$QueueName | Out-Default
 }

 static [void] ClearLocalSQSQueue([string]$QueueUrl) {
 awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs purge-queue --queue-url $QueueUrl | Out-Default
 }

 static [void] SendLocalSQSMessage([string]$QueueUrl, [string]$MessageBody, [string]$GroupId) {
 $gid = if ($GroupId) { $GroupId } else { "default-group" }
 awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs send-message --queue-url $QueueUrl --message-body $MessageBody --message-group-id $gid | Out-Default
 }

 static [void] GetLocalSQSMessage([string]$QueueUrl) {
 awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs receive-message --queue-url $QueueUrl | Out-Default
 }

 static [void] GetLocalSQSAttributes([string]$QueueUrl) {
 awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs get-queue-attributes --queue-url $QueueUrl --attribute-names All | Out-Default
 }
}
#endregion

#region SYSTEM HELPER
# ==============================================================================
# PS1-only system utility: clearing shell history.
#
# Everything else that used to live here (disk space, public IP, kill-port,
# process picker, system monitor) now lives in AgyTui.SystemHelper (Program.cs).
# ClearHistory stays PS1-only: it reaches into PSReadLine's own static class
# and the engine-native Clear-History cmdlet, both of which are PowerShell
# session/engine internals with no real portability win from moving to C#.
# ==============================================================================

class SystemHelper {
    static [object[]] GetDiskSpace() {
        return @(Get-PSDrive -PSProvider FileSystem | Select-Object Name,
            @{N='Used(GB)';E={"{0:N2}" -f ($_.Used/1GB)}},
            @{N='Free(GB)';E={"{0:N2}" -f ($_.Free/1GB)}},
            @{N='Total(GB)';E={"{0:N2}" -f (($_.Used + $_.Free)/1GB)}})
    }

    static [void] StopProcessFriendly([string]$Name) {
        if ($Name) {
            Stop-Process -Name $Name -Force
        } else {
            Get-Process | Out-GridView -Title "Select Process to Kill" -PassThru | Stop-Process -Force
        }
    }

    static [void] OpenExplorer() {
        Invoke-Item .
    }

    static [string] GetPublicIP() {
        try {
            $webClient = [System.Net.WebClient]::new()
            return $webClient.DownloadString("https://api.ipify.org").Trim()
        } catch {
            return "Unable to resolve public IP."
        }
    }

    static [void] ClearHistory() {
        Clear-Host
        Remove-Item (Get-PSReadlineOption).HistorySavePath -ErrorAction SilentlyContinue
        $prType = [Type]"Microsoft.PowerShell.PSConsoleReadLine"
        if ($prType) { $prType::ClearHistory() }
        Clear-History
        Write-Host "🧹 All command history has been cleared." -ForegroundColor Yellow
    }

    static [void] KillPort([int]$Port) {
        $connections = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
        if (-not $connections) {
            Write-Host "No process found listening on port $Port." -ForegroundColor Yellow
            return
        }
        foreach ($conn in $connections) {
            $owningPid = $conn.OwningProcess
            try {
                $proc = Get-Process -Id $owningPid -ErrorAction SilentlyContinue
                if ($proc) {
                    Write-Host "Killing process '$($proc.Name)' (PID $owningPid) listening on port $Port..." -ForegroundColor Red
                    Stop-Process -Id $owningPid -Force
                }
            } catch {
                Write-Host "Failed to kill process PID $($owningPid): $_" -ForegroundColor Red
            }
        }
    }

    static [void] SystemMonitor() {
        # Check network & metrics first
        $cpu = 0.0
        try {
            $cpuSample = Get-Counter '\Processor(_Total)\% Processor Time' -ErrorAction SilentlyContinue
            if ($cpuSample) { $cpu = [Math]::Round($cpuSample.CounterSamples[0].CookedValue, 1) }
        } catch {}

        $os = Get-CimInstance Win32_OperatingSystem -ErrorAction SilentlyContinue
        $totalRam = 1.0
        $freeRam = 1.0
        if ($os) {
            $totalRam = $os.TotalVisibleMemorySize
            $freeRam = $os.FreePhysicalMemory
        }
        $ramPercent = [Math]::Round((($totalRam - $freeRam) / $totalRam) * 100, 1)

        $diskPercent = 0.0
        try {
            $diskSample = Get-Counter '\PhysicalDisk(_Total)\% Disk Time' -ErrorAction SilentlyContinue
            if ($diskSample) { $diskPercent = [Math]::Round([Math]::Min(100.0, $diskSample.CounterSamples[0].CookedValue), 1) }
        } catch {}

        if ($Global:AiMode) {
            Write-Host "$cpu%,$ramPercent%,$diskPercent%"
            return
        }

        Write-Host "Press Escape to exit System Monitor..." -ForegroundColor Gray
        $charFilled = [char]0x2588
        $charEmpty = [char]0x2591
        
        $startRow = [Console]::CursorTop
        $startCol = 0
        
        try {
            while ($true) {
                # Get metrics
                $cpu = 0.0
                try {
                    $cpuSample = Get-Counter '\Processor(_Total)\% Processor Time' -ErrorAction SilentlyContinue
                    if ($cpuSample) { $cpu = [Math]::Round($cpuSample.CounterSamples[0].CounterSamples[0].CookedValue, 1) }
                    if (-not $cpu -and $cpuSample) { $cpu = [Math]::Round($cpuSample.CounterSamples[0].CookedValue, 1) }
                } catch {}

                $os = Get-CimInstance Win32_OperatingSystem -ErrorAction SilentlyContinue
                if ($os) {
                    $totalRam = $os.TotalVisibleMemorySize
                    $freeRam = $os.FreePhysicalMemory
                }
                $ramPercent = [Math]::Round((($totalRam - $freeRam) / $totalRam) * 100, 1)
                $usedRamGB = [Math]::Round(($totalRam - $freeRam) / 1024 / 1024, 2)
                $totalRamGB = [Math]::Round($totalRam / 1024 / 1024, 2)

                $diskPercent = 0.0
                try {
                    $diskSample = Get-Counter '\PhysicalDisk(_Total)\% Disk Time' -ErrorAction SilentlyContinue
                    if ($diskSample) { $diskPercent = [Math]::Round([Math]::Min(100.0, $diskSample.CounterSamples[0].CookedValue), 1) }
                } catch {}

                # Create Gauges (length 20)
                $cpuFilled = [int][Math]::Round(($cpu / 100.0) * 20)
                if ($cpuFilled -gt 20) { $cpuFilled = 20 }
                if ($cpuFilled -lt 0) { $cpuFilled = 0 }
                $cpuBar = ([string]$charFilled * $cpuFilled) + ([string]$charEmpty * (20 - $cpuFilled))

                $ramFilled = [int][Math]::Round(($ramPercent / 100.0) * 20)
                if ($ramFilled -gt 20) { $ramFilled = 20 }
                if ($ramFilled -lt 0) { $ramFilled = 0 }
                $ramBar = ([string]$charFilled * $ramFilled) + ([string]$charEmpty * (20 - $ramFilled))

                $diskFilled = [int][Math]::Round(($diskPercent / 100.0) * 20)
                if ($diskFilled -gt 20) { $diskFilled = 20 }
                if ($diskFilled -lt 0) { $diskFilled = 0 }
                $diskBar = ([string]$charFilled * $diskFilled) + ([string]$charEmpty * (20 - $diskFilled))

                try {
                    [Console]::SetCursorPosition($startCol, $startRow)
                } catch {
                    $startRow = [Console]::CursorTop
                    $startCol = 0
                }
                
                Write-Host "  CPU Usage: [$cpuBar] $cpu%                " -ForegroundColor Cyan
                Write-Host "  RAM Usage: [$ramBar] $ramPercent% ($usedRamGB GB / $totalRamGB GB)     " -ForegroundColor Green
                Write-Host "  Disk I/O:  [$diskBar] $diskPercent%               " -ForegroundColor Yellow

                # Check key available with 2s timeout
                $sleepCycles = 20
                $exit = $false
                for ($s = 0; $s -lt $sleepCycles; $s++) {
                    if ([Console]::KeyAvailable) {
                        $key = [Console]::ReadKey($true)
                        if ($key.Key -eq [ConsoleKey]::Escape -or $key.Key -eq [ConsoleKey]::Enter) {
                            $exit = $true
                            break
                        }
                    }
                    Start-Sleep -Milliseconds 100
                }
                if ($exit) { break }
            }
        } finally {
            try {
                [Console]::SetCursorPosition($startCol, $startRow)
                Write-Host (" " * ([Console]::WindowWidth - 1))
                Write-Host (" " * ([Console]::WindowWidth - 1))
                Write-Host (" " * ([Console]::WindowWidth - 1))
                [Console]::SetCursorPosition($startCol, $startRow)
            } catch {}
        }
    }
}
#endregion
#region PROFILE HELP
# ==============================================================================
#  Exposes interactive help documentation for all custom commands.
#
#  Embedded via Invoke-Expression for the same reason as GitHelper above: Show()
#  references [AgyTui.ProfileHelp] inside the class body.
# ==============================================================================
try {
    Load-AgyTuiDll
    Invoke-Expression @'
class ProfileHelp {
    # Category/command menu building, filtering, and the drill-down loop all live in
    # [AgyTui.ProfileHelp]::ShowInteractive() now. This just runs the returned command's alias —
    # the one part that has to stay PS1-side, since it executes an arbitrary alias in the live
    # session and C# can't do that.
    static [void] Show([string]$CategoryFilter) {
        $jsonPath = Join-Path $Global:ProfileRepoRoot "Profile\Core\CommandsMenu.json"
        while ($true) {
            $cmdObj = [AgyTui.ProfileHelp]::ShowInteractive($jsonPath, $CategoryFilter)
            $CategoryFilter = ""
            if (-not $cmdObj) { return }
 
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
    }
}
'@
} catch {
}
#endregion

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
function Set-LocationParent { Set-Location .. }
function Set-LocationGrandParent { Set-Location ..\.. }
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
 oh-my-posh --init --shell pwsh --config $ThemePath | Invoke-Expression
 [AgyAccountManager]::RegisterPromptHook()
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
function Get-GitStatus { [AgyTui.GitHelper]::ShowStatus() }
function Show-GitDiff { git diff $args }
function Get-GitLogGraph { git log --graph --oneline --decorate --all }
function Get-GitLogPretty { git log --pretty=format:"%h - %an, %ar : %s" }
function Get-GitLog { [AgyTui.GitHelper]::ShowLog() }
function Get-GitBranches { [AgyTui.GitHelper]::ShowBranches() }
function Invoke-GitCheckout {
 param([string]$branchName)
 [AgyTui.GitHelper]::Checkout($branchName)
}
function New-GitBranch {
 param([string]$branchName)
 git checkout -b $branchName
}
function Remove-GitBranch {
 param([string]$branchName)
 git branch -d $branchName
}
function Invoke-GitAddAll { [AgyTui.GitHelper]::AddAll() }
function Invoke-GitUnstage { git restore --staged . }
function Invoke-GitCommit {
 param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Message)
 if ($Message) { git commit -m ($Message -join " ") } else { [AgyTui.GitHelper]::ConventionalCommitWizard() }
}
function Invoke-GitAmend { git commit --amend $args }
function Invoke-GitUndo { [AgyTui.GitHelper]::InvokeGitUndo() }
function Invoke-GitResetSoft { git reset --soft HEAD~1 }
function Invoke-GitResetHard { git reset --hard }
function Invoke-GitFetch { [AgyTui.GitHelper]::Fetch() }
function Invoke-GitPull { [AgyTui.GitHelper]::Pull() }
function Invoke-GitPush { [AgyTui.GitHelper]::Push() }
function Invoke-GitPushForce { git push --force $args }
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
 if ($All) { docker ps -a } else { docker ps }
}
function Remove-AllDockerContainers { [DockerHelper]::RemoveAllContainers() }
function Stop-AllDockerContainers { [DockerHelper]::StopAllContainers() }
function Invoke-ComposeUp { [DockerHelper]::ComposeUp($args) }
function Invoke-ComposeUpBuild { [DockerHelper]::ComposeUpBuild($args) }
function Invoke-ComposeDown { [DockerHelper]::ComposeDown($args) }
function Remove-UnusedDockerVolumes { [DockerHelper]::RemoveUnusedVolumes() }
function Remove-UnusedDockerImages { [DockerHelper]::RemoveUnusedImages() }

# Docker Aliases
Set-Alias -Name dps -Value Get-DockerContainers -Force
Set-Alias -Name containers -Value Get-DockerContainers -Force
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
    $releaseDll = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "csapp\AgyTuiApp\dist\AgyTuiApp.dll"
    $debugDll = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "csapp\AgyTuiApp\bin\Debug\net10.0\AgyTuiApp.dll"
    if (Test-Path $releaseDll) {
        dotnet $releaseDll @args
    } elseif (Test-Path $debugDll) {
        dotnet $debugDll @args
    } else {
        $proj = Join-Path $Global:ProfileRepoRoot "csapp\AgyTuiApp\AgyTuiApp.csproj"
        dotnet run --project $proj -- $args
    }
    $selectedProjFile = Join-Path -Path $Global:AgySourceHome -ChildPath "selected_project.txt"
    if (Test-Path $selectedProjFile) {
        $projPath = Get-Content $selectedProjFile -Raw -ErrorAction SilentlyContinue
        if ($projPath -and (Test-Path $projPath.Trim())) {
            Write-Host "🛸 Navigating to selected workspace: $($projPath.Trim())" -ForegroundColor Cyan
            Set-Location $($projPath.Trim())
        }
        Remove-Item $selectedProjFile -Force -ErrorAction SilentlyContinue
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
function go { Invoke-ControlCenter "go" $args }
function open-term { [AgyTui.SystemHelper]::OpenNewTerminalSession() }
Set-Alias -Name term -Value open-term -Force
function dpack { [AgyTui.DotNetHelper]::Pack($args) }
function dpubpkg { [AgyTui.DotNetHelper]::PublishPackage($args) }
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
function Invoke-GitBranchCheckout { Invoke-GitCheckout $args }

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
Set-Alias -Name agyswitch -Value Toggle-AutoSwitch -Force

function Show-AccountsSummary { [AgyAccountManager]::ShowAllAccountsSummary() }
Set-Alias -Name acc-sum -Value Show-AccountsSummary -Force

# --- AI Session Dashboard Wrappers ---
function Show-AiDashboard { if (-not (Test-AgyAiGate)) { return }; [AgyTui.AgyAiCore]::ShowAiDashboard() }
Set-Alias -Name ai-dash -Value Show-AiDashboard -Force

# --- Master Learning Suite Router ---
function Invoke-MasterLearningSuite {
    param([string]$Topic)
    $root = if ($Global:ProfileRepoRoot) { $Global:ProfileRepoRoot } elseif ($global:AGY_WORKSPACE_ROOT) { $global:AGY_WORKSPACE_ROOT } else { Split-Path -Parent -Path $MyInvocation.MyCommand.Definition }
    $projPath = Join-Path $root "AgyTuiApp\AgyTuiApp.csproj"
    
    $resolvedTopic = $Topic
    if (-not $resolvedTopic) {
        $invName = $MyInvocation.InvocationName
        if ($invName -and $invName -ne 'Invoke-MasterLearningSuite') {
            $resolvedTopic = $invName
        }
    }
    
    $dllPath = Join-Path $root "AgyTuiApp\bin\Debug\net10.0\AgyTuiApp.dll"
    if (-not (Test-Path $dllPath)) {
        Write-Host "⚠️ AgyTuiApp binary not built. Building now..." -ForegroundColor Yellow
        dotnet build "$projPath" | Out-Null
    }
    
    if ($resolvedTopic) {
        switch ($resolvedTopic) {
            "obsidian" { dotnet run --project "$projPath" -- obsidian $args }
            "refresh"  { dotnet run --project "$projPath" -- refresh $args }
            "vault-open" { dotnet run --project "$projPath" -- vault $args }
            default { dotnet run --project "$projPath" -- learn $resolvedTopic $args }
        }
    } else {
        dotnet run --project "$projPath" -- learn
    }
}

Set-Alias -Name learn -Value Invoke-MasterLearningSuite -Force
Set-Alias -Name flashcard -Value Invoke-MasterLearningSuite -Force
Set-Alias -Name vocab -Value Invoke-MasterLearningSuite -Force
Set-Alias -Name kana -Value Invoke-MasterLearningSuite -Force
Set-Alias -Name kanji -Value Invoke-MasterLearningSuite -Force
Set-Alias -Name jlpt -Value Invoke-MasterLearningSuite -Force
Set-Alias -Name grammar -Value Invoke-MasterLearningSuite -Force
Set-Alias -Name algo -Value Invoke-MasterLearningSuite -Force
Set-Alias -Name complexity -Value Invoke-MasterLearningSuite -Force
Set-Alias -Name problems -Value Invoke-MasterLearningSuite -Force
Set-Alias -Name snippets -Value Invoke-MasterLearningSuite -Force
Set-Alias -Name sheets -Value Invoke-MasterLearningSuite -Force
Set-Alias -Name quiz -Value Invoke-MasterLearningSuite -Force
Set-Alias -Name interview -Value Invoke-MasterLearningSuite -Force
Set-Alias -Name star -Value Invoke-MasterLearningSuite -Force
Set-Alias -Name mock -Value Invoke-MasterLearningSuite -Force
Set-Alias -Name obsidian -Value Invoke-MasterLearningSuite -Force
Set-Alias -Name refresh -Value Invoke-MasterLearningSuite -Force
Set-Alias -Name vault-open -Value Invoke-MasterLearningSuite -Force

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
