#region CORE SETUP
# ------------------------------------------------------------------------------
#  Initial configuration for the shell environment, theme, and modules.
# ------------------------------------------------------------------------------

# Ensure UTF8 for Icons
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "[*] Loading Enhanced PowerShell Profile... (Core)" -ForegroundColor Cyan

# --- Oh My Posh Theme ---
$env:POSH_THEMES_PATH = Join-Path -Path $env:USERPROFILE -ChildPath "Documents\PowerShell\asset\powershell-themes"
$env:THEME = "neko" # Change your theme here
$themePath = Join-Path -Path $env:POSH_THEMES_PATH -ChildPath "$($env:THEME).omp.json"
if (Test-Path $themePath) {
    if (-not $global:PoshInitialized) {
        oh-my-posh --init --shell pwsh --config $themePath | Invoke-Expression
        $global:PoshInitialized = $true
    }
} else {
    Write-Warning "Oh My Posh theme '$($env:THEME)' not found at '$themePath'."
}

# --- Module Loading & Auto-Healing ---
# List of essential modules. 'Z' is added for smart directory jumping.
$modules = @(
    @{ Name = "PSReadLine";                         Description = "Core CLI Experience" }
    @{ Name = "Terminal-Icons";                     Description = "Rich File Icons" }
    @{ Name = "posh-git";                           Description = "Git Status in Prompt" }
    @{ Name = "z";                                  Description = "Smart Directory Navigation" }
    @{ Name = "Microsoft.PowerShell.ConsoleGuiTools"; Description = "Terminal UI (Out-ConsoleGridView)" } 
    @{ Name = "BurntToast";                         Description = "Windows Notifications" }
)

foreach ($mod in $modules) {
    # 1. Auto-Install if missing (only in interactive console)
    if (-not (Get-Module -ListAvailable -Name $mod.Name)) {
        if ([Console]::IsOutputRedirected -or -not [Environment]::UserInteractive) {
            Write-Warning "[!] Module $($mod.Name) is missing and console is non-interactive. Skipping installation."
            continue
        }
        Write-Host "[+] Installing $($mod.Name) ($($mod.Description))..." -ForegroundColor Cyan
        try {
            Install-Module $mod.Name -Scope CurrentUser -Force -AllowClobber -SkipPublisherCheck -ErrorAction Stop
        } catch {
            Write-Warning "[!] Failed to install $($mod.Name). Skipping."
            continue
        }
    }
    
    # 2. Safe Import
    try {
        if ($mod.Name -eq "Terminal-Icons") {
            Import-Module $mod.Name -Force -ErrorAction SilentlyContinue
        } else {
            Import-Module $mod.Name -ErrorAction SilentlyContinue
        }
    } catch {
        Write-Warning "[x] Error loading $($mod.Name): $_"
    }
}

# --- PSReadLine Options ---
Set-PSReadLineOption -EditMode Windows
# Only enable prediction when VT is supported and the parameter is available (PSReadLine 2.1+)
$psReadLineCmd = Get-Command Set-PSReadLineOption -ErrorAction SilentlyContinue
if ($psReadLineCmd -and $psReadLineCmd.Parameters.ContainsKey('PredictionSource')) {
    try {
        $supportsVt = $Host.UI.SupportsVirtualTerminal -and -not [Console]::IsOutputRedirected
        if ($supportsVt) {
            Set-PSReadLineOption -PredictionSource History
            Set-PSReadLineOption -PredictionViewStyle ListView
        } else {
            Set-PSReadLineOption -PredictionSource None
        }
    } catch {
        # Safe fallback for hosts that do not expose VT info
        Set-PSReadLineOption -PredictionSource None
    }
}
Set-PSReadLineOption -BellStyle None

# Define colors compatible with both older and newer PSReadLine versions
$psReadlineColors = @{
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
}
# InlinePrediction is only supported in PSReadLine 2.1+
if ($psReadLineCmd -and $psReadLineCmd.Parameters.ContainsKey('PredictionSource')) {
    $psReadlineColors["InlinePrediction"] = '#70A99F'
}

try {
    Set-PSReadlineOption -Color $psReadlineColors
} catch {
    # If setting the colors hash table fails, fallback to basic options or ignore
}

# --- PSReadLine Key Bindings ---
if ($Host.Name -eq 'ConsoleHost' -and (Get-Command Set-PSReadLineKeyHandler -ErrorAction SilentlyContinue)) {
    Set-PSReadLineKeyHandler -Key UpArrow -Function HistorySearchBackward
    Set-PSReadLineKeyHandler -Key DownArrow -Function HistorySearchForward
    Set-PSReadLineKeyHandler -Chord 'Ctrl+Spacebar' -Function Complete
    Set-PSReadLineKeyHandler -Key F7 -ScriptBlock {
        $command = Get-History | Out-GridView -Title 'Command History' -PassThru
        if ($command) {
            [Microsoft.PowerShell.PSConsoleReadLine]::RevertLine()
            [Microsoft.PowerShell.PSConsoleReadLine]::Insert($command.CommandLine)
        }
    }
}
# .NET Hotkeys
Set-PSReadLineKeyHandler -Key 'Ctrl+Shift+b' -ScriptBlock { [Microsoft.PowerShell.PSConsoleReadLine]::RevertLine(); [Microsoft.PowerShell.PSConsoleReadLine]::Insert('db'); [Microsoft.PowerShell.PSConsoleReadLine]::AcceptLine() }
Set-PSReadLineKeyHandler -Key 'Ctrl+Shift+t' -ScriptBlock { [Microsoft.PowerShell.PSConsoleReadLine]::RevertLine(); [Microsoft.PowerShell.PSConsoleReadLine]::Insert('dt'); [Microsoft.PowerShell.PSConsoleReadLine]::AcceptLine() }

#endregion

