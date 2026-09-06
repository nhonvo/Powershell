# ==============================================================================
#  ENHANCED POWERSHELL PROFILE — ROOT FORWARDER
#  Redirects execution to modularized shell/windows/Microsoft.PowerShell_profile.ps1
# ==============================================================================
$target = Join-Path $PSScriptRoot "shell/windows/Microsoft.PowerShell_profile.ps1"
if (Test-Path $target) {
    . $target @args
} else {
    Write-Error "Could not locate PowerShell profile target at: $target"
}
