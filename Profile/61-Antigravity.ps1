#region ANTIGRAVITY MANAGER
# ------------------------------------------------------------------------------
#  Shortcuts and automation for Antigravity Manager development.
# ------------------------------------------------------------------------------

Write-Host "🛸 Loading Antigravity Manager Configuration..." -ForegroundColor Cyan

<#
.SYNOPSIS
Starts the Antigravity Manager Electron project in development mode.
.DESCRIPTION
Automatically navigates to the project directory, ensures dependencies are installed,
and launches the application via electron-forge.
.CATEGORY
AI Tools
#>
function Start-AntigravityManager {
    $projectDir = "C:\Users\TruongNhon\Desktop\back-up\1.project\antigrafity_config\AntigravityManager"
    
    if (-not (Test-Path $projectDir)) {
        Write-Error "Project directory not found: $projectDir"
        return
    }

    # Navigate to project first as requested
    Set-Location $projectDir
    
    try {
        Write-Host "`n[1/2] 📦 Checking dependencies in AntigravityManager..." -ForegroundColor Cyan
        
        if (-not (Test-Path "node_modules")) {
            Write-Host "   -> node_modules not found. Installing dependencies (npm install)..." -ForegroundColor Yellow
            npm install
        } else {
            Write-Host "   -> node_modules found." -ForegroundColor Green
        }

        Write-Host "[2/2] 🚀 Launching Antigravity Manager..." -ForegroundColor Green
        npm start
    }
    catch {
        Write-Error "Failed to start Antigravity Manager: $_"
    }
}

<#
.SYNOPSIS
Builds the Antigravity Manager project as an installer (.exe).
.DESCRIPTION
Runs the electron-forge make command to generate distributables in the 'out/' directory.
.CATEGORY
AI Tools
#>
function Build-AntigravityManager {
    $projectDir = "C:\Users\TruongNhon\Desktop\back-up\1.project\antigrafity_config\AntigravityManager"
    $installedPath = "$env:LOCALAPPDATA\antigravity_manager\antigravity-manager.exe"
    
    if (-not (Test-Path $projectDir)) {
        Write-Error "Project directory not found: $projectDir"
        return
    }

    # Optimization: If the app is already installed and we just want to run it, check there first
    if (Test-Path $installedPath) {
        Write-Host "✨ Found installed version, launching..." -ForegroundColor Green
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
            Write-Host "⏭️  Recent build found (< 1hr old). Skipping build and running..." -ForegroundColor Yellow
            Start-Process -FilePath $recentInstaller.FullName
        } else {
            Write-Host "🏗️  No recent build found. Packaging Antigravity Manager..." -ForegroundColor Cyan
            npm run make
            
            $installer = Get-ChildItem -Path $outPath -Filter "*.exe" -Recurse | 
                         Where-Object { $_.Name -notmatch "nupkg" } | 
                         Sort-Object LastWriteTime -Descending | 
                         Select-Object -First 1

            if ($installer) {
                Write-Host "🚀 Launching installer: $($installer.FullName)" -ForegroundColor Cyan
                Start-Process -FilePath $installer.FullName
            }
        }
    }
    catch {
        Write-Error "Action failed: $_"
    }
    finally {
        Set-Location $currentDir
    }
}

#endregion
