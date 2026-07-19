#region CENTRALIZED SHELL ALIASES & WRAPPER FUNCTIONS
# ==============================================================================
#  Centralized routing layer bridging CLI commands to the static class helpers.
# ==============================================================================

# --- Core Aliases ---
Set-Alias -Name ip  -Value Get-NetIPConfiguration -Force
Set-Item -Path Alias:\cls -Value Clear-Host -Force -Option AllScope

# --- Navigation & System Wrappers ---
function Set-LocationParent { [ProfileNavigator]::GoParent() }
function Set-LocationGrandParent { [ProfileNavigator]::GoGrandParent() }
function Invoke-OpenExplorer { [SystemHelper]::OpenExplorer() }
function Enter-Project {
    param([string]$Name)
    [ProfileNavigator]::EnterProject($Name)
}
function Reload-Profile {
    $global:AgyProfileLoaded = $false
    $global:PoshInitialized = $false
    . $PROFILE
    Write-Host "✅ Profile reloaded." -ForegroundColor Green
}
function New-DirAndEnter {
    param([string]$Path)
    $null = New-Item -ItemType Directory -Path $Path -Force
    Set-Location $Path
}
function Get-DiskSpace { [SystemHelper]::GetDiskSpace() | Format-Table -AutoSize }
function Get-PublicIP { [SystemHelper]::GetPublicIP() }
function Get-FileTree {
    param([int]$Depth = 2)
    tree.com /f /a | Select-Object -First (50 * $Depth)
}
function Stop-ProcessFriendly {
    param([string]$Name)
    [SystemHelper]::StopProcessFriendly($Name)
}
function Get-CustomCommands {
    [CmdletBinding(DefaultParameterSetName = "Menu")]
    param(
        [Parameter(ParameterSetName = "Menu")]
        [switch]$Menu,

        [Parameter(ParameterSetName = "Account")]
        [switch]$Account,

        [Parameter(ParameterSetName = "Account")]
        [switch]$Agy,

        [Parameter(ParameterSetName = "Ai")]
        [switch]$Ai,

        [Parameter(ParameterSetName = "Project")]
        [string]$Project,

        [Parameter(ParameterSetName = "Theme")]
        [switch]$Theme,

        [Parameter(ParameterSetName = "Ssh")]
        [switch]$Ssh,

        [Parameter(ParameterSetName = "Manual")]
        [switch]$Manual,

        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$ArgsList
    )

    if ($Account -or $Agy) {
        [AgyAccountManager]::ManageAccountsInteractive()
        return
    }

    if ($Ai) {
        Invoke-MultiAgent
        return
    }

    if ($null -ne $Project -and $Project -ne "") {
        Enter-Project $Project
        return
    }

    if ($Theme) {
        Select-ShellTheme
        return
    }

    if ($Ssh) {
        Get-SshConnectionInfo
        return
    }

    if ($Manual) {
        [ProfileHelp]::Show("")
        return
    }

    if ($ArgsList) {
        $argStr = $ArgsList -join " "
        $matchedProj = [ProfileNavigator]::FindWorkspaces($argStr)
        if ($matchedProj.Count -gt 0) {
            Enter-Project $argStr
        } else {
            [ProfileHelp]::Show($argStr)
        }
        return
    }

    # 1. Run the heavy setup and keyring verification checks now when CC opens
    [AgyAccountManager]::RunHeavySetupCheck()
    # Load dynamic command database from JSON file
    $commandsMap = [ProfileHelp]::GetCommands()

    # 2. Dynamically populate [Account] Manage Accounts
    $accSubDict = [System.Collections.Generic.Dictionary[string, CommandDoc[]]]::new()
    $accList = [System.Collections.Generic.List[CommandDoc]]::new()
    foreach ($acc in [AgyAccountManager]::GetAccounts()) {
        $null = $accList.Add([CommandDoc]@{
            Alias = $acc
            FullName = "acc $acc"
            Desc = "Switch active context to $acc"
            Command = "Invoke-AccountSession $acc"
        })
    }
    $accSubDict["Select Active Account"] = $accList.ToArray()
    $commandsMap["[Account] Manage Accounts"] = $accSubDict

    # 3. Dynamically populate [AI Agent] Select AI Agent
    $aiSubDict = [System.Collections.Generic.Dictionary[string, CommandDoc[]]]::new()
    $aiSubDict["Launch AI Agent"] = @(
        [CommandDoc]@{ Alias = "claude"; FullName = "Invoke-Claude-By-Ollama"; Desc = "Launch Anthropic Claude Code via Ollama"; Command = "Invoke-Claude-By-Ollama" }
        [CommandDoc]@{ Alias = "codex"; FullName = "Invoke-Codex-By-Ollama"; Desc = "Launch Codex CLI via Ollama"; Command = "Invoke-Codex-By-Ollama" }
        [CommandDoc]@{ Alias = "openclaw"; FullName = "Invoke-ChatGPT"; Desc = "Launch ChatGPT/OpenClaw client"; Command = "Invoke-ChatGPT" }
        [CommandDoc]@{ Alias = "hermes"; FullName = "Invoke-Hermes-By-Ollama"; Desc = "Launch Hermes local reasoning LLM console"; Command = "Invoke-Hermes-By-Ollama" }
    )
    $commandsMap["[AI Agent] Select AI Agent"] = $aiSubDict

    # 4. Dynamically populate [Project] Switch Project
    $projSubDict = [System.Collections.Generic.Dictionary[string, CommandDoc[]]]::new()
    $projList = [System.Collections.Generic.List[CommandDoc]]::new()
    if ($null -ne $Global:ProfileWorkspaces) {
        foreach ($p in $Global:ProfileWorkspaces) {
            $null = $projList.Add([CommandDoc]@{
                Alias = $p.Name
                FullName = "proj $($p.Name)"
                Desc = "Navigate to project: $($p.Path)"
                Command = "Enter-Project $($p.Name)"
            })
        }
    }
    $projSubDict["Jump to Workspace"] = $projList.ToArray()
    $commandsMap["[Project] Switch Project"] = $projSubDict

    # 5. Dynamically populate [Theme] Select Theme
    $themeSubDict = [System.Collections.Generic.Dictionary[string, CommandDoc[]]]::new()
    $themeSubDict["Apply Shell Theme"] = @(
        [CommandDoc]@{ Alias = "theme-desktop"; FullName = "Set-DesktopThemeMode"; Desc = "Apply Desktop theme (Unicode/Emoji)"; Command = "Set-DesktopThemeMode" }
        [CommandDoc]@{ Alias = "theme-mobile"; FullName = "Set-MobileThemeMode"; Desc = "Apply Mobile theme (ASCII/Stacked)"; Command = "Set-MobileThemeMode" }
        [CommandDoc]@{ Alias = "theme-tui"; FullName = "Select-ShellTheme"; Desc = "Launch interactive theme selector TUI"; Command = "Select-ShellTheme" }
    )
    $commandsMap["[Theme] Select Theme"] = $themeSubDict

    # 6. Dynamically populate [SSH Info] System SSH Info
    $sshSubDict = [System.Collections.Generic.Dictionary[string, CommandDoc[]]]::new()
    $sshSubDict["SSH & Port Management"] = @(
        [CommandDoc]@{ Alias = "ssh-info"; FullName = "Get-SshConnectionInfo"; Desc = "View Tailscale & active port 22 sessions"; Command = "Get-SshConnectionInfo" }
        [CommandDoc]@{ Alias = "ssh-addkey"; FullName = "Add-SshAuthorizedKey"; Desc = "Authorize a public SSH key"; Command = "Add-SshAuthorizedKey" }
        [CommandDoc]@{ Alias = "ssh-addkey-mobile"; FullName = "Start-MobileSshKeyReceiver"; Desc = "Start background key receiver"; Command = "Start-MobileSshKeyReceiver" }
    )
    $commandsMap["[SSH Info] System SSH Info"] = $sshSubDict

    # 7. Dynamically populate [Exit] Exit Control Center
    $exitSubDict = [System.Collections.Generic.Dictionary[string, CommandDoc[]]]::new()
    $exitSubDict["Quit Control Center"] = @(
        [CommandDoc]@{ Alias = "exit"; FullName = "Exit-ControlCenter"; Desc = "Close Control Center dashboard"; Command = "EXIT_TUI" }
    )
    $commandsMap["[Exit] Exit Control Center"] = $exitSubDict

    # Define 3-pane hierarchy mapping dynamically from loaded commandsMap (including admin entries!)
    $hierarchy = [ordered]@{}
    foreach ($k in $commandsMap.Keys) {
        $hierarchy[$k] = [string[]]($commandsMap[$k].Keys)
    }

    $leftItems = @(
        "[Account] Manage Accounts",
        "[AI Agent] Select AI Agent",
        "[Project] Switch Project",
        "[Theme] Select Theme",
        "[SSH Info] System SSH Info",
        "---------------------------",
        "1. Workspace & Navigation",
        "2. Development Tools",
        "3. System & Network Operations",
        "4. AI & Profile Contexts",
        "---------------------------",
        "[Exit] Exit Control Center"
    )

    $currentLeftIndex = 0
    $currentMiddleIndex = 0
    $currentRightIndex = 0
    $focusPane = 0 # 0 = Left (Parent), 1 = Middle (Subcategory), 2 = Right (Commands)

    # Initialize layout drawing
    Clear-Host
    $lastLinesCount = 0
    $lastWidth = 0
    $lastHeight = 0

    while ($true) {
        if ($null -eq $Global:TuiColors) { [TerminalMenu]::InitializeTuiColors() }
        
        $activeAcc = [AgyAccountManager]::GetActiveAccount()
        $timeStr = Get-Date -Format 'yyyy-MM-dd HH:mm'
        
        # Get selected parent category name
        $selectedParentName = $leftItems[$currentLeftIndex]
        $isCategory = $hierarchy.Contains($selectedParentName)

        # Resolve subcategories and commands list
        $middleItems = @()
        $rightItems = @()
        if ($isCategory) {
            $middleItems = $hierarchy[$selectedParentName]
            # Ensure middle index is within bounds
            if ($currentMiddleIndex -ge $middleItems.Count) { $currentMiddleIndex = 0 }
            $selectedSubName = $middleItems[$currentMiddleIndex]
            if ($commandsMap[$selectedParentName].ContainsKey($selectedSubName)) {
                $rightItems = $commandsMap[$selectedParentName][$selectedSubName] | Sort-Object Alias
            }
        }

        # Ensure right index is within bounds
        if ($currentRightIndex -ge $rightItems.Count) { $currentRightIndex = 0 }

        # Detect window size change to force clear screen (responsiveness!)
        $currentWidth = [Console]::WindowWidth
        $currentHeight = [Console]::WindowHeight
        if ($currentWidth -ne $lastWidth -or $currentHeight -ne $lastHeight) {
            Clear-Host
            $lastWidth = $currentWidth
            $lastHeight = $currentHeight
            $lastLinesCount = 0
        }

        # Reset cursor position to the top of the visible console viewport to prevent screen scrolling
        [Console]::SetCursorPosition(0, [Console]::WindowTop)

        # Dynamic Responsive Header & Layout Width Calculation
        $tuiWidth = 110
        if ($currentWidth -lt 110) { $tuiWidth = $currentWidth - 4 }
        if ($tuiWidth -lt 65) { $tuiWidth = 65 } # safety floor
        $borderWidth = $tuiWidth
        $border = "=" * $borderWidth

        # Draw Header with "CC" block ASCII logo
        Write-Host $border -ForegroundColor $Global:TuiColors.Header
        Write-Host "  ▄████▄   ▄████▄     🛸 Powershell Profile Control Center v3.0 🛸" -ForegroundColor $Global:TuiColors.Header
        Write-Host " █▀     ▀ █▀     ▀    System dashboard and control suite." -ForegroundColor $Global:TuiColors.Header
        Write-Host " █        █           " -ForegroundColor $Global:TuiColors.Header
        Write-Host " █▄     ▄ █▄     ▄    Active Account: $activeAcc" -ForegroundColor $Global:TuiColors.Footer
        Write-Host "  ▀████▀   ▀████▀     Time: $timeStr" -ForegroundColor $Global:TuiColors.Footer
        Write-Host $border -ForegroundColor $Global:TuiColors.Header
        
        $helpStr = " [Tab/Right] Navigate Panes | [Left/Esc] Go Back | [Enter] Select & Run"
        $helpPad = [Math]::Max(0, [Math]::Floor(($borderWidth - $helpStr.Length) / 2))
        Write-Host ((" " * $helpPad) + $helpStr) -ForegroundColor $Global:TuiColors.Footer
        
        Write-Host $border -ForegroundColor $Global:TuiColors.Header

        # Calculate scrollable viewport bounds to strictly prevent console scrolling
        $viewportHeight = $currentHeight - 11
        if ($viewportHeight -lt 6) { $viewportHeight = 6 }

        # Scroll viewport offsets so the selected item remains centered/visible
        $startLeft = 0
        if ($currentLeftIndex -ge $viewportHeight) {
            $startLeft = $currentLeftIndex - $viewportHeight + 1
        }

        $startMiddle = 0
        if ($currentMiddleIndex -ge $viewportHeight) {
            $startMiddle = $currentMiddleIndex - $viewportHeight + 1
        }

        $startRight = 0
        if ($currentRightIndex -ge $viewportHeight) {
            $startRight = $currentRightIndex - $viewportHeight + 1
        }

        # Determine rows count to render
        $maxRows = [Math]::Min($viewportHeight, [Math]::Max($leftItems.Count, [Math]::Max($middleItems.Count, $rightItems.Count)))
        if (-not $isCategory) {
            $maxRows = [Math]::Min($viewportHeight, [Math]::Max($leftItems.Count, 3))
        }

        # Dynamic Responsive Column Width Scaling
        $leftColWidth = 30
        $midColWidth = 22
        if ($tuiWidth -lt 95) {
            $leftColWidth = 24
            $midColWidth = 18
        }
        if ($tuiWidth -lt 75) {
            $leftColWidth = 20
            $midColWidth = 15
        }

        for ($i = 0; $i -lt $maxRows; $i++) {
            # --- PANE 0: LEFT COLUMN (MAIN CATEGORIES & ADMIN ACTIONS) ---
            $leftPrefix = "   "
            $leftColor = $Global:TuiColors.Regular
            $leftTextColor = $Global:TuiColors.Regular
            $leftText = ""

            $leftIdxToPrint = $i + $startLeft
            if ($leftIdxToPrint -lt $leftItems.Count) {
                $item = $leftItems[$leftIdxToPrint]
                if ($leftIdxToPrint -eq $currentLeftIndex) {
                    if ($focusPane -eq 0) {
                        $leftPrefix = " > "
                        $leftColor = $Global:TuiColors.Selected
                        $leftTextColor = $Global:TuiColors.Selected
                    } else {
                        $leftPrefix = "   "
                        $leftColor = $Global:TuiColors.Search
                        $leftTextColor = $Global:TuiColors.Search
                    }
                }
                
                if ($item -match '^-+$') {
                    $leftTextColor = "DarkGray"
                    $leftColor = "DarkGray"
                } elseif ($item.StartsWith("[")) {
                    if ($leftIdxToPrint -ne $currentLeftIndex) {
                        $leftTextColor = "Cyan"
                    }
                }
                # Strictly enforce exact width and truncation to keep separator line straight
                $rawLeftText = $item.PadRight($leftColWidth)
                $leftText = if ($rawLeftText.Length -gt $leftColWidth) { $rawLeftText.Substring(0, $leftColWidth) } else { $rawLeftText }
            } else {
                $leftText = " " * $leftColWidth
            }

            # Left total is prefix (3) + text ($leftColWidth) + separator (3) = $leftColWidth + 6
            Write-Host $leftPrefix -ForegroundColor $leftColor -NoNewline
            Write-Host $leftText -ForegroundColor $leftTextColor -NoNewline
            Write-Host " | " -ForegroundColor "DarkGray" -NoNewline

            # --- PANE 1: MIDDLE COLUMN & PANE 2: RIGHT COLUMN ---
            if ($isCategory) {
                # --- PANE 1: MIDDLE COLUMN (SUBCATEGORIES) ---
                $midPrefix = "   "
                $midColor = $Global:TuiColors.Regular
                $midTextColor = $Global:TuiColors.Regular
                $midText = ""

                $midIdxToPrint = $i + $startMiddle
                if ($midIdxToPrint -lt $middleItems.Count) {
                    $subCat = $middleItems[$midIdxToPrint]
                    if ($midIdxToPrint -eq $currentMiddleIndex) {
                        if ($focusPane -eq 1) {
                            $midPrefix = " > "
                            $midColor = $Global:TuiColors.Selected
                            $midTextColor = $Global:TuiColors.Selected
                        } else {
                            $midPrefix = "   "
                            $midColor = $Global:TuiColors.Search
                            $midTextColor = $Global:TuiColors.Search
                        }
                    }
                    # Strictly enforce exact width and truncation to keep separator line straight
                    $rawMidText = $subCat.PadRight($midColWidth)
                    $midText = if ($rawMidText.Length -gt $midColWidth) { $rawMidText.Substring(0, $midColWidth) } else { $rawMidText }
                } else {
                    $midText = " " * $midColWidth
                }
                
                # Middle total is prefix (3) + text ($midColWidth) + separator (3) = $midColWidth + 6
                Write-Host $midPrefix -ForegroundColor $midColor -NoNewline
                Write-Host $midText -ForegroundColor $midTextColor -NoNewline
                Write-Host " | " -ForegroundColor "DarkGray" -NoNewline

                # --- PANE 2: RIGHT COLUMN (COMMANDS LIST) ---
                $rightPrefix = "   "
                $rightColor = $Global:TuiColors.Regular
                $rightTextColor = $Global:TuiColors.Regular
                $rightText = ""

                $rightIdxToPrint = $i + $startRight
                if ($rightIdxToPrint -lt $rightItems.Count) {
                    $doc = $rightItems[$rightIdxToPrint]
                    if ($focusPane -eq 2 -and $rightIdxToPrint -eq $currentRightIndex) {
                        $rightPrefix = " > "
                        $rightColor = $Global:TuiColors.Selected
                        $rightTextColor = $Global:TuiColors.Selected
                    }
                    $aliasStr = if ($doc.Alias -eq "-") { $doc.FullName } else { $doc.Alias }
                    $rightText = "$($aliasStr.PadRight(15)) - $($doc.Desc)"
                }
                
                # Mathematically calculate safe remaining width to prevent line-wrapping on narrow screens
                # Columns before Right Text: LeftPrefix(3) + LeftText($leftColWidth) + LeftSeparator(3) + MidPrefix(3) + MidText($midColWidth) + MidSeparator(3) + RightPrefix(3) = $leftColWidth + $midColWidth + 15
                $maxRightWidth = $tuiWidth - ($leftColWidth + $midColWidth + 17)
                if ($maxRightWidth -lt 10) { $maxRightWidth = 10 }
                $paddedRightText = $rightText.PadRight($maxRightWidth)
                if ($paddedRightText.Length -gt $maxRightWidth) {
                    $paddedRightText = $paddedRightText.Substring(0, $maxRightWidth)
                }

                Write-Host $rightPrefix -ForegroundColor $rightColor -NoNewline
                Write-Host $paddedRightText -ForegroundColor $rightTextColor
            } else {
                # Placeholder for non-category items
                $remainingWidth = $tuiWidth - ($leftColWidth + 8)
                if ($remainingWidth -lt 10) { $remainingWidth = 10 }
                Write-Host (" " * $remainingWidth)
            }
        }

        # Erasure logic for trailing lines from previous iterations
        $headerLines = 9
        $linesWritten = $headerLines + $maxRows
        if ($linesWritten -lt $lastLinesCount) {
            $diff = $lastLinesCount - $linesWritten
            for ($d = 0; $d -lt $diff; $d++) {
                Write-Host (" " * ($tuiWidth - 2))
            }
        }
        $lastLinesCount = $linesWritten

        # Clear any extra keys in buffer
        while ([Console]::KeyAvailable) { [void][Console]::ReadKey($true) }

        # Read keystroke
        $key = [Console]::ReadKey($true)
        if ($key.KeyChar -eq '/') {
            Invoke-CcSearch
            # Reset redraw coordinates
            Clear-Host
            $lastLinesCount = 0
            continue
        }
        if ($key.Key -eq "UpArrow") {
            if ($focusPane -eq 0) {
                do {
                    $currentLeftIndex = ($currentLeftIndex - 1 + $leftItems.Count) % $leftItems.Count
                } while ($leftItems[$currentLeftIndex] -match '^-+$')
                $currentMiddleIndex = 0
                $currentRightIndex = 0
            } elseif ($focusPane -eq 1) {
                if ($middleItems.Count -gt 0) {
                    $currentMiddleIndex = ($currentMiddleIndex - 1 + $middleItems.Count) % $middleItems.Count
                    $currentRightIndex = 0
                }
            } elseif ($focusPane -eq 2) {
                if ($rightItems.Count -gt 0) {
                    $currentRightIndex = ($currentRightIndex - 1 + $rightItems.Count) % $rightItems.Count
                }
            }
        }
        elseif ($key.Key -eq "DownArrow") {
            if ($focusPane -eq 0) {
                do {
                    $currentLeftIndex = ($currentLeftIndex + 1) % $leftItems.Count
                } while ($leftItems[$currentLeftIndex] -match '^-+$')
                $currentMiddleIndex = 0
                $currentRightIndex = 0
            } elseif ($focusPane -eq 1) {
                if ($middleItems.Count -gt 0) {
                    $currentMiddleIndex = ($currentMiddleIndex + 1) % $middleItems.Count
                    $currentRightIndex = 0
                }
            } elseif ($focusPane -eq 2) {
                if ($rightItems.Count -gt 0) {
                    $currentRightIndex = ($currentRightIndex + 1) % $rightItems.Count
                }
            }
        }
        elseif ($key.Key -eq "RightArrow" -or $key.Key -eq "Tab") {
            if ($focusPane -eq 0 -and $isCategory -and $middleItems.Count -gt 0) {
                $focusPane = 1
                $currentMiddleIndex = 0
                $currentRightIndex = 0
            } elseif ($focusPane -eq 1 -and $rightItems.Count -gt 0) {
                $focusPane = 2
                $currentRightIndex = 0
            }
        }
        elseif ($key.Key -eq "LeftArrow") {
            if ($focusPane -eq 2) {
                $focusPane = 1
            } elseif ($focusPane -eq 1) {
                $focusPane = 0
            }
        }
        elseif ($key.Key -eq "Escape") {
            if ($focusPane -eq 2) {
                $focusPane = 1
            } elseif ($focusPane -eq 1) {
                $focusPane = 0
            } else {
                break
            }
        }
        elseif ($key.Key -eq "Enter") {
            if ($focusPane -eq 0) {
                if ($isCategory -and $middleItems.Count -gt 0) {
                    $focusPane = 1
                    $currentMiddleIndex = 0
                }
            } elseif ($focusPane -eq 1) {
                if ($rightItems.Count -gt 0) {
                    $focusPane = 2
                    $currentRightIndex = 0
                }
            } elseif ($focusPane -eq 2) {
                # Execute selected command!
                $selectedCmdObj = $rightItems[$currentRightIndex]
                $cmdToRun = $selectedCmdObj.Command
                $aliasName = $selectedCmdObj.Alias

                if ($cmdToRun -eq "EXIT_TUI") {
                    break
                }

                Clear-Host
                Write-Host ">>> Running: $aliasName ($cmdToRun)" -ForegroundColor Green
                Write-Host "----------------------------------------------------------" -ForegroundColor Gray
                
                try {
                    Invoke-Expression $cmdToRun
                } catch {
                    Write-Error $_
                }

                Write-Host "`n----------------------------------------------------------" -ForegroundColor Gray
                Write-Host "Press any key to return to Control Center..." -ForegroundColor Yellow
                while ([Console]::KeyAvailable) { [void][Console]::ReadKey($true) }
                [void][Console]::ReadKey($true)

                # Reset redraw coordinates
                Clear-Host
                $lastLinesCount = 0
            }
        }
    }
    Clear-Host
}
function Invoke-CcSearch {
    # 1. Gather all commands from commandsMap
    $commandsMap = [ProfileHelp]::GetCommands()
    
    # 2. Add extra admin entries dynamically
    # [Account]
    $accSubDict = [System.Collections.Generic.Dictionary[string, CommandDoc[]]]::new()
    $accList = [System.Collections.Generic.List[CommandDoc]]::new()
    foreach ($acc in [AgyAccountManager]::GetAccounts()) {
        $null = $accList.Add([CommandDoc]@{
            Alias = $acc
            FullName = "acc $acc"
            Desc = "Switch active context to $acc"
            Command = "Invoke-AccountSession $acc"
        })
    }
    $accSubDict["Select Active Account"] = $accList.ToArray()
    $commandsMap["[Account] Manage Accounts"] = $accSubDict

    # [AI Agent]
    $aiSubDict = [System.Collections.Generic.Dictionary[string, CommandDoc[]]]::new()
    $aiSubDict["Launch AI Agent"] = @(
        [CommandDoc]@{ Alias = "claude"; FullName = "Invoke-Claude-By-Ollama"; Desc = "Launch Anthropic Claude Code via Ollama"; Command = "Invoke-Claude-By-Ollama" }
        [CommandDoc]@{ Alias = "codex"; FullName = "Invoke-Codex-By-Ollama"; Desc = "Launch Codex CLI via Ollama"; Command = "Invoke-Codex-By-Ollama" }
        [CommandDoc]@{ Alias = "openclaw"; FullName = "Invoke-ChatGPT"; Desc = "Launch ChatGPT/OpenClaw client"; Command = "Invoke-ChatGPT" }
        [CommandDoc]@{ Alias = "hermes"; FullName = "Invoke-Hermes-By-Ollama"; Desc = "Launch Hermes local reasoning LLM console"; Command = "Invoke-Hermes-By-Ollama" }
    )
    $commandsMap["[AI Agent] Select AI Agent"] = $aiSubDict

    # [Project]
    $projSubDict = [System.Collections.Generic.Dictionary[string, CommandDoc[]]]::new()
    $projList = [System.Collections.Generic.List[CommandDoc]]::new()
    if ($null -ne $Global:ProfileWorkspaces) {
        foreach ($p in $Global:ProfileWorkspaces) {
            $null = $projList.Add([CommandDoc]@{
                Alias = $p.Name
                FullName = "proj $($p.Name)"
                Desc = "Navigate to project: $($p.Path)"
                Command = "Enter-Project $($p.Name)"
            })
        }
    }
    $projSubDict["Jump to Workspace"] = $projList.ToArray()
    $commandsMap["[Project] Switch Project"] = $projSubDict

    # [Theme]
    $themeSubDict = [System.Collections.Generic.Dictionary[string, CommandDoc[]]]::new()
    $themeSubDict["Apply Shell Theme"] = @(
        [CommandDoc]@{ Alias = "theme-desktop"; FullName = "Set-DesktopThemeMode"; Desc = "Apply Desktop theme (Unicode/Emoji)"; Command = "Set-DesktopThemeMode" }
        [CommandDoc]@{ Alias = "theme-mobile"; FullName = "Set-MobileThemeMode"; Desc = "Apply Mobile theme (ASCII/Stacked)"; Command = "Set-MobileThemeMode" }
        [CommandDoc]@{ Alias = "theme-tui"; FullName = "Select-ShellTheme"; Desc = "Launch interactive theme selector TUI"; Command = "Select-ShellTheme" }
    )
    $commandsMap["[Theme] Select Theme"] = $themeSubDict

    # [SSH Info]
    $sshSubDict = [System.Collections.Generic.Dictionary[string, CommandDoc[]]]::new()
    $sshSubDict["SSH & Port Management"] = @(
        [CommandDoc]@{ Alias = "ssh-info"; FullName = "Get-SshConnectionInfo"; Desc = "View Tailscale & active port 22 sessions"; Command = "Get-SshConnectionInfo" }
        [CommandDoc]@{ Alias = "ssh-addkey"; FullName = "Add-SshAuthorizedKey"; Desc = "Authorize a public SSH key"; Command = "Add-SshAuthorizedKey" }
        [CommandDoc]@{ Alias = "ssh-addkey-mobile"; FullName = "Start-MobileSshKeyReceiver"; Desc = "Start background key receiver"; Command = "Start-MobileSshKeyReceiver" }
    )
    $commandsMap["[SSH Info] System SSH Info"] = $sshSubDict

    # [Exit]
    $exitSubDict = [System.Collections.Generic.Dictionary[string, CommandDoc[]]]::new()
    $exitSubDict["Quit Control Center"] = @(
        [CommandDoc]@{ Alias = "exit"; FullName = "Exit-ControlCenter"; Desc = "Close Control Center dashboard"; Command = "EXIT_TUI" }
    )
    $commandsMap["[Exit] Exit Control Center"] = $exitSubDict

    $searchLabels = [System.Collections.Generic.List[string]]::new()
    $searchCmds = [System.Collections.Generic.List[string]]::new()

    # Add slash suggestions first
    $slashSuggestions = @(
        "git status             - Show git working tree status",
        "git add .              - Stage all changes in workspace",
        "gcmt                   - conventional commits wizard",
        "git push               - Push commits to remote branch",
        "dotnet run             - Build and run active .NET project",
        "dotnet build           - Compile active .NET project",
        "dotnet test            - Run active .NET project unit tests",
        "docker-compose up      - Start Docker Compose services",
        "docker-compose down    - Stop Docker Compose services",
        "sysmon                 - Display resource diagnostics",
        "dkcl                   - Display Docker container manager",
        "killport               - Terminate port collision process"
    )

    $sep = [char]0x2502
    foreach ($s in $slashSuggestions) {
        if ($s -match '^([a-zA-Z0-9_\-]+(\s+[a-zA-Z0-9_\-\.]+)*)\s+-\s+(.*)$') {
            $null = $searchLabels.Add("$($Matches[1].PadRight(25)) $sep $($Matches[3])")
            $null = $searchCmds.Add($Matches[1].Trim())
        }
    }

    # Add all commands from commandsMap
    foreach ($parent in $commandsMap.Keys) {
        foreach ($sub in $commandsMap[$parent].Keys) {
            foreach ($doc in $commandsMap[$parent][$sub]) {
                $aliasStr = if ($doc.Alias -eq "-") { $doc.FullName } else { $doc.Alias }
                $label = "$($aliasStr.PadRight(25)) $sep $($doc.Desc)"
                if ($searchLabels -notcontains $label) {
                    $null = $searchLabels.Add($label)
                    $null = $searchCmds.Add($doc.Command)
                }
            }
        }
    }

    # Show the search TUI
    $headers = @(
        "==========================================================================",
        "                       🛸 Command Center Search 🛸",
        "=========================================================================="
    )

    $selectedIdx = [TerminalMenu]::Show($headers, $searchLabels.ToArray(), $searchCmds.ToArray(), 0, $true, $true)
    if ($selectedIdx -ge 0) {
        $cmdToRun = $searchCmds[$selectedIdx]
        if ($cmdToRun -eq "EXIT_TUI") { return }
        Clear-Host
        Write-Host ">>> Running: $cmdToRun" -ForegroundColor Green
        Write-Host "----------------------------------------------------------" -ForegroundColor Gray
        try {
            Invoke-Expression $cmdToRun
        } catch {
            Write-Error $_
        }
        Write-Host "`n----------------------------------------------------------" -ForegroundColor Gray
        Write-Host "Press any key to return to Control Center..." -ForegroundColor Yellow
        while ([Console]::KeyAvailable) { [void][Console]::ReadKey($true) }
        [void][Console]::ReadKey($true)
    }
}
function Get-SshConnectionInfo { [SshHelper]::GetConnectionInfo() }
function Add-SshAuthorizedKey {
    param([string]$Key, [string]$Account)
    [SshHelper]::AddAuthorizedKey($Key, $Account)
}
function Toggle-MobileMode {
    [ThemeHelper]::ToggleMobileMode()
    if ($Global:NewThemeToApply) {
        $themePath = $Global:NewThemeToApply
        $Global:NewThemeToApply = $null
        $Global:AgyOriginalPromptCmd = $null
        Remove-Module -Name "oh-my-posh-core" -Force -ErrorAction SilentlyContinue
        Remove-Item -Path "Function:\prompt" -Force -ErrorAction SilentlyContinue
        Remove-Item -Path "Function:\prompt_original" -Force -ErrorAction SilentlyContinue
        oh-my-posh --init --shell pwsh --config $themePath | Invoke-Expression
        [AgyAccountManager]::RegisterPromptHook()
        [TerminalMenu]::InitializeTuiColors()
    }
}
function Set-DesktopThemeMode {
    [ThemeHelper]::SetMobileMode($false)
    if ($Global:NewThemeToApply) {
        $themePath = $Global:NewThemeToApply
        $Global:NewThemeToApply = $null
        $Global:AgyOriginalPromptCmd = $null
        Remove-Module -Name "oh-my-posh-core" -Force -ErrorAction SilentlyContinue
        Remove-Item -Path "Function:\prompt" -Force -ErrorAction SilentlyContinue
        Remove-Item -Path "Function:\prompt_original" -Force -ErrorAction SilentlyContinue
        oh-my-posh --init --shell pwsh --config $themePath | Invoke-Expression
        [AgyAccountManager]::RegisterPromptHook()
        [TerminalMenu]::InitializeTuiColors()
    }
}
function Set-MobileThemeMode {
    [ThemeHelper]::SetMobileMode($true)
    if ($Global:NewThemeToApply) {
        $themePath = $Global:NewThemeToApply
        $Global:NewThemeToApply = $null
        $Global:AgyOriginalPromptCmd = $null
        Remove-Module -Name "oh-my-posh-core" -Force -ErrorAction SilentlyContinue
        Remove-Item -Path "Function:\prompt" -Force -ErrorAction SilentlyContinue
        Remove-Item -Path "Function:\prompt_original" -Force -ErrorAction SilentlyContinue
        oh-my-posh --init --shell pwsh --config $themePath | Invoke-Expression
        [AgyAccountManager]::RegisterPromptHook()
        [TerminalMenu]::InitializeTuiColors()
    }
}
function Start-MobileSshKeyReceiver { [SshHelper]::StartMobileSshKeyReceiver() }

