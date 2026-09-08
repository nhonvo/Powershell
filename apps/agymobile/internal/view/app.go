package view

import (
	"fmt"
	"io"
	"os"
	"os/exec"
	"strings"

	"golang.org/x/term"

	"agymobile/internal/model"
	"agymobile/internal/service/agentops"
	"agymobile/internal/service/hostops"
	"agymobile/internal/service/tailscaleops"
	"agymobile/internal/web"
)

type App struct {
	ActiveTab     int // 0: Main Cockpit, 1: Tailscale & SSH Info, 2: Recent Logs
	StatusMsg     string
	needsReload   bool
	cachedHost    model.HostStats
	cachedTS      model.TailscaleInfo
	cachedAgent   agentops.AgentSummary
	serverRunning bool
}

func NewApp() *App {
	return &App{
		ActiveTab:   0,
		needsReload: true,
	}
}

func (a *App) PrintStatus(w io.Writer) {
	host := hostops.GetHostStats()
	ts := tailscaleops.GetTailscaleInfo()
	ag := agentops.GetAgentSummary()

	fmt.Fprintln(w, "\r\n📱 AGYMOBILE - Mobile Cockpit & Remote Station")
	fmt.Fprintln(w, strings.Repeat("─", 40))
	fmt.Fprintf(w, " 🌐 Tailscale IP:  %s\r\n", ts.IPv4)
	if ts.MagicDNS != "" {
		fmt.Fprintf(w, " 📡 MagicDNS:      %s\r\n", ts.MagicDNS)
	}
	if ts.MobilePeer != "" {
		fmt.Fprintf(w, " 📲 Mobile Peer:   %s\r\n", ts.MobilePeer)
	}
	fmt.Fprintf(w, " 🧠 WSL2 RAM:      %.1f / %.1f GB (%.0f%%)\r\n",
		float64(host.UsedRAMMB)/1024, float64(host.TotalRAMMB)/1024, host.RAMUsedPct)
	fmt.Fprintf(w, " 👤 Active AI:     %s ($%.2f)\r\n", ag.ActiveAccount, ag.QuotaUSD)
	fmt.Fprintf(w, " 🐳 Containers:    %d / %d Up\r\n", ag.ContainersUp, ag.ContainersTotal)
	if ag.RecentStep != "" {
		fmt.Fprintf(w, " 🤖 Agent Step:    %s\r\n", truncate(ag.RecentStep, 26))
	}
	fmt.Fprintln(w, strings.Repeat("─", 40))
}

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
		fmt.Print("\033[?25h\033[?1049l")
		_ = term.Restore(fd, oldState)
	}()

	fmt.Print("\033[?1049h\033[?25l\033[H\033[2J")

	for {
		if a.needsReload {
			a.cachedHost = hostops.GetHostStats()
			a.cachedTS = tailscaleops.GetTailscaleInfo()
			a.cachedAgent = agentops.GetAgentSummary()
			a.needsReload = false
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
				// Solitary Esc -> Quit
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Print("\r\n")
				return nil
			}
			continue
		}

		switch b {
		case 'q', 'Q', 0x03:
			fmt.Print("\033[?25h\033[?1049l")
			_ = term.Restore(fd, oldState)
			fmt.Print("\r\n")
			return nil
		case 'r', 'R':
			a.needsReload = true
			a.StatusMsg = "\033[32m✔ Refreshed metrics\033[0m"
		case 'b', 'B':
			a.ActiveTab = 0
		case '1': // AI Agents
			a.runExternalCommand(fd, oldState, "agyx")
		case '2': // Switch AI Account
			a.runExternalCommand(fd, oldState, "agyswitch")
		case '3': // Docker Containers
			a.runExternalCommand(fd, oldState, "agydocker")
		case '4': // Flush WSL2 RAM
			a.StatusMsg = "\033[36mReclaiming WSL2 RAM...\033[0m"
			if err := hostops.DropCaches(); err != nil {
				a.StatusMsg = fmt.Sprintf("\033[31mRAM flush failed: %v\033[0m", err)
			} else {
				a.needsReload = true
				a.StatusMsg = "\033[32m✔ Flushed WSL2 RAM caches\033[0m"
			}
		case '5': // Git Status
			a.runExternalCommand(fd, oldState, "agygit")
		case '6': // Workspace Manager
			a.runExternalCommand(fd, oldState, "agyproj")
		case '7': // Tailscale & SSH Info
			a.ActiveTab = 1
		case '8': // Start Web UI
			if !a.serverRunning {
				a.serverRunning = true
				go func() {
					_ = web.StartServer(7890)
				}()
				a.StatusMsg = "\033[32m✔ Web UI started at :7890\033[0m"
			} else {
				a.StatusMsg = "\033[33mWeb UI already running at :7890\033[0m"
			}
		case '9': // Recent Logs
			a.ActiveTab = 2
		}
	}
	return nil
}

