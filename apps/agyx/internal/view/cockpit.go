package view

import (
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
	ActiveTab    int // 0: Switch, 1: Proj, 2: Git, 3: Swarm, 4: Tools
	ToolSubIndex int // 0: Docker, 1: Ports, 2: Ollama, 3: Term, 4: Mobile, 5: Bot, 6: AWS
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

	const totalTabs = 5
	const totalTools = 7

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
					a.ActiveTab = (a.ActiveTab + 1) % totalTabs
					a.tabSwitched = true
					continue
				case 'D': // Left
					a.ActiveTab = (a.ActiveTab + totalTabs - 1) % totalTabs
					a.tabSwitched = true
					continue
				case 'A': // Up
					if a.ActiveTab == 4 {
						a.ToolSubIndex = (a.ToolSubIndex + totalTools - 1) % totalTools
					} else {
						a.ActiveTab = (a.ActiveTab + totalTabs - 1) % totalTabs
						a.tabSwitched = true
					}
					continue
				case 'B': // Down
					if a.ActiveTab == 4 {
						a.ToolSubIndex = (a.ToolSubIndex + 1) % totalTools
					} else {
						a.ActiveTab = (a.ActiveTab + 1) % totalTabs
						a.tabSwitched = true
					}
					continue
				}
			}
			continue
		}

		switch b {
		case '\t':
			a.ActiveTab = (a.ActiveTab + 1) % totalTabs
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
		case 'j', 'J':
			if a.ActiveTab == 4 {
				a.ToolSubIndex = (a.ToolSubIndex + 1) % totalTools
			}
		case 'k', 'K':
			if a.ActiveTab == 4 {
				a.ToolSubIndex = (a.ToolSubIndex + totalTools - 1) % totalTools
			}
		case 'd', 'D':
			if a.ActiveTab == 4 {
				if a.ToolSubIndex == 0 {
					toolName := a.getActiveToolBinary()
					a.launchTool(toolName, fd, oldState)
				} else {
					a.ToolSubIndex = 0
					a.tabSwitched = true
				}
			}
		case 'p', 'P':
			if a.ActiveTab == 4 {
				if a.ToolSubIndex == 1 {
					toolName := a.getActiveToolBinary()
					a.launchTool(toolName, fd, oldState)
				} else {
					a.ToolSubIndex = 1
					a.tabSwitched = true
				}
			}
		case 'o', 'O':
			if a.ActiveTab == 4 {
				if a.ToolSubIndex == 2 {
					toolName := a.getActiveToolBinary()
					a.launchTool(toolName, fd, oldState)
				} else {
					a.ToolSubIndex = 2
					a.tabSwitched = true
				}
			}
		case 't', 'T':
			if a.ActiveTab == 4 {
				if a.ToolSubIndex == 3 {
					toolName := a.getActiveToolBinary()
					a.launchTool(toolName, fd, oldState)
				} else {
					a.ToolSubIndex = 3
					a.tabSwitched = true
				}
			}
		case 'm', 'M':
			if a.ActiveTab == 4 {
				if a.ToolSubIndex == 4 {
					toolName := a.getActiveToolBinary()
					a.launchTool(toolName, fd, oldState)
				} else {
					a.ToolSubIndex = 4
					a.tabSwitched = true
				}
			}
		case 'b', 'B':
			if a.ActiveTab == 4 {
				if a.ToolSubIndex == 5 {
					toolName := a.getActiveToolBinary()
					a.launchTool(toolName, fd, oldState)
				} else {
					a.ToolSubIndex = 5
					a.tabSwitched = true
				}
			}
		case 'a', 'A':
			if a.ActiveTab == 4 {
				if a.ToolSubIndex == 6 {
					toolName := a.getActiveToolBinary()
					a.launchTool(toolName, fd, oldState)
				} else {
					a.ToolSubIndex = 6
					a.tabSwitched = true
				}
			}
		case 'w', 'W':
			if a.ActiveTab == 4 && a.ToolSubIndex == 1 {
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
			if a.ActiveTab == 4 && a.ToolSubIndex == 1 {
				bin, err := proxy.FindBinary("agyport")
				if err == nil {
					out, _ := exec.Command(bin, "reclaim").Output()
					a.StatusMsg = fmt.Sprintf("\033[32m✔ %s\033[0m", strings.TrimSpace(string(out)))
				}
				a.tabSwitched = true
			}
		case 's', 'S':
			if a.ActiveTab == 4 && a.ToolSubIndex == 5 {
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
		return "agyswarm"
	case 4:
		subTools := []string{
			"agydocker",
			"agyport",
			"agyollama",
			"agyterm",
			"agymobile",
			"agybot",
			"aws",
		}
		if a.ToolSubIndex >= 0 && a.ToolSubIndex < len(subTools) {
			return subTools[a.ToolSubIndex]
		}
		return "agydocker"
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
		tabNames := []string{"1:Sw", "2:Pr", "3:Git", "4:Swarm", "5:Tools"}
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
			"[4] 🐝 Swarm",
			"[5] 🛠️ Tools",
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
		a.renderSwarmSummary(&b, width)
	case 4:
		a.renderToolsSummary(&b, width)
	}

	b.WriteString(hr(width))
	if a.StatusMsg != "" {
		fmt.Fprintf(&b, " %s\033[K\r\n", a.StatusMsg)
	}

	if width < 80 {
		b.WriteString(" \033[1m[1-5]\033[0mNav \033[1m[↑/↓]\033[0mSelect \033[1;32m[Enter]\033[0mOpen \033[1;31m[Q]\033[0mExit\033[K\r\n")
	} else {
		if a.ActiveTab == 4 {
			b.WriteString(" \033[1m[Tab/1-5]\033[0m Tabs · \033[1;33m[↑/↓]\033[0m Select · \033[1;32m[Enter/D/P/O/T/M/B/A]\033[0m Open · \033[1;35m[S]\033[0m Bot · \033[1;36m[W]\033[0m Ports UI · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
		} else {
			b.WriteString(" \033[1m[Tab/1-5]\033[0m Switch Module · \033[1;32m[Enter]\033[0m Launch Dedicated App · \033[1;36m[R]\033[0m Refresh · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
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

func (a *CockpitApp) renderSwarmSummary(b *strings.Builder, width int) {
	fmt.Fprintf(b, " 🐝 \033[1;36mMulti-Agent Child Terminal Cockpit & PTY Swarm (agyswarm)\033[0m\033[K\r\n\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mPrimary Command:\033[0m  agyx swarm [cockpit|run|spawn|version]\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mKey Features:\033[0m     Multi-child terminal PTY management, direct human keyboard pass-through\033[K\r\n")
	fmt.Fprintf(b, "                      ([Enter/i] attach, [Ctrl+] detach), status heuristics (● WORKING, ⚡ NEED INPUT)\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mOrchestration:\033[0m    Cross-account isolated environments, pure Markdown deliverable export\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mDeliverables:\033[0m     ./doc/swarm/*.md (Pure GFM with ANSI stripped)\033[K\r\n\033[K\r\n")
	b.WriteString("  \033[1;32mPress [Enter] to launch interactive 'agyswarm' Multi-Agent Cockpit...\033[0m\033[K\r\n")
}

func (a *CockpitApp) renderToolsSummary(b *strings.Builder, width int) {
	fmt.Fprintf(b, " 🛠️  \033[1;36mDeveloper Utilities & Secondary Cockpits Drawer\033[0m\033[K\r\n\033[K\r\n")
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
			key:   "D",
			emoji: "🐳",
			title: "Container Fleet & WSL2 RAM Guard",
			bin:   "agydocker",
			desc:  "Linux kernel memory guard (/proc/meminfo), Docker prune cache & container fleet",
			cmd:   "agyx docker [ls|ram|prune|start|stop|restart|logs]",
		},
		{
			key:   "P",
			emoji: "🌐",
			title: "Active Ports & Smart RAM Optimizer",
			bin:   "agyport",
			desc:  "Live TCP/UDP socket scanner, dev server detection (Next, Vite), RAM reclaim & Web UI",
			cmd:   "agyx port [ls|check|kill|kill-all|ram|top|reclaim|ui]",
		},
		{
			key:   "O",
			emoji: "🤖",
			title: "Local Ollama & Open LLM AI Cockpit",
			bin:   "agyollama",
			desc:  "Local AI daemon control, model manager, hardware benchmark, PTY chat (:11434)",
			cmd:   "agyx ollama [status|ls|pull|run|start|stop|benchmark|delete]",
		},
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
			key:   "B",
			emoji: "🤖",
			title: "Antigravity Telegram Controller & Daemon",
			bin:   "agybot",
			desc:  "Background Telegram bot daemon, multi-project access & research",
			cmd:   "agyx bot [start|stop|restart|logs|status|config]",
		},
		{
			key:   "A",
			emoji: "☁️",
			title: "AWS & LocalStack Cheat Sheet",
			bin:   "aws",
			desc:  "Interactive recipes: SSO, S3, SQS, DynamoDB & LocalStack port 4566",
			cmd:   "agyx aws [sheet|whoami|s3|sqs|local]",
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

		if item.bin == "agyport" && i == a.ToolSubIndex {
			fmt.Fprintf(b, "      \033[90m• Press \033[1;36m[W]\033[90m for Web UI (:5999) · Press \033[1;33m[C]\033[90m to Reclaim RAM\033[0m\033[K\r\n")
		} else if item.bin == "agybot" {
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
