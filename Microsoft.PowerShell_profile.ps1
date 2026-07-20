# ==============================================================================
#  🛸 Enhanced PowerShell Profile Entry Point v3.0 🛸
# ==============================================================================

if ($global:AgyUserProfileLoaded) { return }
$global:AgyUserProfileLoaded = $true
$Global:ProfileRepoRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition

# Dot-source all modular domain scripts in Profile\ in order (10-Core, 20-Dev, 30-Sys, 40-Learn, 50-Aliases)
Get-ChildItem -Path (Join-Path $Global:ProfileRepoRoot "Profile\*.ps1") | Sort-Object Name | ForEach-Object {
    try {
        . $_.FullName
    } catch {
        Write-Warning "[!] Error loading profile module '$($_.Name)': $_"
    }
}