# Navigation & System Aliases
Set-Alias -Name ..          -Value Set-LocationParent          -Force
Set-Alias -Name ...         -Value Set-LocationGrandParent     -Force
Set-Alias -Name f           -Value Invoke-OpenExplorer         -Force
Set-Alias -Name prj         -Value Enter-Project               -Force
Set-Alias -Name proj        -Value Enter-Project               -Force
Set-Alias -Name go          -Value Reload-Profile              -Force
Set-Alias -Name mkcd        -Value New-DirAndEnter             -Force
Set-Alias -Name usage       -Value Get-DiskSpace               -Force
Set-Alias -Name myip        -Value Get-PublicIP                -Force
Set-Alias -Name tree        -Value Get-FileTree                -Force
Set-Item -Path Alias:\kill -Value Stop-ProcessFriendly -Force -Option AllScope
Set-Alias -Name commands    -Value Get-CustomCommands          -Force
Set-Alias -Name ssh-info    -Value Get-SshConnectionInfo       -Force
Set-Alias -Name ssh-addkey  -Value Add-SshAuthorizedKey        -Force
Set-Alias -Name mobile             -Value Toggle-MobileMode            -Force
Set-Alias -Name ssh-addkey-mobile  -Value Start-MobileSshKeyReceiver    -Force

