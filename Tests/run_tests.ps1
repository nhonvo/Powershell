Write-Host "Running PowerShell Profile Tests..." -ForegroundColor Cyan

# 1. Check syntax of all profile files
$profileDir = Join-Path $PSScriptRoot "..\Profile"
$files = Get-ChildItem -Path $profileDir -Filter "*.ps1" -Recurse

Write-Host "Parsing file syntax via AST..." -ForegroundColor Cyan
foreach ($file in $files) {
    try {
        $errors = $null
        $tokens = $null
        $ast = [System.Management.Automation.Language.Parser]::ParseFile($file.FullName, [ref]$tokens, [ref]$errors)
        if ($errors) {
            Write-Error "Syntax error in $($file.Name): $($errors.Message)"
        } else {
            Write-Host "  [OK] $($file.Name): Syntax OK" -ForegroundColor Green
        }
    } catch {
        Write-Error "Failed to parse $($file.Name): $_"
    }
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
    . "C:\Users\TruongNhon\Documents\Powershell\Microsoft.PowerShell_profile.ps1"
    Write-Host "  [OK] Profile loaded successfully with no execution errors." -ForegroundColor Green
} catch {
    Write-Error "Failed to load profile: $_"
}

# 4. Verify AI Integration Functions & Aliases
Write-Host "`nVerifying AI Integration Functions & Aliases..." -ForegroundColor Cyan

$aiItems = @(
    @{ Type = "Function"; Name = "Invoke-Claude-By-Ollama" }
    @{ Type = "Function"; Name = "Invoke-Codex-By-Ollama" }
    @{ Type = "Function"; Name = "Invoke-OpenClaw-By-Ollama" }
    @{ Type = "Function"; Name = "Invoke-Clawdbot-By-Ollama" }
    @{ Type = "Function"; Name = "Invoke-Hermes-By-Ollama" }
    @{ Type = "Function"; Name = "Invoke-HermesDesktop-By-Ollama" }
    @{ Type = "Function"; Name = "Initialize-OllamaServer" }
    @{ Type = "Function"; Name = "Install-AIIntegrations" }
    
    @{ Type = "Alias"; Name = "claude" }
    @{ Type = "Alias"; Name = "codex" }
    @{ Type = "Alias"; Name = "openclaw" }
    @{ Type = "Alias"; Name = "clawdbot" }
    @{ Type = "Alias"; Name = "hermes" }
    @{ Type = "Alias"; Name = "hermesd" }
    @{ Type = "Alias"; Name = "model" }
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

# 5. Dry-run Invocation Tests (non-blocking)
Write-Host "`nPerforming dry-run tests on wrapper functions..." -ForegroundColor Cyan

Write-Host "  Testing claude (--version)..."
try {
    $out = claude --version 2>&1
    Write-Host "    Response: $out" -ForegroundColor Gray
} catch {
    Write-Warning "    Failed: $_"
}

Write-Host "  Testing codex (--help)..."
try {
    # Just grab first line of help to verify execution
    $out = (codex --help 2>&1 | Select-Object -First 1)
    Write-Host "    Response: $out" -ForegroundColor Gray
} catch {
    Write-Warning "    Failed: $_"
}

Write-Host "  Testing openclaw (--help)..."
try {
    $out = (openclaw --help 2>&1 | Select-Object -First 1)
    Write-Host "    Response: $out" -ForegroundColor Gray
} catch {
    Write-Warning "    Failed: $_"
}

# 6. Run detailed Pester Unit Tests
Write-Host "`nRunning detailed Pester unit tests..." -ForegroundColor Cyan
if (Get-Module -ListAvailable Pester) {
    $tests = @(
        (Join-Path $PSScriptRoot "Unit\AI-Tools.Tests.ps1"),
        (Join-Path $PSScriptRoot "Unit\Profile-All.Tests.ps1")
    )
    Invoke-Pester -Script $tests -EnableExit
} else {
    Write-Warning "Pester module is not available. Skipping unit tests."
}
