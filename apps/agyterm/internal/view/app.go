package view

import (
	"fmt"
	"io"
	"os"
	"path/filepath"
	"strings"

	"golang.org/x/term"

	"agyterm/internal/model"
	"agyterm/internal/service/shelltool"
	"agyterm/internal/service/theme"
	"agyterm/internal/service/winterm"
)

type App struct {
	ActiveTab     int // 0: Themes, 1: WT Fonts, 2: Palette, 3: External Apps & Tools
	SelectedIndex int
	StatusMsg     string
	tabSwitched   bool
	searchMode    bool
	searchQuery   string

	themesDir      string
	currentTheme   string
	settingsFile   string
	cachedThemes   []model.ThemeInfo
	cachedProfiles []model.WindowsTerminalProfile
	cachedFonts    []model.FontInfo
	cachedTools    []model.ExternalToolInfo
	needsReload    bool
}

func NewApp() *App {
	themesDir := theme.ResolveThemesDir()
	curTheme := theme.GetSelectedTheme()
	settingsFile := winterm.FindSettingsFile()

	return &App{
		ActiveTab:    0,
		themesDir:    themesDir,
		currentTheme: curTheme,
		settingsFile: settingsFile,
		tabSwitched:  true,
		needsReload:  true,
	}
}

func (a *App) getFilteredThemes() []model.ThemeInfo {
	q := strings.TrimSpace(strings.ToLower(a.searchQuery))
	if q == "" {
		return a.cachedThemes
	}
	var filtered []model.ThemeInfo
	for _, t := range a.cachedThemes {
		if strings.Contains(strings.ToLower(t.Name), q) {
			filtered = append(filtered, t)
		}
	}
	return filtered
}

