#region LOG HELPER
# ==============================================================================
#  Keyword-highlighted log streamer and follower.
# ==============================================================================

class LogHelper {
    static [void] StreamLogs([string]$LogPath) {
        if ([string]::IsNullOrWhiteSpace($LogPath)) {
            $logFiles = Get-ChildItem -Filter "*.log" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending
            if ($logFiles.Count -gt 0) {
                $LogPath = $logFiles[0].FullName
            } else {
                $tempLogs = Get-ChildItem -Path $env:TEMP -Filter "*.log" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending
                if ($tempLogs.Count -gt 0) {
                    $LogPath = $tempLogs[0].FullName
                }
            }
        }

        if (-not $LogPath -or -not (Test-Path $LogPath)) {
            Write-Error "No log files found to stream."
            return
        }

        Write-Host "Streaming logs from: $LogPath" -ForegroundColor Cyan

        if ($Global:AiMode) {
            $last20 = Get-Content -Path $LogPath -Tail 20 -ErrorAction SilentlyContinue
            if ($last20) {
                foreach ($line in $last20) {
                    Write-Host $line
                }
            }
            return
        }

        Write-Host "Press Ctrl+C to stop streaming." -ForegroundColor Gray
        try {
            Get-Content -Path $LogPath -Tail 50 -Wait -ErrorAction SilentlyContinue | ForEach-Object {
                $line = $_
                if ($line -match 'error|fail|exception|err\b|critical') {
                    Write-Host $line -ForegroundColor Red
                }
                elseif ($line -match 'warn|warning') {
                    Write-Host $line -ForegroundColor Yellow
                }
                elseif ($line -match 'success|ok\b|complete|done') {
                    Write-Host $line -ForegroundColor Green
                }
                else {
                    Write-Host $line
                }
            }
        } catch {}
    }

    static [object] InvokeWithSpinner([string]$Message, [scriptblock]$Action) {
        if ($Global:AiMode -or [Console]::IsOutputRedirected -or (Get-Command Describe -ErrorAction SilentlyContinue)) {
            return &$Action
        }

        $frames = @('⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏')
        
        $iss = [system.management.automation.runspaces.initialsessionstate]::CreateDefault()
        $powershell = [powershell]::Create($iss)
        $null = $powershell.AddScript($Action)
        
        try {
            $asyncResult = $powershell.BeginInvoke()
        } catch {
            Write-Host "$Message... " -NoNewline
            $res = &$Action
            Write-Host "[OK]" -ForegroundColor Green
            return $res
        }

        $i = 0
        Write-Host "$Message " -NoNewline
        
        $startPos = 0
        $startTop = 0
        $canSetCursor = $false
        try {
            $startPos = [Console]::CursorLeft
            $startTop = [Console]::CursorTop
            if ($startPos -ge 0 -and $startTop -ge 0) {
                $canSetCursor = $true
            }
        } catch {}

        while (-not $asyncResult.IsCompleted) {
            try {
                if ($canSetCursor) {
                    [Console]::SetCursorPosition($startPos, $startTop)
                    Write-Host $frames[$i] -NoNewline -ForegroundColor Cyan
                } else {
                    Write-Host "." -NoNewline
                }
            } catch {}
            $i = ($i + 1) % $frames.Count
            Start-Sleep -Milliseconds 100
        }

        $result = $null
        try {
            $result = $powershell.EndInvoke($asyncResult)
            try {
                if ($canSetCursor) {
                    [Console]::SetCursorPosition($startPos, $startTop)
                    Write-Host "[OK] " -ForegroundColor Green
                    Write-Host ""
                } else {
                    Write-Host " [OK]" -ForegroundColor Green
                }
            } catch {
                Write-Host " [OK]" -ForegroundColor Green
            }
        } catch {
            try {
                if ($canSetCursor) {
                    [Console]::SetCursorPosition($startPos, $startTop)
                    Write-Host "[ERR]" -ForegroundColor Red
                    Write-Host ""
                } else {
                    Write-Host " [ERR]" -ForegroundColor Red
                }
            } catch {
                Write-Host " [ERR]" -ForegroundColor Red
            }
            throw $_
        } finally {
            $powershell.Dispose()
        }

        return $result
    }

    static [string] GetLogFilePath() {
        $logDir = Join-Path $env:USERPROFILE ".gemini\antigravity"
        if (-not (Test-Path $logDir)) {
            $null = New-Item -ItemType Directory -Path $logDir -Force -ErrorAction SilentlyContinue
        }
        return Join-Path $logDir "profile.log"
    }

    static [void] Log([string]$Message, [string]$Level = "INFO") {
        try {
            $logPath = [LogHelper]::GetLogFilePath()
            $timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
            $logEntry = "[$timestamp] [$Level] $Message"
            Add-Content -Path $logPath -Value $logEntry -ErrorAction SilentlyContinue
        } catch {}
    }

    static [void] LogError([string]$Message, [Exception]$Exception = $null) {
        $errStr = if ($Exception) { "$Message - Exception: $($Exception.Message)`n$($Exception.StackTrace)" } else { $Message }
        [LogHelper]::Log($errStr, "ERROR")
    }

    static [void] LogWarning([string]$Message) {
        [LogHelper]::Log($Message, "WARNING")
    }
}
#endregion
