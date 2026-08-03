# Install-AgyEnvironment.ps1 — Automated Fresh Machine Onboarding
[CmdletBinding()]
param(
    [string]$TargetDir = "$HOME\Documents\Powershell",
    [switch]$DevEnvironment = $false
)

Write-Host "🚀 Starting PowerShell Control Center Fresh Machine Setup..." -ForegroundColor Cyan

if ($DevEnvironment) {
    $env:ENVIRONMENT = "Development"
    Write-Host "⚠️ Setting up DEVELOPMENT environment (agytui.dev.db)..." -ForegroundColor Yellow
} else {
    $env:ENVIRONMENT = "Production"
}

# 1. Audit and install .NET 9 SDK via winget if missing
if (-not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
    Write-Host "📦 Installing .NET 9 SDK via winget..." -ForegroundColor Yellow
    winget install Microsoft.DotNet.SDK.9 --silent --accept-package-agreements --accept-source-agreements
}

# 2. Provision ~/.gemini/ directories (logs/, history/, data/)
$geminiHome = Join-Path $HOME ".gemini"
@($geminiHome, (Join-Path $geminiHome "logs"), (Join-Path $geminiHome "history"), (Join-Path $geminiHome "data")) | ForEach-Object {
    if (-not (Test-Path $_)) { New-Item -ItemType Directory -Path $_ -Force | Out-Null }
}

# 3. Link $PROFILE to Microsoft.PowerShell_profile.ps1
$profilePath = $PROFILE.CurrentUserAllHosts
if (-not $profilePath) { $profilePath = $PROFILE }
$profileDir = Split-Path $profilePath
if (-not (Test-Path $profileDir)) { New-Item -ItemType Directory -Path $profileDir -Force | Out-Null }

$profileSource = Join-Path $TargetDir "Microsoft.PowerShell_profile.ps1"
$profileDotSource = ". '$profileSource'"

if (-not (Test-Path $profilePath) -or -not (Get-Content $profilePath -ErrorAction SilentlyContinue | Select-String -Pattern [regex]::Escape($profileSource))) {
    Add-Content -Path $profilePath -Value "`n# PowerShell Control Center Profile`n$profileDotSource"
    Write-Host "✅ Linked PowerShell profile to $profileSource" -ForegroundColor Green
}

# 4. Restore & Compile AgyTui Production Binary
Write-Host "🔨 Compiling & Publishing AgyTui Control Center production binary..." -ForegroundColor Cyan
Push-Location $TargetDir
try {
    & "$TargetDir\build-release.ps1"
} finally {
    Pop-Location
}

# 5. Initialize SQLite Database & Environment Variable
$env:GEMINI_HOME = $geminiHome
Write-Host "🎉 Onboarding complete! Run 'rterm' or restart PowerShell to launch AgyTui Control Center." -ForegroundColor Green
