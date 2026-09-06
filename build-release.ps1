# ==============================================================================
#  BUILD-RELEASE — ROOT FORWARDER
#  Redirects execution to scripts/build-release.ps1
# ==============================================================================
[CmdletBinding()]
param(
    [string]$OutputDir = "",
    [string]$Version,
    [string]$Command,
    [switch]$SkipTests = $false
)
$target = Join-Path $PSScriptRoot "scripts/build-release.ps1"
if (Test-Path $target) {
    & $target @PSBoundParameters
} else {
    Write-Error "Could not locate build script at: $target"
}