# --- Git Wrappers ---
function Get-GitStatus { [GitHelper]::Status($args) }
function Show-GitDiff { [GitHelper]::Diff() }
function Get-GitLogGraph { [GitHelper]::LogGraph() }
function Get-GitLogPretty { [GitHelper]::LogPretty() }
function Get-GitLog { [GitHelper]::Log() }
function Get-GitBranches { [GitHelper]::GetBranches($args) }
function Invoke-GitCheckout {
    param([string]$branchName)
    [GitHelper]::Checkout($branchName)
}
function New-GitBranch {
    param([string]$branchName)
    [GitHelper]::NewBranch($branchName)
}
function Remove-GitBranch {
    param([string]$branchName)
    [GitHelper]::RemoveBranch($branchName)
}
function Invoke-GitAddAll { [GitHelper]::AddAll() }
function Invoke-GitUnstage { [GitHelper]::Unstage() }
function Invoke-GitCommit {
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Message)
    [GitHelper]::Commit($Message)
}
function Invoke-GitAmend { [GitHelper]::Amend($args) }
function Invoke-GitUndo { [GitHelper]::Undo() }
function Invoke-GitResetSoft { [GitHelper]::ResetSoft() }
function Invoke-GitResetHard { [GitHelper]::ResetHard() }
function Invoke-GitFetch { [GitHelper]::Fetch() }
function Invoke-GitPull { [GitHelper]::Pull($args) }
function Invoke-GitPush { [GitHelper]::Push($args) }
function Invoke-GitPushForce { [GitHelper]::PushForce($args) }
function Invoke-GitMergeSquash {
    param([string]$BranchName)
    [GitHelper]::MergeSquash($BranchName)
}
function Invoke-GitStashSnapshot {
    param([string]$Message)
    [GitHelper]::StashSnapshot($Message)
}

