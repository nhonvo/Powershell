<#
.SYNOPSIS
    Release Build Script Wrapper for AgyTuiApp
#>
param(
    [string]$Command = "",
    [switch]$SkipTests = $true
)

$script = Join-Path $PSScriptRoot "build-release.ps1"
if ($SkipTests) {
    & $script -Command $Command -SkipTests
} else {
    & $script -Command $Command
}
