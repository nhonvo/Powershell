#region ANTIGRAVITY MULTI-ACCOUNT MANAGER
# ==============================================================================
#  Dynamic Multi-Account Manager for Antigravity (agy) CLI.
# ==============================================================================

class AgyAccountManager {
    static [string]$AgySourceHome = "C:\Users\Public\.gemini"
    static [string]$AgyAccountPrefix = "C:\Users\Public\.gemini_"
    static [string]$AgyActiveAccountFile
    static [string]$AgyBinaryPath = "C:\ProgramData\agy\bin\agy.exe"
    static [string]$GlobalBinDir = "C:\ProgramData\agy\bin"

    static AgyAccountManager() {
        [AgyAccountManager]::AgyActiveAccountFile = Join-Path ([AgyAccountManager]::AgySourceHome) "active_account.txt"
        if ($env:PATH -split ';' -notcontains [AgyAccountManager]::GlobalBinDir) {
            $env:PATH = [AgyAccountManager]::GlobalBinDir + ";" + $env:PATH
        }
    }

    static [bool] CheckNetworkStatus() {
        if ($null -ne $Global:ProfileNetworkOnline) {
            return $Global:ProfileNetworkOnline
        }
        try {
            $hasNetwork = [System.Net.NetworkInformation.NetworkInterface]::GetIsNetworkAvailable()
            $online = $hasNetwork -and (Test-Connection -ComputerName 8.8.8.8 -Count 1 -TimeoutMilliSeconds 200 -ErrorAction SilentlyContinue)
            $Global:ProfileNetworkOnline = $online
            return $online
        } catch {
            $Global:ProfileNetworkOnline = $false
            return $false
        }
    }

    static [hashtable] CalculateRollingQuotas([string]$AccountName) {
        $meta = [AgyAccountManager]::GetAccountMetadata($AccountName)
        $history = @()
        if ($meta.RequestHistory) { $history = @($meta.RequestHistory) }
        
        $now = (Get-Date).ToUniversalTime()
        $fiveHoursAgo = $now.AddHours(-5)
        $sevenDaysAgo = $now.AddDays(-7)
        
        $reqs5H = 0
        $reqsWeekly = 0
        
        $dummy = [DateTime]::MinValue
        foreach ($ts in $history) {
            if ($ts -and [DateTime]::TryParse($ts, [ref]$dummy)) {
                $dt = $dummy
                if ($dt -ge $fiveHoursAgo) { $reqs5H++ }
                if ($dt -ge $sevenDaysAgo) { $reqsWeekly++ }
            }
        }
        
        $limit5H = 50
        $limitWeekly = 1000
        
        $remaining5H = [Math]::Max(0.0, 100.0 - [Math]::Round(($reqs5H / $limit5H) * 100.0, 2))
        $remainingWeekly = [Math]::Max(0.0, 100.0 - [Math]::Round(($reqsWeekly / $limitWeekly) * 100.0, 2))
        
        $oldest5H = $now
        $oldestWeekly = $now
        foreach ($ts in $history) {
            if ($ts -and [DateTime]::TryParse($ts, [ref]$dummy)) {
                $dt = $dummy
                if ($dt -ge $fiveHoursAgo -and $dt -lt $oldest5H) { $oldest5H = $dt }
                if ($dt -ge $sevenDaysAgo -and $dt -lt $oldestWeekly) { $oldestWeekly = $dt }
            }
        }
        
        $refresh5H = [Math]::Max(0, [Math]::Round((($oldest5H.AddHours(5) - $now).TotalSeconds), 0))
        $refreshWeekly = [Math]::Max(0, [Math]::Round((($oldestWeekly.AddDays(7) - $now).TotalSeconds), 0))
        
        $h5H = [Math]::Floor($refresh5H / 3600)
        $m5H = [Math]::Floor(($refresh5H % 3600) / 60)
        
        $hW = [Math]::Floor($refreshWeekly / 3600)
        $mW = [Math]::Floor(($refreshWeekly % 3600) / 60)
        
        $time5H = "$($h5H)h $($m5H)m"
        $timeWeekly = "$($hW)h $($mW)m"
        
        return @{
            RemainingWeekly = $remainingWeekly
            Remaining5H = $remaining5H
            TimeWeekly = $timeWeekly
            Time5H = $time5H
            CountWeekly = $reqsWeekly
            Count5H = $reqs5H
        }
    }

    static [void] AutoSwitchOnDirectoryChange([string]$Path) {
        if ($null -eq $Global:ProfileWorkspaces) { return }
        $matchedProject = $Global:ProfileWorkspaces | Where-Object { $_.Path -and $Path -like "$($_.Path)*" } | Select-Object -First 1
        if ($null -ne $matchedProject -and $matchedProject.AssociatedAccount -ne [AgyAccountManager]::GetActiveAccount()) {
            if (-not $Global:AiMode) {
                Write-Host "[Auto-Switch] Changing credentials to: $($matchedProject.AssociatedAccount)" -ForegroundColor Cyan
            }
            [AgyAccountManager]::SetActiveAccount($matchedProject.AssociatedAccount, $true)
        }
    }

    static [hashtable] GetAccountMetadata([string]$AccountName) {
        $accDir = [AgyAccountManager]::GetAccountDirectory($AccountName)
        $metaFile = Join-Path $accDir "account_metadata.json"
        $defaultMeta = @{
            LastUsed = "Never"
            UsageCount = 0
            QuotaStatus = "OK"
            RequestHistory = @()
        }
        if (Test-Path $metaFile) {
            try {
                $raw = Get-Content -Path $metaFile -Raw -ErrorAction SilentlyContinue
                if ($raw) {
                    $json = ConvertFrom-Json $raw
                    if ($json.LastUsed) { $defaultMeta.LastUsed = $json.LastUsed }
                    if ($json.UsageCount) { $defaultMeta.UsageCount = $json.UsageCount }
                    if ($json.QuotaStatus) { $defaultMeta.QuotaStatus = $json.QuotaStatus }
                    if ($json.RequestHistory) {
                        $defaultMeta.RequestHistory = @($json.RequestHistory)
                    }
                }
            } catch {}
        }
        return $defaultMeta
    }

    static [void] UpdateAccountMetadata([string]$AccountName) {
        $accDir = [AgyAccountManager]::GetAccountDirectory($AccountName)
        if (-not (Test-Path $accDir)) {
            try { $null = New-Item -ItemType Directory -Path $accDir -Force } catch {}
        }
        if (Test-Path $accDir) {
            $metaFile = Join-Path $accDir "account_metadata.json"
            $meta = [AgyAccountManager]::GetAccountMetadata($AccountName)
            $meta.LastUsed = (Get-Date).ToString("yyyy-MM-ddTHH:mm:sszzz")
            $meta.UsageCount = [int]($meta.UsageCount) + 1
            
            $now = (Get-Date).ToUniversalTime().ToString("o")
            $history = @()
            if ($meta.RequestHistory) { $history = @($meta.RequestHistory) }
            $history += $now
            $sevenDaysAgo = (Get-Date).AddDays(-7).ToUniversalTime()
            $filteredHistory = @()
            $dummyDate = [DateTime]::MinValue
            foreach ($ts in $history) {
                if ($ts -and [DateTime]::TryParse($ts, [ref]$dummyDate)) {
                    $dt = [DateTime]::Parse($ts)
                    if ($dt -ge $sevenDaysAgo) {
                        $filteredHistory += $ts
                    }
                }
            }
            $meta.RequestHistory = $filteredHistory
            
            try {
                $json = ConvertTo-Json $meta
                $json | Out-File -FilePath $metaFile -Force -Encoding utf8
            } catch {}
        }
    }

    static [long] GetPrivateDirectorySize([string]$Path) {
        if (-not (Test-Path $Path)) { return 0 }
        $totalSize = 0
        try {
            $items = Get-ChildItem -Path $Path -Recurse -Force -ErrorAction SilentlyContinue
            foreach ($item in $items) {
                if (-not $item.PSIsContainer) {
                    $isInsideJunction = $false
                    $parent = Split-Path $item.FullName -Parent
                    while ($parent -and $parent.Length -ge $Path.Length) {
                        if (Test-Path $parent) {
                            $parentObj = Get-Item $parent -ErrorAction SilentlyContinue
                            if ($parentObj -and $parentObj.LinkType -eq "Junction") {
                                $isInsideJunction = $true
                                break
                            }
                        }
                        $parent = Split-Path $parent -Parent
                    }
                    if (-not $isInsideJunction) {
                        $totalSize += $item.Length
                    }
                }
            }
        } catch {}
        return $totalSize
    }

