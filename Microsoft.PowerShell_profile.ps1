if ($global:AgyUserProfileLoaded) { return }
$global:AgyUserProfileLoaded = $true
$Global:ProfileRepoRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
$env:POSH_THEMES_PATH = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "asset\powershell-themes"

$Global:AiMode = $false
$Global:VerboseStartup = $false
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

# ==============================================================================
#  Enhanced PowerShell Profile - Main Loader
# ==============================================================================

$ProfileDir = Join-Path -Path (Split-Path -Parent -Path $MyInvocation.MyCommand.Definition) -ChildPath 'Profile'
$files = Get-ChildItem -Path $ProfileDir -Filter "*.ps1" -Recurse

# Core order: TerminalMenu.ps1 first, then ProfileEnvironment.ps1, then Projects.ps1, then the rest alphabetically
$orderedFiles = [System.Collections.Generic.List[System.IO.FileInfo]]::new()

$menuFile = $files | Where-Object { $_.Name -eq 'TerminalMenu.ps1' } | Select-Object -First 1
$envFile  = $files | Where-Object { $_.Name -eq 'ProfileEnvironment.ps1' } | Select-Object -First 1
$projFile = $files | Where-Object { $_.Name -eq 'Projects.ps1' } | Select-Object -First 1
$logHelperFile = $files | Where-Object { $_.Name -eq 'LogHelper.ps1' } | Select-Object -First 1

if ($menuFile) { $orderedFiles.Add($menuFile) }
if ($envFile)  { $orderedFiles.Add($envFile) }
if ($projFile) { $orderedFiles.Add($projFile) }
if ($logHelperFile) { $orderedFiles.Add($logHelperFile) }

foreach ($f in ($files | Sort-Object Name)) {
    if ($f.Name -notin @('TerminalMenu.ps1', 'ProfileEnvironment.ps1', 'Projects.ps1', 'LogHelper.ps1')) {
        $orderedFiles.Add($f)
    }
}

foreach ($file in $orderedFiles) {
    try {
        . $file.FullName
    } catch {
        if ([type]"LogHelper" -as [type]) {
            [LogHelper]::LogError("Failed to load script file: $($file.Name)", $_.Exception)
        } else {
            Write-Error "Failed to load script file: $($file.Name): $_"
        }
    }
}

try {
    [LogHelper]::Log("Enhanced PowerShell Profile loaded successfully. (AiMode = $Global:AiMode)")
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

#f45873b3-b655-43a6-b217-97c00aa0db58 PowerToys CommandNotFound module

Import-Module -Name Microsoft.WinGet.CommandNotFound
#f45873b3-b655-43a6-b217-97c00aa0db58
