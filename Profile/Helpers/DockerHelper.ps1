#region DOCKER HELPER
# ==============================================================================
#  Shortcuts and prune utility wrappers for Docker and Docker Compose.
# ==============================================================================

class DockerHelper {
    static [void] GetContainers([bool]$All) {
        Write-Host "[Docker] Containers:" -ForegroundColor Blue
        if ($All) { docker container ls -a | Out-Default } else { docker container ls | Out-Default }
    }

    static [void] RemoveAllContainers() {
        Write-Host "[Prune] Removing ALL containers..." -ForegroundColor Red
        if ((Read-Host "This will remove ALL containers. Are you sure? (y/N)") -eq 'y') {
            $c = docker ps -aq
            if ($c) { docker rm $c | Out-Default }
            Write-Host "All containers removed."
        } else {
            Write-Host "Cancelled."
        }
    }

    static [void] StopAllContainers() {
        Write-Host "[Stop] Stopping ALL running containers..." -ForegroundColor Yellow
        $c = docker ps -q
        if ($c) { docker stop $c | Out-Default }
        Write-Host "All containers stopped."
    }

    static [void] ComposeUp([string[]]$PassThruArgs) {
        Write-Host "[Compose] Starting Docker Compose..." -ForegroundColor Green
        if ($PassThruArgs) { docker-compose up $PassThruArgs | Out-Default } else { docker-compose up | Out-Default }
    }

    static [void] ComposeUpBuild([string[]]$PassThruArgs) {
        Write-Host "[Compose] Building and starting Docker Compose... " -ForegroundColor Blue
        if ($PassThruArgs) { docker-compose up --build $PassThruArgs | Out-Default } else { docker-compose up --build | Out-Default }
    }

    static [void] ComposeDown([string[]]$PassThruArgs) {
        Write-Host "[Compose] Stopping Docker Compose..." -ForegroundColor Yellow
        if ($PassThruArgs) { docker-compose down $PassThruArgs | Out-Default } else { docker-compose down | Out-Default }
    }

    static [void] RemoveUnusedVolumes() {
        Write-Host "[Prune] Pruning Docker volumes..." -ForegroundColor Magenta
        docker volume prune | Out-Default
    }

    static [void] RemoveUnusedImages() {
        Write-Host "[Prune] Pruning Docker images..." -ForegroundColor Magenta
        docker image prune | Out-Default
    }

    static [void] Dkcl() {
        $containers = @()
        try {
            $open = "{" + "{"
            $close = "}" + "}"
            $fmt = "$open.Names$close:::$open.State$close:::$open.Image$close:::$open.Label 'com.docker.compose.project'$close"
            $raw = docker ps -a --format $fmt 2>$null
            if ($null -ne $raw) {
                foreach ($line in $raw) {
                    if ([string]::IsNullOrWhiteSpace($line)) { continue }
                    $parts = $line -split ':::'
                    $proj = "(Standalone)"
                    if ($parts[3]) { $proj = $parts[3] }
                    $containers += [PSCustomObject]@{
                        Name = $parts[0]
                        State = $parts[1]
                        Image = $parts[2]
                        Project = $proj
                    }
                }
            }
        } catch {}

        if ($Global:AiMode) {
            foreach ($c in $containers) {
                Write-Host "$($c.Name),$($c.State),$($c.Image)"
            }
            return
        }

        if ($containers.Count -eq 0) {
            Write-Host "No Docker containers found." -ForegroundColor Yellow
            return
        }

        while ($true) {
            $containers = [LogHelper]::InvokeWithSpinner("[Docker] Querying active container configurations...", {
                $cList = @()
                try {
                    $open = "{" + "{"
                    $close = "}" + "}"
                    $fmt = "$open.Names$close:::$open.State$close:::$open.Image$close:::$open.Label 'com.docker.compose.project'$close"
                    $raw = docker ps -a --format $fmt 2>$null
                    if ($null -ne $raw) {
                        foreach ($line in $raw) {
                            if ([string]::IsNullOrWhiteSpace($line)) { continue }
                            $parts = $line -split ':::'
                            $proj = "(Standalone)"
                            if ($parts[3]) { $proj = $parts[3] }
                            $cList += [PSCustomObject]@{
                                Name = $parts[0]
                                State = $parts[1]
                                Image = $parts[2]
                                Project = $proj
                            }
                        }
                    }
                } catch {}
                return $cList
            })

            $grouped = $containers | Group-Object Project
            $menuItems = @()
            $itemMapping = @()
            foreach ($group in $grouped) {
                $menuItems += "[$($group.Name)]"
                $itemMapping += $null
                foreach ($c in $group.Group) {
                    $statusIcon = "[-]"
                    if ($c.State -eq "running") { $statusIcon = "[+]" }
                    $menuItems += "  $statusIcon $($c.Name) ($($c.State)) - $($c.Image)"
                    $itemMapping += $c
                }
            }
            $menuItems += "[x] Exit Dashboard"
            $itemMapping += $null

            $selected = ([type]"TerminalMenu")::Show("Docker Containers Dashboard (dkcl)", $menuItems, 0)
            if ($selected -lt 0 -or $selected -eq ($menuItems.Count - 1)) {
                break
            }

            $c = $itemMapping[$selected]
            if ($null -eq $c) {
                continue
            }

            $subItems = @(
                "[Start] Start Container",
                "[Stop] Stop Container",
                "[Restart] Restart Container",
                "[Logs] View Logs (tail 50)",
                "[Back] Return"
            )
            $subSel = ([type]"TerminalMenu")::Show("Manage Container: $($c.Name)", $subItems, 0)
            if ($subSel -eq 0) {
                Write-Host "Starting $($c.Name)..." -ForegroundColor Green
                docker start $c.Name | Out-Null
            }
            elseif ($subSel -eq 1) {
                Write-Host "Stopping $($c.Name)..." -ForegroundColor Yellow
                docker stop $c.Name | Out-Null
            }
            elseif ($subSel -eq 2) {
                Write-Host "Restarting $($c.Name)..." -ForegroundColor Cyan
                docker restart $c.Name | Out-Null
            }
            elseif ($subSel -eq 3) {
                Write-Host "Fetching logs for $($c.Name)..." -ForegroundColor Blue
                $logs = docker logs --tail 50 $c.Name 2>&1
                ([type]"TerminalMenu")::ShowScrollableContent("Logs: $($c.Name)", $logs)
            }
        }
    }
}
#endregion
