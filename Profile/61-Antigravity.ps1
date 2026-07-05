#region ANTIGRAVITY MULTI-ACCOUNT MANAGER
# ------------------------------------------------------------------------------
#  Dynamic Multi-Account Manager for Antigravity (agy) CLI.
#  Isolates credentials, tokens, settings, and browser profiles while
#  sharing context, history, skills, and agents via directory junctions.
# ------------------------------------------------------------------------------

Write-Host "🛸 Loading Antigravity Multi-Account Manager..." -ForegroundColor Cyan

# --- Core Configurations ---
$script:AgySourceHome = "C:\Users\Public\.gemini"
$script:AgyAccountPrefix = "C:\Users\Public\.gemini_"
$script:AgyActiveAccountFile = Join-Path $script:AgySourceHome "active_account.txt"
$script:AgyBinaryPath = "C:\ProgramData\agy\bin\agy.exe"

# Dynamically add global agy binary directory to PATH for the current session
$globalBinDir = "C:\ProgramData\agy\bin"
if ($env:PATH -split ';' -notcontains $globalBinDir) {
    $env:PATH = "$globalBinDir;$env:PATH"
}

# --- Helper Functions ---

<#
.SYNOPSIS
Scan the home directory and return a list of all initialized Antigravity accounts.
.CATEGORY
AI Tools
#>
function Get-AgyAccounts {
    [CmdletBinding()]
    param()

    $accounts = [System.Collections.Generic.List[string]]::new()
    $accounts.Add("default")

    $scanPaths = @()
    if (Test-Path $env:USERPROFILE) { $scanPaths += $env:USERPROFILE }
    $parentDir = Split-Path $script:AgyAccountPrefix -Parent
    if ((Test-Path $parentDir) -and ($parentDir -notin $scanPaths)) { $scanPaths += $parentDir }

    foreach ($path in $scanPaths) {
        $dirs = Get-ChildItem -Path $path -Directory -Filter ".gemini_*" -ErrorAction SilentlyContinue
        foreach ($dir in $dirs) {
            if ($dir.Name -match '^\.gemini_(.+)$') {
                $name = $Matches[1]
                # Filter out system/backup folder patterns
                if ($name -notmatch 'backup|copy|temp' -and $name -notin $accounts) {
                    $accounts.Add($name)
                }
            }
        }
    }
    return $accounts
}

<#
.SYNOPSIS
Retrieve the name of the currently active Antigravity account.
.CATEGORY
AI Tools
#>
function Get-AgyActiveAccount {
    [CmdletBinding()]
    param()

    if ($env:GEMINI_HOME -and $env:GEMINI_HOME -match '\.gemini_(.+)$') {
        return $Matches[1]
    }
    return "default"
}

<#
.SYNOPSIS
Switch the active Antigravity account for either the current session or persistently.
.CATEGORY
AI Tools
#>
function Set-AgyActiveAccount {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, Position=0)]
        [string]$AccountName,

        [Parameter()]
        [switch]$Temporary
    )

    if ($AccountName -eq "default") {
        $env:GEMINI_HOME = $script:AgySourceHome
        if (-not $Temporary) {
            if (Test-Path $script:AgyActiveAccountFile) {
                Remove-Item $script:AgyActiveAccountFile -Force
            }
        }
        Write-Host "🟢 Switched to default Antigravity account (Primary)." -ForegroundColor Green
        return
    }

    $targetDir = "$($script:AgyAccountPrefix)$AccountName"
    if (-not (Test-Path $targetDir)) {
        $userProfilePath = Join-Path $env:USERPROFILE ".gemini_$AccountName"
        if (Test-Path $userProfilePath) {
            $targetDir = $userProfilePath
        } else {
            Write-Error "Account '$AccountName' does not exist. Create it first using 'agy-account add $AccountName'."
            return
        }
    }

    # Self-healing: Ensure the account has a unique installation_id to isolate credentials
    $defaultIdFile = Join-Path $script:AgySourceHome "installation_id"
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
        # Ensure root directory exists
        if (-not (Test-Path $script:AgySourceHome)) {
            $null = New-Item -ItemType Directory -Path $script:AgySourceHome -Force
        }
        $AccountName | Out-File -FilePath $script:AgyActiveAccountFile -Force -Encoding utf8
        Write-Host "🟢 Switched to account '$AccountName' (Persistent)." -ForegroundColor Green
    } else {
        Write-Host "🟡 Switched to account '$AccountName' (Temporary - current session only)." -ForegroundColor Yellow
    }
}

