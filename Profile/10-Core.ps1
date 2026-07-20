# 🛸 Profile Module: 10-Core.ps1
# Core initialization, PATH loading, and configuration resolution.

$global:AGY_WORKSPACE_ROOT = "C:\Users\TruongNhon\Documents\Powershell"

# Ensure global profile configuration path
$global:PROFILE_CONFIG_PATH = Join-Path $global:AGY_WORKSPACE_ROOT "profile.config.json"

function Get-ProfileConfig {
    if (Test-Path $global:PROFILE_CONFIG_PATH) {
        try {
            return Get-Content $global:PROFILE_CONFIG_PATH -Raw | ConvertFrom-Json
        } catch {
            return $null
        }
    }
    return $null
}

function Set-ProfileConfig {
    param([hashtable]$ConfigData)
    $json = $ConfigData | ConvertTo-Json -Depth 5
    Set-Content -Path $global:PROFILE_CONFIG_PATH -Value $json -Force
}
