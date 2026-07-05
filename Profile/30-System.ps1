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

<#
.SYNOPSIS
Displays active SSH connections, Tailscale IPs, and mobile terminal connection guides.
.EXAMPLE
  Get-SshConnectionInfo
.CATEGORY
  System & Utility Commands
#>
function Get-SshConnectionInfo {
    [CmdletBinding()]
    param()

    Write-Host "🌐 Network Connection Status" -ForegroundColor Cyan
    Write-Host "===========================" -ForegroundColor Cyan
    
    # Get Tailscale status and IPs
    $tsStatus = Get-Command "tailscale" -ErrorAction SilentlyContinue
    if ($tsStatus) {
        $ipInfo = (tailscale ip -4 2>$null)
        if ($ipInfo) {
            Write-Host "  Tailscale IPv4 Address: $ipInfo" -ForegroundColor Green
        } else {
            Write-Host "  [WARN] Tailscale is installed but may not be logged in or connected." -ForegroundColor Yellow
        }
    } else {
        Write-Host "  Tailscale is not installed on this machine." -ForegroundColor DarkGray
    }

    # Get local network IPs
    $ips = Get-NetIPAddress -AddressFamily IPv4 -InterfaceAlias "Wi-Fi", "Ethernet" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty IPAddress
    if ($ips) {
        Write-Host "  Local IPv4 Address(es): $($ips -join ', ')" -ForegroundColor Cyan
    }

    Write-Host "`n🔒 Active SSH Sessions" -ForegroundColor Cyan
    Write-Host "====================" -ForegroundColor Cyan
    
    $sshConns = Get-NetTCPConnection -LocalPort 22 -State Established -ErrorAction SilentlyContinue
    if ($sshConns) {
        foreach ($conn in $sshConns) {
            $proc = Get-Process -Id $conn.OwningProcess -ErrorAction SilentlyContinue
            Write-Host "  Established connection from $($conn.RemoteAddress):$($conn.RemotePort) (Process: $($proc.Name), PID: $($conn.OwningProcess))" -ForegroundColor Green
        }
    } else {
        Write-Host "  No active SSH connections on port 22." -ForegroundColor DarkGray
    }

    Write-Host "`n📱 Phone to PC Control Quick Guide" -ForegroundColor Cyan
    Write-Host "================================" -ForegroundColor Cyan
    Write-Host "  1. On your phone (Termux), run: ssh sshuser@<IP>" -ForegroundColor Gray
    $displayIp = '100.x.y.z'
    if ($ipInfo) { $displayIp = $ipInfo }
    Write-Host "  2. Use your Tailscale IP ($displayIp) for secure access anywhere." -ForegroundColor Gray
    Write-Host "  3. To authorize a passwordless login key, run: Add-SshAuthorizedKey -Key 'ssh-ed25519 ...'" -ForegroundColor Gray
}

<#
.SYNOPSIS
Adds a public SSH key to the current user's or sshuser's authorized_keys file and sets appropriate security permissions.
.PARAMETER Key
The public SSH key string to authorize.
.PARAMETER Account
Target account name (defaults to current user).
.EXAMPLE
  Add-SshAuthorizedKey -Key "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5..."
.CATEGORY
  System & Utility Commands
#>
function Add-SshAuthorizedKey {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, Position=0)]
        [string]$Key,

        [Parameter(Position=1)]
        [string]$Account
    )

    $targetUser = $env:USERNAME
    if ($Account) { $targetUser = $Account }

    $userHome = $env:USERPROFILE
    if ($targetUser -ne $env:USERNAME) {
        $userHome = Join-Path (Split-Path $env:USERPROFILE -Parent) $targetUser
    }

    if (-not (Test-Path $userHome)) {
        Write-Error "Home directory for user '$targetUser' not found at $userHome."
        return
    }

    $sshDir = Join-Path $userHome ".ssh"
    $authFile = Join-Path $sshDir "authorized_keys"

    if (-not (Test-Path $sshDir)) {
        $null = New-Item -ItemType Directory -Path $sshDir -Force
        Write-Host "📂 Created directory: $sshDir" -ForegroundColor Cyan
    }

    # Append the key
    Add-Content -Path $authFile -Value "`r`n$Key" -Force
    Write-Host "🟢 Key successfully authorized for '$targetUser' in $authFile" -ForegroundColor Green

    # Set NTFS security permissions on the authorized_keys file (OpenSSH requirement)
    try {
        $acl = Get-Acl $authFile
        $acl.SetAccessRuleProtection($true, $false) # Disable inheritance, remove inherited rules
        
        # Grant Full Control to target user and SYSTEM
        $userRule = [System.Security.AccessControl.FileSystemAccessRule]::new($targetUser, "FullControl", "Allow")
        $systemRule = [System.Security.AccessControl.FileSystemAccessRule]::new("SYSTEM", "FullControl", "Allow")
        
        $acl.AddAccessRule($userRule)
        $acl.AddAccessRule($systemRule)
        Set-Acl $authFile $acl
        Write-Host "🔒 Set strict NTFS permissions on authorized_keys file." -ForegroundColor Green
    } catch {
        Write-Warning "Could not automatically set NTFS permissions. OpenSSH may reject connection if permissions are too open."
    }
}

#endregion