    static [string] SyncSharedComponents([string]$AccountName) {
        if ($AccountName -eq "default") { return "Healthy (Primary)" }
        $destDir = [AgyAccountManager]::GetAccountDirectory($AccountName)
        if (-not (Test-Path $destDir)) { return "Uninitialized" }
        
        $subDirsToShare = @("antigravity", "antigravity-cli", "config", "history", "antigravity-ide", "wf")
        $repairedAny = $false
        foreach ($subDir in $subDirsToShare) {
            $srcSubPath = Join-Path ([AgyAccountManager]::AgySourceHome) $subDir
            $destSubPath = Join-Path $destDir $subDir

            if (-not (Test-Path $srcSubPath)) {
                try { $null = New-Item -ItemType Directory -Path $srcSubPath -Force } catch {}
            }

            $needsRelink = $false
            if (-not (Test-Path $destSubPath)) {
                $needsRelink = $true
            } else {
                $item = Get-Item $destSubPath -ErrorAction SilentlyContinue
                if ($item.LinkType -ne "Junction") {
                    Remove-Item $destSubPath -Recurse -Force -ErrorAction SilentlyContinue
                    $needsRelink = $true
                }
            }

            if ($needsRelink) {
                try {
                    $null = New-Item -ItemType Junction -Path $destSubPath -Value $srcSubPath -Force
                    $repairedAny = $true
                } catch {
                    Write-Warning "Failed to re-link shared components for '$subDir'."
                }
            }
        }
        if ($repairedAny) { return "Restored" } else { return "Healthy" }
    }

    static [hashtable] GetAccountStats([string]$AccountName) {
        $meta = [AgyAccountManager]::GetAccountMetadata($AccountName)
        $dir = [AgyAccountManager]::GetAccountDirectory($AccountName)
        $privateSize = [AgyAccountManager]::GetPrivateDirectorySize($dir)
        $junctionStatus = [AgyAccountManager]::SyncSharedComponents($AccountName)
        
        $skillsPath = Join-Path ([AgyAccountManager]::AgySourceHome) "config\skills"
        $skillsCount = 0
        if (Test-Path $skillsPath) {
            $skillsCount = (Get-ChildItem -Path $skillsPath -Directory -ErrorAction SilentlyContinue).Count
        }

        $convPath = Join-Path ([AgyAccountManager]::AgySourceHome) "antigravity\brain"
        $convCount = 0
        if (Test-Path $convPath) {
            $convCount = (Get-ChildItem -Path $convPath -Directory -ErrorAction SilentlyContinue).Count
        }

        $tokenFile = Join-Path $dir "keyring_token.txt"
        $tokenStatus = if (Test-Path $tokenFile) { "Logged In" } else { "Not Logged In" }

        $formattedSize = "0 B"
        if ($privateSize -gt 1MB) {
            $formattedSize = "$([Math]::Round($privateSize / 1MB, 2)) MB"
        } elseif ($privateSize -gt 1KB) {
            $formattedSize = "$([Math]::Round($privateSize / 1KB, 2)) KB"
        } else {
            $formattedSize = "$privateSize B"
        }

        $quotaMetrics = [AgyAccountManager]::CalculateRollingQuotas($AccountName)
        $quotaStatus = if ($meta.QuotaStatus) { $meta.QuotaStatus } else { "OK" }
        $geminiWeekly = $quotaMetrics.RemainingWeekly
        $geminiFiveHour = $quotaMetrics.Remaining5H

        return @{
            LastUsed = $meta.LastUsed
            UsageCount = $meta.UsageCount
            PrivateSize = $formattedSize
            JunctionStatus = $junctionStatus
            SkillsCount = $skillsCount
            ConversationsCount = $convCount
            TokenStatus = $tokenStatus
            QuotaStatus = $quotaStatus
            GeminiWeekly = $geminiWeekly
            GeminiFiveHour = $geminiFiveHour
        }
    }

    static [string[]] GetAccounts() {
        $accounts = [System.Collections.Generic.List[string]]::new()
        $accounts.Add("default")

        $scanPaths = @()
        if (Test-Path $env:USERPROFILE) { $scanPaths += $env:USERPROFILE }
        $parentDir = Split-Path ([AgyAccountManager]::AgyAccountPrefix) -Parent
        if ((Test-Path $parentDir) -and ($parentDir -notin $scanPaths)) { $scanPaths += $parentDir }

        foreach ($path in $scanPaths) {
            $dirs = Get-ChildItem -Path $path -Directory -Filter ".gemini_*" -ErrorAction SilentlyContinue
            foreach ($dir in $dirs) {
                if ($dir.Name -match '^\.gemini_(.+)$') {
                    $name = $Matches[1]
                    if ($name -notmatch 'backup|copy|temp' -and $name -notin $accounts) {
                        $accounts.Add($name)
                    }
                }
            }
        }
        return $accounts.ToArray()
    }

    static [string] GetActiveAccount() {
        if ($env:GEMINI_HOME -and $env:GEMINI_HOME -match '\.gemini_(.+)$') {
            return $Matches[1]
        }
        return "default"
    }

    static [string] GetAccountDirectory([string]$AccountName) {
        if ($AccountName -eq "default") {
            return [AgyAccountManager]::AgySourceHome
        }
        $targetDir = [AgyAccountManager]::AgyAccountPrefix + $AccountName
        if (-not (Test-Path $targetDir)) {
            $userProfilePath = Join-Path $env:USERPROFILE ".gemini_$AccountName"
            if (Test-Path $userProfilePath) {
                return $userProfilePath
            }
        }
        return $targetDir
    }

    static [void] BackupActiveToken() {
        $activeAcc = [AgyAccountManager]::GetActiveAccount()
        $token = [AgyKeyringHelper]::ReadToken("gemini:antigravity")
        $accDir = [AgyAccountManager]::GetAccountDirectory($activeAcc)
        if (-not (Test-Path $accDir)) {
            try { $null = New-Item -ItemType Directory -Path $accDir -Force } catch {}
        }
        if (Test-Path $accDir) {
            $tokenFile = Join-Path $accDir "keyring_token.txt"
            if ($token) {
                try {
                    $secure = ConvertTo-SecureString $token -AsPlainText -Force
                    $encrypted = ConvertFrom-SecureString $secure
                    $encrypted | Out-File -FilePath $tokenFile -Force -Encoding utf8
                } catch {
                    Write-Warning "Failed to encrypt/save keyring token for '$activeAcc'."
                }
            } else {
                if (Test-Path $tokenFile) {
                    Remove-Item $tokenFile -Force -ErrorAction SilentlyContinue
                }
            }
        }
    }

    static [void] RestoreActiveToken([string]$AccountName) {
        $accDir = [AgyAccountManager]::GetAccountDirectory($AccountName)
        $tokenFile = Join-Path $accDir "keyring_token.txt"
        if (Test-Path $tokenFile) {
            try {
                $encrypted = (Get-Content -Path $tokenFile -Raw -ErrorAction SilentlyContinue)
                if ($encrypted) {
                    $encrypted = $encrypted.Trim()
                    $secure = ConvertTo-SecureString $encrypted
                    $networkCred = [System.Net.NetworkCredential]::new("", $secure)
                    $token = $networkCred.Password
                    $res = [AgyKeyringHelper]::WriteToken("gemini:antigravity", "antigravity", $token)
                } else {
                    $res = [AgyKeyringHelper]::DeleteToken("gemini:antigravity")
                }
            } catch {
                Write-Warning "Failed to decrypt/restore keyring token for '$AccountName'."
            }
        } else {
            $res = [AgyKeyringHelper]::DeleteToken("gemini:antigravity")
        }
    }

