package view

import (
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"os"
	"strconv"
	"strings"
	"sync"
	"time"

	"golang.org/x/term"

	"agyport/internal/model"
	"agyport/internal/service/portops"
	"agyport/internal/service/ramops"
	"agyport/internal/web"
)

type App struct {
	ActiveTab     int // 0: Ports, 1: RAM & Suggestions, 2: Process Inspector
	SelectedIndex int
	StatusMsg     string
	FilterQuery   string
	isFiltering   bool

	cachedPorts       []model.PortInfo
	cachedMem         *model.MemorySummary
	cachedTopProcs    []model.ProcessMemInfo
	cachedSuggestions []model.RAMSuggestion

	mu          sync.RWMutex
	isReloading bool
	tabSwitched bool
}

func NewApp() *App {
	return &App{
		ActiveTab:   0,
		tabSwitched: true,
	}
}

// ReloadData synchronously refreshes all system port and RAM data
func (a *App) ReloadData() {
	ports, _ := portops.ListPorts()
	mem, _ := ramops.GetMemorySummary()
	procs, _ := ramops.GetTopMemoryProcesses(25)
	suggestions := ramops.GenerateRAMSuggestions(ports, procs, mem)

	a.mu.Lock()
	a.cachedPorts = ports
	a.cachedMem = mem
	a.cachedTopProcs = procs
	a.cachedSuggestions = suggestions
	a.mu.Unlock()
}

// ReloadAsync refreshes data in background
func (a *App) ReloadAsync() {
	a.mu.Lock()
	if a.isReloading {
		a.mu.Unlock()
		return
	}
	a.isReloading = true
	a.mu.Unlock()

	go func() {
		ports, _ := portops.ListPorts()
		mem, _ := ramops.GetMemorySummary()
		procs, _ := ramops.GetTopMemoryProcesses(25)
		suggestions := ramops.GenerateRAMSuggestions(ports, procs, mem)

		a.mu.Lock()
		a.cachedPorts = ports
		a.cachedMem = mem
		a.cachedTopProcs = procs
		a.cachedSuggestions = suggestions
		a.isReloading = false
		a.mu.Unlock()
	}()
}

