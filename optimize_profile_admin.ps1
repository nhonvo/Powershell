# ==============================================================================
#  Antigravity PowerShell Profile Startup Optimizer (Run as Administrator)
# ==============================================================================

$path1 = "C:\ProgramData\PowerShell\Microsoft.PowerShell_profile.ps1"
$path2 = "C:\ProgramData\PowerShell\Profile\00-Core.ps1"

if (Test-Path $path1) {
    $content = [System.IO.File]::ReadAllText($path1)
    # Normalize to CRLF
    $content = $content -replace "`r`n", "`n" -replace "`n", "`r`n"
    $old1 = "if (`$global:AgyProfileLoaded) { return }`r`n`$global:AgyProfileLoaded = `$true"
    $new1 = "if (`$global:AgyProfileLoaded -or `$global:AgyUserProfileLoaded) { return }`r`n`$global:AgyProfileLoaded = `$true`r`n`$global:AgyUserProfileLoaded = `$true"
    if ($content.Contains($old1)) {
        $content = $content.Replace($old1, $new1)
        [System.IO.File]::WriteAllText($path1, $content)
        Write-Host "✅ Updated $path1 successfully." -ForegroundColor Green
    } else {
        Write-Host "ℹ️ $path1 is already optimized." -ForegroundColor Cyan
    }
} else {
    Write-Warning "⚠️ Global profile loader not found at $path1"
}

if (Test-Path $path2) {
    $content = [System.IO.File]::ReadAllText($path2)
    # Normalize to CRLF
    $content = $content -replace "`r`n", "`n" -replace "`n", "`r`n"
    
    $old2 = 'foreach ($mod in $modules) {
    # 1. Auto-Install if missing (only in interactive console)
    if (-not (Get-Module -ListAvailable -Name $mod.Name)) {
        if ([Console]::IsOutputRedirected -or -not [Environment]::UserInteractive) {
            Write-Warning "[!] Module $($mod.Name) is missing and console is non-interactive. Skipping installation."
            continue
        }
        Write-Host "[+] Installing $($mod.Name) ($($mod.Description))..." -ForegroundColor Cyan
        try {
            Install-Module $mod.Name -Scope CurrentUser -Force -AllowClobber -SkipPublisherCheck -ErrorAction Stop
        } catch {
            Write-Warning "[!] Failed to install $($mod.Name). Skipping."
            continue
        }
    }
    
    # 2. Safe Import
    try {
        if ($mod.Name -eq "Terminal-Icons") {
            Import-Module $mod.Name -Force -ErrorAction SilentlyContinue
        } else {
            Import-Module $mod.Name -ErrorAction SilentlyContinue
        }
    } catch {
        Write-Warning "[x] Error loading $($mod.Name): $_"
    }
}'

    $new2 = 'foreach ($mod in $modules) {
    $loaded = $false
    try {
        if ($mod.Name -eq "Terminal-Icons") {
            Import-Module $mod.Name -Force -ErrorAction Stop
        } else {
            Import-Module $mod.Name -ErrorAction Stop
        }
        $loaded = $true
    } catch {
        # Auto-Install if missing or failed to import (only in interactive console)
        if ([Console]::IsOutputRedirected -or -not [Environment]::UserInteractive) {
            Write-Warning "[!] Module $($mod.Name) is missing or failed to load and console is non-interactive. Skipping installation."
            continue
        }
        Write-Host "[+] Installing $($mod.Name) ($($mod.Description))..." -ForegroundColor Cyan
        try {
            Install-Module $mod.Name -Scope CurrentUser -Force -AllowClobber -SkipPublisherCheck -ErrorAction Stop
            if ($mod.Name -eq "Terminal-Icons") {
                Import-Module $mod.Name -Force -ErrorAction SilentlyContinue
            } else {
                Import-Module $mod.Name -ErrorAction SilentlyContinue
            }
        } catch {
            Write-Warning "[!] Failed to install/load $($mod.Name). Skipping."
        }
    }
}'
    
    # Normalize CRLF on our pattern strings too
    $old2 = $old2 -replace "`r`n", "`n" -replace "`n", "`r`n"
    $new2 = $new2 -replace "`r`n", "`n" -replace "`n", "`r`n"
    
    if ($content.Contains($old2)) {
        $content = $content.Replace($old2, $new2)
        [System.IO.File]::WriteAllText($path2, $content)
        Write-Host "✅ Updated $path2 successfully (direct import optimization)." -ForegroundColor Green
    } else {
        Write-Host "ℹ️ $path2 is already optimized." -ForegroundColor Cyan
    }
} else {
    Write-Warning "⚠️ Global core profile script not found at $path2"
}
