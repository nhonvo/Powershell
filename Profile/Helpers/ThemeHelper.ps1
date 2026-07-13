#region SHELL THEME SWITCHER UTILITY
# ==============================================================================
#  Dynamic Oh My Posh style switcher using the existing TerminalMenu TUI select component.
# ==============================================================================

class ThemeHelper {
    static [void] SelectThemeInteractive() {
        $themesPath = $env:POSH_THEMES_PATH
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
        $displayLabels = [System.Collections.Generic.List[string]]::new()
        
        foreach ($file in $files) {
            $name = $file.Name -replace "\.omp\.json$", ""
            $null = $themeNames.Add($name)
            
            # Extract segment visual previews
            $preview = ""
            try {
                $json = Get-Content $file.FullName -Raw | ConvertFrom-Json
                $segs = @()
                if ($json.blocks) {
                    foreach ($block in $json.blocks) {
                        if ($block.segments) {
                            $segs += $block.segments
                        }
                    }
                }
                
                # Render color squares & segment types for the first 3 segments
                $previewParts = @()
                for ($i = 0; $i -lt [Math]::Min(3, $segs.Count); $i++) {
                    $seg = $segs[$i]
                    $bg = if ($seg.background) { $seg.background } else { $seg.foreground }
                    $type = $seg.type
                    
                    # Map hex to emoji color block
                    $emoji = "🔵"
                    if ($bg -match '^#?([0-9a-fA-F]{6})') {
                        $clean = $Matches[1]
                        $r = [System.Convert]::ToInt32($clean.Substring(0, 2), 16)
                        $g = [System.Convert]::ToInt32($clean.Substring(2, 2), 16)
                        $b = [System.Convert]::ToInt32($clean.Substring(4, 2), 16)
                        
                        $max = [Math]::Max($r, [Math]::Max($g, $b))
                        $min = [Math]::Min($r, [Math]::Min($g, $b))
                        if (($max - $min) -lt 30) {
                            $emoji = if ($max -lt 64) { "⚫" } elseif ($max -gt 192) { "⚪" } else { "🔘" }
                        } elseif ($r -gt $g -and $r -gt $b) {
                            $emoji = if (($g - $b) -gt 40) { "🟠" } else { "🔴" }
                        } elseif ($g -gt $r -and $g -gt $b) {
                            $emoji = "🟢"
                        } elseif ($b -gt $r -and $b -gt $g) {
                            $emoji = if (($r - $g) -gt 40) { "🟣" } else { "🔵" }
                        } elseif ($r -gt $b -and $g -gt $b) {
                            $emoji = if ([Math]::Abs($r - $g) -lt 40) { "🟡" } else { "🟠" }
                        }
                    }
                    $previewParts += "$emoji $type"
                }
                if ($previewParts.Count -gt 0) {
                    $preview = $previewParts -join "  "
                }
            } catch {}
            
            $namePadded = $name.PadRight(25)
            $null = $displayLabels.Add("$namePadded │ $preview")
        }

        $currentTheme = $env:THEME
        $defaultIndex = $themeNames.IndexOf($currentTheme)
        if ($defaultIndex -lt 0) { $defaultIndex = 0 }

        $selectedIndex = [TerminalMenu]::Show("Select Oh My Posh Theme (Color segment preview)", $displayLabels.ToArray(), $defaultIndex)
        if ($selectedIndex -ge 0) {
            $selectedTheme = $themeNames[$selectedIndex]
            $env:THEME = $selectedTheme

            # Centralized JSON Config
            $configPath = Join-Path -Path $themesPath -ChildPath "config.json"
            $cfg = @{ active_theme = $selectedTheme; enable_mobile = ($selectedTheme -like "*-mobile") }
            $cfg | ConvertTo-Json | Out-File -FilePath $configPath -Force -Encoding utf8

            # Cleanup legacy files
            Remove-Item -Path (Join-Path -Path $themesPath -ChildPath "active_theme.txt") -Force -ErrorAction SilentlyContinue
            Remove-Item -Path (Join-Path -Path $themesPath -ChildPath "mobile_mode_active.txt") -Force -ErrorAction SilentlyContinue

            $themePath = Join-Path -Path $themesPath -ChildPath "$selectedTheme.omp.json"
            if (Test-Path $themePath) {
                $Global:NewThemeToApply = $themePath
                [TerminalMenu]::InitializeTuiColors()
                Write-Host "[Theme] Oh My Posh theme switched to '$selectedTheme' (Persistent)." -ForegroundColor Green
            }
        }
    }