// RunInteractive enters full-screen VT100 interactive TUI
func (a *App) RunInteractive() error {
	fd := int(os.Stdin.Fd())
	if !term.IsTerminal(fd) {
		a.PrintStatus(os.Stdout)
		return nil
	}

	oldState, err := term.MakeRaw(fd)
	if err != nil {
		a.PrintStatus(os.Stdout)
		return nil
	}

	defer func() {
		fmt.Print("\033[?25h\033[?1049l") // Show cursor, exit alt buffer
		_ = term.Restore(fd, oldState)
	}()

	fmt.Print("\033[?1049h\033[?25l") // Enter alt buffer, hide cursor
	a.ReloadData()

	for {
		a.mu.RLock()
		ports := a.filteredPorts()
		mem := a.cachedMem
		suggestions := a.cachedSuggestions
		procs := a.cachedTopProcs
		a.mu.RUnlock()

		a.Render(ports, mem, suggestions, procs)

		// Poll input with timeout for live background refresh
		if !waitKey(fd, 800) {
			a.ReloadAsync()
			continue
		}

		var buf [32]byte
		n, err := os.Stdin.Read(buf[:])
		if err != nil || n == 0 {
			break
		}

		ch := buf[0]

		// 1. Unconditional SIGINT (Ctrl+C) or EOF (Ctrl+D) -> Always exit immediately!
		if ch == 0x03 || ch == 0x04 {
			return nil
		}

		// 2. Solitary Esc key handling
		if ch == 0x1b && n == 1 {
			if a.isFiltering {
				a.isFiltering = false
				a.FilterQuery = ""
				a.SelectedIndex = 0
				a.tabSwitched = true
				continue
			}
			// When not filtering, solitary Esc exits cleanly!
			return nil
		}

		// 3. Handle ANSI Escape sequences (arrows, tab switching)
		if n >= 3 && buf[0] == 0x1b && buf[1] == '[' {
			switch buf[2] {
			case 'A': // Up arrow
				if a.SelectedIndex > 0 {
					a.SelectedIndex--
				}
				continue
			case 'B': // Down arrow
				maxIdx := len(ports) - 1
				if a.ActiveTab == 1 {
					maxIdx = len(suggestions) - 1
				} else if a.ActiveTab == 2 {
					maxIdx = len(procs) - 1
				}
				if a.SelectedIndex < maxIdx {
					a.SelectedIndex++
				}
				continue
			case 'C': // Right arrow -> Next tab
				a.ActiveTab = (a.ActiveTab + 1) % 3
				a.SelectedIndex = 0
				a.tabSwitched = true
				continue
			case 'D': // Left arrow -> Prev tab
				a.ActiveTab = (a.ActiveTab + 2) % 3
				a.SelectedIndex = 0
				a.tabSwitched = true
				continue
			}
		}

		// 4. Filter typing mode
		if a.isFiltering {
			if ch == '\r' || ch == '\n' { // Enter finishes filter
				a.isFiltering = false
			} else if ch == 0x7f || ch == 0x08 { // Backspace
				if len(a.FilterQuery) > 0 {
					a.FilterQuery = a.FilterQuery[:len(a.FilterQuery)-1]
					a.SelectedIndex = 0
				}
			} else if ch >= 32 && ch <= 126 {
				a.FilterQuery += string(ch)
				a.SelectedIndex = 0
			}
			continue
		}

		// 5. Normal key bindings
		switch ch {
		case 'q', 'Q': // Clean Quit
			return nil

		case '\t': // Tab switch
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

		case 'w', 'W': // Launch Web UI Dashboard & open default browser
			go func() {
				client := http.Client{Timeout: 300 * time.Millisecond}
				resp, err := client.Get("http://127.0.0.1:5999/api/stats")
				if err == nil && resp.StatusCode == 200 {
					_ = resp.Body.Close()
					web.OpenBrowser("http://127.0.0.1:5999")
				} else {
					go func() {
						_ = web.StartServer(5999, true)
					}()
				}
			}()
			a.StatusMsg = "\033[32m✔ Launched Web Dashboard on http://127.0.0.1:5999 (opening browser)\033[0m"

		case 'k': // Graceful kill
			if a.ActiveTab == 0 && len(ports) > 0 && a.SelectedIndex < len(ports) {
				p := ports[a.SelectedIndex]
				res, err := portops.KillPort(p.Port, false)
				if err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31m✖ Error: %v\033[0m", err)
				} else if res.Success {
					a.StatusMsg = fmt.Sprintf("\033[32m✔ Terminated %s on port %d (PID %d)\033[0m", res.ProcessName, res.Port, res.PID)
					a.ReloadData()
					a.tabSwitched = true
				}
			} else if a.ActiveTab == 1 && len(suggestions) > 0 && a.SelectedIndex < len(suggestions) {
				sug := suggestions[a.SelectedIndex]
				a.executeSuggestion(sug)
			}

		case 'K', 'f', 'F': // Force kill
			if a.ActiveTab == 0 && len(ports) > 0 && a.SelectedIndex < len(ports) {
				p := ports[a.SelectedIndex]
				res, err := portops.KillPort(p.Port, true)
				if err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31m✖ Error: %v\033[0m", err)
				} else if res.Success {
					a.StatusMsg = fmt.Sprintf("\033[33m⚡ Force-killed %s on port %d (PID %d)\033[0m", res.ProcessName, res.Port, res.PID)
					a.ReloadData()
					a.tabSwitched = true
				}
			}

		case 'a', 'A': // Kill all dev ports modal
			if a.ActiveTab == 0 {
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Print("\r\n\033[1;33m⚠️  Are you sure you want to terminate ALL running dev server ports? (y/N): \033[0m")
				var confirm string
				fmt.Scanln(&confirm)
				if strings.EqualFold(strings.TrimSpace(confirm), "y") {
					results, _ := portops.KillAllDevPorts(false)
					killed := 0
					for _, r := range results {
						if r.Success {
							killed++
						}
					}
					a.StatusMsg = fmt.Sprintf("\033[32m✔ Terminated %d dev server processes\033[0m", killed)
					a.ReloadData()
				}
				newOld, _ := term.MakeRaw(fd)
				*oldState = *newOld
				fmt.Print("\033[?1049h\033[?25l")
				a.tabSwitched = true
			}

		case 'c', 'C': // One-click Dev RAM Reclaim
			res, err := ramops.ReclaimDevRAM(false)
			if err != nil {
				a.StatusMsg = fmt.Sprintf("\033[31m✖ Reclaim error: %v\033[0m", err)
			} else {
				a.StatusMsg = fmt.Sprintf("\033[32m✔ Reclaimed %s RAM across %d dev processes\033[0m", res.FreedFormatted, len(res.KilledPIDs))
				a.ReloadData()
				a.tabSwitched = true
			}

		case '/': // Filter search
			a.isFiltering = true
			a.StatusMsg = "\033[36mType filter text (Enter/Esc to exit)...\033[0m"

		case 'r', 'R': // Refresh
			a.ReloadData()
			a.StatusMsg = "\033[32m✔ Refreshed ports & memory\033[0m"
			a.tabSwitched = true

		case 'i', '\r', '\n': // Inspector Modal
			a.showDetailModal(fd, oldState, ports, procs)
		}
	}

	return nil
}

