[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

Write-Host "==================================================" -ForegroundColor Red
Write-Host " 💣 NUKING & RE-CREATING ALL 5 AGY ACCOUNTS " -ForegroundColor Red
Write-Host "==================================================" -ForegroundColor Red

# 1. Stop any background AgyTui / agy processes
Write-Host "🛑 Stopping running processes..." -ForegroundColor Yellow
Get-Process AgyTui, agy, antigravity -ErrorAction SilentlyContinue | Stop-Process -Force

# 2. Delete ALL Windows Credential Manager entries matching gemini, antigravity, or agy
Write-Host "💣 Clearing Windows Credential Manager tokens..." -ForegroundColor Yellow
try {
    $cmdOutput = cmdkey /list 2>&1
    if ($cmdOutput) {
        $targets = $cmdOutput | Where-Object { $_ -match "Target:\s*(.*)" } | ForEach-Object {
            $matches[1].Trim()
        } | Where-Object { $_ -match "gemini|antigravity|agy" }

        foreach ($t in $targets) {
            cmdkey /delete:$t | Out-Null
            cmdkey /delete:LegacyGeneric:target=$t | Out-Null
            Write-Host "   Deleted credential target: $t" -ForegroundColor Gray
        }
    }
} catch {
    try { cmdkey /delete:gemini:antigravity | Out-Null } catch {}
    try { cmdkey /delete:LegacyGeneric:target=gemini:antigravity | Out-Null } catch {}
}

# 3. Reset AGY Account DB cache (Clearing accounts and auth state)
$dbPaths = @(
    "$env:USERPROFILE\.gemini\agytui.db",
    "$env:USERPROFILE\.gemini\agytui.dev.db",
    "$env:APPDATA\AgyTui\agytui.db",
    "$env:APPDATA\AgyTui\agytui.dev.db",
    "$env:LOCALAPPDATA\AgyTui\accounts.db",
    "C:\Users\TruongNhon\AppData\Local\AgyTui\accounts.db",
    (Join-Path $PSScriptRoot "csapp\AgyTui\accounts.db")
)

foreach ($db in $dbPaths) {
    if (Test-Path $db) {
        try {
            $connStr = "Data Source=$db"
            $conn = [Microsoft.Data.Sqlite.SqliteConnection]::new($connStr)
            $conn.Open()
            $cmd = $conn.CreateCommand()
            $cmd.CommandText = "DELETE FROM accounts;"
            $cmd.ExecuteNonQuery() | Out-Null

            # Clear any token/auth related entries in system_state if present
            try {
                $cmdState = $conn.CreateCommand()
                $cmdState.CommandText = "DELETE FROM system_state WHERE state_key LIKE '%token%' OR state_key LIKE '%auth%' OR state_key LIKE '%account%';"
                $cmdState.ExecuteNonQuery() | Out-Null
            } catch {}

            $conn.Close()
            Write-Host "🧹 Cleared accounts table in DB: $db" -ForegroundColor Yellow
        } catch {
            Write-Host "ℹ️ DB cache ready: $db" -ForegroundColor Gray
        }
    }
}

# 4. Clean up primary and account keyring tokens and remove old account folders
Write-Host "💣 Wiping credential files and account directories..." -ForegroundColor Yellow
$authFileNames = @("keyring_token.txt", "oauth_creds.json", "state.json", "installation_id", "session.json", "auth.json")

$searchRoots = @($env:USERPROFILE, "C:\Users\Public")
if ($env:GEMINI_HOME -and (Test-Path $env:GEMINI_HOME)) {
    $searchRoots += (Split-Path $env:GEMINI_HOME -Parent)
}

foreach ($root in ($searchRoots | Select-Object -Unique)) {
    if (-not (Test-Path $root)) { continue }

    # Purge auth files inside any .gemini* directory
    $geminiDirs = Get-ChildItem -Path $root -Filter ".gemini*" -Directory -ErrorAction SilentlyContinue
    foreach ($d in $geminiDirs) {
        foreach ($af in $authFileNames) {
            $p1 = Join-Path $d.FullName $af
            if (Test-Path $p1) { Remove-Item -Path $p1 -Force -ErrorAction SilentlyContinue }
            $p2 = Join-Path $d.FullName "antigravity-cli\$af"
            if (Test-Path $p2) { Remove-Item -Path $p2 -Force -ErrorAction SilentlyContinue }
        }
    }

    # Delete all account directories (.gemini_*)
    $strayDirs = Get-ChildItem -Path $root -Filter ".gemini_*" -Directory -ErrorAction SilentlyContinue
    foreach ($sd in $strayDirs) {
        try {
            Remove-Item -Path $sd.FullName -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "💣 Removed account folder: $($sd.FullName)" -ForegroundColor Red
        } catch {}
    }
}

# Completely wipe primary .gemini directory to avoid leftover auth files
$primaryDir = Join-Path $env:USERPROFILE ".gemini"
if (Test-Path $primaryDir) {
    try {
        Remove-Item -Path $primaryDir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "💣 Cleared primary .gemini directory" -ForegroundColor Yellow
    } catch {}
}

# 5. Target Account Definitions
$accounts = [ordered]@{
    'fptvttnhon2020'         = 'fptvttnhon2020@gmail.com'
    'fptvttnhon2026'         = 'fptvttnhon2026@gmail.com'
    'nhontruongvo'           = 'nhontruongvo@gmail.com'
    'nhontruongvo3'          = 'nhontruongvo3@gmail.com'
    'vothuongtruongnhon2002' = 'vothuongtruongnhon2002@gmail.com'
}

# 6. Re-create clean account directories for each target account with isolated credentials
foreach ($acc in $accounts.Keys) {
    $email = $accounts[$acc]
    $dir = Join-Path $env:USERPROFILE (".gemini_" + $acc)

    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $dir "antigravity-cli") -Force | Out-Null

    # Write google_accounts.json
    $gObj = [ordered]@{
        accounts      = @( @{ email = $email } )
        activeAccount = $email
    }
    $gJson = $gObj | ConvertTo-Json -Depth 5
    [System.IO.File]::WriteAllText((Join-Path $dir "google_accounts.json"), $gJson, [System.Text.UTF8Encoding]::new($false))

    # Write settings.json
    $sObj = [ordered]@{
        accountName = $acc
        userEmail   = $email
    }
    $sJson = $sObj | ConvertTo-Json -Depth 5
    [System.IO.File]::WriteAllText((Join-Path $dir "antigravity-cli\settings.json"), $sJson, [System.Text.UTF8Encoding]::new($false))

    Write-Host "✨ Re-created clean account context: '$acc' -> ($email)" -ForegroundColor Green
}

