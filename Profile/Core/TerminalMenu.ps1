class ColoredLine {
    [string]$Text
    [string]$Color
}

class MenuItem {
    [string]$Label
    [string]$Type    # "Category" or "Command"
    [object]$Value   # Category name string or CommandDoc object

    [string] ToString() {
        return $this.Label
    }
}

class TerminalMenu {
    static [string] MapHexToConsoleColor([string]$hex) {
        if (-not $hex -or $hex -notmatch '^#?([0-9a-fA-F]{6})') { return "Cyan" }
        $clean = $Matches[1]
        try {
            $r = [System.Convert]::ToInt32($clean.Substring(0, 2), 16)
            $g = [System.Convert]::ToInt32($clean.Substring(2, 2), 16)
            $b = [System.Convert]::ToInt32($clean.Substring(4, 2), 16)
            
            $max = [Math]::Max($r, [Math]::Max($g, $b))
            $min = [Math]::Min($r, [Math]::Min($g, $b))
            
            if (($max - $min) -lt 35) {
                return if ($max -lt 80) { "DarkGray" } else { "White" }
            }
            if ($r -gt $g -and $r -gt $b) {
                if ($b -gt 90 -and ($r - $b) -lt 110) { return "Magenta" }
                return if ($g -gt 120 -and ($r - $g) -lt 60) { "Yellow" } else { "Red" }
            }
            if ($g -gt $r -and $g -gt $b) {
                return "Green"
            }
            if ($b -gt $r -and $b -gt $g) {
                if (($r - $g) -gt 40) { return "Magenta" }
                return "Cyan"
            }
            if ($g -gt $r -and $b -gt $r) {
                return "Cyan"
            }
            if ($r -gt $b -and $g -gt $b) {
                return "Yellow"
            }
        } catch {}
        return "Cyan"
    }

    static [void] InitializeTuiColors() {
        $theme = $env:THEME
        $colors = @{
            Header    = "Cyan"
            Selected  = "Green"
            Regular   = "Gray"
            Search    = "Green"
            Suggest   = "Yellow"
            Footer    = "DarkGray"
            Alert     = "Red"
        }

        $themesPath = $env:POSH_THEMES_PATH
        if (-not $themesPath) {
            $themesPath = Join-Path -Path $env:USERPROFILE -ChildPath "Documents\PowerShell\asset\powershell-themes"
        }

        # Try dynamic theme color extraction
        $firstColor = $null
        $secondColor = $null
        if ($theme -and (Test-Path $themesPath)) {
            $themePath = Join-Path -Path $themesPath -ChildPath "$theme.omp.json"
            if (Test-Path $themePath) {
                try {
                    $json = Get-Content $themePath -Raw | ConvertFrom-Json
                    if ($json.blocks) {
                        foreach ($block in $json.blocks) {
                            if ($block.segments) {
                                foreach ($seg in $block.segments) {
                                    $col = if ($seg.background) { $seg.background } else { $seg.foreground }
                                    if ($col) {
                                        if (-not $firstColor) {
                                            $firstColor = $col
                                        } elseif (-not $secondColor -and $col -ne $firstColor) {
                                            $secondColor = $col
                                            break
                                        }
                                    }
                                }
                            }
                            if ($firstColor -and $secondColor) { break }
                        }
                    }
                } catch {}
            }
        }

        if ($firstColor) {
            $headerColor = [TerminalMenu]::MapHexToConsoleColor($firstColor)
            $colors.Header = $headerColor
            $colors.Search = $headerColor
            $colors.Suggest = if ($headerColor -eq "Yellow") { "Cyan" } else { "Yellow" }
            
            if ($secondColor) {
                $colors.Selected = [TerminalMenu]::MapHexToConsoleColor($secondColor)
            } else {
                $colors.Selected = $headerColor
            }
        } elseif ($theme) {
            $themeLower = $theme.ToLower()
            if ($themeLower -like "*dracula*") {
                $colors.Header = "Magenta"
                $colors.Selected = "DarkCyan"
                $colors.Suggest = "Magenta"
            }
            elseif ($themeLower -like "*tokyonight*") {
                $colors.Header = "Cyan"
                $colors.Selected = "Magenta"
                $colors.Suggest = "Yellow"
            }
            elseif ($themeLower -like "*agnoster*") {
                $colors.Header = "Blue"
                $colors.Selected = "Yellow"
                $colors.Suggest = "Cyan"
            }
            elseif ($themeLower -like "*minimal*" -or $themeLower -like "*pure*") {
                $colors.Header = "White"
                $colors.Selected = "White"
                $colors.Suggest = "Gray"
            }
        }
        $Global:TuiColors = $colors
    }

    static [int] Show([string]$Header, [string[]]$Items, [int]$DefaultIndex) {
        return [TerminalMenu]::Show($Header, $Items, $DefaultIndex, $false)
    }
    static [int] ShowRobust([string[]]$HeaderLines, [string[]]$Items, [int]$DefaultIndex, [bool]$SearchByDefault, [bool]$FullScreen) {
        $method = [TerminalMenu].GetMethod("Show", [type[]]@([string[]], [string[]], [int], [bool], [bool]))
        return $method.Invoke($null, @($HeaderLines, $Items, $DefaultIndex, $SearchByDefault, $FullScreen))
    }
    static [int] Show([string[]]$HeaderLines, [string[]]$Items, [int]$DefaultIndex, [bool]$SearchByDefault, [bool]$FullScreen) {
        return [TerminalMenu]::Show($HeaderLines, $Items, [string[]]@(), $DefaultIndex, $SearchByDefault, $FullScreen)
    }