func (a *App) executeSuggestion(sug model.RAMSuggestion) {
	if sug.ActionType == "kill_dev" {
		res, _ := ramops.ReclaimDevRAM(false)
		a.StatusMsg = fmt.Sprintf("\033[32m✔ Reclaimed %s RAM (%d processes)\033[0m", res.FreedFormatted, len(res.KilledPIDs))
	} else if len(sug.TargetPIDs) > 0 {
		for _, pid := range sug.TargetPIDs {
			_ = portops.KillPID(pid, false)
		}
		a.StatusMsg = fmt.Sprintf("\033[32m✔ Terminated PIDs %v (Freed %s)\033[0m", sug.TargetPIDs, sug.ReclaimableFormatted)
	}
	a.ReloadData()
	a.tabSwitched = true
}

func (a *App) filteredPorts() []model.PortInfo {
	if a.FilterQuery == "" {
		return a.cachedPorts
	}
	q := strings.ToLower(a.FilterQuery)
	var filtered []model.PortInfo
	for _, p := range a.cachedPorts {
		if strings.Contains(strconv.Itoa(p.Port), q) ||
			strings.Contains(strings.ToLower(p.ProcessName), q) ||
			strings.Contains(strings.ToLower(p.Framework), q) ||
			strings.Contains(strings.ToLower(p.CommandLine), q) ||
			strings.Contains(strings.ToLower(p.DevCategory), q) {
			filtered = append(filtered, p)
		}
	}
	return filtered
}

// getTermSize gets active terminal dimensions with sensible defaults
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

func hr(width int) string {
	w := width - 1
	if w < 20 {
		w = 20
	}
	return strings.Repeat("─", w) + "\033[K\r\n"
}

func hrDotted(width int) string {
	w := width - 1
	if w < 20 {
		w = 20
	}
	return strings.Repeat("┈", w) + "\033[K\r\n"
}

func truncateStr(s string, maxLen int) string {
	if maxLen <= 0 {
		return ""
	}
	runes := []rune(s)
	if len(runes) <= maxLen {
		return s
	}
	if maxLen == 1 {
		return "…"
	}
	return string(runes[:maxLen-1]) + "…"
}

