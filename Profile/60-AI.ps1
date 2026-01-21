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
    param([Parameter(Mandatory=$true)][string]$Prompt)
    
    if (-not $env:GEMINI_API_KEY) {
        Write-Error "GEMINI_API_KEY is not set. Use 'Set-GeminiKey' first."
        return
    }

    $url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key=$($env:GEMINI_API_KEY)"
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
    param([Parameter(Mandatory=$true)][string]$Key)
    $env:GEMINI_API_KEY = $Key
    Write-Host "🔑 Gemini API Key set for this session." -ForegroundColor Green
}

#endregion
