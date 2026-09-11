package view

import (
	"fmt"
	"path/filepath"
	"strings"

	"agybot/internal/account"
	"agybot/internal/auth"
	"agybot/internal/config"
	"agybot/internal/daemon"
	"agybot/internal/sysinfo"
	"agybot/internal/workspace"
)

type DashboardView struct {
	Cfg     *config.Config
	AuthMgr *auth.AuthManager
	WsMgr   *workspace.WorkspaceManager
	AccMgr  *account.AccountManager
}

func NewDashboardView(cfg *config.Config, authMgr *auth.AuthManager, wsMgr *workspace.WorkspaceManager, accMgr *account.AccountManager) *DashboardView {
	return &DashboardView{
		Cfg:     cfg,
		AuthMgr: authMgr,
		WsMgr:   wsMgr,
		AccMgr:  accMgr,
	}
}

// RenderOverview generates a beautiful ASCII cockpit summary
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
