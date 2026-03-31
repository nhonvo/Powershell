#region SYSTEM & UTILITY COMMANDS
# ------------------------------------------------------------------------------
#  Functions for system interaction, profile management, and fun.
# ------------------------------------------------------------------------------

<#
.SYNOPSIS
Reloads the PowerShell profile.
.CATEGORY
System & Utility Commands
#>
function Reload-Profile {
    [CmdletBinding()]
    param()
    . $PROFILE; Write-Host "✅ Profile reloaded." -ForegroundColor Green
}

<#
.SYNOPSIS
Gets the current public IP address.
.CATEGORY
System & Utility Commands
#>
function Get-PublicIP {
    [CmdletBinding()]
    param()
    try {
        $ip = Invoke-RestMethod -Uri "https://api.ipify.org" -ErrorAction Stop
        Write-Host "🌍 Public IP: $ip" -ForegroundColor Cyan
    } catch {
        Write-Error "Could not get public IP."
    }
}

<#
.SYNOPSIS
Displays disk space usage for all drives.
.CATEGORY
System & Utility Commands
#>
function Get-DiskSpace {
    Get-PSDrive -PSProvider FileSystem | Select-Object Name, @{N='Used(GB)';E={"{0:N2}" -f ($_.Used/1GB)}}, @{N='Free(GB)';E={"{0:N2}" -f ($_.Free/1GB)}}, @{N='Total(GB)';E={"{0:N2}" -f (($_.Used + $_.Free)/1GB)}} | Format-Table -AutoSize
}

<#
.SYNOPSIS
Displays a tree view of the current directory (ignoring git/bin/obj).
.CATEGORY
System & Utility Commands
#>
function Get-FileTree {
    [CmdletBinding()]
    param([int]$Depth = 2)
    tree.com /f /a | Select-Object -First (50 * $Depth)
}

<#
.SYNOPSIS
Interactive process killer.
.CATEGORY
System & Utility Commands
#>
function Stop-ProcessFriendly {
    [CmdletBinding()]
    param([string]$Name)
    if ($Name) {
        Stop-Process -Name $Name -Force
    } else {
        Get-Process | Out-GridView -Title "Select Process to Kill" -PassThru | Stop-Process -Force
    }
}

<#
.SYNOPSIS
Opens the current directory in File Explorer.
.CATEGORY
System & Utility Commands
#>
function Invoke-OpenExplorer {
    [CmdletBinding()]
    param()
    Invoke-Item .
}

<#
.SYNOPSIS
Clears the entire command history, including the saved history file.
.CATEGORY
System & Utility Commands
#>
function Clear-SavedHistory {
    [CmdletBinding(ConfirmImpact = 'High', SupportsShouldProcess)]
    param()

    if ($pscmdlet.ShouldProcess("entire command history, including the saved history file")) {
        Clear-Host
        Remove-Item (Get-PSReadlineOption).HistorySavePath -ErrorAction SilentlyContinue
        [Microsoft.PowerShell.PSConsoleReadLine]::ClearHistory()
        Clear-History
        Write-Host "🧹 All command history has been cleared." -ForegroundColor Yellow
    }
}

#endregion