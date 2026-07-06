#region ANTIGRAVITY PROJECTS LAUNCHERS
# ==============================================================================
#  Development launchers for the Antigravity Manager and Claude Proxy projects.
# ==============================================================================

Write-Host "🛸 Loading Antigravity Projects..." -ForegroundColor Cyan

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