# Git Aliases
Set-Alias -Name gs        -Value Get-GitStatus        -Force
Set-Alias -Name gd        -Value Show-GitDiff         -Force
Set-Alias -Name glo       -Value Get-GitLogGraph      -Force
Set-Alias -Name glg       -Value Get-GitLogGraph      -Force
Set-Alias -Name glog      -Value Get-GitLogPretty     -Force
Set-Alias -Name gb        -Value Get-GitBranches      -Force
Set-Alias -Name co        -Value Invoke-GitCheckout   -Force
Set-Alias -Name cob       -Value New-GitBranch        -Force
Set-Alias -Name gbd       -Value Remove-GitBranch     -Force
Set-Alias -Name ga        -Value Invoke-GitAddAll     -Force
Set-Alias -Name gunstage  -Value Invoke-GitUnstage    -Force
Set-Alias -Name gcmt      -Value Invoke-GitCommit     -Force
Set-Alias -Name gca       -Value Invoke-GitAmend      -Force
Set-Alias -Name gundo     -Value Invoke-GitUndo       -Force
Set-Alias -Name gr        -Value Invoke-GitResetSoft  -Force
Set-Alias -Name grh       -Value Invoke-GitResetHard  -Force
Set-Alias -Name gf        -Value Invoke-GitFetch      -Force
Set-Alias -Name gpu       -Value Invoke-GitPull       -Force
Set-Alias -Name gus       -Value Invoke-GitPush       -Force
Set-Alias -Name guf       -Value Invoke-GitPushForce  -Force
Set-Alias -Name gms       -Value Invoke-GitMergeSquash -Force
Set-Alias -Name gsnap     -Value Invoke-GitStashSnapshot -Force

