#region HELP AND DOCUMENTATION
# ------------------------------------------------------------------------------
#  Arrow-key navigable help menu for all custom profile commands.
# ------------------------------------------------------------------------------
<#
.SYNOPSIS
Browse custom profile commands with arrow-key menus.
.PARAMETER CategoryFilter
Optional. Show a specific category directly (e.g. 'Git', 'AI').
#>
function Get-CustomCommands {
    [CmdletBinding()]
    param([Parameter(Position=0)][string]$CategoryFilter)

    # --- Alias map: FunctionName -> alias ---
    $aliasFile = Join-Path (Split-Path $PSCommandPath -Parent) "10-Aliases.ps1"
    $aliasMap  = @{}
    if (Test-Path $aliasFile) {
        foreach ($line in (Get-Content $aliasFile)) {
            if ($line -match 'Set-Alias -Name\s+[''"]?([\w\?\.-]+)[''"]?\s+-Value\s+[''"]?([\w-]+)[''"]?') {
                $aliasMap[$Matches[2]] = $Matches[1]
            }
        }
    }

    # --- Synopsis from source (fast — avoids Get-Help overhead) ---
    # --- Build category map ---
    $fileCache = @{}   # file path -> string[] of lines
    $aliasFileName = Split-Path $aliasFile -Leaf
    $profileFunctions = Get-Command -CommandType Function | Where-Object {
        $_.ScriptBlock.File -like "$PSScriptRoot\*" -and
        (Split-Path $_.ScriptBlock.File -Leaf) -ne $aliasFileName -and
        $_.Name -notin @('Get-CustomCommands','Render-AIMenu','Render-ProjectMenu')
    }

    $commandsByCategory = @{}
    foreach ($func in $profileFunctions) {
        $file = $func.ScriptBlock.File
        $cat  = switch -Regex ($file) {
            'Nav|Sys|Help'  { 'Navigation & System' }
            'DotNet'        { '.NET' }
            'Git'           { 'Git' }
            'Docker'        { 'Docker' }
            'AWS'           { 'AWS' }
            'AI'            { 'AI Tools' }
            'Projects'      { 'Projects' }
            default         { 'Navigation & System' }
        }
        $alias = $aliasMap[$func.Name]
        $label = if ($alias) { "$alias" } else { $func.Name }

        # Synopsis: walk backwards from function definition line
        $desc = ""
        if ($file -and (Test-Path $file)) {
            if (-not $fileCache[$file]) { $fileCache[$file] = Get-Content $file }
            $lines = $fileCache[$file]
            $funcLine = -1
            for ($i = 0; $i -lt $lines.Count; $i++) {
                if ($lines[$i] -match "^\s*function\s+$([regex]::Escape($func.Name))\b") { $funcLine = $i; break }
            }
            if ($funcLine -gt 0) {
                for ($i = $funcLine - 1; $i -ge [Math]::Max(0, $funcLine - 15); $i--) {
                    if ($lines[$i] -match '^\s*\.SYNOPSIS\s*$') {
                        if (($i + 1) -lt $funcLine -and $lines[$i+1] -match '\S') {
                            $desc = $lines[$i+1].Trim()
                        }
                        break
                    }
                }
            }
        }
        if ($desc.Length -gt 55) { $desc = $desc.Substring(0,52) + '...' }

        if (-not $commandsByCategory[$cat]) { $commandsByCategory[$cat] = [System.Collections.Generic.List[object]]::new() }
        $commandsByCategory[$cat].Add([PSCustomObject]@{
            Label    = $label
            FullName = if ($alias) { "$alias › $($func.Name)" } else { $func.Name }
            Desc     = $desc
            Func     = $func.Name
        })
    }

    # --- Quick filter mode ---
    if ($CategoryFilter) {
        $cat = $commandsByCategory.Keys | Where-Object { $_ -like "*$CategoryFilter*" } | Select-Object -First 1
        if (-not $cat) { Write-Warning "No category matching '$CategoryFilter'"; return }
        Write-Host "  $cat" -ForegroundColor Cyan
        $pad = ($commandsByCategory[$cat] | ForEach-Object { $_.FullName.Length } | Measure-Object -Maximum).Maximum + 2
        foreach ($c in ($commandsByCategory[$cat] | Sort-Object Label)) {
            Write-Host ("  {0,-$pad}" -f $c.FullName) -NoNewline -ForegroundColor White
            Write-Host $c.Desc -ForegroundColor DarkGray
        }
        return
    }

    # --- Shared arrow-key picker ---
    # Returns selected index, or -1 on Esc
    function Invoke-ArrowMenu([string]$Header, [string[]]$Items, [string[]]$Hints) {
        $sel = 0
        [Console]::CursorVisible = $false
        Write-Host ""
        Write-Host "  $Header" -ForegroundColor DarkGray
        Write-Host "  (↑↓ move · Enter select · Esc back)" -ForegroundColor DarkGray
        Write-Host ""
        $top = [Console]::CursorTop

        function Draw {
            [Console]::SetCursorPosition(0, $top)
            for ($i = 0; $i -lt $Items.Count; $i++) {
                $hint = if ($Hints -and $Hints[$i]) { "  $($Hints[$i])" } else { "" }
                if ($i -eq $sel) {
                    Write-Host ("  ▶ {0,-30}{1}" -f $Items[$i], $hint) -ForegroundColor Cyan
                } else {
                    Write-Host ("    {0,-30}{1}" -f $Items[$i], $hint) -ForegroundColor DarkGray
                }
            }
        }
        Draw

        while ($true) {
            $k = [Console]::ReadKey($true)
            switch ($k.Key) {
                'UpArrow'   { if ($sel -gt 0)              { $sel-- }; Draw }
                'DownArrow' { if ($sel -lt $Items.Count-1) { $sel++ }; Draw }
                'Enter'     { [Console]::CursorVisible = $true; Write-Host ""; return $sel }
                'Escape'    { [Console]::CursorVisible = $true; Write-Host ""; return -1  }
            }
        }
    }

    # --- Category menu ---
    $categories = $commandsByCategory.Keys | Sort-Object
    while ($true) {
        Clear-Host
        Write-Host ""
        Write-Host "  ╔══════════════════════════════╗" -ForegroundColor Cyan
        Write-Host "  ║   Custom PowerShell Commands  ║" -ForegroundColor Cyan
        Write-Host "  ╚══════════════════════════════╝" -ForegroundColor Cyan
        $catLabels = $categories | ForEach-Object { $_ }
        $catHints  = $categories | ForEach-Object { "$($commandsByCategory[$_].Count) cmds" }

        $catIdx = Invoke-ArrowMenu "Select category" $catLabels $catHints
        if ($catIdx -lt 0) { return }

        # --- Command menu ---
        $selectedCat = $categories[$catIdx]
        $cmds = $commandsByCategory[$selectedCat] | Sort-Object Label

        while ($true) {
            Clear-Host
            Write-Host ""
            Write-Host "  ╔══════════════════════════════╗" -ForegroundColor Cyan
            Write-Host ("  ║  {0,-28}║" -f $selectedCat) -ForegroundColor Cyan
            Write-Host "  ╚══════════════════════════════╝" -ForegroundColor Cyan
            $cmdLabels = $cmds | ForEach-Object { $_.FullName }
            $cmdHints  = $cmds | ForEach-Object { $_.Desc }

            $cmdIdx = Invoke-ArrowMenu "Select command" $cmdLabels $cmdHints
            if ($cmdIdx -lt 0) { break }   # Esc → back to category menu

            # Run selected command
            $target = $cmds[$cmdIdx]
            Clear-Host
            Write-Host "  Running: $($target.Label)" -ForegroundColor Green
            Write-Host ""
            try { Invoke-Expression $target.Label }
            catch { Write-Error "Failed: $_" }
            Write-Host ""
            Write-Host "  [any key to return]" -ForegroundColor DarkGray
            $null = [Console]::ReadKey($true)
        }
    }
}

#endregion
