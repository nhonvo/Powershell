#region PROFILE NAVIGATOR
# ==============================================================================
#  Strongly-typed directory navigator class.
# ==============================================================================

class ProfileNavigator {
    static [void] GoParent() {
        Set-Location ..
    }

    static [void] GoGrandParent() {
        Set-Location ..\..
    }

    static [void] EnterProject([string]$Name) {
        $searchPaths = @(
            "$env:USERPROFILE\Desktop\back-up\1.project",
            "$env:USERPROFILE\Desktop\project",
            "C:\Users\sshuser\project",
            "$env:USERPROFILE\Documents\Powershell"
        ) | Where-Object { Test-Path $_ }

        $priorityProjects = @(
            @{ Name = "Powershell";         Short = "pw"   }
            @{ Name = "finance-dashboard";  Short = "fin"  }
            @{ Name = "clean-architecture-net-8.0"; Short = "clean" }
            @{ Name = "BinhDinhFood";       Short = "food" }
            @{ Name = "test-road-map";      Short = "road" }
        )
        $excludeFolders = @("My Music","My Pictures","My Videos","WindowsPowerShell",
                            "Custom Office Templates","Visual Studio 2022","Modules",
                            "vscode-config","typora-themes","img")

        if (-not $searchPaths) {
            Write-Host "No search paths found. Configure search paths in ProfileNavigator." -ForegroundColor Yellow
            return
        }

        # Collect immediate-child directories
        $allProjects = $searchPaths | ForEach-Object {
            Get-ChildItem -Path $_ -Directory -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -notin $excludeFolders }
        } | Sort-Object Name -Unique

        if (-not $allProjects) {
            Write-Host "No projects found in configured paths." -ForegroundColor Yellow
            return
        }

        # Build ordered list: priorities first, then alphabetical
        $priorityNames = $priorityProjects.Name
        $items = @()
        foreach ($p in $priorityProjects) {
            $match = $allProjects | Where-Object { $_.Name -eq $p.Name } | Select-Object -First 1
            if ($match) { $items += [PSCustomObject]@{ Label = $match.Name; Path = $match.FullName; Priority = $true } }
        }
        foreach ($proj in ($allProjects | Where-Object { $_.Name -notin $priorityNames })) {
            $items += [PSCustomObject]@{ Label = $proj.Name; Path = $proj.FullName; Priority = $false }
        }

        # Direct match check
        if ($Name) {
            # Check priority short names first
            $matchedPriority = $priorityProjects | Where-Object { $_.Short -eq $Name } | Select-Object -First 1
            if ($matchedPriority) {
                $matchedProj = $items | Where-Object { $_.Label -eq $matchedPriority.Name } | Select-Object -First 1
                if ($matchedProj) {
                    Write-Host "  Navigating to: $($matchedProj.Label)" -ForegroundColor Green
                    Set-Location $matchedProj.Path
                    return
                }
            }

            # Match by name/label (case-insensitive contains)
            $matches = $items | Where-Object { $_.Label -like "*$Name*" }
            if ($matches.Count -eq 1) {
                Write-Host "  Navigating to: $($matches[0].Label)" -ForegroundColor Green
                Set-Location $matches[0].Path
                return
            } elseif ($matches.Count -gt 1) {
                $items = $matches
            } else {
                Write-Host "  No project matching '$Name' found." -ForegroundColor Yellow
                return
            }
        }

        # Build labels array for TUI selection
        $labels = @()
        for ($i = 0; $i -lt $items.Count; $i++) {
            $icon = if ($items[$i].Priority) { "★" } else { " " }
            $labels += "$icon $($items[$i].Label)"
        }

        $selected = ([type]"TerminalMenu")::Show("Select Project", $labels, 0)
        if ($selected -ge 0) {
            Write-Host ""
            Write-Host "  Navigating to: $($items[$selected].Label)" -ForegroundColor Green
            Set-Location $items[$selected].Path
        } else {
            Write-Host ""
            Write-Host "  Cancelled." -ForegroundColor DarkGray
        }
    }
}
#endregion
