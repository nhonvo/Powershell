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

    static [void] EnsureOllamaProxy() {
        $proxyPort = 11435
        try {
            $null = Invoke-RestMethod -Uri "http://127.0.0.1:$proxyPort/" -TimeoutSec 1 -ErrorAction Stop
        } catch {
            Write-Host "[AI] Ollama Proxy is not running on port $proxyPort. Starting..." -ForegroundColor Yellow
            $currentDir = $PSScriptRoot
            # Resolve scripts directory relatively
            $rootDir = Split-Path -Parent -Path $currentDir
            $proxyScript = Join-Path $rootDir "Scripts\ollama-proxy.js"
            if (-not (Test-Path $proxyScript)) {
                $proxyScript = Join-Path $rootDir "tests\ollama-proxy.js"
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

    static [void] EnsureOllamaServer() {
        try {
            $null = Invoke-RestMethod -Uri "http://127.0.0.1:11434/" -TimeoutSec 1 -ErrorAction Stop
        } catch {
            Write-Host "[AI] Ollama is not running. Auto-starting..." -ForegroundColor Yellow
            Initialize-OllamaServer
        }
        [AiHelper]::EnsureOllamaProxy()
    }

    static [void] InvokeClaude([string[]]$ArgsList) {
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

            & ollama.exe launch claude @flags @ArgsList
        } finally {
            $env:OLLAMA_HOST = $oldOllamaHost
            $env:ANTHROPIC_BASE_URL = $oldAnthropicBaseUrl
            $env:NODE_OPTIONS = $oldNodeOptions
        }
    }

    static [void] InvokeCodex([string[]]$ArgsList) {
        Ensure-OllamaServer

        $oldOllamaHost = $env:OLLAMA_HOST
        $oldBaseUrl = $env:OPENAI_BASE_URL
        $oldApiKey = $env:OPENAI_API_KEY
        $oldNodeOptions = $env:NODE_OPTIONS
        try {
            $env:OLLAMA_HOST = "127.0.0.1:11434"
            $env:OPENAI_BASE_URL = "http://127.0.0.1:11435/v1"
            $env:OPENAI_API_KEY = "ollama"
            $env:NODE_OPTIONS = if ($env:NODE_OPTIONS) { "$env:NODE_OPTIONS --dns-result-order=ipv4first" } else { "--dns-result-order=ipv4first" }

            $flags = @()
            if ($ArgsList -notcontains "-c" -and $ArgsList -notcontains "--config") {
                $flags += "-c", "model_provider=ollama_custom"
            }
            if ($ArgsList -notcontains "--model") {
                $flags += "--model", [AiHelper]::OllamaDefaultModel
            }

            & codex.cmd @flags @ArgsList
        } finally {
            $env:OLLAMA_HOST = $oldOllamaHost
            $env:OPENAI_BASE_URL = $oldBaseUrl
            $env:OPENAI_API_KEY = $oldApiKey
            $env:NODE_OPTIONS = $oldNodeOptions
        }
    }

    static [void] InvokeOpenClaw([string[]]$ArgsList) {
        Ensure-OllamaServer

        $oldOllamaHost = $env:OLLAMA_HOST
        try {
            $env:OLLAMA_HOST = "127.0.0.1:11434"
            $flags = @()
            if ($ArgsList -notcontains "--model") {
                $flags += "--model", [AiHelper]::OllamaDefaultModel
            }
            & ollama.exe launch openclaw @flags @ArgsList
        } finally {
            $env:OLLAMA_HOST = $oldOllamaHost
        }
    }

    static [void] InvokeClawdbot([string[]]$ArgsList) {
        [AiHelper]::InvokeOpenClaw($ArgsList)
    }

    static [void] InvokeHermes([string[]]$ArgsList) {
        Ensure-OllamaServer

        $oldOllamaHost = $env:OLLAMA_HOST
        try {
            $env:OLLAMA_HOST = "127.0.0.1:11434"
            $flags = @()
            if ($ArgsList -notcontains "--model") {
                $flags += "--model", [AiHelper]::OllamaDefaultModel
            }
            & ollama.exe launch hermes @flags @ArgsList
        } finally {
            $env:OLLAMA_HOST = $oldOllamaHost
        }
    }

    static [void] InvokeHermesDesktop([string[]]$ArgsList) {
        Ensure-OllamaServer

        $oldOllamaHost = $env:OLLAMA_HOST
        try {
            $env:OLLAMA_HOST = "127.0.0.1:11434"
            $flags = @()
            if ($ArgsList -notcontains "--model") {
                $flags += "--model", [AiHelper]::OllamaDefaultModel
            }
            & ollama.exe launch hermes-desktop @flags @ArgsList
        } finally {
            $env:OLLAMA_HOST = $oldOllamaHost
        }
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

        Start-Process -FilePath $ollamaPath -ArgumentList "serve" -WindowStyle Normal -ErrorAction SilentlyContinue

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

    static [void] InvokeMultiAgent([string]$Query, [string]$ParameterSetName, [string]$Model) {
        if ($ParameterSetName -and $ParameterSetName -ne "Menu") {
            switch ($ParameterSetName) {
                "Gemini"  { if ($Query) { Invoke-GeminiChat $Query } else { Invoke-GeminiChat }; return }
                "Copilot" {
                    if ([string]::IsNullOrWhiteSpace($Query)) { Write-Warning "Copilot requires a prompt."; return }
                    Invoke-CopilotExplain -Command $Query; return
                }
                "Ollama"  { [AiHelper]::InvokeOpenClaw(@(if ($Model) { "--model"; $Model })); return }
                "Claude"  { [AiHelper]::InvokeClaude(@()); return }
                "Local"   { [AiHelper]::InvokeClaude(@("-Local", (if ($Model) { "--model"; $Model } else { "--model"; "qwen3-coder" }))); return }
                "ChatGPT" { if ($Query) { Invoke-ChatGPT $Query } else { Invoke-ChatGPT }; return }
            }
        }

        # Build list of options for the TUI agent menu
        $agents = @(
            [PSCustomObject]@{ Label = "[Gemini] Gemini CLI";              Action = { if ($Query) { Invoke-GeminiChat $Query } else { Invoke-GeminiChat } } }
            [PSCustomObject]@{ Label = "[Copilot] GitHub Copilot (explain)"; Action = {
                $prompt = $Query
                if ([string]::IsNullOrWhiteSpace($prompt)) { $prompt = Read-Host "Copilot prompt" }
                Invoke-CopilotExplain -Command $prompt
            }}
            [PSCustomObject]@{ Label = "[Claude] Claude (Antigravity proxy)"; Action = { [AiHelper]::InvokeClaude(@()) } }
            [PSCustomObject]@{ Label = "[Local Claude] Claude (local Ollama)"; Action = { [AiHelper]::InvokeClaude(@("--model", (if ($Model) { $Model } else { "qwen3-coder" }))) } }
            [PSCustomObject]@{ Label = "[Ollama] Ollama (interactive)";    Action = { [AiHelper]::InvokeOpenClaw(@(if ($Model) { "--model"; $Model })) } }
            [PSCustomObject]@{ Label = "[ChatGPT] ChatGPT CLI";             Action = { if ($Query) { Invoke-ChatGPT $Query } else { Invoke-ChatGPT } } }
        )

        $labels = @()
        foreach ($agent in $agents) {
            $labels += $agent.Label
        }

        $selected = ([type]"TerminalMenu")::Show("Select AI Agent", $labels, 0)
        if ($selected -ge 0) {
            Write-Host ""
            & $agents[$selected].Action
        } else {
            Write-Host ""
            Write-Host "  Cancelled." -ForegroundColor DarkGray
        }
    }
}
#endregion