// Render writes the cockpit screen without scrolling or ghost text
func (a *App) Render(ports []model.PortInfo, mem *model.MemorySummary, suggestions []model.RAMSuggestion, procs []model.ProcessMemInfo) {
	width, height := getTermSize()
	var b strings.Builder
	b.Grow(4096)

	if a.tabSwitched {
		b.WriteString("\033[H\033[2J")
		a.tabSwitched = false
	} else {
		b.WriteString("\033[H")
	}

	// 1. Header & Live System Gauge
	if width < 75 {
		b.WriteString("\033[1;36m⚡ AGYPORT\033[0m \033[90m|\033[0m \033[1mPort & RAM Manager\033[0m\033[K\r\n")
	} else {
		b.WriteString("\033[1;36m⚡ AGYPORT\033[0m \033[90m|\033[0m \033[1mPort Manager & RAM Optimizer (Go Engine)\033[0m\033[K\r\n")
	}

	if mem != nil {
		gaugeLen := 14
		if width >= 95 {
			gaugeLen = 18
		} else if width < 70 {
			gaugeLen = 8
		}
		ramGauge := renderProgressBar(mem.UsedPercent, gaugeLen)

		if width >= 90 && mem.SwapTotalBytes > 0 {
			swapGauge := renderProgressBar(mem.SwapUsedPercent, 8)
			b.WriteString(fmt.Sprintf("🧠 \033[1mRAM:\033[0m %s \033[1m%.1f%%\033[0m (%s / %s, \033[32m%s avail\033[0m) \033[90m|\033[0m \033[1mSwap:\033[0m %s %.1f%%\033[K\r\n",
				ramGauge, mem.UsedPercent, mem.UsedFormatted, mem.TotalFormatted, mem.AvailableFormatted, swapGauge, mem.SwapUsedPercent))
		} else {
			b.WriteString(fmt.Sprintf("🧠 \033[1mRAM:\033[0m %s \033[1m%.1f%%\033[0m (%s / %s, \033[32m%s avail\033[0m)\033[K\r\n",
				ramGauge, mem.UsedPercent, mem.UsedFormatted, mem.TotalFormatted, mem.AvailableFormatted))
		}
	} else {
		b.WriteString("\033[K\r\n")
	}

	// 2. Navigation Tabs
	var tabs []string
	if width < 80 {
		tabs = []string{
			fmt.Sprintf("[1] 🌐 Ports (%d)", len(ports)),
			fmt.Sprintf("[2] 🧠 RAM (%d)", len(suggestions)),
			fmt.Sprintf("[3] 🔍 Procs (%d)", len(procs)),
		}
	} else {
		tabs = []string{
			fmt.Sprintf("[1] 🌐 Ports Cockpit (%d)", len(ports)),
			fmt.Sprintf("[2] 🧠 RAM Leverage & Suggestions (%d)", len(suggestions)),
			fmt.Sprintf("[3] 🔍 Process Inspector (%d)", len(procs)),
		}
	}

	for i, t := range tabs {
		if i == a.ActiveTab {
			b.WriteString(fmt.Sprintf("\033[1;44;37m %s \033[0m ", t))
		} else {
			b.WriteString(fmt.Sprintf("\033[2m %s \033[0m ", t))
		}
	}
	b.WriteString("\033[K\r\n")
	b.WriteString(hr(width))

	// Calculate vertical row limit to prevent screen overflow and viewport scrolling
	maxRows := height - 10
	if maxRows < 4 {
		maxRows = 4
	}
	if maxRows > 22 {
		maxRows = 22
	}

	// 3. Tab Body
	switch a.ActiveTab {
	case 0:
		a.renderPortsTab(&b, ports, width, maxRows)
	case 1:
		a.renderRAMTab(&b, mem, suggestions, procs, width, maxRows)
	case 2:
		a.renderProcessTab(&b, procs, width, maxRows)
	}

	// 4. Status Bar / Search Bar
	b.WriteString(hr(width))
	if a.isFiltering {
		b.WriteString(fmt.Sprintf("\033[1;33m🔍 Filter: \033[0m%s\033[5m_\033[0m\033[K\r\n", a.FilterQuery))
	} else if a.StatusMsg != "" {
		b.WriteString(fmt.Sprintf("%s\033[K\r\n", a.StatusMsg))
	} else if a.FilterQuery != "" {
		b.WriteString(fmt.Sprintf("\033[90mFilter active: '%s' (Press Esc to clear)\033[0m\033[K\r\n", a.FilterQuery))
	} else {
		b.WriteString("\033[90mReady. Select port or process and press hotkey.\033[0m\033[K\r\n")
	}

	// 5. Hotkeys Footer
	if width < 85 {
		b.WriteString("\033[1mKeys:\033[0m [↑/↓]Move \033[31m[k]\033[0mKill \033[33m[a]\033[0mKillAll \033[32m[c]\033[0mReclaim \033[36m[w]\033[0mWeb \033[36m[/]\033[0mFind \033[35m[Tab]\033[0mTab \033[31m[q/Esc]\033[0mQuit\033[K")
	} else {
		b.WriteString("\033[1mHotkeys:\033[0m [↑/↓] Move  \033[31m[k]\033[0m Kill  \033[31m[K/f]\033[0m Force  \033[33m[a]\033[0m Kill-All  \033[32m[c]\033[0m Reclaim  \033[36m[w]\033[0m Web UI  \033[36m[/]\033[0m Filter  [i/↵] Info  [Tab] Switch  \033[31m[q/Esc]\033[0m Quit\033[K")
	}

	// Erase below cursor to clear any remnants
	b.WriteString("\033[J")

	fmt.Print(b.String())
}