# --- .NET Development Wrappers ---
function Invoke-DotNetRun { [DotNetHelper]::Run($args) }
function Invoke-DotNetWatch { [DotNetHelper]::Watch($args) }
function Invoke-DotNetBuild { [DotNetHelper]::Build($args) }
function Invoke-DotNetFormat { [DotNetHelper]::Format($args) }
function Invoke-DotNetTest { [DotNetHelper]::Test($args) }
function Invoke-DotNetWatchTest { [DotNetHelper]::WatchTest($args) }
function Invoke-DotNetClean { [DotNetHelper]::Clean($args) }
function Invoke-DotNetRestore { [DotNetHelper]::Restore($args) }
function Remove-BinObj { [DotNetHelper]::CleanBinObj() }
function Update-Database {
    param([string]$Context)
    [DotNetHelper]::UpdateDatabase($Context)
}
function Add-Migration {
    param([string]$MigrationName, [string]$Context)
    [DotNetHelper]::AddMigration($MigrationName, $Context)
}
function Remove-Database {
    param([string]$Context)
    [DotNetHelper]::RemoveDatabase($Context)
}
function Remove-Migration {
    param([string]$Context)
    [DotNetHelper]::RemoveMigration($Context)
}
function New-Solution {
    param([string]$Name)
    [DotNetHelper]::NewSolution($Name)
}
function Add-AllProjectsToSolution { [DotNetHelper]::AddAllProjectsToSolution() }
function New-ConsoleProject {
    param([string]$Name)
    [DotNetHelper]::NewConsole($Name)
}
function New-WebApiProject {
    param([string]$Name)
    [DotNetHelper]::NewWebApi($Name)
}

