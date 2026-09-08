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
	ActiveTab int // 0: Switch, 1: Proj, 2: Git, 3: Docker, 4: Term
	StatusMsg string
}

func NewCockpitApp() *CockpitApp {
	return &CockpitApp{
		ActiveTab: 0,
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
				case 'C', 'B': // Right / Down
					a.ActiveTab = (a.ActiveTab + 1) % 5
					continue
				case 'D', 'A': // Left / Up
					a.ActiveTab = (a.ActiveTab + 4) % 5
					continue
				}
			}
			continue
		}

		switch b {
		case '\t':
			a.ActiveTab = (a.ActiveTab + 1) % 5
		case '1':
			a.ActiveTab = 0
		case '2':
			a.ActiveTab = 1
		case '3':
			a.ActiveTab = 2
		case '4':
			a.ActiveTab = 3
		case '5':
			a.ActiveTab = 4
		case '\r', '\n', 'e', 'E', 'l', 'L': // Launch active tool
			toolName := a.getActiveToolBinary()
			fmt.Print("\033[?25h\033[?1049l")
			_ = term.Restore(fd, oldState)
			_ = proxy.Execute(toolName, nil)
			oldState, _ = term.MakeRaw(fd)
			fmt.Print("\033[?1049h\033[?25l")
		case 'r', 'R':
			a.StatusMsg = "\033[32mRefreshed status.\033[0m"
		case 'q', 'Q', 0x03:
			fmt.Print("\033[?25h\033[?1049l")
			_ = term.Restore(fd, oldState)
			fmt.Print("\r\n")
			return nil
		}
	}
	return nil
}