func (a *App) renderPortsTab(b *strings.Builder, ports []model.PortInfo, width int, maxRows int) {
	if width >= 90 {
		b.WriteString(fmt.Sprintf("\033[1m %-7s %-5s %-7s %-7s %-16s %-16s %-12s %-10s\033[0m\033[K\r\n",
			"PORT", "PROTO", "STATE", "PID", "PROCESS", "FRAMEWORK", "RAM (MB/%)", "USER"))
	} else {
		b.WriteString(fmt.Sprintf("\033[1m %-6s %-5s %-6s %-15s %-14s %-11s\033[0m\033[K\r\n",
			"PORT", "PROTO", "PID", "PROCESS", "FRAMEWORK", "RAM"))
	}
	b.WriteString(hrDotted(width))

	if len(ports) == 0 {
		b.WriteString("  \033[33mNo listening network ports found matching query.\033[0m\033[K\r\n")
		for i := 1; i < maxRows; i++ {
			b.WriteString("\033[K\r\n")
		}
		return
	}

	start := 0
	if a.SelectedIndex >= maxRows {
		start = a.SelectedIndex - maxRows + 1
	}
	end := start + maxRows
	if end > len(ports) {
		end = len(ports)
	}

	for i := start; i < end; i++ {
		p := ports[i]
		pointer := "  "
		lineColor := "\033[0m"

		if i == a.SelectedIndex {
			pointer = "\033[1;36m▶ \033[0m"
			lineColor = "\033[1;7m" // invert
		}

		protoCol := "\033[32m" + strings.ToUpper(p.Protocol) + "\033[0m"
		if p.Protocol == "udp" {
			protoCol = "\033[33mUDP\033[0m"
		}

		pidStr := strconv.Itoa(p.PID)
		if p.PID <= 0 {
			pidStr = "-"
		}

		ramStr := "-"
		if p.MemoryBytes > 0 {
			if width >= 90 {
				ramStr = fmt.Sprintf("%s (%.1f%%)", p.MemoryFormatted, p.MemoryPercent)
			} else {
				ramStr = p.MemoryFormatted
			}
		}

		portCol := "\033[1;36m"
		if p.IsSystemPort {
			portCol = "\033[90m"
		} else if p.DevCategory == "Database" {
			portCol = "\033[33m"
		}

		if width >= 90 {
			procName := truncateStr(p.ProcessName, 16)
			framework := truncateStr(p.Framework, 16)
			user := truncateStr(p.User, 10)
			if i == a.SelectedIndex {
				b.WriteString(fmt.Sprintf("%s%s %-7d %-5s %-7s %-7s %-16s %-16s %-12s %-10s\033[0m\033[K\r\n",
					pointer, lineColor, p.Port, strings.ToUpper(p.Protocol), p.State, pidStr, procName, framework, ramStr, user))
			} else {
				b.WriteString(fmt.Sprintf("%s%s%-7d\033[0m %-5s %-7s %-7s %-16s %-16s %-12s %-10s\033[K\r\n",
					pointer, portCol, p.Port, protoCol, p.State, pidStr, procName, framework, ramStr, user))
			}
		} else {
			procName := truncateStr(p.ProcessName, 15)
			framework := truncateStr(p.Framework, 14)
			if i == a.SelectedIndex {
				b.WriteString(fmt.Sprintf("%s%s %-6d %-5s %-6s %-15s %-14s %-11s\033[0m\033[K\r\n",
					pointer, lineColor, p.Port, strings.ToUpper(p.Protocol), pidStr, procName, framework, ramStr))
			} else {
				b.WriteString(fmt.Sprintf("%s%s%-6d\033[0m %-5s %-6s %-15s %-14s %-11s\033[K\r\n",
					pointer, portCol, p.Port, protoCol, pidStr, procName, framework, ramStr))
			}
		}
	}

	for i := end - start; i < maxRows; i++ {
		b.WriteString("\033[K\r\n")
	}
}

