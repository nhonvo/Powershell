#region AI HELPER
# ==============================================================================
#  Ollama and CLI wrappers for AI agents (Claude, Codex, Hermes, OpenClaw).
# ==============================================================================

class AiHelper {
    static [string]$OllamaDefaultModelFile
    static [string]$OllamaDefaultModel = "qwen3:1.7b"

    static AiHelper() {
        [AiHelper]::OllamaDefaultModelFile = Join-Path $env:USERPROFILE ".ollama_default_model"
        if (Test-Path [AiHelper]::OllamaDefaultModelFile) {
            try {
                $savedModel = (Get-Content [AiHelper]::OllamaDefaultModelFile -ErrorAction SilentlyContinue).Trim()
                if ($savedModel) {
                    [AiHelper]::OllamaDefaultModel = $savedModel
                }
            } catch {}
        }
    }

    static [bool] IsPortResponding([int]$Port, [string]$Pattern) {
        $oldHttpProxy = $env:http_proxy
        $oldHttpsProxy = $env:https_proxy
        $oldAllProxy = $env:all_proxy
        try {
            Remove-Item env:http_proxy -ErrorAction SilentlyContinue
            Remove-Item env:https_proxy -ErrorAction SilentlyContinue
            Remove-Item env:all_proxy -ErrorAction SilentlyContinue
            
            $resp = Invoke-RestMethod -Uri "http://127.0.0.1:$Port/" -TimeoutSec 2 -ErrorAction Stop
            if (-not $Pattern -or ($resp -like $Pattern)) {
                return $true
            }
        } catch {
            $connection = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($connection) {
                return $true
            }
        } finally {
            if ($oldHttpProxy) { $env:http_proxy = $oldHttpProxy }
            if ($oldHttpsProxy) { $env:https_proxy = $oldHttpsProxy }
            if ($oldAllProxy) { $env:all_proxy = $oldAllProxy }
        }
        return $false
    }

    static [void] EnsureOllamaProxy() {
        $proxyPort = 11435
        if (-not [AiHelper]::IsPortResponding($proxyPort, $null)) {
            Write-Host "[AI] Ollama Proxy is not running on port $proxyPort. Starting..." -ForegroundColor Yellow
            $currentDir = $PSScriptRoot
            # Resolve repository root (up two levels from Profile/Helpers)
            $repoDir = Split-Path -Parent -Path (Split-Path -Parent -Path $currentDir)
            $proxyScript = Join-Path $repoDir "Tests\Mocks\ollama-proxy.js"
            if (-not (Test-Path $proxyScript)) {
                $proxyScript = Join-Path $repoDir "Tests\ollama-proxy.js"
            }

            $stdoutPath = Join-Path $env:TEMP "ollama_proxy_out.log"
            $stderrPath = Join-Path $env:TEMP "ollama_proxy_err.log"
            if (Test-Path $stdoutPath) { Remove-Item $stdoutPath -Force -ErrorAction SilentlyContinue }
            if (Test-Path $stderrPath) { Remove-Item $stderrPath -Force -ErrorAction SilentlyContinue }

            try {
                Start-Process -FilePath "node" -ArgumentList $proxyScript -WindowStyle Hidden -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath -ErrorAction Stop
            } catch {
                Write-Host "[AI] Failed to start Ollama Proxy: $_" -ForegroundColor Red
            }
            Start-Sleep -Seconds 1
        }
    }

    static [bool] IsOllamaRunning() {
        return [AiHelper]::IsPortResponding(11434, "*Ollama is running*")
    }

    static [void] EnsureOllamaServer() {
        if (Get-Command Describe -ErrorAction SilentlyContinue) {
            if (-not [AiHelper]::IsOllamaRunning()) {
                Initialize-OllamaServer
            }
            [AiHelper]::EnsureOllamaProxy()
            return
        }

        $null = [LogHelper]::InvokeWithSpinner("[AI] Verifying local Ollama server status...", {
            if (-not [AiHelper]::IsOllamaRunning()) {
                Initialize-OllamaServer
            }
            [AiHelper]::EnsureOllamaProxy()
        })
    }

