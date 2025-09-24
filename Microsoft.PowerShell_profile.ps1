# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  Enhanced PowerShell Profile - Main Loader
#  This profile is modular. It loads scripts from the 'Profile' subdirectory.
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

$PSScriptRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
$ProfileDir = Join-Path -Path $PSScriptRoot -ChildPath 'Profile'

# The order of sourcing matters. Core setup and aliases should come first.
$modules = @(
    "Core.ps1",
    "Aliases.ps1",
    "Navigation.ps1",
    "System.ps1",
    "DotNet.ps1",
    "Git.ps1",
    "Docker.ps1",
    "AWS.ps1",
    "Help.ps1"
)

foreach ($module in $modules) {
    $modulePath = Join-Path -Path $ProfileDir -ChildPath $module
    if (Test-Path $modulePath) {
        . $modulePath
    } else {
        Write-Warning "Profile module not found: $modulePath"
    }
}

Write-Host "✅ Enhanced PowerShell Profile loaded." -ForegroundColor Green