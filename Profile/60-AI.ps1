#region AI TOOLS
# ------------------------------------------------------------------------------
#  Wrappers for AI CLI tools: Gemini CLI, GitHub Copilot, Claude, Ollama, ChatGPT.
# ------------------------------------------------------------------------------

Write-Host "[AI] Loading AI Tools..." -ForegroundColor Cyan

$Script:AiToolsDir = $PSScriptRoot

$script:OllamaDefaultModelFile = Join-Path $env:USERPROFILE ".ollama_default_model"
$script:OllamaDefaultModel = "qwen3:1.7b"

if (Test-Path $script:OllamaDefaultModelFile) {
    try {
        $savedModel = (Get-Content $script:OllamaDefaultModelFile -ErrorAction SilentlyContinue).Trim()
        if ($savedModel) {
            $script:OllamaDefaultModel = $savedModel
        }
    } catch {}
}

# --- Unified AI Router ---

<#
.SYNOPSIS
Interactive AI tool selector. Use arrow keys to pick an agent, then launch it.
.EXAMPLE
  ai              → arrow-key menu to pick agent
  ai -g "prompt"  → Gemini CLI directly
  ai -c "prompt"  → Copilot directly
  ai -o           → Ollama directly
  ai -claude      → Claude (proxy)
  ai -local       → Claude (local Ollama)
  ai -gpt         → ChatGPT CLI
.CATEGORY
AI Tools
#>
function Invoke-MultiAgent {
    [CmdletBinding(DefaultParameterSetName="Menu")]
    param(
        [Parameter(Position=0)][string]$Query,

        [Parameter(ParameterSetName="Gemini")][Alias("g")][switch]$UseGemini,
        [Parameter(ParameterSetName="Copilot")][Alias("c")][switch]$UseCopilot,
        [Parameter(ParameterSetName="Ollama")][Alias("o")][switch]$UseOllama,
        [Parameter(ParameterSetName="Claude")][switch]$UseClaude,
        [Parameter(ParameterSetName="Local")][switch]$UseLocal,
        [Parameter(ParameterSetName="ChatGPT")][Alias("gpt")][switch]$UseChatGPT,

        [string]$Model
    )

    # --- Direct dispatch (flag provided) ---
    switch ($PSCmdlet.ParameterSetName) {
        "Gemini"  { if ($Query) { Invoke-GeminiChat $Query } else { Invoke-GeminiChat }; return }
        "Copilot" {
            if ([string]::IsNullOrWhiteSpace($Query)) { Write-Warning "Copilot requires a prompt."; return }
            Invoke-CopilotExplain -Command $Query; return
        }
        "Ollama"  { Invoke-OllamaChat -Model (if ($Model) { $Model } else { "llama3" }); return }
        "Claude"  { Invoke-ClaudeChat; return }
        "Local"   { Invoke-ClaudeChat -Local -Model (if ($Model) { $Model } else { "qwen3-coder" }); return }
        "ChatGPT" { if ($Query) { Invoke-ChatGPT $Query } else { Invoke-ChatGPT }; return }
    }

    # --- Arrow-key menu ---
    $agents = @(
        [PSCustomObject]@{ Label = "[Gemini] Gemini CLI";              Action = { if ($Query) { Invoke-GeminiChat $Query } else { Invoke-GeminiChat } } }
        [PSCustomObject]@{ Label = "[Copilot] GitHub Copilot (explain)"; Action = {
            if ([string]::IsNullOrWhiteSpace($Query)) {
                $Query = Read-Host "Copilot prompt"
            }
            Invoke-CopilotExplain -Command $Query
        }}
        [PSCustomObject]@{ Label = "[Claude] Claude (Antigravity proxy)"; Action = { Invoke-ClaudeChat } }
        [PSCustomObject]@{ Label = "[Local Claude] Claude (local Ollama / qwen3-coder)"; Action = { Invoke-ClaudeChat -Local -Model (if ($Model) { $Model } else { "qwen3-coder" }) } }
        [PSCustomObject]@{ Label = "[Ollama] Ollama (interactive)";    Action = { Invoke-OllamaChat -Model (if ($Model) { $Model } else { "llama3" }) } }
        [PSCustomObject]@{ Label = "[ChatGPT] ChatGPT CLI";             Action = { if ($Query) { Invoke-ChatGPT $Query } else { Invoke-ChatGPT } } }
    )

    $selected = 0
    $key = $null

    # Save cursor position and hide it
    [Console]::CursorVisible = $false
    Write-Host ""
    Write-Host "  Select AI agent  (Up/Down to move, Enter to confirm, Esc to cancel)" -ForegroundColor DarkGray
    Write-Host ""
    $menuTop = [Console]::CursorTop

    function Render-AIMenu {
        [Console]::SetCursorPosition(0, $menuTop)
        for ($i = 0; $i -lt $agents.Count; $i++) {
            if ($i -eq $selected) {
                Write-Host "  > $($agents[$i].Label)  " -ForegroundColor Cyan
            } else {
                Write-Host "    $($agents[$i].Label)  " -ForegroundColor DarkGray
            }
        }
    }

    Render-AIMenu

    while ($true) {
        $key = [Console]::ReadKey($true)
        switch ($key.Key) {
            'UpArrow'   { if ($selected -gt 0) { $selected-- }; Render-AIMenu }
            'DownArrow' { if ($selected -lt $agents.Count - 1) { $selected++ }; Render-AIMenu }
            'Enter' {
                [Console]::CursorVisible = $true
                Write-Host ""
                & $agents[$selected].Action
                return
            }
            'Escape' {
                [Console]::CursorVisible = $true
                Write-Host ""
                Write-Host "  Cancelled." -ForegroundColor DarkGray
                return
            }
        }
    }
}