    static [void] InvokeOllamaNative([string]$Model) {
        Ensure-OllamaServer
        $activeModel = if ($Model) { $Model } else { [AiHelper]::OllamaDefaultModel }
        Write-Host "Starting native Ollama interactive session for '$activeModel'..." -ForegroundColor Cyan
        $proc = Start-Process -FilePath "ollama" -ArgumentList "run", $activeModel -NoNewWindow -PassThru -Wait
    }

    static [void] InvokeClaude([string[]]$ArgsList) {
        if ([AiHelper]::GetEffectiveProviderMode() -eq "cloud") {
            $proc = Start-Process -FilePath "claude.cmd" -ArgumentList $ArgsList -NoNewWindow -PassThru -Wait
            return
        }

        Ensure-OllamaServer

        $oldOllamaHost = $env:OLLAMA_HOST
        $oldAnthropicBaseUrl = $env:ANTHROPIC_BASE_URL
        $oldNodeOptions = $env:NODE_OPTIONS
        try {
            $env:OLLAMA_HOST = "127.0.0.1:11434"
            $env:ANTHROPIC_BASE_URL = "http://127.0.0.1:11434"
            $env:NODE_OPTIONS = if ($env:NODE_OPTIONS) { "$env:NODE_OPTIONS --dns-result-order=ipv4first" } else { "--dns-result-order=ipv4first" }

            $flags = @()
            if ($ArgsList -notcontains "--model") {
                $flags += "--model", [AiHelper]::OllamaDefaultModel
            }

            # Build argument list for Start-Process to preserve TTY state
            $argList = @("launch", "claude")
            foreach ($f in $flags) { $argList += $f }
            foreach ($a in $ArgsList) { $argList += $a }

            $proc = Start-Process -FilePath "ollama.exe" -ArgumentList $argList -NoNewWindow -PassThru -Wait
        } finally {
            $env:OLLAMA_HOST = $oldOllamaHost
            $env:ANTHROPIC_BASE_URL = $oldAnthropicBaseUrl
            $env:NODE_OPTIONS = $oldNodeOptions
        }
    }

