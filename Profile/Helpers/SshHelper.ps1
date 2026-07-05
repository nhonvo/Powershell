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


    static [void] StartMobileSshKeyReceiver() {
        # Check active network IPs
        $tsIP = $null
        $tsStatus = Get-Command "tailscale" -ErrorAction SilentlyContinue
        if ($tsStatus) { $tsIP = (tailscale ip -4 2>$null) }
        
        $localIPs = Get-NetIPAddress -AddressFamily IPv4 -InterfaceAlias "Wi-Fi", "Ethernet" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty IPAddress
        $displayIP = if ($tsIP) { $tsIP } elseif ($localIPs) { $localIPs[0] } else { "localhost" }
        
        $port = 8999
        $listener = [System.Net.HttpListener]::new()
        $listener.Prefixes.Add("http://*:$port/")
        
        Write-Host ""
        Write-Host "📱 Mobile SSH Key Authorizer" -ForegroundColor Cyan
        Write-Host "=============================" -ForegroundColor Cyan
        Write-Host "Starting temporary local server to receive your public SSH key..." -ForegroundColor Gray
        Write-Host ""
        Write-Host "👉 Link to open in your phone's browser:" -ForegroundColor Cyan
        Write-Host "   http://${displayIP}:${port}/" -ForegroundColor Green
        Write-Host "  (or http://localhost:${port}/ if local)" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Waiting for connection... (Timeout in 2 minutes. Press Ctrl+C to cancel)" -ForegroundColor Yellow
        Write-Host ""

        try {
            $listener.Start()
        } catch {
            Write-Error "Failed to start HTTP listener: $_. Make sure port $port is not in use and you have administrator permissions."
            return
        }

        $timeoutSeconds = 120
        $startTime = [DateTime]::Now
        $success = $false
        
        try {
            while ($true) {
                # Check for timeout
                $elapsed = ([DateTime]::Now - $startTime).TotalSeconds
                if ($elapsed -ge $timeoutSeconds) {
                    Write-Host "⏱️ Session timed out after 2 minutes. Stopping listener..." -ForegroundColor Red
                    break
                }
                
                # Check for context asynchronously to prevent locking the loop (so we can check timeout)
                $asyncResult = $listener.BeginGetContext($null, $null)
                while (-not $asyncResult.IsCompleted) {
                    Start-Sleep -Milliseconds 200
                    $elapsed = ([DateTime]::Now - $startTime).TotalSeconds
                    if ($elapsed -ge $timeoutSeconds) {
                        break
                    }
                }
                
                if ($elapsed -ge $timeoutSeconds) {
                    break
                }
                
                $context = $listener.EndGetContext($asyncResult)
                $request = $context.Request
                $response = $context.Response
                
                # Handle GET request (serve the HTML form)
                if ($request.HttpMethod -eq "GET") {
                    $html = @"
<!DOCTYPE html>
<html>
<head>
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>PowerShell Mobile SSH Key Authorizer</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
            background-color: #0f141c;
            color: #abb2bf;
            margin: 0;
            padding: 20px;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 90vh;
        }
        .container {
            background-color: #161b22;
            border-radius: 12px;
            padding: 24px;
            max-width: 500px;
            width: 100%;
            box-shadow: 0 4px 12px rgba(0,0,0,0.3);
            border: 1px solid #30363d;
        }
        h2 { color: #56b6c2; margin-top: 0; font-size: 1.5rem; text-align: center; }
        p { font-size: 0.95rem; line-height: 1.5; color: #8b949e; }
        textarea {
            width: 100%;
            height: 120px;
            box-sizing: border-box;
            background-color: #0d1117;
            color: #c9d1d9;
            border: 1px solid #30363d;
            border-radius: 6px;
            padding: 10px;
            font-family: monospace;
            font-size: 0.85rem;
            resize: vertical;
            margin-top: 10px;
            margin-bottom: 20px;
        }
        textarea:focus {
            outline: none;
            border-color: #58a6ff;
        }
        button {
            width: 100%;
            background-color: #238636;
            color: white;
            border: none;
            border-radius: 6px;
            padding: 12px;
            font-size: 1rem;
            font-weight: bold;
            cursor: pointer;
            transition: background-color 0.2s;
        }
        button:hover { background-color: #2ea043; }
        .footer {
            margin-top: 24px;
            text-align: center;
            font-size: 0.8rem;
            color: #484f58;
        }
    </style>
</head>
<body>
    <div class="container">
        <h2>📱 Add SSH Public Key</h2>
        <p>Paste the public SSH key from your mobile phone (e.g. from Termux's <code>~/.ssh/id_ed25519.pub</code>) to authorize connection.</p>
        <form method="POST">
            <label for="key" style="font-weight: bold; font-size: 0.9rem;">Public SSH Key:</label>
            <textarea name="key" id="key" placeholder="ssh-ed25519 AAAAC3NzaC1lZDI1NTE5..." required></textarea>
            <button type="submit">Authorize Key</button>
        </form>
        <div class="footer">Temporary local server will stop immediately after submission.</div>
    </div>
</body>
</html>
"@
                    $buffer = [System.Text.Encoding]::UTF8.GetBytes($html)
                    $response.ContentLength64 = $buffer.Length
                    $response.ContentType = "text/html"
                    $response.OutputStream.Write($buffer, 0, $buffer.Length)
                    $response.OutputStream.Close()
                }
                # Handle POST request (receive public key)
                elseif ($request.HttpMethod -eq "POST") {
                    $reader = [System.IO.StreamReader]::new($request.InputStream, [System.Text.Encoding]::UTF8)
                    $body = $reader.ReadToEnd()
                    $reader.Close()
                    
                    # Parse URL-encoded body
                    $decoded = [System.Net.WebUtility]::UrlDecode($body)
                    $sshKey = $decoded
                    if ($decoded.StartsWith("key=")) {
                        $sshKey = $decoded.Substring(4)
                    }
                    $sshKey = $sshKey.Trim()
                    
                    # Basic validation
                    $isValid = $false
                    if ($sshKey -match '^ssh-(ed25519|rsa|dss|ecdsa) [A-Za-z0-9+/=]+( .+)?$') {
                        $isValid = $true
                    }
                    
                    $resultHtml = ""
                    if ($isValid) {
                        # Authorize the key using the existing static method
                        [SshHelper]::AddAuthorizedKey($sshKey, $null)
                        $success = $true
                        
                        $resultHtml = @"
<!DOCTYPE html>
<html>
<head>
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Success</title>
    <style>
        body { font-family: sans-serif; background-color: #0f141c; color: #abb2bf; text-align: center; padding: 50px 20px; }
        .card { background-color: #161b22; border-radius: 12px; padding: 30px; max-width: 450px; margin: 0 auto; border: 1px solid #30363d; }
        h2 { color: #2ea043; }
    </style>
</head>
<body>
    <div class="card">
        <h2>✅ Success!</h2>
        <p>The SSH key has been successfully added to authorized_keys and NTFS file permissions have been secured.</p>
        <p>You can close this window now.</p>
    </div>
</body>
</html>
"@
                    } else {
                        $resultHtml = @"
<!DOCTYPE html>
<html>
<head>
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Invalid Key</title>
    <style>
        body { font-family: sans-serif; background-color: #0f141c; color: #abb2bf; text-align: center; padding: 50px 20px; }
        .card { background-color: #161b22; border-radius: 12px; padding: 30px; max-width: 450px; margin: 0 auto; border: 1px solid #f85149; }
        h2 { color: #f85149; }
        a { color: #58a6ff; text-decoration: none; }
    </style>
</head>
<body>
    <div class="card">
        <h2>❌ Invalid SSH Key Format</h2>
        <p>The key provided does not match a valid public SSH key format (e.g. ssh-ed25519 or ssh-rsa).</p>
        <p><a href="/">Go back and try again</a></p>
    </div>
</body>
</html>
"@
                    }
                    
                    $buffer = [System.Text.Encoding]::UTF8.GetBytes($resultHtml)
                    $response.ContentLength64 = $buffer.Length
                    $response.ContentType = "text/html"
                    $response.OutputStream.Write($buffer, 0, $buffer.Length)
                    $response.OutputStream.Close()
                    
                    if ($success) {
                        # Break loop to stop listener after serving success page
                        break
                    }
                }
            }
        } finally {
            $listener.Stop()
            $listener.Close()
            Write-Host "🛑 Mobile Key Authorizer server stopped." -ForegroundColor DarkGray
        }
    }
}
#endregion
