# C:\Users\TruongNhon\Documents\Powershell\Scripts\Test-OllamaFunctions.ps1

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$profilePath = Join-Path $scriptDir "..\Profile\60-AI.ps1"
. $profilePath

Write-Host "1. Initializing Ollama Server and Proxy..." -ForegroundColor Cyan
Ensure-OllamaServer

Write-Host "`n2. Sending 'hi' via Codex (local Ollama custom provider)..." -ForegroundColor Cyan
try {
    # Send 'hi' to Codex CLI and print output
    "" | Invoke-Codex-By-Ollama exec "hi" --ephemeral
} catch {
    Write-Error "Failed to run Codex: $_"
}

Write-Host "`nFlow verification completed." -ForegroundColor Green
