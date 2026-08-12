[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

Write-Host "==================================================" -ForegroundColor Red
Write-Host " 💣 NUKING & RE-CREATING ALL 5 AGY ACCOUNTS " -ForegroundColor Red
Write-Host "==================================================" -ForegroundColor Red

# 1. Stop any background AgyTui / agy processes
Get-Process AgyTui -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process agy -ErrorAction SilentlyContinue | Stop-Process -Force

# 2. Delete Windows Credential Manager token target "gemini:antigravity"
try {
    cmdkey /delete:gemini:antigravity | Out-Null
    Write-Host "💣 Cleared Windows Credential Manager token target: gemini:antigravity" -ForegroundColor Yellow
} catch {}

# 3. Reset AGY Account DB cache (Preserving themes, workspaces, and learning data)
$dbPaths = @(
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
            $conn.Close()
            Write-Host "🧹 Cleared accounts table in DB: $db" -ForegroundColor Yellow
        } catch {
            Write-Host "ℹ️ DB cache ready: $db" -ForegroundColor Gray
        }
    }
}

# 4. Clean up all .gemini_* directories
$strayDirs = Get-ChildItem -Path $env:USERPROFILE -Filter ".gemini_*" -Directory
foreach ($sd in $strayDirs) {
    try {
        Remove-Item -Path $sd.FullName -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "💣 Removed account folder: $($sd.Name)" -ForegroundColor Red
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
    Set-Content -LiteralPath (Join-Path $dir "google_accounts.json") -Value $gJson -Encoding UTF8

    # Write settings.json
    $sObj = [ordered]@{
        accountName = $acc
        userEmail   = $email
    }
    $sJson = $sObj | ConvertTo-Json -Depth 5
    Set-Content -LiteralPath (Join-Path $dir "antigravity-cli\settings.json") -Value $sJson -Encoding UTF8

    Write-Host "✨ Re-created clean account context: '$acc' -> ($email)" -ForegroundColor Green
}

# 7. Set active account context to vothuongtruongnhon2002
$activeAcc = "vothuongtruongnhon2002"
$activeDir = Join-Path $env:USERPROFILE (".gemini_" + $activeAcc)
$primaryDir = Join-Path $env:USERPROFILE ".gemini"

if (Test-Path $activeDir) {
    if (-not (Test-Path $primaryDir)) { New-Item -ItemType Directory -Path $primaryDir -Force | Out-Null }
    Copy-Item -Path "$activeDir\*" -Destination $primaryDir -Recurse -Force -ErrorAction SilentlyContinue
    Set-Content -LiteralPath (Join-Path $primaryDir "active_account.txt") -Value $activeAcc -Encoding UTF8
    $env:GEMINI_HOME = $activeDir
    [System.Environment]::SetEnvironmentVariable("GEMINI_HOME", $activeDir, "User")
    Write-Host "👉 Set active account context to: '$activeAcc'" -ForegroundColor Cyan
}

Write-Host "`n==================================================" -ForegroundColor Cyan
Write-Host " ✔ All 5 Accounts Cleanly Re-created!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan
