$env:AI_MODE = 'true'
$env:AGY_LOAD_DLL = '1'
Write-Host "Running PowerShell Profile Tests..." -ForegroundColor Cyan

# Pre-load C# types assembly so the AST parser can resolve types during parsing
$dllPath = Join-Path $PSScriptRoot "..\..\csapp\AgyTui\dist\AgyTui.dll"
if (-not (Test-Path $dllPath)) {
    $dllPath = Join-Path $PSScriptRoot "..\..\csapp\AgyTui\bin\Debug\net9.0\AgyTui.dll"
}
if (-not (Test-Path $dllPath)) {
    $dllPath = Join-Path $PSScriptRoot "..\..\csapp\AgyTui\bin\Debug\net10.0\AgyTui.dll"
}
if (Test-Path $dllPath) {
    Get-ChildItem -Path (Split-Path $dllPath) -Filter "*.dll" | Where-Object { $_.Name -ne "AgyTui.dll" } | ForEach-Object {
        try { Add-Type -Path $_.FullName -ErrorAction SilentlyContinue } catch {}
    }
    try {
        Add-Type -Path $dllPath -ErrorAction SilentlyContinue
        $acc = [psobject].Assembly.GetType('System.Management.Automation.TypeAccelerators')
        $agyAssembly = [System.AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq "AgyTui" } | Select-Object -First 1
        if ($acc -and $agyAssembly) {
            foreach ($type in $agyAssembly.GetExportedTypes()) {
                if ($type.IsClass -and $type.Name -and -not $acc::Get.ContainsKey($type.Name)) {
                    try { $acc::Add($type.Name, $type) } catch {}
                }
            }
        }
    } catch {}
}

# 1. Check syntax of the consolidated profile file
$profilePath = Join-Path $PSScriptRoot "..\..\Microsoft.PowerShell_profile.ps1"

Write-Host "Parsing profile syntax via AST..." -ForegroundColor Cyan
try {
    $errors = $null
    $tokens = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseFile($profilePath, [ref]$tokens, [ref]$errors)
    if ($errors) {
        Write-Error "Syntax error in Microsoft.PowerShell_profile.ps1: $($errors.Message)"
    } else {
        Write-Host "  [OK] Microsoft.PowerShell_profile.ps1: Syntax OK" -ForegroundColor Green
    }
} catch {
    Write-Error "Failed to parse Microsoft.PowerShell_profile.ps1: $_"
}

# 2. Check external tool dependencies
$dependencies = @("git", "docker", "aws", "ollama", "gh", "claude.cmd", "codex.cmd", "gemini.cmd", "openclaw.cmd")
Write-Host "`nChecking External Tool Dependencies..." -ForegroundColor Cyan
foreach ($dep in $dependencies) {
    $cmd = Get-Command $dep -ErrorAction SilentlyContinue
    if ($cmd) {
        Write-Host "  [OK] $dep found: $($cmd.Source)" -ForegroundColor Green
    } else {
        Write-Warning "  [WARN] $dep not found in PATH"
    }
}

# 3. Import profile and verify function resolution
Write-Host "`nImporting Profile to verify function resolution..." -ForegroundColor Cyan
try {
    $global:AgyProfileLoaded = $null
    . (Join-Path $PSScriptRoot "..\..\Microsoft.PowerShell_profile.ps1")
    Write-Host "  [OK] Profile loaded successfully with no execution errors." -ForegroundColor Green
} catch {
    Write-Error "Failed to load profile: $_"
}

# 4. Verify AI Integration Functions & Aliases
Write-Host "`nVerifying AI Integration Functions & Aliases..." -ForegroundColor Cyan

$aiItems = @(
    @{ Type = "Function"; Name = "Invoke-MultiAgent" }
    @{ Type = "Function"; Name = "Invoke-ControlCenter" }
    @{ Type = "Function"; Name = "Invoke-ControlCenterDev" }
    @{ Type = "Function"; Name = "Reset-AgyAccountData" }
    @{ Type = "Function"; Name = "Invoke-ControlCenterNavigator" }
    @{ Type = "Function"; Name = "Purge-AgyAccounts" }
    @{ Type = "Function"; Name = "Show-DotNetInfo" }
    
    @{ Type = "Alias"; Name = "ai" }
    @{ Type = "Alias"; Name = "cai" }
    @{ Type = "Alias"; Name = "claude" }
    @{ Type = "Alias"; Name = "cc" }
    @{ Type = "Alias"; Name = "ccd" }
    @{ Type = "Alias"; Name = "cnav" }
    @{ Type = "Alias"; Name = "reset-agy" }
    @{ Type = "Alias"; Name = "purge-accounts" }
    @{ Type = "Alias"; Name = "dotnet-info" }
)

foreach ($item in $aiItems) {
    if ($item.Type -eq "Function") {
        $cmd = Get-Command $item.Name -CommandType Function -ErrorAction SilentlyContinue
        if ($cmd) {
            Write-Host "  [OK] Function '$($item.Name)' is defined." -ForegroundColor Green
        } else {
            Write-Error "  [FAIL] Function '$($item.Name)' is NOT defined!"
        }
    } else {
        $alias = Get-Alias -Name $item.Name -ErrorAction SilentlyContinue
        if ($alias) {
            Write-Host "  [OK] Alias '$($item.Name)' -> '$($alias.Definition)' is defined." -ForegroundColor Green
        } else {
            Write-Error "  [FAIL] Alias '$($item.Name)' is NOT defined!"
        }
    }
}

# 5. Dry-run Invocation Tests (non-blocking) - Removed to prevent interactive hangs in compiled C# helper wrappers.

# 6. Run detailed Pester Unit Tests
Write-Host "`nRunning detailed Pester unit tests..." -ForegroundColor Cyan
if (Get-Module -ListAvailable Pester) {
    $tests = Get-ChildItem -Path (Join-Path $PSScriptRoot "Unit") -Filter "*.Tests.ps1"
    $failedTotal = 0
    foreach ($test in $tests) {
        $global:AgyUserProfileLoaded = $null
        $res = Invoke-Pester -Script $test.FullName -PassThru
        if ($res.FailedCount -gt 0) {
            $failedTotal += $res.FailedCount
        }
    }
    if ($failedTotal -gt 0) {
        Write-Error "❌ $failedTotal Pester test(s) failed."
        exit 1
    }
} else {
    Write-Warning "Pester module is not available. Skipping unit tests."
}
