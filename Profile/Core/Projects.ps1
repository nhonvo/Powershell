#region ANTIGRAVITY PROJECTS LAUNCHERS
# ==============================================================================
#  Development launchers for the Antigravity Manager and Claude Proxy projects.
# ==============================================================================

$priorityFile = Join-Path $env:USERPROFILE ".gemini\antigravity\priority_workspaces.json"
$defaultWorkspaces = @(
    @{ Name = "Powershell";                  Short = "pw";    AssociatedAccount = "fptvttnhon2026@gmail.com" }
    @{ Name = "finance-dashboard";           Short = "fin";   AssociatedAccount = "personal@gmail.com" }
    @{ Name = "clean-architecture-net-8.0";  Short = "clean"; AssociatedAccount = "default" }
    @{ Name = "BinhDinhFood";                Short = "food";  AssociatedAccount = "default" }
    @{ Name = "test-road-map";               Short = "road";  AssociatedAccount = "default" }
)

if (-not (Test-Path $priorityFile)) {
    try {
        $parent = Split-Path $priorityFile
        if (-not (Test-Path $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
        $defaultWorkspaces | ConvertTo-Json | Set-Content $priorityFile -Force
    } catch {}
}

$Global:ProfileWorkspaces = @()
if (Test-Path $priorityFile) {
    try {
        $loaded = Get-Content $priorityFile -Raw | ConvertFrom-Json
        foreach ($item in $loaded) {
            $ht = @{}
            foreach ($prop in $item.psobject.Properties) {
                $ht[$prop.Name] = $prop.Value
            }
            $Global:ProfileWorkspaces += $ht
        }
    } catch {
        $Global:ProfileWorkspaces = $defaultWorkspaces
    }
} else {
    $Global:ProfileWorkspaces = $defaultWorkspaces
}

# Dynamically resolve paths from the workspace cache at startup
$cacheFile = Join-Path $env:USERPROFILE ".gemini\antigravity\workspace_cache.json"
if (Test-Path $cacheFile) {
    try {
        $cachedProjects = Get-Content $cacheFile -Raw | ConvertFrom-Json
        foreach ($pw in $Global:ProfileWorkspaces) {
            $match = $cachedProjects | Where-Object { $_.Label -eq $pw.Name } | Select-Object -First 1
            if ($match) {
                $pw.Path = $match.Path
            }
        }
    } catch {}
}

if (-not $Global:AiMode -and $Global:VerboseStartup) {
    Write-Host "🛸 Loading Antigravity Projects..." -ForegroundColor Cyan
}

class Projects {
    static [string]$AgBaseDir

    static Projects() {
        [Projects]::AgBaseDir = if (Test-Path "C:\Users\sshuser\project") { "C:\Users\sshuser\project" } else { "$env:USERPROFILE\Desktop\project" }
    }

    static [void] StartManager() {
        $projectDir = Join-Path ([Projects]::AgBaseDir) "AntigravityManager"
        if (-not (Test-Path $projectDir)) {
            Write-Error "Project not found: $projectDir"
            return
        }

        Set-Location $projectDir
        Write-Host "[1/2] 📦 Checking dependencies..." -ForegroundColor Cyan
        if (-not (Test-Path "node_modules")) {
            Write-Host "   -> Installing (npm install)..." -ForegroundColor Yellow
            npm install
        } else {
            Write-Host "   -> node_modules OK." -ForegroundColor Green
        }

        Write-Host "[2/2] 🚀 Launching Antigravity Manager..." -ForegroundColor Green
        npm start
    }

    static [void] StartProxy() {
        $projectDir = Join-Path ([Projects]::AgBaseDir) "antigravity-claude-proxy"
        if (-not (Test-Path $projectDir)) {
            Write-Error "Project not found: $projectDir"
            return
        }

        $env:ANTHROPIC_BASE_URL   = 'http://localhost:8080'
        $env:ANTHROPIC_AUTH_TOKEN = 'test'
        Write-Host "🛸 Proxy env set (BASE_URL=localhost:8080)" -ForegroundColor DarkCyan

        Set-Location $projectDir
        Write-Host "[1/2] 📦 Checking dependencies..." -ForegroundColor Cyan
        if (-not (Test-Path "node_modules")) {
            Write-Host "   -> Installing (npm install)..." -ForegroundColor Yellow
            npm install
        } else {
            Write-Host "   -> node_modules OK." -ForegroundColor Green
        }

        Write-Host "[2/2] 🚀 Launching Antigravity Proxy..." -ForegroundColor Green
        npm start
    }
}
#endregion



