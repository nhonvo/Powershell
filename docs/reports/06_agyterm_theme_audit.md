# 🎨 Deep Technical Audit: `agyterm` Terminal & Theme Customizer (Go Engine)

> **Category**: Developer Suite Core Audit  
> **Target Subsystem**: `apps/agyterm`  
> **Source Files**: [apps/agyterm/main.go](../../apps/agyterm/main.go) · [apps/agyterm/internal/service/theme/theme.go](../../apps/agyterm/internal/service/theme/theme.go) · [apps/agyterm/internal/service/winterm/winterm.go](../../apps/agyterm/internal/service/winterm/winterm.go) · [apps/agyterm/internal/service/shelltool/shelltool.go](../../apps/agyterm/internal/service/shelltool/shelltool.go) · [apps/agyterm/internal/view/app.go](../../apps/agyterm/internal/view/app.go)  
> **Comparison Baseline**: C# `ThemeManager.cs` & Shell configuration scripts  
> **Windows UNC Reference**: `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\apps\agyterm`

---

## 1. Executive Summary & Architecture

`agyterm` provides terminal font customization, prompt theme management, and shell diagnostics across Windows Terminal and WSL2 environments:
1. **Windows Terminal JSON Profile Manipulation**: Direct mutation of Windows Terminal `settings.json` from WSL2 to adjust font faces, sizes, and window opacity (`70%–100%`) with automatic `.bak` safety backups.
2. **Oh-My-Posh Theme Engine & Segment Preview**: Parses 80+ `.omp.json` theme files and dynamically builds ANSI colored segment representations (e.g. `\033[32m[dir]\033[35m[git]\033[0m`).
3. **Live Search Filter**: Interactive `/` search filter for instant theme searching.
4. **Environment Diagnostics**: Scans 9 shell subsystems (Oh-My-Posh, Starship, Zsh history, PSReadLine, fzf, eza, zoxide, bat, Docker).

```
apps/agyterm/
├── go.mod                                   # Go 1.25.0, requires only x/term and x/sys
├── main.go                                  # CLI command router & interactive TUI dispatcher
└── internal/
    ├── model/
    │   └── term.go                          # ThemeInfo, FontInfo, WinTermProfile schemas
    ├── service/
    │   ├── theme/
    │   │   ├── theme.go                     # Oh-My-Posh parser & ANSI previewer (240 LOC)
    │   │   └── theme_test.go                # Unit tests
    │   ├── winterm/
    │   │   ├── winterm.go                   # Windows Terminal JSON editor (210 LOC)
    │   │   └── winterm_test.go              # Unit tests
    │   ├── shelltool/
    │   │   ├── shelltool.go                 # 9-tool shell diagnostics scanner (320 LOC)
    │   │   └── shelltool_test.go            # Diagnostics tests
    │   └── linuxterm/                       # ⚠️ CRITICAL: EMPTY DIRECTORY (0 files)
    └── view/
        ├── app.go                           # 4-Tab ANSI TUI engine with live search
        └── view_test.go                     # View smoke tests
```

---

## 2. Code Defects & Implementation Gaps

### 2.1 Architectural Anomaly: Empty `internal/service/linuxterm` Directory
- **Location**: [apps/agyterm/internal/service/linuxterm](../../apps/agyterm/internal/service/linuxterm)
- **Defect**: The directory contains 0 files.
- **Root Cause**: Windows Terminal (`winterm`) was implemented, but configuration editors for Linux-native terminals (Alacritty, Kitty, WezTerm, Ghostty) were stubbed and never written.
- **Impact**: Developers running native Linux GUI terminals cannot adjust fonts or themes via `agyterm`.
- **Remediation**: Implement TOML and Lua parsers for `alacritty.toml`, `kitty.conf`, and `wezterm.lua`.

### 2.2 Missing Automated Font Downloader
- **Defect**: While `agyterm fonts` lists available system and Nerd Fonts, `agyterm` cannot download or install missing Nerd Fonts. It merely prints a tip: `drop .ttf into ~/.local/share/fonts and run fc-cache -f`.
- **Remediation**: Implement `agyterm font install <name>` (e.g. `Hack`, `FiraCode`, `JetBrainsMono`, `CascadiaCode`) that downloads release archives from GitHub (`ryanoasis/nerd-fonts`), unpacks them into `~/.local/share/fonts/`, and invokes `fc-cache -f`.

### 2.3 Starship Prompt Configuration Unimplemented
- **Defect**: Starship binary and config path are detected in diagnostics (`Tab 3`), but `agyterm` cannot switch Starship presets or write to `~/.config/starship.toml`.

---

## 3. Prioritized Action Plan & Next Steps

1. **Implement `linuxterm` Service**:
   - Add Alacritty (`~/.config/alacritty/alacritty.toml`) and Kitty (`~/.config/kitty/kitty.conf`) font face and opacity modifiers.
2. **Automated Nerd Font Installer**:
   - Provide 1-tap download and installation of popular Nerd Fonts with progress bar.
3. **Starship Preset Switcher**:
   - Add preset switcher for Starship prompt alongside Oh-My-Posh themes.
