# C:\Users\TruongNhon\Documents\Powershell\Scripts\Test-OllamaFunctions.ps1

$profilePath = Join-Path $PSScriptRoot "..\Profile\60-AI.ps1"
. $profilePath

Write-Host "1. Setting up applications..." -ForegroundColor Cyan
Install-AIIntegrations

Write-Host "`n2. Initializing Ollama server..." -ForegroundColor Cyan
Initialize-OllamaServer

Write-Host "`n3. Testing functions (using --help or --version to avoid blocking)..." -ForegroundColor Cyan

Write-Host "Testing Invoke-Claude-By-Ollama:"
try { Invoke-Claude-By-Ollama --version } catch { Write-Warning $_.Exception.Message }

Write-Host "`nTesting Invoke-Codex-By-Ollama:"
try { Invoke-Codex-By-Ollama --help } catch { Write-Warning $_.Exception.Message }

Write-Host "`nTesting Invoke-OpenClaw-By-Ollama:"
try { Invoke-OpenClaw-By-Ollama --help } catch { Write-Warning $_.Exception.Message }

Write-Host "`nTesting Invoke-Hermes-By-Ollama:"
try { Invoke-Hermes-By-Ollama --help } catch { Write-Warning $_.Exception.Message }

Write-Host "`nTesting Invoke-HermesDesktop-By-Ollama:"
try { Invoke-HermesDesktop-By-Ollama --help } catch { Write-Warning $_.Exception.Message }

Write-Host "`nAll tests completed." -ForegroundColor Green