    static [void] SetActiveAccount([string]$AccountName, [bool]$Temporary) {
        if ($AccountName -ne "default") {
            $targetDir = [AgyAccountManager]::GetAccountDirectory($AccountName)
            if (-not (Test-Path $targetDir)) {
                Write-Error "Account '$AccountName' does not exist. Create it first using 'agy-account add $AccountName'."
                return
            }
        }

        # Update usage metadata
        [AgyAccountManager]::UpdateAccountMetadata($AccountName)

        # 1. Back up active token before switching
        [AgyAccountManager]::BackupActiveToken()

        if ($AccountName -eq "default") {
            $env:GEMINI_HOME = [AgyAccountManager]::AgySourceHome
            if (-not $Temporary) {
                if (Test-Path ([AgyAccountManager]::AgyActiveAccountFile)) {
                    Remove-Item ([AgyAccountManager]::AgyActiveAccountFile) -Force
                }
            }
            # 2. Restore token for default account
            [AgyAccountManager]::RestoreActiveToken("default")
            Write-Host "[*] Switched to default Antigravity account (Primary)." -ForegroundColor Green
            return
        }

        $targetDir = [AgyAccountManager]::GetAccountDirectory($AccountName)

        # Self-healing unique installation ID
        $defaultIdFile = Join-Path ([AgyAccountManager]::AgySourceHome) "installation_id"
        $targetIdFile = Join-Path $targetDir "installation_id"
        $defaultId = ""
        if (Test-Path $defaultIdFile) {
            $content = Get-Content $defaultIdFile -ErrorAction SilentlyContinue
            if ($null -ne $content) { $defaultId = $content.ToString().Trim() }
        }
        $targetId = ""
        if (Test-Path $targetIdFile) {
            $content = Get-Content $targetIdFile -ErrorAction SilentlyContinue
            if ($null -ne $content) { $targetId = $content.ToString().Trim() }
        }
        if ([string]::IsNullOrWhiteSpace($targetId) -or $targetId -eq $defaultId) {
            if (-not [System.IO.Directory]::Exists($targetDir)) {
                [void][System.IO.Directory]::CreateDirectory($targetDir)
            }
            $newId = [guid]::NewGuid().ToString()
            $newId | Out-File -FilePath $targetIdFile -NoNewline -Force -Encoding utf8
            Write-Host "[config] Re-generated unique installation ID for '$AccountName' to separate credentials." -ForegroundColor Yellow
        }

        $env:GEMINI_HOME = $targetDir
        # 2. Restore token for the switched account
        [AgyAccountManager]::RestoreActiveToken($AccountName)

        if (-not $Temporary) {
            if (-not (Test-Path ([AgyAccountManager]::AgySourceHome))) {
                $null = New-Item -ItemType Directory -Path ([AgyAccountManager]::AgySourceHome) -Force
            }
            $AccountName | Out-File -FilePath ([AgyAccountManager]::AgyActiveAccountFile) -Force -Encoding utf8
            Write-Host "[*] Switched to account '$AccountName' (Persistent)." -ForegroundColor Green
        } else {
            Write-Host "[!] Switched to account '$AccountName' (Temporary - current session only)." -ForegroundColor Yellow
        }
    }

    static [void] AddAccount([string]$AccountName) {
        if ([string]::IsNullOrWhiteSpace($AccountName)) {
            Write-Error "Account name cannot be empty."
            return
        }
        if ($AccountName -eq "default") {
            Write-Error "Cannot create an account named 'default'. It is reserved for the primary account."
            return
        }
        if (-not (Test-Path ([AgyAccountManager]::AgySourceHome))) {
            Write-Error ("Source directory " + [AgyAccountManager]::AgySourceHome + " does not exist. Run 'agy' at least once first.")
            return
        }

        $destDir = [AgyAccountManager]::AgyAccountPrefix + $AccountName
        if (Test-Path $destDir) {
            Write-Warning "Account '$AccountName' already exists at $destDir."
            return
        }

        Write-Host "[dir] Creating isolated account directory: $destDir" -ForegroundColor Cyan
        $null = New-Item -ItemType Directory -Path $destDir -Force

        $credentialsFiles = @("google_accounts.json", "oauth_creds.json", "state.json", "installation_id", "keyring_token.txt")
        Get-ChildItem -Path ([AgyAccountManager]::AgySourceHome) -File | ForEach-Object {
            if ($_.Name -notin $credentialsFiles) {
                $destFile = Join-Path $destDir $_.Name
                Write-Host "[file] Copying root file: $($_.Name) -> $destDir" -ForegroundColor Gray
                Copy-Item -Path $_.FullName -Destination $destFile -Force
            }
        }

        $uniqueId = [guid]::NewGuid().ToString()
        $uniqueId | Out-File -FilePath (Join-Path $destDir "installation_id") -NoNewline -Force -Encoding utf8
        Write-Host "[file] Generated unique installation ID for account '$AccountName'." -ForegroundColor Gray

        $subDirsToShare = @("antigravity", "antigravity-cli", "config", "history", "antigravity-ide", "wf")
        foreach ($subDir in $subDirsToShare) {
            $srcSubPath = Join-Path ([AgyAccountManager]::AgySourceHome) $subDir
            $destSubPath = Join-Path $destDir $subDir
            if (-not (Test-Path $srcSubPath)) {
                $null = New-Item -ItemType Directory -Path $srcSubPath -Force
            }
            Write-Host "[link] Creating Directory Junction: $destSubPath -> $srcSubPath" -ForegroundColor Green
            $null = New-Item -ItemType Junction -Path $destSubPath -Value $srcSubPath -Force
        }

        # Dynamically load wrapper for the new account immediately
        [AgyAccountManager]::NewAgyDynamicWrapper($AccountName, $destDir)

        Write-Host "[ok] Antigravity account '$AccountName' initialized successfully." -ForegroundColor Green
    }

    static [void] RemoveAccount([string]$AccountName) {
        if ([string]::IsNullOrWhiteSpace($AccountName)) {
            Write-Error "Account name cannot be empty."
            return
        }
        if ($AccountName -eq "default") {
            Write-Error "Cannot remove the 'default' account."
            return
        }

        $targetDir = [AgyAccountManager]::GetAccountDirectory($AccountName)
        if (-not (Test-Path $targetDir)) {
            Write-Error "Account '$AccountName' does not exist."
            return
        }

        $choice = Read-Host "Are you sure you want to delete account '$AccountName'? This will erase settings and login sessions. (y/N)"
        if ($choice -ne "y" -and $choice -ne "yes") {
            Write-Host "Deletion cancelled." -ForegroundColor Yellow
            return
        }

        $subfolders = Get-ChildItem -Path $targetDir -Directory
        foreach ($folder in $subfolders) {
            $isJunction = (Get-Item $folder.FullName).LinkType -eq "Junction"
            if ($isJunction) {
                Write-Host "[link] Removing Junction link: $($folder.FullName)" -ForegroundColor Yellow
                cmd /c rmdir "$($folder.FullName)"
            }
        }

        Write-Host "[del] Deleting directory: $targetDir" -ForegroundColor Red
        Remove-Item -Path $targetDir -Recurse -Force

        $activeAcc = [AgyAccountManager]::GetActiveAccount()
        if ($activeAcc -eq $AccountName) {
            Write-Host "[!] Active account was deleted. Reverting to default." -ForegroundColor Warning
            [AgyAccountManager]::SetActiveAccount("default", $false)
        }

        Write-Host "[ok] Account '$AccountName' removed successfully." -ForegroundColor Green
    }

    static [void] ResetCredentials([string]$AccountName) {
        $targetHome = [AgyAccountManager]::GetAccountDirectory($AccountName)
        if (-not (Test-Path $targetHome)) {
            Write-Error "Account '$AccountName' does not exist."
            return
        }

        $filesToRemove = @("google_accounts.json", "oauth_creds.json", "state.json")
        $clearedAny = $false
        foreach ($file in $filesToRemove) {
            $filePath = Join-Path $targetHome $file
            if (Test-Path $filePath) {
                Remove-Item $filePath -Force
                $clearedAny = $true
            }
        }
        $tokenFile = Join-Path $targetHome "keyring_token.txt"
        if (Test-Path $tokenFile) {
            Remove-Item $tokenFile -Force
            $clearedAny = $true
        }

        # Clear active keyring token if this is the active account
        if ($AccountName -eq [AgyAccountManager]::GetActiveAccount()) {
            $deletedKeyring = [AgyKeyringHelper]::DeleteToken("gemini:antigravity")
            if ($deletedKeyring) { $clearedAny = $true }
        }

        if ($clearedAny) {
            Write-Host "[ok] Credentials cleared for account '$AccountName'. Next run will prompt for login." -ForegroundColor Green
        } else {
            Write-Host "[info] Account '$AccountName' is already signed out." -ForegroundColor Cyan
        }
    }

