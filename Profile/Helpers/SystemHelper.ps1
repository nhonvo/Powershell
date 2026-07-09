#region SYSTEM HELPER
# ==============================================================================
#  Utility functions for system, disk, and process management.
# ==============================================================================

class SystemHelper {
    static [object[]] GetDiskSpace() {
        return @(Get-PSDrive -PSProvider FileSystem | Select-Object Name,
            @{N='Used(GB)';E={"{0:N2}" -f ($_.Used/1GB)}},
            @{N='Free(GB)';E={"{0:N2}" -f ($_.Free/1GB)}},
            @{N='Total(GB)';E={"{0:N2}" -f (($_.Used + $_.Free)/1GB)}})
    }

    static [void] StopProcessFriendly([string]$Name) {
        if ($Name) {
            Stop-Process -Name $Name -Force
        } else {
            Get-Process | Out-GridView -Title "Select Process to Kill" -PassThru | Stop-Process -Force
        }
    }

    static [void] OpenExplorer() {
        Invoke-Item .
    }

    static [string] GetPublicIP() {
        try {
            $webClient = [System.Net.WebClient]::new()
            return $webClient.DownloadString("https://api.ipify.org").Trim()
        } catch {
            return "Unable to resolve public IP."
        }
    }

    static [void] ClearHistory() {
        Clear-Host
        Remove-Item (Get-PSReadlineOption).HistorySavePath -ErrorAction SilentlyContinue
        $prType = [Type]"Microsoft.PowerShell.PSConsoleReadLine"
        if ($prType) { $prType::ClearHistory() }
        Clear-History
        Write-Host "🧹 All command history has been cleared." -ForegroundColor Yellow
    }

    static [void] KillPort([int]$Port) {
        $connections = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
        if (-not $connections) {
            Write-Host "No process found listening on port $Port." -ForegroundColor Yellow
            return
        }
        foreach ($conn in $connections) {
            $owningPid = $conn.OwningProcess
            try {
                $proc = Get-Process -Id $owningPid -ErrorAction SilentlyContinue
                if ($proc) {
                    Write-Host "Killing process '$($proc.Name)' (PID $owningPid) listening on port $Port..." -ForegroundColor Red
                    Stop-Process -Id $owningPid -Force
                }
            } catch {
                Write-Host "Failed to kill process PID $($owningPid): $_" -ForegroundColor Red
            }
        }
    }

