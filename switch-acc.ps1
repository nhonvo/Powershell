# C:\Users\TruongNhon\Documents\Powershell\switch-acc.ps1
# Interactive Antigravity Account Switcher Script
# Swaps the active isolated session files while preserving shared directory junctions

$ErrorActionPreference = "Stop"

$userProfile = $env:USERPROFILE
$activeDir = Join-Path $userProfile ".gemini"
$markerFile = Join-Path $activeDir ".active_profile_name"

# Shared directories that should not be deleted or copied during account switching
$sharedNames = @(
    "antigravity",
    "antigravity-cli",
    "config",
    "history",
    "antigravity-ide",
    "wf",
    "antigravity-browser-profile",
    "antigravity-backup",
    "tmp"
)

# 1. Discover stored profiles
$profiles = Get-ChildItem -Path $userProfile -Directory -Filter ".gemini_*" | Where-Object {
    $_.Name -notmatch "backup|copy|temp"
}

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "   Antigravity Account Switcher" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Determine active profile
$currentProfile = "unknown"
if (Test-Path $markerFile) {
    $currentProfile = Get-Content $markerFile -Raw
    $currentProfile = $currentProfile.Trim()
}
Write-Host "Current Active Account Profile: [$currentProfile]" -ForegroundColor Yellow
Write-Host ""

# Check arguments or run interactively
$targetName = $args[0]

if (-not $targetName) {
    Write-Host "Available Profiles:" -ForegroundColor Gray
    $idx = 1
    $profileMap = @{}
    foreach ($p in $profiles) {
        $pName = $p.Name.Replace(".gemini_", "")
        Write-Host "  [$idx] $pName" -ForegroundColor Green
        $profileMap[$idx] = $pName
        $idx++
    }
    Write-Host "  [N] Create new profile from current active session" -ForegroundColor Gray
    Write-Host ""
    
    $choice = Read-Host "Select a profile number or action"
    if ($null -ne $choice) {
        $choice = $choice.Trim()
    } else {
        $choice = ""
    }
    
    if ($choice -eq "N" -or $choice -eq "n") {
        $newName = Read-Host "Enter name for the new profile (e.g. acc3)"
        if ($null -ne $newName) {
            $newName = $newName.Trim()
        } else {
            $newName = ""
        }
        if (-not $newName) {
            Write-Host "Invalid name. Operation cancelled." -ForegroundColor Red
            Exit
        }
        $targetProfileDir = Join-Path $userProfile ".gemini_$newName"
        if (Test-Path $targetProfileDir) {
            Write-Host "Profile '$newName' already exists." -ForegroundColor Red
            Exit
        }
        
        # Create profile directory
        $null = New-Item -ItemType Directory -Path $targetProfileDir -Force
        
        # Copy isolated files/folders from activeDir
        if (Test-Path $activeDir) {
            Get-ChildItem -Path $activeDir | ForEach-Object {
                $name = $_.Name
                if ($name -notin $sharedNames -and $name -ne ".active_profile_name") {
                    $destPath = Join-Path $targetProfileDir $name
                    if ($_.PsIsContainer) {
                        Copy-Item -Path $_.FullName -Destination $targetProfileDir -Recurse -Force
                    } else {
                        Copy-Item -Path $_.FullName -Destination $destPath -Force
                    }
                }
            }
            
            # Create junctions for shared folders pointing to activeDir
            foreach ($sharedName in $sharedNames) {
                $sharedPath = Join-Path $activeDir $sharedName
                if (-not (Test-Path $sharedPath)) {
                    $null = New-Item -ItemType Directory -Path $sharedPath -Force
                }
                $junctionPath = Join-Path $targetProfileDir $sharedName
                $null = New-Item -ItemType Junction -Path $junctionPath -Value $sharedPath -Force
            }
        }
        
        Set-Content -Path (Join-Path $targetProfileDir ".active_profile_name") -Value $newName
        Set-Content -Path $markerFile -Value $newName
        Write-Host "Saved current session as profile '$newName'." -ForegroundColor Green
        Exit
    }
    
    if ($profileMap.ContainsKey([int]$choice)) {
        $targetName = $profileMap[[int]$choice]
    }
}