    static [void] InvokeCodex([string[]]$ArgsList) {
        if ([AiHelper]::GetEffectiveProviderMode() -eq "cloud") {
            $proc = Start-Process -FilePath "codex.cmd" -ArgumentList $ArgsList -NoNewWindow -PassThru -Wait
            return
        }

        Ensure-OllamaServer

        $oldOllamaHost = $env:OLLAMA_HOST
        $oldBaseUrl = $env:OPENAI_BASE_URL
        $oldApiKey = $env:OPENAI_API_KEY
        $oldNodeOptions = $env:NODE_OPTIONS
        $oldCodexHome = $env:CODEX_HOME
        try {
            # Route built-in Ollama provider to our proxy port 11435
            $env:OLLAMA_HOST = "127.0.0.1:11435"
            $env:NODE_OPTIONS = if ($env:NODE_OPTIONS) { "$env:NODE_OPTIONS --dns-result-order=ipv4first" } else { "--dns-result-order=ipv4first" }

            # Clear OpenAI environment variables to avoid conflicting settings
            Remove-Item env:OPENAI_BASE_URL -ErrorAction SilentlyContinue
            Remove-Item env:OPENAI_API_KEY -ErrorAction SilentlyContinue

            $model = [AiHelper]::OllamaDefaultModel
            $newArgsList = [System.Collections.Generic.List[string]]::new()
            for ($i = 0; $i -lt $ArgsList.Count; $i++) {
                if (($ArgsList[$i] -eq "--model" -or $ArgsList[$i] -eq "-m") -and $i -lt $ArgsList.Count - 1) {
                    $model = $ArgsList[$i+1] -replace '^ollama_custom/', ''
                    $newArgsList.Add($ArgsList[$i])
                    $newArgsList.Add($model)
                    $i++
                } else {
                    $newArgsList.Add($ArgsList[$i])
                }
            }

            # Setup Sandbox
            $sandboxPath = Join-Path $env:TEMP ".codex_local_ollama"
            if (-not (Test-Path $sandboxPath)) {
                $null = New-Item -ItemType Directory -Path $sandboxPath -Force
            }
            $emptySkillsDir = "C:\Users\TruongNhon\.gemini\antigravity\scratch\empty_skills"
            if (-not (Test-Path $emptySkillsDir)) {
                $null = New-Item -ItemType Directory -Path $emptySkillsDir -Force
            }
            
            $configToml = @"
# Temp sandbox configuration generated at $sandboxPath/config.toml
model = "$model"

[codex]
skills_directory = "$emptySkillsDir"

[mcp_servers]
# Intentionally empty to disable external tool description loads
"@
            # Ensure path separators are forward slashes for TOML
            $configToml = $configToml.Replace('\', '/')
            $configToml | Out-File -FilePath (Join-Path $sandboxPath "config.toml") -Force -Encoding utf8

            $env:CODEX_HOME = $sandboxPath

            $flags = @()
            if ($newArgsList -notcontains "--model" -and $newArgsList -notcontains "-m") {
                $flags += "--model", $model
            }
            $flags += "--oss"
            $flags += "--local-provider", "ollama"

            $argList = @()
            foreach ($f in $flags) { $argList += $f }
            foreach ($a in $newArgsList) { $argList += $a }
            $proc = Start-Process -FilePath "codex.cmd" -ArgumentList $argList -NoNewWindow -PassThru -Wait
        } finally {
            $env:OLLAMA_HOST = $oldOllamaHost
            $env:OPENAI_BASE_URL = $oldBaseUrl
            $env:OPENAI_API_KEY = $oldApiKey
            $env:NODE_OPTIONS = $oldNodeOptions
            $env:CODEX_HOME = $oldCodexHome
        }
    }

    static [void] EnsureOpenClawGateway() {
        $port = 18789
        $connection = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $connection) {
            Write-Host "[AI] OpenClaw Gateway is not running. Starting..." -ForegroundColor Yellow
            Start-Process -FilePath "openclaw" -ArgumentList "gateway", "start" -WindowStyle Hidden -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 2
        }
    }

    static [void] InvokeOpenClaw([string[]]$ArgsList) {
        if ([AiHelper]::GetEffectiveProviderMode() -eq "cloud") {
            $argList = if ($ArgsList.Count -eq 0) { @("chat") } else { $ArgsList }
            $proc = Start-Process -FilePath "openclaw.cmd" -ArgumentList $argList -NoNewWindow -PassThru -Wait
            return
        }

        Ensure-OllamaServer
        [AiHelper]::EnsureOpenClawGateway()

        $oldOllamaHost = $env:OLLAMA_HOST
        try {
            $env:OLLAMA_HOST = "127.0.0.1:11434"

            # Parse out model parameter if passed in arguments
            $model = $null
            $cleanArgs = @()
            for ($i = 0; $i -lt $ArgsList.Count; $i++) {
                if ($ArgsList[$i] -eq "--model" -and $i -lt $ArgsList.Count - 1) {
                    $model = $ArgsList[$i+1]
                    $i++ # Skip model name
                } else {
                    $cleanArgs += $ArgsList[$i]
                }
            }

            if (-not $model) {
                $model = [AiHelper]::OllamaDefaultModel
            }

            # Set model in OpenClaw config programmatically
            if ($model) {
                $cleanModel = $model -replace '^ollama/', ''
                # Execute config set synchronously before starting chat UI
                $null = Start-Process -FilePath "openclaw.cmd" -ArgumentList @("config", "set", "agents.defaults.model.primary", "ollama/$cleanModel") -NoNewWindow -Wait -ErrorAction SilentlyContinue
            }

            # Default to chat if no arguments passed
            $argList = if ($cleanArgs.Count -eq 0) { @("chat") } else { $cleanArgs }

            # Use Start-Process to preserve TTY state inside nested shell scripts
            $proc = Start-Process -FilePath "openclaw.cmd" -ArgumentList $argList -NoNewWindow -PassThru -Wait
        } finally {
            $env:OLLAMA_HOST = $oldOllamaHost
        }
    }