# .NET Aliases
Set-Alias -Name dr     -Value Invoke-DotNetRun       -Force
Set-Alias -Name dw     -Value Invoke-DotNetWatch     -Force
Set-Alias -Name db     -Value Invoke-DotNetBuild     -Force
Set-Alias -Name df     -Value Invoke-DotNetFormat    -Force
Set-Alias -Name dt     -Value Invoke-DotNetTest      -Force
Set-Alias -Name wt     -Value Invoke-DotNetWatchTest -Force
Set-Alias -Name dcl    -Value Invoke-DotNetClean     -Force
Set-Alias -Name dres   -Value Invoke-DotNetRestore   -Force
Set-Alias -Name dclean -Value Remove-BinObj          -Force
Set-Alias -Name du      -Value Update-Database   -Force
Set-Alias -Name da      -Value Add-Migration     -Force
Set-Alias -Name dd      -Value Remove-Database   -Force
Set-Alias -Name dremove -Value Remove-Migration  -Force
Set-Alias -Name sln     -Value New-Solution              -Force
Set-Alias -Name sln-add -Value Add-AllProjectsToSolution -Force
Set-Alias -Name console -Value New-ConsoleProject        -Force
Set-Alias -Name webapi  -Value New-WebApiProject         -Force

# --- Docker Wrappers ---
function Get-DockerContainers {
    param([switch]$All)
    [DockerHelper]::GetContainers($All)
}
function Remove-AllDockerContainers { [DockerHelper]::RemoveAllContainers() }
function Stop-AllDockerContainers { [DockerHelper]::StopAllContainers() }
function Invoke-ComposeUp { [DockerHelper]::ComposeUp($args) }
function Invoke-ComposeUpBuild { [DockerHelper]::ComposeUpBuild($args) }
function Invoke-ComposeDown { [DockerHelper]::ComposeDown($args) }
function Remove-UnusedDockerVolumes { [DockerHelper]::RemoveUnusedVolumes() }
function Remove-UnusedDockerImages { [DockerHelper]::RemoveUnusedImages() }

# Docker Aliases
Set-Alias -Name dkcl       -Value Get-DockerContainers       -Force
Set-Alias -Name dkrmac     -Value Remove-AllDockerContainers -Force
Set-Alias -Name dkstac     -Value Stop-AllDockerContainers   -Force
Set-Alias -Name dkcpu      -Value Invoke-ComposeUp           -Force
Set-Alias -Name dkcpub     -Value Invoke-ComposeUpBuild      -Force
Set-Alias -Name dkcpd      -Value Invoke-ComposeDown         -Force
Set-Alias -Name fix-volume -Value Remove-UnusedDockerVolumes -Force
Set-Alias -Name fix-image  -Value Remove-UnusedDockerImages  -Force

# --- AWS LocalStack Wrappers ---
function Get-S3Buckets { [AwsHelper]::GetS3Buckets() }
function New-S3Bucket {
    param([string]$Name)
    [AwsHelper]::NewS3Bucket($Name)
}
function Get-LambdaFunctions { [AwsHelper]::GetLambdaFunctions() }
function Get-LocalSQSQueues { [AwsHelper]::GetLocalSQSQueues() }
function New-LocalSQSQueue {
    param([string]$QueueName)
    [AwsHelper]::NewLocalSQSQueue($QueueName)
}
function Clear-LocalSQSQueue {
    param([string]$QueueUrl)
    [AwsHelper]::ClearLocalSQSQueue($QueueUrl)
}
function Send-LocalSQSMessage {
    param([string]$QueueUrl, [string]$MessageBody, [string]$GroupId)
    [AwsHelper]::SendLocalSQSMessage($QueueUrl, $MessageBody, $GroupId)
}
function Get-LocalSQSMessage {
    param([string]$QueueUrl)
    [AwsHelper]::GetLocalSQSMessage($QueueUrl)
}
function Get-LocalSQSAttributes {
    param([string]$QueueUrl)
    [AwsHelper]::GetLocalSQSAttributes($QueueUrl)
}

# --- AI Tools Wrappers ---
function Invoke-Codex-By-Ollama { [AiHelper]::InvokeCodex($args) }
function Invoke-Claude-By-Ollama { [AiHelper]::InvokeClaude($args) }
function Invoke-OpenClaw-By-Ollama { [AiHelper]::InvokeOpenClaw($args) }
function Invoke-Clawdbot-By-Ollama { [AiHelper]::InvokeClawdbot($args) }
function Invoke-Hermes-By-Ollama { [AiHelper]::InvokeHermes($args) }
function Invoke-HermesDesktop-By-Ollama { [AiHelper]::InvokeHermesDesktop($args) }
function Invoke-CopilotExplain {
    param([string]$Command)
    if (Get-Command gh -ErrorAction SilentlyContinue) {
        $extensions = gh extension list -ErrorAction SilentlyContinue
        if ($extensions -notmatch "gh-copilot") {
            Write-Host "Installing github/gh-copilot extension..." -ForegroundColor Cyan
            gh extension install github/gh-copilot
        }
        gh copilot explain $Command
    } else {
        Write-Error "GitHub CLI (gh) is not installed. Please install it from https://cli.github.com/"
    }
}
function Install-AIIntegrations { [AiHelper]::InstallAIIntegrations() }
function Initialize-OllamaServer { [AiHelper]::InitializeOllamaServer() }
function Set-OllamaModel {
    param([string]$ModelName)
    [AiHelper]::SetOllamaModel($ModelName)
}
function Ensure-OllamaServer { [AiHelper]::EnsureOllamaServer() }
function Invoke-OllamaLogs { [AiHelper]::ShowOllamaLogs() }
function Invoke-Npm {
    param([string[]]$ArgsList)
    & npm @ArgsList
}

function Invoke-ChatGPT {
    param([string]$Query)
    if (Get-Command chatgpt -ErrorAction SilentlyContinue) {
        if ($Query) { chatgpt $Query } else { chatgpt }
    } else {
        Write-Warning "ChatGPT CLI command 'chatgpt' is not installed. Routing to local OpenClaw instead."
        [AiHelper]::InvokeOpenClaw(@())
    }
}