func (a *App) renderRAMTab(b *strings.Builder, mem *model.MemorySummary, suggestions []model.RAMSuggestion, procs []model.ProcessMemInfo, width int, maxRows int) {
	b.WriteString("\033[1;33m💡 SMART RAM LEVERAGE SUGGESTIONS:\033[0m\033[K\r\n")

	renderedLines := 1
	if len(suggestions) == 0 {
		b.WriteString("  \033[32m✔ RAM usage is optimal! No memory hogs or dangling dev servers detected.\033[0m\033[K\r\n")
		renderedLines++
	} else {
		maxSugs := 3
		if len(suggestions) < maxSugs {
			maxSugs = len(suggestions)
		}
		for i := 0; i < maxSugs; i++ {
			s := suggestions[i]
			pointer := "  "
			if i == a.SelectedIndex {
				pointer = "\033[1;36m▶ \033[0m"
			}
			title := truncateStr(s.Title, width-30)
			b.WriteString(fmt.Sprintf("%s\033[1m[%d] %s\033[0m \033[32m(+%s reclaimable)\033[0m\033[K\r\n",
				pointer, i+1, title, s.ReclaimableFormatted))
			renderedLines++

			desc := truncateStr(s.Description, width-12)
			b.WriteString(fmt.Sprintf("     \033[90m%s\033[0m\033[K\r\n", desc))
			renderedLines++
		}
	}

	b.WriteString(hrDotted(width))
	renderedLines++
	b.WriteString("\033[1m🔥 TOP MEMORY CONSUMING PROCESSES:\033[0m\033[K\r\n")
	renderedLines++

	if width >= 90 {
		b.WriteString(fmt.Sprintf("\033[1m %-7s %-18s %-12s %-10s %-12s %-20s\033[0m\033[K\r\n",
			"PID", "PROCESS", "RAM (MB/%)", "DEV SERVER", "PORTS", "USER"))
	} else {
		b.WriteString(fmt.Sprintf("\033[1m %-6s %-16s %-12s %-10s %-10s\033[0m\033[K\r\n",
			"PID", "PROCESS", "RAM", "DEV SERVER", "PORTS"))
	}
	renderedLines++

	procLimit := maxRows - renderedLines
	if procLimit < 2 {
		procLimit = 2
	}
	if procLimit > len(procs) {
		procLimit = len(procs)
	}

	for i := 0; i < procLimit; i++ {
		p := procs[i]
		devTag := "\033[90mNo\033[0m"
		if p.IsDevServer {
			devTag = "\033[1;32mYes (" + truncateStr(p.Framework, 6) + ")\033[0m"
		}

		portsStr := "-"
		if len(p.Ports) > 0 {
			var strList []string
			for _, port := range p.Ports {
				strList = append(strList, strconv.Itoa(port))
			}
			portsStr = strings.Join(strList, ",")
		}
		portsStr = truncateStr(portsStr, 10)

		if width >= 90 {
			name := truncateStr(p.Name, 18)
			user := truncateStr(p.User, 20)
			ramStr := fmt.Sprintf("%s (%.1f%%)", p.RSSFormatted, p.RSSPercent)
			b.WriteString(fmt.Sprintf(" %-7d %-18s \033[1;31m%-12s\033[0m %-10s %-12s %-20s\033[K\r\n",
				p.PID, name, ramStr, devTag, portsStr, user))
		} else {
			name := truncateStr(p.Name, 16)
			ramStr := p.RSSFormatted
			b.WriteString(fmt.Sprintf(" %-6d %-16s \033[1;31m%-12s\033[0m %-10s %-10s\033[K\r\n",
				p.PID, name, ramStr, devTag, portsStr))
		}
		renderedLines++
	}

	for i := renderedLines; i < maxRows; i++ {
		b.WriteString("\033[K\r\n")
	}
}

