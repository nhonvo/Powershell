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
function Set-LocationParent { Set-Location .. }

<#
.SYNOPSIS
Navigates to the grandparent directory.
.CATEGORY
System & Utility Commands
#>
function Set-LocationGrandParent { Set-Location ../.. }

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
function proj {
    [CmdletBinding()]
    param()

    # --- CONFIGURATION ---
    $projectsPath = Join-Path $env:USERPROFILE "Desktop"
    $priorityProjects = @(
        @{ Name = "nhonvo.github.io";    Short = "blog" },
        @{ Name = "senior-developer-study-plan";    Short = "senior" },
        @{ Name = "back-up";    Short = "ba" },
        @{ Name = "clean-architecture-net-8.0";    Short = "clean" },
        @{ Name = "profile";    Short = "profile" }
    )
    # --- END CONFIGURATION ---

    if (-not (Test-Path $projectsPath)) {
        Write-Host "❌ Projects directory not found: $projectsPath" -ForegroundColor Red
        return
    }

    $allProjects = Get-ChildItem -Path $projectsPath -Directory | Sort-Object Name
    if ($allProjects.Count -eq 0) {
        Write-Host "🟡 No project folders found in $projectsPath" -ForegroundColor Yellow
        return
    }

    $tableData = @()
    $number = 1

    foreach ($priority in $priorityProjects) {
        $project = $allProjects | Where-Object { $_.Name -eq $priority.Name }
        if ($project) {
            $tableData += [PSCustomObject]@{ Number = $number++; Short = $priority.Short; Project = $project.Name; Path = $project.FullName; IsPriority = $true }
        }
    }

    $priorityNames = $priorityProjects.Name
    $remainingProjects = $allProjects | Where-Object { $_.Name -notin $priorityNames }

    foreach ($project in $remainingProjects) {
        $words = $project.Name -split '[\.\-_]+'
        $shortName = if ($words.Count -eq 1) { $words[0].Substring(0, [Math]::Min(3, $words[0].Length)) } else { ($words | ForEach-Object { $_.Substring(0, 1) }) -join '' }
        $shortName = $shortName.ToLower()
        $originalShort = $shortName
        $counter = 2
        while ($tableData.Short -contains $shortName) { $shortName = "$($originalShort)$($counter++)" }
        $tableData += [PSCustomObject]@{ Number = $number++; Short = $shortName; Project = $project.Name; Path = $project.FullName; IsPriority = $false }
    }

    Write-Host "Available Projects:" -ForegroundColor Cyan
    Write-Host ("-" * 40)
    foreach ($item in $tableData) {
        $line = "{0,3}. [{1,-7}] {2}" -f $item.Number, $item.Short, $item.Project
        if ($item.IsPriority) { Write-Host $line -ForegroundColor Cyan } else { Write-Host $line }
    }
    Write-Host ("-" * 40)

    $selection = Read-Host "Select a project (by short name or number) or '0' to stay"

    if ($selection -eq '0' -or [string]::IsNullOrWhiteSpace($selection)) {
        Write-Host "➡️ Staying in current directory." -ForegroundColor Green
        return
    }

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
#endregion
#endregion