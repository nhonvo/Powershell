#region SHELL THEME SWITCHER UTILITY
# ==============================================================================
#  Dynamic Oh My Posh style switcher using the existing TerminalMenu TUI select component.
# ==============================================================================

class ThemeHelper {
    static [void] SelectThemeInteractive() {
        $themesPath = Join-Path -Path $env:USERPROFILE -ChildPath "Documents\PowerShell\asset\powershell-themes"
        if (-not (Test-Path $themesPath)) {
            Write-Error "Themes directory not found: $themesPath"
            return
        }

        $files = Get-ChildItem -Path $themesPath -Filter "*.omp.json" | Sort-Object Name
        if ($files.Count -eq 0) {
            Write-Error "No Oh My Posh themes (.omp.json) found in $themesPath."
            return
        }

        $themeNames = [System.Collections.Generic.List[string]]::new()
        foreach ($file in $files) {
            $name = $file.Name -replace "\.omp\.json$", ""
            $null = $themeNames.Add($name)
        }

        $currentTheme = $env:THEME
        $defaultIndex = $themeNames.IndexOf($currentTheme)
        if ($defaultIndex -lt 0) { $defaultIndex = 0 }

        $selectedIndex = [TerminalMenu]::Show("Select Oh My Posh Theme", $themeNames.ToArray(), $defaultIndex)
        if ($selectedIndex -ge 0) {
            $selectedTheme = $themeNames[$selectedIndex]
            $env:THEME = $selectedTheme

            # Persist selection
            $activeThemeFile = Join-Path -Path $themesPath -ChildPath "active_theme.txt"
            Set-Content -Path $activeThemeFile -Value $selectedTheme -Force

            # Re-initialize current session
            $themePath = Join-Path -Path $themesPath -ChildPath "$selectedTheme.omp.json"
            if (Test-Path $themePath) {
                oh-my-posh init pwsh --config $themePath | Invoke-Expression
                Write-Host "🟢 Oh My Posh theme switched to '$selectedTheme' (Persistent)." -ForegroundColor Green
                [TerminalMenu]::InitializeTuiColors()
            }
        }
    }
}
#endregion