func (a *App) runExternalCommand(fd int, oldState *term.State, appName string) {
	fmt.Print("\033[?25h\033[?1049l")
	_ = term.Restore(fd, oldState)
	
	cmd := exec.Command(appName)
	cmd.Stdin = os.Stdin
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr
	_ = cmd.Run()

	_, _ = term.MakeRaw(fd)
	fmt.Print("\033[?1049h\033[?25l\033[H\033[2J")
	a.needsReload = true
}

func truncate(s string, maxLen int) string {
	if maxLen <= 0 {
		return ""
	}
	if len(s) <= maxLen {
		return s
	}
	if maxLen <= 3 {
		return s[:maxLen]
	}
	return s[:maxLen-3] + "..."
}

func hr38() string {
	return strings.Repeat("─", 38) + "\033[K\r\n"
}

func (a *App) Render() {
	var b strings.Builder
	b.Grow(2048)
	b.WriteString("\033[H")

	// Header (Strict 38 columns max)
	b.WriteString("\r\n📱 \033[1;36mAGYMOBILE · Remote Cockpit\033[0m\033[K\r\n")
	tsBadge := "\033[31m[Offline]\033[0m"
	if a.cachedTS.IsOnline {
		tsBadge = "\033[32m[Online]\033[0m"
	}
	fmt.Fprintf(&b, " 🌐 \033[37mTailscale:\033[0m %-15s %s\033[K\r\n", truncate(a.cachedTS.IPv4, 15), tsBadge)
	b.WriteString(hr38())

	switch a.ActiveTab {
	case 1:
		a.renderTailscaleTab(&b)
	case 2:
		a.renderLogsTab(&b)
	default:
		a.renderMainTab(&b)
	}

	b.WriteString(hr38())
	if a.StatusMsg != "" {
		fmt.Fprintf(&b, " %s\033[K\r\n", a.StatusMsg)
	} else {
		b.WriteString("\033[K\r\n")
	}

	if a.ActiveTab == 0 {
		b.WriteString(" \033[1m[1-9]\033[0m Action · \033[1m[r]\033[0m Ref · \033[1;31m[q]\033[0m Exit\033[K\r\n")
	} else {
		b.WriteString(" \033[1m[b]\033[0m Back · \033[1m[r]\033[0m Refresh · \033[1;31m[q]\033[0m Exit\033[K\r\n")
	}

	b.WriteString("\033[J")
	os.Stdout.WriteString(b.String())
}

