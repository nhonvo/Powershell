#region AI TOOLS CONFIGURATION
# ------------------------------------------------------------------------------
#  Configuration and shortcuts for AI tools: Gemini, Copilot, Claude, Ollama.
# ------------------------------------------------------------------------------

Write-Host "🤖 Loading AI Configuration..." -ForegroundColor Cyan

# --- Python / Environment Helpers ---
<#
.SYNOPSIS
Creates a Python virtual environment (.venv).
.CATEGORY
AI Tools
#>
function New-PythonVenv {
    Write-Host "🐍 Creating Python virtual environment..." -ForegroundColor Green
    python -m venv .venv
    Write-Host "✅ Created .venv. Run 'activate' to enter."
}

<#
.SYNOPSIS
Activates the local Python virtual environment.
.CATEGORY
AI Tools
#>
function Invoke-VenvActivate {
    if (Test-Path .\.venv\Scripts\Activate.ps1) {
        & .\.venv\Scripts\Activate.ps1
    } else {
        Write-Error "No .venv found in current directory."
    }
}

# --- Prompt Engineering ---
<#
.SYNOPSIS
Saves the last clipboard content as a prompt file.
.CATEGORY
AI Tools
#>
function Save-Prompt {
    [CmdletBinding()]
    param([string]$Name)
    $text = Get-Clipboard
    $path = Join-Path $env:USERPROFILE "Documents\Prompts\$Name.txt"
    $parent = Split-Path $path
    if (!(Test-Path $parent)) { mkdir $parent | Out-Null }
    $text | Set-Content $path
    Write-Host "💾 Prompt saved to $path" -ForegroundColor Green
}

<#
.SYNOPSIS
Reads a saved prompt to clipboard.
.CATEGORY
AI Tools
#>
function Get-Prompt {
    [CmdletBinding()]
    param([string]$Name)
    $path = Join-Path $env:USERPROFILE "Documents\Prompts\$Name.txt"
    if (Test-Path $path) {
        Get-Content $path | Set-Clipboard
        Write-Host "📋 Prompt '$Name' copied to clipboard!" -ForegroundColor Green
    } else {
        Write-Error "Prompt not found at $path"
    }
}

# --- Gemini Chat (Curl Wrapper) ---
<#
.SYNOPSIS
Simple chat with Gemini using curl (requires API Key).
.CATEGORY
AI Tools
#>
function Invoke-GeminiChat {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)][string]$Prompt,
        [string]$Model
    )
    
    if (-not $env:GEMINI_API_KEY) {
        Write-Error "GEMINI_API_KEY is not set. Use 'Set-GeminiKey' first."
        return
    }

    $modelToUse = if ($Model) { $Model } elseif ($env:GEMINI_MODEL) { $env:GEMINI_MODEL } else { "gemini-pro" }
    $url = "https://generativelanguage.googleapis.com/v1beta/models/$modelToUse:generateContent?key=$($env:GEMINI_API_KEY)"
    $body = @{ contents = @(@{ parts = @(@{ text = $Prompt }) }) } | ConvertTo-Json -Depth 5

    Write-Host "♊ Asking Gemini..." -ForegroundColor Cyan
    try {
        $response = Invoke-RestMethod -Uri $url -Method Post -Body $body -ContentType "application/json"
        $answer = $response.candidates[0].content.parts[0].text
        Write-Host $answer -ForegroundColor White
    } catch {
        Write-Error "Gemini API Error: $_"
    }
}

<#
.SYNOPSIS
Check if Ollama is running and listed models.
.CATEGORY
AI Tools
#>
function Get-OllamaModels {
    [CmdletBinding()]
    param()
    if (Get-Command ollama -ErrorAction SilentlyContinue) {
        ollama list
    } else {
        Write-Warning "Ollama is not installed or not in PATH."
    }
}

# --- Claude Configuration ---

<#
.SYNOPSIS
Configures Claude CLI to use Ollama (Local).
.CATEGORY
AI Tools
#>
function Use-ClaudeLocal {
    $env:ANTHROPIC_AUTH_TOKEN = "ollama"
    $env:ANTHROPIC_BASE_URL = "http://localhost:11434"
    Write-Host "🤖 Claude is now using Ollama (Local)." -ForegroundColor Green
}

