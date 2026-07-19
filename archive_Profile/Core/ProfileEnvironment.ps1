#region PROFILE ENVIRONMENT
# ==============================================================================
#  Shell environment setup, PSReadLine settings, and community modules loading.
# ==============================================================================

class ProfileEnvironment {
    static [void] InitializeSession() {
        # Ensure UTF8 for Icons
        [Console]::OutputEncoding = [System.Text.Encoding]::UTF8

        if (-not $Global:AiMode -and $Global:VerboseStartup) {
            Write-Host "[*] Loading Enhanced PowerShell Profile... (Core)" -ForegroundColor Cyan
        }



        # --- Module Loading & Auto-Healing ---
        $modules = @(
            @{ Name = "PSReadLine";                         Description = "Core CLI Experience" }
            @{ Name = "z";                                  Description = "Smart Directory Navigation" }
        )
        if (-not $Global:AiMode) {
            $modules += @(
                @{ Name = "Terminal-Icons";                     Description = "Rich File Icons" }
                @{ Name = "posh-git";                           Description = "Git Status in Prompt" }
                @{ Name = "Microsoft.PowerShell.ConsoleGuiTools"; Description = "Terminal UI (Out-ConsoleGridView)" }
                @{ Name = "BurntToast";                         Description = "Windows Notifications" }
            )
        }

        foreach ($mod in $modules) {
            # Auto-Install if missing (only in interactive console)
            if (-not (Get-Module -ListAvailable -Name $mod.Name)) {
                if ($Global:AiMode -or [Console]::IsOutputRedirected -or -not [Environment]::UserInteractive) {
                    if (-not $Global:AiMode) {
                        Write-Warning "[!] Module $($mod.Name) is missing and console is non-interactive. Skipping installation."
                    }
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

            # Safe Import
            try {
                if ($mod.Name -eq "Terminal-Icons") {
                    Import-Module $mod.Name -Force -ErrorAction SilentlyContinue
                } else {
                    Import-Module $mod.Name -ErrorAction SilentlyContinue
                }
            } catch {
                if (-not $Global:AiMode) {
                    Write-Warning "[x] Error loading $($mod.Name): $_"
                }
            }
        }

        # --- PSReadLine Options ---
        Set-PSReadLineOption -EditMode Windows
        $psReadLineCmd = Get-Command Set-PSReadLineOption -ErrorAction SilentlyContinue
        if ($psReadLineCmd -and $psReadLineCmd.Parameters.ContainsKey('PredictionSource')) {
            try {
                $supportsVt = -not $Global:AiMode -and $global:Host.UI.SupportsVirtualTerminal -and -not [Console]::IsOutputRedirected
                if ($supportsVt) {
                    Set-PSReadLineOption -PredictionSource History
                    Set-PSReadLineOption -PredictionViewStyle ListView
                } else {
                    Set-PSReadLineOption -PredictionSource None
                }
            } catch {
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
        if ($psReadLineCmd -and $psReadLineCmd.Parameters.ContainsKey('PredictionSource')) {
            $psReadlineColors["InlinePrediction"] = '#70A99F'
        }

        try {
            Set-PSReadlineOption -Color $psReadlineColors
        } catch {}

        # --- PSReadLine Key Bindings ---
        if (-not $Global:AiMode -and $global:Host.Name -eq 'ConsoleHost' -and (Get-Command Set-PSReadLineKeyHandler -ErrorAction SilentlyContinue)) {
            Set-PSReadLineKeyHandler -Key UpArrow -Function HistorySearchBackward
            Set-PSReadLineKeyHandler -Key DownArrow -Function HistorySearchForward
            Set-PSReadLineKeyHandler -Chord 'Ctrl+Spacebar' -Function Complete
            Set-PSReadLineKeyHandler -Key F7 -ScriptBlock {
                $command = Get-History | Out-GridView -Title 'Command History' -PassThru
                if ($command) {
                    $pr = [Type]"Microsoft.PowerShell.PSConsoleReadLine"
                    if ($pr) {
                        $pr::RevertLine()
                        $pr::Insert($command.CommandLine)
                    }
                }
            }
        }
        # .NET Hotkeys
        if (-not $Global:AiMode) {
            Set-PSReadLineKeyHandler -Key 'Ctrl+Shift+b' -ScriptBlock {
                $pr = [Type]"Microsoft.PowerShell.PSConsoleReadLine"
                if ($pr) {
                    $pr::RevertLine()
                    $pr::Insert('db')
                    $pr::AcceptLine()
                }
            }
            Set-PSReadLineKeyHandler -Key 'Ctrl+Shift+t' -ScriptBlock {
                $pr = [Type]"Microsoft.PowerShell.PSConsoleReadLine"
                if ($pr) {
                    $pr::RevertLine()
                    $pr::Insert('dt')
                    $pr::AcceptLine()
                }
            }
            Set-PSReadLineKeyHandler -Key 'Ctrl+Spacebar' -ScriptBlock {
                $pr = [Type]"Microsoft.PowerShell.PSConsoleReadLine"
                if ($pr) {
                    $pr::RevertLine()
                    $pr::Insert('cc')
                    $pr::AcceptLine()
                }
            }
            # Initialize TUI colors based on loaded prompt theme
            [TerminalMenu]::InitializeTuiColors()
        }
    }
}

[ProfileEnvironment]::InitializeSession()

# --- Oh My Posh Theme (Initialized in global script scope to bypass class method scoping constraints) ---
if (-not $Global:AiMode) {
    $theme = "neko"
    $configPath = Join-Path -Path $env:POSH_THEMES_PATH -ChildPath "config.json"
    if (Test-Path $configPath) {
        try {
            $json = Get-Content $configPath -Raw | ConvertFrom-Json
            if ($json.active_theme) {
                $theme = $json.active_theme
            }
        } catch {}
    } else {
        # Migrate from legacy active_theme.txt if exists
        $activeThemeFile = Join-Path -Path $env:POSH_THEMES_PATH -ChildPath "active_theme.txt"
        if (Test-Path $activeThemeFile) {
            $theme = (Get-Content $activeThemeFile -Raw | Out-String).Trim()
            # Clean up immediately
            Remove-Item -Path $activeThemeFile -Force -ErrorAction SilentlyContinue
            # Create config.json
            $cfg = @{ active_theme = $theme; enable_mobile = ($theme -like "*-mobile") }
            $cfg | ConvertTo-Json | Out-File -FilePath $configPath -Force -Encoding utf8
        }
    }
    $env:THEME = $theme
    $themePath = Join-Path -Path $env:POSH_THEMES_PATH -ChildPath "$($env:THEME).omp.json"
    if (Test-Path $themePath) {
        if (-not $global:PoshInitialized) {
            try {
                oh-my-posh --init --shell pwsh --config $themePath | Invoke-Expression
                $global:PoshInitialized = $true
            } catch {
                Write-Warning "Failed to initialize oh-my-posh: $_"
            }
        }
    } else {
        Write-Warning "Oh My Posh theme '$($env:THEME)' not found at '$themePath'."
    }
}
#endregion



