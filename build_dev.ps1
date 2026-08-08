<#
.SYNOPSIS
    Dev Build Script for AgyTuiApp (TreatWarningsAsErrors=true)
#>
param(
    [string]$Command
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
if (-not (Test-Path (Join-Path $repoRoot "csapp\AgyTui\AgyTui.csproj"))) {
    $repoRoot = Split-Path -Parent $PSScriptRoot
}
if (-not (Test-Path (Join-Path $repoRoot "csapp\AgyTui\AgyTui.csproj"))) {
    $repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}
$projectPath = Join-Path $repoRoot "csapp\AgyTui\AgyTui.csproj"

Write-Host "⚙ Building AgyTuiApp [Dev Mode - WarningsAsErrors]..." -ForegroundColor Cyan
pushd $repoRoot
try {
    # Unlock DLL if loaded
    $dll = Join-Path $repoRoot "csapp\AgyTui\bin\Debug\net9.0\AgyTui.dll"
    if (-not (Test-Path $dll)) {
        $dll = Join-Path $repoRoot "csapp\AgyTui\bin\Debug\net10.0\AgyTui.dll"
    }
    if (Test-Path $dll) {
        $rand = Get-Random
        Rename-Item -Path $dll -NewName "AgyTui.dll.old_$rand" -Force -ErrorAction SilentlyContinue
    }
    dotnet build "$projectPath" -p:TreatWarningsAsErrors=true
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Dev Build Succeeded cleanly." -ForegroundColor Green
        if (-not [string]::IsNullOrWhiteSpace($Command)) {
            Write-Host "🚀 Direct launch into command: '$Command'..." -ForegroundColor Yellow
            $exe = "csapp\AgyTui\bin\Debug\net9.0\AgyTui.exe"
            if (-not (Test-Path $exe)) {
                $exe = "csapp\AgyTui\bin\Debug\net10.0\AgyTui.exe"
            }
            if (Test-Path $exe) {
                & $exe $Command
            }
        }
    } else {
        Write-Error "❌ Dev Build Failed."
    }
} finally {
    Get-ChildItem -Path "csapp\AgyTui\bin\Debug\net9.0" -Filter "AgyTui.dll.old_*" -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    Get-ChildItem -Path "csapp\AgyTui\bin\Debug\net10.0" -Filter "AgyTui.dll.old_*" -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    popd
}
