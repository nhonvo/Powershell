# build-release.ps1 — Master Production Release Publish Script
[CmdletBinding()]
param(
    [string]$OutputDir = "csapp/AgyTui/dist",
    [string]$Version,
    [switch]$SkipTests = $false
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot

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
if (![string]::IsNullOrEmpty($Version)) {
    Write-Host "📌 Release Version: $Version" -ForegroundColor Yellow
}

pushd $repoRoot
try {
    Unlock-Binaries -Dir "csapp\AgyTui\bin"
    Unlock-Binaries -Dir "csapp\AgyTui\dist"

    if (-not $SkipTests) {
        Write-Host "🧪 Executing test suite validation..." -ForegroundColor Cyan
        dotnet test csapp/AgyTui.Tests/AgyTui.Tests.csproj -c Release --verbosity quiet
        if ($LASTEXITCODE -ne 0) {
            Write-Error "❌ Tests failed. Aborting release publish."
            return
        }
    }

    Unlock-Binaries -Dir "csapp\AgyTui\dist"

    dotnet publish csapp/AgyTui/AgyTui.csproj `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:EnableCompressionInSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $OutputDir

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Release Publish Succeeded! Single-file binary located at $OutputDir\AgyTui.exe" -ForegroundColor Green
    } else {
        Write-Error "❌ Release Publish Failed."
    }
} finally {
    Get-ChildItem -Path "csapp\AgyTui" -Filter "*.old_*" -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    popd
}