    static [void] InvokeWithAccount([string]$AccountName, [string[]]$ArgsList) {
        # Update usage metadata
        [AgyAccountManager]::UpdateAccountMetadata($AccountName)

        $targetHome = [AgyAccountManager]::GetAccountDirectory($AccountName)
        if (-not (Test-Path $targetHome)) {
            Write-Error "Account '$AccountName' does not exist."
            return
        }

        # Synchronize keyring to match this account before invoking
        [AgyAccountManager]::BackupActiveToken()
        [AgyAccountManager]::RestoreActiveToken($AccountName)

        $oldHome = $env:GEMINI_HOME
        $oldRealHome = $env:HOME
        $oldProfile = $env:USERPROFILE
        $oldToken = $env:GEMINI_CLI_IDE_AUTH_TOKEN
        $oldPort = $env:GEMINI_CLI_IDE_SERVER_PORT
        $oldLsAddress = $env:ANTIGRAVITY_LS_ADDRESS
        $oldCsrfToken = $env:ANTIGRAVITY_CSRF_TOKEN
        $oldProjId = $env:ANTIGRAVITY_PROJECT_ID
        $oldConvId = $env:ANTIGRAVITY_CONVERSATION_ID
        $oldAgent = $env:ANTIGRAVITY_AGENT

        if ($AccountName -ne "default") {
            $defaultIdFile = Join-Path ([AgyAccountManager]::AgySourceHome) "installation_id"
            $targetIdFile = Join-Path $targetHome "installation_id"
            $defaultId = ""
            if (Test-Path $defaultIdFile) {
                $content = Get-Content $defaultIdFile -ErrorAction SilentlyContinue
                if ($null -ne $content) { $defaultId = $content.ToString().Trim() }
            }
            $targetId = ""
            if (Test-Path $targetIdFile) {
                $content = Get-Content $targetIdFile -ErrorAction SilentlyContinue
                if ($null -ne $content) { $targetId = $content.ToString().Trim() }
            }
            if ([string]::IsNullOrWhiteSpace($targetId) -or $targetId -eq $defaultId) {
                if (-not [System.IO.Directory]::Exists($targetHome)) {
                    [void][System.IO.Directory]::CreateDirectory($targetHome)
                }
                $newId = [guid]::NewGuid().ToString()
                $newId | Out-File -FilePath $targetIdFile -NoNewline -Force -Encoding utf8
                Write-Host "[config] Re-generated unique installation ID for '$AccountName' to separate credentials." -ForegroundColor Yellow
            }
        }

        $binPath = [AgyAccountManager]::AgyBinaryPath
        try {
            $env:GEMINI_HOME = $targetHome
            if ($AccountName -ne "default") {
                $env:HOME = $targetHome
                $env:USERPROFILE = $targetHome
                $env:GEMINI_CLI_IDE_AUTH_TOKEN = $null
                $env:GEMINI_CLI_IDE_SERVER_PORT = $null
                $env:ANTIGRAVITY_LS_ADDRESS = $null
                $env:ANTIGRAVITY_CSRF_TOKEN = $null
                $env:ANTIGRAVITY_PROJECT_ID = $null
                $env:ANTIGRAVITY_CONVERSATION_ID = $null
                $env:ANTIGRAVITY_AGENT = $null
            }

            if (-not (Test-Path $binPath)) {
                Write-Error ("Antigravity CLI executable not found at " + $binPath + ".")
                return
            }

            if ($ArgsList) {
                & $binPath @ArgsList
            } else {
                & $binPath
            }
        } finally {
            # Back up any token generated or updated during this invocation (such as 'agy login')
            try {
                [AgyAccountManager]::BackupActiveToken()
            } catch {}

            $env:GEMINI_HOME = $oldHome
            $env:HOME = $oldRealHome
            $env:USERPROFILE = $oldProfile
            $env:GEMINI_CLI_IDE_AUTH_TOKEN = $oldToken
            $env:GEMINI_CLI_IDE_SERVER_PORT = $oldPort
            $env:ANTIGRAVITY_LS_ADDRESS = $oldLsAddress
            $env:ANTIGRAVITY_CSRF_TOKEN = $oldCsrfToken
            $env:ANTIGRAVITY_PROJECT_ID = $oldProjId
            $env:ANTIGRAVITY_CONVERSATION_ID = $oldConvId
            $env:ANTIGRAVITY_AGENT = $oldAgent

            # Restore the prior active account's token
            $priorActive = [AgyAccountManager]::GetActiveAccount()
            [AgyAccountManager]::RestoreActiveToken($priorActive)
        }
    }

    static [void] ShowAccounts() {
        $accounts = [AgyAccountManager]::GetAccounts()
        $active = [AgyAccountManager]::GetActiveAccount()

        Write-Host ""
        Write-Host "  Antigravity Accounts:" -ForegroundColor Cyan
        Write-Host "  =====================" -ForegroundColor Cyan

        foreach ($acc in $accounts) {
            $path = [AgyAccountManager]::GetAccountDirectory($acc)
            if ($acc -eq $active) {
                Write-Host "  >  $('{0,-15}' -f $acc) ($path)" -ForegroundColor Green
            } else {
                Write-Host "     $('{0,-15}' -f $acc) ($path)" -ForegroundColor DarkGray
            }
        }
        Write-Host ""
    }

    static [void] NewAgyDynamicWrapper([string]$AccountName, [string]$GeminiHomePath) {
        $funcName = "agy-$AccountName"
        $binPath = [AgyAccountManager]::AgyBinaryPath
        $scriptBlock = [ScriptBlock]::Create(@"
            param([Parameter(ValueFromRemainingArguments=`$true)][string[]]`$PassThruArgs)
            `$oldHome = `$env:GEMINI_HOME
            `$oldRealHome = `$env:HOME
            `$oldProfile = `$env:USERPROFILE
            `$oldToken = `$env:GEMINI_CLI_IDE_AUTH_TOKEN
            `$oldPort = `$env:GEMINI_CLI_IDE_SERVER_PORT
            `$oldLsAddress = `$env:ANTIGRAVITY_LS_ADDRESS
            `$oldCsrfToken = `$env:ANTIGRAVITY_CSRF_TOKEN
            `$oldProjId = `$env:ANTIGRAVITY_PROJECT_ID
            `$oldConvId = `$env:ANTIGRAVITY_CONVERSATION_ID
            `$oldAgent = `$env:ANTIGRAVITY_AGENT
            try {
                `$env:GEMINI_HOME = "$GeminiHomePath"
                `$env:HOME = "$GeminiHomePath"
                `$env:USERPROFILE = "$GeminiHomePath"
                `$env:GEMINI_CLI_IDE_AUTH_TOKEN = `$null
                `$env:GEMINI_CLI_IDE_SERVER_PORT = `$null
                `$env:ANTIGRAVITY_LS_ADDRESS = `$null
                `$env:ANTIGRAVITY_CSRF_TOKEN = `$null
                `$env:ANTIGRAVITY_PROJECT_ID = `$null
                `$env:ANTIGRAVITY_CONVERSATION_ID = `$null
                `$env:ANTIGRAVITY_AGENT = `$null
                
                # Temporarily sync keyring token for this specific wrapper run
                [AgyAccountManager]::BackupActiveToken()
                [AgyAccountManager]::RestoreActiveToken("$AccountName")

                if (`$PassThruArgs) { & "$binPath" @PassThruArgs } else { & "$binPath" }
            } finally {
                `$env:GEMINI_HOME = `$oldHome
                `$env:HOME = `$oldRealHome
                `$env:USERPROFILE = `$oldProfile
                `$env:GEMINI_CLI_IDE_AUTH_TOKEN = `$oldToken
                `$env:GEMINI_CLI_IDE_SERVER_PORT = `$oldPort
                `$env:ANTIGRAVITY_LS_ADDRESS = `$oldLsAddress
                `$env:ANTIGRAVITY_CSRF_TOKEN = `$oldCsrfToken
                `$env:ANTIGRAVITY_PROJECT_ID = `$oldProjId
                `$env:ANTIGRAVITY_CONVERSATION_ID = `$oldConvId
                `$env:ANTIGRAVITY_AGENT = `$oldAgent

                # Restore previous active token
                `$priorActive = [AgyAccountManager]::GetActiveAccount()
                [AgyAccountManager]::RestoreActiveToken(`$priorActive)
            }
"@
        )
        Set-Item -Path "Function:\global:$funcName" -Value $scriptBlock -Force
    }

