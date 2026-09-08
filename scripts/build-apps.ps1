# ==============================================================================
# build-apps.ps1 — Master Build & Installation Script for PowerShell
# ==============================================================================
[CmdletBinding()]
param(
    [Parameter(Position=0)]
    [string]$Target = "all",
    [switch]$SkipTests,
    [switch]$Windows,
    [string]$OutputDir
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$apps = @("agyswitch", "agyproj", "agygit", "agydocker", "agyterm", "agyx")

if (-not $OutputDir) {
    if ($IsWindows -or $env:OS -like "*Windows*") {
        $OutputDir = "$env:LOCALAPPDATA\Microsoft\WindowsApps"
    } else {
        $OutputDir = "$env:HOME/.local/bin"
    }
}

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

function Build-SingleApp {
    param([string]$AppName)
    $appDir = Join-Path $repoRoot "apps/$AppName"
    if (-not (Test-Path $appDir)) {
        Write-Error "App directory not found: $appDir"
        return
    }

    Write-Host "📦 [$AppName] Processing..." -ForegroundColor Cyan
    Push-Location $appDir
    try {
        if (-not $SkipTests) {
            go test ./...
            if ($LASTEXITCODE -ne 0) {
                Write-Error "❌ Tests failed for $AppName"
                return
            }
        }

        $exeSuffix = if ($IsWindows -or $Windows) { ".exe" } else { "" }
        $outPath = Join-Path $OutputDir "$AppName$exeSuffix"

        if ($Windows -and -not $IsWindows) {
            $env:GOOS = "windows"
            $env:GOARCH = "amd64"
        }

        go build -o $outPath .
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✔ Installed: $outPath" -ForegroundColor Green
        }
    }
    finally {
        Pop-Location
    }
}

switch ($Target.ToLower()) {
    "switch" { $Target = "agyswitch" }
    "proj"   { $Target = "agyproj" }
    "git"    { $Target = "agygit" }
    "docker" { $Target = "agydocker" }
    "term"   { $Target = "agyterm" }
    "proxy"  { $Target = "agyx" }
}

if ($Target -eq "all") {
    Write-Host "🚀 Building all 6 Antigravity Suite applications..." -ForegroundColor Cyan
    foreach ($app in $apps) {
        Build-SingleApp -AppName $app
    }
    Write-Host "🎉 Finished building all applications!" -ForegroundColor Green
} else {
    Build-SingleApp -AppName $Target
}