<#
.SYNOPSIS
Configures Claude CLI to use Anthropic API (Cloud).
.CATEGORY
AI Tools
#>
function Use-ClaudeCloud {
    $env:ANTHROPIC_AUTH_TOKEN = $null
    $env:ANTHROPIC_BASE_URL = $null
    Write-Host "☁️  Claude is now using Anthropic API." -ForegroundColor Cyan
}

# --- GitHub Copilot CLI (via gh) ---

<#
.SYNOPSIS
Wrapper for 'gh copilot suggest'.
.CATEGORY
AI Tools
#>
function Invoke-CopilotSuggest {
    [CmdletBinding()]
    param([Parameter(ValueFromRemainingArguments=$true)]$Query)
    gh copilot suggest -t shell "$Query"
}

<#
.SYNOPSIS
Wrapper for 'gh copilot explain'.
.CATEGORY
AI Tools
#>
function Invoke-CopilotExplain {
    [CmdletBinding()]
    param([Parameter(ValueFromRemainingArguments=$true)]$Command)
    gh copilot explain "$Command"
}

# --- Gemini CLI Configuration ---

<#
.SYNOPSIS
Sets the Gemini API Key for the current session.
.CATEGORY
AI Tools
#>
function Set-GeminiKey {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)][string]$Key,
        [switch]$Persist
    )
    $env:GEMINI_API_KEY = $Key
    if ($Persist) {
        [System.Environment]::SetEnvironmentVariable("GEMINI_API_KEY", $Key, "User")
    }
    Write-Host "🔑 Gemini API Key set for this session." -ForegroundColor Green
}

<#
.SYNOPSIS
Sets the Gemini model for the current session (and optionally persists it).
.CATEGORY
AI Tools
#>
function Set-GeminiModel {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)][string]$Model,
        [switch]$Persist
    )
    $env:GEMINI_MODEL = $Model
    if ($Persist) {
        [System.Environment]::SetEnvironmentVariable("GEMINI_MODEL", $Model, "User")
    }
    Write-Host "📌 Gemini model set for this session: $Model" -ForegroundColor Green
}

# --- Codex CLI Integration ---

<#
.SYNOPSIS
Wrapper for Codex CLI (Oracle/Agent).
.CATEGORY
AI Tools
#>
function Invoke-Codex {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, ValueFromRemainingArguments=$true)]
        [string]$Prompt,
        [string]$Model = "gpt-5"
    )
    if (Get-Command codex -ErrorAction SilentlyContinue) {
        Write-Host "🔮 Asking Codex ($Model)..." -ForegroundColor Magenta
        codex exec "$Prompt" --model $Model --dangerously-bypass-approvals-and-sandbox
    } else {
        Write-Error "Codex CLI not found."
    }
}

# --- Ollama Integration ---

<#
.SYNOPSIS
Wrapper for Ollama run.
.CATEGORY
AI Tools
#>
function Invoke-Ollama {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$Prompt,
        [string]$Model = "llama3"
    )
    if (Get-Command ollama -ErrorAction SilentlyContinue) {
        Write-Host "🦙 Asking Ollama ($Model)..." -ForegroundColor Yellow
        ollama run $Model $Prompt
    } else {
        Write-Error "Ollama CLI not found."
    }
}

# --- Unified Multi-Agent Flow ---

