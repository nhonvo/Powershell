package view

import (
	"bufio"
	"fmt"
	"io"
	"os"
	"path/filepath"
	"strings"

	"golang.org/x/term"

	"agyproj/internal/model"
	"agyproj/internal/service/launcher"
	"agyproj/internal/service/registry"
)

type App struct {
	Registry        *registry.Manager
	Launcher        *launcher.Launcher
	ScanRootDir     string
	ActiveTab       int // 0: Workspaces, 1: Discover, 2: IDEs & Config
	SelectedIndex   int
	StatusMsg       string
	DiscoveredCache []model.ProjectInfo
	tabSwitched     bool

	// Cached state & filtering
	registeredCache []model.ProjectInfo
	needsReload     bool
	searchQuery     string
	inSearchMode    bool
}

func NewApp(reg *registry.Manager, lnch *launcher.Launcher) *App {
	scanRoot := ""
	if reg != nil {
		scanRoot = filepath.Join(reg.UserHome, "projects")
	}
	return &App{
		Registry:    reg,
		Launcher:    lnch,
		ScanRootDir: scanRoot,
		ActiveTab:   0,
		tabSwitched: true,
		needsReload: true,
	}
}

func (a *App) getFilteredWorkspaces() []model.ProjectInfo {
	if a.searchQuery == "" {
		return a.registeredCache
	}
	q := strings.ToLower(strings.TrimSpace(a.searchQuery))
	if q == "" {
		return a.registeredCache
	}
	var filtered []model.ProjectInfo
	for _, p := range a.registeredCache {
		if strings.Contains(strings.ToLower(p.Name), q) ||
			strings.Contains(strings.ToLower(p.Stack), q) ||
			strings.Contains(strings.ToLower(p.GitBranch), q) ||
			strings.Contains(strings.ToLower(p.Path), q) {
			filtered = append(filtered, p)
		}
	}
	return filtered
}

// SetSearchQuery sets the search query filter.
func (a *App) SetSearchQuery(q string) {
	a.searchQuery = q
}

// GetFilteredWorkspaces returns filtered workspaces matching current search query.
func (a *App) GetFilteredWorkspaces() []model.ProjectInfo {
	return a.getFilteredWorkspaces()
}

// SetRegisteredCache sets cached workspaces.
func (a *App) SetRegisteredCache(list []model.ProjectInfo) {
	a.registeredCache = list
}

func (a *App) Run() error {
	return a.RunInteractive()
}

