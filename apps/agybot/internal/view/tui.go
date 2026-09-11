package view

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"time"

	"golang.org/x/term"

	"agybot/internal/account"
	"agybot/internal/auth"
	"agybot/internal/config"
	"agybot/internal/daemon"
	"agybot/internal/sysinfo"
	"agybot/internal/workspace"
)

type DashboardView struct {
	Cfg           *config.Config
	AuthMgr       *auth.AuthManager
	WsMgr         *workspace.WorkspaceManager
	AccMgr        *account.AccountManager
	ActiveTab     int // 0: Overview, 1: Projects, 2: Config, 3: Logs, 4: Sessions
	SelectedIndex int
	StatusMsg     string
	tabSwitched   bool
}

func NewDashboardView(cfg *config.Config, authMgr *auth.AuthManager, wsMgr *workspace.WorkspaceManager, accMgr *account.AccountManager) *DashboardView {
	return &DashboardView{
		Cfg:         cfg,
		AuthMgr:     authMgr,
		WsMgr:       wsMgr,
		AccMgr:      accMgr,
		ActiveTab:   0,
		tabSwitched: true,
	}
}

// RunInteractive runs the full-screen interactive cockpit TUI
func (v *DashboardView) RunInteractive() error {
	fd := int(os.Stdin.Fd())
	if !term.IsTerminal(fd) {
		fmt.Print(v.RenderOverview())
		return nil
	}

	oldState, err := term.MakeRaw(fd)
	if err != nil {
		fmt.Print(v.RenderOverview())
		return nil
	}

	// Alternate screen buffer + hide cursor
	fmt.Print("\033[?1049h\033[?25l")
	defer func() {
		fmt.Print("\033[?25h\033[?1049l")
		_ = term.Restore(fd, oldState)
		fmt.Print("\r\n")
	}()

	var buf [32]byte
	for {
		v.Render()

		n, err := os.Stdin.Read(buf[:])
		if err != nil || n == 0 {
			break
		}

		v.StatusMsg = ""
		b := buf[0]

		if b == 0x1b { // Escape sequences
			if n == 1 {
				// Solitary Esc -> exit
				return nil
			}
			if n >= 3 && buf[1] == '[' {
				switch buf[2] {
				case 'C': // Right
					v.ActiveTab = (v.ActiveTab + 1) % 5
					v.SelectedIndex = 0
					v.tabSwitched = true
					continue
				case 'D': // Left
					v.ActiveTab = (v.ActiveTab + 4) % 5
					v.SelectedIndex = 0
					v.tabSwitched = true
					continue
				case 'A': // Up
					if v.SelectedIndex > 0 {
						v.SelectedIndex--
					}
					continue
				case 'B': // Down
					v.SelectedIndex++
					continue
				}
			}
			continue
		}

		switch b {
		case '\t':
			v.ActiveTab = (v.ActiveTab + 1) % 5
			v.SelectedIndex = 0
			v.tabSwitched = true
		case '1':
			v.ActiveTab = 0
			v.SelectedIndex = 0
			v.tabSwitched = true
		case '2':
			v.ActiveTab = 1
			v.SelectedIndex = 0
			v.tabSwitched = true
		case '3':
			v.ActiveTab = 2
			v.SelectedIndex = 0
			v.tabSwitched = true
		case '4':
			v.ActiveTab = 3
			v.SelectedIndex = 0
			v.tabSwitched = true
		case '5':
			v.ActiveTab = 4
			v.SelectedIndex = 0
			v.tabSwitched = true
		case 'j', 'J':
			v.SelectedIndex++
		case 'k', 'K':
			if v.SelectedIndex > 0 {
				v.SelectedIndex--
			}
		case 's', 'S':
			running, pid := daemon.IsRunning()
			if running {
				_, err := daemon.Stop()
				if err != nil {
					v.StatusMsg = fmt.Sprintf("\033[31mFailed to stop daemon: %v\033[0m", err)
				} else {
					v.StatusMsg = fmt.Sprintf("\033[33m⏹ Stopped agybot daemon (was PID %d)\033[0m", pid)
				}
			} else {
				newPid, err := daemon.Start("")
				if err != nil {
					v.StatusMsg = fmt.Sprintf("\033[31mFailed to start daemon: %v\033[0m", err)
				} else {
					v.StatusMsg = fmt.Sprintf("\033[32m▶ Started agybot daemon (PID %d)\033[0m", newPid)
				}
			}
			v.tabSwitched = true
		case 'r', 'R':
			if v.ActiveTab == 0 {
				running, _ := daemon.IsRunning()
				if running {
					newPid, err := daemon.Restart("")
					if err != nil {
						v.StatusMsg = fmt.Sprintf("\033[31mFailed to restart daemon: %v\033[0m", err)
					} else {
						v.StatusMsg = fmt.Sprintf("\033[32m🔄 Restarted daemon (PID %d)\033[0m", newPid)
					}
				} else {
					v.StatusMsg = "\033[36mRefreshed status.\033[0m"
				}
			} else {
				v.StatusMsg = "\033[36mRefreshed view.\033[0m"
			}
			v.tabSwitched = true
		case '\r', '\n':
			if v.ActiveTab == 1 { // Projects tab
				projs := v.WsMgr.ListProjects()
				if v.SelectedIndex >= 0 && v.SelectedIndex < len(projs) {
					target := projs[v.SelectedIndex]
					v.WsMgr.SetUserWorkspace(0, target.Path)
					v.StatusMsg = fmt.Sprintf("\033[32m✔ Active workspace set to: %s\033[0m", target.Name)
				}
			}
		case 'q', 'Q', 0x03:
			return nil
		}
	}
	return nil
}

