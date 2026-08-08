<#
.SYNOPSIS
    Release Build & Direct Launch Script for AgyTuiApp
.EXAMPLE
    .\psapp\scripts\build_release.ps1 -Command "theme"
#>
param(
    [string]$Command = "theme"
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Write-Host "📦 Building & Publishing AgyTuiApp [Production Release Mode]..." -ForegroundColor Cyan
pushd $repoRoot
try {
    # Unlock DLL if loaded
    $dll = "csapp\AgyTui\bin\Release\net9.0\AgyTui.dll"
    if (Test-Path $dll) {
        $rand = Get-Random
        Rename-Item -Path $dll -NewName "AgyTui.dll.old_$rand" -Force -ErrorAction SilentlyContinue
    }

    dotnet publish csapp/AgyTui/AgyTui.csproj -c Release --nologo
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Release Build & Publish Succeeded." -ForegroundColor Green
        if (-not [string]::IsNullOrWhiteSpace($Command)) {
            Write-Host "🚀 Direct launch into command step: '$Command'..." -ForegroundColor Yellow
            $exe = "csapp\AgyTui\bin\Release\net9.0\publish\AgyTui.exe"
            if (-not (Test-Path $exe)) {
                $exe = "csapp\AgyTui\bin\Release\net9.0\AgyTui.exe"
            }
            if (Test-Path $exe) {
                & $exe $Command
            }
        }
    } else {
        Write-Error "❌ Release Build Failed."
    }
} finally {
    Get-ChildItem -Path "csapp\AgyTui\bin\Release\net9.0" -Filter "AgyTui.dll.old_*" -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    popd
}