    static [void] SelectAccountInteractive() {
        $accounts = [AgyAccountManager]::GetAccounts()
        $active = [AgyAccountManager]::GetActiveAccount()

        # Build TUI Menu items using global TerminalMenu
        $menuItems = @()
        $defaultIdx = 0
        for ($i = 0; $i -lt $accounts.Count; $i++) {
            $acc = $accounts[$i]
            if ($acc -eq $active) {
                $menuItems += "$acc (Active)"
                $defaultIdx = $i
            } else {
                $menuItems += $acc
            }
        }
        $menuItems += "[+] Add New Account"
        $menuItems += "[x] Delete Account"
        $menuItems += "[exit] Cancel / Exit"

        $selected = ([type]"TerminalMenu")::ShowRobust(@("Select Antigravity Account"), $menuItems, $defaultIdx, $false, $true)

        if ($selected -lt 0) { return }

        if ($selected -lt $accounts.Count) {
            [AgyAccountManager]::SetActiveAccount($accounts[$selected], $false)
        }
        elseif ($selected -eq $accounts.Count) {
            Write-Host ""
            $name = Read-Host "Enter new account name"
            if (-not [string]::IsNullOrWhiteSpace($name)) {
                [AgyAccountManager]::AddAccount($name)
            }
        }
        elseif ($selected -eq ($accounts.Count + 1)) {
            $deletable = $accounts | Where-Object { $_ -ne "default" }
            if ($deletable.Count -eq 0) {
                Write-Warning "No secondary accounts available to delete."
                return
            }
            $selectedDel = ([type]"TerminalMenu")::ShowRobust(@("Delete Antigravity Account"), $deletable, 0, $false, $true)
            if ($selectedDel -ge 0) {
                [AgyAccountManager]::RemoveAccount($deletable[$selectedDel])
            }
        }
    }

    static [bool] IsAutoSwitchEnabled() {
        $file = Join-Path ([AgyAccountManager]::AgySourceHome) "auto_switch_enabled.txt"
        if (Test-Path $file) {
            try {
                $content = (Get-Content -Path $file -Raw -ErrorAction SilentlyContinue)
                if ($content -and $content.Trim() -eq "False") {
                    return $false
                }
            } catch {}
        }
        return $true
    }

    static [void] ToggleAutoSwitch() {
        $file = Join-Path ([AgyAccountManager]::AgySourceHome) "auto_switch_enabled.txt"
        $current = [AgyAccountManager]::IsAutoSwitchEnabled()
        $newVal = if ($current) { "False" } else { "True" }
        try {
            if (-not (Test-Path ([AgyAccountManager]::AgySourceHome))) {
                $null = New-Item -ItemType Directory -Path ([AgyAccountManager]::AgySourceHome) -Force
            }
            $newVal | Out-File -FilePath $file -Force -Encoding utf8
            Write-Host "[Settings] Auto-Switch is now: $(if ($current) { 'Disabled' } else { 'Enabled' })" -ForegroundColor Yellow
        } catch {
            Write-Error "Failed to update Auto-Switch setting."
        }
    }

    static [void] ShowAllAccountsSummary() {
        $lines = @()
        $lines += "================================================================================================================="
        $lines += "                                    ANTIGRAVITY ACCOUNT USAGE SUMMARY"
        $lines += "================================================================================================================="
        $lines += ("  {0,-25} | {1,-12} | {2,-15} | {3,-12} | {4,-20} | {5,-10}" -f "Account Name", "Status", "Quota (W / 5h)", "Usage Count", "Last Used", "Dir Size")
        $lines += "  -----------------------------------------------------------------------------------------------------------------"

        $accounts = [AgyAccountManager]::GetAccounts()
        $active = [AgyAccountManager]::GetActiveAccount()

        foreach ($acc in $accounts) {
            $stats = [AgyAccountManager]::GetAccountStats($acc)
            $statusStr = $stats.TokenStatus
            
            $quotaStr = "--"
            if ($statusStr -eq "Logged In") {
                if ($stats.QuotaStatus -eq "Exceeded") {
                    $quotaStr = "Exceeded"
                } else {
                    $w = [Math]::Round($stats.GeminiWeekly)
                    $f = [Math]::Round($stats.GeminiFiveHour)
                    $quotaStr = "$w% / $f%"
                }
            }

            $usageCountStr = $stats.UsageCount.ToString()
            
            $lastUsedStr = "Never"
            if ($stats.LastUsed -and $stats.LastUsed -match '^\d{4}-\d{2}-\d{2}T(\d{2}:\d{2}:\d{2})') {
                $lastUsedStr = $stats.LastUsed.Substring(0,10) + " " + $Matches[1]
            }

            $sizeStr = $stats.PrivateSize

            $nameLabel = if ($acc -eq $active) { "* $acc" } else { "  $acc" }

            # Format line
            $line = "  {0,-25} | {1,-12} | {2,-15} | {3,-12} | {4,-20} | {5,-10}" -f $nameLabel, $statusStr, $quotaStr, $usageCountStr, $lastUsedStr, $sizeStr
            $lines += $line
        }
        $lines += "================================================================================================================="

        ([type]"TerminalMenu")::ShowScrollableContent("Account Usage Summary", $lines)
    }

