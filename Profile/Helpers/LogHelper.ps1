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
}
#endregion
