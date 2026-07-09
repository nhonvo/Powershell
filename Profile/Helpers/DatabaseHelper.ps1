#region DATABASE HELPER
# ==============================================================================
#  SQLite database schema and TUI table inspector.
# ==============================================================================

class DatabaseHelper {
    static [void] DbTui([string]$DbPath) {
        if ([string]::IsNullOrWhiteSpace($DbPath)) {
            Write-Error "Please specify a path to a SQLite database."
            return
        }
        $fullPath = Resolve-Path $DbPath -ErrorAction SilentlyContinue
        if (-not $fullPath -or -not (Test-Path $fullPath.Path)) {
            Write-Error "Database file not found: $DbPath"
            return
        }
        $db = $fullPath.Path

        $sqliteCmd = Get-Command sqlite3 -ErrorAction SilentlyContinue
        if (-not $sqliteCmd) {
            Write-Error "sqlite3 CLI is not installed or not in PATH."
            return
        }

        $tablesRaw = & sqlite3 $db ".tables" 2>$null
        $tables = @()
        if ($tablesRaw) {
            foreach ($line in $tablesRaw) {
                $parts = $line -split '\s+'
                foreach ($p in $parts) {
                    if (-not [string]::IsNullOrWhiteSpace($p)) {
                        $tables += $p.Trim()
                    }
                }
            }
        }

        if ($Global:AiMode) {
            Write-Host "SQLite Database: $db"
            Write-Host "Tables:"
            foreach ($t in $tables) {
                Write-Host "  * $t"
                $schema = & sqlite3 $db ".schema $t" 2>$null
                foreach ($s in $schema) {
                    Write-Host "    $s"
                }
            }
            return
        }

        if ($tables.Count -eq 0) {
            Write-Host "No tables found in database." -ForegroundColor Yellow
            return
        }

        while ($true) {
            $menuItems = @()
            foreach ($t in $tables) {
                $menuItems += "Table: $t"
            }
            $menuItems += "[x] Close Viewer"

            $selected = ([type]"TerminalMenu")::Show("SQLite Database Viewer: $(Split-Path $db -Leaf)", $menuItems, 0)
            if ($selected -lt 0 -or $selected -eq ($menuItems.Count - 1)) {
                break
            }

            $table = $tables[$selected]
            $schemaLines = & sqlite3 $db ".schema $table" 2>$null
            $rowsLines = & sqlite3 $db -header -column "SELECT * FROM $table LIMIT 10" 2>$null
            
            $viewerLines = @()
            $viewerLines += "=== SCHEMA ==="
            foreach ($s in $schemaLines) { $viewerLines += $s }
            $viewerLines += ""
            $viewerLines += "=== TOP 10 ROWS ==="
            foreach ($r in $rowsLines) { $viewerLines += $r }

            ([type]"TerminalMenu")::ShowScrollableContent("Table: $table", $viewerLines)
        }
    }
}
#endregion