    static [void] ManageAccountsInteractive() {
        while ($true) {
            $accounts = [AgyAccountManager]::GetAccounts()
            $active = [AgyAccountManager]::GetActiveAccount()

            # Build TUI Menu items showing login status
            $menuItems = @()
            $defaultIdx = 0
            for ($i = 0; $i -lt $accounts.Count; $i++) {
                $acc = $accounts[$i]
                $accDir = [AgyAccountManager]::GetAccountDirectory($acc)
                $tokenFile = Join-Path $accDir "keyring_token.txt"
                $status = "Not Logged In"
                if (Test-Path $tokenFile) {
                    $status = "Logged In"
                }
                
                $label = ""
                if ($acc -eq $active) {
                    $label = "* $acc (Active, $status)"
                    $defaultIdx = $i
                } else {
                    $label = "  $acc ($status)"
                }
                $menuItems += $label
            }
            $menuItems += "[+] Add New Account"
            
            $autoSwitchStatus = if ([AgyAccountManager]::IsAutoSwitchEnabled()) { "Enabled" } else { "Disabled" }
            $menuItems += "[Settings] Toggle Auto-Switch (Currently: $autoSwitchStatus)"
            $menuItems += "[Stats] Show All Accounts Summary"
            $menuItems += "[x] Exit Dashboard"

            $selected = ([type]"TerminalMenu")::ShowRobust(@("Antigravity Multi-Account Manager"), $menuItems, $defaultIdx, $false, $true)

            if ($selected -lt 0 -or $selected -eq ($menuItems.Count - 1)) {
                break
            }

            if ($selected -lt $accounts.Count) {
                # Account Selected -> Open Sub-menu of actions!
                $targetAcc = $accounts[$selected]

                # Fetch and display stats card
                Clear-Host
                $stats = [AgyAccountManager]::GetAccountStats($targetAcc)
                Write-Host "=============================================" -ForegroundColor Cyan
                Write-Host " ACCOUNT STATS: $targetAcc" -ForegroundColor Cyan
                Write-Host "=============================================" -ForegroundColor Cyan
                Write-Host "  * Status:          $($stats.TokenStatus)" -ForegroundColor Gray
                Write-Host "  * Quota Status:    $($stats.QuotaStatus)" -ForegroundColor Gray
                Write-Host "  * Last Used:       $($stats.LastUsed)" -ForegroundColor Gray
                Write-Host "  * Usage Count:     $($stats.UsageCount) sessions/calls" -ForegroundColor Gray
                Write-Host "  * Private Size:    $($stats.PrivateSize) (excluding shared)" -ForegroundColor Gray
                Write-Host "  * Sync Health:     $($stats.JunctionStatus)" -ForegroundColor Gray
                Write-Host "  * Shared Skills:   $($stats.SkillsCount) skills" -ForegroundColor Gray
                Write-Host "  * Shared History:  $($stats.ConversationsCount) conversations" -ForegroundColor Gray
                Write-Host "=============================================" -ForegroundColor Cyan
                Write-Host ""

                $accDir = [AgyAccountManager]::GetAccountDirectory($targetAcc)
                $tokenFile = Join-Path $accDir "keyring_token.txt"
                $status = if (Test-Path $tokenFile) { "Logged In" } else { "Not Logged In" }

                $subItems = @(
                    "[Switch] Set as Active (Persistent)",
                    "[Switch] Set as Active (Temporary)",
                    "[Usage] Models & Quota",
                    "[Login] Sign In / Re-authenticate",
                    "[Logout] Sign Out / Reset Credentials"
                )
                if ($targetAcc -ne "default") {
                    $subItems += "[Delete] Remove Account"
                }
                $subItems += "[Back] Return to Main Menu"

                $subSel = ([type]"TerminalMenu")::ShowRobust(@("Manage Account: $targetAcc ($status)"), $subItems, 0, $false, $true)
                if ($subSel -lt 0) { continue }

                switch ($subItems[$subSel]) {
                    "[Switch] Set as Active (Persistent)" {
                        [AgyAccountManager]::SetActiveAccount($targetAcc, $false)
                        Start-Sleep -Milliseconds 1000
                    }
                    "[Switch] Set as Active (Temporary)" {
                        [AgyAccountManager]::SetActiveAccount($targetAcc, $true)
                        Start-Sleep -Milliseconds 1000
                    }
                    "[Usage] Models & Quota" {
                        $usageLines = [AgyAccountManager]::GetUsageLines($targetAcc)
                        ([type]"TerminalMenu")::ShowScrollableContent("Models & Quota", $usageLines)
                    }
                    "[Login] Sign In / Re-authenticate" {
                        [AgyAccountManager]::ResetCredentials($targetAcc)
                        Write-Host "Starting login process for '$targetAcc'..." -ForegroundColor Cyan
                        [AgyAccountManager]::InvokeWithAccount($targetAcc, @("login"))
                        Write-Host "Press any key to continue..." -ForegroundColor Gray
                        try {
                            [void][Console]::ReadKey($true)
                        } catch {
                            Start-Sleep -Seconds 2
                        }
                    }
                    "[Logout] Sign Out / Reset Credentials" {
                        [AgyAccountManager]::ResetCredentials($targetAcc)
                        Start-Sleep -Milliseconds 1000
                    }
                    "[Delete] Remove Account" {
                        [AgyAccountManager]::RemoveAccount($targetAcc)
                        Start-Sleep -Milliseconds 1000
                    }
                }
            }
            elseif ($selected -eq $accounts.Count) {
                # Add Account
                Write-Host ""
                $name = Read-Host "Enter new account name"
                if (-not [string]::IsNullOrWhiteSpace($name)) {
                    [AgyAccountManager]::AddAccount($name)
                    Start-Sleep -Milliseconds 1000
                }
            }
            elseif ($selected -eq ($accounts.Count + 1)) {
                # Toggle Auto-Switch
                [AgyAccountManager]::ToggleAutoSwitch()
                Start-Sleep -Milliseconds 1000
            }
            elseif ($selected -eq ($accounts.Count + 2)) {
                # Show Stats
                [AgyAccountManager]::ShowAllAccountsSummary()
            }
        }
    }

    static [void] SetAccountQuotaExceeded([string]$AccountName, [bool]$Exceeded) {
        $accDir = [AgyAccountManager]::GetAccountDirectory($AccountName)
        if (Test-Path $accDir) {
            $metaFile = Join-Path $accDir "account_metadata.json"
            $meta = [AgyAccountManager]::GetAccountMetadata($AccountName)
            $newStatus = if ($Exceeded) { "Exceeded" } else { "OK" }
            if ($meta.QuotaStatus -ne $newStatus) {
                $meta.QuotaStatus = $newStatus
                try {
                    $json = ConvertTo-Json $meta
                    $json | Out-File -FilePath $metaFile -Force -Encoding utf8
                } catch {}
            }
        }
    }

    static [void] CheckQuotaAfterRun([string]$AccountName) {
        try {
            $brainDir = "C:\Users\TruongNhon\.gemini\antigravity\brain"
            if (-not (Test-Path $brainDir)) { return }
            
            # Find the most recently modified transcript.jsonl
            $latestFile = Get-ChildItem -Path $brainDir -Filter "transcript.jsonl" -Recurse -File -ErrorAction SilentlyContinue |
                          Sort-Object LastWriteTime -Descending | Select-Object -First 1

            if ($latestFile) {
                # If modified within the last 60 seconds
                $diff = (Get-Date) - $latestFile.LastWriteTime
                if ($diff.TotalSeconds -le 60) {
                    $content = Get-Content -Path $latestFile.FullName -Tail 15 -ErrorAction SilentlyContinue
                    $quotaErr = $false
                    foreach ($line in $content) {
                        if ($line -match 'RESOURCE_EXHAUSTED|quota exceeded|quotaExceeded|ResourceExhausted|quota limit') {
                            if ($line -match '"status"\s*:\s*"ERROR"' -or $line -match '"code"\s*:\s*429') {
                                $quotaErr = $true
                                break
                            }
                        }
                    }

                    if ($quotaErr) {
                        Write-Host "[!] Detected quota exceeded or API error for '$AccountName'." -ForegroundColor Red
                        [AgyAccountManager]::SetAccountQuotaExceeded($AccountName, $true)
                    } else {
                        [AgyAccountManager]::SetAccountQuotaExceeded($AccountName, $false)
                    }
                }
            }
        } catch {}
    }

    static [void] AutoSwitchOnQuotaExceeded() {
        if (-not [AgyAccountManager]::IsAutoSwitchEnabled()) { return }
        
        $active = [AgyAccountManager]::GetActiveAccount()
        $activeMeta = [AgyAccountManager]::GetAccountMetadata($active)
        if ($activeMeta.QuotaStatus -eq "Exceeded") {
            # Active account is out of quota. Let's find another logged-in account with OK quota
            $available = [AgyAccountManager]::GetAccounts()
            $candidate = $null
            foreach ($acc in $available) {
                if ($acc -eq $active) { continue }
                
                $accDir = [AgyAccountManager]::GetAccountDirectory($acc)
                $tokenFile = Join-Path $accDir "keyring_token.txt"
                if (Test-Path $tokenFile) {
                    $meta = [AgyAccountManager]::GetAccountMetadata($acc)
                    $quota = if ($meta.QuotaStatus) { $meta.QuotaStatus } else { "OK" }
                    if ($quota -eq "OK") {
                        $candidate = $acc
                        break
                    }
                }
            }

            if ($candidate) {
                Write-Host "[!] Active account '$active' exceeded quota. Auto-switching to candidate account '$candidate' with available quota." -ForegroundColor Yellow
                [AgyAccountManager]::SetActiveAccount($candidate, $false)
            }
        }
    }

    static [string] GetProgressBar([double]$percentage) {
        $totalChars = 50
        $filledChars = [int][Math]::Round(($percentage / 100.0) * $totalChars)
        if ($filledChars -gt $totalChars) { $filledChars = $totalChars }
        if ($filledChars -lt 0) { $filledChars = 0 }
        $emptyChars = $totalChars - $filledChars
        $charFilled = [char]0x2588
        $charEmpty = [char]0x2591
        $bar = ([string]$charFilled * $filledChars) + ([string]$charEmpty * $emptyChars)
        return "    [$bar] $([Math]::Round($percentage, 2).ToString('F2'))%"
    }

