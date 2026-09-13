package view

import (
	"context"
	"fmt"
	"io"
	"os"
	"os/exec"
	"path/filepath"
	"strconv"
	"strings"
	"syscall"
	"time"

	"golang.org/x/term"

	"agyx/internal/proxy"
)

type CockpitApp struct {
	ActiveTab    int // 0: Switch, 1: Proj, 2: Git, 3: Docker, 4: Ollama, 5: Tools
	ToolSubIndex int // 0: Term, 1: Mobile, 2: AWS, 3: Bot (inside Tab 5)
	StatusMsg    string
	tabSwitched  bool
}

func NewCockpitApp() *CockpitApp {
	return &CockpitApp{
		ActiveTab:   0,
		tabSwitched: true,
	}
}

func (a *CockpitApp) RunInteractive() error {
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

	fmt.Print("\033[?1049h\033[?25l")

	for {
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
				// Solitary Esc key pressed -> Exit cleanly!
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Print("\r\n")
				return nil
			}
			if n >= 3 && buf[1] == '[' {
				switch buf[2] {
				case 'C': // Right
					a.ActiveTab = (a.ActiveTab + 1) % 7
					a.tabSwitched = true
					continue
				case 'D': // Left
					a.ActiveTab = (a.ActiveTab + 6) % 7
					a.tabSwitched = true
					continue
				case 'A': // Up
					if a.ActiveTab == 6 {
						a.ToolSubIndex = (a.ToolSubIndex + 3) % 4
					} else {
						a.ActiveTab = (a.ActiveTab + 6) % 7
						a.tabSwitched = true
					}
					continue
				case 'B': // Down
					if a.ActiveTab == 6 {
						a.ToolSubIndex = (a.ToolSubIndex + 1) % 4
					} else {
						a.ActiveTab = (a.ActiveTab + 1) % 7
						a.tabSwitched = true
					}
					continue
				}
			}
			continue
		}

		switch b {
		case '\t':
			a.ActiveTab = (a.ActiveTab + 1) % 7
			a.tabSwitched = true
		case '1':
			a.ActiveTab = 0
			a.tabSwitched = true
		case '2':
			a.ActiveTab = 1
			a.tabSwitched = true
		case '3':
			a.ActiveTab = 2
			a.tabSwitched = true
		case '4':
			a.ActiveTab = 3
			a.tabSwitched = true
		case '5':
			a.ActiveTab = 4
			a.tabSwitched = true
		case '6':
			a.ActiveTab = 5
			a.tabSwitched = true
		case '7':
			a.ActiveTab = 6
			a.tabSwitched = true
		case 'j', 'J':
			if a.ActiveTab == 6 {
				a.ToolSubIndex = (a.ToolSubIndex + 1) % 4
			}
		case 'k', 'K':
			if a.ActiveTab == 6 {
				a.ToolSubIndex = (a.ToolSubIndex + 3) % 4
			}
		case 't', 'T':
			if a.ActiveTab == 6 {
				if a.ToolSubIndex == 0 {
					toolName := a.getActiveToolBinary()
					a.launchTool(toolName, fd, oldState)
				} else {
					a.ToolSubIndex = 0
					a.tabSwitched = true
				}
			}
		case 'm', 'M':
			if a.ActiveTab == 6 {
				if a.ToolSubIndex == 1 {
					toolName := a.getActiveToolBinary()
					a.launchTool(toolName, fd, oldState)
				} else {
					a.ToolSubIndex = 1
					a.tabSwitched = true
				}
			}
		case 'p', 'P':
			a.ActiveTab = 4
			a.tabSwitched = true
		case 'w', 'W':
			if a.ActiveTab == 4 {
				bin, err := proxy.FindBinary("agyport")
				if err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mError finding agyport: %v\033[0m", err)
				} else {
					cmd := exec.Command(bin, "ui")
					_ = cmd.Start()
					a.StatusMsg = "\033[32m✔ Launched AGYPORT Web UI Dashboard on http://127.0.0.1:5999\033[0m"
				}
				a.tabSwitched = true
			}
		case 'c', 'C':
			if a.ActiveTab == 4 {
				bin, err := proxy.FindBinary("agyport")
				if err == nil {
					out, _ := exec.Command(bin, "reclaim").Output()
					a.StatusMsg = fmt.Sprintf("\033[32m✔ %s\033[0m", strings.TrimSpace(string(out)))
				}
				a.tabSwitched = true
			}
		case 'a', 'A':
			if a.ActiveTab == 4 {
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Print("\r\n\033[1;33m⚠️  Terminate ALL active developer server ports? (y/N): \033[0m")
				var confirm string
				fmt.Scanln(&confirm)
				if strings.EqualFold(strings.TrimSpace(confirm), "y") {
					bin, _ := proxy.FindBinary("agyport")
					_ = exec.Command(bin, "kill-all").Run()
					a.StatusMsg = "\033[32m✔ Terminated all active developer ports\033[0m"
				}
				newOld, _ := term.MakeRaw(fd)
				*oldState = *newOld
				fmt.Print("\033[?1049h\033[?25l")
				a.tabSwitched = true
			} else if a.ActiveTab == 6 {
				if a.ToolSubIndex == 2 {
					toolName := a.getActiveToolBinary()
					a.launchTool(toolName, fd, oldState)
				} else {
					a.ToolSubIndex = 2
					a.tabSwitched = true
				}
			}
		case 'b', 'B':
			if a.ActiveTab == 6 {
				if a.ToolSubIndex == 3 {
					toolName := a.getActiveToolBinary()
					a.launchTool(toolName, fd, oldState)
				} else {
					a.ToolSubIndex = 3
					a.tabSwitched = true
				}
			}
		case 's', 'S':
			if a.ActiveTab == 6 && a.ToolSubIndex == 3 {
				bin, err := proxy.FindBinary("agybot")
				if err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mError locating agybot binary: %v\033[0m", err)
				} else {
					running, pid := isBotRunning()
					if running {
						_ = exec.Command(bin, "stop").Run()
						a.StatusMsg = fmt.Sprintf("\033[33m⏹ agybot daemon stopped (was PID %d)\033[0m", pid)
					} else {
						_ = exec.Command(bin, "start").Run()
						time.Sleep(150 * time.Millisecond)
						if r, p := isBotRunning(); r {
							a.StatusMsg = fmt.Sprintf("\033[32m▶ agybot daemon started (PID %d)\033[0m", p)
						} else {
							a.StatusMsg = "\033[32m▶ agybot daemon start initiated\033[0m"
						}
					}
				}
				a.tabSwitched = true
			}
		case '\r', '\n': // Launch active tool only on Enter
			toolName := a.getActiveToolBinary()
			a.launchTool(toolName, fd, oldState)
		case 'r', 'R':
			a.StatusMsg = "\033[32mRefreshed status.\033[0m"
			a.tabSwitched = true
		case 'q', 'Q', 0x03:
			fmt.Print("\033[?25h\033[?1049l")
			_ = term.Restore(fd, oldState)
			fmt.Print("\r\n")
			return nil
		}
	}
	return nil
}

