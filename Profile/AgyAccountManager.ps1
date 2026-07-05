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
            $env:PATH = "$([AgyAccountManager]::GlobalBinDir);$env:PATH"
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

    static [void] SetActiveAccount([string]$AccountName, [bool]$Temporary) {
        if ($AccountName -eq "default") {
            $env:GEMINI_HOME = [AgyAccountManager]::AgySourceHome
            if (-not $Temporary) {
                if (Test-Path ([AgyAccountManager]::AgyActiveAccountFile)) {
                    Remove-Item ([AgyAccountManager]::AgyActiveAccountFile) -Force
                }
            }
            Write-Host "🟢 Switched to default Antigravity account (Primary)." -ForegroundColor Green
            return
        }

        $targetDir = "$([AgyAccountManager]::AgyAccountPrefix)$AccountName"
        if (-not (Test-Path $targetDir)) {
            $userProfilePath = Join-Path $env:USERPROFILE ".gemini_$AccountName"
            if (Test-Path $userProfilePath) {
                $targetDir = $userProfilePath
            } else {
                Write-Error "Account '$AccountName' does not exist. Create it first using 'agy-account add $AccountName'."
                return
            }
        }

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
            Write-Host "⚙️ Re-generated unique installation ID for '$AccountName' to separate credentials." -ForegroundColor Yellow
        }

        $env:GEMINI_HOME = $targetDir
        if (-not $Temporary) {
            if (-not (Test-Path ([AgyAccountManager]::AgySourceHome))) {
                $null = New-Item -ItemType Directory -Path ([AgyAccountManager]::AgySourceHome) -Force
            }
            $AccountName | Out-File -FilePath ([AgyAccountManager]::AgyActiveAccountFile) -Force -Encoding utf8
            Write-Host "🟢 Switched to account '$AccountName' (Persistent)." -ForegroundColor Green
        } else {
            Write-Host "🟡 Switched to account '$AccountName' (Temporary - current session only)." -ForegroundColor Yellow
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
            Write-Error "Source directory $([AgyAccountManager]::AgySourceHome) does not exist. Run 'agy' at least once first."
            return
        }

        $destDir = "$([AgyAccountManager]::AgyAccountPrefix)$AccountName"
        if (Test-Path $destDir) {
            Write-Warning "Account '$AccountName' already exists at $destDir."
            return
        }

        Write-Host "📂 Creating isolated account directory: $destDir" -ForegroundColor Cyan
        $null = New-Item -ItemType Directory -Path $destDir -Force

        $credentialsFiles = @("google_accounts.json", "oauth_creds.json", "state.json", "installation_id")
        Get-ChildItem -Path ([AgyAccountManager]::AgySourceHome) -File | ForEach-Object {
            if ($_.Name -notin $credentialsFiles) {
                $destFile = Join-Path $destDir $_.Name
                Write-Host "📄 Copying root file: $($_.Name) -> $destDir" -ForegroundColor Gray
                Copy-Item -Path $_.FullName -Destination $destFile -Force
            }
        }

        $uniqueId = [guid]::NewGuid().ToString()
        $uniqueId | Out-File -FilePath (Join-Path $destDir "installation_id") -NoNewline -Force -Encoding utf8
        Write-Host "📄 Generated unique installation ID for account '$AccountName'." -ForegroundColor Gray

        $subDirsToShare = @("antigravity", "antigravity-cli", "config", "history", "antigravity-ide", "wf")
        foreach ($subDir in $subDirsToShare) {
            $srcSubPath = Join-Path ([AgyAccountManager]::AgySourceHome) $subDir
            $destSubPath = Join-Path $destDir $subDir
            if (-not (Test-Path $srcSubPath)) {
                $null = New-Item -ItemType Directory -Path $srcSubPath -Force
            }
            Write-Host "🔗 Creating Directory Junction: $destSubPath -> $srcSubPath" -ForegroundColor Green
            $null = New-Item -ItemType Junction -Path $destSubPath -Value $srcSubPath -Force
        }

        # Dynamically load wrapper for the new account immediately
        [AgyAccountManager]::NewAgyDynamicWrapper($AccountName, $destDir)

        Write-Host "✅ Antigravity account '$AccountName' initialized successfully." -ForegroundColor Green
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

        $targetDir = "$([AgyAccountManager]::AgyAccountPrefix)$AccountName"
        if (-not (Test-Path $targetDir)) {
            $userProfilePath = Join-Path $env:USERPROFILE ".gemini_$AccountName"
            if (Test-Path $userProfilePath) {
                $targetDir = $userProfilePath
            } else {
                Write-Error "Account '$AccountName' does not exist."
                return
            }
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
                Write-Host "🔗 Removing Junction link: $($folder.FullName)" -ForegroundColor Yellow
                cmd /c rmdir "$($folder.FullName)"
            }
        }

        Write-Host "🗑 Deleting directory: $targetDir" -ForegroundColor Red
        Remove-Item -Path $targetDir -Recurse -Force

        $activeAcc = [AgyAccountManager]::GetActiveAccount()
        if ($activeAcc -eq $AccountName) {
            Write-Host "⚠️ Active account was deleted. Reverting to default." -ForegroundColor Warning
            [AgyAccountManager]::SetActiveAccount("default", $false)
        }

        Write-Host "✅ Account '$AccountName' removed successfully." -ForegroundColor Green
    }

    static [void] ResetCredentials([string]$AccountName) {
        $targetHome = ""
        if ($AccountName -eq "default") {
            $targetHome = [AgyAccountManager]::AgySourceHome
        } else {
            $targetHome = "$([AgyAccountManager]::AgyAccountPrefix)$AccountName"
        }
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
        if ($clearedAny) {
            Write-Host "✅ Credentials cleared for account '$AccountName'. Next run will prompt for login." -ForegroundColor Green
        } else {
            Write-Host "ℹ️ Account '$AccountName' is already signed out." -ForegroundColor Cyan
        }
    }

    static [void] InvokeWithAccount([string]$AccountName, [string[]]$ArgsList) {
        $targetHome = ""
        if ($AccountName -eq "default") {
            $targetHome = [AgyAccountManager]::AgySourceHome
        } else {
            $targetHome = "$([AgyAccountManager]::AgyAccountPrefix)$AccountName"
            if (-not (Test-Path $targetHome)) {
                $userProfilePath = Join-Path $env:USERPROFILE ".gemini_$AccountName"
                if (Test-Path $userProfilePath) {
                    $targetHome = $userProfilePath
                }
            }
        }
        if (-not (Test-Path $targetHome)) {
            Write-Error "Account '$AccountName' does not exist."
            return
        }

        $oldHome = $env:GEMINI_HOME
        $oldToken = $env:GEMINI_CLI_IDE_AUTH_TOKEN
        $oldPort = $env:GEMINI_CLI_IDE_SERVER_PORT

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
                Write-Host "⚙️ Re-generated unique installation ID for '$AccountName' to separate credentials." -ForegroundColor Yellow
            }
        }

        try {
            $env:GEMINI_HOME = $targetHome
            if ($AccountName -ne "default") {
                $env:GEMINI_CLI_IDE_AUTH_TOKEN = $null
                $env:GEMINI_CLI_IDE_SERVER_PORT = $null
            }

            if (-not (Test-Path ([AgyAccountManager]::AgyBinaryPath))) {
                Write-Error "Antigravity CLI executable not found at $([AgyAccountManager]::AgyBinaryPath)."
                return
            }

            if ($ArgsList) {
                & ([AgyAccountManager]::AgyBinaryPath) @ArgsList
            } else {
                & ([AgyAccountManager]::AgyBinaryPath)
            }
        } finally {
            $env:GEMINI_HOME = $oldHome
            $env:GEMINI_CLI_IDE_AUTH_TOKEN = $oldToken
            $env:GEMINI_CLI_IDE_SERVER_PORT = $oldPort
        }
    }

    static [void] ShowAccounts() {
        $accounts = [AgyAccountManager]::GetAccounts()
        $active = [AgyAccountManager]::GetActiveAccount()

        Write-Host ""
        Write-Host "  Antigravity Accounts:" -ForegroundColor Cyan
        Write-Host "  =====================" -ForegroundColor Cyan

        foreach ($acc in $accounts) {
            $path = if ($acc -eq "default") { [AgyAccountManager]::AgySourceHome } else { "$([AgyAccountManager]::AgyAccountPrefix)$acc" }
            if ($acc -eq $active) {
                Write-Host "  ▶  $("{0,-15}" -f $acc) ($path)" -ForegroundColor Green
            } else {
                Write-Host "     $("{0,-15}" -f $acc) ($path)" -ForegroundColor DarkGray
            }
        }
        Write-Host ""
    }

    static [void] NewAgyDynamicWrapper([string]$AccountName, [string]$GeminiHomePath) {
        $funcName = "agy-$AccountName"
        $scriptBlock = [ScriptBlock]::Create(@"
            param([Parameter(ValueFromRemainingArguments=`$true)][string[]]`$PassThruArgs)
            `$oldHome = `$env:GEMINI_HOME
            `$oldToken = `$env:GEMINI_CLI_IDE_AUTH_TOKEN
            `$oldPort = `$env:GEMINI_CLI_IDE_SERVER_PORT
            try {
                `$env:GEMINI_HOME = "$GeminiHomePath"
                `$env:GEMINI_CLI_IDE_AUTH_TOKEN = `$null
                `$env:GEMINI_CLI_IDE_SERVER_PORT = `$null
                if (`$PassThruArgs) { & "$([AgyAccountManager]::AgyBinaryPath)" @PassThruArgs } else { & "$([AgyAccountManager]::AgyBinaryPath)" }
            } finally {
                `$env:GEMINI_HOME = `$oldHome
                `$env:GEMINI_CLI_IDE_AUTH_TOKEN = `$oldToken
                `$env:GEMINI_CLI_IDE_SERVER_PORT = `$oldPort
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
        $menuItems += "➕ Add New Account"
        $menuItems += "❌ Delete Account"
        $menuItems += "🚪 Cancel / Exit"

        $selected = ([type]"TerminalMenu")::Show("Select Antigravity Account", $menuItems, $defaultIdx)

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
            Write-Host ""
            Write-Host "❌ Select Account to Delete:" -ForegroundColor Red
            for ($i = 0; $i -lt $deletable.Count; $i++) {
                Write-Host "  [$($i + 1)] $($deletable[$i])" -ForegroundColor Gray
            }
            $delChoice = Read-Host "Select account to delete [1-$($deletable.Count)]"
            $dIdx = 0
            if ([int]::TryParse($delChoice, [ref]$dIdx) -and $dIdx -ge 1 -and $dIdx -le $deletable.Count) {
                [AgyAccountManager]::RemoveAccount($deletable[$dIdx - 1])
            } else {
                Write-Error "Invalid selection."
            }
        }
    }

    static [void] InitializeManager() {
        Write-Host "🛸 Loading Antigravity Multi-Account Manager..." -ForegroundColor Cyan

        # 1. Load active account selection from persistent settings file if available
        if (Test-Path [AgyAccountManager]::AgyActiveAccountFile) {
            try {
                $savedAcc = (Get-Content [AgyAccountManager]::AgyActiveAccountFile -ErrorAction SilentlyContinue).Trim()
                if ($savedAcc -and $savedAcc -ne "default") {
                    $targetPath = "$([AgyAccountManager]::AgyAccountPrefix)$savedAcc"
                    if (-not (Test-Path $targetPath)) {
                        $userProfilePath = Join-Path $env:USERPROFILE ".gemini_$savedAcc"
                        if (Test-Path $userProfilePath) {
                            $targetPath = $userProfilePath
                        }
                    }
                    if (Test-Path $targetPath) {
                        $env:GEMINI_HOME = $targetPath
                        Write-Host "🛸 Active account configured to '$savedAcc' via persistent settings." -ForegroundColor Cyan
                    }
                }
            } catch {}
        }

        # 2. Dynamically scan all available accounts and generate wrapper functions and indexed aliases
        $availableAccounts = [AgyAccountManager]::GetAccounts()
        $idx = 1
        foreach ($acc in $availableAccounts) {
            if ($acc -ne "default") {
                $accHome = "$([AgyAccountManager]::AgyAccountPrefix)$acc"
                if (-not (Test-Path $accHome)) {
                    $userProfilePath = Join-Path $env:USERPROFILE ".gemini_$acc"
                    if (Test-Path $userProfilePath) {
                        $accHome = $userProfilePath
                    }
                }
                [AgyAccountManager]::NewAgyDynamicWrapper($acc, $accHome)

                $aliasShortcut = "agy$idx"
                Set-Alias -Name $aliasShortcut -Value "agy-$acc" -Force -Description "Alias mapping to agy-$acc"
                $idx++
            }
        }
    }

    static [void] InvokeAgy([string[]]$PassThruArgs) {
        $activeAcc = [AgyAccountManager]::GetActiveAccount()
        if ($activeAcc -ne "default") {
            $oldToken = $env:GEMINI_CLI_IDE_AUTH_TOKEN
            $oldPort = $env:GEMINI_CLI_IDE_SERVER_PORT
            try {
                $env:GEMINI_CLI_IDE_AUTH_TOKEN = $null
                $env:GEMINI_CLI_IDE_SERVER_PORT = $null
                if ($PassThruArgs) { & ([AgyAccountManager]::AgyBinaryPath) @PassThruArgs } else { & ([AgyAccountManager]::AgyBinaryPath) }
            } finally {
                $env:GEMINI_CLI_IDE_AUTH_TOKEN = $oldToken
                $env:GEMINI_CLI_IDE_SERVER_PORT = $oldPort
            }
        } else {
            if ($PassThruArgs) { & ([AgyAccountManager]::AgyBinaryPath) @PassThruArgs } else { & ([AgyAccountManager]::AgyBinaryPath) }
        }
    }

    static [void] InvokeMultigravity([string[]]$PassThruArgs) {
        $mgScript = Join-Path $env:USERPROFILE ".local\bin\multigravity.ps1"
        if (Test-Path $mgScript) {
            & $mgScript @PassThruArgs
        } else {
            Write-Error "multigravity script not found at $mgScript"
        }
    }
}

# Auto-initialize account manager
[AgyAccountManager]::InitializeManager()
#endregion