    static [void] ToggleMobileMode() {
        $themesPath = $env:POSH_THEMES_PATH
        if (-not (Test-Path $themesPath)) { return }
        
        $configPath = Join-Path -Path $themesPath -ChildPath "config.json"
        $isMobile = $false
        $currentTheme = "neko"
        if (Test-Path $configPath) {
            try {
                $json = Get-Content $configPath -Raw | ConvertFrom-Json
                $isMobile = if ($null -ne $json.enable_mobile) { [bool]$json.enable_mobile } else { $false }
                if ($json.active_theme) { $currentTheme = $json.active_theme }
            } catch {}
        }
        
        $newMobileState = -not $isMobile
        $baseTheme = $currentTheme -replace "-mobile$", ""
        $themeName = $baseTheme
        if ($newMobileState) {
            $mobileCandidate = "$baseTheme-mobile"
            if (Test-Path (Join-Path -Path $themesPath -ChildPath "$mobileCandidate.omp.json")) {
                $themeName = $mobileCandidate
            }
        }
        
        # Save centralized json config
        $cfg = @{ active_theme = $themeName; enable_mobile = $newMobileState }
        $cfg | ConvertTo-Json | Out-File -FilePath $configPath -Force -Encoding utf8
        $env:THEME = $themeName
        
        # Cleanup legacy files
        Remove-Item -Path (Join-Path -Path $themesPath -ChildPath "active_theme.txt") -Force -ErrorAction SilentlyContinue
        Remove-Item -Path (Join-Path -Path $themesPath -ChildPath "mobile_mode_active.txt") -Force -ErrorAction SilentlyContinue

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

    static [void] SetMobileMode([bool]$enableMobile) {
        $themesPath = $env:POSH_THEMES_PATH
        if (-not (Test-Path $themesPath)) { return }
        
        $configPath = Join-Path -Path $themesPath -ChildPath "config.json"
        $currentTheme = "neko"
        if (Test-Path $configPath) {
            try {
                $json = Get-Content $configPath -Raw | ConvertFrom-Json
                if ($json.active_theme) { $currentTheme = $json.active_theme }
            } catch {}
        }
        
        $baseTheme = $currentTheme -replace "-mobile$", ""
        $themeName = $baseTheme
        if ($enableMobile) {
            $mobileCandidate = "$baseTheme-mobile"
            if (Test-Path (Join-Path -Path $themesPath -ChildPath "$mobileCandidate.omp.json")) {
                $themeName = $mobileCandidate
            }
        }
        
        # Save centralized json config
        $cfg = @{ active_theme = $themeName; enable_mobile = $enableMobile }
        $cfg | ConvertTo-Json | Out-File -FilePath $configPath -Force -Encoding utf8
        $env:THEME = $themeName
        
        # Cleanup legacy files
        Remove-Item -Path (Join-Path -Path $themesPath -ChildPath "active_theme.txt") -Force -ErrorAction SilentlyContinue
        Remove-Item -Path (Join-Path -Path $themesPath -ChildPath "mobile_mode_active.txt") -Force -ErrorAction SilentlyContinue

        $themePath = Join-Path -Path $themesPath -ChildPath "$themeName.omp.json"
        if (Test-Path $themePath) {
            $Global:NewThemeToApply = $themePath
            if ($enableMobile) {
                Write-Host "[Theme] Mobile Prompt Theme activated (ASCII mode, stacked)." -ForegroundColor Cyan
            } else {
                Write-Host "[Theme] Desktop Prompt Theme activated (Rich Unicode/Emoji mode)." -ForegroundColor Green
            }
        }
    }
}
#endregion



