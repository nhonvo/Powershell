if ($global:AgyProfileLoaded) { return }
$global:AgyProfileLoaded = $true

# ==============================================================================
#  Enhanced PowerShell Profile - Main Loader
# ==============================================================================

$ProfileDir = Join-Path -Path (Split-Path -Parent -Path $MyInvocation.MyCommand.Definition) -ChildPath 'Profile'
$files = Get-ChildItem -Path $ProfileDir -Filter "*.ps1"

# Load core dependencies (TerminalMenu) first, then the rest alphabetically
$first = $files | Where-Object { $_.Name -eq 'TerminalMenu.ps1' }
$rest = $files | Where-Object { $_.Name -ne 'TerminalMenu.ps1' } | Sort-Object Name

if ($first) { . $first.FullName }
foreach ($file in $rest) {
    . $file.FullName
}

Write-Host "🛸 Enhanced PowerShell Profile loaded." -ForegroundColor Green
