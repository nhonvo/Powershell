package view

import (
	"fmt"
	"io"
	"os"
	"strings"

	"golang.org/x/term"

	"agyx/internal/proxy"
)

type CockpitApp struct {
	ActiveTab    int // 0: Switch, 1: Proj, 2: Git, 3: Docker, 4: Ollama, 5: Tools
	ToolSubIndex int // 0: Term, 1: Mobile, 2: AWS (inside Tab 5)
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
					a.ActiveTab = (a.ActiveTab + 1) % 6
					a.tabSwitched = true
					continue
				case 'D': // Left
					a.ActiveTab = (a.ActiveTab + 5) % 6
					a.tabSwitched = true
					continue
				case 'A': // Up
					if a.ActiveTab == 5 {
						a.ToolSubIndex = (a.ToolSubIndex + 2) % 3
					} else {
						a.ActiveTab = (a.ActiveTab + 5) % 6
						a.tabSwitched = true
					}
					continue
				case 'B': // Down
					if a.ActiveTab == 5 {
						a.ToolSubIndex = (a.ToolSubIndex + 1) % 3
					} else {
						a.ActiveTab = (a.ActiveTab + 1) % 6
						a.tabSwitched = true
					}
					continue
				}
			}
			continue
		}

		switch b {
		case '\t':
			a.ActiveTab = (a.ActiveTab + 1) % 6
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
		case 'j', 'J':
			if a.ActiveTab == 5 {
				a.ToolSubIndex = (a.ToolSubIndex + 1) % 3
			}
		case 'k', 'K':
			if a.ActiveTab == 5 {
				a.ToolSubIndex = (a.ToolSubIndex + 2) % 3
			}
		case 't', 'T': // Direct launch agyterm
			a.launchTool("agyterm", fd, oldState)
		case 'm', 'M': // Direct launch agymobile
			a.launchTool("agymobile", fd, oldState)
		case 'a', 'A': // Direct launch aws
			a.launchTool("aws", fd, oldState)
		case '\r', '\n', 'e', 'E', 'l', 'L': // Launch active tool
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
	_ = proxy.Execute(binName, nil)
	newOld, _ := term.MakeRaw(fd)
	*oldState = *newOld
	fmt.Print("\033[?1049h\033[?25l")
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
		return "agyollama"
	case 5:
		subTools := []string{"agyterm", "agymobile", "aws"}
		if a.ToolSubIndex >= 0 && a.ToolSubIndex < len(subTools) {
			return subTools[a.ToolSubIndex]
		}
		return "agyterm"
	}
	return "agyswitch"
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
		tabNames := []string{"1:Sw", "2:Pr", "3:Git", "4:Doc", "5:Ai", "6:Tools"}
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
			"[5] 🤖 Ollama",
			"[6] 🛠️ Tools",
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
		a.renderOllamaSummary(&b, width)
	case 5:
		a.renderToolsSummary(&b, width)
	}

	b.WriteString(hr(width))
	if a.StatusMsg != "" {
		fmt.Fprintf(&b, " %s\033[K\r\n", a.StatusMsg)
	}

	if width < 80 {
		b.WriteString(" \033[1m[1-6]\033[0mNav \033[1m[↑/↓]\033[0mSelect \033[1;32m[Enter]\033[0mOpen \033[1;31m[Q]\033[0mExit\033[K\r\n")
	} else {
		if a.ActiveTab == 5 {
			b.WriteString(" \033[1m[Tab/1-6]\033[0m Tabs · \033[1;33m[↑/↓]\033[0m Select Utility · \033[1;32m[Enter/T/M/A]\033[0m Launch · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
		} else {
			b.WriteString(" \033[1m[Tab/1-6]\033[0m Switch Module · \033[1;32m[Enter]\033[0m Launch Dedicated App · \033[1;36m[R]\033[0m Refresh · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
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
			title: "AWS Cloud & LocalStack Diagnostics",
			bin:   "aws",
			desc:  "STS identity, S3 bucket explorer & LocalStack port 4566 diagnostics",
			cmd:   "agyx aws [whoami|s3|sqs|local]",
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
		fmt.Fprintf(b, "      \033[90m• Command: %s\033[0m\033[K\r\n\033[K\r\n", item.cmd)
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