func (a *App) RunInteractive() error {
	fd := int(os.Stdin.Fd())
	if !term.IsTerminal(fd) {
		return a.runNonInteractive()
	}

	oldState, err := term.MakeRaw(fd)
	if err != nil {
		return a.runNonInteractive()
	}

	defer func() {
		fmt.Print("\033[?25h\033[?1049l")
		_ = term.Restore(fd, oldState)
	}()

	fmt.Print("\033[?1049h\033[?25l\033[H\033[2J")

	for {
		if a.needsReload || a.cachedThemes == nil {
			a.cachedThemes, _ = theme.ListThemes(a.themesDir, a.currentTheme)
			a.currentTheme = theme.GetSelectedTheme()
			if a.settingsFile != "" {
				a.cachedProfiles, _ = winterm.ReadProfiles(a.settingsFile)
			}
			a.cachedFonts, _ = winterm.ListAvailableFonts()
			a.cachedTools = shelltool.ListExternalTools()
			a.needsReload = false
		}

		filteredThemes := a.getFilteredThemes()
		totalItems := len(filteredThemes)
		if a.ActiveTab == 1 {
			totalItems = len(a.cachedProfiles)
		} else if a.ActiveTab == 2 {
			totalItems = 1
		} else if a.ActiveTab == 3 {
			totalItems = len(a.cachedTools)
		}

		if a.SelectedIndex >= totalItems && totalItems > 0 {
			a.SelectedIndex = totalItems - 1
		}
		if a.SelectedIndex < 0 {
			a.SelectedIndex = 0
		}

		a.Render()
		a.StatusMsg = ""

		var buf [32]byte
		n, err := os.Stdin.Read(buf[:])
		if err != nil || n == 0 {
			break
		}

		b := buf[0]

		if b == 0x1b {
			if n == 1 {
				// Solitary Esc key pressed
				if a.ActiveTab == 0 && (a.searchMode || a.searchQuery != "") {
					a.searchMode = false
					a.searchQuery = ""
					a.SelectedIndex = 0
					a.StatusMsg = "\033[33mCleared search filter.\033[0m"
					continue
				}
				// Solitary Esc key pressed -> Exit cleanly!
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Print("\r\n")
				return nil
			}
			if n >= 3 && buf[1] == '[' {
				switch buf[2] {
				case 'A': // Up
					if a.SelectedIndex > 0 {
						a.SelectedIndex--
					}
					continue
				case 'B': // Down
					if a.SelectedIndex < totalItems-1 {
						a.SelectedIndex++
					}
					continue
				case 'C': // Right Tab
					a.searchMode = false
					a.ActiveTab = (a.ActiveTab + 1) % 4
					a.SelectedIndex = 0
					a.tabSwitched = true
					continue
				case 'D': // Left Tab
					a.searchMode = false
					a.ActiveTab = (a.ActiveTab + 3) % 4
					a.SelectedIndex = 0
					a.tabSwitched = true
					continue
				case '5': // PageUp
					a.SelectedIndex -= 8
					if a.SelectedIndex < 0 {
						a.SelectedIndex = 0
					}
					continue
				case '6': // PageDown
					a.SelectedIndex += 8
					if a.SelectedIndex >= totalItems && totalItems > 0 {
						a.SelectedIndex = totalItems - 1
					}
					continue
				}
			}
			continue
		}

		// If in active search mode on Themes tab:
		if a.ActiveTab == 0 && a.searchMode {
			switch b {
			case '\r', '\n': // Select and apply currently selected filtered theme
				if len(filteredThemes) > 0 && a.SelectedIndex < len(filteredThemes) {
					target := filteredThemes[a.SelectedIndex].Name
					if err := theme.SetTheme(target, a.themesDir); err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mError applying theme: %v\033[0m", err)
					} else {
						a.currentTheme = target
						a.StatusMsg = fmt.Sprintf("\033[32m✔ Applied Oh My Posh theme '%s'\033[0m", target)
					}
				}
				a.searchMode = false
				continue
			case 127, 8: // Backspace
				if len(a.searchQuery) > 0 {
					a.searchQuery = a.searchQuery[:len(a.searchQuery)-1]
					a.SelectedIndex = 0
				}
				continue
			case 0x17: // Ctrl+W (Clear query)
				a.searchQuery = ""
				a.SelectedIndex = 0
				continue
			case '\t': // Tab switches tab
				a.searchMode = false
				a.ActiveTab = (a.ActiveTab + 1) % 4
				a.SelectedIndex = 0
				a.tabSwitched = true
				continue
			default:
				if b >= 32 && b <= 126 {
					a.searchQuery += string(b)
					a.SelectedIndex = 0
					continue
				}
			}
		}

		switch b {
		case '\t':
			a.ActiveTab = (a.ActiveTab + 1) % 4
			a.SelectedIndex = 0
			a.tabSwitched = true
		case '1':
			a.ActiveTab = 0
			a.SelectedIndex = 0
			a.tabSwitched = true
		case '2':
			a.ActiveTab = 1
			a.SelectedIndex = 0
			a.tabSwitched = true
		case '3':
			a.ActiveTab = 2
			a.SelectedIndex = 0
			a.tabSwitched = true
		case '4':
			a.ActiveTab = 3
			a.SelectedIndex = 0
			a.tabSwitched = true
		case '/', 's', 'S', 0x06: // [/] or [s] or [Ctrl+F] activates search
			if a.ActiveTab == 0 {
				a.searchMode = true
				a.StatusMsg = "\033[36mSearch active - type theme name to filter...\033[0m"
				continue
			}
		case 'c', 'C': // Clear search query
			if a.ActiveTab == 0 && a.searchQuery != "" {
				a.searchQuery = ""
				a.SelectedIndex = 0
				a.StatusMsg = "\033[33mSearch cleared.\033[0m"
				continue
			}
		case 'k', 'K':
			if a.SelectedIndex > 0 {
				a.SelectedIndex--
			}
		case 'j', 'J':
			if a.SelectedIndex < totalItems-1 {
				a.SelectedIndex++
			}
		case 'p', 'P':
			if a.ActiveTab == 0 || a.ActiveTab == 3 {
				a.SelectedIndex -= 8
				if a.SelectedIndex < 0 {
					a.SelectedIndex = 0
				}
			}
		case 'n', 'N':
			if a.ActiveTab == 0 || a.ActiveTab == 3 {
				a.SelectedIndex += 8
				if a.SelectedIndex >= totalItems && totalItems > 0 {
					a.SelectedIndex = totalItems - 1
				}
			}
		case '\r', '\n': // Select / Apply Theme or Font or Inspect
			if a.ActiveTab == 0 && len(filteredThemes) > 0 && a.SelectedIndex < len(filteredThemes) {
				target := filteredThemes[a.SelectedIndex].Name
				if err := theme.SetTheme(target, a.themesDir); err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mError applying theme: %v\033[0m", err)
				} else {
					a.currentTheme = target
					a.StatusMsg = fmt.Sprintf("\033[32m✔ Applied Oh My Posh theme '%s'\033[0m", target)
				}
			} else if a.ActiveTab == 3 && len(a.cachedTools) > 0 && a.SelectedIndex < len(a.cachedTools) {
				tool := a.cachedTools[a.SelectedIndex]
				if tool.ID == "oh-my-posh" {
					a.ActiveTab = 0
					a.SelectedIndex = 0
					a.tabSwitched = true
					a.StatusMsg = "\033[32mSwitched to Tab 1 [Prompt Themes] to select theme\033[0m"
				} else {
					a.StatusMsg = fmt.Sprintf("\033[32m✔ %s (%s)\033[0m", tool.Name, tool.Summary)
				}
			}
		case 'f', 'F': // Cycle Font in Tab 1
			if a.ActiveTab == 1 && len(a.cachedProfiles) > 0 && a.SelectedIndex < len(a.cachedProfiles) {
				p := a.cachedProfiles[a.SelectedIndex]
				var fontNames []string
				for _, f := range a.cachedFonts {
					fontNames = append(fontNames, f.Name)
				}
				if len(fontNames) == 0 {
					fontNames = []string{"Hack Nerd Font", "MesloLGS NF", "Cascadia Code", "FiraCode Nerd Font", "JetBrainsMono NF"}
				}
				curIdx := 0
				for i, fn := range fontNames {
					if strings.EqualFold(fn, p.FontFace) {
						curIdx = i
						break
					}
				}
				nextIdx := (curIdx + 1) % len(fontNames)
				nextFont := fontNames[nextIdx]
				if err := winterm.UpdateProfileFont(a.settingsFile, p.Name, nextFont, p.FontSize); err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mError updating font: %v\033[0m", err)
				} else {
					a.needsReload = true
					a.StatusMsg = fmt.Sprintf("\033[32mSet font for '%s' -> '%s'\033[0m", p.Name, nextFont)
				}
			}
		case '+', '=': // Increase font size
			if a.ActiveTab == 1 && len(a.cachedProfiles) > 0 && a.SelectedIndex < len(a.cachedProfiles) {
				p := a.cachedProfiles[a.SelectedIndex]
				newSize := p.FontSize + 1.0
				if newSize < 8.0 {
					newSize = 12.0
				}
				if err := winterm.UpdateProfileFont(a.settingsFile, p.Name, p.FontFace, newSize); err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mError: %v\033[0m", err)
				} else {
					a.needsReload = true
					a.StatusMsg = fmt.Sprintf("\033[32mIncreased font size for '%s' to %.0f pt\033[0m", p.Name, newSize)
				}
			}
		case '-', '_': // Decrease font size
			if a.ActiveTab == 1 && len(a.cachedProfiles) > 0 && a.SelectedIndex < len(a.cachedProfiles) {
				p := a.cachedProfiles[a.SelectedIndex]
				newSize := p.FontSize - 1.0
				if newSize < 8.0 {
					newSize = 8.0
				}
				if err := winterm.UpdateProfileFont(a.settingsFile, p.Name, p.FontFace, newSize); err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mError: %v\033[0m", err)
				} else {
					a.needsReload = true
					a.StatusMsg = fmt.Sprintf("\033[32mDecreased font size for '%s' to %.0f pt\033[0m", p.Name, newSize)
				}
			}
		case 'o', 'O': // Cycle opacity
			if a.ActiveTab == 1 && len(a.cachedProfiles) > 0 && a.SelectedIndex < len(a.cachedProfiles) {
				p := a.cachedProfiles[a.SelectedIndex]
				opSteps := []int{100, 95, 90, 85, 80, 75, 70}
				curIdx := 0
				for i, op := range opSteps {
					if op == p.Opacity {
						curIdx = i
						break
					}
				}
				nextIdx := (curIdx + 1) % len(opSteps)
				nextOp := opSteps[nextIdx]
				if err := winterm.UpdateProfileOpacity(a.settingsFile, p.Name, nextOp); err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mError: %v\033[0m", err)
				} else {
					a.needsReload = true
					a.StatusMsg = fmt.Sprintf("\033[32mSet opacity for '%s' -> %d%%\033[0m", p.Name, nextOp)
				}
			}
		case 'r', 'R': // Refresh
			a.needsReload = true
			a.StatusMsg = "\033[32mRefreshed themes and profiles.\033[0m"
		case 'q', 'Q', 0x03:
			fmt.Print("\033[?25h\033[?1049l")
			_ = term.Restore(fd, oldState)
			fmt.Print("\r\n")
			return nil
		}
	}
	return nil
}