# --- Ollama / Claude Code Integration ---

function Ensure-OllamaProxy {
    $proxyPort = 11435
    try {
        $null = Invoke-RestMethod -Uri "http://127.0.0.1:$proxyPort/" -TimeoutSec 1 -ErrorAction Stop
    } catch {
        Write-Host "[AI] Ollama Proxy is not running on port $proxyPort. Starting..." -ForegroundColor Yellow
        
        $rootDir = Split-Path -Parent -Path $Script:AiToolsDir
        $proxyScript = Join-Path $rootDir "Scripts\ollama-proxy.js"
        
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

function Ensure-OllamaServer {
    try {
        $null = Invoke-RestMethod -Uri "http://127.0.0.1:11434/" -TimeoutSec 1 -ErrorAction Stop
    } catch {
        Write-Host "[AI] Ollama is not running. Auto-starting..." -ForegroundColor Yellow
        Initialize-OllamaServer
    }
    Ensure-OllamaProxy
}

function Invoke-Claude-By-Ollama {
    Ensure-OllamaServer
    
    $oldNodeOptions = $env:NODE_OPTIONS
    try {
        # Force Node.js (Claude Code) to resolve localhost to IPv4 (127.0.0.1) instead of IPv6 ([::1])
        $env:NODE_OPTIONS = if ($env:NODE_OPTIONS) { "$env:NODE_OPTIONS --dns-result-order=ipv4first" } else { "--dns-result-order=ipv4first" }
        
        $flags = @()
        if ($args -notcontains "--model") {
            $flags += "--model", $script:OllamaDefaultModel
        }
        
        if ($MyInvocation.ExpectingInput) {
            $input | & ollama.exe launch claude @flags @args
        } else {
            & ollama.exe launch claude @flags @args
        }
    } finally {
        $env:NODE_OPTIONS = $oldNodeOptions
    }
}

function Invoke-Codex-By-Ollama {
    Ensure-OllamaServer
    
    $oldBaseUrl = $env:OPENAI_BASE_URL
    $oldApiKey = $env:OPENAI_API_KEY
    $oldNodeOptions = $env:NODE_OPTIONS
    try {
        $env:OPENAI_BASE_URL = "http://127.0.0.1:11435/v1"
        $env:OPENAI_API_KEY = "ollama"
        # Force Node.js (Codex CLI) to resolve localhost to IPv4 (127.0.0.1) instead of IPv6 ([::1])
        $env:NODE_OPTIONS = if ($env:NODE_OPTIONS) { "$env:NODE_OPTIONS --dns-result-order=ipv4first" } else { "--dns-result-order=ipv4first" }
        
        $flags = @()
        if ($args -notcontains "-c" -and $args -notcontains "--config") {
            $flags += "-c", "model_provider=ollama_custom"
        }
        if ($args -notcontains "--model") {
            $flags += "--model", $script:OllamaDefaultModel
        }
        
        if ($MyInvocation.ExpectingInput) {
            $input | & codex.cmd @flags @args
        } else {
            & codex.cmd @flags @args
        }
    } finally {
        $env:OPENAI_BASE_URL = $oldBaseUrl
        $env:OPENAI_API_KEY = $oldApiKey
        $env:NODE_OPTIONS = $oldNodeOptions
    }
}

function Invoke-OpenClaw-By-Ollama {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromRemainingArguments=$true)][string[]]$ArgsList
    )
    Ensure-OllamaServer
    
    $flags = @()
    if ($ArgsList -notcontains "--model") {
        $flags += "--model", $script:OllamaDefaultModel
    }
    & ollama.exe launch openclaw @flags @ArgsList
}

# Note: aliases for clawdbot also supported
function Invoke-Clawdbot-By-Ollama {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromRemainingArguments=$true)][string[]]$ArgsList
    )
    Invoke-OpenClaw-By-Ollama @ArgsList
}

function Invoke-Hermes-By-Ollama {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromRemainingArguments=$true)][string[]]$ArgsList
    )
    $flags = @()
    if ($ArgsList -notcontains "--model") {
        $flags += "--model", $script:OllamaDefaultModel
    }
    & ollama.exe launch hermes @flags @ArgsList
}

