# Antigravity Allow List Setup Script for Windows PowerShell
# Run as Administrator: irm https://raw.githubusercontent.com/TUAN130294/antigravityallowlist/main/setup.ps1 | iex

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Antigravity Terminal Allow List Setup" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Check if Antigravity is running
$antigravity = Get-Process -Name "Antigravity" -ErrorAction SilentlyContinue
if ($antigravity) {
    Write-Host "[WARNING] Antigravity is running!" -ForegroundColor Yellow
    Write-Host "Please close Antigravity before running this script." -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Press Enter to exit"
    exit 1
}

# Define paths
$targetDir = "$env:APPDATA\Antigravity\User"
$settingsFile = "$targetDir\settings.json"

# Create directory if not exists
if (-not (Test-Path $targetDir)) {
    Write-Host "[INFO] Creating directory: $targetDir" -ForegroundColor Gray
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
}

# Backup existing settings
if (Test-Path $settingsFile) {
    Write-Host "[INFO] Backing up existing settings..." -ForegroundColor Gray
    Copy-Item $settingsFile "$settingsFile.backup" -Force
}

# Download settings from GitHub
Write-Host "[INFO] Downloading settings from GitHub..." -ForegroundColor Gray
$settingsUrl = "https://raw.githubusercontent.com/TUAN130294/antigravityallowlist/main/settings.json"

try {
    Invoke-WebRequest -Uri $settingsUrl -OutFile $settingsFile -UseBasicParsing
    Write-Host ""
    Write-Host "============================================" -ForegroundColor Green
    Write-Host "  [OK] Setup completed successfully!" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "File saved to: $settingsFile" -ForegroundColor White
    Write-Host ""
    Write-Host "Commands in Allow List: Auto-run" -ForegroundColor Green
    Write-Host "Commands NOT in list:   Require confirmation" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Please restart Antigravity to apply changes." -ForegroundColor Cyan
} catch {
    Write-Host "[ERROR] Failed to download settings: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