    static [string[]] GetUsageLines([string]$AccountName) {
        $meta = [AgyAccountManager]::GetAccountMetadata($AccountName)
        
        $quotaMetrics = [AgyAccountManager]::CalculateRollingQuotas($AccountName)
        $meta.GeminiWeekly = $quotaMetrics.RemainingWeekly
        $meta.GeminiFiveHour = $quotaMetrics.Remaining5H
        $refreshStr = $quotaMetrics.TimeWeekly
        $refresh5HStr = $quotaMetrics.Time5H

        # Ensure quota fields exist
        if ($null -eq $meta.GeminiWeekly) { $meta.GeminiWeekly = 52.60 }
        if ($null -eq $meta.GeminiFiveHour) { $meta.GeminiFiveHour = 100.00 }
        if ($null -eq $meta.ClaudeWeekly) { $meta.ClaudeWeekly = 100.00 }
        if ($null -eq $meta.ClaudeFiveHour) { $meta.ClaudeFiveHour = 100.00 }

        # Define encoding-safe Unicode characters
        $charLine = [char]0x2500     # ─
        $charAngle = [char]0x2514    # └
        $charVertical = [char]0x2502 # │
        $charBullet = [char]0x00B7   # ·
        $horizontalBar = [string]$charLine * 140

        $lines = @()
        $lines += $horizontalBar
        $lines += ">"
        $lines += $horizontalBar
        $lines += "$charAngle Models & Quota"
        $lines += ""
        $lines += "  Account: $AccountName"
        $lines += ""
        $lines += "GEMINI MODELS"
        $lines += "  Models within this group: Gemini Flash, Gemini Pro"
        $lines += ""
        $lines += "  Weekly Limit"
        $lines += [AgyAccountManager]::GetProgressBar($meta.GeminiWeekly)
        $remGeminiWeekly = [int][Math]::Round($meta.GeminiWeekly)
        $lines += "    $remGeminiWeekly% remaining $charBullet Refreshes in $refreshStr"
        $lines += ""
        $lines += "  Five Hour Limit"
        $lines += [AgyAccountManager]::GetProgressBar($meta.GeminiFiveHour)
        $remGemini5h = [int][Math]::Round($meta.GeminiFiveHour)
        if ($meta.GeminiFiveHour -ge 100.0) {
            $lines += "    Quota available"
        } else {
            $lines += "    $remGemini5h% remaining $charBullet Refreshes in $refresh5HStr"
        }
        $lines += ""
        $lines += ""
        $lines += "CLAUDE AND GPT MODELS"
        $lines += "  Models within this group: Claude Opus, Claude Sonnet, GPT-OSS"
        $lines += ""
        $lines += "  Weekly Limit"
        $lines += [AgyAccountManager]::GetProgressBar($meta.ClaudeWeekly)
        $remClaudeWeekly = [int][Math]::Round($meta.ClaudeWeekly)
        if ($meta.ClaudeWeekly -ge 100.0) {
            $lines += "    Quota available"
        } else {
            $lines += "    $remClaudeWeekly% remaining"
        }
        $lines += ""
        $lines += "  Five Hour Limit"
        $lines += [AgyAccountManager]::GetProgressBar($meta.ClaudeFiveHour)
        $remClaude5h = [int][Math]::Round($meta.ClaudeFiveHour)
        if ($meta.ClaudeFiveHour -ge 100.0) {
            $lines += "    Quota available"
        } else {
            $lines += "    $remClaude5h% remaining"
        }
        $lines += ""
        $lines += ""
        $lines += "  $charVertical Within each group, models share a weekly limit and a 5-hour limit. Quota is"
        $lines += "  $charVertical consumed proportionally to the cost of the tokens. Thus, limits will last"
        $lines += "  $charVertical longer with shorter tasks or using more cost-effective models. The 5-hour"
        $lines += "  $charVertical limit smooths out aggregate demand to fairly distribute global capacity"
        $lines += "  $charVertical across all users, while your weekly limit is tied directly to your individual"
        $lines += "  $charVertical tier."

        # Weekly Request Distribution Chart
        $lines += ""
        $lines += "  Weekly Request Distribution (Last 7 Days)"
        $lines += "  ==========================================="
        
        $now = Get-Date
        $dayNames = @("Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat")
        $daysData = @{}
        for ($i = 0; $i -lt 7; $i++) {
            $dayDate = $now.Date.AddDays(-$i)
            $dayName = $dayDate.ToString("ddd")
            $daysData[$dayName] = 0
        }
        
        $history = @()
        if ($meta.RequestHistory) { $history = @($meta.RequestHistory) }
        $dummy = [DateTime]::MinValue
        foreach ($ts in $history) {
            if ($ts -and [DateTime]::TryParse($ts, [ref]$dummy)) {
                $dt = $dummy.ToLocalTime()
                $dayName = $dt.ToString("ddd")
                if ($daysData.ContainsKey($dayName)) {
                    $daysData[$dayName]++
                }
            }
        }
        
        $charFilled = [char]0x2588
        $charEmpty = [char]0x2591
        for ($i = 6; $i -ge 0; $i--) {
            $dayDate = $now.Date.AddDays(-$i)
            $dayName = $dayDate.ToString("ddd")
            $count = $daysData[$dayName]
            $barLen = [int][Math]::Min(10, $count)
            $barStr = [string]$charFilled * $barLen
            $emptyStr = [string]$charEmpty * (10 - $barLen)
            $lines += "  $dayName [$barStr$emptyStr] $count requests"
        }
        
        $lines += "  -------------------------------------------"
        $lines += "  Total Weekly Requests: $($quotaMetrics.CountWeekly) / 1000 limit"

        return $lines
    }


    static [void] SyncActiveAccountWithKeyring([bool]$Silent) {
        if (-not [AgyAccountManager]::IsAutoSwitchEnabled()) { return }
        try {
            $savedAcc = "default"
            if (Test-Path ([AgyAccountManager]::AgyActiveAccountFile)) {
                $content = (Get-Content ([AgyAccountManager]::AgyActiveAccountFile) -ErrorAction SilentlyContinue)
                if ($content) { $savedAcc = $content.Trim() }
            }

            $currentKeyringToken = [AgyKeyringHelper]::ReadToken("gemini:antigravity")
            if ($currentKeyringToken) {
                $matchedAcc = $null
                $availableAccounts = [AgyAccountManager]::GetAccounts()
                foreach ($acc in $availableAccounts) {
                    $accDir = [AgyAccountManager]::GetAccountDirectory($acc)
                    $tokenFile = Join-Path $accDir "keyring_token.txt"
                    if (Test-Path $tokenFile) {
                        $encrypted = (Get-Content -Path $tokenFile -Raw -ErrorAction SilentlyContinue)
                        if ($encrypted) {
                            $encrypted = $encrypted.Trim()
                            $secure = ConvertTo-SecureString $encrypted
                            $networkCred = [System.Net.NetworkCredential]::new("", $secure)
                            $savedToken = $networkCred.Password
                            if ($savedToken -eq $currentKeyringToken) {
                                $matchedAcc = $acc
                                break
                            } else {
                                try {
                                    $savedJson = ConvertFrom-Json $savedToken -ErrorAction SilentlyContinue
                                    $currentJson = ConvertFrom-Json $currentKeyringToken -ErrorAction SilentlyContinue
                                    if ($savedJson.token.refresh_token -and $currentJson.token.refresh_token -and $savedJson.token.refresh_token -eq $currentJson.token.refresh_token) {
                                        $matchedAcc = $acc
                                        break
                                    }
                                } catch {}
                            }
                        }
                    }
                }

                # If no matching offline token was found, try online TokenInfo API lookup using Google OAuth
                if (-not $matchedAcc) {
                    try {
                        if ([AgyAccountManager]::CheckNetworkStatus()) {
                            $json = ConvertFrom-Json $currentKeyringToken -ErrorAction SilentlyContinue
                            $accessToken = $json.token.access_token
                            if ($accessToken) {
                                $tokenInfo = Invoke-RestMethod -Uri "https://oauth2.googleapis.com/tokeninfo?access_token=$accessToken" -TimeoutSec 3 -ErrorAction Stop
                                if ($tokenInfo.email) {
                                    $email = $tokenInfo.email.Trim().ToLower()
                                    foreach ($acc in $availableAccounts) {
                                        if ($acc.Trim().ToLower() -eq $email) {
                                            $matchedAcc = $acc
                                            break
                                        }
                                    }
                                }
                            }
                        }
                    } catch {}
                }

                if ($matchedAcc) {
                    if ($matchedAcc -ne $savedAcc) {
                        if (-not $Silent) {
                            Write-Host "[agy] Keyring matches account '$matchedAcc'. Auto-switching active account." -ForegroundColor Yellow
                        }
                        $savedAcc = $matchedAcc
                        if (-not (Test-Path ([AgyAccountManager]::AgySourceHome))) {
                            $null = New-Item -ItemType Directory -Path ([AgyAccountManager]::AgySourceHome) -Force
                        }
                        $savedAcc | Out-File -FilePath ([AgyAccountManager]::AgyActiveAccountFile) -Force -Encoding utf8
                        $env:GEMINI_HOME = [AgyAccountManager]::GetAccountDirectory($savedAcc)
                    }
                    # Always backup the token to the matched account folder so it is updated and persistent
                    $accDir = [AgyAccountManager]::GetAccountDirectory($savedAcc)
                    if (-not (Test-Path $accDir)) {
                        $null = New-Item -ItemType Directory -Path $accDir -Force
                    }
                    $tokenFile = Join-Path $accDir "keyring_token.txt"
                    $secure = ConvertTo-SecureString $currentKeyringToken -AsPlainText -Force
                    $encrypted = ConvertFrom-SecureString $secure
                    $encrypted | Out-File -FilePath $tokenFile -Force -Encoding utf8
                } else {
                    # No account matched the keyring token. Backup to currently active account.
                    $accDir = [AgyAccountManager]::GetAccountDirectory($savedAcc)
                    if (-not (Test-Path $accDir)) {
                        $null = New-Item -ItemType Directory -Path $accDir -Force
                    }
                    $tokenFile = Join-Path $accDir "keyring_token.txt"
                    $secure = ConvertTo-SecureString $currentKeyringToken -AsPlainText -Force
                    $encrypted = ConvertFrom-SecureString $secure
                    $encrypted | Out-File -FilePath $tokenFile -Force -Encoding utf8
                }
            }
        } catch {}
    }

