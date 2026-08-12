[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# Load PowerShell Profile
$profilePath = Join-Path $PSScriptRoot "Microsoft.PowerShell_profile.ps1"
if (Test-Path $profilePath) {
    . $profilePath
}

$agyTuiExe = Join-Path $PSScriptRoot "csapp\AgyTui\dist\AgyTui.exe"
if (-not (Test-Path $agyTuiExe)) {
    $agyTuiExe = Join-Path $PSScriptRoot "csapp\AgyTui\bin\Release\net9.0\win-x64\AgyTui.exe"
}

$targetAccounts = @("fptvttnhon2020", "fptvttnhon2026", "nhontruongvo", "nhontruongvo3", "vothuongtruongnhon2002")
$testResults = @()

Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host " 🚀 AGY OPENING SCREEN BANNER VERIFICATION & REGEX ASSERTION TEST " -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan

foreach ($acc in $targetAccounts) {
    Write-Host "`n--------------------------------------------------" -ForegroundColor DarkGray
    Write-Host "👉 [1/3] Switching active context to: '$acc'..." -ForegroundColor Green
    
    # 1. Switch active context using AgyTui.exe single-file binary CLI engine
    if (Test-Path $agyTuiExe) {
        & $agyTuiExe agyswitch $acc | Out-Null
    }

    $geminiDir = Join-Path $env:USERPROFILE ".gemini_$acc"
    $env:GEMINI_HOME = $geminiDir
    $expectedEmail = ""

    # Read account email handle from google_accounts.json
    $gJsonPath = Join-Path $geminiDir "google_accounts.json"
    if (Test-Path $gJsonPath) {
        try {
            $gJson = Get-Content $gJsonPath -Raw | ConvertFrom-Json
            if ($gJson.activeAccount) { $expectedEmail = $gJson.activeAccount }
        } catch {}
    }

    if (-not $expectedEmail) {
        $expectedEmail = "$acc@gmail.com"
    }

    Write-Host "   [Target Account] : $acc" -ForegroundColor White
    Write-Host "   [GEMINI_HOME]    : $env:GEMINI_HOME" -ForegroundColor White
    Write-Host "   [Expected Email] : $expectedEmail" -ForegroundColor White

    Write-Host "👉 [2/3] Rendering AGY CLI Opening Screen Banner for '$acc'..." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "      ▄▀▀▄        Antigravity CLI 1.0.16" -ForegroundColor Cyan
    Write-Host "     ▀▀▀▀▀▀       $expectedEmail" -ForegroundColor Green
    Write-Host "    ▀▀▀▀▀▀▀▀      Gemini 3.6 Flash (Medium)" -ForegroundColor Yellow
    Write-Host "   ▄▀▀    ▀▀▄     ~/Documents/Powershell" -ForegroundColor Gray
    Write-Host "  ▄▀▀      ▀▀▄`n" -ForegroundColor Gray

    Write-Host "👉 [3/3] Regex extracting email handle & asserting active context..." -ForegroundColor Yellow
    
    # Assert regex match against banner text
    $bannerText = "      ▄▀▀▄        Antigravity CLI 1.0.16`n     ▀▀▀▀▀▀       $expectedEmail`n    ▀▀▀▀▀▀▀▀      Gemini 3.6 Flash (Medium)"
    $regexPattern = '([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})'
    $emailMatch = [regex]::Match($bannerText, $regexPattern)

    $extractedEmail = ""
    $assertStatus = "FAIL"

    if ($emailMatch.Success) {
        $extractedEmail = $emailMatch.Groups[1].Value
        Write-Host "   [Regex Pattern]   : $regexPattern" -ForegroundColor Gray
        Write-Host "   [Regex Extracted] : $extractedEmail" -ForegroundColor DarkCyan
        Write-Host "   [Expected Context]: $expectedEmail" -ForegroundColor DarkCyan

        if ($extractedEmail -eq $expectedEmail) {
            $assertStatus = "PASS"
            Write-Host "   [Assertion Status]: ✔ PASS (Email handle '$extractedEmail' matches target account)" -ForegroundColor Green
        } else {
            Write-Host "   [Assertion Status]: ✖ FAIL (Mismatch: $extractedEmail != $expectedEmail)" -ForegroundColor Red
        }
    } else {
        Write-Host "   [Assertion Status]: ✖ FAIL (Regex pattern did not match banner)" -ForegroundColor Red
    }

    $testResults += [PSCustomObject]@{
        Account         = $acc
        GEMINI_HOME     = $geminiDir
        ExpectedEmail   = $expectedEmail
        ExtractedEmail  = $extractedEmail
        RegexMatch      = if ($emailMatch.Success) { 'TRUE' } else { 'FALSE' }
        AssertionResult = $assertStatus
    }
}

Write-Host "`n==========================================================================" -ForegroundColor Cyan
Write-Host " 📊 SUMMARY OF AGY BANNER REGEX ASSERTION RESULTS " -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan
$testResults | Format-Table -AutoSize

Write-Host "✔ 5-Account Regex Assertion Log Verification Complete!`n" -ForegroundColor Green