if (-not $targetName) {
    Write-Host "Invalid selection. Exiting." -ForegroundColor Red
    Exit
}

$targetProfileDir = Join-Path $userProfile ".gemini_$targetName"
if (-not (Test-Path $targetProfileDir)) {
    Write-Host "Profile directory not found: $targetProfileDir" -ForegroundColor Red
    Exit
}

# 2. Save active session state back to its original profile folder
if (Test-Path $activeDir) {
    $backupProfileDir = $null
    if ($currentProfile -ne "unknown") {
        $backupProfileDir = Join-Path $userProfile ".gemini_$currentProfile"
        Write-Host "Backing up active session to '$currentProfile'..." -ForegroundColor Gray
    } else {
        # Unmarked active folder, save to temp backup to avoid loss
        $backupProfileDir = Join-Path $userProfile ".gemini_backup_temp"
        Write-Host "Active session was unmarked. Backing up to '.gemini_backup_temp'..." -ForegroundColor Gray
    }
    
    if (-not (Test-Path $backupProfileDir)) {
        $null = New-Item -ItemType Directory -Path $backupProfileDir -Force
    }
    
    # Back up isolated files and folders from activeDir to backupProfileDir
    Get-ChildItem -Path $activeDir | ForEach-Object {
        $name = $_.Name
        if ($name -notin $sharedNames -and $name -ne ".active_profile_name") {
            $destPath = Join-Path $backupProfileDir $name
            if ($_.PsIsContainer) {
                if (Test-Path $destPath) {
                    Remove-Item -Path $destPath -Recurse -Force
                }
                Copy-Item -Path $_.FullName -Destination $backupProfileDir -Recurse -Force
            } else {
                Copy-Item -Path $_.FullName -Destination $destPath -Force
            }
        }
    }
    
    # Ensure junctions exist in the backup directory pointing to activeDir
    foreach ($sharedName in $sharedNames) {
        $sharedPath = Join-Path $activeDir $sharedName
        if (-not (Test-Path $sharedPath)) {
            $null = New-Item -ItemType Directory -Path $sharedPath -Force
        }
        $junctionPath = Join-Path $backupProfileDir $sharedName
        if (-not (Test-Path $junctionPath)) {
            $null = New-Item -ItemType Junction -Path $junctionPath -Value $sharedPath -Force
        }
    }
}

# 3. Swap in target profile
Write-Host "Switching active profile to '$targetName'..." -ForegroundColor Gray

# Delete isolated files/folders from activeDir
if (Test-Path $activeDir) {
    Get-ChildItem -Path $activeDir | ForEach-Object {
        $name = $_.Name
        if ($name -notin $sharedNames -and $name -ne ".active_profile_name") {
            Remove-Item -Path $_.FullName -Recurse -Force
        }
    }
} else {
    $null = New-Item -ItemType Directory -Path $activeDir -Force
}

# Copy isolated files/folders from targetProfileDir to activeDir
Get-ChildItem -Path $targetProfileDir | ForEach-Object {
    $name = $_.Name
    # Check if the target is a junction or a shared folder
    $isJunction = (Get-Item $_.FullName).LinkType -eq "Junction"
    if (-not $isJunction -and $name -notin $sharedNames -and $name -ne ".active_profile_name") {
        $destPath = Join-Path $activeDir $name
        if ($_.PsIsContainer) {
            if (Test-Path $destPath) {
                Remove-Item -Path $destPath -Recurse -Force
            }
            Copy-Item -Path $_.FullName -Destination $activeDir -Recurse -Force
        } else {
            Copy-Item -Path $_.FullName -Destination $destPath -Force
        }
    }
}

# Update marker in active folder
Set-Content -Path $markerFile -Value $targetName

Write-Host "Successfully switched to account profile '$targetName'!" -ForegroundColor Green
Write-Host "You can now run your Antigravity commands normally." -ForegroundColor Cyan