func (a *CockpitApp) getActiveToolBinary() string {
	tools := []string{"agyswitch", "agyproj", "agygit", "agydocker", "agyterm"}
	if a.ActiveTab >= 0 && a.ActiveTab < len(tools) {
		return tools[a.ActiveTab]
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
	return strings.Repeat("─", width) + "\r\n"
}

func (a *CockpitApp) Render() {
	width, _ := getTermSize()
	var b strings.Builder
	b.Grow(4096)

	b.WriteString("\033[H")

	if width < 80 {
		b.WriteString("\r\n⚡ \033[1;36mAGYX MASTER COCKPIT\033[0m\r\n")
		b.WriteString(hr(width))
		tabNames := []string{"1:Sw", "2:Pr", "3:Git", "4:Doc", "5:Term"}
		for i, t := range tabNames {
			if i == a.ActiveTab {
				fmt.Fprintf(&b, "\033[1;37;44m [%s] \033[0m ", t)
			} else {
				fmt.Fprintf(&b, "\033[36m[%s]\033[0m ", t)
			}
		}
		b.WriteString("\r\n" + hr(width))
	} else {
		b.WriteString("\r\n⚡ \033[1;36mAGYX - Unified Developer Suite Proxy & Master Control Center\033[0m\r\n")
		b.WriteString(hr(width))
		tabs := []string{
			"[1] 🛸 Switch",
			"[2] 📁 Proj",
			"[3] 🐙 Git",
			"[4] 🐳 Docker",
			"[5] 🎨 Term",
		}
		for i, t := range tabs {
			if i == a.ActiveTab {
				fmt.Fprintf(&b, " \033[1;37;44m %s \033[0m ", t)
			} else {
				fmt.Fprintf(&b, " \033[36m%s\033[0m ", t)
			}
		}
		b.WriteString("\r\n" + hr(width))
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
		a.renderTermSummary(&b, width)
	}

	b.WriteString(hr(width))
	if a.StatusMsg != "" {
		fmt.Fprintf(&b, " %s\r\n", a.StatusMsg)
	}

	if width < 80 {
		b.WriteString(" \033[1m[Tab/1-5]\033[0mNav \033[1;32m[Enter]\033[0mOpen App \033[1;31m[Q]\033[0mExit\r\n")
	} else {
		b.WriteString(" \033[1m[Tab/1-5]\033[0m Switch Module · \033[1;32m[Enter]\033[0m Launch Dedicated App · \033[1;36m[R]\033[0m Refresh · \033[1;31m[Q/Esc]\033[0m Exit\r\n")
	}

	b.WriteString("\033[J")
	os.Stdout.WriteString(b.String())
}

func (a *CockpitApp) renderSwitchSummary(b *strings.Builder, width int) {
	fmt.Fprintf(b, " 🛸 \033[1;36mAntigravity Control & Context (agyswitch)\033[0m\r\n\r\n")
	fmt.Fprintf(b, "  • \033[1mPrimary Command:\033[0m  agyx switch [status|token|seed|reset]\r\n")
	fmt.Fprintf(b, "  • \033[1mKey Features:\033[0m     Multi-account OAuth token vault, live quotas, skills sync, rules & MCP\r\n")
	fmt.Fprintf(b, "  • \033[1mWeb Sidecar:\033[0m      http://localhost:8080/api/v1/status\r\n\r\n")
	b.WriteString("  \033[1;32mPress [Enter] to launch full interactive 'agyswitch' Control Center...\033[0m\r\n")
}

func (a *CockpitApp) renderProjSummary(b *strings.Builder, width int) {
	fmt.Fprintf(b, " 📁 \033[1;36mProject Workspaces & IDE Manager (agyproj)\033[0m\r\n\r\n")
	fmt.Fprintf(b, "  • \033[1mPrimary Command:\033[0m  agyx proj [ls|scan|add|rm|open|cd]\r\n")
	fmt.Fprintf(b, "  • \033[1mKey Features:\033[0m     Automatic tech stack detection (.NET, React, Go, Docker), IDE launcher\r\n")
	fmt.Fprintf(b, "  • \033[1mRegistry:\033[0m         ~/.config/antigravity/projects.json\r\n\r\n")
	b.WriteString("  \033[1;32mPress [Enter] to launch full interactive 'agyproj' Workspace Hub...\033[0m\r\n")
}

func (a *CockpitApp) renderGitSummary(b *strings.Builder, width int) {
	fmt.Fprintf(b, " 🐙 \033[1;36mMulti-Agent Git Fleet & Worktrees (agygit)\033[0m\r\n\r\n")
	fmt.Fprintf(b, "  • \033[1mPrimary Command:\033[0m  agyx git [ls|status|worktree|fetch|pull|push]\r\n")
	fmt.Fprintf(b, "  • \033[1mKey Features:\033[0m     Parallel AI agent worktree isolation (.worktrees/<branch>), fleet sync\r\n")
	fmt.Fprintf(b, "  • \033[1mScope:\033[0m            Discovers repos across ~/projects with dirty & sync metrics\r\n\r\n")
	b.WriteString("  \033[1;32mPress [Enter] to launch full interactive 'agygit' Fleet Hub...\033[0m\r\n")
}

func (a *CockpitApp) renderDockerSummary(b *strings.Builder, width int) {
	fmt.Fprintf(b, " 🐳 \033[1;36mContainer Lifecycle & WSL2 RAM Guard (agydocker)\033[0m\r\n\r\n")
	fmt.Fprintf(b, "  • \033[1mPrimary Command:\033[0m  agyx docker [ls|ram|prune|start|stop|restart|logs]\r\n")
	fmt.Fprintf(b, "  • \033[1mKey Features:\033[0m     Direct Linux kernel memory guard (/proc/meminfo), Docker prune cache\r\n")
	fmt.Fprintf(b, "  • \033[1mWSL Health:\033[0m       Monitors vmmem consumption to prevent Windows host freezing\r\n\r\n")
	b.WriteString("  \033[1;32mPress [Enter] to launch full interactive 'agydocker' Container Hub...\033[0m\r\n")
}

func (a *CockpitApp) renderTermSummary(b *strings.Builder, width int) {
	fmt.Fprintf(b, " 🎨 \033[1;36mTerminal Font & Shell Theme Customizer (agyterm)\033[0m\r\n\r\n")
	fmt.Fprintf(b, "  • \033[1mPrimary Command:\033[0m  agyx term [ls|theme|font|opacity|fonts]\r\n")
	fmt.Fprintf(b, "  • \033[1mKey Features:\033[0m     Oh My Posh themes (100+), Windows Terminal font face/size/opacity\r\n")
	fmt.Fprintf(b, "  • \033[1mScope:\033[0m            Dual-platform configuration for Windows Terminal & Linux shells\r\n\r\n")
	b.WriteString("  \033[1;32mPress [Enter] to launch full interactive 'agyterm' Customizer...\033[0m\r\n")
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
