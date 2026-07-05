# C:\Users\TruongNhon\Documents\Powershell\Scripts\Test-OllamaFunctions.ps1

$ProfileDir = Join-Path $PSScriptRoot "..\Profile"
. (Join-Path $ProfileDir "TerminalMenu.ps1")
. (Join-Path $ProfileDir "ProfileEnvironment.ps1")
. (Join-Path $ProfileDir "AiHelper.ps1")
. (Join-Path $ProfileDir "Aliases.ps1")

Write-Host "1. Initializing Ollama Server and Proxy..." -ForegroundColor Cyan
[AiHelper]::EnsureOllamaServer()

Write-Host "`n2. Sending 'hi' via Codex (local Ollama custom provider)..." -ForegroundColor Cyan
try {
    "" | Invoke-Codex-By-Ollama exec "hi" --ephemeral
} catch {
    Write-Error "Failed to run Codex: $_"
}

Write-Host "`n3. Testing Claude-Ollama integration (claude)..." -ForegroundColor Cyan
try {
    Invoke-Claude-By-Ollama --help
} catch {
    Write-Warning "Claude wrapper output/error: $_"
}

Write-Host "`n4. Testing OpenClaw integration (openclaw)..." -ForegroundColor Cyan
try {
    Invoke-OpenClaw-By-Ollama --help
} catch {
    Write-Warning "OpenClaw wrapper output/error: $_"
}

Write-Host "`n5. Testing Hermes integration (hermes)..." -ForegroundColor Cyan
try {
    Invoke-Hermes-By-Ollama --help
} catch {
    Write-Warning "Hermes wrapper output/error: $_"
}

Write-Host "`n6. Testing Hermes Desktop integration (hermesd)..." -ForegroundColor Cyan
try {
    Invoke-HermesDesktop-By-Ollama --help
} catch {
    Write-Warning "Hermes Desktop wrapper output/error: $_"
}

Write-Host "`nFlow verification completed." -ForegroundColor Green
