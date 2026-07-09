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

            $themePath = Join-Path -Path $themesPath -ChildPath "$selectedTheme.omp.json"
            if (Test-Path $themePath) {
                $Global:NewThemeToApply = $themePath
                Write-Host "[Theme] Oh My Posh theme switched to '$selectedTheme' (Persistent)." -ForegroundColor Green
            }
        }
    }


    static [void] ToggleMobileMode() {
        $themesPath = Join-Path -Path $env:USERPROFILE -ChildPath "Documents\PowerShell\asset\powershell-themes"
        $mobileFlagFile = Join-Path -Path $themesPath -ChildPath "mobile_mode_active.txt"
        
        $isMobile = $false
        if (Test-Path $mobileFlagFile) {
            $isMobile = (Get-Content $mobileFlagFile -Raw | Out-String).Trim() -eq "true"
        }
        
        $newMobileState = -not $isMobile
        Set-Content -Path $mobileFlagFile -Value ($newMobileState.ToString().ToLower()) -Force
        
        # Decide which theme file to load
        $themeName = if ($newMobileState) { "neko-mobile" } else { "neko" }
        
        # Persist standard active theme
        $activeThemeFile = Join-Path -Path $themesPath -ChildPath "active_theme.txt"
        Set-Content -Path $activeThemeFile -Value $themeName -Force
        $env:THEME = $themeName
        
        $themePath = Join-Path -Path $themesPath -ChildPath "$themeName.omp.json"
        if (Test-Path $themePath) {
            $Global:NewThemeToApply = $themePath
            if ($newMobileState) {
                Write-Host "[Theme] Mobile Prompt Theme activated (ASCII mode, stacked)." -ForegroundColor Cyan
            } else {
                Write-Host "[Theme] Desktop Prompt Theme activated (Rich Unicode/Emoji mode)." -ForegroundColor Green
            }
        }
    }
}
#endregion



