#region CORE SETUP
# ------------------------------------------------------------------------------
#  Initial configuration for the shell environment, theme, and modules.
# ------------------------------------------------------------------------------

Write-Host "🚀 Loading Enhanced PowerShell Profile... (Core)" -ForegroundColor Cyan

# --- Oh My Posh Theme ---
$env:POSH_THEMES_PATH = Join-Path -Path $env:USERPROFILE -ChildPath "Documents\PowerShell\powershell-themes"
$env:THEME = "neko" # Change your theme here
$themePath = Join-Path -Path $env:POSH_THEMES_PATH -ChildPath "$($env:THEME).omp.json"
if (Test-Path $themePath) {
    oh-my-posh --init --shell pwsh --config $themePath | Invoke-Expression
} else {
    Write-Warning "Oh My Posh theme '$($env:THEME)' not found at '$themePath'."
}

# --- Module Loading ---
if ($Host.Name -eq 'ConsoleHost' -or $host.Name -eq 'Visual Studio Code Host') {
    try {
        Import-Module PSReadLine -Force -ErrorAction SilentlyContinue
        Import-Module Terminal-Icons -Force -ErrorAction SilentlyContinue
        if (Get-Module -ListAvailable -Name posh-git) {
            Import-Module posh-git -Force -ErrorAction SilentlyContinue
        }
    } catch {
        Write-Warning "An error occurred while loading modules: $_"
    }
}

# --- PSReadLine Options ---
Set-PSReadLineOption -EditMode Windows
Set-PSReadLineOption -PredictionSource History
Set-PSReadLineOption -PredictionViewStyle ListView
Set-PSReadLineOption -BellStyle None
Set-PSReadlineOption -Color @{
    "Command"          = [ConsoleColor]::Green
    "Parameter"        = [ConsoleColor]::Gray
    "Operator"         = [ConsoleColor]::Magenta
    "Variable"         = [ConsoleColor]::Yellow
    "String"           = [ConsoleColor]::Cyan
    "Number"           = [ConsoleColor]::White
    "Type"             = [ConsoleColor]::Blue
    "Comment"          = [ConsoleColor]::DarkGreen
    "Keyword"          = [ConsoleColor]::DarkYellow
    "Error"            = [ConsoleColor]::Red
    "InlinePrediction" = '#70A99F'
}

# --- PSReadLine Key Bindings ---
Set-PSReadLineKeyHandler -Key UpArrow -Function HistorySearchBackward
Set-PSReadLineKeyHandler -Key DownArrow -Function HistorySearchForward
Set-PSReadLineKeyHandler -Key 'Ctrl+Spacebar' -Function Complete
Set-PSReadLineKeyHandler -Key F7 -ScriptBlock {
    $command = Get-History | Out-GridView -Title 'Command History' -PassThru
    if ($command) {
        [Microsoft.PowerShell.PSConsoleReadLine]::RevertLine()
        [Microsoft.PowerShell.PSConsoleReadLine]::Insert($command.CommandLine)
    }
}
# .NET Hotkeys
Set-PSReadLineKeyHandler -Key 'Ctrl+Shift+b' -ScriptBlock { [Microsoft.PowerShell.PSConsoleReadLine]::RevertLine(); [Microsoft.PowerShell.PSConsoleReadLine]::Insert("db"); [Microsoft.PowerShell.PSConsoleReadLine]::AcceptLine() }
Set-PSReadLineKeyHandler -Key 'Ctrl+Shift+t' -ScriptBlock { [Microsoft.PowerShell.PSConsoleReadLine]::RevertLine(); [Microsoft.PowerShell.PSConsoleReadLine]::Insert("dt"); [Microsoft.PowerShell.PSConsoleReadLine]::AcceptLine() }
# Auto-pairing brackets and quotes
Set-PSReadLineKeyHandler -Key '(', '{', '[', '"', "'" -ScriptBlock {
    param($key, $arg)
    $openChar = $key.KeyChar
    $closeChar = switch ($openChar) { '(' { ')' } '{' { '}' } '[' { ']' } '"' { '"' } "'" { "'" } }
    [Microsoft.PowerShell.PSConsoleReadLine]::Insert("$openChar$closeChar")
    [Microsoft.PowerShell.PSConsoleReadLine]::BackwardChar()
}
# Smart backspace
Set-PSReadLineKeyHandler -Key 'Backspace' -ScriptBlock {
    $line = ''; $cursor = 0
    [Microsoft.PowerShell.PSConsoleReadLine]::GetBufferState([ref]$line, [ref]$cursor)
    if ($cursor -gt 0) {
        $charBehind = $line[$cursor - 1]
        $charAhead = if ($cursor -lt $line.Length) { $line[$cursor] } else { $null }
        $pairs = @{ '(' = ')'; '{' = '}'; '[' = ']'; '"' = '"'; "'" = "'" }
        if ($pairs.ContainsKey($charBehind) -and $pairs[$charBehind] -eq $charAhead) {
            [Microsoft.PowerShell.PSConsoleReadLine]::Delete($cursor - 1, 2)
        } else {
            [Microsoft.PowerShell.PSConsoleReadLine]::BackwardDeleteChar()
        }
    }
}

#endregion