# Unified AI Agent Selector
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
        [Parameter(ParameterSetName="Codex")][Alias("cx")][switch]$UseCodex,
        [string]$Model
    )
    [AiHelper]::InvokeMultiAgent($Query, $PSCmdlet.ParameterSetName, $Model)
}

# AI Tools Aliases
Set-Alias -Name ai          -Value Invoke-MultiAgent        -Force
Set-Alias -Name codex       -Value Invoke-Codex-By-Ollama   -Force
Set-Alias -Name claude      -Value Invoke-Claude-By-Ollama  -Force
Set-Alias -Name openclaw    -Value Invoke-OpenClaw-By-Ollama -Force
Set-Alias -Name clawdbot    -Value Invoke-Clawdbot-By-Ollama -Force
Set-Alias -Name hermes      -Value Invoke-Hermes-By-Ollama   -Force
Set-Alias -Name hermesd     -Value Invoke-HermesDesktop-By-Ollama -Force
Set-Alias -Name model       -Value Set-OllamaModel          -Force
Set-Alias -Name ollama-logs -Value Invoke-OllamaLogs         -Force

# --- Antigravity Multi-Account Manager Wrappers ---
function Invoke-AgyAccount {
    [CmdletBinding()]
    param(
        [Parameter(Position=0)]
        [ValidateSet("list", "lst", "show", "use", "add", "create", "remove", "delete", "del", "run", "exec", "logout", "signout", "login", "signin", "interactive", "select")]
        [string]$Action = "interactive",
        [Parameter(Position=1)]
        [string]$Account,
        [Parameter(Position=2, ValueFromRemainingArguments=$true)]
        [string[]]$ExtraArgs,
        [Parameter()][switch]$Temporary
    )
    switch ($Action) {
        "interactive" { [AgyAccountManager]::SelectAccountInteractive() }
        "select"      { [AgyAccountManager]::SelectAccountInteractive() }
        { $_ -in "list", "lst", "show" } { [AgyAccountManager]::ShowAccounts() }
        "use" {
            if ([string]::IsNullOrWhiteSpace($Account)) {
                [AgyAccountManager]::SelectAccountInteractive()
                return
            }
            [AgyAccountManager]::SetActiveAccount($Account, $Temporary)
        }
        { $_ -in "add", "create" } {
            if ([string]::IsNullOrWhiteSpace($Account)) {
                Write-Error "Please specify an account name to add."
                return
            }
            [AgyAccountManager]::AddAccount($Account)
        }
        { $_ -in "remove", "delete", "del" } {
            if ([string]::IsNullOrWhiteSpace($Account)) {
                Write-Error "Please specify an account name to remove."
                return
            }
            [AgyAccountManager]::RemoveAccount($Account)
        }
        { $_ -in "logout", "signout" } {
            $target = if ($Account) { $Account } else { [AgyAccountManager]::GetActiveAccount() }
            [AgyAccountManager]::ResetCredentials($target)
        }
        { $_ -in "login", "signin" } {
            $target = if ($Account) { $Account } else { [AgyAccountManager]::GetActiveAccount() }
            [AgyAccountManager]::ResetCredentials($target)
            [AgyAccountManager]::InvokeWithAccount($target, @())
        }
        { $_ -in "run", "exec" } {
            if ([string]::IsNullOrWhiteSpace($Account)) {
                Write-Error "Please specify an account name to run under."
                return
            }
            [AgyAccountManager]::InvokeWithAccount($Account, $ExtraArgs)
        }
    }
}
Set-Alias -Name agy-account -Value Invoke-AgyAccount -Force
Set-Alias -Name agy-acc     -Value Invoke-AgyAccount -Force

function Invoke-AgyMenu {
    [AgyAccountManager]::ManageAccountsInteractive()
}
Set-Alias -Name agy-m -Value Invoke-AgyMenu -Force

function Invoke-AccountSession {
    [CmdletBinding()]
    param(
        [Parameter(Position=0)][string]$Account,
        [Parameter(Position=1)][ScriptBlock]$Command,
        [Parameter()][switch]$Temp,
        [Parameter()][switch]$Status
    )

    if ($Status) {
        $active = [AgyAccountManager]::GetActiveAccount()
        $quota = [AgyAccountManager]::CalculateRollingQuotas($active)
        if ($Global:AiMode) {
            Write-Host "Account:$active;Weekly:$($quota.RemainingWeekly)%;5H:$($quota.Remaining5H)%;Count5H:$($quota.Count5H)"
        } else {
            Write-Host "=============================================" -ForegroundColor Cyan
            Write-Host " QUOTA STATUS: $active" -ForegroundColor Cyan
            Write-Host "=============================================" -ForegroundColor Cyan
            Write-Host "  * Weekly Quota:   $($quota.RemainingWeekly)% remaining ($($quota.CountWeekly)/1000)" -ForegroundColor Gray
            Write-Host "  * Five Hour Limit: $($quota.Remaining5H)% remaining ($($quota.Count5H)/50)" -ForegroundColor Gray
            Write-Host "=============================================" -ForegroundColor Cyan
        }
        return
    }

    if ([string]::IsNullOrWhiteSpace($Account)) {
        if ($Global:AiMode) {
            $accounts = [AgyAccountManager]::GetAccounts()
            foreach ($acc in $accounts) {
                $meta = [AgyAccountManager]::GetAccountMetadata($acc)
                $tokenFile = Join-Path ([AgyAccountManager]::GetAccountDirectory($acc)) "keyring_token.txt"
                $status = if (Test-Path $tokenFile) { "LoggedIn" } else { "LoggedOut" }
                Write-Host "$acc,$status,$($meta.UsageCount)"
            }
        } else {
            [AgyAccountManager]::ManageAccountsInteractive()
        }
        return
    }

    if ($Command) {
        $targetPath = [AgyAccountManager]::GetAccountDirectory($Account)
        if (-not (Test-Path $targetPath)) {
            Write-Error "Account '$Account' does not exist."
            return
        }
        $oldHome = $env:GEMINI_HOME
        try {
            $env:GEMINI_HOME = $targetPath
            [AgyAccountManager]::RestoreActiveToken($Account)
            $Command.Invoke()
        } finally {
            $env:GEMINI_HOME = $oldHome
            $priorActive = [AgyAccountManager]::GetActiveAccount()
            [AgyAccountManager]::RestoreActiveToken($priorActive)
        }
        return
    }

    [AgyAccountManager]::SetActiveAccount($Account, $Temp)
}
Set-Alias -Name acc -Value Invoke-AccountSession -Force


function agy {
    [CmdletBinding()]
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$PassThruArgs)
    [AgyAccountManager]::InvokeAgy($PassThruArgs)
}

function multigravity {
    [CmdletBinding()]
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$PassThruArgs)
    [AgyAccountManager]::InvokeMultigravity($PassThruArgs)
}

# --- Tab Autocompleters ---
if (Get-Command multigravity -ErrorAction SilentlyContinue) {
    Register-ArgumentCompleter -Native -CommandName multigravity -ScriptBlock {
        param($wordToComplete, $commandAst, $cursorPosition)
        $opts = @('new', 'list', 'status', 'rename', 'delete', 'clone', 'template', 'export', 'import', 'update', 'doctor', 'stats', 'completion', 'help')
        $profiles = if (Test-Path (Join-Path $env:USERPROFILE "AntigravityProfiles")) {
            Get-ChildItem -Directory -Path (Join-Path $env:USERPROFILE "AntigravityProfiles") | Select-Object -ExpandProperty Name
        } else { @() }
        ($opts + $profiles) | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
            [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
        }
    }
}

