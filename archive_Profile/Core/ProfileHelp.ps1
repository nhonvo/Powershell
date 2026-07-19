#region PROFILE HELP
# ==============================================================================
#  Exposes interactive help documentation for all custom commands.
# ==============================================================================

class CommandDoc {
    [string]$Alias
    [string]$FullName
    [string]$Desc
    [string]$Command
}

class ProfileHelp {
    static [System.Collections.Generic.Dictionary[string, System.Collections.Generic.Dictionary[string, CommandDoc[]]]] GetCommands() {
        $dict = [System.Collections.Generic.Dictionary[string, System.Collections.Generic.Dictionary[string, CommandDoc[]]]]::new()

        $jsonPath = Join-Path $PSScriptRoot "CommandsMenu.json"
        if (Test-Path $jsonPath) {
            $rawJson = Get-Content -Raw -Path $jsonPath
            $rawDict = $rawJson | ConvertFrom-Json

            foreach ($parentCat in $rawDict.psobject.Properties.Name) {
                $subDict = [System.Collections.Generic.Dictionary[string, CommandDoc[]]]::new()
                $rawSubObj = $rawDict.$parentCat

                foreach ($subCat in $rawSubObj.psobject.Properties.Name) {
                    $cmdList = [System.Collections.Generic.List[CommandDoc]]::new()
                    foreach ($rawDoc in $rawSubObj.$subCat) {
                        $doc = [CommandDoc]@{
                            Alias    = $rawDoc.Alias
                            FullName = $rawDoc.FullName
                            Desc     = $rawDoc.Desc
                            Command  = $rawDoc.Command
                        }
                        $null = $cmdList.Add($doc)
                    }
                    $subDict[$subCat] = $cmdList.ToArray()
                }
                $dict[$parentCat] = $subDict
            }
        }

        return $dict
    }

    static [void] Show([string]$CategoryFilter) {
        $cmdsNested = [ProfileHelp]::GetCommands()
        $cmds = [System.Collections.Generic.Dictionary[string, CommandDoc[]]]::new()
        foreach ($parent in $cmdsNested.Keys) {
            foreach ($sub in $cmdsNested[$parent].Keys) {
                $cmds[$sub] = $cmdsNested[$parent][$sub]
            }
        }

        # Dynamic TUI category menu using global TerminalMenu
        $categories = [string[]]($cmds.Keys | Sort-Object)

        # Pre-build flat list of all commands for global search matching
        $allCommands = @()
        foreach ($cat in $categories) {
            foreach ($cmd in $cmds[$cat]) {
                $allCommands += $cmd
            }
        }

        $resolver = {
            param([string]$filterText)
            $results = [System.Collections.Generic.List[MenuItem]]::new()

            if ([string]::IsNullOrWhiteSpace($filterText)) {
                # Show top-level categories
                foreach ($cat in $categories) {
                    $item = [MenuItem]@{
                        Label = "$cat ($($cmds[$cat].Count) commands)"
                        Type = "Category"
                        Value = $cat
                    }
                    $null = $results.Add($item)
                }
            } else {
                # Perform global flat child command search
                foreach ($cmd in $allCommands) {
                    if ($cmd.Alias -like "*$filterText*" -or $cmd.Desc -like "*$filterText*" -or $cmd.Command -like "*$filterText*") {
                        $item = [MenuItem]@{
                            Label = "{0,-10} - {1}" -f $cmd.Alias, $cmd.Desc
                            Type = "Command"
                            Value = $cmd
                        }
                        $null = $results.Add($item)
                    }
                }
            }
            return $results.ToArray()
        }

        while ($true) {
            $selected = [TerminalMenu]::ShowDynamic("Select Help Category", $resolver, 0, $CategoryFilter)
            if ($null -eq $selected) { return }

            # Reset the filter once a selection is made or on loop return so it doesn't get locked
            $CategoryFilter = ""

            if ($selected.Type -eq "Command") {
                # Select command from global search result and run directly
                $cmdObj = $selected.Value
                Clear-Host
                Write-Host ""
                Write-Host "  Running: $($cmdObj.Alias) ($($cmdObj.Command))" -ForegroundColor Green
                Write-Host ""

                try {
                    Invoke-Expression $cmdObj.Alias | Out-Host
                } catch {
                    Write-Error "Execution failed: $_"
                }

                Write-Host ""
                Write-Host "  [Press any key to return to help menu]" -ForegroundColor DarkGray
                $null = [Console]::ReadKey($true)
            }
            elseif ($selected.Type -eq "Category") {
                $catName = $selected.Value
                $catCmds = $cmds[$catName]

                while ($true) {
                    $subResolver = {
                        param([string]$subFilter)
                        $subResults = [System.Collections.Generic.List[MenuItem]]::new()
                        foreach ($cmd in $catCmds) {
                            if ([string]::IsNullOrWhiteSpace($subFilter) -or $cmd.Alias -like "*$subFilter*" -or $cmd.Desc -like "*$subFilter*" -or $cmd.Command -like "*$subFilter*") {
                                $item = [MenuItem]@{
                                    Label = "{0,-10} - {1}" -f $cmd.Alias, $cmd.Desc
                                    Type = "Command"
                                    Value = $cmd
                                }
                                $null = $subResults.Add($item)
                            }
                        }
                        return $subResults.ToArray()
                    }

                    $selectedSub = [TerminalMenu]::ShowDynamic("Category: $catName", $subResolver, 0)
                    if ($null -eq $selectedSub) { break }

                    $cmdObj = $selectedSub.Value
                    Clear-Host
                    Write-Host ""
                    Write-Host "  Running: $($cmdObj.Alias) ($($cmdObj.Command))" -ForegroundColor Green
                    Write-Host ""

                    try {
                        Invoke-Expression $cmdObj.Alias | Out-Host
                    } catch {
                        Write-Error "Execution failed: $_"
                    }

                    Write-Host ""
                    Write-Host "  [Press any key to return to category menu]" -ForegroundColor DarkGray
                    $null = [Console]::ReadKey($true)
                }
            }
        }
    }
}
#endregion



