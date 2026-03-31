#region ANTIGRAVITY PROJECTS
# ------------------------------------------------------------------------------
#  Dev launchers for the Antigravity Manager and Claude Proxy projects.
# ------------------------------------------------------------------------------

Write-Host "🛸 Loading Antigravity Projects..." -ForegroundColor Cyan

$script:AgBaseDir = "$env:USERPROFILE\Desktop\back-up\1.project\antigrafity_config"

<#
.SYNOPSIS
Starts the Antigravity Manager Electron app in dev mode.
.CATEGORY
AI Tools
#>
function Start-AntigravityManager {
    $projectDir = Join-Path $script:AgBaseDir "AntigravityManager"
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

<#
.SYNOPSIS
Starts the Antigravity Claude Proxy in dev mode.
.CATEGORY
AI Tools
#>
function Start-AntigravityProxy {
    $projectDir = Join-Path $script:AgBaseDir "antigravity-claude-proxy"
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

<#
.SYNOPSIS
Packages Antigravity Manager as an installer (.exe) via electron-forge.
.CATEGORY
AI Tools
#>
function Build-AntigravityManager {
    $projectDir  = Join-Path $script:AgBaseDir "AntigravityManager"
    $installedPath = "$env:LOCALAPPDATA\antigravity_manager\antigravity-manager.exe"

    if (-not (Test-Path $projectDir)) {
        Write-Error "Project not found: $projectDir"
        return
    }

    if (Test-Path $installedPath) {
        Write-Host "✨ Found installed version. Launching..." -ForegroundColor Green
        Start-Process -FilePath $installedPath
        return
    }

    $currentDir = Get-Location
    Set-Location $projectDir

    try {
        $outPath = Join-Path $projectDir "out\make"
        $recentInstaller = if (Test-Path $outPath) {
            Get-ChildItem -Path $outPath -Filter "*.exe" -Recurse |
            Where-Object { $_.LastWriteTime -gt (Get-Date).AddHours(-1) } |
            Select-Object -First 1
        }

        if ($recentInstaller) {
            Write-Host "⏭️  Recent build found (<1hr). Running..." -ForegroundColor Yellow
            Start-Process -FilePath $recentInstaller.FullName
        } else {
            Write-Host "🏗️  Building Antigravity Manager..." -ForegroundColor Cyan
            npm run make
            $installer = Get-ChildItem -Path $outPath -Filter "*.exe" -Recurse |
                         Where-Object { $_.Name -notmatch "nupkg" } |
                         Sort-Object LastWriteTime -Descending |
                         Select-Object -First 1
            if ($installer) {
                Write-Host "🚀 Launching installer: $($installer.Name)" -ForegroundColor Cyan
                Start-Process -FilePath $installer.FullName
            }
        }
    } catch {
        Write-Error "Build failed: $_"
    } finally {
        Set-Location $currentDir
    }
}

#endregion