func getTermSize() (int, int) {
	width := 80
	height := 24
	if fd := int(os.Stdin.Fd()); term.IsTerminal(fd) {
		if w, h, err := term.GetSize(fd); err == nil {
			if w > 0 {
				width = w
			}
			if h > 0 {
				height = h
			}
		}
	}
	return width, height
}

func truncateString(s string, maxLen int) string {
	if maxLen <= 0 {
		return ""
	}
	if maxLen <= 3 {
		if len(s) > maxLen {
			return s[:maxLen]
		}
		return s
	}
	if len(s) > maxLen {
		return s[:maxLen-3] + "..."
	}
	return s
}

func hr(width int) string {
	w := width - 1
	if w < 20 {
		w = 20
	}
	if w > 100 {
		w = 100
	}
	return strings.Repeat("─", w) + "\033[K\r\n"
}

func (a *App) Render() {
	width, height := getTermSize()
	var b strings.Builder
	b.Grow(4096)

	if a.tabSwitched {
		b.WriteString("\033[H\033[2J")
		a.tabSwitched = false
	} else {
		b.WriteString("\033[H")
	}

	if width < 85 {
		b.WriteString("\r\n🎨 \033[1;36mAGYTERM\033[0m · Font & Theme Center\033[K\r\n")
		b.WriteString(hr(width))
		tabNames := []string{"1:Themes", "2:WT Fonts", "3:Palette", "4:Apps"}
		for i, t := range tabNames {
			if i == a.ActiveTab {
				fmt.Fprintf(&b, "\033[1;37;44m [%s] \033[0m ", t)
			} else {
				fmt.Fprintf(&b, "\033[36m[%s]\033[0m ", t)
			}
		}
		b.WriteString("\033[K\r\n" + hr(width))
	} else {
		b.WriteString("\r\n🎨 \033[1;36mAGYTERM - Terminal Font & Theme Manager (Go Engine)\033[0m\033[K\r\n")
		b.WriteString(hr(width))
		tabs := []string{"[1] 🎨 Prompt Themes", "[2] 🔤 Windows Terminal Fonts", "[3] 🐧 Linux Palette & Colors", "[4] ⚡ External Apps & Config"}
		for i, t := range tabs {
			if i == a.ActiveTab {
				fmt.Fprintf(&b, " \033[1;37;44m %s \033[0m ", t)
			} else {
				fmt.Fprintf(&b, " \033[36m%s\033[0m ", t)
			}
		}
		b.WriteString("\033[K\r\n" + hr(width))
	}

	switch a.ActiveTab {
	case 0:
		a.renderThemesTab(&b, width, height)
	case 1:
		a.renderWTFontsTab(&b, width, height)
	case 2:
		a.renderPaletteTab(&b, width)
	case 3:
		a.renderToolsTab(&b, width, height)
	}

	b.WriteString(hr(width))
	if a.StatusMsg != "" {
		fmt.Fprintf(&b, " %s\033[K\r\n", a.StatusMsg)
	} else {
		b.WriteString("\033[K\r\n")
	}

	if width < 85 {
		switch a.ActiveTab {
		case 0:
			if a.searchMode {
				b.WriteString(" \033[1;33m[Typing]\033[0mFilter \033[1;32m[Enter]\033[0mApply \033[1;31m[Esc]\033[0mExit\033[K\r\n")
			} else if a.searchQuery != "" {
				b.WriteString(" \033[1m[↑/↓]\033[0mNav \033[1;32m[Enter]\033[0mApply \033[1;33m[/]\033[0mSearch \033[1;31m[Esc/C]\033[0mClear\033[K\r\n")
			} else {
				b.WriteString(" \033[1m[Tab]\033[0mNav \033[1m[↑/↓]\033[0mNav \033[1;33m[/]\033[0mSearch \033[1;32m[Enter]\033[0mSet \033[1;31m[Q]\033[0mExit\033[K\r\n")
			}
		case 1:
			b.WriteString(" \033[1m[Tab]\033[0mNav \033[1m[↑/↓]\033[0mNav \033[1;36m[F]\033[0mFont \033[1;36m[+/-]\033[0mSize \033[1;36m[O]\033[0mOpac \033[1;31m[Q]\033[0mExit\033[K\r\n")
		case 2:
			b.WriteString(" \033[1m[Tab]\033[0mNav \033[1;31m[Q]\033[0mExit\033[K\r\n")
		case 3:
			b.WriteString(" \033[1m[Tab]\033[0mNav \033[1m[↑/↓]\033[0mNav \033[1;32m[Enter]\033[0mAct \033[1;36m[R]\033[0mRef \033[1;31m[Q]\033[0mExit\033[K\r\n")
		}
	} else {
		switch a.ActiveTab {
		case 0:
			if a.searchMode {
				b.WriteString(" \033[1;33m[Type to Search]\033[0m · \033[1;32m[Enter]\033[0m Apply · \033[1;36m[Backspace]\033[0m Edit · \033[1;36m[Ctrl+W]\033[0m Clear · \033[1;31m[Esc]\033[0m Exit Search\033[K\r\n")
			} else if a.searchQuery != "" {
				b.WriteString(" \033[1m[Tab/1-4]\033[0m Switch · \033[1m[↑/↓ j/k]\033[0m Nav · \033[1;32m[Enter]\033[0m Apply · \033[1;33m[/]\033[0m Search · \033[1;33m[Esc/c]\033[0m Clear Filter · \033[1;31m[Q]\033[0m Exit\033[K\r\n")
			} else {
				b.WriteString(" \033[1m[Tab/1-4]\033[0m Switch · \033[1m[↑/↓ j/k]\033[0m Nav · \033[1;33m[/ or s]\033[0m Search Theme · \033[1;32m[Enter]\033[0m Apply · \033[1;36m[n/p]\033[0m Page · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
			}
		case 1:
			b.WriteString(" \033[1m[Tab/1-4]\033[0m Switch · \033[1m[↑/↓ j/k]\033[0m Nav · \033[1;36m[F]\033[0m Font · \033[1;36m[+/-]\033[0m Size · \033[1;36m[O]\033[0m Opacity · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
		case 2:
			b.WriteString(" \033[1m[Tab/1-4]\033[0m Switch · \033[1;36m[R]\033[0m Refresh · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
		case 3:
			b.WriteString(" \033[1m[Tab/1-4]\033[0m Switch · \033[1m[↑/↓ j/k]\033[0m Select Tool · \033[1;32m[Enter]\033[0m Action · \033[1;36m[R]\033[0m Refresh · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
		}
	}

	b.WriteString("\033[J")
	os.Stdout.WriteString(b.String())
}

func (a *App) renderThemesTab(b *strings.Builder, width int, height int) {
	allThemes := a.cachedThemes
	if len(allThemes) == 0 {
		b.WriteString(" \033[33mNo Oh My Posh themes discovered in shell/assets/powershell-themes.\033[0m\033[K\r\n")
		return
	}

	themes := a.getFilteredThemes()

	pageSize := height - 14
	if pageSize < 4 {
		pageSize = 4
	}
	if pageSize > 10 {
		pageSize = 10
	}

	fmt.Fprintf(b, " 🎨 \033[1;36mShell Prompt Themes (%d total · Active: \033[1;32m%s\033[1;36m):\033[0m\033[K\r\n",
		len(allThemes), a.currentTheme)

	if a.searchMode {
		fmt.Fprintf(b, " 🔍 \033[1;33mSearch:\033[0m \033[1;37;44m %-22s \033[0m \033[36m(%d matches · [Enter] Apply · [Esc] Exit)\033[0m\033[K\r\n\033[K\r\n",
			a.searchQuery+"█", len(themes))
	} else if a.searchQuery != "" {
		fmt.Fprintf(b, " 🔍 \033[1;33mFilter:\033[0m \033[1;32m\"%s\"\033[0m \033[36m(%d matches · [/] Search · [Esc] Clear)\033[0m\033[K\r\n\033[K\r\n",
			a.searchQuery, len(themes))
	} else {
		fmt.Fprintf(b, " 🔍 \033[37mFilter:\033[0m \033[90mPress [/] or [s] to search 80+ themes (e.g. cat, drac, pure, agy)\033[0m\033[K\r\n\033[K\r\n")
	}

	if len(themes) == 0 {
		fmt.Fprintf(b, "  \033[33mNo themes matching \"%s\". Press [Esc] or [c] to clear filter.\033[0m\033[K\r\n", a.searchQuery)
		for k := 1; k < pageSize; k++ {
			b.WriteString("\033[K\r\n")
		}
		fmt.Fprintf(b, "\033[K\r\n \033[37m[0 matches found · Press [Esc] to reset]\033[0m\033[K\r\n")
		return
	}

	page := a.SelectedIndex / pageSize
	startIdx := page * pageSize
	endIdx := startIdx + pageSize
	if endIdx > len(themes) {
		endIdx = len(themes)
	}
	totalPages := (len(themes) + pageSize - 1) / pageSize
	if totalPages == 0 {
		totalPages = 1
	}

	for i := startIdx; i < endIdx; i++ {
		t := themes[i]
		cursor := "  "
		highlightStart := ""
		highlightEnd := ""
		if i == a.SelectedIndex {
			cursor = "\033[1;32m▶ \033[0m"
			highlightStart = "\033[1;37;44m"
			highlightEnd = "\033[0m"
		}

		activeTag := "  "
		if strings.EqualFold(t.Name, a.currentTheme) {
			activeTag = "\033[1;32m● \033[0m"
		}

		if width < 85 {
			name := truncateString(t.Name, 18)
			fmt.Fprintf(b, "%s%s%s%2d. \033[1m%-18s\033[0m %s%s\033[K\r\n",
				cursor, highlightStart, activeTag, i+1, name, t.Preview, highlightEnd)
		} else {
			name := truncateString(t.Name, 26)
			fmt.Fprintf(b, "%s%s%s%2d. \033[1m%-26s\033[0m │ %s%s\033[K\r\n",
				cursor, highlightStart, activeTag, i+1, name, t.Preview, highlightEnd)
		}
	}

	for i := endIdx - startIdx; i < pageSize; i++ {
		b.WriteString("\033[K\r\n")
	}

	fmt.Fprintf(b, "\033[K\r\n \033[37m[Page %d/%d · %d-%d of %d themes · [Enter] Apply Theme · [n/p] Page]\033[0m\033[K\r\n",
		page+1, totalPages, startIdx+1, endIdx, len(themes))
}

func (a *App) renderWTFontsTab(b *strings.Builder, width int, height int) {
	if a.settingsFile == "" {
		b.WriteString(" \033[33mWindows Terminal settings.json not found on this host.\033[0m\033[K\r\n")
		return
	}

	fmt.Fprintf(b, " 🔤 \033[1;36mWindows Terminal Profiles & Fonts (\033[37m%s\033[1;36m):\033[0m\033[K\r\n\033[K\r\n",
		filepath.Base(a.settingsFile))

	profiles := a.cachedProfiles
	for i, p := range profiles {
		cursor := "  "
		highlightStart := ""
		highlightEnd := ""
		if i == a.SelectedIndex {
			cursor = "\033[1;32m▶ \033[0m"
			highlightStart = "\033[1;37;44m"
			highlightEnd = "\033[0m"
		}

		face := p.FontFace
		if face == "" {
			face = "(inherited)"
		}
		szStr := fmt.Sprintf("%.0f pt", p.FontSize)
		if p.FontSize <= 0 {
			szStr = "default"
		}
		opStr := fmt.Sprintf("%d%%", p.Opacity)
		if p.Opacity <= 0 {
			opStr = "100%"
		}

		if width < 85 {
			name := truncateString(p.Name, 14)
			fmt.Fprintf(b, "%s%s%2d. \033[1m%-14s\033[0m \033[35m%-16s\033[0m %s%s\033[K\r\n",
				cursor, highlightStart, i+1, name, truncateString(face, 16), opStr, highlightEnd)
		} else {
			name := truncateString(p.Name, 22)
			fmt.Fprintf(b, "%s%s%2d. \033[1m%-22s\033[0m Font: \033[35m%-22s\033[0m Size: \033[33m%-8s\033[0m Opacity: \033[32m%s\033[0m%s\033[K\r\n",
				cursor, highlightStart, i+1, name, face, szStr, opStr, highlightEnd)
		}
	}

	b.WriteString("\033[K\r\n 💡 \033[1;33mAvailable Nerd Fonts (Cycle with [F]):\033[0m\033[K\r\n")
	for idx, f := range a.cachedFonts {
		if idx >= 5 {
			break
		}
		tag := "[Nerd]"
		if !f.IsNerdFont {
			tag = "[Mono]"
		}
		fmt.Fprintf(b, "    • \033[36m%-6s\033[0m %s\033[K\r\n", tag, f.Name)
	}
}

func (a *App) renderPaletteTab(b *strings.Builder, width int) {
	fmt.Fprintf(b, " 🐧 \033[1;36mLinux & ANSI 16-Color Palette Test:\033[0m\033[K\r\n\033[K\r\n")

	b.WriteString("  Normal:  ")
	for c := 30; c <= 37; c++ {
		fmt.Fprintf(b, "\033[%dm███ \033[0m", c)
	}
	b.WriteString("\033[K\r\n")

	b.WriteString("  Bright:  ")
	for c := 90; c <= 97; c++ {
		fmt.Fprintf(b, "\033[%dm███ \033[0m", c)
	}
	b.WriteString("\033[K\r\n\033[K\r\n")

	b.WriteString(" 📁 \033[1;36mFont Storage Locations:\033[0m\033[K\r\n")
	b.WriteString("  • Windows Host User: \033[35m%LOCALAPPDATA%\\Microsoft\\Windows\\Fonts\033[0m\033[K\r\n")
	b.WriteString("  • Linux User Fonts:  \033[35m~/.local/share/fonts\033[0m\033[K\r\n")
	b.WriteString("  • Linux System Fonts:\033[35m/usr/share/fonts\033[0m\033[K\r\n\033[K\r\n")
	b.WriteString(" 💡 \033[37mTip: To install any Nerd font in WSL, drop .ttf into ~/.local/share/fonts and run 'fc-cache -f'.\033[0m\033[K\r\n")
}

func (a *App) renderToolsTab(b *strings.Builder, width int, height int) {
	tools := a.cachedTools
	if len(tools) == 0 {
		b.WriteString(" \033[33mNo external tools or configs discovered.\033[0m\033[K\r\n")
		return
	}

	fmt.Fprintf(b, " ⚡ \033[1;36mExternal Apps, Shell Predictions & Integrations (%d total):\033[0m\033[K\r\n\033[K\r\n", len(tools))

	listLimit := 6
	if height >= 28 {
		listLimit = 9
	}
	if listLimit > len(tools) {
		listLimit = len(tools)
	}

	page := a.SelectedIndex / listLimit
	startIdx := page * listLimit
	endIdx := startIdx + listLimit
	if endIdx > len(tools) {
		endIdx = len(tools)
	}

	for i := startIdx; i < endIdx; i++ {
		t := tools[i]
		cursor := "  "
		highlightStart := ""
		highlightEnd := ""
		if i == a.SelectedIndex {
			cursor = "\033[1;32m▶ \033[0m"
			highlightStart = "\033[1;37;44m"
			highlightEnd = "\033[0m"
		}

		statusDot := "\033[1;32m●\033[0m"
		if !t.Installed {
			statusDot = "\033[1;33m○\033[0m"
		}

		verTag := t.Version
		if verTag == "" {
			if t.Installed {
				verTag = "active"
			} else {
				verTag = "optional"
			}
		}

		if width < 85 {
			name := truncateString(t.Name, 18)
			sum := truncateString(t.Summary, 28)
			fmt.Fprintf(b, "%s%s%s%2d. \033[1m%-18s\033[0m \033[36m%-28s\033[0m%s\033[K\r\n",
				cursor, highlightStart, statusDot, i+1, name, sum, highlightEnd)
		} else {
			name := truncateString(t.Name, 22)
			verDisp := truncateString(verTag, 14)
			sum := truncateString(t.Summary, width-56)
			fmt.Fprintf(b, "%s%s%s%2d. \033[1m%-22s\033[0m \033[35m[%-14s]\033[0m │ \033[37m%s\033[0m%s\033[K\r\n",
				cursor, highlightStart, statusDot, i+1, name, verDisp, sum, highlightEnd)
		}
	}

	if a.SelectedIndex >= 0 && a.SelectedIndex < len(tools) {
		sel := tools[a.SelectedIndex]
		b.WriteString("\033[K\r\n")
		b.WriteString(hr(width))
		fmt.Fprintf(b, " 🔎 \033[1;33mDetails for %s\033[0m (\033[36m%s\033[0m):\033[K\r\n", sel.Name, sel.Category)

		maxDetails := 4
		if height >= 28 {
			maxDetails = 6
		}
		for dIdx, d := range sel.Details {
			if dIdx >= maxDetails {
				break
			}
			fmt.Fprintf(b, "   • \033[37m%s\033[0m\033[K\r\n", truncateString(d, width-7))
		}
		if sel.Tips != "" {
			fmt.Fprintf(b, " 💡 \033[1;32mTip:\033[0m \033[37m%s\033[0m\033[K\r\n", truncateString(sel.Tips, width-9))
		}
	}
}

func (a *App) runNonInteractive() error {
	a.PrintStatus(os.Stdout)
	return nil
}

func (a *App) PrintStatus(w io.Writer) {
	fmt.Fprintln(w, "\n🎨 \033[1;36mAGYTERM - Terminal Font & Theme Manager\033[0m")
	fmt.Fprintln(w, "──────────────────────────────────────────────────────────────────────────────────")
	curTheme := theme.GetSelectedTheme()
	fmt.Fprintf(w, " Active Shell Prompt Theme: \033[1;32m%s\033[0m\n", curTheme)
	settingsFile := winterm.FindSettingsFile()
	if settingsFile != "" {
		fmt.Fprintf(w, " Windows Terminal Settings: \033[35m%s\033[0m\n", settingsFile)
		if profiles, err := winterm.ReadProfiles(settingsFile); err == nil {
			fmt.Fprintln(w, "\n Profiles:")
			for _, p := range profiles {
				fmt.Fprintf(w, "  • %-20s Font: %-20s (Opacity: %d%%)\n", p.Name, p.FontFace, p.Opacity)
			}
		}
	}
	tools := shelltool.ListExternalTools()
	fmt.Fprintln(w, "\n External Apps & Shell Configurations:")
	for _, t := range tools {
		status := "\033[32m✔\033[0m"
		if !t.Installed {
			status = "\033[33m○\033[0m"
		}
		fmt.Fprintf(w, "  %s %-22s [%-14s] %s\n", status, t.Name, t.Category, t.Summary)
	}
	fmt.Fprintln(w, "──────────────────────────────────────────────────────────────────────────────────")
}