func (a *App) renderMainTab(b *strings.Builder) {
	// Status summary
	ramUsedGB := float64(a.cachedHost.UsedRAMMB) / 1024.0
	ramTotalGB := float64(a.cachedHost.TotalRAMMB) / 1024.0
	ramColor := "\033[32m"
	if a.cachedHost.RAMUsedPct > 80 {
		ramColor = "\033[31m"
	} else if a.cachedHost.RAMUsedPct > 65 {
		ramColor = "\033[33m"
	}

	fmt.Fprintf(b, " 🧠 \033[1mRAM:\033[0m %s%.1f/%.1fGB (%.0f%%)\033[0m\033[K\r\n",
		ramColor, ramUsedGB, ramTotalGB, a.cachedHost.RAMUsedPct)
	fmt.Fprintf(b, " 👤 \033[1mAI:\033[0m  \033[35m%-10s\033[0m \033[33m$%.2f\033[0m\033[K\r\n",
		truncate(a.cachedAgent.ActiveAccount, 10), a.cachedAgent.QuotaUSD)
	fmt.Fprintf(b, " 🐳 \033[1mDoc:\033[0m \033[36m%d/%d Up\033[0m\033[K\r\n",
		a.cachedAgent.ContainersUp, a.cachedAgent.ContainersTotal)
	b.WriteString(hr38())

	// Quick Actions Menu (1-9)
	b.WriteString(" ⚡ \033[1;33mQUICK ACTIONS (Press 1-9):\033[0m\033[K\r\n\033[K\r\n")
	b.WriteString("  \033[1;32m[1]\033[0m 🤖 AI Agents & Sessions\033[K\r\n")
	b.WriteString("  \033[1;32m[2]\033[0m 👤 Switch AI Account\033[K\r\n")
	b.WriteString("  \033[1;32m[3]\033[0m 🐳 Docker & Stacks\033[K\r\n")
	b.WriteString("  \033[1;32m[4]\033[0m 🧠 Flush WSL2 RAM\033[K\r\n")
	b.WriteString("  \033[1;32m[5]\033[0m 🐙 Git Cockpit & Sync\033[K\r\n")
	b.WriteString("  \033[1;32m[6]\033[0m 📁 Workspaces & IDE\033[K\r\n")
	b.WriteString("  \033[1;32m[7]\033[0m 📲 Tailscale & SSH Info\033[K\r\n")
	webBadge := "\033[36m[Port 7890]\033[0m"
	if a.serverRunning {
		webBadge = "\033[1;32m[Active]\033[0m"
	}
	fmt.Fprintf(b, "  \033[1;32m[8]\033[0m 🌐 Mobile Web Cockpit %s\033[K\r\n", webBadge)
	b.WriteString("  \033[1;32m[9]\033[0m 📜 Agent Transcript Log\033[K\r\n")
}

func (a *App) renderTailscaleTab(b *strings.Builder) {
	fmt.Fprintf(b, " 📲 \033[1;36mTailscale & SSH Remote Info:\033[0m\033[K\r\n\033[K\r\n")
	fmt.Fprintf(b, "  • IPv4:     \033[32m%s\033[0m\033[K\r\n", a.cachedTS.IPv4)
	if a.cachedTS.MagicDNS != "" {
		fmt.Fprintf(b, "  • MagicDNS: \033[35m%s\033[0m\033[K\r\n", truncate(a.cachedTS.MagicDNS, 24))
	}
	if a.cachedTS.MobilePeer != "" {
		fmt.Fprintf(b, "  • Mobile:   \033[33m%s\033[0m\033[K\r\n", a.cachedTS.MobilePeer)
	}
	b.WriteString("\033[K\r\n 🔑 \033[1mSSH Connect String:\033[0m\033[K\r\n")
	user := os.Getenv("USER")
	if user == "" {
		user = "truongnhon"
	}
	fmt.Fprintf(b, "  \033[1;32mssh %s@%s\033[0m\033[K\r\n", user, a.cachedTS.IPv4)
	if a.cachedTS.MagicDNS != "" {
		fmt.Fprintf(b, "  \033[36mssh %s@%s\033[0m\033[K\r\n", user, a.cachedTS.HostName)
	}
	b.WriteString("\033[K\r\n 💡 \033[37mOpen in Termux, ConnectBot or Termius.\033[0m\033[K\r\n")
}

func (a *App) renderLogsTab(b *strings.Builder) {
	fmt.Fprintf(b, " 📜 \033[1;36mLatest Agent Activity:\033[0m\033[K\r\n\033[K\r\n")
	if a.cachedAgent.RecentStep != "" {
		fmt.Fprintf(b, "  • %s\033[K\r\n", a.cachedAgent.RecentStep)
	} else {
		b.WriteString("  \033[33m(No active transcript records found)\033[0m\033[K\r\n")
	}
	b.WriteString("\033[K\r\n 💡 \033[37mPress [1] to open full agent cockpit.\033[0m\033[K\r\n")
}
