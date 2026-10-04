package bot

import (
	"context"
	"fmt"
	"path/filepath"
	"strings"
)

func (h *BotHandler) handlePlanDashboard(ctx context.Context, userID int64, chatID int64) {
	sess := h.getSession(userID)
	ws := h.wsMgr.GetUserWorkspace(userID)
	acc := h.accMgr.GetActiveAccount()

	thinkState := "🔴 OFF"
	if sess.ShowThinking {
		thinkState = "🟢 ON ($0 FREE)"
	}

	modelState := sess.ActiveModel
	if modelState == "" {
		modelState = h.cfg.DefaultModel
	}

	effortState := sess.ActiveEffort
	if effortState == "" {
		effortState = h.cfg.DefaultEffort
	}

	msg := fmt.Sprintf(
		"🎛️ **ANTIGRAVITY REMOTE CONTROLLER (5-MODULE DASHBOARD)**\n\n"+
			"📌 **CURRENT RUNTIME STATE:**\n"+
			"• **Workspace:** `%s`\n"+
			"• **Account:** `%s` (agyswitch)\n"+
			"• **Model:** `%s` · Effort: `%s`\n"+
			"• **Thinking:** %s\n"+
			"• **Telemetry:** `%d prompts · %d steps · $%.4f USD`\n\n"+
			"🕹️ **Group 1: System Control**\n"+
			"`/plan` · `/status` · `/health` · `/help` · `/lock`\n\n"+
			"🧠 **Group 2: AI Engine & Quotas**\n"+
			"`/model` · `/effort` · `/thinking` · `/quota` · `/switch` · `/skills`\n\n"+
			"📁 **Group 3: Workspace & Files**\n"+
			"`/proj` · `/cd` · `/ls` · `/search` · `/getfile` · `/cat` · `/newproj`\n\n"+
			"🛠️ **Group 4: Automation & Swarm**\n"+
			"`-c` · `/rebuild` · `/replan` · `/swarm` · `/review` · `/research`\n\n"+
			"💾 **Group 5: Session Management**\n"+
			"`/sessions` · `/resume` · `/session` · `/clear`\n\n"+
			"💡 *Tap an inline module button below to execute instantly:*",
		filepath.Base(ws), acc, modelState, effortState, thinkState,
		sess.Metrics.TotalPrompts, sess.Metrics.TotalSteps, sess.Metrics.EstCostUSD,
	)

	inlineKeyboard := &InlineKeyboardMarkup{
		InlineKeyboard: [][]InlineKeyboardButton{
			{
				{Text: "🕹️ Status", CallbackData: "btn_status"},
				{Text: "🩺 Health", CallbackData: "btn_health"},
				{Text: "❓ Help", CallbackData: "btn_help"},
			},
			{
				{Text: "🧠 Models", CallbackData: "btn_models"},
				{Text: "⚡ Effort", CallbackData: "btn_effort"},
				{Text: "💭 Thinking", CallbackData: "btn_thinking"},
				{Text: "👤 Quotas", CallbackData: "btn_accounts"},
			},
			{
				{Text: "📁 Projects", CallbackData: "btn_projects"},
				{Text: "📂 Files", CallbackData: "btn_files"},
				{Text: "🔎 Search", CallbackData: "btn_search"},
			},
			{
				{Text: "🔄 Continue", CallbackData: "btn_continue"},
				{Text: "🔨 Rebuild", CallbackData: "btn_rebuild"},
				{Text: "🐝 Swarm", CallbackData: "btn_swarm"},
				{Text: "🛡️ Review", CallbackData: "btn_review"},
			},
			{
				{Text: "💾 Sessions", CallbackData: "btn_sessions"},
				{Text: "🔄 Clear", CallbackData: "btn_clear"},
				{Text: "🛑 Abort", CallbackData: "cancel_prompt"},
			},
		},
	}

	_, _ = h.client.SendMessage(ctx, chatID, msg, inlineKeyboard)
	// Send reply keyboard only when dashboard explicitly triggered
	_, _ = h.client.SendMessage(ctx, chatID, "⌨️ *Quick Navigation Menu:*", h.buildMainKeyboard(userID))
}

func (h *BotHandler) getSession(userID int64) *UserSessionState {
	sess, ok := h.userSessions[userID]
	if !ok {
		sess = &UserSessionState{
			ActiveModel:  h.cfg.DefaultModel,
			ActiveEffort: h.cfg.DefaultEffort,
		}
		h.userSessions[userID] = sess
	}
	if sess.ActiveModel == "" {
		sess.ActiveModel = h.cfg.DefaultModel
	}
	if sess.ActiveEffort == "" {
		sess.ActiveEffort = h.cfg.DefaultEffort
	}
	return sess
}

func (h *BotHandler) handleToggleThinking(ctx context.Context, userID int64, chatID int64, arg string) {
	sess := h.getSession(userID)
	arg = strings.ToLower(strings.TrimSpace(arg))
	if arg == "on" || arg == "true" || arg == "1" {
		sess.ShowThinking = true
	} else if arg == "off" || arg == "false" || arg == "0" {
		sess.ShowThinking = false
	} else {
		sess.ShowThinking = !sess.ShowThinking
	}

	status := "🔴 OFF (Clean mode)"
	if sess.ShowThinking {
		status = "🟢 ON (Thinking reasoning will be streamed into chat - $0 FREE local cost)"
	}
	_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("💭 **Thinking Steps Visibility:** %s", status), h.buildMainKeyboard(userID))
}

