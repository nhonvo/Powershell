<#
.SYNOPSIS
    Legacy Release Publish Wrapper (Delegates to root build-release.ps1)
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$OutputDir = "csapp/AgyTui/dist",
    [switch]$SkipTests = $false
)

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
& "$repoRoot\build-release.ps1" -OutputDir $OutputDir -Version $Version -SkipTests:$SkipTests