    static [int] Show([string[]]$HeaderLines, [string[]]$Items, [string[]]$Cmds, [int]$DefaultIndex, [bool]$SearchByDefault, [bool]$FullScreen) {
        $count = $Items.Count
        if ($count -eq 0) { return -1 }

        $Global:TerminalMenuLastKey = $null
        if ($null -eq $Global:TuiColors) { [TerminalMenu]::InitializeTuiColors() }

        $currentIndex = $DefaultIndex
        $filterText = ""
        $searchMode = $SearchByDefault

        if ($FullScreen) {
            Clear-Host
            $startRow = 0
            $startCol = 0
        } else {
            $maxVisible = [Math]::Min(12, [Math]::Max(3, ([Console]::WindowHeight - 10)))
            $estimatedLines = 7 + $maxVisible
            if ($searchMode) { $estimatedLines += 3 }
            
            $visibleCursorRow = [Console]::CursorTop - [Console]::WindowTop
            $remainingVisibleLines = [Console]::WindowHeight - $visibleCursorRow
            $spaceNeeded = $estimatedLines - $remainingVisibleLines
            if ($spaceNeeded -gt 0) {
                for ($i = 0; $i -lt $spaceNeeded; $i++) { Write-Host "" }
                $targetRow = [Math]::Max(0, [Console]::CursorTop - $spaceNeeded)
                try { [Console]::SetCursorPosition([Console]::CursorLeft, $targetRow) } catch {}
            }
            $startRow = [Console]::CursorTop
            $startCol = [Console]::CursorLeft
        }

        $lastLinesCount = 0
        $width = 0

        $oldCursorVisible = $null
        try {
            $oldCursorVisible = [Console]::CursorVisible
            [Console]::CursorVisible = $false
        } catch {}

        try {
            while ($true) {
                $width = [Console]::WindowWidth - 1
                $cleanFilter = $filterText
                if ($filterText.StartsWith("/")) {
                    $cleanFilter = $filterText.Substring(1)
                }

                $activeItems = $Items
                $activeCount = $count
                $activeCmds = $Cmds
                
                $isSlashSuggestion = $false
                if ($filterText.StartsWith("/")) {
                    $isSlashSuggestion = $true
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
                    $activeItems = $slashSuggestions
                    $activeCount = $slashSuggestions.Count
                    $activeCmds = $null
                }

                $filteredIndices = [System.Collections.Generic.List[int]]::new()
                for ($i = 0; $i -lt $activeCount; $i++) {
                    if ([string]::IsNullOrWhiteSpace($cleanFilter) -or $activeItems[$i] -like "*$cleanFilter*") {
                        $null = $filteredIndices.Add($i)
                    }
                }

                $filteredCount = $filteredIndices.Count
                if ($filteredCount -gt 0) {
                    if ($currentIndex -ge $filteredCount) { $currentIndex = 0 }
                } else {
                    $currentIndex = 0
                }

                $outputLines = [System.Collections.Generic.List[ColoredLine]]::new()
                $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })

                foreach ($hLine in $HeaderLines) {
                    $null = $outputLines.Add([ColoredLine]@{ Text = $hLine; Color = $Global:TuiColors.Header })
                }
                $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })

                if ($searchMode) {
                    $null = $outputLines.Add([ColoredLine]@{ Text = "  [Search] query: $filterText"; Color = $Global:TuiColors.Search })
                    if ($filteredCount -gt 0) {
                        $null = $outputLines.Add([ColoredLine]@{ Text = "  [Suggest]: $($activeItems[$filteredIndices[0]])"; Color = $Global:TuiColors.Suggest })
                    } else {
                        $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })
                    }
                    $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })
                } elseif (-not [string]::IsNullOrWhiteSpace($cleanFilter)) {
                    $null = $outputLines.Add([ColoredLine]@{ Text = "  [Filter]: $cleanFilter (Press [Backspace] to clear, [/] to edit)"; Color = $Global:TuiColors.Search })
                    $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })
                }

                # Display command definition details above alias list
                $hasCmdLine = $false
                if ($filteredCount -gt 0) {
                    $highlightedActualIndex = $filteredIndices[$currentIndex]
                    if ($activeCmds -and $highlightedActualIndex -lt $activeCmds.Count) {
                        $cmdText = $activeCmds[$highlightedActualIndex]
                        if (-not [string]::IsNullOrWhiteSpace($cmdText)) {
                            $null = $outputLines.Add([ColoredLine]@{ Text = "  [Cmd]: $cmdText"; Color = $Global:TuiColors.Suggest })
                            $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })
                            $hasCmdLine = $true
                        }
                    }
                }

                if ($filteredCount -eq 0) {
                    $null = $outputLines.Add([ColoredLine]@{ Text = "     [No items matching filter]"; Color = $Global:TuiColors.Alert })
                    $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })
                } else {
                    $nonItemLines = 6 + $HeaderLines.Count
                    if ($searchMode) { $nonItemLines += 2 }
                    if ($hasCmdLine) { $nonItemLines += 2 }
                    $maxVisible = [Math]::Min(12, [Math]::Max(3, ([Console]::WindowHeight - $nonItemLines)))
                    $start = 0
                    if ($currentIndex -ge $maxVisible) {
                        $start = $currentIndex - $maxVisible + 1
                    }
                    $end = [Math]::Min($start + $maxVisible - 1, $filteredCount - 1)
                    if ($end - $start + 1 -lt $maxVisible -and $filteredCount -gt $maxVisible) {
                        $start = [Math]::Max(0, $end - $maxVisible + 1)
                    }

                    if ($start -gt 0) {
                        $null = $outputLines.Add([ColoredLine]@{ Text = "     ^ ... ($start more above) ..."; Color = $Global:TuiColors.Footer })
                    }
                    for ($i = $start; $i -le $end; $i++) {
                        $actualIndex = $filteredIndices[$i]
                        $label = $activeItems[$actualIndex]
                        $maxLabelLen = $width - 6
                        if ($maxLabelLen -gt 3 -and $label.Length -gt $maxLabelLen) {
                            $label = $label.Substring(0, $maxLabelLen - 3) + "..."
                        }
                        if ($i -eq $currentIndex) {
                            $null = $outputLines.Add([ColoredLine]@{ Text = "  >  $label"; Color = $Global:TuiColors.Selected })
                        } else {
                            if ($label -match "future|support future") {
                                $null = $outputLines.Add([ColoredLine]@{ Text = "     $label"; Color = "DarkGray" })
                            } else {
                                $null = $outputLines.Add([ColoredLine]@{ Text = "     $label"; Color = $Global:TuiColors.Regular })
                            }
                        }
                    }
                    if ($end -lt $filteredCount - 1) {
                        $remaining = $filteredCount - 1 - $end
                        $null = $outputLines.Add([ColoredLine]@{ Text = "     v ... ($remaining more below) ..."; Color = $Global:TuiColors.Footer })
                    }
                    $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })
                }

                $helpStr = if ($width -lt 70) { "Arrows/Enter: select, [/] search, [Esc] exit" } else { "Type to filter. [Enter] to select suggestion, [Esc] to exit search mode." }
                $null = $outputLines.Add([ColoredLine]@{ Text = $helpStr; Color = $Global:TuiColors.Footer })

                try {
                    [Console]::SetCursorPosition($startCol, $startRow)
                } catch {
                    Clear-Host
                    $startRow = 0
                    $startCol = 0
                    try { [Console]::SetCursorPosition($startCol, $startRow) } catch {}
                }

                foreach ($line in $outputLines) {
                    $txt = $line.Text
                    $padded = if ($txt.Length -lt $width) { $txt + (" " * ($width - $txt.Length)) } else { $txt.Substring(0, $width) }
                    Write-Host $padded -ForegroundColor $line.Color
                }

                $diff = $lastLinesCount - $outputLines.Count
                if ($diff -gt 0) {
                    for ($d = 0; $d -lt $diff; $d++) {
                        Write-Host (" " * ([Console]::WindowWidth - 1))
                    }
                }
                $lastLinesCount = $outputLines.Count

                $key = $null
                try {
                    if ($null -ne $Global:TerminalMenuOnIdle) {
                        $needsRedraw = $false
                        while (-not [Console]::KeyAvailable) {
                            $updatedItems = &$Global:TerminalMenuOnIdle
                            if ($null -ne $updatedItems) {
                                $Items = $updatedItems
                                $count = $Items.Count
                                $needsRedraw = $true
                                break
                            }
                            Start-Sleep -Milliseconds 100
                        }
                        if ($needsRedraw) {
                            continue
                        }
                        $key = [Console]::ReadKey($true)
                    } else {
                        $key = [Console]::ReadKey($true)
                    }
                } catch {
                    [Console]::CursorVisible = $true
                    $choice = Read-Host "Select index [1-$filteredCount]"
                    $val = 0
                    if ([int]::TryParse($choice, [ref]$val) -and $val -ge 1 -and $val -le $filteredCount) {
                        return $filteredIndices[$val - 1]
                    }
                    return -1
                }

                if ($searchMode) {
                    if ($key.Key -eq [ConsoleKey]::Escape) {
                        $searchMode = $false
                        $currentIndex = 0
                    }
                    elseif ($key.Key -eq [ConsoleKey]::Enter) {
                        if ($filteredCount -gt 0) {
                            $actualIdx = $filteredIndices[$currentIndex]
                            if ($isSlashSuggestion) {
                                $chosenSuggestion = $activeItems[$actualIdx]
                                if ($chosenSuggestion -match '^([a-zA-Z0-9_\-]+(\s+[a-zA-Z0-9_\-\.]+)*)\s+-') {
                                    $Global:TerminalMenuSlashCommand = $Matches[1].Trim()
                                } else {
                                    $Global:TerminalMenuSlashCommand = $chosenSuggestion.Trim()
                                }
                                $Global:TerminalMenuLastKey = "slashcmd"
                                return -10
                            }
                            return $actualIdx
                        }
                        $searchMode = $false
                    }
                    elseif ($key.Key -eq [ConsoleKey]::F5) {
                        $Global:TerminalMenuLastKey = "f5"
                        return -5
                    }
                    elseif ($key.Modifiers -eq [ConsoleModifiers]::Control -and $key.Key -eq [ConsoleKey]::P) {
                        if ($filteredCount -gt 0) {
                            $actualIdx = $filteredIndices[$currentIndex]
                            $Global:TerminalMenuLastKey = "ctrlp"
                            return $actualIdx
                        }
                    }
                    elseif ($key.Key -eq [ConsoleKey]::Backspace) {
                        if ($filterText.Length -gt 0) {
                            $filterText = $filterText.Substring(0, $filterText.Length - 1)
                        }
                        $currentIndex = 0
                    }
                    elseif ($key.Key -eq [ConsoleKey]::UpArrow) {
                        if ($filteredCount -gt 0) {
                            $currentIndex = ($currentIndex - 1 + $filteredCount) % $filteredCount
                        }
                    }
                    elseif ($key.Key -eq [ConsoleKey]::DownArrow) {
                        if ($filteredCount -gt 0) {
                            $currentIndex = ($currentIndex + 1) % $filteredCount
                        }
                    }
                    elseif ($key.KeyChar -ge 32 -and $key.KeyChar -le 126) {
                        $filterText += $key.KeyChar
                        $currentIndex = 0
                    }
                } else {
                    if ($key.Key -eq [ConsoleKey]::UpArrow) {
                        if ($filteredCount -gt 0) {
                            $origIdx = $currentIndex
                            do {
                                $currentIndex = ($currentIndex - 1 + $filteredCount) % $filteredCount
                            } while ($Items[$filteredIndices[$currentIndex]] -match "future|support future" -and $currentIndex -ne $origIdx)
                        }
                    }
                    elseif ($key.Key -eq [ConsoleKey]::DownArrow) {
                        if ($filteredCount -gt 0) {
                            $origIdx = $currentIndex
                            do {
                                $currentIndex = ($currentIndex + 1) % $filteredCount
                            } while ($Items[$filteredIndices[$currentIndex]] -match "future|support future" -and $currentIndex -ne $origIdx)
                        }
                    }
                    elseif ($HeaderLines -and $HeaderLines.Count -gt 0 -and $HeaderLines[0] -like "*Select Project*" -and $key.KeyChar.ToString().ToLower() -in @('c', 'i', 'd', 't')) {
                        if ($filteredCount -gt 0) {
                            $actualIdx = $filteredIndices[$currentIndex]
                            if ($Items[$actualIdx] -match "future|support future") { continue }
                            $Global:TerminalMenuLastKey = $key.KeyChar.ToString().ToLower()
                            return $actualIdx
                        }
                    }
                    elseif ($key.Key -eq [ConsoleKey]::Enter) {
                        if ($filteredCount -gt 0) {
                            $actualIdx = $filteredIndices[$currentIndex]
                            if ($isSlashSuggestion) {
                                $chosenSuggestion = $activeItems[$actualIdx]
                                if ($chosenSuggestion -match '^([a-zA-Z0-9_\-]+(\s+[a-zA-Z0-9_\-\.]+)*)\s+-') {
                                    $Global:TerminalMenuSlashCommand = $Matches[1].Trim()
                                } else {
                                    $Global:TerminalMenuSlashCommand = $chosenSuggestion.Trim()
                                }
                                $Global:TerminalMenuLastKey = "slashcmd"
                                return -10
                            }
                            if ($Items[$actualIdx] -match "future|support future") { continue }
                            $Global:TerminalMenuLastKey = "enter"
                            return $actualIdx
                        }
                        return -1
                    }
                    elseif ($key.Key -eq [ConsoleKey]::F5) {
                        $Global:TerminalMenuLastKey = "f5"
                        return -5
                    }
                    elseif ($key.Modifiers -eq [ConsoleModifiers]::Control -and $key.Key -eq [ConsoleKey]::P) {
                        if ($filteredCount -gt 0) {
                            $actualIdx = $filteredIndices[$currentIndex]
                            $Global:TerminalMenuLastKey = "ctrlp"
                            return $actualIdx
                        }
                    }
                    elseif ($key.Key -eq [ConsoleKey]::Escape) {
                        if (-not [string]::IsNullOrWhiteSpace($filterText)) {
                            $filterText = ""
                            $currentIndex = 0
                        } else {
                            $Global:TerminalMenuLastKey = "escape"
                            return -1
                        }
                    }
                    elseif ($key.KeyChar -ge 32 -and $key.KeyChar -le 126) {
                        $searchMode = $true
                        $filterText += $key.KeyChar
                        $currentIndex = 0
                    }
                    elseif ($key.Key -eq [ConsoleKey]::Backspace) {
                        $filterText = ""
                        $currentIndex = 0
                    }
                }
            }
        } finally {
            $Global:TerminalMenuOnIdle = $null
            try {
                [Console]::SetCursorPosition($startCol, $startRow)
                for ($d = 0; $d -lt $lastLinesCount; $d++) {
                    Write-Host (" " * ([Console]::WindowWidth - 1))
                }
                [Console]::SetCursorPosition($startCol, $startRow)
                if ($null -ne $oldCursorVisible) {
                    [Console]::CursorVisible = $oldCursorVisible
                }
            } catch {}
        }
        return -1
    }

    static [int] Show([string]$Header, [string[]]$Items, [int]$DefaultIndex, [bool]$SearchByDefault) {
        $count = $Items.Count
        if ($count -eq 0) { return -1 }

        $Global:TerminalMenuLastKey = $null

        # Ensure TUI colors are initialized
        if ($null -eq $Global:TuiColors) { [TerminalMenu]::InitializeTuiColors() }

        # Resolve first selectable index
        $firstSelectable = -1
        for ($i = 0; $i -lt $count; $i++) {
            if ($Items[$i] -notmatch "future|support future") {
                $firstSelectable = $i
                break
            }
        }
        $currentIndex = if ($firstSelectable -ge 0) { $firstSelectable } else { $DefaultIndex }
        $filterText = ""
        $searchMode = $SearchByDefault

        # Pre-scroll if drawing near the bottom to avoid repeated headers due to scroll shifts
        $maxVisible = [Math]::Min(12, [Math]::Max(3, ([Console]::WindowHeight - 10)))
        $estimatedLines = 7 + $maxVisible
        if ($searchMode) { $estimatedLines += 3 }
        
        $visibleCursorRow = [Console]::CursorTop - [Console]::WindowTop
        $remainingVisibleLines = [Console]::WindowHeight - $visibleCursorRow
        $spaceNeeded = $estimatedLines - $remainingVisibleLines
        if ($spaceNeeded -gt 0) {
            for ($i = 0; $i -lt $spaceNeeded; $i++) {
                Write-Host ""
            }
            $targetRow = [Math]::Max(0, [Console]::CursorTop - $spaceNeeded)
            try {
                [Console]::SetCursorPosition([Console]::CursorLeft, $targetRow)
            } catch {}
        }
        $startRow = [Console]::CursorTop
        $startCol = [Console]::CursorLeft
        $lastLinesCount = 0
        $width = 0

        # Hide cursor
        $oldCursorVisible = $null
        try {
            $oldCursorVisible = [Console]::CursorVisible
            [Console]::CursorVisible = $false
        } catch {}

        try {
            while ($true) {
                $width = [Console]::WindowWidth - 1
                # Filter items
                $cleanFilter = $filterText
                if ($filterText.StartsWith("/")) {
                    $cleanFilter = $filterText.Substring(1)
                }

                $filteredIndices = [System.Collections.Generic.List[int]]::new()
                for ($i = 0; $i -lt $count; $i++) {
                    if ([string]::IsNullOrWhiteSpace($cleanFilter) -or $Items[$i] -like "*$cleanFilter*") {
                        $null = $filteredIndices.Add($i)
                    }
                }

                $filteredCount = $filteredIndices.Count
                if ($filteredCount -gt 0) {
                    if ($currentIndex -ge $filteredCount) {
                        $currentIndex = 0
                    }
                } else {
                    $currentIndex = 0
                }

                # Compile colored output lines
                $outputLines = [System.Collections.Generic.List[ColoredLine]]::new()
                $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })

                # Header rendering
                $headerText = if ([string]::IsNullOrWhiteSpace($cleanFilter)) { $Header } else { "$Header (Filtered)" }
                $null = $outputLines.Add([ColoredLine]@{ Text = $headerText; Color = $Global:TuiColors.Header })
                $null = $outputLines.Add([ColoredLine]@{ Text = $("=" * $headerText.Length); Color = $Global:TuiColors.Header })
                $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })

                # Real-time search prompt
                if ($searchMode) {
                    $null = $outputLines.Add([ColoredLine]@{ Text = "  [Search] query: $filterText"; Color = $Global:TuiColors.Search })
                    if ($filteredCount -gt 0) {
                        $null = $outputLines.Add([ColoredLine]@{ Text = "  [Suggest]: $($Items[$filteredIndices[0]])"; Color = $Global:TuiColors.Suggest })
                    } else {
                        $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })
                    }
                    $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })
                } elseif (-not [string]::IsNullOrWhiteSpace($cleanFilter)) {
                    $null = $outputLines.Add([ColoredLine]@{ Text = "  [Filter]: $cleanFilter (Press [Backspace] to clear, [/] to edit)"; Color = $Global:TuiColors.Search })
                    $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })
                }

                if ($filteredCount -eq 0) {
                    $null = $outputLines.Add([ColoredLine]@{ Text = "     [No items matching filter]"; Color = $Global:TuiColors.Alert })
                    $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })
                } else {
                    # Render window dynamically based on console height
                    $nonItemLines = 10
                    if ($searchMode) { $nonItemLines += 2 }
                    $maxVisible = [Math]::Min(12, [Math]::Max(3, ([Console]::WindowHeight - $nonItemLines)))
                    $start = 0
                    if ($currentIndex -ge $maxVisible) {
                        $start = $currentIndex - $maxVisible + 1
                    }
                    $end = [Math]::Min($start + $maxVisible - 1, $filteredCount - 1)
                    if ($end - $start + 1 -lt $maxVisible -and $filteredCount -gt $maxVisible) {
                        $start = [Math]::Max(0, $end - $maxVisible + 1)
                    }

                    if ($start -gt 0) {
                        $null = $outputLines.Add([ColoredLine]@{ Text = "     ^ ... ($start more above) ..."; Color = $Global:TuiColors.Footer })
                    }
                    for ($i = $start; $i -le $end; $i++) {
                        $actualIndex = $filteredIndices[$i]
                        $label = $Items[$actualIndex]
                        $maxLabelLen = $width - 6
                        if ($maxLabelLen -gt 3 -and $label.Length -gt $maxLabelLen) {
                            $label = $label.Substring(0, $maxLabelLen - 3) + "..."
                        }
                        if ($i -eq $currentIndex) {
                            $null = $outputLines.Add([ColoredLine]@{ Text = "  >  $label"; Color = $Global:TuiColors.Selected })
                        } else {
                            if ($label -match "future|support future") {
                                $null = $outputLines.Add([ColoredLine]@{ Text = "     $label"; Color = "DarkGray" })
                            } else {
                                $null = $outputLines.Add([ColoredLine]@{ Text = "     $label"; Color = $Global:TuiColors.Regular })
                            }
                        }
                    }
                    if ($end -lt $filteredCount - 1) {
                        $remaining = $filteredCount - 1 - $end
                        $null = $outputLines.Add([ColoredLine]@{ Text = "     v ... ($remaining more below) ..."; Color = $Global:TuiColors.Footer })
                    }
                    $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })
                }

                # Instruction footer
                $helpStr = if ($width -lt 70) { "Arrows/Enter: select, [/] search, [Esc] exit" } else { "Use Arrow Keys [Up/Down] to navigate, [Enter] to select, [/] to search, [Esc] to cancel." }
                if ($searchMode) {
                    $helpStr = if ($width -lt 70) { "Type: filter. [Enter] select, [Esc] exit" } else { "Type to filter list. [Enter] to select suggestion, [Esc] to exit search mode." }
                }
                $null = $outputLines.Add([ColoredLine]@{ Text = $helpStr; Color = $Global:TuiColors.Footer })
                # Write out compiled lines inline
                try {
                    [Console]::SetCursorPosition($startCol, $startRow)
                } catch {
                    Clear-Host
                    $startRow = 0
                    $startCol = 0
                    try { [Console]::SetCursorPosition($startCol, $startRow) } catch {}
                }
                foreach ($line in $outputLines) {
                    $txt = $line.Text
                    $padded = if ($txt.Length -lt $width) { $txt + (" " * ($width - $txt.Length)) } else { $txt.Substring(0, $width) }
                    Write-Host $padded -ForegroundColor $line.Color
                }

                # Clear remaining lines from previous cycles
                $diff = $lastLinesCount - $outputLines.Count
                if ($diff -gt 0) {
                    for ($d = 0; $d -lt $diff; $d++) {
                        Write-Host (" " * ([Console]::WindowWidth - 1))
                    }
                }
                $lastLinesCount = $outputLines.Count

                $key = $null
                try {
                    $key = [Console]::ReadKey($true)
                } catch {
                    [Console]::CursorVisible = $true
                    $choice = Read-Host "Select index [1-$filteredCount]"
                    $val = 0
                    if ([int]::TryParse($choice, [ref]$val) -and $val -ge 1 -and $val -le $filteredCount) {
                        return $filteredIndices[$val - 1]
                    }
                    return -1
                }

                if ($searchMode) {
                    if ($key.Key -eq [ConsoleKey]::Escape) {
                        $searchMode = $false
                        $currentIndex = 0
                    }
                    elseif ($key.Key -eq [ConsoleKey]::Enter) {
                        if ($filteredCount -gt 0) {
                            return $filteredIndices[$currentIndex]
                        }
                        $searchMode = $false
                    }
                    elseif ($key.Key -eq [ConsoleKey]::Backspace) {
                        if ($filterText.Length -gt 0) {
                            $filterText = $filterText.Substring(0, $filterText.Length - 1)
                        }
                        $currentIndex = 0
                    }
                    elseif ($key.Key -eq [ConsoleKey]::UpArrow) {
                        if ($filteredCount -gt 0) {
                            $origIdx = $currentIndex
                            do {
                                $currentIndex = ($currentIndex - 1 + $filteredCount) % $filteredCount
                            } while ($Items[$filteredIndices[$currentIndex]] -match "future|support future" -and $currentIndex -ne $origIdx)
                        }
                    }
                    elseif ($key.Key -eq [ConsoleKey]::DownArrow) {
                        if ($filteredCount -gt 0) {
                            $origIdx = $currentIndex
                            do {
                                $currentIndex = ($currentIndex + 1) % $filteredCount
                            } while ($Items[$filteredIndices[$currentIndex]] -match "future|support future" -and $currentIndex -ne $origIdx)
                        }
                    }
                    elseif ($key.KeyChar -ge 32 -and $key.KeyChar -le 126) {
                        $filterText += $key.KeyChar
                        $currentIndex = 0
                    }
                } else {
                    if ($key.Key -eq [ConsoleKey]::UpArrow) {
                        if ($filteredCount -gt 0) {
                            $origIdx = $currentIndex
                            do {
                                $currentIndex = ($currentIndex - 1 + $filteredCount) % $filteredCount
                            } while ($Items[$filteredIndices[$currentIndex]] -match "future|support future" -and $currentIndex -ne $origIdx)
                        }
                    }
                    elseif ($key.Key -eq [ConsoleKey]::DownArrow) {
                        if ($filteredCount -gt 0) {
                            $origIdx = $currentIndex
                            do {
                                $currentIndex = ($currentIndex + 1) % $filteredCount
                            } while ($Items[$filteredIndices[$currentIndex]] -match "future|support future" -and $currentIndex -ne $origIdx)
                        }
                    }
                    elseif ($key.Key -eq [ConsoleKey]::Enter) {
                        if ($filteredCount -gt 0) {
                            $actualIdx = $filteredIndices[$currentIndex]
                            if ($Items[$actualIdx] -match "future|support future") {
                                continue # Block selection of future items
                            }
                            $Global:TerminalMenuLastKey = "enter"
                            return $actualIdx
                        }
                        return -1
                    }
                    elseif ($Header -eq "Select Project" -and $key.KeyChar.ToString().ToLower() -in @('c', 'i', 'd', 't')) {
                        if ($filteredCount -gt 0) {
                            $actualIdx = $filteredIndices[$currentIndex]
                            if ($Items[$actualIdx] -match "future|support future") {
                                continue
                            }
                            $Global:TerminalMenuLastKey = $key.KeyChar.ToString().ToLower()
                            return $actualIdx
                        }
                    }
                    elseif ($key.KeyChar -ge 32 -and $key.KeyChar -le 126) {
                        $searchMode = $true
                        $filterText += $key.KeyChar
                        $currentIndex = 0
                    }
                    elseif ($key.Key -eq [ConsoleKey]::Backspace) {
                        $filterText = ""
                        $currentIndex = 0
                    }
                    elseif ($key.Key -eq [ConsoleKey]::Escape) {
                        if (-not [string]::IsNullOrWhiteSpace($filterText)) {
                            $filterText = ""
                            $currentIndex = 0
                        } else {
                            $Global:TerminalMenuLastKey = "escape"
                            return -1
                        }
                    }
                }
            }
        } finally {
            try {
                # Clear all menu lines from screen to restore prompt
                [Console]::SetCursorPosition($startCol, $startRow)
                for ($d = 0; $d -lt $lastLinesCount; $d++) {
                    Write-Host (" " * ([Console]::WindowWidth - 1))
                }
                [Console]::SetCursorPosition($startCol, $startRow)
                if ($null -ne $oldCursorVisible) {
                    [Console]::CursorVisible = $oldCursorVisible
                }
            } catch {}
        }
        return -1
    }

    static [MenuItem] ShowDynamic([string]$Header, [ScriptBlock]$Resolver, [int]$DefaultIndex, [string]$InitialFilter = "") {
        # Ensure TUI colors are initialized
        if ($null -eq $Global:TuiColors) { [TerminalMenu]::InitializeTuiColors() }

        $currentIndex = $DefaultIndex
        $filterText = ""
        $searchMode = $false
        $marqueeOffset = 0

        # Pre-scroll if drawing near the bottom to avoid repeated headers due to scroll shifts
        $maxVisible = [Math]::Min(12, [Math]::Max(3, ([Console]::WindowHeight - 10)))
        $estimatedLines = 7 + $maxVisible
        if ($searchMode) { $estimatedLines += 3 }
        
        $visibleCursorRow = [Console]::CursorTop - [Console]::WindowTop
        $remainingVisibleLines = [Console]::WindowHeight - $visibleCursorRow
        $spaceNeeded = $estimatedLines - $remainingVisibleLines
        if ($spaceNeeded -gt 0) {
            for ($i = 0; $i -lt $spaceNeeded; $i++) {
                Write-Host ""
            }
            $targetRow = [Math]::Max(0, [Console]::CursorTop - $spaceNeeded)
            try {
                [Console]::SetCursorPosition([Console]::CursorLeft, $targetRow)
            } catch {}
        }
        $startRow = [Console]::CursorTop
        $startCol = [Console]::CursorLeft
        $lastLinesCount = 0
        $width = 0

        # Hide cursor
        $oldCursorVisible = $null
        try {
            $oldCursorVisible = [Console]::CursorVisible
            [Console]::CursorVisible = $false
        } catch {}

        try {
            while ($true) {
                $width = [Console]::WindowWidth - 1
                # Resolve list of MenuItems dynamically based on filter text
                $cleanFilter = $filterText
                if ($filterText.StartsWith("/")) {
                    $cleanFilter = $filterText.Substring(1)
                }

                [MenuItem[]]$currentItems = $Resolver.Invoke($cleanFilter)
                $filteredCount = $currentItems.Count

                if ($filteredCount -gt 0) {
                    if ($currentIndex -ge $filteredCount) {
                        $currentIndex = 0
                    }
                } else {
                    $currentIndex = 0
                }

                # Compile colored output lines
                $outputLines = [System.Collections.Generic.List[ColoredLine]]::new()
                $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })

                # Header rendering
                $headerText = if ([string]::IsNullOrWhiteSpace($cleanFilter)) { $Header } else { "$Header (Filtered)" }
                $null = $outputLines.Add([ColoredLine]@{ Text = $headerText; Color = $Global:TuiColors.Header })
                $null = $outputLines.Add([ColoredLine]@{ Text = $("=" * $headerText.Length); Color = $Global:TuiColors.Header })
                $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })

                # Real-time search prompt
                if ($searchMode) {
                    $null = $outputLines.Add([ColoredLine]@{ Text = "  [Search] query: $filterText"; Color = $Global:TuiColors.Search })
                    if ($filteredCount -gt 0) {
                        $null = $outputLines.Add([ColoredLine]@{ Text = "  [Suggest]: $($currentItems[0].Label)"; Color = $Global:TuiColors.Suggest })
                    } else {
                        $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })
                    }
                    $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })
                } elseif (-not [string]::IsNullOrWhiteSpace($cleanFilter)) {
                    $null = $outputLines.Add([ColoredLine]@{ Text = "  [Filter]: $cleanFilter (Press [Backspace] to clear, [/] to edit)"; Color = $Global:TuiColors.Search })
                    $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })
                }

                if ($filteredCount -eq 0) {
                    $null = $outputLines.Add([ColoredLine]@{ Text = "     [No items matching filter]"; Color = $Global:TuiColors.Alert })
                    $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })
                    $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" }) # Spacing placeholder
                } else {
                    # Render window dynamically based on console height
                    $nonItemLines = 10
                    if ($searchMode) { $nonItemLines += 2 }
                    $maxVisible = [Math]::Min(12, [Math]::Max(3, ([Console]::WindowHeight - $nonItemLines)))
                    $start = 0
                    if ($currentIndex -ge $maxVisible) {
                        $start = $currentIndex - $maxVisible + 1
                    }
                    $end = [Math]::Min($start + $maxVisible - 1, $filteredCount - 1)
                    if ($end - $start + 1 -lt $maxVisible -and $filteredCount -gt $maxVisible) {
                        $start = [Math]::Max(0, $end - $maxVisible + 1)
                    }

                    if ($start -gt 0) {
                        $null = $outputLines.Add([ColoredLine]@{ Text = "     ^ ... ($start more above) ..."; Color = $Global:TuiColors.Footer })
                    }
                    for ($i = $start; $i -le $end; $i++) {
                        $actualItem = $currentItems[$i]
                        $label = $actualItem.Label
                        $maxLabelLen = $width - 6
                        if ($maxLabelLen -gt 3 -and $label.Length -gt $maxLabelLen) {
                            $label = $label.Substring(0, $maxLabelLen - 3) + "..."
                        }
                        if ($i -eq $currentIndex) {
                            $null = $outputLines.Add([ColoredLine]@{ Text = "  >  $label"; Color = $Global:TuiColors.Selected })
                        } else {
                            $null = $outputLines.Add([ColoredLine]@{ Text = "     $label"; Color = $Global:TuiColors.Regular })
                        }
                    }
                    if ($end -lt $filteredCount - 1) {
                        $remaining = $filteredCount - 1 - $end
                        $null = $outputLines.Add([ColoredLine]@{ Text = "     v ... ($remaining more below) ..."; Color = $Global:TuiColors.Footer })
                    }
                    $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })

                    # Render marquee scroll animation details if highlighted item is a Command
                    $highlightedItem = $currentItems[$currentIndex]
                    if ($highlightedItem.Type -eq "Command") {
                        $details = "Full Command: $($highlightedItem.Value.Command)"
                        $maxLen = $width - 10
                        if ($details.Length -gt $maxLen) {
                            $scrollText = $details + "   |   " + $details
                            $offset = $marqueeOffset % ($details.Length + 7)
                            $displayText = $scrollText.Substring($offset, $maxLen)
                            $null = $outputLines.Add([ColoredLine]@{ Text = "  [Cmd]: $displayText"; Color = $Global:TuiColors.Suggest })
                        } else {
                            $null = $outputLines.Add([ColoredLine]@{ Text = "  [Cmd]: $details"; Color = $Global:TuiColors.Suggest })
                        }
                    } else {
                        $null = $outputLines.Add([ColoredLine]@{ Text = ""; Color = "Gray" })
                    }
                }

                # Instruction footer
                $helpStr = if ($width -lt 70) { "Arrows/Enter: select, [/] search, [Esc] exit" } else { "Use Arrow Keys [Up/Down] to navigate, [Enter] to select, [/] to search, [Esc] to cancel." }
                if ($searchMode) {
                    $helpStr = if ($width -lt 70) { "Type: filter. [Enter] select, [Esc] exit" } else { "Type to filter list. [Enter] to select suggestion, [Esc] to exit search mode." }
                }
                $null = $outputLines.Add([ColoredLine]@{ Text = $helpStr; Color = $Global:TuiColors.Footer })
                # Write out compiled lines inline
                try {
                    [Console]::SetCursorPosition($startCol, $startRow)
                } catch {
                    Clear-Host
                    $startRow = 0
                    $startCol = 0
                    try { [Console]::SetCursorPosition($startCol, $startRow) } catch {}
                }
                foreach ($line in $outputLines) {
                    $txt = $line.Text
                    $padded = if ($txt.Length -lt $width) { $txt + (" " * ($width - $txt.Length)) } else { $txt.Substring(0, $width) }
                    Write-Host $padded -ForegroundColor $line.Color
                }

                # Clear remaining lines from previous cycles
                $diff = $lastLinesCount - $outputLines.Count
                if ($diff -gt 0) {
                    for ($d = 0; $d -lt $diff; $d++) {
                        Write-Host (" " * ([Console]::WindowWidth - 1))
                    }
                }
                $lastLinesCount = $outputLines.Count

                # Non-blocking read with 150ms sleep for horizontal marquee scroll animation
                $key = $null
                $scrollTimer = 0
                $keyAvailable = $false

                try {
                    while (-not [Console]::KeyAvailable) {
                        Start-Sleep -Milliseconds 150
                        $scrollTimer++
                        if ($scrollTimer -ge 4) {
                            # Trigger redraw to animate marquee
                            break
                        }
                    }
                    $keyAvailable = [Console]::KeyAvailable
                } catch {
                    # If KeyAvailable throws, we are likely in a non-interactive/redirected host. Exit gracefully.
                    return $null
                }

                if ($keyAvailable) {
                    try {
                        $key = [Console]::ReadKey($true)
                    } catch {
                        # If ReadKey throws, exit gracefully to prevent infinite rendering loop.
                        return $null
                    }
                    $marqueeOffset = 0 # Reset marquee on keypress
                } else {
                    $marqueeOffset++ # Increment marquee offset on timeout redraw
                    continue
                }

                if ($null -eq $key) {
                    continue
                }

                if ($searchMode) {
                    if ($key.Key -eq [ConsoleKey]::Escape) {
                        $searchMode = $false
                        $currentIndex = 0
                    }
                    elseif ($key.Key -eq [ConsoleKey]::Enter) {
                        if ($filteredCount -gt 0) {
                            return $currentItems[$currentIndex]
                        }
                        $searchMode = $false
                    }
                    elseif ($key.Key -eq [ConsoleKey]::Backspace) {
                        if ($filterText.Length -gt 0) {
                            $filterText = $filterText.Substring(0, $filterText.Length - 1)
                        }
                        $currentIndex = 0
                    }
                    elseif ($key.Key -eq [ConsoleKey]::UpArrow) {
                        if ($filteredCount -gt 0) {
                            $currentIndex = ($currentIndex - 1 + $filteredCount) % $filteredCount
                        }
                    }
                    elseif ($key.Key -eq [ConsoleKey]::DownArrow) {
                        if ($filteredCount -gt 0) {
                            $currentIndex = ($currentIndex + 1) % $filteredCount
                        }
                    }
                    elseif ($key.KeyChar -ge 32 -and $key.KeyChar -le 126) {
                        $filterText += $key.KeyChar
                        $currentIndex = 0
                    }
                } else {
                    if ($key.Key -eq [ConsoleKey]::UpArrow) {
                        if ($filteredCount -gt 0) {
                            $currentIndex = ($currentIndex - 1 + $filteredCount) % $filteredCount
                        }
                    }
                    elseif ($key.Key -eq [ConsoleKey]::DownArrow) {
                        if ($filteredCount -gt 0) {
                            $currentIndex = ($currentIndex + 1) % $filteredCount
                        }
                    }
                    elseif ($key.Key -eq [ConsoleKey]::Enter) {
                        if ($filteredCount -gt 0) {
                            return $currentItems[$currentIndex]
                        }
                        return $null
                    }
                    elseif ($key.KeyChar -eq '/') {
                        $searchMode = $true
                    }
                    elseif ($key.Key -eq [ConsoleKey]::Backspace) {
                        $filterText = ""
                        $currentIndex = 0
                    }
                    elseif ($key.Key -eq [ConsoleKey]::Escape) {
                        if (-not [string]::IsNullOrWhiteSpace($filterText)) {
                            $filterText = ""
                            $currentIndex = 0
                        } else {
                            return $null
                        }
                    }
                }
            }
        } finally {
            try {
                # Clear all menu lines from screen to restore prompt
                [Console]::SetCursorPosition($startCol, $startRow)
                for ($d = 0; $d -lt $lastLinesCount; $d++) {
                    Write-Host (" " * ([Console]::WindowWidth - 1))
                }
                [Console]::SetCursorPosition($startCol, $startRow)
                if ($null -ne $oldCursorVisible) {
                    [Console]::CursorVisible = $oldCursorVisible
                }
            } catch {}
        }
        return $null
    }

    static [void] ShowScrollableContent([string]$Title, [string[]]$Lines) {
        if ($null -eq $Global:TuiColors) { [TerminalMenu]::InitializeTuiColors() }

        $width = 80
        try {
            $width = [Console]::WindowWidth - 1
        } catch {}
        if ($width -lt 40) { $width = 80 }

        # Define encoding-safe Unicode characters
        $charLine = [char]0x2500     # ─
        $charAngle = [char]0x2514    # └
        $charUp = [char]0x2191       # ↑
        $charDown = [char]0x2193     # ↓
        $charBullet = [char]0x00B7   # ·

        # Max visible lines in the pager window
        $winHeight = 25
        try {
            $winHeight = [Console]::WindowHeight
        } catch {}
        $maxVisible = [Math]::Min(15, [Math]::Max(5, ($winHeight - 8)))
        
        $currentIndex = 0 # Top visible line index
        
        # Pre-scroll based on $maxVisible + headers + footers (about 6 lines total header/footer)
        $totalHeight = $maxVisible + 6
        
        $visibleCursorRow = 0
        $remainingVisibleLines = 25
        try {
            $visibleCursorRow = [Console]::CursorTop - [Console]::WindowTop
            $remainingVisibleLines = [Console]::WindowHeight - $visibleCursorRow
        } catch {}
        
        $spaceNeeded = $totalHeight - $remainingVisibleLines
        if ($spaceNeeded -gt 0) {
            for ($i = 0; $i -lt $spaceNeeded; $i++) {
                Write-Host ""
            }
            $targetRow = 0
            try {
                $targetRow = [Math]::Max(0, [Console]::CursorTop - $spaceNeeded)
                [Console]::SetCursorPosition([Console]::CursorLeft, $targetRow)
            } catch {}
        }
        
        $startRow = 0
        $startCol = 0
        try {
            $startRow = [Console]::CursorTop
            $startCol = [Console]::CursorLeft
        } catch {}

        # Hide cursor
        $oldCursorVisible = $null
        try {
            $oldCursorVisible = [Console]::CursorVisible
            [Console]::CursorVisible = $false
        } catch {}

        try {
            $running = $true
            while ($running) {
                # Move to start position
                try {
                    [Console]::SetCursorPosition($startCol, $startRow)
                } catch {}

                # Print Header
                Write-Host ([string]$charLine * $width) -ForegroundColor Gray
                Write-Host "$charAngle $Title" -ForegroundColor Cyan
                Write-Host ""

                # Print visible window of lines
                for ($i = 0; $i -lt $maxVisible; $i++) {
                    $lineIndex = $currentIndex + $i
                    if ($lineIndex -lt $Lines.Count) {
                        $l = $Lines[$lineIndex]
                        # Pad line to clear previous content
                        $padded = $l.PadRight($width).Substring(0, $width)
                        if ($padded -match '^\s*\*') {
                            Write-Host $padded -ForegroundColor Green
                        } else {
                            Write-Host $padded
                        }
                    } else {
                        Write-Host (" " * $width)
                    }
                }

                # Print Footer
                Write-Host ""
                Write-Host "  $charUp/$charDown Scroll $charBullet pgup/pgdown Page $charBullet ctrl+end Bottom $charBullet ctrl+home Top $charBullet esc Close" -ForegroundColor Gray
                Write-Host ([string]$charLine * $width) -ForegroundColor Gray

                # Wait for key input
                try {
                    $key = [Console]::ReadKey($true)
                } catch {
                    # Fallback if ReadKey is not supported
                    try {
                        Write-Host "  [Fallback] Press Enter to close..." -ForegroundColor Yellow
                        $null = Read-Host
                    } catch {
                        Start-Sleep -Seconds 1
                    }
                    $running = $false
                    break
                }

                switch ($key.Key) {
                    "DownArrow" {
                        if ($currentIndex + $maxVisible -lt $Lines.Count) {
                            $currentIndex++
                        }
                    }
                    "UpArrow" {
                        if ($currentIndex -gt 0) {
                            $currentIndex--
                        }
                    }
                    "PageDown" {
                        $currentIndex = [Math]::Min($Lines.Count - $maxVisible, $currentIndex + $maxVisible)
                        if ($currentIndex -lt 0) { $currentIndex = 0 }
                    }
                    "PageUp" {
                        $currentIndex = [Math]::Max(0, $currentIndex - $maxVisible)
                    }
                    "Home" {
                        $currentIndex = 0
                    }
                    "End" {
                        $currentIndex = [Math]::Max(0, $Lines.Count - $maxVisible)
                    }
                    "Escape" {
                        $running = $false
                    }
                    "Enter" {
                        $running = $false
                    }
                }
            }
        } finally {
            # Clean up / Erase from screen to restore console state
            $targetRow = $startRow
            for ($i = 0; $i -lt ($totalHeight); $i++) {
                try {
                    [Console]::SetCursorPosition($startCol, $targetRow)
                    Write-Host (" " * $width)
                } catch {}
                $targetRow++
            }
            try {
                [Console]::SetCursorPosition($startCol, $startRow)
                if ($null -ne $oldCursorVisible) {
                    [Console]::CursorVisible = $oldCursorVisible
                }
            } catch {}
        }
    }
}