<#
.SYNOPSIS
Create a new isolated Antigravity account with shared context junctions.
.CATEGORY
AI Tools
#>
function Add-AgyAccount {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, Position=0)]
        [string]$AccountName
    )

    if ([string]::IsNullOrWhiteSpace($AccountName)) {
        Write-Error "Account name cannot be empty."
        return
    }

    if ($AccountName -eq "default") {
        Write-Error "Cannot create an account named 'default'. It is reserved for the primary account."
        return
    }

    if (-not (Test-Path $script:AgySourceHome)) {
        Write-Error "Source directory $script:AgySourceHome does not exist. Please run 'agy' at least once first."
        return
    }

    $destDir = "$($script:AgyAccountPrefix)$AccountName"
    if (Test-Path $destDir) {
        Write-Warning "Account '$AccountName' already exists at $destDir."
        return
    }

    Write-Host "📂 Creating isolated account directory: $destDir" -ForegroundColor Cyan
    $null = New-Item -ItemType Directory -Path $destDir -Force

    # Copy root configuration and token files (exclude directories and credentials/IDs to prevent sharing key)
    $credentialsFiles = @("google_accounts.json", "oauth_creds.json", "state.json", "installation_id")
    Get-ChildItem -Path $script:AgySourceHome -File | ForEach-Object {
        if ($_.Name -notin $credentialsFiles) {
            $destFile = Join-Path $destDir $_.Name
            Write-Host "📄 Copying root file: $($_.Name) -> $destDir" -ForegroundColor Gray
            Copy-Item -Path $_.FullName -Destination $destFile -Force
        }
    }

    # Generate a unique installation_id for this isolated account context to separate Windows Credential Manager keys
    $uniqueId = [guid]::NewGuid().ToString()
    $uniqueId | Out-File -FilePath (Join-Path $destDir "installation_id") -NoNewline -Force -Encoding utf8
    Write-Host "📄 Generated unique installation ID for account '$AccountName'." -ForegroundColor Gray

    # Key subdirectories to share context while maintaining separate account cookies
    $subDirsToShare = @(
        "antigravity",
        "antigravity-cli",
        "config",
        "history",
        "antigravity-ide",
        "wf"
    )

    foreach ($subDir in $subDirsToShare) {
        $srcSubPath = Join-Path $script:AgySourceHome $subDir
        $destSubPath = Join-Path $destDir $subDir

        # Pre-create source subdirectory if it doesn't exist
        if (-not (Test-Path $srcSubPath)) {
            $null = New-Item -ItemType Directory -Path $srcSubPath -Force
        }

        Write-Host "🔗 Creating Directory Junction: $destSubPath -> $srcSubPath" -ForegroundColor Green
        $null = New-Item -ItemType Junction -Path $destSubPath -Value $srcSubPath -Force
    }

    Write-Host "✅ Antigravity account '$AccountName' initialized successfully." -ForegroundColor Green
}