    static [void] SystemMonitor() {
        # Check network & metrics first
        $cpu = 0.0
        try {
            $cpuSample = Get-Counter '\Processor(_Total)\% Processor Time' -ErrorAction SilentlyContinue
            if ($cpuSample) { $cpu = [Math]::Round($cpuSample.CounterSamples[0].CookedValue, 1) }
        } catch {}

        $os = Get-CimInstance Win32_OperatingSystem -ErrorAction SilentlyContinue
        $totalRam = 1.0
        $freeRam = 1.0
        if ($os) {
            $totalRam = $os.TotalVisibleMemorySize
            $freeRam = $os.FreePhysicalMemory
        }
        $ramPercent = [Math]::Round((($totalRam - $freeRam) / $totalRam) * 100, 1)

        $diskPercent = 0.0
        try {
            $diskSample = Get-Counter '\PhysicalDisk(_Total)\% Disk Time' -ErrorAction SilentlyContinue
            if ($diskSample) { $diskPercent = [Math]::Round([Math]::Min(100.0, $diskSample.CounterSamples[0].CookedValue), 1) }
        } catch {}

        if ($Global:AiMode) {
            Write-Host "$cpu%,$ramPercent%,$diskPercent%"
            return
        }

        Write-Host "Press Escape to exit System Monitor..." -ForegroundColor Gray
        $charFilled = [char]0x2588
        $charEmpty = [char]0x2591
        
        $startRow = [Console]::CursorTop
        $startCol = 0
        
        try {
            while ($true) {
                # Get metrics
                $cpu = 0.0
                try {
                    $cpuSample = Get-Counter '\Processor(_Total)\% Processor Time' -ErrorAction SilentlyContinue
                    if ($cpuSample) { $cpu = [Math]::Round($cpuSample.CounterSamples[0].CounterSamples[0].CookedValue, 1) }
                    if (-not $cpu -and $cpuSample) { $cpu = [Math]::Round($cpuSample.CounterSamples[0].CookedValue, 1) }
                } catch {}

                $os = Get-CimInstance Win32_OperatingSystem -ErrorAction SilentlyContinue
                if ($os) {
                    $totalRam = $os.TotalVisibleMemorySize
                    $freeRam = $os.FreePhysicalMemory
                }
                $ramPercent = [Math]::Round((($totalRam - $freeRam) / $totalRam) * 100, 1)
                $usedRamGB = [Math]::Round(($totalRam - $freeRam) / 1024 / 1024, 2)
                $totalRamGB = [Math]::Round($totalRam / 1024 / 1024, 2)

                $diskPercent = 0.0
                try {
                    $diskSample = Get-Counter '\PhysicalDisk(_Total)\% Disk Time' -ErrorAction SilentlyContinue
                    if ($diskSample) { $diskPercent = [Math]::Round([Math]::Min(100.0, $diskSample.CounterSamples[0].CookedValue), 1) }
                } catch {}

                # Create Gauges (length 20)
                $cpuFilled = [int][Math]::Round(($cpu / 100.0) * 20)
                if ($cpuFilled -gt 20) { $cpuFilled = 20 }
                if ($cpuFilled -lt 0) { $cpuFilled = 0 }
                $cpuBar = ([string]$charFilled * $cpuFilled) + ([string]$charEmpty * (20 - $cpuFilled))

                $ramFilled = [int][Math]::Round(($ramPercent / 100.0) * 20)
                if ($ramFilled -gt 20) { $ramFilled = 20 }
                if ($ramFilled -lt 0) { $ramFilled = 0 }
                $ramBar = ([string]$charFilled * $ramFilled) + ([string]$charEmpty * (20 - $ramFilled))

                $diskFilled = [int][Math]::Round(($diskPercent / 100.0) * 20)
                if ($diskFilled -gt 20) { $diskFilled = 20 }
                if ($diskFilled -lt 0) { $diskFilled = 0 }
                $diskBar = ([string]$charFilled * $diskFilled) + ([string]$charEmpty * (20 - $diskFilled))

                try {
                    [Console]::SetCursorPosition($startCol, $startRow)
                } catch {
                    $startRow = [Console]::CursorTop
                    $startCol = 0
                }
                
                Write-Host "  CPU Usage: [$cpuBar] $cpu%                " -ForegroundColor Cyan
                Write-Host "  RAM Usage: [$ramBar] $ramPercent% ($usedRamGB GB / $totalRamGB GB)     " -ForegroundColor Green
                Write-Host "  Disk I/O:  [$diskBar] $diskPercent%               " -ForegroundColor Yellow

                # Check key available with 2s timeout
                $sleepCycles = 20
                $exit = $false
                for ($s = 0; $s -lt $sleepCycles; $s++) {
                    if ([Console]::KeyAvailable) {
                        $key = [Console]::ReadKey($true)
                        if ($key.Key -eq [ConsoleKey]::Escape -or $key.Key -eq [ConsoleKey]::Enter) {
                            $exit = $true
                            break
                        }
                    }
                    Start-Sleep -Milliseconds 100
                }
                if ($exit) { break }
            }
        } finally {
            try {
                [Console]::SetCursorPosition($startCol, $startRow)
                Write-Host (" " * ([Console]::WindowWidth - 1))
                Write-Host (" " * ([Console]::WindowWidth - 1))
                Write-Host (" " * ([Console]::WindowWidth - 1))
                [Console]::SetCursorPosition($startCol, $startRow)
            } catch {}
        }
    }
}
#endregion