function Invoke-HermesDesktop-By-Ollama {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromRemainingArguments=$true)][string[]]$ArgsList
    )
    $flags = @()
    if ($ArgsList -notcontains "--model") {
        $flags += "--model", $script:OllamaDefaultModel
    }
    & ollama.exe launch hermes-desktop @flags @ArgsList
}

# --- Ollama Server Initialization ---

function Initialize-OllamaServer {
    [CmdletBinding()]
    param()
    
    $port = 11434
    Write-Host "[Ollama] Resetting port $port..." -ForegroundColor Cyan
    
    # Find process using the port
    $connection = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($connection) {
        $pidToKill = $connection.OwningProcess
        $proc = Get-Process -Id $pidToKill -ErrorAction SilentlyContinue
        if ($proc) {
            Write-Host "[Ollama] Killing existing process '$($proc.Name)' (PID $pidToKill) on port $port..." -ForegroundColor Yellow
            Stop-Process -Id $pidToKill -Force
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
    
    # Start Ollama server in a new visible window so the user can check the logs directly
    Start-Process -FilePath $ollamaPath -ArgumentList "serve" -WindowStyle Normal -ErrorAction SilentlyContinue
    
    # Wait for server to respond
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

# --- AI Integrations Installer ---

function Invoke-Npm {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromRemainingArguments=$true)][string[]]$ArgsList
    )
    & npm @ArgsList
}

function Install-AIIntegrations {
    [CmdletBinding()]
    param()
    
    # 1. Claude Code
    if (-not (Get-Command "claude" -ErrorAction SilentlyContinue)) {
        Write-Host "[AI] Installing Claude Code via npm..." -ForegroundColor Cyan
        Invoke-Npm install -g @anthropic-ai/claude-code
    } else {
        Write-Host "[AI] Claude Code is already installed." -ForegroundColor Green
    }
    
    # 2. Codex CLI
    if (-not (Get-Command "codex" -ErrorAction SilentlyContinue)) {
        Write-Host "[AI] Installing Codex CLI via npm..." -ForegroundColor Cyan
        Invoke-Npm install -g @openai/codex
    } else {
        Write-Host "[AI] Codex CLI is already installed." -ForegroundColor Green
    }
    
    # 3. OpenClaw
    if (-not (Get-Command "openclaw" -ErrorAction SilentlyContinue)) {
        Write-Host "[AI] Installing OpenClaw via npm..." -ForegroundColor Cyan
        Invoke-Npm install -g openclaw
    } else {
        Write-Host "[AI] OpenClaw is already installed." -ForegroundColor Green
    }
}

<#
.SYNOPSIS
Lists local Ollama models and allows setting the default model used by the local wrapper integrations.
.EXAMPLE
  model-o
  model-o qwen3:4b
.CATEGORY
  AI Tools
#>
function Set-OllamaModel {
    [CmdletBinding()]
    param(
        [Parameter(Position=0)]
        [string]$ModelName
    )

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
            $script:OllamaDefaultModel = $ModelName
            $script:OllamaDefaultModel | Out-File -FilePath $script:OllamaDefaultModelFile -Force -Encoding utf8
            Write-Host "🟢 Default Ollama model set to '$ModelName'." -ForegroundColor Green
        } else {
            Write-Error "Model '$ModelName' is not available locally. Available models: $($localModels -join ', ')"
        }
        return
    }

    # Interactive selector
    Write-Host ""
    Write-Host "🤖 Select Default Ollama Model:" -ForegroundColor Cyan
    Write-Host "==============================" -ForegroundColor Cyan
    for ($i = 0; $i -lt $localModels.Count; $i++) {
        $m = $localModels[$i]
        if ($m -eq $script:OllamaDefaultModel) {
            Write-Host "  ▶  [$($i + 1)] $m (Active)" -ForegroundColor Green
        } else {
            Write-Host "     [$($i + 1)] $m" -ForegroundColor Gray
        }
    }
    Write-Host "     [Q] Cancel / Exit" -ForegroundColor Red
    Write-Host ""

    $choice = Read-Host "Select model [1-$($localModels.Count)]"
    if ($choice -eq "Q" -or $choice -eq "q" -or [string]::IsNullOrWhiteSpace($choice)) {
        Write-Host "Cancelled." -ForegroundColor Yellow
        return
    }

    if ([int]::TryParse($choice, [ref]$idx) -and $idx -ge 1 -and $idx -le $localModels.Count) {
        $selectedModel = $localModels[$idx - 1]
        $script:OllamaDefaultModel = $selectedModel
        $selectedModel | Out-File -FilePath $script:OllamaDefaultModelFile -Force -Encoding utf8
        Write-Host "🟢 Default Ollama model set to '$selectedModel'." -ForegroundColor Green
    } else {
        Write-Error "Invalid selection."
    }
}

#endregion