func (h *BotHandler) handleModels(ctx context.Context, userID int64, chatID int64) {
	sess := h.getSession(userID)
	curModel := sess.ActiveModel
	if curModel == "" {
		curModel = h.cfg.DefaultModel
	}

	models := []string{
		"Gemini 3.7 Flash",
		"Gemini 3.8 Pro",
		"Claude 3.5 Sonnet",
		"Claude 3.7 Sonnet",
		"Claude Opus 4.6",
		"Ollama Local",
	}

	var keyboard [][]InlineKeyboardButton
	var row []InlineKeyboardButton

	for _, m := range models {
		prefix := "  "
		if strings.EqualFold(m, curModel) {
			prefix = "⭐ "
		}
		key := strings.ReplaceAll(strings.ToLower(m), " ", "_")
		row = append(row, InlineKeyboardButton{
			Text:         prefix + m,
			CallbackData: "set_model_" + key,
		})
		if len(row) == 2 {
			keyboard = append(keyboard, row)
			row = nil
		}
	}
	if len(row) > 0 {
		keyboard = append(keyboard, row)
	}

	msg := fmt.Sprintf("🧠 **SELECT ACTIVE ANTIGRAVITY AI MODEL:**\n\nActive Model: `%s`\n\n👉 *Tap a model button below to switch instantly:*", curModel)
	_, _ = h.client.SendMessage(ctx, chatID, msg, &InlineKeyboardMarkup{InlineKeyboard: keyboard})
}

func (h *BotHandler) handleSetModel(ctx context.Context, userID int64, chatID int64, modelName string) {
	sess := h.getSession(userID)
	sess.ActiveModel = modelName
	_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("🧠 Active AI model set to: `%s`", modelName), h.buildMainKeyboard(userID))
}

func (h *BotHandler) handleEffort(ctx context.Context, userID int64, chatID int64, arg string) {
	sess := h.getSession(userID)
	arg = strings.ToLower(strings.TrimSpace(arg))
	if arg == "" {
		curEffort := sess.ActiveEffort
		if curEffort == "" {
			curEffort = h.cfg.DefaultEffort
		}
		keyboard := [][]InlineKeyboardButton{
			{
				{Text: "⚡ High (Default)", CallbackData: "set_effort_high"},
				{Text: "⚡ Medium", CallbackData: "set_effort_medium"},
				{Text: "⚡ Low", CallbackData: "set_effort_low"},
			},
		}
		msg := fmt.Sprintf("⚡ **SELECT MODEL REASONING EFFORT:**\n\nCurrent Effort: `%s`\n\n👉 *Tap an effort level below:*", curEffort)
		_, _ = h.client.SendMessage(ctx, chatID, msg, &InlineKeyboardMarkup{InlineKeyboard: keyboard})
		return
	}

	switch arg {
	case "high", "medium", "low":
		sess.ActiveEffort = arg
		_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("⚡ Model reasoning effort set to: `%s`", arg), h.buildMainKeyboard(userID))
	default:
		_, _ = h.client.SendMessage(ctx, chatID, "Usage: `/effort [high|medium|low]`", nil)
	}
}

func (h *BotHandler) handleAccount(ctx context.Context, chatID int64) {
	active := h.accMgr.GetActiveAccount()
	accounts := h.accMgr.ListAccounts()

	var sb strings.Builder
	sb.WriteString("👤 **ANTIGRAVITY ACCOUNTS & QUOTA (VIA `agyswitch`):**\n\n")

	for i, a := range accounts {
		marker := "  "
		if a.IsActive {
			marker = "⭐ "
		}
		sb.WriteString(fmt.Sprintf("%s%d. **%s** (`%s`)\n   %s · Gemini: `%.1f%%` · Claude: `%.1f%%`\n",
			marker, i+1, a.AccountName, a.Email, a.QuotaStatus, a.GeminiQuotaPct, a.ClaudeQuotaPct))
	}
	sb.WriteString(fmt.Sprintf("\nActive Account: `%s`\n👉 Switch with: `/switch <account_name>`", active))
	_, _ = h.client.SendMessage(ctx, chatID, sb.String(), nil)
}

func (h *BotHandler) handleSwitch(ctx context.Context, chatID int64, targetName string) {
	err := h.accMgr.SwitchAccount(targetName)
	if err != nil {
		_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("❌ Switch error: %v", err), nil)
		return
	}
	_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("✅ Switched active Antigravity account to: `%s`", targetName), nil)
}

func (h *BotHandler) handleSkills(ctx context.Context, chatID int64) {
	skills := []string{
		"agy-customizations (Skill & Rule Engine)",
		"code-review (P0-P4 Severity Audits)",
		"security-audit (Vulnerability & Credential Scanner)",
		"product-roadmap-evolution (Roadmap & Architecture Plan)",
		"dotnet-clean-architecture (.NET Solution Auditing)",
		"csharp-ef-seeder (EF Core Migrations & Seeders)",
		"react-chart-auditor (React UI & Chart Audit)",
		"docker-full-stack (Container Stack Operations)",
		"git-worktree-fleet (Parallel Worktree Agent Fleet)",
	}

	var sb strings.Builder
	sb.WriteString("⚡ **ACTIVE ANTIGRAVITY SKILLS & PLUGINS:**\n\n")
	for _, s := range skills {
		sb.WriteString(fmt.Sprintf("• `%s`\n", s))
	}
	sb.WriteString("\n👉 *All skills are automatically loaded from `.agents/plugins/` and local rules.*")
	_, _ = h.client.SendMessage(ctx, chatID, sb.String(), nil)
}
