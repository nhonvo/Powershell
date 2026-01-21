#region HELP AND DOCUMENTATION
# ------------------------------------------------------------------------------
#  Command to display a summary of all custom functions and aliases.
# ------------------------------------------------------------------------------
<#
.SYNOPSIS
Displays a categorized list of all custom commands available in the profile.
.PARAMETER CategoryFilter
Optional. If provided (e.g., 'Git', 'Net'), skips the menu and directly shows that category's commands.
#>
function Get-CustomCommands {
    [CmdletBinding()]
    param(
        [Parameter(Position=0)]
        [string]$CategoryFilter
    )

    # --- 1. SCANNING PHASE ---
    if (-not $CategoryFilter) {
        Write-Host "🔄 Scanning commands..." -ForegroundColor Cyan
    }
    
    # 1. Map Aliases: Alias -> FunctionName
    # Regex fix: [\w\?-]+ allows hyphens in alias names (e.g. sln-add)
    $aliasFile = "$PSScriptRoot\10-Aliases.ps1"
    $aliasMap = @{}
    if (Test-Path $aliasFile) {
        $lines = Get-Content $aliasFile
        foreach ($line in $lines) {
            if ($line -match 'Set-Alias -Name\s+[''"]?([\w\?\.-]+)[''"]?\s+-Value\s+[''"]?([\w-]+)[''"]?') {
                $aliasMap[$Matches[2]] = $Matches[1] 
            }
        }
    }

    # Helper to get description directly from the .SYNOPSIS block
    function Get-Synopsis {
        param($ScriptBlock)
        $text = $ScriptBlock.ToString()
        if ($text -match '(?ms)\.SYNOPSIS\s*\r?\n\s*(.+?)\s*(\.|#>)') { 
            return $Matches[1].Trim() 
        }
        return "Custom Command"
    }

    # Scan Functions
    $profileFunctions = Get-Command -CommandType Function | Where-Object { 
        $_.ScriptBlock.File -like "$PSScriptRoot\*" -and $_.Name -ne "Get-CustomCommands"
    }

    $commandsByCategory = @{}
    $total = $profileFunctions.Count
    $current = 0

    foreach ($func in $profileFunctions) {
        if (-not $CategoryFilter) {
            $current++
            $p = [int](($current / $total) * 100)
            Write-Progress -Activity "Building Help Menu" -Status "Analyzing $($func.Name)..." -PercentComplete $p
        }

        $file = $func.ScriptBlock.File
        $category = if ($file -match "Nav") { "System & Navigation" }
                    elseif ($file -match "Sys") { "System & Navigation" }
                    elseif ($file -match "DotNet") { ".NET Development" }
                    elseif ($file -match "Git") { "Git Ops" }
                    elseif ($file -match "Docker") { "Docker Containers" }
                    elseif ($file -match "AWS") { "AWS LocalStack" }
                    elseif ($file -match "AI") { "AI Tools" }
                    else { "Other" }

        # Reliable Synopsis extraction using Get-Help
        $helpInfo = Get-Help $func.Name -ErrorAction SilentlyContinue
        $desc = if ($helpInfo.Synopsis) { $helpInfo.Synopsis.Trim() } else { "Custom Command" }
        
        $alias = $aliasMap[$func.Name]
        
        # Display Name Logic
        $entryName = if ($alias) { "$alias" } else { $func.Name }
        $fullName = if ($alias) { "$alias ($($func.Name))" } else { $func.Name }

        $entry = @{ 
            DisplayName = $fullName; 
            ShortName = $entryName;
            Description = $desc; 
            Function = $func.Name 
        }

        if (-not $commandsByCategory[$category]) { $commandsByCategory[$category] = @() }
        $commandsByCategory[$category] += $entry
    }
    if (-not $CategoryFilter) {
        Write-Progress -Activity "Building Help Menu" -Completed
    }

    # Add Winget (Special Case - Manual Entry)
    if (-not $commandsByCategory["System & Navigation"]) { $commandsByCategory["System & Navigation"] = @() }
    $commandsByCategory["System & Navigation"] += @{ DisplayName = "wsearch, winstall"; ShortName = "wsearch"; Description = "Winget Package Manager Shortcuts"; Function = "wsearch" }

    # --- DIRECT CATEGORY MODE (Quick Filter) ---
    if ($CategoryFilter) {
        $foundCat = $commandsByCategory.Keys | Where-Object { $_ -like "*$CategoryFilter*" } | Select-Object -First 1
        
        if ($foundCat) {
            Write-Host "::: $foundCat :::" -ForegroundColor Yellow -BackgroundColor DarkBlue
            $cmds = $commandsByCategory[$foundCat] | Sort-Object DisplayName
            
            # Align
            $maxLength = 0
            foreach ($c in $cmds) { if ($c.DisplayName.Length -gt $maxLength) { $maxLength = $c.DisplayName.Length } }
            $padLimit = $maxLength + 2

            foreach ($cmd in $cmds) {
                Write-Host "  $($cmd.DisplayName)" -NoNewline -ForegroundColor White
                $pad = $padLimit - $cmd.DisplayName.Length
                if ($pad -lt 1) { $pad = 1 }
                Write-Host (" " * $pad) -NoNewline
                Write-Host "- $($cmd.Description)" -ForegroundColor Gray
            }
            return # Exit after showing without interactivity
        } else {
            Write-Warning "No category found matching '$CategoryFilter'."
            return
        }
    }

    # Map Categories to Shortcuts
    $catShortcuts = @{
        "System & Navigation" = "csys"
        ".NET Development"    = "cnet"
        "Git Ops"            = "cg"
        "Docker Containers"   = "cdk"
        "AWS LocalStack"      = "caws"
        "AI Tools"           = "cai"
    }

    # --- INTERACTIVE MENU LOOP ---
    while ($true) {
        Clear-Host
        Write-Host "╔═════════════════════════════════════╗" -ForegroundColor Cyan
        Write-Host "║      Custom PowerShell Commands     ║" -ForegroundColor Cyan
        Write-Host "╚═════════════════════════════════════╝" -ForegroundColor Cyan
        Write-Host ""
        
        # Display Categories
        $categories = $commandsByCategory.Keys | Sort-Object
        $i = 1
        foreach ($cat in $categories) {
            $shortcut = $catShortcuts[$cat]
            $suffix = if ($shortcut) { "($shortcut)" } else { "" }
            Write-Host "  $i. $cat $suffix" -ForegroundColor Yellow
            $i++
        }
        Write-Host "  A. Show All (List Only)" -ForegroundColor Gray
        Write-Host "  Q. Quit" -ForegroundColor Gray
        Write-Host ""

        $selection = Read-Host "Select a category (1-$($categories.Count))"
        
        if ($selection -match "^[qQ]$") { break }
        
        # --- SHOW ALL MODE ---
        if ($selection -match "^[aA]$") { 
             Clear-Host
             foreach ($cat in $categories) {
                Write-Host "::: $cat :::" -ForegroundColor Yellow -BackgroundColor DarkBlue
                $cmds = $commandsByCategory[$cat] | Sort-Object DisplayName
                
                # Calculate Max Length for Alignment in this category
                $maxLength = 0
                foreach ($c in $cmds) { if ($c.DisplayName.Length -gt $maxLength) { $maxLength = $c.DisplayName.Length } }
                $padLimit = $maxLength + 2

                foreach ($cmd in $cmds) {
                    Write-Host "  $($cmd.DisplayName)" -NoNewline -ForegroundColor White
                    $pad = $padLimit - $cmd.DisplayName.Length
                    if ($pad -lt 1) { $pad = 1 }
                    Write-Host (" " * $pad) -NoNewline
                    Write-Host "- $($cmd.Description)" -ForegroundColor Gray
                }
                Write-Host ""
            }
            Write-Host "Press any key to return..." -ForegroundColor DarkGray
            $null = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
            continue
        }

        # --- CATEGORY SELECTION MODE ---
        if ($selection -match "^\d+$" -and [int]$selection -le $categories.Count -and [int]$selection -gt 0) {
            $selectedCat = $categories[[int]$selection - 1]
            
            while ($true) {
                Clear-Host
                Write-Host "::: $selectedCat :::" -ForegroundColor Yellow -BackgroundColor DarkBlue
                Write-Host "(Select a number to RUN & EXIT, or 0 to go back)" -ForegroundColor DarkGray
                Write-Host ""

                $cmds = $commandsByCategory[$selectedCat] | Sort-Object DisplayName
                
                # Calculate Max Length for Alignment
                $maxLength = 0
                foreach ($c in $cmds) { if ($c.DisplayName.Length -gt $maxLength) { $maxLength = $c.DisplayName.Length } }
                $padLimit = $maxLength + 2

                $j = 1
                foreach ($cmd in $cmds) {
                    # Print Item:  1. alias (Func)     - Description
                    $numStr = "{0,2}. " -f $j
                    Write-Host $numStr -NoNewline -ForegroundColor Green
                    
                    Write-Host "$($cmd.DisplayName)" -NoNewline -ForegroundColor White
                    $pad = $padLimit - $cmd.DisplayName.Length
                    if ($pad -lt 1) { $pad = 1 }
                    Write-Host (" " * $pad) -NoNewline
                    Write-Host "- $($cmd.Description)" -ForegroundColor Gray
                    $j++
                }
                Write-Host ""
                $cmdSelection = Read-Host "Run Command #"

                if ($cmdSelection -eq '0' -or [string]::IsNullOrWhiteSpace($cmdSelection)) { break }

                if ($cmdSelection -match "^\d+$" -and [int]$cmdSelection -le $cmds.Count -and [int]$cmdSelection -gt 0) {
                    $targetCmd = $cmds[[int]$cmdSelection - 1]
                    
                    # EXECUTION LOGIC
                    
                    # 1. Clear Host to start fresh for the command output
                    Clear-Host
                    
                    Write-Host "🚀 Running: $($targetCmd.ShortName)" -ForegroundColor Green
                    
                    try {
                        $exe = if ($targetCmd.ShortName) { $targetCmd.ShortName } else { $targetCmd.Function }
                        Invoke-Expression $exe
                    } catch {
                        Write-Error "Failed to run command: $_"
                    }

                    # Pause to verify output, then cycle back to menu
                    Write-Host "`n[Press Enter to return to menu]" -ForegroundColor DarkGray
                    Read-Host
                }
            }
        }
    }
}

#endregion