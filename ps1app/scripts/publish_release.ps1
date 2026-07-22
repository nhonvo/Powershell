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
    dotnet test app/AgyTuiApp.Tests/AgyTuiApp.Tests.csproj --no-restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ Tests failed. Aborting release publish."
        return
    }

    # Unlock dist exe if running
    $distExe = "app\AgyTuiApp\dist\AgyTuiApp.exe"
    if (Test-Path $distExe) {
        $rand = Get-Random
        Rename-Item -Path $distExe -NewName "AgyTuiApp.exe.old_$rand" -Force -ErrorAction SilentlyContinue
    }

    dotnet publish app/AgyTuiApp/AgyTuiApp.csproj -c Release -r win-x64 --self-contained -o app/AgyTuiApp/dist
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Release Publish Succeeded! Single-file binary located at app/AgyTuiApp/dist/AgyTuiApp.exe" -ForegroundColor Green
    } else {
        Write-Error "❌ Release Publish Failed."
    }
} finally {
    popd
}
