package bot

import (
	"context"
	"fmt"
	"os"
	"path/filepath"
	"strings"

	"agybot/internal/config"
	"agybot/internal/sysinfo"
)

func (h *BotHandler) handleStatus(ctx context.Context, chatID int64) {
	st := sysinfo.GetSystemStatus()
	msg := fmt.Sprintf(
		"🖥️ **HOST SYSTEM & AGY HEALTH**\n\n"+
			"• **Host:** `%s` (%s · %d Cores)\n"+
			"• **Uptime:** `%s`\n"+
			"• **RAM:** `%.2f / %.2f GB` (%.1f%%)\n"+
			"• **Disk:** `%.2f / %.2f GB` (%.1f%%)\n"+
			"• **Containers:** `%d running / %d total`\n"+
			"• **Antigravity Sessions:** `%d active/cached`\n",
		st.Hostname, st.Platform, st.NumCPU, st.UptimeString,
		st.MemUsedGB, st.MemTotalGB, st.MemUsedPct,
		st.DiskUsedGB, st.DiskTotalGB, st.DiskUsedPct,
		st.ContainersUp, st.ContainersTotal, st.BrainSessions,
	)
	_, _ = h.client.SendMessage(ctx, chatID, msg, nil)
}

func (h *BotHandler) handleHealthCheck(ctx context.Context, chatID int64) {
	home, _ := os.UserHomeDir()
	apps := []string{
		"agybot.exe", "agyswitch.exe", "agyproj.exe", "agygit.exe",
		"agydocker.exe", "agyterm.exe", "agyx.exe", "agymobile.exe",
		"agyollama.exe", "agyport.exe", "agyswarm.exe", "agyreview.exe",
	}

	var sb strings.Builder
	sb.WriteString("🩺 **ANTIGRAVITY SUITE HEALTH DIAGNOSTICS**\n\n")

	missingCount := 0
	for _, app := range apps {
		p := filepath.Join(home, ".local", "bin", app)
		if _, err := os.Stat(p); err == nil {
			sb.WriteString(fmt.Sprintf("✅ `%s`: Healthy (`%s`)\n", app, p))
		} else {
			missingCount++
			sb.WriteString(fmt.Sprintf("❌ `%s`: **MISSING / DOWN**\n", app))
		}
	}

	if missingCount > 0 {
		sb.WriteString(fmt.Sprintf("\n⚠️ **Diagnostic Alert:** %d micro-app(s) missing or outdated.\n👉 *Run `/rebuild` to automatically compile all 12 micro-apps!*", missingCount))
	} else {
		sb.WriteString("\n🎉 **All 12 Antigravity Micro-Apps are ONLINE and healthy!**")
	}

	_, _ = h.client.SendMessage(ctx, chatID, sb.String(), nil)
}

func (h *BotHandler) handleHelp(ctx context.Context, chatID int64) {
	helpText := "🤖 **ANTIGRAVITY BOT COMMAND REFERENCE**\n\n" +
		"📋 **Planner & Controls:**\n" +
		"• `/plan` - Interactive Plan & Controller Dashboard\n" +
		"• `/status` - Host RAM, CPU, Container & Session Metrics\n" +
		"• `/health` - System & Micro-App Health Diagnostics\n" +
		"• `/thinking [on|off]` - Toggle Reasoning Steps ($0 Free)\n" +
		"• `/metrics` - View Token Usage & Cost Telemetry\n\n" +
		"🧠 **AI Model & Account Controls:**\n" +
		"• `/model <name>` - Switch Active AI Model\n" +
		"• `/effort <low|med|high>` - Set Reasoning Effort\n" +
		"• `/quota` - Check Account Quotas (`agyswitch`)\n" +
		"• `/skills` - Inspect Loaded Skills & Plugins\n\n" +
		"📁 **Workspaces & Files:**\n" +
		"• `/proj` - Registered Projects List (`agyproj`)\n" +
		"• `/cd <dir>` - Switch Workspace (`/cd D:\\...`)\n" +
		"• `/ls [dir]` - List Files & Subfolders\n" +
		"• `/search <query>` - Deep Workspace File Search & AI Summary\n" +
		"• `/getfile <file>` - Upload File from PC to Phone Attachment\n" +
		"• `/newproj <name>` - Scaffold & Register New Project\n\n" +
		"🛠️ **Build & Multi-Agent Operations:**\n" +
		"• `/rebuild` - Rebuild All 12 Suite Micro-Apps\n" +
		"• `/replan` - Audit Tech Debt & Replan Roadmap\n" +
		"• `/swarm <task>` - Run Multi-Agent Parallel Swarm\n" +
		"• `/review` - Perform P0-P4 Code Security Review\n" +
		"• `/clear` - Clear Conversation Context & Start Fresh\n\n" +
		"🔄 **Session Continuation Shortcuts:**\n" +
		"• Reply `-c` or `0c` to seamlessly continue active prompt!"

	_, _ = h.client.SendMessage(ctx, chatID, helpText, nil)
}

func (h *BotHandler) handleConfig(ctx context.Context, chatID int64) {
	msg := fmt.Sprintf(
		"⚙️ **AGYBOT SYSTEM CONFIGURATION**\n\n"+
			"• **Config Path:** `%s`\n"+
			"• **Whitelisted Users:** `%d`\n"+
			"• **Auto-Lock Inactivity:** `%v`\n"+
			"• **Max PIN Attempts:** `%d`\n"+
			"• **Lockout Duration:** `%v`\n"+
			"• **Task Timeout:** `%v`\n"+
			"• **Default Model:** `%s`\n"+
			"• **Default Workspace:** `%s`\n",
		config.GetConfigFilePath(),
		len(h.cfg.AllowedUserIDs),
		h.cfg.AuthAutoLockTimeout,
		h.cfg.AuthMaxAttempts,
		h.cfg.AuthLockoutDuration,
		h.cfg.TaskTimeout,
		h.cfg.DefaultModel,
		h.cfg.DefaultWorkspace,
	)
	_, _ = h.client.SendMessage(ctx, chatID, msg, nil)
}

func (h *BotHandler) handleHideMenu(ctx context.Context, chatID int64) {
	_, _ = h.client.SendMessage(ctx, chatID, "🙈 Reply keyboard hidden. Type `/menu` or `/plan` anytime to trigger it again!", &ReplyKeyboardRemove{RemoveKeyboard: true})
}

func (h *BotHandler) buildMainKeyboard(userID int64) *ReplyKeyboardMarkup {
	return &ReplyKeyboardMarkup{
		Keyboard: [][]KeyboardButton{
			{
				{Text: "🕹️ Control"},
				{Text: "🧠 AI & Quota"},
				{Text: "📁 Workspace"},
			},
			{
				{Text: "🛠️ Automation"},
				{Text: "💾 Session"},
				{Text: "❓ Help"},
			},
			{
				{Text: "❌ Hide Menu"},
			},
		},
		ResizeKeyboard: true,
		IsPersistent:   false,
	}
}
