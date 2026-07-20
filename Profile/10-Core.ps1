
  if ($global:AgyUserProfileLoaded) { return }
$global:AgyUserProfileLoaded = $true
$Global:ProfileRepoRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition

# --- Load configuration from profile.config.json ---
$configPath = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "profile.config.json"
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
        
        "_Note_ProjectsBaseDir" = "Base directory for development projects. Leave empty to use '~/Desktop/project' or '~/project'."
        "ProjectsBaseDir" = ""
        
        "_Note_AgySourceHome" = "Base directory for Antigravity settings and accounts. Leave empty for default: ~/.gemini or C:\Users\Public\.gemini"
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
        $config = Get-Content $configPath -Raw | ConvertFrom-Json
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
    $env:POSH_THEMES_PATH = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "asset\powershell-themes"
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
    $Global:AgySourceHome = if (Test-Path "C:\Users\Public\.gemini") { "C:\Users\Public\.gemini" } else { Join-Path $env:USERPROFILE ".gemini" }
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
$Global:AgyTuiAppProject = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "AgyTuiApp\AgyTuiApp.csproj"

# Load compiled C# AgyTuiApp assembly into PowerShell AppDomain if available
$debugDll = Join-Path -Path $Global:ProfileRepoRoot "AgyTuiApp\bin\Debug\net10.0\AgyTuiApp.dll"
if (Test-Path $debugDll) {
    try {
        Get-ChildItem -Path (Split-Path $debugDll) -Filter "*.dll" | Where-Object { $_.Name -ne "AgyTuiApp.dll" } | ForEach-Object {
            try { Add-Type -Path $_.FullName -ErrorAction SilentlyContinue } catch {}
        }
        Add-Type -Path $debugDll -ErrorAction SilentlyContinue
    } catch {}
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
        $env:THEME = [AgyTui.ThemeHelper]::ResolveStartupTheme($env:POSH_THEMES_PATH)
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