func (a *CockpitApp) launchTool(binName string, fd int, oldState *term.State) {
	fmt.Print("\033[?25h\033[?1049l")
	_ = term.Restore(fd, oldState)
	var args []string
	if binName == "aws" {
		args = []string{"cockpit"}
	}
	err := proxy.Execute(binName, args)
	newOld, _ := term.MakeRaw(fd)
	*oldState = *newOld
	fmt.Print("\033[?1049h\033[?25l")
	if err != nil {
		a.StatusMsg = fmt.Sprintf("\033[31mError running %s: %v\033[0m", binName, err)
	}
	a.tabSwitched = true
}

func (a *CockpitApp) getActiveToolBinary() string {
	switch a.ActiveTab {
	case 0:
		return "agyswitch"
	case 1:
		return "agyproj"
	case 2:
		return "agygit"
	case 3:
		return "agydocker"
	case 4:
		return "agyport"
	case 5:
		return "agyollama"
	case 6:
		subTools := []string{"agyterm", "agymobile", "aws", "agybot"}
		if a.ToolSubIndex >= 0 && a.ToolSubIndex < len(subTools) {
			return subTools[a.ToolSubIndex]
		}
		return "agyterm"
	}
	return "agyswitch"
}

func isBotRunning() (bool, int) {
	home, _ := os.UserHomeDir()
	pidPath := filepath.Join(home, ".config", "antigravity", "agybot.pid")
	data, err := os.ReadFile(pidPath)
	if err != nil {
		return false, 0
	}
	pidStr := strings.TrimSpace(string(data))
	pid, err := strconv.Atoi(pidStr)
	if err != nil || pid <= 0 {
		return false, 0
	}
	process, err := os.FindProcess(pid)
	if err != nil {
		return false, 0
	}
	if err := process.Signal(syscall.Signal(0)); err == nil {
		return true, pid
	}
	return false, 0
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

func hr(width int) string {
	if width < 30 {
		width = 30
	}
	if width > 100 {
		width = 100
	}
	return strings.Repeat("─", width) + "\033[K\r\n"
}

func (a *CockpitApp) Render() {
	width, _ := getTermSize()
	var b strings.Builder
	b.Grow(4096)

	if a.tabSwitched {
		b.WriteString("\033[H\033[2J")
		a.tabSwitched = false
	} else {
		b.WriteString("\033[H")
	}

	if width < 80 {
		b.WriteString("\r\n⚡ \033[1;36mAGYX MASTER COCKPIT\033[0m\033[K\r\n")
		b.WriteString(hr(width))
		tabNames := []string{"1:Sw", "2:Pr", "3:Git", "4:Doc", "5:Port", "6:Ai", "7:Tool"}
		for i, t := range tabNames {
			if i == a.ActiveTab {
				fmt.Fprintf(&b, "\033[1;37;44m [%s] \033[0m ", t)
			} else {
				fmt.Fprintf(&b, "\033[36m[%s]\033[0m ", t)
			}
		}
		b.WriteString("\033[K\r\n" + hr(width))
	} else {
		b.WriteString("\r\n⚡ \033[1;36mAGYX - Unified Developer Suite Proxy & Master Control Center\033[0m\033[K\r\n")
		b.WriteString(hr(width))
		tabs := []string{
			"[1] 🛸 Switch",
			"[2] 📁 Proj",
			"[3] 🐙 Git",
			"[4] 🐳 Docker",
			"[5] 🌐 Ports",
			"[6] 🤖 Ollama",
			"[7] 🛠️ Tools",
		}
		for i, t := range tabs {
			if i == a.ActiveTab {
				fmt.Fprintf(&b, " \033[1;37;44m %s \033[0m ", t)
			} else {
				fmt.Fprintf(&b, " \033[36m%s\033[0m ", t)
			}
		}
		b.WriteString("\033[K\r\n" + hr(width))
	}

	// Tab content preview
	switch a.ActiveTab {
	case 0:
		a.renderSwitchSummary(&b, width)
	case 1:
		a.renderProjSummary(&b, width)
	case 2:
		a.renderGitSummary(&b, width)
	case 3:
		a.renderDockerSummary(&b, width)
	case 4:
		a.renderPortsSummary(&b, width)
	case 5:
		a.renderOllamaSummary(&b, width)
	case 6:
		a.renderToolsSummary(&b, width)
	}

	b.WriteString(hr(width))
	if a.StatusMsg != "" {
		fmt.Fprintf(&b, " %s\033[K\r\n", a.StatusMsg)
	}

	if width < 80 {
		b.WriteString(" \033[1m[1-7]\033[0mNav \033[1m[↑/↓]\033[0mSelect \033[1;32m[Enter]\033[0mOpen \033[1;31m[Q]\033[0mExit\033[K\r\n")
	} else {
		if a.ActiveTab == 4 {
			b.WriteString(" \033[1m[Tab/1-7]\033[0m Tabs · \033[1;32m[Enter]\033[0m TUI Cockpit · \033[1;36m[W]\033[0m Web Dashboard · \033[1;33m[C]\033[0m Reclaim RAM · \033[1;31m[A]\033[0m Kill All Dev · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
		} else if a.ActiveTab == 6 {
			b.WriteString(" \033[1m[Tab/1-7]\033[0m Tabs · \033[1;33m[↑/↓]\033[0m Select · \033[1;32m[Enter/T/M/A/B]\033[0m Open · \033[1;35m[S]\033[0m Bot Daemon · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
		} else {
			b.WriteString(" \033[1m[Tab/1-7]\033[0m Switch Module · \033[1;32m[Enter]\033[0m Launch Dedicated App · \033[1;36m[R]\033[0m Refresh · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
		}
	}

	b.WriteString("\033[J")
	os.Stdout.WriteString(b.String())
}

func (a *CockpitApp) renderSwitchSummary(b *strings.Builder, width int) {
	fmt.Fprintf(b, " 🛸 \033[1;36mAntigravity Control & Context (agyswitch)\033[0m\033[K\r\n\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mPrimary Command:\033[0m  agyx switch [status|token|seed|reset]\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mKey Features:\033[0m     Multi-account OAuth token vault, live quotas, skills sync, rules & MCP\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mWeb Sidecar:\033[0m      http://localhost:8080/api/v1/status\033[K\r\n\033[K\r\n")
	b.WriteString("  \033[1;32mPress [Enter] to launch full interactive 'agyswitch' Control Center...\033[0m\033[K\r\n")
}

func (a *CockpitApp) renderProjSummary(b *strings.Builder, width int) {
	fmt.Fprintf(b, " 📁 \033[1;36mProject Workspaces & IDE Manager (agyproj)\033[0m\033[K\r\n\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mPrimary Command:\033[0m  agyx proj [ls|scan|add|rm|open|cd]\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mKey Features:\033[0m     Automatic tech stack detection (.NET, React, Go, Docker), IDE launcher\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mRegistry:\033[0m         ~/.config/antigravity/projects.json\033[K\r\n\033[K\r\n")
	b.WriteString("  \033[1;32mPress [Enter] to launch full interactive 'agyproj' Workspace Hub...\033[0m\033[K\r\n")
}

func (a *CockpitApp) renderGitSummary(b *strings.Builder, width int) {
	fmt.Fprintf(b, " 🐙 \033[1;36mMulti-Agent Git Fleet & Worktrees (agygit)\033[0m\033[K\r\n\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mPrimary Command:\033[0m  agyx git [ls|status|worktree|fetch|pull|push]\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mKey Features:\033[0m     Parallel AI agent worktree isolation (.worktrees/<branch>), fleet sync\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mScope:\033[0m            Discovers repos across ~/projects with dirty & sync metrics\033[K\r\n\033[K\r\n")
	b.WriteString("  \033[1;32mPress [Enter] to launch full interactive 'agygit' Fleet Hub...\033[0m\033[K\r\n")
}

func (a *CockpitApp) renderDockerSummary(b *strings.Builder, width int) {
	fmt.Fprintf(b, " 🐳 \033[1;36mContainer Lifecycle & WSL2 RAM Guard (agydocker)\033[0m\033[K\r\n\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mPrimary Command:\033[0m  agyx docker [ls|ram|prune|start|stop|restart|logs]\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mKey Features:\033[0m     Direct Linux kernel memory guard (/proc/meminfo), Docker prune cache\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mWSL Health:\033[0m       Monitors vmmem consumption to prevent Windows host freezing\033[K\r\n\033[K\r\n")
	b.WriteString("  \033[1;32mPress [Enter] to launch full interactive 'agydocker' Container Hub...\033[0m\033[K\r\n")
}

func (a *CockpitApp) renderPortsSummary(b *strings.Builder, width int) {
	fmt.Fprintf(b, " 🌐 \033[1;36mActive Ports & Smart RAM Leverage Optimizer (agyport)\033[0m\033[K\r\n\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mPrimary Command:\033[0m  agyx port [ls|check|kill|kill-all|ram|top|reclaim|ui]\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mKey Features:\033[0m     Live TCP/UDP socket scanner, dev server detection (Vite, Next, FastAPI),\033[K\r\n")
	fmt.Fprintf(b, "                      PID memory attribution, safe port kill & bulk dev server reclaim\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mWeb Dashboard:\033[0m    http://127.0.0.1:5999 (Press \033[1;36m[W]\033[0m to open in browser)\033[K\r\n\033[K\r\n")

	b.WriteString("  \033[1;33mLive System & Memory Telemetry:\033[0m\033[K\r\n")
	bin, err := proxy.FindBinary("agyport")
	if err == nil {
		ctx, cancel := context.WithTimeout(context.Background(), 800*time.Millisecond)
		defer cancel()
		if memOut, err := exec.CommandContext(ctx, bin, "ram").Output(); err == nil {
			lines := strings.Split(strings.TrimSpace(string(memOut)), "\n")
			for _, l := range lines {
				trimmed := strings.TrimSpace(l)
				if strings.HasPrefix(trimmed, "Total:") || strings.HasPrefix(trimmed, "Used:") || strings.HasPrefix(trimmed, "Available:") || strings.HasPrefix(trimmed, "Cached:") || strings.HasPrefix(trimmed, "Swap:") {
					fields := strings.Fields(trimmed)
					if len(fields) >= 2 {
						fmt.Fprintf(b, "    • \033[1m%-11s\033[0m %s\033[K\r\n", fields[0], strings.Join(fields[1:], " "))
					}
				}
			}
		} else {
			b.WriteString("    • RAM Status:       Scanning memory...\033[K\r\n")
		}
	} else {
		b.WriteString("    • RAM Status:       agyport engine ready\033[K\r\n")
	}
	b.WriteString("\033[K\r\n")

	b.WriteString("  \033[1;32mPress [Enter] to launch interactive TUI  ·  Press [W] for Web UI Dashboard\033[0m\033[K\r\n")
	b.WriteString("  \033[1;33mPress [C] for 1-Click Dev RAM Reclaim     ·  Press [A] to Kill All Dev Ports\033[0m\033[K\r\n")
}

func (a *CockpitApp) renderOllamaSummary(b *strings.Builder, width int) {
	fmt.Fprintf(b, " 🤖 \033[1;36mLocal Ollama & Open LLM AI Cockpit (agyollama)\033[0m\033[K\r\n\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mPrimary Command:\033[0m  agyx ollama [status|ls|pull|run|start|stop|benchmark|delete]\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mKey Features:\033[0m     Local AI daemon control, model manager, hardware benchmark, PTY chat\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mEndpoint:\033[0m         http://127.0.0.1:11434\033[K\r\n\033[K\r\n")
	b.WriteString("  \033[1;32mPress [Enter] to launch full interactive 'agyollama' AI Cockpit...\033[0m\033[K\r\n")
}

func (a *CockpitApp) renderToolsSummary(b *strings.Builder, width int) {
	fmt.Fprintf(b, " 🛠️  \033[1;36mDeveloper Utilities & Cloud Services Drawer\033[0m\033[K\r\n\033[K\r\n")
	fmt.Fprintf(b, "  Use \033[1m[↑/↓]\033[0m to navigate or press shortcut key to launch directly:\033[K\r\n\033[K\r\n")

	items := []struct {
		key   string
		emoji string
		title string
		bin   string
		desc  string
		cmd   string
	}{
		{
			key:   "T",
			emoji: "🎨",
			title: "Terminal Themes & Fonts",
			bin:   "agyterm",
			desc:  "Windows Terminal settings.json mutation & 80+ Oh-My-Posh themes",
			cmd:   "agyx term [ls|theme|font|opacity]",
		},
		{
			key:   "M",
			emoji: "📱",
			title: "Mobile Cockpit & Remote Station",
			bin:   "agymobile",
			desc:  "38-col smartphone TUI & Web Cockpit (:7890) over Tailscale / SSH",
			cmd:   "agyx mobile [status|serve|flush|qr]",
		},
		{
			key:   "A",
			emoji: "☁️",
			title: "AWS & LocalStack Cheat Sheet",
			bin:   "aws",
			desc:  "Interactive recipes: SSO, S3, SQS, DynamoDB & LocalStack port 4566",
			cmd:   "agyx aws [sheet|whoami|s3|sqs|local]",
		},
		{
			key:   "B",
			emoji: "🤖",
			title: "Antigravity Telegram Controller & Daemon",
			bin:   "agybot",
			desc:  "Background Telegram bot daemon, multi-project access & research",
			cmd:   "agyx bot [start|stop|restart|logs|status|config]",
		},
	}

	for i, item := range items {
		prefix := "    "
		badgeStyle := "\033[36m"
		textStyle := "\033[0m"
		if i == a.ToolSubIndex {
			prefix = " \033[1;32m▶ \033[0m"
			badgeStyle = "\033[1;37;42m"
			textStyle = "\033[1m"
		}

		fmt.Fprintf(b, "%s%s [%s] \033[0m %s %s%s\033[0m \033[90m(%s)\033[0m\033[K\r\n",
			prefix, badgeStyle, item.key, item.emoji, textStyle, item.title, item.bin)
		fmt.Fprintf(b, "      \033[90m• %s\033[0m\033[K\r\n", item.desc)
		fmt.Fprintf(b, "      \033[90m• Command: %s\033[0m\033[K\r\n", item.cmd)
		if item.bin == "agybot" {
			if running, pid := isBotRunning(); running {
				fmt.Fprintf(b, "      \033[90m• Daemon State: \033[1;32m🟢 Running (PID: %d)\033[0m \033[90m· Press [S] to Stop\033[0m\033[K\r\n", pid)
			} else {
				fmt.Fprintf(b, "      \033[90m• Daemon State: \033[90m⚪ Stopped · Press [S] to Start\033[0m\033[K\r\n")
			}
		}
		b.WriteString("\033[K\r\n")
	}

	selectedName := items[a.ToolSubIndex].title
	fmt.Fprintf(b, "  \033[1;32mPress [Enter] to launch '%s'...\033[0m\033[K\r\n", selectedName)
}

func (a *CockpitApp) runNonInteractive() error {
	a.PrintStatus(os.Stdout)
	return nil
}

func (a *CockpitApp) PrintStatus(w io.Writer) {
	fmt.Fprintln(w, "\n⚡ \033[1;36mAGYX - Unified Developer Suite Proxy\033[0m")
	fmt.Fprintln(w, "──────────────────────────────────────────────────────────────────────────────────")
	tools := proxy.GetRegisteredTools()
	for i, t := range tools {
		fmt.Fprintf(w, " %d. \033[1m%-8s\033[0m (bin: %-10s) %s\n",
			i+1, t.Name, t.BinaryName, t.Description)
	}
	fmt.Fprintln(w, "──────────────────────────────────────────────────────────────────────────────────")
}
