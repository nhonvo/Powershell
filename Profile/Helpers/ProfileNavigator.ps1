#region PROFILE NAVIGATOR
# ==============================================================================
#  Strongly-typed directory navigator class.
# ==============================================================================

class ProfileNavigator {
    static [bool] PollGitStatusJobs() {
        if ($null -eq $Global:GitStatusCache) {
            $Global:GitStatusCache = @{}
        }
        $job = Get-Job -Name "GitStatusScanner" -ErrorAction SilentlyContinue
        if ($job -and $job.State -eq "Completed") {
            $result = Receive-Job -Job $job -ErrorAction SilentlyContinue
            if ($result -is [hashtable]) {
                foreach ($key in $result.Keys) {
                    $Global:GitStatusCache[$key] = $result[$key]
                }
            }
            Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
            return $true
        }
        return $false
    }

    static [string] GetGitStatusText([string]$path) {
        if (-not (Test-Path (Join-Path $path ".git"))) {
            return "|"
        }
        if ($null -eq $Global:GitStatusCache) {
            $Global:GitStatusCache = @{}
        }
        if ($Global:GitStatusCache.ContainsKey($path)) {
            return $Global:GitStatusCache[$path]
        }
        return "...|"
    }

    static [void] GoParent() {
        Set-Location ..
    }

    static [void] GoGrandParent() {
        Set-Location ..\..
    }