    static [void] InitializeManager() {
        $isOnline = [AgyAccountManager]::CheckNetworkStatus()
        $onlineTag = if ($isOnline) { "" } else { " [Offline]" }
        if (-not $Global:AiMode -and $Global:VerboseStartup) {
            Write-Host "[agy] Loading Antigravity Multi-Account Manager...$onlineTag" -ForegroundColor Cyan
        }

        # Sync active account with keyring to auto-detect any changes from Agy Desktop
        [AgyAccountManager]::SyncActiveAccountWithKeyring($true)

        # 1. Load active account selection from persistent settings file if available
        $savedAcc = "default"
        if (Test-Path ([AgyAccountManager]::AgyActiveAccountFile)) {
            try {
                $content = (Get-Content ([AgyAccountManager]::AgyActiveAccountFile) -ErrorAction SilentlyContinue)
                if ($content) {
                    $savedAcc = $content.Trim()
                }
            } catch {}
        }

        if ($savedAcc -and $savedAcc -ne "default") {
            $targetPath = [AgyAccountManager]::GetAccountDirectory($savedAcc)
            if (Test-Path $targetPath) {
                $env:GEMINI_HOME = $targetPath
                if (-not $Global:AiMode -and $Global:VerboseStartup) {
                    Write-Host "[agy] Active account configured to '$savedAcc' via persistent settings." -ForegroundColor Cyan
                }
            } else {
                $savedAcc = "default"
            }
        }

        # Sync/restore keyring token for the active account
        try {
            [AgyAccountManager]::RestoreActiveToken($savedAcc)
        } catch {}

        # 2. Dynamically scan all available accounts and generate wrapper functions and indexed aliases
        $availableAccounts = [AgyAccountManager]::GetAccounts()
        $idx = 1
        foreach ($acc in $availableAccounts) {
            if ($acc -ne "default") {
                $accHome = [AgyAccountManager]::GetAccountDirectory($acc)
                [AgyAccountManager]::NewAgyDynamicWrapper($acc, $accHome)

                $aliasShortcut = "agy$idx"
                Set-Alias -Name $aliasShortcut -Value "agy-$acc" -Force -Description "Alias mapping to agy-$acc"
                $idx++
            }
        }

    }

    static [void] InvokeAgy([string[]]$PassThruArgs) {
        # Sync active account with keyring to handle changes since terminal session started
        [AgyAccountManager]::SyncActiveAccountWithKeyring($false)

        # Auto switch if current account has exceeded quota
        [AgyAccountManager]::AutoSwitchOnQuotaExceeded()

        $binPath = [AgyAccountManager]::AgyBinaryPath
        $activeAcc = [AgyAccountManager]::GetActiveAccount()
        # Update usage metadata
        [AgyAccountManager]::UpdateAccountMetadata($activeAcc)
        try {
            if ($activeAcc -ne "default") {
                $oldHome = $env:GEMINI_HOME
                $oldRealHome = $env:HOME
                $oldProfile = $env:USERPROFILE
                $oldToken = $env:GEMINI_CLI_IDE_AUTH_TOKEN
                $oldPort = $env:GEMINI_CLI_IDE_SERVER_PORT
                $oldLsAddress = $env:ANTIGRAVITY_LS_ADDRESS
                $oldCsrfToken = $env:ANTIGRAVITY_CSRF_TOKEN
                $oldProjId = $env:ANTIGRAVITY_PROJECT_ID
                $oldConvId = $env:ANTIGRAVITY_CONVERSATION_ID
                $oldAgent = $env:ANTIGRAVITY_AGENT
                try {
                    $targetHome = $env:GEMINI_HOME
                    $env:HOME = $targetHome
                    $env:USERPROFILE = $targetHome
                    $env:GEMINI_CLI_IDE_AUTH_TOKEN = $null
                    $env:GEMINI_CLI_IDE_SERVER_PORT = $null
                    $env:ANTIGRAVITY_LS_ADDRESS = $null
                    $env:ANTIGRAVITY_CSRF_TOKEN = $null
                    $env:ANTIGRAVITY_PROJECT_ID = $null
                    $env:ANTIGRAVITY_CONVERSATION_ID = $null
                    $env:ANTIGRAVITY_AGENT = $null
                    if ($PassThruArgs) { & $binPath @PassThruArgs | Out-Default } else { & $binPath | Out-Default }
                } finally {
                    $env:GEMINI_HOME = $oldHome
                    $env:HOME = $oldRealHome
                    $env:USERPROFILE = $oldProfile
                    $env:GEMINI_CLI_IDE_AUTH_TOKEN = $oldToken
                    $env:GEMINI_CLI_IDE_SERVER_PORT = $oldPort
                    $env:ANTIGRAVITY_LS_ADDRESS = $oldLsAddress
                    $env:ANTIGRAVITY_CSRF_TOKEN = $oldCsrfToken
                    $env:ANTIGRAVITY_PROJECT_ID = $oldProjId
                    $env:ANTIGRAVITY_CONVERSATION_ID = $oldConvId
                    $env:ANTIGRAVITY_AGENT = $oldAgent
                }
            } else {
                if ($PassThruArgs) { & $binPath @PassThruArgs | Out-Default } else { & $binPath | Out-Default }
            }
        } finally {
            # Auto-save any new token or token refresh on exit
            [AgyAccountManager]::BackupActiveToken()

            # Check if quota exceeded error occurred during execution
            [AgyAccountManager]::CheckQuotaAfterRun($activeAcc)

            # Auto switch if current account has exceeded quota
            [AgyAccountManager]::AutoSwitchOnQuotaExceeded()
        }
    }

    static [void] InvokeMultigravity([string[]]$PassThruArgs) {
        $mgScript = Join-Path $env:USERPROFILE ".local\bin\multigravity.ps1"
        if (Test-Path $mgScript) {
            & $mgScript @PassThruArgs | Out-Default
        } else {
            Write-Error "multigravity script not found at $mgScript"
        }
    }
}


# Auto-initialize account manager
[AgyAccountManager]::InitializeManager()
#endregion

