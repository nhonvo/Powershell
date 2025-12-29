# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  Enhanced PowerShell Profile - Main Loader
#  This profile is modular. It dynamically loads all scripts from 'Profile'
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

$PSScriptRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
$ProfileDir = Join-Path -Path $PSScriptRoot -ChildPath 'Profile'

# Dynamically load all .ps1 files in the Profile directory, sorted by name
$modules = Get-ChildItem -Path $ProfileDir -Filter "*.ps1" | Sort-Object Name

foreach ($module in $modules) {
    . $module.FullName
}

Write-Host "✅ Enhanced PowerShell Profile loaded." -ForegroundColor Green