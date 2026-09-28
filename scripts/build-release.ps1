# build-release.ps1 — Master Production Release Publish Script
[CmdletBinding()]
param(
    [string]$OutputDir = "dist/windows",
    [string]$Version,
    [string]$Command,
    [switch]$SkipTests = $false
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $repoRoot "scripts\build-apps.ps1"))) {
    $repoRoot = $PSScriptRoot
}

Write-Host "🚀 Publishing Antigravity Go Suite [Production Release]..." -ForegroundColor Cyan
if (![string]::IsNullOrEmpty($Version)) {
    Write-Host "📌 Release Version: $Version" -ForegroundColor Yellow
}

Push-Location $repoRoot
try {
    if (-not $SkipTests) {
        Write-Host "🧪 Validating PowerShell Profile & Test Suite..." -ForegroundColor Cyan
        $runTests = Join-Path $repoRoot "shell\tests\run_tests.ps1"
        if (Test-Path $runTests) {
            pwsh -NoProfile -ExecutionPolicy Bypass -File $runTests
            if ($LASTEXITCODE -ne 0) {
                Write-Error "❌ PowerShell profile test validation failed. Aborting release publish."
                return
            }
        }
    }

    $buildScript = Join-Path $repoRoot "scripts\build-apps.ps1"
    if (Test-Path $buildScript) {
        & $buildScript -Target "all" -Windows -OutputDir $OutputDir
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Release Publish Succeeded! Binaries built to $OutputDir" -ForegroundColor Green
        } else {
            Write-Error "❌ Release Publish Failed."
        }
    }
} finally {
    Pop-Location
}