<#
.SYNOPSIS
Safely delete an Antigravity account directory and its junctions.
.CATEGORY
AI Tools
#>
function Remove-AgyAccount {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, Position=0)]
        [string]$AccountName
    )

    if ([string]::IsNullOrWhiteSpace($AccountName)) {
        Write-Error "Account name cannot be empty."
        return
    }

    if ($AccountName -eq "default") {
        Write-Error "Cannot remove the 'default' account."
        return
    }

    $targetDir = "$($script:AgyAccountPrefix)$AccountName"
    if (-not (Test-Path $targetDir)) {
        $userProfilePath = Join-Path $env:USERPROFILE ".gemini_$AccountName"
        if (Test-Path $userProfilePath) {
            $targetDir = $userProfilePath
        } else {
            Write-Error "Account '$AccountName' does not exist."
            return
        }
    }

    # Interactively prompt for deletion verification
    $choice = Read-Host "Are you sure you want to delete account '$AccountName'? This will erase settings and login sessions. (y/N)"
    if ($choice -ne "y" -and $choice -ne "yes") {
        Write-Host "Deletion cancelled." -ForegroundColor Yellow
        return
    }

    # Remove junction folders safely using cmd /c rmdir to avoid deleting target contents
    $subfolders = Get-ChildItem -Path $targetDir -Directory
    foreach ($folder in $subfolders) {
        $isJunction = (Get-Item $folder.FullName).LinkType -eq "Junction"
        if ($isJunction) {
            Write-Host "🔗 Removing Junction link: $($folder.FullName)" -ForegroundColor Yellow
            cmd /c rmdir "$($folder.FullName)"
        }
    }

    # Recursively delete the remaining isolated files and base directory
    Write-Host "🗑 Deleting directory: $targetDir" -ForegroundColor Red
    Remove-Item -Path $targetDir -Recurse -Force

    # Fallback if active account was deleted
    $activeAcc = Get-AgyActiveAccount
    if ($activeAcc -eq $AccountName) {
        Write-Host "⚠️ Active account was deleted. Reverting to default." -ForegroundColor Warning
        Set-AgyActiveAccount "default"
    }

    Write-Host "✅ Account '$AccountName' removed successfully." -ForegroundColor Green
}

<#
.SYNOPSIS
Execute the agy command temporarily under the context of a specific account.
.CATEGORY
AI Tools
#>
function Invoke-AgyWithAccount {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, Position=0)]
        [string]$AccountName,

        [Parameter(Position=1, ValueFromRemainingArguments=$true)]
        [string[]]$Args
    )

        $targetHome = ""
    if ($AccountName -eq "default") {
        $targetHome = $script:AgySourceHome
    } else {
        $targetHome = "$($script:AgyAccountPrefix)$AccountName"
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

    # Self-healing: Ensure the account has a unique installation_id to isolate credentials
    if ($AccountName -ne "default") {
        $defaultIdFile = Join-Path $script:AgySourceHome "installation_id"
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
        # Clear IDE variables if using a non-default/secondary account to force standalone execution
        if ($AccountName -ne "default") {
            $env:GEMINI_CLI_IDE_AUTH_TOKEN = $null
            $env:GEMINI_CLI_IDE_SERVER_PORT = $null
        }

        if (-not (Test-Path $script:AgyBinaryPath)) {
            Write-Error "Antigravity CLI executable not found at $script:AgyBinaryPath."
            return
        }

        if ($Args) {
            & $script:AgyBinaryPath @Args
        } else {
            & $script:AgyBinaryPath
        }
    } finally {
        $env:GEMINI_HOME = $oldHome
        $env:GEMINI_CLI_IDE_AUTH_TOKEN = $oldToken
        $env:GEMINI_CLI_IDE_SERVER_PORT = $oldPort
    }
}

