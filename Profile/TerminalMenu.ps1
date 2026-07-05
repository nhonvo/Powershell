#region TERMINAL MENU UTILITY
# ==============================================================================
#  Strongly-typed terminal console selection menu with arrow-key navigation.
# ==============================================================================

class TerminalMenu {
    static [int] Show([string]$Header, [string[]]$Items, [int]$DefaultIndex) {
        $currentIndex = $DefaultIndex
        $count = $Items.Count

        if ($count -eq 0) { return -1 }

        # Hide cursor
        $oldCursorVisible = $null
        try {
            $oldCursorVisible = [Console]::CursorVisible
            [Console]::CursorVisible = $false
        } catch {}

        try {
            while ($true) {
                Clear-Host
                Write-Host ""
                Write-Host $Header -ForegroundColor Cyan
                Write-Host ("=" * $Header.Length) -ForegroundColor Cyan
                Write-Host ""

                for ($i = 0; $i -lt $count; $i++) {
                    if ($i -eq $currentIndex) {
                        Write-Host "  ▶  $($Items[$i])" -ForegroundColor Green
                    } else {
                        Write-Host "     $($Items[$i])" -ForegroundColor Gray
                    }
                }
                Write-Host ""
                Write-Host "Use Arrow Keys [↑/↓] to navigate, [Enter] to select, [Esc] to cancel." -ForegroundColor DarkGray

                $key = $null
                try {
                    $key = [Console]::ReadKey($true)
                } catch {
                    # Non-interactive fallback: ask for numeric input
                    [Console]::CursorVisible = $true
                    $choice = Read-Host "Select index [1-$count]"
                    $val = 0
                    if ([int]::TryParse($choice, [ref]$val) -and $val -ge 1 -and $val -le $count) {
                        return ($val - 1)
                    }
                    return -1
                }

                if ($key.Key -eq [ConsoleKey]::UpArrow) {
                    $currentIndex = ($currentIndex - 1 + $count) % $count
                }
                elseif ($key.Key -eq [ConsoleKey]::DownArrow) {
                    $currentIndex = ($currentIndex + 1) % $count
                }
                elseif ($key.Key -eq [ConsoleKey]::Enter) {
                    return $currentIndex
                }
                elseif ($key.Key -eq [ConsoleKey]::Escape) {
                    return -1
                }
            }
        } finally {
            try {
                if ($null -ne $oldCursorVisible) {
                    [Console]::CursorVisible = $oldCursorVisible
                }
            } catch {}
        }
        return -1
    }
}
#endregion