    static [void] InvokeClawdbot([string[]]$ArgsList) {
        [AiHelper]::InvokeOpenClaw($ArgsList)
    }

    static [void] InvokeHermes([string[]]$ArgsList) {
        Ensure-OllamaServer

        $bin = Get-Command hermes -CommandType Application -ErrorAction SilentlyContinue
        if (-not $bin) {
            $localPaths = @(
                "$env:USERPROFILE\.hermes\bin\hermes.exe",
                "$env:USERPROFILE\.hermes\bin\hermes.cmd",
                "$env:LOCALAPPDATA\Programs\Hermes\bin\hermes.exe"
            )
            foreach ($p in $localPaths) {
                if (Test-Path $p) {
                    $bin = Get-Item $p
                    break
                }
            }
        }

        if (-not $bin) {
            Write-Host "⚠️ Hermes Agent is not installed on your system." -ForegroundColor Yellow
            $choice = Read-Host "Would you like to install Hermes Agent now? (Y/N)"
            if ($choice -match "^[Yy]") {
                Write-Host "🚀 Running Hermes Agent PowerShell Installer..." -ForegroundColor Cyan
                Invoke-Expression (Invoke-RestMethod https://hermes-agent.nousresearch.com/install.ps1)
                Write-Host "✅ Installation complete! Please reload your shell or recall CC." -ForegroundColor Green
            }
            return
        }

        # Auto-configure local Ollama endpoint in config.toml
        $configPath = "$env:USERPROFILE\.hermes\config.toml"
        if (-not (Test-Path $configPath)) {
            $null = New-Item -ItemType File -Path $configPath -Force
        }
        $configContent = Get-Content -Raw -Path $configPath -ErrorAction SilentlyContinue
        if (-not $configContent -or $configContent -notlike "*127.0.0.1:11434*") {
            Write-Host "[AI] Configuring local Ollama endpoint in Hermes config.toml..." -ForegroundColor Yellow
            $ollamaConfig = @"

[model_providers.ollama_custom]
name = "Ollama Custom"
base_url = "http://127.0.0.1:11434/v1"
"@
            Add-Content -Path $configPath -Value $ollamaConfig -Force
        }

        $argList = @("chat")
        foreach ($a in $ArgsList) {
            if ($a -ne "--model" -and $a -ne [AiHelper]::OllamaDefaultModel) {
                $argList += $a
            }
        }

        Write-Host "Starting Hermes Agent TUI..." -ForegroundColor Cyan
        $proc = Start-Process -FilePath $bin.Source -ArgumentList $argList -NoNewWindow -PassThru -Wait
    }

    static [void] InvokeHermesDesktop([string[]]$ArgsList) {
        Ensure-OllamaServer

        $bin = Get-Command hermes-desktop -CommandType Application -ErrorAction SilentlyContinue
        if (-not $bin) {
            $localPaths = @(
                "$env:USERPROFILE\.hermes\bin\hermes-desktop.exe",
                "$env:LOCALAPPDATA\Programs\Hermes\bin\hermes-desktop.exe"
            )
            foreach ($p in $localPaths) {
                if (Test-Path $p) {
                    $bin = Get-Item $p
                    break
                }
            }
        }

        if (-not $bin) {
            # Try to run hermes command with desktop argument or notify
            $cliBin = Get-Command hermes -CommandType Application -ErrorAction SilentlyContinue
            if ($cliBin) {
                Write-Host "Starting Hermes Desktop..." -ForegroundColor Cyan
                $proc = Start-Process -FilePath $cliBin.Source -ArgumentList @("desktop") -NoNewWindow -PassThru -Wait
            } else {
                Write-Host "⚠️ Hermes Desktop is not installed." -ForegroundColor Yellow
                $choice = Read-Host "Would you like to install Hermes Agent now? (Y/N)"
                if ($choice -match "^[Yy]") {
                    Write-Host "🚀 Running Hermes Agent PowerShell Installer..." -ForegroundColor Cyan
                    Invoke-Expression (Invoke-RestMethod https://hermes-agent.nousresearch.com/install.ps1)
                }
            }
            return
        }

        Write-Host "Starting Hermes Desktop..." -ForegroundColor Cyan
        $proc = Start-Process -FilePath $bin.Source -NoNewWindow -PassThru -Wait
    }

    static [void] InitializeOllamaServer() {
        $port = 11434
        Write-Host "[Ollama] Resetting port $port..." -ForegroundColor Cyan

        $connection = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($connection) {
            $pidToKill = $connection.OwningProcess
            $proc = Get-Process -Id $pidToKill -ErrorAction SilentlyContinue
            if ($proc) {
                Write-Host "[Ollama] Killing existing process '$($proc.Name)' (PID $pidToKill) on port $port..." -ForegroundColor Yellow
                Stop-Process -Id $pidToKill -Force -ErrorAction SilentlyContinue
                Start-Sleep -Seconds 1
            }
        } else {
            Write-Host "[Ollama] Port $port is free." -ForegroundColor Green
        }

        Write-Host "[Ollama] Starting Ollama server..." -ForegroundColor Cyan
        $oldHost = $env:OLLAMA_HOST
        $env:OLLAMA_HOST = "127.0.0.1:$port"

        $ollamaCmd = Get-Command "ollama" -ErrorAction SilentlyContinue
        $ollamaPath = if ($ollamaCmd) { $ollamaCmd.Source } else { "ollama" }

        $logPath = Join-Path $env:LOCALAPPDATA "Ollama\server.log"
        $logParent = Split-Path $logPath -Parent
        if (-not (Test-Path $logParent)) {
            $null = New-Item -ItemType Directory -Path $logParent -Force -ErrorAction SilentlyContinue
        }

        # Start hidden and redirect all output/errors to standard log file
        Start-Process -FilePath $ollamaPath -ArgumentList "serve" -WindowStyle Hidden -RedirectStandardOutput $logPath -RedirectStandardError $logPath -ErrorAction SilentlyContinue

        $retry = 0
        while ($retry -lt 10) {
            Start-Sleep -Seconds 1
            try {
                $resp = Invoke-RestMethod -Uri "http://127.0.0.1:$port/" -TimeoutSec 2 -ErrorAction SilentlyContinue
                if ($resp -like "*Ollama is running*") {
                    Write-Host "[Ollama] Ollama server is running and ready!" -ForegroundColor Green
                    $env:OLLAMA_HOST = $oldHost
                    return
                }
            } catch {}
            $retry++
        }
        $env:OLLAMA_HOST = $oldHost
        Write-Warning "[Ollama] Failed to verify if Ollama started successfully after 10 seconds."
    }

    static [void] InstallAIIntegrations() {
        # 1. Claude Code
        if (-not (Get-Command "claude" -ErrorAction SilentlyContinue)) {
            Write-Host "[AI] Installing Claude Code via npm..." -ForegroundColor Cyan
            Invoke-Npm @("install", "-g", "@anthropic-ai/claude-code")
        } else {
            Write-Host "[AI] Claude Code is already installed." -ForegroundColor Green
        }

        # 2. Codex CLI
        if (-not (Get-Command "codex" -ErrorAction SilentlyContinue)) {
            Write-Host "[AI] Installing Codex CLI via npm..." -ForegroundColor Cyan
            Invoke-Npm @("install", "-g", "@openai/codex")
        } else {
            Write-Host "[AI] Codex CLI is already installed." -ForegroundColor Green
        }

        # 3. OpenClaw
        if (-not (Get-Command "openclaw" -ErrorAction SilentlyContinue)) {
            Write-Host "[AI] Installing OpenClaw via npm..." -ForegroundColor Cyan
            Invoke-Npm @("install", "-g", "openclaw")
        } else {
            Write-Host "[AI] OpenClaw is already installed." -ForegroundColor Green
        }
    }

    static [void] SetOllamaModel([string]$ModelName) {
        $ollamaCmd = Get-Command "ollama" -ErrorAction SilentlyContinue
        if (-not $ollamaCmd) {
            Write-Error "Ollama is not installed or not in PATH."
            return
        }

        $localModels = (ollama list | Select-Object -Skip 1 | ForEach-Object {
            $parts = $_ -split '\s+'
            if ($parts[0] -and $parts[0] -ne "") { $parts[0] }
        }) | Where-Object { $_ }

        if (-not $localModels) {
            Write-Error "No local Ollama models found. Please download one using 'ollama pull'."
            return
        }

        if ($ModelName) {
            if ($ModelName -in $localModels) {
                [AiHelper]::OllamaDefaultModel = $ModelName
                $ModelName | Out-File -FilePath [AiHelper]::OllamaDefaultModelFile -Force -Encoding utf8
                Write-Host "🟢 Default Ollama model set to '$ModelName'." -ForegroundColor Green
            } else {
                Write-Error "Model '$ModelName' is not available locally. Available models: $($localModels -join ', ')"
            }
            return
        }

        # Refactored TUI model selection using global TerminalMenu
        $menuItems = @()
        $defaultIdx = 0
        for ($i = 0; $i -lt $localModels.Count; $i++) {
            $m = $localModels[$i]
            if ($m -eq [AiHelper]::OllamaDefaultModel) {
                $menuItems += "$m (Active)"
                $defaultIdx = $i
            } else {
                $menuItems += $m
            }
        }

        $selected = ([type]"TerminalMenu")::Show("Select Default Ollama Model", $menuItems, $defaultIdx)
        if ($selected -ge 0) {
            $selectedModel = $localModels[$selected]
            [AiHelper]::OllamaDefaultModel = $selectedModel
            $selectedModel | Out-File -FilePath [AiHelper]::OllamaDefaultModelFile -Force -Encoding utf8
            Write-Host "🟢 Default Ollama model set to '$selectedModel'." -ForegroundColor Green
        } else {
            Write-Host "Cancelled." -ForegroundColor Yellow
        }
    }

    static [void] ShowOllamaLogs() {
        [AiHelper]::EnsureOllamaServer()
        $logPath = Join-Path $env:LOCALAPPDATA "Ollama\server.log"
        if (Test-Path $logPath) {
            Write-Host "--- Ollama Server Log (Last 50 lines) ---" -ForegroundColor Cyan
            Get-Content -Path $logPath -Tail 50 -ErrorAction SilentlyContinue
            Write-Host "----------------------------------------" -ForegroundColor Cyan
        } else {
            Write-Warning "Ollama server log not found at $logPath."
        }
    }

    static [string] GetEffectiveProviderMode() {
        $mode = if ($Global:AiProviderMode) { $Global:AiProviderMode } else { "cloud" }
        if ($mode -eq "auto") {
            return if ([AiHelper]::IsOllamaRunning()) { "local" } else { "cloud" }
        }
        return $mode
    }

    static [void] InvokeMultiAgent([string]$Query, [string]$ParameterSetName, [string]$Model) {
        if ($ParameterSetName -and $ParameterSetName -ne "Menu") {
            switch ($ParameterSetName) {
                "Gemini"  { 
                    if ([AiHelper]::GetEffectiveProviderMode() -eq "cloud") {
                        if ($Query) { Invoke-GeminiChat $Query } else { Invoke-GeminiChat }
                    } else {
                        $activeModel = if ($Model) { $Model } else { [AiHelper]::OllamaDefaultModel }
                        [AiHelper]::InvokeOllamaNative($activeModel)
                    }
                    return 
                }
                "Copilot" {
                    if ([string]::IsNullOrWhiteSpace($Query)) { Write-Warning "Copilot requires a prompt."; return }
                    Invoke-CopilotExplain -Command $Query; return
                }
                "Ollama"    { [AiHelper]::InvokeOllamaNative($Model); return }
                "OpenClaw"  { [AiHelper]::InvokeOpenClaw(@(if ($Model) { "--model"; $Model })); return }
                "Claude"  {
                    $activeModel = if ($Model) { $Model } else { "qwen3-coder" }
                    [AiHelper]::InvokeClaude(@("--model", $activeModel))
                    return
                }
                "Local"   { [AiHelper]::InvokeClaude(@("--model", (if ($Model) { $Model } else { "qwen3-coder" }))); return }
                "ChatGPT" { 
                    if ([AiHelper]::GetEffectiveProviderMode() -eq "cloud") {
                        if ($Query) { Invoke-ChatGPT $Query } else { Invoke-ChatGPT }
                    } else {
                        $activeModel = if ($Model) { $Model } else { [AiHelper]::OllamaDefaultModel }
                        [AiHelper]::InvokeOpenClaw(@(if ($activeModel) { "--model"; $activeModel }))
                    }
                    return 
                }
                "Codex"   {
                    [AiHelper]::InvokeCodex(@(if ($Model) { "--model"; $Model }))
                    return
                }
            }
        }

        if ($Global:AiMode) {
            Write-Host "Select AI Agent"
            Write-Host "Usage: ai -Query <prompt> [-Gemini|-Copilot|-Ollama|-Claude|-Local|-ChatGPT|-Codex] [-Model <model>]"
            Write-Host "Available Agents: Codex, Ollama, OpenClaw, Hermes, Claude"
            return
        }

        [AiHelper]::ShowAiDashboard()
    }

    static [void] ShowAiDashboard() {
        while ($true) {
            $statusInfo = [LogHelper]::InvokeWithSpinner("[AI] Loading Ollama server configuration...", {
                $status = "Offline"
                try {
                    $null = Invoke-RestMethod -Uri "http://127.0.0.1:11434/" -TimeoutSec 1 -ErrorAction Stop
                    $status = "Running"
                } catch {}

                $models = [System.Collections.Generic.List[string]]::new()
                if ($status -eq "Running") {
                    try {
                        $list = ollama list 2>$null
                        if ($list) {
                            $lines = $list -split '\r?\n'
                            for ($i = 1; $i -lt $lines.Count; $i++) {
                                $parts = $lines[$i] -split '\s+'
                                if ($parts[0]) { $null = $models.Add($parts[0]) }
                            }
                        }
                    } catch {}
                }
                return [PSCustomObject]@{ Status = $status; Models = $models }
            })

            $cHalf = [char]0x2584
            $cFull = [char]0x2588
            $cTop  = [char]0x2580
            $aiHeaders = @(
                "  $cHalf$cFull$cFull$cFull$cFull$cHalf   $cHalf$cFull$cFull$cFull$cFull$cHalf     Powershell Profile CLI v2.0",
                " $cFull$cTop     $cTop $cFull$cTop     $cTop    Ollama Local AI Hub",
                " $cFull        $cFull           Ollama Status: $($statusInfo.Status)",
                " $cFull$cHalf     $cHalf $cFull$cHalf     $cHalf    Active Model:  $([AiHelper]::OllamaDefaultModel)",
                "  $cTop$cFull$cFull$cFull$cFull$cTop   $cTop$cFull$cFull$cFull$cFull$cTop     Select an agent to run. Esc to back.",
                "============================================="
            )

            $providerMode = if ($Global:AiProviderMode) { $Global:AiProviderMode } else { "cloud" }
            $modeLabel = switch ($providerMode) {
                "cloud" { "Cloud API (Normal)" }
                "local" { "Local Ollama" }
                "auto"  { "Auto (Local if online, else Cloud)" }
                default { "Cloud API (Normal)" }
            }
            $menuItems = @()
            $menuItems += "[Agent] Claude CLI (Interactive coding chat)"
            $menuItems += "[Agent] Hermes TUI (Autonomous workspace assistant)"
            $menuItems += "[Agent] Codex CLI (Natural language command tool)"
            $menuItems += "[Agent] OpenClaw CLI (Local agent router)"
            $menuItems += "[Agent] Clawdbot TUI (Interactive helper)"
            $menuItems += "[Setting] Provider Mode: $modeLabel"
            $menuItems += "[Model] Select / Set Default Local Model"
            $menuItems += "[Action] Auto-Install missing LLM CLI tools"
            $menuItems += "[x] Return to Main Menu"

            while ([Console]::KeyAvailable) { [void][Console]::ReadKey($true) }

            $selected = [TerminalMenu]::ShowRobust($aiHeaders, $menuItems, 0, $false, $true)
            if ($selected -lt 0 -or $selected -eq ($menuItems.Count - 1)) {
                break
            }

            switch ($selected) {
                0 {
                    [AiHelper]::InvokeClaude(@())
                }
                1 {
                    # Invoke-Hermes-By-Ollama
                    $activeModel = [AiHelper]::OllamaDefaultModel
                    [AiHelper]::InvokeHermes(@(if ($activeModel) { "--model"; $activeModel }))
                }
                2 {
                    # Invoke-Codex-By-Ollama
                    $activeModel = [AiHelper]::OllamaDefaultModel
                    [AiHelper]::InvokeCodex(@(if ($activeModel) { "--model"; $activeModel }))
                }
                3 {
                    [AiHelper]::InvokeOpenClaw(@())
                }
                4 {
                    # Invoke-Clawdbot-By-Ollama
                    $activeModel = [AiHelper]::OllamaDefaultModel
                    [AiHelper]::InvokeClawdbot(@(if ($activeModel) { "--model"; $activeModel }))
                }
                5 {
                    $choices = @("cloud", "local", "auto")
                    $labels = @("Cloud API (Normal)", "Local Ollama", "Auto (Local if online, else Cloud)")
                    $defaultIdx = $choices.IndexOf($providerMode)
                    if ($defaultIdx -lt 0) { $defaultIdx = 0 }
                    $chosenIdx = ([type]"TerminalMenu")::Show("Select AI Provider Mode", $labels, $defaultIdx)
                    if ($chosenIdx -ge 0) {
                        $chosenMode = $choices[$chosenIdx]
                        $configPath = Join-Path -Path $Global:ProfileRepoRoot -ChildPath "profile.config.json"
                        if (Test-Path $configPath) {
                            try {
                                $cfg = Get-Content $configPath -Raw | ConvertFrom-Json
                                $cfg.AiProviderMode = $chosenMode
                                ConvertTo-Json $cfg -Depth 4 | Set-Content $configPath -Force
                                $Global:AiProviderMode = $chosenMode
                                Write-Host "🟢 AI Provider Mode set to '$($labels[$chosenIdx])'." -ForegroundColor Green
                                Start-Sleep -Seconds 1
                            } catch {}
                        }
                    }
                }
                6 {
                    [AiHelper]::SetOllamaModel("")
                }
                7 {
                    [AiHelper]::InstallAIIntegrations()
                }
            }
        }
    }

    static [void] AskAi([string]$query) {
        $errorMessage = $null
        if ([string]::IsNullOrWhiteSpace($query)) {
            if ($Global:Error -and $Global:Error.Count -gt 0) {
                $lastErr = $Global:Error[0]
                $errorMessage = "Last Shell Error:\n$($lastErr | Format-List * -Force | Out-String)"
                if ($lastErr.InvocationInfo) {
                    $errorMessage += "\nInvocation Line: $($lastErr.InvocationInfo.Line)"
                }
            } else {
                Write-Host "No recent console errors found to explain." -ForegroundColor Yellow
                return
            }
        } else {
            $errorMessage = $query
        }

        Write-Host "🤖 Querying local AI for explanation/fix..." -ForegroundColor Cyan
        
        $prompt = @"
Analyze the following PowerShell error or question and provide a brief explanation and a clear, copy-pasteable fix:

$errorMessage
"@
        
        $body = @{
            model = [AiHelper]::OllamaDefaultModel
            prompt = $prompt
            stream = $false
        } | ConvertTo-Json
        
        try {
            $res = Invoke-RestMethod -Method Post -Uri "http://127.0.0.1:11434/api/generate" -Body $body -ContentType "application/json" -TimeoutSec 10
            if ($res.response) {
                Write-Host ""
                Write-Host "🤖 AI Explanation:" -ForegroundColor Green
                Write-Host $res.response.Trim()
            }
        } catch {
            Write-Error "Failed to connect to local Ollama. Ensure Ollama server is running."
        }
    }
}
#endregion