func getTermWidth() int {
	width := 86
	if fd := int(os.Stdin.Fd()); term.IsTerminal(fd) {
		if w, _, err := term.GetSize(fd); err == nil && w > 0 {
			width = w
		}
	}
	return width
}

func hrLine(width int) string {
	if width < 30 {
		width = 30
	}
	if width > 96 {
		width = 96
	}
	return strings.Repeat("─", width) + "\033[K\r\n"
}

func (v *DashboardView) Render() {
	width := getTermWidth()
	var b strings.Builder
	b.Grow(4096)

	if v.tabSwitched {
		b.WriteString("\033[H\033[2J")
		v.tabSwitched = false
	} else {
		b.WriteString("\033[H")
	}

	b.WriteString("\r\n🤖 \033[1;36mAGYBOT - Antigravity Remote Controller & Multi-Project Daemon\033[0m\033[K\r\n")
	b.WriteString(hrLine(width))

	tabs := []string{
		"[1] 🤖 Overview",
		"[2] 📁 Projects",
		"[3] ⚙️ Config",
		"[4] 📜 Logs",
		"[5] 🧠 Sessions",
	}

	for i, t := range tabs {
		if i == v.ActiveTab {
			fmt.Fprintf(&b, " \033[1;37;44m %s \033[0m ", t)
		} else {
			fmt.Fprintf(&b, " \033[36m%s\033[0m ", t)
		}
	}
	b.WriteString("\033[K\r\n" + hrLine(width))

	switch v.ActiveTab {
	case 0:
		v.renderOverviewTab(&b)
	case 1:
		v.renderProjectsTab(&b)
	case 2:
		v.renderConfigTab(&b)
	case 3:
		v.renderLogsTab(&b)
	case 4:
		v.renderSessionsTab(&b)
	}

	b.WriteString(hrLine(width))
	if v.StatusMsg != "" {
		fmt.Fprintf(&b, " %s\033[K\r\n", v.StatusMsg)
	}

	b.WriteString(" \033[1m[Tab/1-5]\033[0m Tabs · \033[1;33m[↑/↓]\033[0m Select · \033[1;35m[S]\033[0m Start/Stop Daemon · \033[1;36m[R]\033[0m Refresh · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
	b.WriteString("\033[J")
	os.Stdout.WriteString(b.String())
}

func (v *DashboardView) renderOverviewTab(b *strings.Builder) {
	st := sysinfo.GetSystemStatus()
	activeAcc := v.AccMgr.GetActiveAccount()
	activeWs := v.WsMgr.GetUserWorkspace(0)

	botStatus := "\033[1;32m✔ Online / Configured\033[0m"
	if v.Cfg.TelegramBotToken == "" {
		botStatus = "\033[1;33m⚠️ Token Missing (Edit .env or ~/.config/antigravity/bot.env)\033[0m"
	}

	daemonStatus := "\033[90m⚪ Stopped · Press [S] to Start\033[0m"
	if isRun, pid := daemon.IsRunning(); isRun {
		daemonStatus = fmt.Sprintf("\033[1;32m🟢 Running (PID: %d)\033[0m · Press [S] to Stop", pid)
	}

	fmt.Fprintf(b, "  • \033[1mDaemon State:\033[0m   %s\033[K\r\n", daemonStatus)
	fmt.Fprintf(b, "  • \033[1mTelegram Bot:\033[0m   %s  ·  Whitelist: %d Users  ·  Auto-Lock: %v\033[K\r\n",
		botStatus, len(v.Cfg.AllowedUserIDs), v.Cfg.AuthAutoLockTimeout)
	fmt.Fprintf(b, "  • \033[1mAI Engine:\033[0m      Google Antigravity (`%s`) · Mode: `%s`\033[K\r\n",
		v.Cfg.DefaultModel, v.Cfg.DefaultMode)
	fmt.Fprintf(b, "  • \033[1mActive Context:\033[0m \033[1;32m%s\033[0m (via agyswitch)\033[K\r\n", activeAcc)
	fmt.Fprintf(b, "  • \033[1mWorkspace Root:\033[0m \033[1;34m%s\033[0m (%s)\033[K\r\n",
		activeWs, filepath.Base(activeWs))
	fmt.Fprintf(b, "  • \033[1mHost Metrics:\033[0m   CPU: %d cores  ·  RAM: %.1f/%.1f GB (%.1f%%)  ·  Disk: %.1f/%.1f GB\033[K\r\n",
		st.NumCPU, st.MemUsedGB, st.MemTotalGB, st.MemUsedPct, st.DiskUsedGB, st.DiskTotalGB)
	fmt.Fprintf(b, "  • \033[1mContainers:\033[0m     %d running / %d total  ·  Brain Sessions: %d\033[K\r\n\033[K\r\n",
		st.ContainersUp, st.ContainersTotal, st.BrainSessions)

	b.WriteString("  \033[1mQuick Shortcuts:\033[0m\033[K\r\n")
	b.WriteString("    [\033[1;35mS\033[0m] Toggle Daemon    [\033[1;36mR\033[0m] Restart Daemon    [\033[1;32m2\033[0m] Switch Workspace    [\033[1;33m4\033[0m] View Logs\033[K\r\n")
}

func (v *DashboardView) renderProjectsTab(b *strings.Builder) {
	projs := v.WsMgr.ListProjects()
	if len(projs) == 0 {
		b.WriteString("  \033[90mNo projects registered in agyproj. Run 'agyproj scan' to discover.\033[0m\033[K\r\n")
		return
	}

	if v.SelectedIndex >= len(projs) {
		v.SelectedIndex = len(projs) - 1
	}

	fmt.Fprintf(b, "  \033[1mRegistered Projects (%d) · Press [Enter] to switch active workspace:\033[0m\033[K\r\n\033[K\r\n", len(projs))
	for i, p := range projs {
		prefix := "    "
		badge := "\033[36m"
		if i == v.SelectedIndex {
			prefix = " \033[1;32m▶ \033[0m"
			badge = "\033[1;37;42m"
		}

		activeMarker := "  "
		if p.IsActive {
			activeMarker = "⭐"
		}

		status := "✔ clean"
		if p.IsDirty {
			status = fmt.Sprintf("↑%d dirty", p.DirtyCount)
		}

		fmt.Fprintf(b, "%s%s [%d] \033[0m %s \033[1m%-20s\033[0m  Branch: %-12s  (%s)\033[K\r\n",
			prefix, badge, i+1, activeMarker, p.Name, p.GitBranch, status)
		fmt.Fprintf(b, "       \033[90mPath: %s\033[0m\033[K\r\n", p.Path)
	}
}

func (v *DashboardView) renderConfigTab(b *strings.Builder) {
	fmt.Fprintf(b, "  \033[1mBot Configuration Settings (~/.config/antigravity/bot.env):\033[0m\033[K\r\n\033[K\r\n")

	tok := "Not Configured"
	if v.Cfg.TelegramBotToken != "" {
		if len(v.Cfg.TelegramBotToken) > 10 {
			tok = v.Cfg.TelegramBotToken[:6] + "..." + v.Cfg.TelegramBotToken[len(v.Cfg.TelegramBotToken)-4:]
		} else {
			tok = "Set (Hidden)"
		}
	}

	pinStatus := "Not set"
	if v.Cfg.AuthPinHash != "" {
		pinStatus = "Configured (scrypt hash)"
	}

	fmt.Fprintf(b, "  • \033[1mTELEGRAM_BOT_TOKEN:\033[0m  %s\033[K\r\n", tok)
	fmt.Fprintf(b, "  • \033[1mALLOWED_USER_IDS:\033[0m    %v\033[K\r\n", v.Cfg.AllowedUserIDs)
	fmt.Fprintf(b, "  • \033[1mAUTH_PIN_HASH:\033[0m       %s\033[K\r\n", pinStatus)
	fmt.Fprintf(b, "  • \033[1mDEFAULT_WORKSPACE:\033[0m   %s\033[K\r\n", v.Cfg.DefaultWorkspace)
	fmt.Fprintf(b, "  • \033[1mDEFAULT_MODEL:\033[0m       %s\033[K\r\n", v.Cfg.DefaultModel)
	fmt.Fprintf(b, "  • \033[1mDEFAULT_EFFORT:\033[0m      %s\033[K\r\n", v.Cfg.DefaultEffort)
	fmt.Fprintf(b, "  • \033[1mDEFAULT_MODE:\033[0m        %s\033[K\r\n", v.Cfg.DefaultMode)
	fmt.Fprintf(b, "  • \033[1mTASK_TIMEOUT:\033[0m        %v\033[K\r\n", v.Cfg.TaskTimeout)
	fmt.Fprintf(b, "  • \033[1mAUTO_LOCK_MINUTES:\033[0m   %v\033[K\r\n\033[K\r\n", v.Cfg.AuthAutoLockTimeout)
	b.WriteString("  \033[90mTip: Update settings via 'agybot config set <KEY> <VALUE>'\033[0m\033[K\r\n")
}

func (v *DashboardView) renderLogsTab(b *strings.Builder) {
	fmt.Fprintf(b, "  \033[1mDaemon Logs (~/.config/antigravity/agybot.log) · Press [R] to Refresh:\033[0m\033[K\r\n\033[K\r\n")
	logs, err := daemon.TailLogs(15)
	if err != nil {
		fmt.Fprintf(b, "  \033[90m%v\033[0m\033[K\r\n", err)
		return
	}
	lines := strings.Split(strings.TrimSpace(logs), "\n")
	for _, l := range lines {
		fmt.Fprintf(b, "  \033[90m│\033[0m %s\033[K\r\n", l)
	}
}

func (v *DashboardView) renderSessionsTab(b *strings.Builder) {
	sessions := v.WsMgr.ListBrainSessions(8)
	fmt.Fprintf(b, "  \033[1mRecent Antigravity AI Sessions (%d found):\033[0m\033[K\r\n\033[K\r\n", len(sessions))
	if len(sessions) == 0 {
		b.WriteString("  \033[90mNo active brain sessions discovered.\033[0m\033[K\r\n")
		return
	}

	for i, s := range sessions {
		age := time.Since(s.CreatedAt).Round(time.Minute)
		fmt.Fprintf(b, "  [%d] \033[1m%s\033[0m  \033[90m(%v ago · %d steps · est: $%.4f)\033[0m\033[K\r\n",
			i+1, s.ID, age, s.Steps, s.CostUSD)
		if s.Summary != "" {
			sum := s.Summary
			if len(sum) > 80 {
				sum = sum[:77] + "..."
			}
			fmt.Fprintf(b, "      \033[90mSummary: %s\033[0m\033[K\r\n", sum)
		}
	}
}

// RenderOverview generates a beautiful ASCII cockpit summary (non-interactive fallback)
func (v *DashboardView) RenderOverview() string {
	var sb strings.Builder

	st := sysinfo.GetSystemStatus()
	activeAcc := v.AccMgr.GetActiveAccount()
	activeWs := v.WsMgr.GetUserWorkspace(0)

	botStatus := "\033[1;32m✔ Online / Configured\033[0m"
	if v.Cfg.TelegramBotToken == "" {
		botStatus = "\033[1;33m⚠️ Token Missing (Edit .env)\033[0m"
	}

	daemonStatus := "\033[90m⚪ Stopped (Run: agybot start)\033[0m"
	if isRun, pid := daemon.IsRunning(); isRun {
		daemonStatus = fmt.Sprintf("\033[1;32m🟢 Running (PID: %d)\033[0m", pid)
	}

	sb.WriteString("\r\n\033[1;36m🤖 AGYBOT - Antigravity Remote Controller & Multi-Project Daemon (Go Engine v2.0)\033[0m\r\n")
	sb.WriteString(strings.Repeat("─", 86) + "\r\n")
	sb.WriteString(fmt.Sprintf(" \033[1mDaemon State:\033[0m   %s\r\n", daemonStatus))
	sb.WriteString(fmt.Sprintf(" \033[1mTelegram Bot:\033[0m   %s  ·  Whitelist: %d Users  ·  Auto-Lock: %v\r\n",
		botStatus, len(v.Cfg.AllowedUserIDs), v.Cfg.AuthAutoLockTimeout))
	sb.WriteString(fmt.Sprintf(" \033[1mAI Engine:\033[0m      Google Antigravity (`%s`) · Mode: `%s`\r\n",
		v.Cfg.DefaultModel, v.Cfg.DefaultMode))
	sb.WriteString(fmt.Sprintf(" \033[1mActive Context:\033[0m \033[1;32m%s\033[0m (via agyswitch)\r\n", activeAcc))
	sb.WriteString(fmt.Sprintf(" \033[1mWorkspace Root:\033[0m \033[1;34m%s\033[0m (%s)\r\n",
		activeWs, filepath.Base(activeWs)))
	sb.WriteString(fmt.Sprintf(" \033[1mHost Metrics:\033[0m   CPU: %d cores  ·  RAM: %.1f/%.1f GB (%.1f%%)  ·  Disk: %.1f/%.1f GB\r\n",
		st.NumCPU, st.MemUsedGB, st.MemTotalGB, st.MemUsedPct, st.DiskUsedGB, st.DiskTotalGB))
	sb.WriteString(fmt.Sprintf(" \033[1mContainers:\033[0m     %d running / %d total  ·  Brain Sessions: %d\r\n",
		st.ContainersUp, st.ContainersTotal, st.BrainSessions))
	sb.WriteString(strings.Repeat("─", 86) + "\r\n")

	// Registered projects overview
	projs := v.WsMgr.ListProjects()
	sb.WriteString(fmt.Sprintf(" 📁 \033[1mDeep Projects Access (%d registered in agyproj):\033[0m\r\n", len(projs)))
	for i, p := range projs {
		if i >= 4 {
			sb.WriteString(fmt.Sprintf("    ... and %d more projects\r\n", len(projs)-4))
			break
		}
		status := "✔ clean"
		if p.IsDirty {
			status = fmt.Sprintf("↑%d dirty", p.DirtyCount)
		}
		sb.WriteString(fmt.Sprintf("    [%d] %-20s  %-12s  (%s)\r\n", i+1, p.Name, p.GitBranch, status))
	}

	sb.WriteString(strings.Repeat("─", 86) + "\r\n")
	sb.WriteString(" \033[1mCommands:\033[0m  agybot start · agybot stop · agybot restart · agybot logs · agybot config\r\n")

	return sb.String()
}
