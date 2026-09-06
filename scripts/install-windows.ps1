# install-windows.ps1 — Automated Fresh Machine Setup for Windows
[CmdletBinding()]
param(
    [string]$TargetDir = "",
    [switch]$DevEnvironment = $false
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $TargetDir) { $TargetDir = $repoRoot }

Write-Host "🚀 Starting PowerShell Control Center Fresh Machine Setup..." -ForegroundColor Cyan

if ($DevEnvironment) {
    $env:ENVIRONMENT = "Development"
    [Environment]::SetEnvironmentVariable("ENVIRONMENT", "Development", "User")
    Write-Host "⚠️ Setting up DEVELOPMENT environment (agytui.dev.db)..." -ForegroundColor Yellow
} else {
    $env:ENVIRONMENT = "Production"
    [Environment]::SetEnvironmentVariable("ENVIRONMENT", "Production", "User")
}

# 1. Audit and install .NET 9 SDK via winget if missing
if (-not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
    Write-Host "📦 Installing .NET 9 SDK via winget..." -ForegroundColor Yellow
    try {
        winget install Microsoft.DotNet.SDK.9 --silent --accept-package-agreements --accept-source-agreements
        $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
    } catch {
        Write-Host "⚠️ Winget auto-install failed. Please install .NET 9 SDK manually: https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Red
    }
} else {
    Write-Host "✅ .NET SDK detected: $(dotnet --version)" -ForegroundColor Green
}

# 2. Provision ~/.gemini/ directories
$geminiHome = Join-Path $HOME ".gemini"
@($geminiHome, (Join-Path $geminiHome "logs"), (Join-Path $geminiHome "history"), (Join-Path $geminiHome "data")) | ForEach-Object {
    if (-not (Test-Path $_)) { New-Item -ItemType Directory -Path $_ -Force | Out-Null }
}

$env:GEMINI_HOME = $geminiHome
[Environment]::SetEnvironmentVariable("GEMINI_HOME", $geminiHome, "User")
Write-Host "✅ Configured GEMINI_HOME: $geminiHome" -ForegroundColor Green

# 3. Link $PROFILE to shell/windows/Microsoft.PowerShell_profile.ps1
$profilePath = $PROFILE.CurrentUserAllHosts
if (-not $profilePath) { $profilePath = $PROFILE }
$profileDir = Split-Path $profilePath
if (-not (Test-Path $profileDir)) { New-Item -ItemType Directory -Path $profileDir -Force | Out-Null }

$profileSource = [System.IO.Path]::GetFullPath((Join-Path $TargetDir "shell/windows/Microsoft.PowerShell_profile.ps1"))
if (-not (Test-Path $profileSource)) {
    $profileSource = [System.IO.Path]::GetFullPath((Join-Path $TargetDir "Microsoft.PowerShell_profile.ps1"))
}
$profileDotSource = ". '$profileSource'"

if (-not (Test-Path $profilePath) -or -not (Get-Content $profilePath -ErrorAction SilentlyContinue | Select-String -Pattern [regex]::Escape($profileSource))) {
    Add-Content -Path $profilePath -Value "`n# PowerShell Control Center Profile`n$profileDotSource"
    Write-Host "✅ Linked PowerShell profile to $profileSource" -ForegroundColor Green
} else {
    Write-Host "✅ PowerShell profile already linked to $profileSource" -ForegroundColor Green
}

Write-Host "✨ Setup complete! Open a new PowerShell terminal to start." -ForegroundColor Green
