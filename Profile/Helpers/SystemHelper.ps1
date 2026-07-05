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
}
#endregion
