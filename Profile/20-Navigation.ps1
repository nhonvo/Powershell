#region NAVIGATION
# ------------------------------------------------------------------------------
#  Functions for directory and project navigation.
# ------------------------------------------------------------------------------

<#
.SYNOPSIS
Navigates to the parent directory.
.CATEGORY
System & Utility Commands
#>
function Set-LocationParent {
    [CmdletBinding()]
    param()
    Set-Location ..
}

<#
.SYNOPSIS
Navigates to the grandparent directory.
.CATEGORY
System & Utility Commands
#>
function Set-LocationGrandParent {
    [CmdletBinding()]
    param()
    Set-Location ..\..
}


#region PROJECT NAVIGATION
# ------------------------------------------------------------------------------
#  Quickly navigate between project directories.
# ------------------------------------------------------------------------------
<#
.SYNOPSIS
Provides an interactive menu to navigate to project directories.
.DESCRIPTION
Scans a root project directory, lists all sub-folders, and allows you to jump
to one by selecting its number or a pre-defined short name.
Priority projects can be configured for quick access.
.CATEGORY
Project Navigation
#>
function Enter-Project {
    [CmdletBinding()]
    param()

    # --- CONFIGURATION (Customize this list!) ---
    # Prioritize specific code directories to avoid scanning entire Documents/Desktop
    $potentialPaths = @(
        "C:\Code",
        "C:\Projects",
        "C:\Dev",
        "C:\Users\TruongNhon\Desktop\back-up\1.project",
        "$env:USERPROFILE\source\repos", # Default VS location
        "$env:USERPROFILE\Documents\Code",
        "$env:USERPROFILE\Documents\Projects",
        "$env:USERPROFILE\Desktop\Projects",
        "$env:USERPROFILE\Documents\Powershell" # Keep this
    )
    
    # Fallback: scan root of Documents/Desktop only if no specific code folders found
    # But filtering out heavy system folders
    $fallbackPaths = @(
        "$env:USERPROFILE\Documents",
        "$env:USERPROFILE\Desktop"
    )

    $searchPaths = @()
    foreach ($p in $potentialPaths) { if (Test-Path $p) { $searchPaths += $p } }
    
    # If no "Code" folders found, use fallback but warn/limit
    if ($searchPaths.Count -eq 0) {
        $searchPaths = $fallbackPaths
    }

    $priorityProjects = @(
        @{ Name = "Powershell";    Short = "pw" },
        @{ Name = "finance-dashboard";    Short = "fin" },
        @{ Name = "nextjs-template";    Short = "next" }
    )
    # --- END CONFIGURATION ---

    $allProjects = @()
    $excludeFolders = @("My Music", "My Pictures", "My Videos", "Default", "Public", "WindowsPowerShell", "Custom Office Templates", "Visual Studio 2022", "img", "Modules","vscode-config", "typora-themes")

    Write-Progress -Activity "Scanning for projects..." -Status "Please wait"
    
    foreach ($path in $searchPaths) {
        if (Test-Path $path) {
            try {
                # Only looking at immediate children (Depth 0) to prevent timeouts
                $items = Get-ChildItem -Path $path -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -notin $excludeFolders }
                if ($items) { $allProjects += $items }
            } catch {
                # Ignore errors
            }
        }
    }
    Write-Progress -Activity "Scanning for projects..." -Completed

    # Remove duplicates and sort
    $allProjects = $allProjects | Sort-Object Name -Unique

    if ($allProjects.Count -eq 0) {
        Write-Host "🟡 No project folders found. Please create a 'C:\Code' or 'Documents\Projects' folder." -ForegroundColor Yellow
        return
    }

    $tableData = @()
    $number = 1

    # 1. Add Priority Projects First
    foreach ($priority in $priorityProjects) {
        $project = $allProjects | Where-Object { $_.Name -eq $priority.Name }
        if ($project) {
            $tableData += [PSCustomObject]@{ Number = $number++; Short = $priority.Short; Project = $project.Name; Path = $project.FullName; IsPriority = $true }
        }
    }

    # 2. Add Remaining Projects
    $priorityNames = $priorityProjects.Name
    $remainingProjects = $allProjects | Where-Object { $_.Name -notin $priorityNames }

    foreach ($project in $remainingProjects) {
        # Generate short name (first 3 letters or initials)
        $words = $project.Name -split '[\.\-_]+'
        $shortName = if ($words.Count -eq 1) { 
            if ($words[0].Length -ge 3) { $words[0].Substring(0, 3) } else { $words[0] }
        } else { 
            # Initials
            ($words | ForEach-Object { $_.Substring(0, 1) }) -join '' 
        }
        $shortName = $shortName.ToLower()
        
        # Ensure uniqueness
        $originalShort = $shortName
        $counter = 2
        while ($tableData.Short -contains $shortName) { $shortName = "$($originalShort)$($counter++)" }

        $tableData += [PSCustomObject]@{ Number = $number++; Short = $shortName; Project = $project.Name; Path = $project.FullName; IsPriority = $false }
    }

    # 3. Display Menu
    Write-Host "Available Projects:" -ForegroundColor Cyan
    Write-Host ("-" * 50)
    foreach ($item in $tableData) {
        $line = "{0,3}. [{1,-7}] {2}" -f $item.Number, $item.Short, $item.Project
        if ($item.IsPriority) { Write-Host $line -ForegroundColor Cyan } else { Write-Host $line }
    }
    Write-Host ("-" * 50)

    # 4. Prompt User
    try {
        $selection = Read-Host "Select a project (by short name or number) or '0' to stay"
    } catch {
        # Handle non-interactive environments gracefully
        return
    }

    if ($selection -eq '0' -or [string]::IsNullOrWhiteSpace($selection)) {
        Write-Host "➡️ Staying in current directory." -ForegroundColor Green
        return
    }

    # 5. Handle Selection
    $selectedProject = $tableData | Where-Object { $_.Short -eq $selection.ToLower() }
    if (-not $selectedProject) {
        if ([int]::TryParse($selection, [ref]$null)) {
            $selectedProject = $tableData | Where-Object { $_.Number -eq [int]$selection }
        }
    }

    if ($selectedProject) {
        Write-Host "🚀 Navigating to: $($selectedProject.Project)" -ForegroundColor Green
        Set-Location $selectedProject.Path
        Get-ChildItem | Format-Table -AutoSize
    } else {
        Write-Host "❌ Invalid selection." -ForegroundColor Red
    }
}