func (a *App) renderProcessTab(b *strings.Builder, procs []model.ProcessMemInfo, width int, maxRows int) {
	if width >= 90 {
		b.WriteString(fmt.Sprintf("\033[1m %-7s %-18s %-14s %-12s %-10s %-20s\033[0m\033[K\r\n",
			"PID", "PROCESS", "RAM (RSS)", "FRAMEWORK", "PORTS", "USER"))
	} else {
		b.WriteString(fmt.Sprintf("\033[1m %-6s %-16s %-12s %-12s %-10s\033[0m\033[K\r\n",
			"PID", "PROCESS", "RAM (RSS)", "FRAMEWORK", "PORTS"))
	}
	b.WriteString(hrDotted(width))

	if len(procs) == 0 {
		b.WriteString("  \033[33mNo active user processes found.\033[0m\033[K\r\n")
		for i := 1; i < maxRows; i++ {
			b.WriteString("\033[K\r\n")
		}
		return
	}

	start := 0
	if a.SelectedIndex >= maxRows {
		start = a.SelectedIndex - maxRows + 1
	}
	end := start + maxRows
	if end > len(procs) {
		end = len(procs)
	}

	for i := start; i < end; i++ {
		p := procs[i]
		pointer := "  "
		lineColor := "\033[0m"

		if i == a.SelectedIndex {
			pointer = "\033[1;36m▶ \033[0m"
			lineColor = "\033[1;7m"
		}

		portsStr := "-"
		if len(p.Ports) > 0 {
			var strList []string
			for _, port := range p.Ports {
				strList = append(strList, strconv.Itoa(port))
			}
			portsStr = strings.Join(strList, ",")
		}
		portsStr = truncateStr(portsStr, 10)

		if width >= 90 {
			name := truncateStr(p.Name, 18)
			framework := truncateStr(p.Framework, 12)
			user := truncateStr(p.User, 20)
			b.WriteString(fmt.Sprintf("%s%s%-7d %-18s %-14s %-12s %-10s %-20s\033[0m\033[K\r\n",
				pointer, lineColor, p.PID, name, p.RSSFormatted, framework, portsStr, user))
		} else {
			name := truncateStr(p.Name, 16)
			framework := truncateStr(p.Framework, 12)
			b.WriteString(fmt.Sprintf("%s%s%-6d %-16s %-12s %-12s %-10s\033[0m\033[K\r\n",
				pointer, lineColor, p.PID, name, p.RSSFormatted, framework, portsStr))
		}
	}

	for i := end - start; i < maxRows; i++ {
		b.WriteString("\033[K\r\n")
	}
}

