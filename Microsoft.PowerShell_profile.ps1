if ($global:AgyProfileLoaded) { return }
$global:AgyProfileLoaded = $true

# Core context detection
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

if ($menuFile) { $orderedFiles.Add($menuFile) }
if ($envFile)  { $orderedFiles.Add($envFile) }
if ($projFile) { $orderedFiles.Add($projFile) }

foreach ($f in ($files | Sort-Object Name)) {
    if ($f.Name -notin @('TerminalMenu.ps1', 'ProfileEnvironment.ps1', 'Projects.ps1')) {
        $orderedFiles.Add($f)
    }
}

foreach ($file in $orderedFiles) {
    . $file.FullName
}

if (-not $Global:AiMode) {
    Write-Host "🛸 Enhanced PowerShell Profile loaded." -ForegroundColor Green
}
