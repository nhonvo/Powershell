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

    # --- CONFIGURATION ---
    $searchPaths = @(
        "$env:USERPROFILE\Desktop\back-up\1.project",
        "$env:USERPROFILE\Documents\Powershell"
    ) | Where-Object { Test-Path $_ }

    $priorityProjects = @(
        @{ Name = "Powershell";         Short = "pw"   }
        @{ Name = "finance-dashboard";  Short = "fin"  }
        @{ Name = "nextjs-template";    Short = "next" }
    )
    $excludeFolders = @("My Music","My Pictures","My Videos","WindowsPowerShell",
                        "Custom Office Templates","Visual Studio 2022","Modules",
                        "vscode-config","typora-themes","img")
    # --- END CONFIGURATION ---

    if (-not $searchPaths) {
        Write-Host "No search paths found. Edit `$searchPaths in 20-Navigation.ps1." -ForegroundColor Yellow
        return
    }

    # Collect immediate-child directories only (fast, no recursion)
    $allProjects = $searchPaths | ForEach-Object {
        Get-ChildItem -Path $_ -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -notin $excludeFolders }
    } | Sort-Object Name -Unique

    if (-not $allProjects) {
        Write-Host "No projects found in configured paths." -ForegroundColor Yellow
        return
    }

    # Build ordered list: priorities first, then the rest alphabetically
    $priorityNames = $priorityProjects.Name
    $items = @()
    foreach ($p in $priorityProjects) {
        $match = $allProjects | Where-Object { $_.Name -eq $p.Name } | Select-Object -First 1
        if ($match) { $items += [PSCustomObject]@{ Label = $match.Name; Path = $match.FullName; Priority = $true } }
    }
    foreach ($proj in ($allProjects | Where-Object { $_.Name -notin $priorityNames })) {
        $items += [PSCustomObject]@{ Label = $proj.Name; Path = $proj.FullName; Priority = $false }
    }

    # Arrow-key menu
    $selected = 0
    [Console]::CursorVisible = $false
    Write-Host ""
    Write-Host "  Select project  (↑↓ move, Enter confirm, Esc cancel)" -ForegroundColor DarkGray
    Write-Host ""
    $menuTop = [Console]::CursorTop

    function Render-ProjectMenu {
        [Console]::SetCursorPosition(0, $menuTop)
        for ($i = 0; $i -lt $items.Count; $i++) {
            $icon = if ($items[$i].Priority) { "★" } else { " " }
            if ($i -eq $selected) {
                Write-Host "  ▶ $icon $($items[$i].Label)  " -ForegroundColor Cyan
            } else {
                $color = if ($items[$i].Priority) { "White" } else { "DarkGray" }
                Write-Host "    $icon $($items[$i].Label)  " -ForegroundColor $color
            }
        }
    }

    Render-ProjectMenu

    while ($true) {
        $key = [Console]::ReadKey($true)
        switch ($key.Key) {
            'UpArrow'   { if ($selected -gt 0) { $selected-- }; Render-ProjectMenu }
            'DownArrow' { if ($selected -lt $items.Count - 1) { $selected++ }; Render-ProjectMenu }
            'Enter' {
                [Console]::CursorVisible = $true
                Write-Host ""
                Write-Host "  Navigating to: $($items[$selected].Label)" -ForegroundColor Green
                Set-Location $items[$selected].Path
                return
            }
            'Escape' {
                [Console]::CursorVisible = $true
                Write-Host ""
                Write-Host "  Cancelled." -ForegroundColor DarkGray
                return
            }
        }
    }
}
