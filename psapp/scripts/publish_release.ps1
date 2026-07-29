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

function Unlock-Binaries {
    param([string]$Dir)
    if (Test-Path $Dir) {
        Get-ChildItem -Path $Dir -Include "*.dll", "*.exe", "*.pdb" -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
            $rand = Get-Random
            Rename-Item -Path $_.FullName -NewName "$($_.Name).old_$rand" -Force -ErrorAction SilentlyContinue
        }
    }
}

Write-Host "🚀 Publishing AgyTui [Production Release]..." -ForegroundColor Cyan
pushd $repoRoot
try {
    if (![string]::IsNullOrEmpty($Version)) {
        Write-Host "📌 Release Version: $Version" -ForegroundColor Yellow
    }
    
    # Unlock any binaries currently locked in memory by active PowerShell sessions
    Unlock-Binaries -Dir "csapp\AgyTui\bin"
    Unlock-Binaries -Dir "csapp\AgyTui\dist"

    # Run test suite before publishing
    Write-Host "🧪 Executing test suite validation..." -ForegroundColor Cyan
    dotnet test csapp/AgyTui.Tests/AgyTui.Tests.csproj -c Release --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ Tests failed. Aborting release publish."
        return
    }

    # Ensure dist is unlocked prior to final publish step
    Unlock-Binaries -Dir "csapp\AgyTui\dist"

    dotnet publish csapp/AgyTui/AgyTui.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o csapp/AgyTui/dist
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Release Publish Succeeded! Single-file binary located at csapp/AgyTui/dist/AgyTui.exe" -ForegroundColor Green
    } else {
        Write-Error "❌ Release Publish Failed."
    }
} finally {
    Get-ChildItem -Path "csapp\AgyTui" -Filter "*.old_*" -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    popd
}
