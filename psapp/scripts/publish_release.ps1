<#
.SYNOPSIS
    Release Publish Script for AgyTuiApp (Release single-file binary to dist/)
#>
[CmdletBinding()]
param(
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Write-Host "🚀 Publishing AgyTuiApp [Production Release]..." -ForegroundColor Cyan
pushd $repoRoot
try {
    if (![string]::IsNullOrEmpty($Version)) {
        Write-Host "📌 Release Version: $Version" -ForegroundColor Yellow
    }
    
    # Run test suite before publishing
    Write-Host "🧪 Executing test suite validation..." -ForegroundColor Cyan
    dotnet test csapp/AgyTuiApp.Tests/AgyTuiApp.Tests.csproj --no-restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ Tests failed. Aborting release publish."
        return
    }

    # Unlock dist exe if running
    $distExe = "csapp\AgyTuiApp\dist\AgyTuiApp.exe"
    if (Test-Path $distExe) {
        $rand = Get-Random
        Rename-Item -Path $distExe -NewName "AgyTuiApp.exe.old_$rand" -Force -ErrorAction SilentlyContinue
    }

    dotnet publish csapp/AgyTuiApp/AgyTuiApp.csproj -c Release -o csapp/AgyTuiApp/dist
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Release Publish Succeeded! Single-file binary located at csapp/AgyTuiApp/dist/AgyTuiApp.exe" -ForegroundColor Green
    } else {
        Write-Error "❌ Release Publish Failed."
    }
} finally {
    Get-ChildItem -Path "csapp\AgyTuiApp\dist" -Filter "AgyTuiApp.*.old_*" -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    popd
}
