#region AI TOOLS
# ------------------------------------------------------------------------------
#  Wrappers for AI CLI tools: Gemini CLI, GitHub Copilot, Claude, Ollama, ChatGPT.
# ------------------------------------------------------------------------------

Write-Host "🤖 Loading AI Tools..." -ForegroundColor Cyan

# --- Gemini CLI ---

<#
.SYNOPSIS
Launch Gemini CLI. Pass a prompt or open interactive mode.
.CATEGORY
AI Tools
#>
function Invoke-GeminiChat {
    [CmdletBinding()]
    param([Parameter(Position=0, ValueFromRemainingArguments=$true)][string[]]$PassThruArgs)
    if (-not (Get-Command gemini -ErrorAction SilentlyContinue)) {
        Write-Warning "Gemini CLI not found. Install: npm install -g @google/generative-ai"
        return
    }
    if ($PassThruArgs) { gemini @PassThruArgs } else { gemini }
}

# --- GitHub Copilot ---

<#
.SYNOPSIS
Ask Copilot to suggest a shell command.
.CATEGORY
AI Tools
#>
function Invoke-CopilotSuggest {
    [CmdletBinding()]
    param([Parameter(Mandatory=$true, ValueFromRemainingArguments=$true)][string]$Query)
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        Write-Warning "GitHub CLI 'gh' not found. Install from https://cli.github.com/"
        return
    }
    gh copilot suggest -t shell $Query
}

<#
.SYNOPSIS
Ask Copilot to explain a shell command.
.CATEGORY
AI Tools
#>
function Invoke-CopilotExplain {
    [CmdletBinding()]
    param([Parameter(Mandatory=$true, ValueFromRemainingArguments=$true)][string]$Command)
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        Write-Warning "GitHub CLI 'gh' not found. Install from https://cli.github.com/"
        return
    }
    gh copilot explain $Command
}

# --- Claude ---

<#
.SYNOPSIS
Launch Claude CLI via Antigravity proxy (default) or local Ollama model.
.EXAMPLE
  Invoke-ClaudeChat                     # proxy mode (Antigravity)
  Invoke-ClaudeChat -Local              # local Ollama (qwen3-coder)
  Invoke-ClaudeChat -Local -Model phi3  # custom local model
.CATEGORY
AI Tools
#>
function Invoke-ClaudeChat {
    [CmdletBinding()]
    param(
        [switch]$Local,
        [string]$Model = "qwen3-coder",
        [Parameter(ValueFromRemainingArguments=$true)][string[]]$PassThruArgs
    )
    if (-not (Get-Command claude -ErrorAction SilentlyContinue)) {
        Write-Warning "Claude CLI not found. Install from https://claude.ai/code"
        return
    }
    if ($Local) {
        Write-Host "🤖 Claude → Ollama ($Model)..." -ForegroundColor Yellow
        claude --model $Model @PassThruArgs
    } else {
        Write-Host "🛸 Claude → Antigravity Proxy..." -ForegroundColor Cyan
        claude @PassThruArgs
    }
}

# --- ChatGPT CLI ---

<#
.SYNOPSIS
Launch ChatGPT CLI. Pass a prompt or open interactive mode.
.CATEGORY
AI Tools
#>
function Invoke-ChatGPT {
    [CmdletBinding()]
    param([Parameter(Position=0, ValueFromRemainingArguments=$true)][string[]]$PassThruArgs)
    if (-not (Get-Command chatgpt -ErrorAction SilentlyContinue)) {
        Write-Warning "ChatGPT CLI not found. Install: npm install -g chatgpt-cli"
        return
    }
    if ($PassThruArgs) { chatgpt @PassThruArgs } else { chatgpt }
}

# --- Ollama ---

<#
.SYNOPSIS
Launch an Ollama model interactively. Defaults to llama3.
.EXAMPLE
  Invoke-OllamaChat             # runs llama3
  Invoke-OllamaChat -Model phi3
.CATEGORY
AI Tools
#>
function Invoke-OllamaChat {
    [CmdletBinding()]
    param([string]$Model = "llama3")
    if (-not (Get-Command ollama -ErrorAction SilentlyContinue)) {
        Write-Warning "Ollama not installed or not in PATH."
        return
    }
    Write-Host "🦙 Ollama ($Model)..." -ForegroundColor Yellow
    ollama run $Model
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
        [PSCustomObject]@{ Label = "♊  Gemini CLI";              Action = { if ($Query) { Invoke-GeminiChat $Query } else { Invoke-GeminiChat } } }
        [PSCustomObject]@{ Label = "🐙  GitHub Copilot (explain)"; Action = {
            if ([string]::IsNullOrWhiteSpace($Query)) {
                $Query = Read-Host "Copilot prompt"
            }
            Invoke-CopilotExplain -Command $Query
        }}
        [PSCustomObject]@{ Label = "🛸  Claude (Antigravity proxy)"; Action = { Invoke-ClaudeChat } }
        [PSCustomObject]@{ Label = "🤖  Claude (local Ollama / qwen3-coder)"; Action = { Invoke-ClaudeChat -Local -Model (if ($Model) { $Model } else { "qwen3-coder" }) } }
        [PSCustomObject]@{ Label = "🦙  Ollama (interactive)";    Action = { Invoke-OllamaChat -Model (if ($Model) { $Model } else { "llama3" }) } }
        [PSCustomObject]@{ Label = "🤖  ChatGPT CLI";             Action = { if ($Query) { Invoke-ChatGPT $Query } else { Invoke-ChatGPT } } }
    )

    $selected = 0
    $key = $null

    # Save cursor position and hide it
    [Console]::CursorVisible = $false
    Write-Host ""
    Write-Host "  Select AI agent  (↑↓ to move, Enter to confirm, Esc to cancel)" -ForegroundColor DarkGray
    Write-Host ""
    $menuTop = [Console]::CursorTop

    function Render-AIMenu {
        [Console]::SetCursorPosition(0, $menuTop)
        for ($i = 0; $i -lt $agents.Count; $i++) {
            if ($i -eq $selected) {
                Write-Host "  ▶ $($agents[$i].Label)  " -ForegroundColor Cyan
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

#endregion