func _unusedNewApp(rootDir string) *App {
	reg := registry.NewManager("")
	lch := launcher.NewLauncher()
	return &App{
		Registry:    reg,
		Launcher:    lch,
		ScanRootDir: rootDir,
		ActiveTab:   0,
		tabSwitched: true,
	}
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
		if a.needsReload || a.registeredCache == nil {
			a.registeredCache = a.Registry.ListRegistered()
			a.needsReload = false
		}
		if a.ActiveTab == 1 && a.DiscoveredCache == nil {
			a.DiscoveredCache, _ = a.Registry.ScanDirectory(a.ScanRootDir)
		}

		registeredList := a.getFilteredWorkspaces()

		totalItems := len(registeredList)
		if a.ActiveTab == 1 {
			totalItems = len(a.DiscoveredCache)
		} else if a.ActiveTab == 2 {
			totalItems = 4 // IDE options
		}

		if a.SelectedIndex >= totalItems && totalItems > 0 {
			a.SelectedIndex = totalItems - 1
		}
		if a.SelectedIndex < 0 {
			a.SelectedIndex = 0
		}

		if a.inSearchMode {
			a.StatusMsg = fmt.Sprintf("🔍 \033[1;33mSearch:\033[0m %s\033[7m \033[0m \033[37m(Enter to apply, Esc to cancel)\033[0m", a.searchQuery)
		} else if a.searchQuery != "" && a.ActiveTab == 0 && a.StatusMsg == "" {
			a.StatusMsg = fmt.Sprintf("🔍 \033[36mFilter active:\033[0m \"%s\" (%d/%d matches · [/] edit · [Esc] clear)", a.searchQuery, len(registeredList), len(a.registeredCache))
		}

		a.Render(registeredList, a.DiscoveredCache)
		a.StatusMsg = ""

		var buf [32]byte
		n, err := os.Stdin.Read(buf[:])
		if err != nil || n == 0 {
			break
		}

		b := buf[0]

		if a.inSearchMode {
			if b == 0x1b {
				if n == 1 {
					// Solitary Esc -> exit search mode and clear query
					a.inSearchMode = false
					a.searchQuery = ""
					a.SelectedIndex = 0
					a.tabSwitched = true
					continue
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
					}
				}
				continue
			}

			if b == '\r' || b == '\n' {
				a.inSearchMode = false
				a.tabSwitched = true
				if a.searchQuery != "" {
					a.StatusMsg = fmt.Sprintf("\033[32m✔ Filter applied: \"%s\" (%d matches)\033[0m", a.searchQuery, len(registeredList))
				}
				continue
			}

			if b == 0x7f || b == 0x08 { // Backspace
				if len(a.searchQuery) > 0 {
					a.searchQuery = a.searchQuery[:len(a.searchQuery)-1]
					a.SelectedIndex = 0
					a.tabSwitched = true
				}
				continue
			}

			if b >= 32 && b <= 126 {
				a.searchQuery += string(b)
				a.SelectedIndex = 0
				a.tabSwitched = true
				continue
			}

			continue
		}

		if b == 0x1b {
			if n == 1 {
				if a.searchQuery != "" {
					a.searchQuery = ""
					a.SelectedIndex = 0
					a.StatusMsg = "\033[32m✔ Search filter cleared\033[0m"
					a.tabSwitched = true
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
					a.ActiveTab = (a.ActiveTab + 1) % 3
					a.SelectedIndex = 0
					a.tabSwitched = true
					continue
				case 'D': // Left Tab
					a.ActiveTab = (a.ActiveTab + 2) % 3
					a.SelectedIndex = 0
					a.tabSwitched = true
					continue
				}
			}
			continue
		}

		switch b {
		case '\t':
			a.ActiveTab = (a.ActiveTab + 1) % 3
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
		case '/': // Search filter in Tab 0
			if a.ActiveTab == 0 {
				a.inSearchMode = true
				a.tabSwitched = true
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
		case '\r', '\n', 'c', 'C': // Enter / c key
			if a.ActiveTab == 0 && a.SelectedIndex < len(registeredList) {
				sel := registeredList[a.SelectedIndex]
				cfg, _ := a.Registry.Load()
				editor := "code"
				if cfg != nil && cfg.DefaultIDE != "" {
					editor = cfg.DefaultIDE
				}
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[36m[agyproj]\033[0m Launching editor in '\033[32m%s\033[0m'...\r\n", sel.Path)
				err := a.Launcher.Launch(editor, sel.Path)
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
				a.tabSwitched = true
				a.needsReload = true
				if err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mError launching editor: %v\033[0m", err)
				} else {
					a.StatusMsg = fmt.Sprintf("\033[32m✔ Closed editor for '%s'\033[0m", sel.Name)
				}
				continue
			} else if a.ActiveTab == 1 && a.SelectedIndex < len(a.DiscoveredCache) {
				sel := a.DiscoveredCache[a.SelectedIndex]
				_, _ = a.Registry.Register(sel.Path, false)
				a.needsReload = true
				a.StatusMsg = fmt.Sprintf("\033[32mRegistered workspace '%s'\033[0m", sel.Name)
				a.DiscoveredCache, _ = a.Registry.ScanDirectory(a.ScanRootDir)
			} else if a.ActiveTab == 2 {
				options := []string{"code", "cursor", "nvim", "agy"}
				if a.SelectedIndex < len(options) {
					targetIDE := options[a.SelectedIndex]
					_ = a.Registry.SetDefaultIDE(targetIDE)
					a.StatusMsg = fmt.Sprintf("\033[32mSet default IDE to '%s'\033[0m", launcher.FormatIdeName(targetIDE))
				}
			}
		case 'u', 'U': // Cursor
			if a.ActiveTab == 0 && a.SelectedIndex < len(registeredList) {
				sel := registeredList[a.SelectedIndex]
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[36m[agyproj]\033[0m Launching Cursor in '\033[32m%s\033[0m'...\r\n", sel.Path)
				err := a.Launcher.Launch("cursor", sel.Path)
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
				a.tabSwitched = true
				a.needsReload = true
				if err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mError launching Cursor: %v\033[0m", err)
				} else {
					a.StatusMsg = fmt.Sprintf("\033[32m✔ Closed Cursor for '%s'\033[0m", sel.Name)
				}
				continue
			}
		case 'v', 'V': // Neovim
			if a.ActiveTab == 0 && a.SelectedIndex < len(registeredList) {
				sel := registeredList[a.SelectedIndex]
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[36m[agyproj]\033[0m Launching editor in '\033[32m%s\033[0m'...\r\n", sel.Path)
				err := a.Launcher.Launch("nvim", sel.Path)
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
				a.tabSwitched = true
				a.needsReload = true
				if err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mError launching editor: %v\033[0m", err)
				} else {
					a.StatusMsg = fmt.Sprintf("\033[32m✔ Closed editor for '%s'\033[0m", sel.Name)
				}
				continue
			}
		case 'a', 'A': // Launch Antigravity in Tab 0, or Register All in Tab 1
			if a.ActiveTab == 0 && a.SelectedIndex < len(registeredList) {
				sel := registeredList[a.SelectedIndex]
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[36m[agyproj]\033[0m Launching Antigravity CLI in '\033[32m%s\033[0m'...\r\n", sel.Path)
				err := a.Launcher.Launch("agy", sel.Path)
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
				a.tabSwitched = true
				a.needsReload = true
				if err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mError launching Antigravity: %v\033[0m", err)
				} else {
					a.StatusMsg = fmt.Sprintf("\033[32m✔ Completed Antigravity session in '%s'\033[0m", sel.Name)
				}
				continue
			} else if a.ActiveTab == 1 {
				count, _ := a.Registry.RegisterAll(a.ScanRootDir)
				a.needsReload = true
				a.StatusMsg = fmt.Sprintf("\033[32mRegistered all %d projects from %s\033[0m", count, a.ScanRootDir)
				a.DiscoveredCache, _ = a.Registry.ScanDirectory(a.ScanRootDir)
			}
		case 't', 'T': // Open Terminal Shell in Tab 0
			if a.ActiveTab == 0 && a.SelectedIndex < len(registeredList) {
				sel := registeredList[a.SelectedIndex]
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[36m[agyproj]\033[0m Dropping into shell in '\033[32m%s\033[0m'...\r\n", sel.Path)
				err := a.Launcher.Launch("sh", sel.Path)
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
				a.tabSwitched = true
				a.needsReload = true
				if err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mShell error: %v\033[0m", err)
				} else {
					a.StatusMsg = fmt.Sprintf("\033[32m✔ Returned from shell in '%s'\033[0m", sel.Name)
				}
				continue
			}
		case 'p', 'P': // Toggle Pin in Tab 0
			if a.ActiveTab == 0 && a.SelectedIndex < len(registeredList) {
				sel := registeredList[a.SelectedIndex]
				pinned, _ := a.Registry.TogglePin(sel.ID)
				a.needsReload = true
				if pinned {
					a.StatusMsg = fmt.Sprintf("\033[32mPinned '%s' to top\033[0m", sel.Name)
				} else {
					a.StatusMsg = fmt.Sprintf("\033[33mUnpinned '%s'\033[0m", sel.Name)
				}
			}
		case 'r', 'R': // Manual Refresh in Tab 0 or Rescan in Tab 1
			if a.ActiveTab == 0 {
				a.needsReload = true
				a.StatusMsg = "\033[32m✔ Refreshed workspaces\033[0m"
			} else if a.ActiveTab == 1 {
				a.DiscoveredCache, _ = a.Registry.ScanDirectory(a.ScanRootDir)
				a.StatusMsg = fmt.Sprintf("\033[32mRescanned %s\033[0m", a.ScanRootDir)
			}
		case 's', 'S': // Set Active Context in Tab 0, or Rescan in Tab 1
			if a.ActiveTab == 0 && a.SelectedIndex < len(registeredList) {
				sel := registeredList[a.SelectedIndex]
				_ = a.Registry.SetActive(sel.ID)
				a.needsReload = true
				a.StatusMsg = fmt.Sprintf("\033[32mSet active context to '%s'\033[0m", sel.Name)
			} else if a.ActiveTab == 1 {
				a.DiscoveredCache, _ = a.Registry.ScanDirectory(a.ScanRootDir)
				a.needsReload = true
				a.StatusMsg = fmt.Sprintf("\033[32mRescanned %s\033[0m", a.ScanRootDir)
			}
		case 'd', 'D': // Delete / Unregister in Tab 0
			if a.ActiveTab == 0 && a.SelectedIndex < len(registeredList) {
				sel := registeredList[a.SelectedIndex]
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[31m[agyproj]\033[0m Remove workspace '%s' from registry? (y/N): ", sel.Name)
				var confirm string
				fmt.Scanln(&confirm)
				if strings.EqualFold(strings.TrimSpace(confirm), "y") {
					_ = a.Registry.Unregister(sel.ID)
					a.needsReload = true
					a.StatusMsg = fmt.Sprintf("\033[33mRemoved '%s' from registry\033[0m", sel.Name)
				}
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
				a.tabSwitched = true
			}
		case 'n', 'N': // Add new path manually
			if a.ActiveTab == 0 {
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Print("\r\n\033[36m[agyproj]\033[0m Enter project directory path: ")
				scanner := bufio.NewScanner(os.Stdin)
				if scanner.Scan() {
					inputPath := strings.TrimSpace(scanner.Text())
					if inputPath != "" {
						if _, err := a.Registry.Register(inputPath, false); err != nil {
							a.StatusMsg = fmt.Sprintf("\033[31mError: %v\033[0m", err)
						} else {
							a.needsReload = true
							a.StatusMsg = fmt.Sprintf("\033[32mRegistered '%s'\033[0m", filepath.Base(inputPath))
						}
					}
				}
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
				a.tabSwitched = true
			}
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

func (a *App) Render(registered []model.ProjectInfo, discovered []model.ProjectInfo) {
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
		if a.searchQuery != "" {
			fmt.Fprintf(&b, "\r\n📁 \033[1;36mAGYPROJ\033[0m · Workspaces: \033[1;32m%d/%d\033[0m (\033[33mFilter: \"%s\"\033[0m)\033[K\r\n",
				len(registered), len(a.registeredCache), a.searchQuery)
		} else {
			fmt.Fprintf(&b, "\r\n📁 \033[1;36mAGYPROJ\033[0m · Workspaces: \033[1;32m%d\033[0m\033[K\r\n", len(registered))
		}
		b.WriteString(hr(width))
		tabNames := []string{"1:Workspaces", "2:Discover", "3:IDEs"}
		for i, t := range tabNames {
			if i == a.ActiveTab {
				fmt.Fprintf(&b, "\033[1;37;44m [%s] \033[0m ", t)
			} else {
				fmt.Fprintf(&b, "\033[36m[%s]\033[0m ", t)
			}
		}
		b.WriteString("\033[K\r\n" + hr(width))
	} else {
		b.WriteString("\r\n📁 \033[1;36mAGYPROJ - Antigravity Workspace & IDE Hub (Go Engine v1.0)\033[0m\033[K\r\n")
		b.WriteString(hr(width))

		tabs := []string{"[1] 📁 Workspaces", "[2] 🔍 Discover & Register", "[3] ⚙️ IDEs & Defaults"}
		for i, t := range tabs {
			if i == a.ActiveTab {
				fmt.Fprintf(&b, " \033[1;37;44m %s \033[0m ", t)
			} else {
				fmt.Fprintf(&b, " \033[36m%s\033[0m ", t)
			}
		}
		b.WriteString("\033[K\r\n" + hr(width))
		cfg, _ := a.Registry.Load()
		activeName := "None"
		defaultIDE := "code"
		if cfg != nil {
			if cfg.ActiveProjectID != "" {
				activeName = cfg.ActiveProjectID
			}
			if cfg.DefaultIDE != "" {
				defaultIDE = cfg.DefaultIDE
			}
		}
		fmt.Fprintf(&b, " Active: \033[1;32m%-16s\033[0m  Default IDE: \033[1;35m%-8s\033[0m  Scan Root: \033[36m%s\033[0m\033[K\r\n\033[K\r\n",
			truncateString(activeName, 16), launcher.FormatIdeName(defaultIDE), truncateString(a.ScanRootDir, width-48))
	}

	switch a.ActiveTab {
	case 0:
		a.renderWorkspacesTab(&b, registered, width, height)
	case 1:
		a.renderDiscoverTab(&b, discovered, width, height)
	case 2:
		a.renderIdesTab(&b, width)
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
			b.WriteString(" \033[1m[Tab]\033[0mNav \033[1;32m[Enter]\033[0mOpen \033[1;36m[/]\033[0mFilter \033[1;35m[T]\033[0mShell \033[1;36m[A]\033[0mAgy \033[1;33m[P]\033[0mPin \033[1;31m[D]\033[0mDel \033[1;31m[Q]\033[0mExit\033[K\r\n")
		case 1:
			b.WriteString(" \033[1m[Tab]\033[0mNav \033[1;32m[Enter]\033[0mRegister \033[1;36m[A]\033[0mAll \033[1;36m[S]\033[0mRescan \033[1;31m[Q]\033[0mExit\033[K\r\n")
		case 2:
			b.WriteString(" \033[1m[Tab]\033[0mNav \033[1;32m[Enter]\033[0mSet Default IDE \033[1;31m[Q]\033[0mExit\033[K\r\n")
		}
	} else {
		switch a.ActiveTab {
		case 0:
			b.WriteString(" \033[1m[Tab/1-3]\033[0m Switch · \033[1m[↑/↓ j/k]\033[0m Nav · \033[1;32m[Enter]\033[0m IDE · \033[1;36m[/]\033[0m Filter · \033[1;35m[T]\033[0m Shell · \033[1;36m[A]\033[0m Agy · \033[1;33m[P]\033[0m Pin · \033[1;32m[S]\033[0m Active · \033[1;31m[D]\033[0m Del · \033[1;31m[Q]\033[0m Exit\033[K\r\n")
		case 1:
			b.WriteString(" \033[1m[Tab/1-3]\033[0m Switch · \033[1m[↑/↓ j/k]\033[0m Nav · \033[1;32m[Enter/R]\033[0m Register · \033[1;36m[A]\033[0m Register All · \033[1;36m[S]\033[0m Rescan · \033[1;31m[Q]\033[0m Exit\033[K\r\n")
		case 2:
			b.WriteString(" \033[1m[Tab/1-3]\033[0m Switch · \033[1m[↑/↓ j/k]\033[0m Nav · \033[1;32m[Enter]\033[0m Set Default IDE · \033[1;31m[Q]\033[0m Exit\033[K\r\n")
		}
	}

	b.WriteString("\033[J")
	os.Stdout.WriteString(b.String())
}

func (a *App) renderWorkspacesTab(b *strings.Builder, registered []model.ProjectInfo, width int, height int) {
	if len(registered) == 0 {
		if a.searchQuery != "" {
			fmt.Fprintf(b, " \033[33mNo workspaces match query '%s'. Press [/] to edit or [Esc] to clear.\033[0m\033[K\r\n", a.searchQuery)
		} else {
			b.WriteString(" \033[33mNo registered workspaces. Switch to [Tab 2] to discover and register projects.\033[0m\033[K\r\n")
		}
		return
	}

	pageSize := height - 12
	if pageSize < 4 {
		pageSize = 4
	}
	if pageSize > 10 {
		pageSize = 10
	}

	page := a.SelectedIndex / pageSize
	startIdx := page * pageSize
	endIdx := startIdx + pageSize
	if endIdx > len(registered) {
		endIdx = len(registered)
	}
	totalPages := (len(registered) + pageSize - 1) / pageSize
	if totalPages == 0 {
		totalPages = 1
	}

	if a.searchQuery != "" {
		fmt.Fprintf(b, " 📁 \033[1;36mRegistered Workspaces · Filter: \"%s\" (%d/%d matches):\033[0m\033[K\r\n\033[K\r\n",
			a.searchQuery, len(registered), len(a.registeredCache))
	} else {
		fmt.Fprintf(b, " 📁 \033[1;36mRegistered Workspaces (%d total):\033[0m\033[K\r\n\033[K\r\n", len(registered))
	}

	for i := startIdx; i < endIdx; i++ {
		p := registered[i]
		cursor := "  "
		highlightStart := ""
		highlightEnd := ""
		if i == a.SelectedIndex {
			cursor = "\033[1;32m▶ \033[0m"
			highlightStart = "\033[1;37;44m"
			highlightEnd = "\033[0m"
		}

		activeMarker := "  "
		if p.IsActive {
			activeMarker = "\033[1;32m●\033[0m "
		}

		pinMarker := "  "
		if p.IsPinned {
			pinMarker = "\033[1;33m★\033[0m "
		}

		gitStr := "\033[37m" + p.GitBranch + "\033[0m"
		if p.IsDirty {
			gitStr = fmt.Sprintf("\033[33m%s · ●%d\033[0m", p.GitBranch, p.DirtyCount)
		}

		sessStr := ""
		if p.SessionCount > 0 {
			sessStr = fmt.Sprintf(" \033[36m(%d sess · $%0.2f)\033[0m", p.SessionCount, p.TotalCost)
		}

		if width < 85 {
			name := truncateString(p.Name, 20)
			stack := truncateString(p.Stack, 16)
			fmt.Fprintf(b, "%s%s%s%s%d. \033[1m%-20s\033[0m \033[35m[%-16s]\033[0m %s%s\033[K\r\n",
				cursor, highlightStart, activeMarker, pinMarker, i+1, name, stack, gitStr, highlightEnd)
		} else {
			stackBadge := truncateString(p.Stack, 22)
			name := truncateString(p.Name, 22)
			fmt.Fprintf(b, "%s%s%s%s%2d. \033[1m%-22s\033[0m \033[35m[%-22s]\033[0m %-16s%s%s\033[K\r\n",
				cursor, highlightStart, activeMarker, pinMarker, i+1, name, stackBadge, gitStr, sessStr, highlightEnd)
		}
	}

	if a.searchQuery != "" {
		fmt.Fprintf(b, "\033[K\r\n \033[37m[Page %d/%d · %d-%d of %d · [/] Filter · [Esc] Clear · [Enter] Open in IDE · [T] Shell · [P] Pin]\033[0m\033[K\r\n",
			page+1, totalPages, startIdx+1, endIdx, len(registered))
	} else {
		fmt.Fprintf(b, "\033[K\r\n \033[37m[Page %d/%d · %d-%d of %d workspaces · [/] Filter · [Enter] Open in IDE · [T] Shell · [P] Pin · [A] Agy]\033[0m\033[K\r\n",
			page+1, totalPages, startIdx+1, endIdx, len(registered))
	}
}

func (a *App) renderDiscoverTab(b *strings.Builder, discovered []model.ProjectInfo, width int, height int) {
	if len(discovered) == 0 {
		fmt.Fprintf(b, " \033[33mNo subdirectories found under '%s'\033[0m\033[K\r\n", a.ScanRootDir)
		return
	}

	pageSize := height - 12
	if pageSize < 4 {
		pageSize = 4
	}
	if pageSize > 10 {
		pageSize = 10
	}

	page := a.SelectedIndex / pageSize
	startIdx := page * pageSize
	endIdx := startIdx + pageSize
	if endIdx > len(discovered) {
		endIdx = len(discovered)
	}
	totalPages := (len(discovered) + pageSize - 1) / pageSize
	if totalPages == 0 {
		totalPages = 1
	}

	fmt.Fprintf(b, " 🔍 \033[1;36mDiscovered Projects in '%s' (%d total):\033[0m\033[K\r\n\033[K\r\n", a.ScanRootDir, len(discovered))

	for i := startIdx; i < endIdx; i++ {
		p := discovered[i]
		cursor := "  "
		highlightStart := ""
		highlightEnd := ""
		if i == a.SelectedIndex {
			cursor = "\033[1;32m▶ \033[0m"
			highlightStart = "\033[1;37;44m"
			highlightEnd = "\033[0m"
		}

		statusBadge := "\033[36m[+ New]     \033[0m"
		if p.IsPinned || p.IsActive {
			statusBadge = "\033[32m[Registered]\033[0m"
		}

		if width < 85 {
			name := truncateString(p.Name, 20)
			stack := truncateString(p.Stack, 16)
			fmt.Fprintf(b, "%s%s%2d. %s \033[1m%-20s\033[0m \033[35m[%s]\033[0m%s\033[K\r\n",
				cursor, highlightStart, i+1, statusBadge, name, stack, highlightEnd)
		} else {
			stackBadge := truncateString(p.Stack, 24)
			name := truncateString(p.Name, 24)
			fmt.Fprintf(b, "%s%s%2d. %s \033[1m%-24s\033[0m \033[35m[%-24s]\033[0m \033[37m%s\033[0m%s\033[K\r\n",
				cursor, highlightStart, i+1, statusBadge, name, stackBadge, truncateString(p.GitBranch, 14), highlightEnd)
		}
	}

	fmt.Fprintf(b, "\033[K\r\n \033[37m[Page %d/%d · %d-%d of %d discovered · [Enter] Register · [A] Register All]\033[0m\033[K\r\n",
		page+1, totalPages, startIdx+1, endIdx, len(discovered))
}

func (a *App) renderIdesTab(b *strings.Builder, width int) {
	cfg, _ := a.Registry.Load()
	curDefault := "code"
	if cfg != nil && cfg.DefaultIDE != "" {
		curDefault = cfg.DefaultIDE
	}

	b.WriteString(" ⚙️ \033[1;36mConfigure Default Editor & Environment:\033[0m\033[K\r\n\033[K\r\n")

	options := []struct {
		Key  string
		Name string
		Desc string
	}{
		{"code", "Visual Studio Code", "Default cross-platform IDE ('code <dir>')"},
		{"cursor", "Cursor AI Editor", "AI-native code editor ('cursor <dir>')"},
		{"nvim", "Neovim / Vim", "Fast modal terminal editor in project dir ('nvim .')"},
		{"agy", "Antigravity CLI Agent", "Launch interactive Antigravity coding agent in workspace ('agy')"},
	}

	for i, opt := range options {
		cursor := "  "
		highlightStart := ""
		highlightEnd := ""
		if i == a.SelectedIndex {
			cursor = "\033[1;32m▶ \033[0m"
			highlightStart = "\033[1;37;44m"
			highlightEnd = "\033[0m"
		}

		selectedMarker := "  "
		if strings.EqualFold(opt.Key, curDefault) {
			selectedMarker = "\033[1;32m(Default) \033[0m"
		}

		if width < 85 {
			fmt.Fprintf(b, "%s%s%d. \033[1m%-18s\033[0m %s%s\033[K\r\n", cursor, highlightStart, i+1, opt.Name, selectedMarker, highlightEnd)
		} else {
			fmt.Fprintf(b, "%s%s%d. \033[1m%-22s\033[0m %-12s \033[37m%s\033[0m%s\033[K\r\n",
				cursor, highlightStart, i+1, opt.Name, selectedMarker, opt.Desc, highlightEnd)
		}
	}
}

func (a *App) runNonInteractive() error {
	a.PrintList(os.Stdout)
	return nil
}

func (a *App) PrintList(w io.Writer) {
	list := a.Registry.ListRegistered()
	fmt.Fprintln(w, "\n📁 \033[1;36mAGYPROJ - Registered Workspaces\033[0m")
	fmt.Fprintln(w, "──────────────────────────────────────────────────────────────────────────────────")
	for i, p := range list {
		activeMarker := "  "
		if p.IsActive {
			activeMarker = "● "
		}
		pinMarker := "  "
		if p.IsPinned {
			pinMarker = "★ "
		}
		fmt.Fprintf(w, " %s%s%2d. \033[1m%-24s\033[0m [%-20s] (%s)\n",
			activeMarker, pinMarker, i+1, p.Name, p.Stack, p.Path)
	}
	fmt.Fprintln(w, "──────────────────────────────────────────────────────────────────────────────────")
}