# --- Help shortcuts ---
Set-Alias -Name cc -Value Get-CustomCommands -Force
function cg   { [ProfileHelp]::Show("Git") }
function cnet { [ProfileHelp]::Show("Net") }
function csys { [ProfileHelp]::Show("Sys") }
function cdk  { [ProfileHelp]::Show("Docker") }
function cai  { [ProfileHelp]::Show("AI") }
function caws { [ProfileHelp]::Show("AWS") }
function cnav { [ProfileHelp]::Show("Navigation") }
function cssh { [ProfileHelp]::Show("SSH") }

# Theme Switcher
function Select-ShellTheme {
    [ThemeHelper]::SelectThemeInteractive()
    if ($Global:NewThemeToApply) {
        $themePath = $Global:NewThemeToApply
        $Global:NewThemeToApply = $null
        $Global:AgyOriginalPromptCmd = $null
        Remove-Module -Name "oh-my-posh-core" -Force -ErrorAction SilentlyContinue
        Remove-Item -Path "Function:\prompt" -Force -ErrorAction SilentlyContinue
        Remove-Item -Path "Function:\prompt_original" -Force -ErrorAction SilentlyContinue
        oh-my-posh --init --shell pwsh --config $themePath | Invoke-Expression
        [AgyAccountManager]::RegisterPromptHook()
        [TerminalMenu]::InitializeTuiColors()
    }
}
Set-Alias -Name theme -Value Select-ShellTheme -Force

function Clone-Project {
    param(
        [Parameter(Mandatory=$true, Position=0)][string]$Url,
        [Parameter(Position=1)][string]$DestName
    )
    
    $baseDir = if (Test-Path "C:\Users\sshuser\project") { "C:\Users\sshuser\project" } else { "$env:USERPROFILE\Desktop\project" }
    if (-not (Test-Path $baseDir)) {
        $baseDir = Join-Path $env:USERPROFILE "Documents"
    }

    if (-not $DestName) {
        if ($Url -match '/([^/]+)\.git$') {
            $DestName = $Matches[1]
        } elseif ($Url -match '/([^/]+)$') {
            $DestName = $Matches[1]
        } else {
            $DestName = "cloned-project-" + (Get-Random)
        }
    }

    $targetPath = Join-Path $baseDir $DestName
    Write-Host "Cloning project from $Url into $targetPath..." -ForegroundColor Cyan
    git clone $Url $targetPath

    if ($LASTEXITCODE -eq 0 -and (Test-Path $targetPath)) {
        $cacheFile = Join-Path $env:USERPROFILE ".gemini\antigravity\workspace_cache.json"
        Remove-Item $cacheFile -Force -ErrorAction SilentlyContinue
        Write-Host "Project successfully cloned and registered to workspace cache!" -ForegroundColor Green
    } else {
        Write-Error "Failed to clone repository."
    }
}
Set-Alias -Name clone-project -Value Clone-Project -Force
# Operations Dashboards & Shortcuts
function Invoke-DockerDashboard { [DockerHelper]::Dkcl() }
Set-Alias -Name dkcl -Value Invoke-DockerDashboard -Force

function Invoke-KillPort {
    param([Parameter(Mandatory=$true, Position=0)][int]$Port)
    [SystemHelper]::KillPort($Port)
}
Set-Alias -Name killport -Value Invoke-KillPort -Force

function Invoke-SystemMonitor { [SystemHelper]::SystemMonitor() }
Set-Alias -Name sysmon -Value Invoke-SystemMonitor -Force

# Scaffolding & Git TUI Shortcuts
function Invoke-ProjectScaffolder {
    param([string]$Template, [string]$Name, [int]$Port)
    [ProjectScaffolder]::ScaffoldProject($Template, $Name, $Port)
}
Set-Alias -Name new-project -Value Invoke-ProjectScaffolder -Force

function Invoke-GitBranchCheckout { [GitHelper]::BranchCheckoutTui() }
Set-Alias -Name co -Value Invoke-GitBranchCheckout -Force

function Invoke-GitCommitWizard {
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Message)
    $msg = $Message -join ' '
    [GitHelper]::Gcmt($msg)
}
Set-Alias -Name gcmt -Value Invoke-GitCommitWizard -Force

function Invoke-AskAi {
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Query)
    $q = $Query -join ' '
    [AiHelper]::AskAi($q)
}
Set-Alias -Name ask-ai -Value Invoke-AskAi -Force

function Invoke-SecretVault {
    [CmdletBinding()]
    param(
        [Parameter(Position=0)]
        [ValidateSet("set", "get", "list", "remove", "rm")]
        [string]$Action = "list",
        [Parameter(Position=1)]
        [string]$Key,
        [Parameter(Position=2)]
        [string]$Value
    )

    switch ($Action) {
        "set" {
            if (-not $Key -or -not $Value) {
                Write-Error "Usage: sec set <key> <value>"
                return
            }
            [AgySecretVault]::SetSecret($Key, $Value)
        }
        "get" {
            if (-not $Key) {
                Write-Error "Usage: sec get <key>"
                return
            }
            $val = [AgySecretVault]::GetSecret($Key)
            if ($val) { Write-Host $val }
        }
        "list" {
            [AgySecretVault]::ListSecrets()
        }
        { $_ -in "remove", "rm" } {
            if (-not $Key) {
                Write-Error "Usage: sec remove <key>"
                return
            }
            [AgySecretVault]::RemoveSecret($Key)
        }
    }
}
Set-Alias -Name sec -Value Invoke-SecretVault -Force
function Invoke-DbTui {
    param([Parameter(Mandatory=$true, Position=0)][string]$DbPath)
    [DatabaseHelper]::DbTui($DbPath)
}
Set-Alias -Name db-tui -Value Invoke-DbTui -Force

function Invoke-LogStream {
    param([Parameter(Position=0)][string]$LogPath)
    [LogHelper]::StreamLogs($LogPath)
}
Set-Alias -Name logstream -Value Invoke-LogStream -Force

# --- Antigravity Projects Wrappers ---
function Start-Manager { [Projects]::StartManager() }
Set-Alias -Name mgr -Value Start-Manager -Force

function Start-Proxy { [Projects]::StartProxy() }
Set-Alias -Name prxy -Value Start-Proxy -Force

# --- Antigravity Account Management Wrappers ---
function Toggle-AutoSwitch { [AgyAccountManager]::ToggleAutoSwitch() }
Set-Alias -Name autoswitch -Value Toggle-AutoSwitch -Force

function Show-AccountsSummary { [AgyAccountManager]::ShowAllAccountsSummary() }
Set-Alias -Name acc-sum -Value Show-AccountsSummary -Force

# --- AI Session Dashboard Wrappers ---
function Show-AiDashboard { [AiHelper]::ShowAiDashboard() }
Set-Alias -Name ai-dash -Value Show-AiDashboard -Force

# --- AWS LocalStack Aliases ---
Set-Alias -Name s3ls      -Value Get-S3Buckets -Force
Set-Alias -Name s3mb      -Value New-S3Bucket -Force
Set-Alias -Name lbls      -Value Get-LambdaFunctions -Force
Set-Alias -Name sqsls     -Value Get-LocalSQSQueues -Force
Set-Alias -Name sqsmb     -Value New-LocalSQSQueue -Force
Set-Alias -Name sqspurge  -Value Clear-LocalSQSQueue -Force
Set-Alias -Name sqssend   -Value Send-LocalSQSMessage -Force
Set-Alias -Name sqsrecv   -Value Get-LocalSQSMessage -Force
Set-Alias -Name sqsattr   -Value Get-LocalSQSAttributes -Force

# --- Terminal IDE Wrapper ---
function Invoke-TerminalIde {
    param([string]$Path)
    $targetPath = if ($Path) { $Path } else { Get-Location }
    [ProfileNavigator]::LaunchTerminalIde($targetPath)
}
Set-Alias -Name ide -Value Invoke-TerminalIde -Force

# --- System History Wrapper ---
function Clear-ShellHistory { [SystemHelper]::ClearHistory() }
Set-Alias -Name clh -Value Clear-ShellHistory -Force

#endregion