<#
.SYNOPSIS
Display all registered Antigravity accounts, highlighting the currently active one.
.CATEGORY
AI Tools
#>
function Show-AgyAccounts {
    [CmdletBinding()]
    param()

    $accounts = Get-AgyAccounts
    $active = Get-AgyActiveAccount

    Write-Host ""
    Write-Host "  Antigravity Accounts:" -ForegroundColor Cyan
    Write-Host "  =====================" -ForegroundColor Cyan
    
    foreach ($acc in $accounts) {
        $path = if ($acc -eq "default") { $script:AgySourceHome } else { "$($script:AgyAccountPrefix)$acc" }
        if ($acc -eq $active) {
            Write-Host "  ▶  $("{0,-15}" -f $acc) ($path)" -ForegroundColor Green
        } else {
            Write-Host "     $("{0,-15}" -f $acc) ($path)" -ForegroundColor DarkGray
        }
    }
    Write-Host ""
}

# --- Dynamic Wrapper Generator Helpers ---

function New-AgyDynamicWrapper {
    param(
        [string]$AccountName,
        [string]$GeminiHomePath
    )

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
            if (`$PassThruArgs) { & "$script:AgyBinaryPath" @PassThruArgs } else { & "$script:AgyBinaryPath" }
        } finally {
            `$env:GEMINI_HOME = `$oldHome
            `$env:GEMINI_CLI_IDE_AUTH_TOKEN = `$oldToken
            `$env:GEMINI_CLI_IDE_SERVER_PORT = `$oldPort
        }
"@
    )
    Set-Item -Path "Function:\global:$funcName" -Value $scriptBlock -Force
}

<#
.SYNOPSIS
Sign out of an Antigravity account by removing its credential files.
.CATEGORY
AI Tools
#>
function Reset-AgyCredentials {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, Position=0)]
        [string]$AccountName
    )

        $targetHome = ""
    if ($AccountName -eq "default") {
        $targetHome = $script:AgySourceHome
    } else {
        $targetHome = "$($script:AgyAccountPrefix)$AccountName"
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

# --- Unified CLI Command ---

<#
.SYNOPSIS
Manage and execute Antigravity CLI configurations across multiple isolated accounts.
.EXAMPLE
  agy-account list
  agy-account use acc1
  agy-account add acc3
  agy-account remove acc3
  agy-account run acc1 --help
  agy-account -List
  agy-account -Use acc1 -Temporary
.CATEGORY
AI Tools
#>
function Select-AgyAccountInteractive {
    [CmdletBinding()]
    param()

    $accounts = Get-AgyAccounts
    $active = Get-AgyActiveAccount

    # Build menu items
    $menuItems = @()
    for ($i = 0; $i -lt $accounts.Count; $i++) {
        $acc = $accounts[$i]
        $status = if ($acc -eq $active) { "(Active)" } else { "" }
        $menuItems += "$($acc) $status"
    }
    $menuItems += "➕ Add New Account"
    $menuItems += "❌ Delete Account"
    $menuItems += "🚪 Cancel / Exit"

    $currentIndex = 0
    # Find active account index to start highlight there
    for ($i = 0; $i -lt $accounts.Count; $i++) {
        if ($accounts[$i] -eq $active) {
            $currentIndex = $i
            break
        }
    }

    # Hide cursor
    $oldCursorVisible = $null
    try {
        $oldCursorVisible = [Console]::CursorVisible
        [Console]::CursorVisible = $false
    } catch {}

    try {
        while ($true) {
            Clear-Host
            Write-Host ""
            Write-Host "🛸 Select Antigravity Account:" -ForegroundColor Cyan
            Write-Host "=============================" -ForegroundColor Cyan
            
            for ($i = 0; $i -lt $menuItems.Count; $i++) {
                if ($i -eq $currentIndex) {
                    Write-Host "  ▶  $($menuItems[$i])" -ForegroundColor Green
                } else {
                    Write-Host "     $($menuItems[$i])" -ForegroundColor Gray
                }
            }
            Write-Host ""
            Write-Host "Use Arrow Keys [↑/↓] to navigate, [Enter] to select, [Esc] to exit." -ForegroundColor DarkGray

            $key = $null
            try {
                $key = [Console]::ReadKey($true)
            } catch {
                # Non-interactive fallback: ask for input text
                [Console]::CursorVisible = $true
                $choice = Read-Host "Select index [1-$($menuItems.Count)]"
                if ([int]::TryParse($choice, [ref]$val) -and $val -ge 1 -and $val -le $menuItems.Count) {
                    $currentIndex = $val - 1
                    break
                }
                return
            }

            if ($key.Key -eq [ConsoleKey]::UpArrow) {
                $currentIndex = ($currentIndex - 1 + $menuItems.Count) % $menuItems.Count
            }
            elseif ($key.Key -eq [ConsoleKey]::DownArrow) {
                $currentIndex = ($currentIndex + 1) % $menuItems.Count
            }
            elseif ($key.Key -eq [ConsoleKey]::Enter) {
                break
            }
            elseif ($key.Key -eq [ConsoleKey]::Escape) {
                return
            }
        }
    } finally {
        try {
            if ($null -ne $oldCursorVisible) {
                [Console]::CursorVisible = $oldCursorVisible
            }
        } catch {}
    }

    # Process selection
    if ($currentIndex -lt $accounts.Count) {
        Set-AgyActiveAccount -AccountName $accounts[$currentIndex]
    }
    elseif ($currentIndex -eq $accounts.Count) {
        Write-Host ""
        $name = Read-Host "Enter new account name"
        if (-not [string]::IsNullOrWhiteSpace($name)) {
            Add-AgyAccount -AccountName $name
        }
    }
    elseif ($currentIndex -eq ($accounts.Count + 1)) {
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
        if ([int]::TryParse($delChoice, [ref]$dIdx) -and $dIdx -ge 1 -and $dIdx -le $deletable.Count) {
            Remove-AgyAccount -AccountName $deletable[$dIdx - 1]
        } else {
            Write-Error "Invalid selection."
        }
    }
}

function Invoke-AgyAccount {
    [CmdletBinding()]
    param(
        [Parameter(Position=0)]
        [ValidateSet("list", "lst", "show", "use", "add", "create", "remove", "delete", "del", "run", "exec", "logout", "signout", "login", "signin", "interactive", "select")]
        [string]$Action = "interactive",

        [Parameter(Position=1)]
        [string]$Account,

        [Parameter(Position=2, ValueFromRemainingArguments=$true)]
        [string[]]$ExtraArgs,

        [Parameter()][switch]$Temporary
    )

    switch ($Action) {
        "interactive" {
            Select-AgyAccountInteractive
        }
        "select" {
            Select-AgyAccountInteractive
        }
        { $_ -in "list", "lst", "show" } {
            Show-AgyAccounts
        }
        "use" {
            if ([string]::IsNullOrWhiteSpace($Account)) {
                Select-AgyAccountInteractive
                return
            }
            Set-AgyActiveAccount -AccountName $Account -Temporary:$Temporary
        }
        { $_ -in "add", "create" } {
            if ([string]::IsNullOrWhiteSpace($Account)) {
                Write-Error "Please specify an account name to add."
                return
            }
            Add-AgyAccount -AccountName $Account
        }
        { $_ -in "remove", "delete", "del" } {
            if ([string]::IsNullOrWhiteSpace($Account)) {
                Write-Error "Please specify an account name to remove."
                return
            }
            Remove-AgyAccount -AccountName $Account
        }
        { $_ -in "logout", "signout" } {
            $target = if ($Account) { $Account } else { Get-AgyActiveAccount }
            Reset-AgyCredentials -AccountName $target
        }
        { $_ -in "login", "signin" } {
            $target = if ($Account) { $Account } else { Get-AgyActiveAccount }
            Reset-AgyCredentials -AccountName $target
            Invoke-AgyWithAccount -AccountName $target
        }
        { $_ -in "run", "exec" } {
            if ([string]::IsNullOrWhiteSpace($Account)) {
                Write-Error "Please specify an account name to run under."
                return
            }
            Invoke-AgyWithAccount -AccountName $Account -Args $ExtraArgs
        }
    }
}

# --- Aliases ---
Set-Alias -Name agy-account -Value Invoke-AgyAccount -Force
Set-Alias -Name agy-acc     -Value Invoke-AgyAccount -Force

# --- Wrapper for standard 'agy' call ---
<#
.SYNOPSIS
Wrapper for the standard 'agy' command to ensure environment variables are safely isolated when running under secondary accounts.
.CATEGORY
AI Tools
#>
function agy {
    [CmdletBinding()]
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$PassThruArgs)

    $activeAcc = Get-AgyActiveAccount
    if ($activeAcc -ne "default") {
        # Force standalone: temporarily bypass IDE variables
        $oldToken = $env:GEMINI_CLI_IDE_AUTH_TOKEN
        $oldPort = $env:GEMINI_CLI_IDE_SERVER_PORT
        try {
            $env:GEMINI_CLI_IDE_AUTH_TOKEN = $null
            $env:GEMINI_CLI_IDE_SERVER_PORT = $null
            if ($PassThruArgs) { & $script:AgyBinaryPath @PassThruArgs } else { & $script:AgyBinaryPath }
        } finally {
            $env:GEMINI_CLI_IDE_AUTH_TOKEN = $oldToken
            $env:GEMINI_CLI_IDE_SERVER_PORT = $oldPort
        }
    } else {
        # Run standard with active environment
        if ($PassThruArgs) { & $script:AgyBinaryPath @PassThruArgs } else { & $script:AgyBinaryPath }
    }
}

# --- Startup Initialization ---

# 1. Load active account selection from persistent settings file if available
if (Test-Path $script:AgyActiveAccountFile) {
    try {
        $savedAcc = (Get-Content $script:AgyActiveAccountFile -ErrorAction SilentlyContinue).Trim()
        if ($savedAcc -and $savedAcc -ne "default") {
            $targetPath = "$($script:AgyAccountPrefix)$savedAcc"
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
$availableAccounts = Get-AgyAccounts
$idx = 1
foreach ($acc in $availableAccounts) {
    if ($acc -ne "default") {
        $accHome = "$($script:AgyAccountPrefix)$acc"
        if (-not (Test-Path $accHome)) {
            $userProfilePath = Join-Path $env:USERPROFILE ".gemini_$acc"
            if (Test-Path $userProfilePath) {
                $accHome = $userProfilePath
            }
        }
        # Generate agy-<name> function
        New-AgyDynamicWrapper -AccountName $acc -GeminiHomePath $accHome
        
        # Generate agy<index> alias mapping to agy-<name>
        $aliasShortcut = "agy$idx"
        Set-Alias -Name $aliasShortcut -Value "agy-$acc" -Force -Description "Alias mapping to agy-$acc"
        $idx++
    }
}

# 3. Multigravity command wrapper
function multigravity {
    [CmdletBinding()]
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$PassThruArgs)
    $mgScript = Join-Path $env:USERPROFILE ".local\bin\multigravity.ps1"; if (Test-Path $mgScript) { & $mgScript @PassThruArgs } else { Write-Error "multigravity script not found at $mgScript" }
}

# 4. Register Multigravity tab-completion if available
if (Get-Command multigravity -ErrorAction SilentlyContinue) {
    Register-ArgumentCompleter -Native -CommandName multigravity -ScriptBlock {
        param($wordToComplete, $commandAst, $cursorPosition)
        $opts = @('new', 'list', 'status', 'rename', 'delete', 'clone', 'template', 'export', 'import', 'update', 'doctor', 'stats', 'completion', 'help')
        $profiles = if (Test-Path (Join-Path $env:USERPROFILE "AntigravityProfiles")) { Get-ChildItem -Directory -Path (Join-Path $env:USERPROFILE "AntigravityProfiles") | Select-Object -ExpandProperty Name } else { @() }
        ($opts + $profiles) | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
            [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
        }
    }
}

#endregion