func (a *App) showDetailModal(fd int, oldState *term.State, ports []model.PortInfo, procs []model.ProcessMemInfo) {
	fmt.Print("\033[H\033[2J") // Clear screen
	width, _ := getTermSize()
	hrLine := hr(width)
	fmt.Print("\r\n\033[1;36m" + hrLine + "\033[0m")
	fmt.Print("\033[1;36m  DETAILED PROCESS INSPECTION\033[0m\033[K\r\n")
	fmt.Print("\033[1;36m" + hrLine + "\033[0m\r\n")

	if a.ActiveTab == 0 && len(ports) > 0 && a.SelectedIndex < len(ports) {
		p := ports[a.SelectedIndex]
		fmt.Printf("  \033[1mPort:\033[0m            %d (%s, %s)\033[K\r\n", p.Port, strings.ToUpper(p.Protocol), p.State)
		fmt.Printf("  \033[1mBind Address:\033[0m    %s\033[K\r\n", p.BindAddress)
		fmt.Printf("  \033[1mPID:\033[0m             %d\033[K\r\n", p.PID)
		fmt.Printf("  \033[1mProcess Name:\033[0m    %s\033[K\r\n", p.ProcessName)
		fmt.Printf("  \033[1mUser:\033[0m            %s\033[K\r\n", p.User)
		fmt.Printf("  \033[1mFramework:\033[0m       %s (%s)\033[K\r\n", p.Framework, p.DevCategory)
		fmt.Printf("  \033[1mRAM Usage:\033[0m       %s (%.2f%% of total host memory)\033[K\r\n", p.MemoryFormatted, p.MemoryPercent)
		fmt.Printf("  \033[1mSystem Protected:\033[0m %v\033[K\r\n", p.IsSystemPort)
		fmt.Printf("  \033[1mCommand Line:\033[0m\033[K\r\n    \033[33m%s\033[0m\033[K\r\n", truncateStr(p.CommandLine, width-6))
	} else if (a.ActiveTab == 1 || a.ActiveTab == 2) && len(procs) > 0 && a.SelectedIndex < len(procs) {
		p := procs[a.SelectedIndex]
		fmt.Printf("  \033[1mPID:\033[0m             %d\033[K\r\n", p.PID)
		fmt.Printf("  \033[1mProcess Name:\033[0m    %s\033[K\r\n", p.Name)
		fmt.Printf("  \033[1mUser:\033[0m            %s\033[K\r\n", p.User)
		fmt.Printf("  \033[1mRAM Usage (RSS):\033[0m %s (%.2f%% of total memory)\033[K\r\n", p.RSSFormatted, p.RSSPercent)
		fmt.Printf("  \033[1mIs Dev Server:\033[0m   %v (%s)\033[K\r\n", p.IsDevServer, p.Framework)
		fmt.Printf("  \033[1mListening Ports:\033[0m %v\033[K\r\n", p.Ports)
		fmt.Printf("  \033[1mFull Command:\033[0m\033[K\r\n    \033[33m%s\033[0m\033[K\r\n", truncateStr(p.Cmdline, width-6))
	}

	fmt.Print("\r\n\033[1mPress any key to return...\033[0m\033[K\r\n")
	var dummy [1]byte
	_, _ = os.Stdin.Read(dummy[:])
	a.tabSwitched = true
}

// PrintStatus outputs standard terminal overview (CLI mode)
func (a *App) PrintStatus(w io.Writer) {
	ports, _ := portops.ListPorts()
	mem, _ := ramops.GetMemorySummary()

	fmt.Fprintln(w, "⚡ AGYPORT - Live Network Ports & Memory Status")
	if mem != nil {
		fmt.Fprintf(w, "🧠 RAM: %s / %s (%.1f%% used, %s available)\n",
			mem.UsedFormatted, mem.TotalFormatted, mem.UsedPercent, mem.AvailableFormatted)
	}
	fmt.Fprintln(w, strings.Repeat("─", 80))
	fmt.Fprintf(w, "%-7s %-5s %-7s %-7s %-16s %-16s %-12s\n",
		"PORT", "PROTO", "STATE", "PID", "PROCESS", "FRAMEWORK", "RAM")
	fmt.Fprintln(w, strings.Repeat("┈", 80))

	for _, p := range ports {
		pidStr := strconv.Itoa(p.PID)
		if p.PID <= 0 {
			pidStr = "-"
		}
		ramStr := "-"
		if p.MemoryBytes > 0 {
			ramStr = p.MemoryFormatted
		}
		fmt.Fprintf(w, "%-7d %-5s %-7s %-7s %-16s %-16s %-12s\n",
			p.Port, strings.ToUpper(p.Protocol), p.State, pidStr, p.ProcessName, p.Framework, ramStr)
	}
}

// PrintJSON prints ports or memory in JSON format
func PrintJSON(w io.Writer, v any) error {
	enc := json.NewEncoder(w)
	enc.SetIndent("", "  ")
	return enc.Encode(v)
}

func renderProgressBar(pct float64, width int) string {
	if pct < 0 {
		pct = 0
	}
	if pct > 100 {
		pct = 100
	}
	filled := int((pct / 100.0) * float64(width))
	empty := width - filled

	bar := strings.Repeat("█", filled) + strings.Repeat("░", empty)
	if pct > 85 {
		return fmt.Sprintf("\033[31m[%s]\033[0m", bar)
	} else if pct > 70 {
		return fmt.Sprintf("\033[33m[%s]\033[0m", bar)
	}
	return fmt.Sprintf("\033[32m[%s]\033[0m", bar)
}
