<#
.SYNOPSIS
    Release Publish Script for AgyTui (Release single-file binary to dist/)
#>
[CmdletBinding()]
param(
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Write-Host "🚀 Publishing AgyTui [Production Release]..." -ForegroundColor Cyan
pushd $repoRoot
try {
    if (![string]::IsNullOrEmpty($Version)) {
        Write-Host "📌 Release Version: $Version" -ForegroundColor Yellow
    }
    
    # Run test suite before publishing
    Write-Host "🧪 Executing test suite validation..." -ForegroundColor Cyan
    dotnet test csapp/AgyTui.Tests/AgyTui.Tests.csproj --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ Tests failed. Aborting release publish."
        return
    }

    # Unlock dist exe if running
    $distExe = "csapp\AgyTui\dist\AgyTui.exe"
    if (Test-Path $distExe) {
        $rand = Get-Random
        Rename-Item -Path $distExe -NewName "AgyTui.exe.old_$rand" -Force -ErrorAction SilentlyContinue
    }

    dotnet publish csapp/AgyTui/AgyTui.csproj -c Release -o csapp/AgyTui/dist
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Release Publish Succeeded! Single-file binary located at csapp/AgyTui/dist/AgyTui.exe" -ForegroundColor Green
    } else {
        Write-Error "❌ Release Publish Failed."
    }
} finally {
    Get-ChildItem -Path "csapp\AgyTui\dist" -Filter "AgyTui.*.old_*" -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    popd
}
