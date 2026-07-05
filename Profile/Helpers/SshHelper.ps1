#region SSH HELPER
# ==============================================================================
#  SSH connection management, status detection, and authorization keys manager.
# ==============================================================================

class SshHelper {
    static [void] GetConnectionInfo() {
        $ipInfo = $null
        $displayIp = $null
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

    static [void] AddAuthorizedKey([string]$Key, [string]$Account) {
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

        if (-not (Test-Path $authFile)) {
            $null = New-Item -ItemType File -Path $authFile -Force
            Write-Host "📄 Created file: $authFile" -ForegroundColor Cyan
        }

        # Check if key already exists
        $existingKeys = Get-Content $authFile -ErrorAction SilentlyContinue
        if ($existingKeys -contains $Key) {
            Write-Host "ℹ️ SSH Key is already authorized." -ForegroundColor Yellow
            return
        }

        # Append key
        Add-Content -Path $authFile -Value $Key -Force
        Write-Host "✅ SSH key successfully authorized for user '$targetUser'." -ForegroundColor Green

        # Fix NTFS permissions
        Write-Host "🔒 Setting secure permissions on SSH files..." -ForegroundColor Cyan

        # Disable inheritance
        $aclDir = Get-Acl $sshDir
        $aclDir.SetAccessRuleProtection($true, $false)
        Set-Acl $sshDir $aclDir

        $aclFile = Get-Acl $authFile
        $aclFile.SetAccessRuleProtection($true, $false)
        Set-Acl $authFile $aclFile

        # Clear existing rules and assign only to target user and SYSTEM
        $rulesDir = $aclDir.Access
        foreach ($rule in $rulesDir) { $aclDir.RemoveAccessRule($rule) | Out-Null }
        $rulesFile = $aclFile.Access
        foreach ($rule in $rulesFile) { $aclFile.RemoveAccessRule($rule) | Out-Null }

        $systemUser = "NT AUTHORITY\SYSTEM"
        $targetIdentity = "$env:COMPUTERNAME\$targetUser"

        $fullControl = [System.Security.AccessControl.FileSystemRights]::FullControl
        $readAndExecute = [System.Security.AccessControl.FileSystemRights]::ReadAndExecute
        $allow = [System.Security.AccessControl.AccessControlType]::Allow

        # Dir rules
        $ruleUserDir = [System.Security.AccessControl.FileSystemAccessRule]::new($targetIdentity, $fullControl, [System.Security.AccessControl.InheritanceFlags]::None, [System.Security.AccessControl.PropagationFlags]::None, $allow)
        $ruleSysDir = [System.Security.AccessControl.FileSystemAccessRule]::new($systemUser, $fullControl, [System.Security.AccessControl.InheritanceFlags]::None, [System.Security.AccessControl.PropagationFlags]::None, $allow)
        $aclDir.AddAccessRule($ruleUserDir)
        $aclDir.AddAccessRule($ruleSysDir)
        Set-Acl $sshDir $aclDir

        # File rules
        $ruleUserFile = [System.Security.AccessControl.FileSystemAccessRule]::new($targetIdentity, $fullControl, [System.Security.AccessControl.InheritanceFlags]::None, [System.Security.AccessControl.PropagationFlags]::None, $allow)
        $ruleSysFile = [System.Security.AccessControl.FileSystemAccessRule]::new($systemUser, $fullControl, [System.Security.AccessControl.InheritanceFlags]::None, [System.Security.AccessControl.PropagationFlags]::None, $allow)
        $aclFile.AddAccessRule($ruleUserFile)
        $aclFile.AddAccessRule($ruleSysFile)
        Set-Acl $authFile $aclFile

        Write-Host "✅ Secure OpenSSH file permissions applied." -ForegroundColor Green
    }
}
#endregion