# 7. Set active account context to vothuongtruongnhon2002
$activeAcc = "vothuongtruongnhon2002"
$activeDir = Join-Path $env:USERPROFILE (".gemini_" + $activeAcc)

if (Test-Path $activeDir) {
    New-Item -ItemType Directory -Path $primaryDir -Force | Out-Null
    Copy-Item -Path "$activeDir\*" -Destination $primaryDir -Recurse -Force -ErrorAction SilentlyContinue
    [System.IO.File]::WriteAllText((Join-Path $primaryDir "active_account.txt"), $activeAcc, [System.Text.UTF8Encoding]::new($false))
    $env:GEMINI_HOME = $activeDir
    [System.Environment]::SetEnvironmentVariable("GEMINI_HOME", $activeDir, "User")
    
    # Clear active session bypass environment variables
    Remove-Item Env:\GEMINI_CLI_IDE_AUTH_TOKEN -ErrorAction SilentlyContinue
    Remove-Item Env:\GEMINI_CLI_IDE_SERVER_PORT -ErrorAction SilentlyContinue

    Write-Host "👉 Set active account context to: '$activeAcc'" -ForegroundColor Cyan
}

Write-Host "`n==================================================" -ForegroundColor Cyan
Write-Host " ✔ All 5 Accounts Cleanly Re-created! (Logged out state forced for agy CLI)" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan
