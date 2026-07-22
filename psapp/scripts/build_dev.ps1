<#
.SYNOPSIS
    Dev Build Script for AgyTuiApp (TreatWarningsAsErrors=true)
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Write-Host "⚙ Building AgyTuiApp [Dev Mode - WarningsAsErrors]..." -ForegroundColor Cyan
pushd $repoRoot
try {
    # Unlock DLL if loaded
    $dll = "csapp\AgyTuiApp\bin\Debug\net10.0\AgyTuiApp.dll"
    if (Test-Path $dll) {
        $rand = Get-Random
        Rename-Item -Path $dll -NewName "AgyTuiApp.dll.old_$rand" -Force -ErrorAction SilentlyContinue
    }
    dotnet build csapp/AgyTuiApp/AgyTuiApp.csproj -p:TreatWarningsAsErrors=true
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Dev Build Succeeded cleanly." -ForegroundColor Green
    } else {
        Write-Error "❌ Dev Build Failed."
    }
} finally {
    popd
}