<#
.SYNOPSIS
Unified AI Router. Selects the best agent or specific agent for the task.
.EXAMPLE
ai "How do I center a div?" (Defaults to Gemini)
ai -c "How do I center a div?" (Copilot)
ai -o "How do I center a div?" (Ollama)
ai -x "Analyze this bug" (Codex)
.CATEGORY
AI Tools
#>
function Show-AIAgentOptions {
    [CmdletBinding()]
    param()

    $hasGh = [bool](Get-Command gh -ErrorAction SilentlyContinue)
    $hasCodex = [bool](Get-Command codex -ErrorAction SilentlyContinue)
    $hasOllama = [bool](Get-Command ollama -ErrorAction SilentlyContinue)
    $hasGeminiKey = [bool]$env:GEMINI_API_KEY
    $geminiModel = if ($env:GEMINI_MODEL) { $env:GEMINI_MODEL } else { "gemini-pro" }

    Write-Host "AI agents available:" -ForegroundColor Cyan
    Write-Host "  - Gemini (default)  : ai ""your question""" -ForegroundColor White
    Write-Host "    Status           : " -NoNewline -ForegroundColor DarkGray
    Write-Host ($(if ($hasGeminiKey) { "READY (GEMINI_API_KEY set, model $geminiModel)" } else { "MISSING KEY (run Set-GeminiKey)" })) -ForegroundColor ($(if ($hasGeminiKey) { "Green" } else { "Yellow" }))

    Write-Host "  - Copilot (gh)      : ai -c ""your question""" -ForegroundColor White
    Write-Host "    Status           : " -NoNewline -ForegroundColor DarkGray
    Write-Host ($(if ($hasGh) { "READY (gh found)" } else { "MISSING (install GitHub CLI + Copilot)" })) -ForegroundColor ($(if ($hasGh) { "Green" } else { "Yellow" }))

    Write-Host "  - Ollama (local)    : ai -o ""your question"" -Model llama3" -ForegroundColor White
    Write-Host "    Status           : " -NoNewline -ForegroundColor DarkGray
    Write-Host ($(if ($hasOllama) { "READY (ollama found)" } else { "MISSING (install Ollama)" })) -ForegroundColor ($(if ($hasOllama) { "Green" } else { "Yellow" }))

    Write-Host "  - Codex (CLI)       : ai -x ""your question"" -Model gpt-5" -ForegroundColor White
    Write-Host "    Status           : " -NoNewline -ForegroundColor DarkGray
    Write-Host ($(if ($hasCodex) { "READY (codex found)" } else { "MISSING (install Codex CLI)" })) -ForegroundColor ($(if ($hasCodex) { "Green" } else { "Yellow" }))

    Write-Host ""
    Write-Host "Shortcuts:" -ForegroundColor Cyan
    Write-Host "  ai -g  (Gemini)   ai -c  (Copilot)   ai -o  (Ollama)   ai -x  (Codex)" -ForegroundColor White
    Write-Host "Tip: run 'ai -list' to show this again." -ForegroundColor DarkGray
}

function Invoke-MultiAgent {
    [Alias("ai")]
    [CmdletBinding(DefaultParameterSetName="Gemini")]
    param(
        [Parameter(Position=0)]
        [string]$Query,

        [Parameter(ParameterSetName="Copilot")]
        [Alias("c")]
        [switch]$UseCopilot,

        [Parameter(ParameterSetName="Ollama")]
        [Alias("o")]
        [switch]$UseOllama,

        [Parameter(ParameterSetName="Codex")]
        [Alias("x")]
        [switch]$UseCodex,

        [Parameter(ParameterSetName="Gemini")]
        [Alias("g")]
        [switch]$UseGemini,

        [Parameter(ParameterSetName="Help")]
        [Alias("list", "help", "?")]
        [switch]$ShowOptions,

        [string]$Model
    )

    if ($ShowOptions -or [string]::IsNullOrWhiteSpace($Query)) {
        Show-AIAgentOptions
        return
    }

    switch ($PSCmdlet.ParameterSetName) {
        "Copilot" {
            # Default to explain for general queries in this flow
            Invoke-CopilotExplain -Command $Query
        }
        "Ollama" {
            $m = if ($Model) { $Model } else { "llama3" }
            Invoke-Ollama -Prompt $Query -Model $m
        }
        "Codex" {
            $m = if ($Model) { $Model } else { "gpt-5" }
            Invoke-Codex -Prompt $Query -Model $m
        }
        "Gemini" {
            Invoke-GeminiChat -Prompt $Query -Model $Model
        }
        Default {
            Invoke-GeminiChat -Prompt $Query -Model $Model
        }
    }
}

#endregion