    static [void] EnterProject([string]$Name) {
        $cacheFile = Join-Path $env:USERPROFILE ".gemini\antigravity\workspace_cache.json"
        $priorityProjects = $Global:ProfileWorkspaces
        
        $items = @()
        $needsScan = $true

        if (-not $Name -and (Test-Path $cacheFile)) {
            $cacheItem = Get-Item $cacheFile
            if ($cacheItem.LastWriteTime -gt (Get-Date).AddHours(-24)) {
                try {
                    $items = Get-Content $cacheFile | ConvertFrom-Json
                    $needsScan = $false
                } catch {}
            }
        }

        if ($needsScan) {
            $searchPaths = @(
                "$env:USERPROFILE\Desktop\back-up\1.project",
                "$env:USERPROFILE\Desktop\project",
                "C:\Users\sshuser\project",
                "$env:USERPROFILE\Documents"
            ) | Where-Object { Test-Path $_ }

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
            foreach ($p in $priorityProjects) {
                $match = $allProjects | Where-Object { $_.Name -eq $p.Name } | Select-Object -First 1
                if ($match) { $items += [PSCustomObject]@{ Label = $match.Name; Path = $match.FullName; Priority = $true } }
            }
            foreach ($proj in ($allProjects | Where-Object { $_.Name -notin $priorityNames })) {
                $items += [PSCustomObject]@{ Label = $proj.Name; Path = $proj.FullName; Priority = $false }
            }
            
            try {
                # Ensure dir exists
                $cacheDir = Split-Path $cacheFile
                if (-not (Test-Path $cacheDir)) { New-Item -ItemType Directory -Path $cacheDir -Force | Out-Null }
                $items | ConvertTo-Json | Set-Content $cacheFile -Force
            } catch {}
        }

        # Direct match/list checks in AI Mode
        if ($Global:AiMode -and -not $Name) {
            foreach ($item in $items) {
                Write-Host "$($item.Label) | $($item.Path)"
            }
            return
        }

        # Direct match check
        if ($Name) {
            # List projects on screen if requested
            if ($Name -eq '-list' -or $Name -eq '-l' -or $Name -eq '--list') {
                Write-Host ""
                Write-Host "Registered Workspaces:" -ForegroundColor Cyan
                Write-Host "======================" -ForegroundColor Cyan
                foreach ($item in $items) {
                    $icon = if ($item.Priority) { "★" } else { " " }
                    Write-Host ("  $icon {0,-30} - {1}" -f $item.Label, $item.Path)
                }
                Write-Host ""
                return
            }

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

        # Trigger the Git status background scanner job if not already running/completed
        $job = Get-Job -Name "GitStatusScanner" -ErrorAction SilentlyContinue
        if (-not $job) {
            $paths = $items.Path
            Start-Job -Name "GitStatusScanner" -ScriptBlock {
                param($pathsList)
                $res = @{}
                foreach ($p in $pathsList) {
                    if (Test-Path (Join-Path $p ".git")) {
                        try {
                            Push-Location $p -ErrorAction SilentlyContinue
                            $branch = (git rev-parse --abbrev-ref HEAD 2>$null)
                            if ($branch) {
                                $status = (git status --porcelain=v1 --ignore-submodules=all 2>$null)
                                $changes = 0
                                if ($status) {
                                    $changes = ($status | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Measure-Object).Count
                                }
                                $res[$p] = "$($branch.Trim())|$changes"
                            } else {
                                $res[$p] = "|"
                            }
                        } catch {
                            $res[$p] = "|"
                        } finally {
                            Pop-Location -ErrorAction SilentlyContinue
                        }
                    } else {
                        $res[$p] = "|"
                    }
                }
                return $res
            } -ArgumentList @(,$paths) | Out-Null
        }

        # Helper scriptblock to generate string labels for the table
        $generateLabels = {
            param($itemList)
            $lbls = [System.Collections.Generic.List[string]]::new()
            for ($i = 0; $i -lt $itemList.Count; $i++) {
                $icon = if ($itemList[$i].Priority) { "★" } else { " " }
                $gitStatusText = [ProfileNavigator]::GetGitStatusText($itemList[$i].Path)
                
                $parts = $gitStatusText -split '\|'
                $branch = if ($parts[0]) { $parts[0] } else { "" }
                $changes = if ($parts.Count -gt 1) { $parts[1] } else { "" }
                
                $namePadded = $itemList[$i].Label.PadRight(35)
                $branchStr = if ($branch -and $branch -ne "...") { "🌳 " + $branch } elseif ($branch -eq "...") { "🔄 checking..." } else { "" }
                $branchPadded = $branchStr.PadRight(20)
                
                $changeStr = ""
                if ($changes -ne "0" -and $changes -ne "") {
                    $changeStr = "📝 $changes files"
                }
                
                $null = $lbls.Add("$icon 📂 $namePadded │ $branchPadded │ $changeStr")
            }
            return $lbls.ToArray()
        }

        # Build initial labels array for TUI selection
        $labels = &$generateLabels $items

        # Setup the global non-blocking update callback closure for TerminalMenu
        $Global:TerminalMenuOnIdle = {
            $updated = [ProfileNavigator]::PollGitStatusJobs()
            if ($updated) {
                return &$generateLabels $items
            }
            return $null
        }.GetNewClosure()

        $cHalf = [char]0x2584
        $cFull = [char]0x2588
        $cTop  = [char]0x2580
        $projHeaders = @(
            "  $cHalf$cFull$cFull$cFull$cFull$cHalf   $cHalf$cFull$cFull$cFull$cFull$cHalf     Powershell Profile CLI v2.0",
            " $cFull$cTop     $cTop $cFull$cTop     $cTop    Workspace Projects",
            " $cFull        $cFull           ",
            " $cFull$cHalf     $cHalf $cFull$cHalf     $cHalf    Select a registered project workspace.",
            "  $cTop$cFull$cFull$cFull$cFull$cTop   $cTop$cFull$cFull$cFull$cFull$cTop     Esc to go back.",
            "====================================================================================="
        )
        
        while ($true) {
            $selected = ([type]"TerminalMenu")::ShowRobust($projHeaders, $labels, 0, $false, $true)
            if ($selected -ge 0) {
                $proj = $items[$selected]
                $actionHeaders = @(
                    "=====================================================================================",
                    " Actions for: $($proj.Label)",
                    "====================================================================================="
                )
                $actionLabels = @(
                    "💻 Open in VS Code",
                    "📝 Open in Terminal IDE (Micro/Nvim)",
                    "🐳 Start Docker Compose",
                    "🧪 Run Unit Tests",
                    "📂 Navigate here in current terminal"
                )
                $actionSelected = ([type]"TerminalMenu")::ShowRobust($actionHeaders, $actionLabels, 0, $false, $false)
                
                Write-Host ""
                if ($actionSelected -eq 0) {
                    Write-Host "  Opening $($proj.Label) in VS Code..." -ForegroundColor Green
                    Start-Process code -ArgumentList "`"$($proj.Path)`"" -ErrorAction SilentlyContinue
                    $Global:ExitCcLoop = $true
                    break
                }
                elseif ($actionSelected -eq 1) {
                    [ProfileNavigator]::LaunchTerminalIde($proj.Path)
                    $Global:ExitCcLoop = $true
                    break
                }
                elseif ($actionSelected -eq 2) {
                    Write-Host "  Starting Docker Compose in $($proj.Label)..." -ForegroundColor Green
                    Start-Process pwsh -ArgumentList "-NoExit", "-Command", "Set-Location `"$($proj.Path)`"; docker-compose up" -ErrorAction SilentlyContinue
                    $Global:ExitCcLoop = $true
                    break
                }
                elseif ($actionSelected -eq 3) {
                    Write-Host "  Running tests in $($proj.Label)..." -ForegroundColor Green
                    Start-Process pwsh -ArgumentList "-NoExit", "-Command", "Set-Location `"$($proj.Path)`"; if (Test-Path .\run_tests.ps1) { .\run_tests.ps1 } else { dotnet test }" -ErrorAction SilentlyContinue
                    $Global:ExitCcLoop = $true
                    break
                }
                elseif ($actionSelected -eq 4) {
                    Write-Host "  Navigating to: $($proj.Label)" -ForegroundColor Green
                    Set-Location $proj.Path
                    $Global:ExitCcLoop = $true
                    break
                }
                elseif ($actionSelected -eq -1) {
                    # Go back to Project List loop on Esc
                    continue
                }
            } else {
                Write-Host ""
                Write-Host "  Cancelled." -ForegroundColor DarkGray
                break
            }
        }
    }

    static [void] LaunchTerminalIde([string]$projectPath) {
        $microCmd = Get-Command micro -ErrorAction SilentlyContinue
        $nvimCmd = Get-Command nvim -ErrorAction SilentlyContinue

        if (-not $microCmd -and -not $nvimCmd) {
            Write-Host "  Neither Micro nor NeoVim found in PATH." -ForegroundColor Yellow
            Write-Host "  Would you like to install the Micro editor via winget now? (y/n): " -NoNewline -ForegroundColor Cyan
            $resp = [Console]::ReadKey($true)
            if ($resp.KeyChar -eq 'y' -or $resp.KeyChar -eq 'Y') {
                Write-Host "y"
                Write-Host "  Installing Micro editor..." -ForegroundColor Green
                try {
                    $process = Start-Process winget -ArgumentList "install zyedidia.micro --silent" -NoNewWindow -PassThru -Wait
                    # Reload path
                    $envPath = [Environment]::GetEnvironmentVariable("Path", "User") + ";" + [Environment]::GetEnvironmentVariable("Path", "Machine")
                    $env:Path = $envPath
                    $microCmd = Get-Command micro -ErrorAction SilentlyContinue
                } catch {
                    Write-Error "Failed to install Micro via winget: $_"
                }
            } else {
                Write-Host "n"
            }
        }

        # Navigate to the project directory first for editor context
        Set-Location $projectPath

        if ($microCmd) {
            # Use an isolated configuration directory specifically for our PowerShell Profile IDE
            $microConfigDir = Join-Path $env:USERPROFILE ".gemini\antigravity\micro-ide"
            if (-not (Test-Path $microConfigDir)) {
                $null = New-Item -ItemType Directory -Path $microConfigDir -Force
            }
            
            # Install filemanager plugin into the isolated configuration dir
            $pluginDir = Join-Path $microConfigDir "plug/filemanager"
            if (-not (Test-Path $pluginDir)) {
                Write-Host "  Installing Micro filemanager plugin..." -ForegroundColor Green
                Start-Process micro -ArgumentList "-config-dir `"$microConfigDir`"", "-plugin install filemanager" -NoNewWindow -Wait
            }

            # Map Ctrl+B to toggle filemanager in bindings.json inside the isolated config
            $bindingsFile = Join-Path $microConfigDir "bindings.json"
            $hasBinding = $false
            if (Test-Path $bindingsFile) {
                try {
                    $bindings = Get-Content $bindingsFile -Raw | ConvertFrom-Json
                    if ($bindings -and $bindings.PSObject.Properties.Name -contains "Ctrl-b" -and $bindings."Ctrl-b" -eq "command:filemanager") {
                        $hasBinding = $true
                    }
                } catch {}
            }
            if (-not $hasBinding) {
                $bindingObj = @{ "Ctrl-b" = "command:filemanager" }
                $bindingObj | ConvertTo-Json | Set-Content $bindingsFile -Force
            }

            # Configure Micro settings to open filemanager on start in the isolated config
            $settingsFile = Join-Path $microConfigDir "settings.json"
            $hasSettings = $false
            if (Test-Path $settingsFile) {
                try {
                    $settings = Get-Content $settingsFile -Raw | ConvertFrom-Json
                    if ($settings -and $settings.PSObject.Properties.Name -contains "filemanager.openonstart" -and $settings."filemanager.openonstart" -eq $true) {
                        $hasSettings = $true
                    }
                } catch {}
            }
            if (-not $hasSettings) {
                $settingsObj = @{ "filemanager.openonstart" = $true }
                $settingsObj | ConvertTo-Json | Set-Content $settingsFile -Force
            }

            Write-Host "  Launching Micro IDE with sidebar (Ctrl-B to toggle)..." -ForegroundColor Green
            Start-Process micro -ArgumentList "-config-dir `"$microConfigDir`"" -NoNewWindow -Wait
        }
        elseif ($nvimCmd) {
            Write-Host "  Launching NeoVim..." -ForegroundColor Green
            Start-Process nvim -NoNewWindow -Wait
        }
        else {
            Write-Warning "No Terminal IDE is available."
        }
    }
}
#endregion



