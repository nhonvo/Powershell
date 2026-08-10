# Install-AgyEnvironment.ps1 — Automated Fresh Machine Onboarding
[CmdletBinding()]
param(
    [string]$TargetDir = "$HOME\Documents\Powershell",
    [switch]$DevEnvironment = $false
)

$ErrorActionPreference = "Stop"

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
        # Refresh env path in current session
        $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
    } catch {
        Write-Host "⚠️ Winget auto-install failed. Please ensure .NET 9 SDK is installed manually: https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Red
    }
} else {
    Write-Host "✅ .NET SDK detected: $(dotnet --version)" -ForegroundColor Green
}

# 2. Provision ~/.gemini/ directories (logs/, history/, data/)
$geminiHome = Join-Path $HOME ".gemini"
@($geminiHome, (Join-Path $geminiHome "logs"), (Join-Path $geminiHome "history"), (Join-Path $geminiHome "data")) | ForEach-Object {
    if (-not (Test-Path $_)) { New-Item -ItemType Directory -Path $_ -Force | Out-Null }
}

# Persist GEMINI_HOME globally in User Environment Variables
$env:GEMINI_HOME = $geminiHome
[Environment]::SetEnvironmentVariable("GEMINI_HOME", $geminiHome, "User")
Write-Host "✅ Configured GEMINI_HOME environment variable: $geminiHome" -ForegroundColor Green

# 3. Link $PROFILE to Microsoft.PowerShell_profile.ps1
$profilePath = $PROFILE.CurrentUserAllHosts
if (-not $profilePath) { $profilePath = $PROFILE }
$profileDir = Split-Path $profilePath
if (-not (Test-Path $profileDir)) { New-Item -ItemType Directory -Path $profileDir -Force | Out-Null }

$profileSource = [System.IO.Path]::GetFullPath((Join-Path $TargetDir "Microsoft.PowerShell_profile.ps1"))
$profileDotSource = ". '$profileSource'"

if (-not (Test-Path $profilePath) -or -not (Get-Content $profilePath -ErrorAction SilentlyContinue | Select-String -Pattern [regex]::Escape($profileSource))) {
    Add-Content -Path $profilePath -Value "`n# PowerShell Control Center Profile`n$profileDotSource"
    Write-Host "✅ Linked PowerShell profile to $profileSource" -ForegroundColor Green
} else {
    Write-Host "✅ PowerShell profile already linked to $profileSource" -ForegroundColor Green
}

# 4. Restore & Compile AgyTui Production Binary
Write-Host "🔨 Compiling & Publishing AgyTui Control Center production binary..." -ForegroundColor Cyan
Push-Location $TargetDir
try {
    $buildScript = Join-Path $TargetDir "build-release.ps1"
    if (Test-Path $buildScript) {
        & $buildScript
    } else {
        Write-Host "⚠️ build-release.ps1 not found in $TargetDir" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Failed to compile AgyTui binary: $_" -ForegroundColor Red
} finally {
    Pop-Location
}

# 5. Onboarding Complete
Write-Host "🎉 Onboarding complete! Run 'rterm' or restart PowerShell to launch AgyTui Control Center." -ForegroundColor Green

