#region PROFILE NAVIGATOR
# ==============================================================================
#  Strongly-typed directory navigator class.
# ==============================================================================

class ProfileNavigator {
    static [void] PollGitStatusJobs() {
        if ($null -eq $Global:GitStatusCache) {
            $Global:GitStatusCache = @{}
        }
        $jobs = Get-Job -Name "GitStatus_*" -ErrorAction SilentlyContinue
        foreach ($job in $jobs) {
            if ($job.State -eq "Completed") {
                $result = Receive-Job -Job $job -ErrorAction SilentlyContinue
                $base64 = $job.Name.Substring("GitStatus_".Length)
                try {
                    $padLen = $base64.Length % 4
                    if ($padLen -gt 0) { $base64 += "=" * (4 - $padLen) }
                    $bytes = [Convert]::FromBase64String($base64)
                    $decodedPath = [Text.Encoding]::UTF8.GetString($bytes)
                    if ($decodedPath) {
                        $Global:GitStatusCache[$decodedPath] = $result
                    }
                } catch {}
                Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
            }
        }
    }

    static [string] GetGitStatusText([string]$path) {
        if (-not (Test-Path (Join-Path $path ".git"))) {
            return ""
        }
        if ($null -eq $Global:GitStatusCache) {
            $Global:GitStatusCache = @{}
        }
        if ($Global:GitStatusCache.ContainsKey($path) -and $Global:GitStatusCache[$path] -ne " (...)") {
            return $Global:GitStatusCache[$path]
        }
        if ($Global:GitStatusCache[$path] -eq " (...)") {
            return " (...)"
        }
        $Global:GitStatusCache[$path] = " (...)"
        try {
            $bytes = [Text.Encoding]::UTF8.GetBytes($path)
            $safeName = [Convert]::ToBase64String($bytes).Replace('=', '')
            $jobName = "GitStatus_$safeName"
            Start-Job -Name $jobName -ScriptBlock {
                param($p)
                try {
                    Set-Location $p
                    $branch = (git rev-parse --abbrev-ref HEAD 2>$null)
                    if (-not $branch) { return "" }
                    $status = (git status --porcelain=v1 --ignore-submodules=all 2>$null)
                    $changesCount = 0
                    if ($null -ne $status) {
                        $nonEmpty = $status | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
                        $changesCount = ($nonEmpty | Measure-Object).Count
                    }
                    if ($changesCount -gt 0) {
                        return " ($($branch.Trim())) [$changesCount files changed]"
                    } else {
                        return " ($($branch.Trim()))"
                    }
                } catch {
                    return ""
                }
            } -ArgumentList $path | Out-Null
        } catch {}
        return " (...)"
    }

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
            "$env:USERPROFILE\Documents"
        ) | Where-Object { Test-Path $_ }

        $priorityProjects = $Global:ProfileWorkspaces
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

        # Poll completed status jobs
        [ProfileNavigator]::PollGitStatusJobs()

        # Build labels array for TUI selection
        $labelsList = [LogHelper]::InvokeWithSpinner("[Project] Scanning workspaces & checking git status...", {
            $lbls = [System.Collections.Generic.List[string]]::new()
            for ($i = 0; $i -lt $items.Count; $i++) {
                $icon = if ($items[$i].Priority) { "★" } else { " " }
                $gitStatusText = [ProfileNavigator]::GetGitStatusText($items[$i].Path)
                $null = $lbls.Add("$icon $($items[$i].Label)$gitStatusText")
            }
            return $lbls
        })
        $labels = $labelsList.ToArray()

        $cHalf = [char]0x2584
        $cFull = [char]0x2588
        $cTop  = [char]0x2580
        $projHeaders = @(
            "  $cHalf$cFull$cFull$cFull$cFull$cHalf   $cHalf$cFull$cFull$cFull$cFull$cHalf     Powershell Profile CLI v2.0",
            " $cFull$cTop     $cTop $cFull$cTop     $cTop    Workspace Projects",
            " $cFull        $cFull           ",
            " $cFull$cHalf     $cHalf $cFull$cHalf     $cHalf    Select a registered project workspace.",
            "  $cTop$cFull$cFull$cFull$cFull$cTop   $cTop$cFull$cFull$cFull$cFull$cTop     Esc to go back.",
            "============================================="
        )
        $selected = ([type]"TerminalMenu")::ShowRobust($projHeaders, $labels, 0, $false, $true)
        if ($selected -ge 0) {
            $proj = $items[$selected]
            $action = $Global:TerminalMenuLastKey
            if ($null -eq $action) { $action = "enter" }

            Write-Host ""
            if ($action -eq "c") {
                Write-Host "  Opening $($proj.Label) in VS Code..." -ForegroundColor Green
                Start-Process code -ArgumentList "`"$($proj.Path)`"" -ErrorAction SilentlyContinue
            }
            elseif ($action -eq "i") {
                [ProfileNavigator]::LaunchTerminalIde($proj.Path)
            }
            elseif ($action -eq "d") {
                Write-Host "  Starting Docker Compose in $($proj.Label)..." -ForegroundColor Green
                Start-Process pwsh -ArgumentList "-NoExit", "-Command", "Set-Location `"$($proj.Path)`"; docker-compose up" -ErrorAction SilentlyContinue
            }
            elseif ($action -eq "t") {
                Write-Host "  Running tests in $($proj.Label)..." -ForegroundColor Green
                Start-Process pwsh -ArgumentList "-NoExit", "-Command", "Set-Location `"$($proj.Path)`"; if (Test-Path .\run_tests.ps1) { .\run_tests.ps1 } else { dotnet test }" -ErrorAction SilentlyContinue
            }
            else {
                Write-Host "  Navigating to: $($proj.Label)" -ForegroundColor Green
                Set-Location $proj.Path
            }
        } else {
            Write-Host ""
            Write-Host "  Cancelled." -ForegroundColor DarkGray
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

        if ($microCmd) {
            $microConfigDir = Join-Path $env:USERPROFILE ".config\micro"
            if (-not (Test-Path $microConfigDir)) {
                $null = New-Item -ItemType Directory -Path $microConfigDir -Force
            }
            
            # Install filemanager plugin if not present
            $pluginDir = Join-Path $microConfigDir "plug/filemanager"
            if (-not (Test-Path $pluginDir)) {
                Write-Host "  Installing Micro filemanager plugin..." -ForegroundColor Green
                Start-Process micro -ArgumentList "-plugin install filemanager" -NoNewWindow -Wait
            }

            # Map Ctrl+B to toggle filemanager in bindings.json
            $bindingsFile = Join-Path $microConfigDir "bindings.json"
            $hasBinding = $false
            if (Test-Path $bindingsFile) {
                try {
                    $bindings = Get-Content $bindingsFile -Raw | ConvertFrom-Json
                    if ($bindings -and $bindings.PSObject.Properties.Name -contains "Ctrl-b") {
                        $hasBinding = $true
                    }
                } catch {}
            }
            if (-not $hasBinding) {
                $bindingObj = @{ "Ctrl-b" = "command-action:filemanager" }
                $bindingObj | ConvertTo-Json | Set-Content $bindingsFile -Force
            }

            Write-Host "  Launching Micro IDE with sidebar (Ctrl-B to toggle)..." -ForegroundColor Green
            Start-Process micro -ArgumentList "`"$projectPath`"" -NoNewWindow -Wait
        }
        elseif ($nvimCmd) {
            Write-Host "  Launching NeoVim..." -ForegroundColor Green
            Start-Process nvim -ArgumentList "`"$projectPath`"" -NoNewWindow -Wait
        }
        else {
            Write-Warning "No Terminal IDE is available. Falling back to Set-Location."
            Set-Location $projectPath
        }
    }
}
#endregion



