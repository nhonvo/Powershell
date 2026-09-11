package bot

import (
	"context"
	"fmt"
	"path/filepath"
	"strings"
	"time"

	"agybot/internal/account"
	"agybot/internal/auth"
	"agybot/internal/config"
	"agybot/internal/runner"
	"agybot/internal/security"
	"agybot/internal/sysinfo"
	"agybot/internal/workspace"
)

type BotHandler struct {
	cfg          *config.Config
	client       *TelegramClient
	authMgr      *auth.AuthManager
	wsMgr        *workspace.WorkspaceManager
	accMgr       *account.AccountManager
	runner       *runner.AntigravityRunner
	guard        *security.SecurityGuard
	userSessions map[int64]*UserSessionState
}

type UserSessionState struct {
	ActiveConvID string
	ActiveModel  string
	ActiveEffort string
}

func NewBotHandler(
	cfg *config.Config,
	client *TelegramClient,
	authMgr *auth.AuthManager,
	wsMgr *workspace.WorkspaceManager,
	accMgr *account.AccountManager,
	r *runner.AntigravityRunner,
	guard *security.SecurityGuard,
) *BotHandler {
	return &BotHandler{
		cfg:          cfg,
		client:       client,
		authMgr:      authMgr,
		wsMgr:        wsMgr,
		accMgr:       accMgr,
		runner:       r,
		guard:        guard,
		userSessions: make(map[int64]*UserSessionState),
	}
}

// StartPolling launches long-polling loop against Telegram Bot API
func (h *BotHandler) StartPolling(ctx context.Context) error {
	var offset int64 = 0

	for {
		select {
		case <-ctx.Done():
			return ctx.Err()
		default:
		}

		updates, err := h.client.GetUpdates(ctx, offset, 20)
		if err != nil {
			time.Sleep(2 * time.Second)
			continue
		}

		for _, u := range updates {
			if u.UpdateID >= offset {
				offset = u.UpdateID + 1
			}
			h.handleUpdate(ctx, u)
		}
	}
}

func (h *BotHandler) handleUpdate(ctx context.Context, u Update) {
	if u.CallbackQuery != nil {
		h.handleCallbackQuery(ctx, u.CallbackQuery)
		return
	}

	if u.Message == nil || u.Message.From == nil {
		return
	}

	msg := u.Message
	userID := msg.From.ID
	chatID := msg.Chat.ID
	text := strings.TrimSpace(msg.Text)

	// 1. Check Whitelist
	if !h.authMgr.IsUserWhitelisted(userID) {
		unauthMsg := fmt.Sprintf(
			"⛔ **ACCESS DENIED (NOT WHITELISTED)**\n\n"+
				"Your Telegram User ID: `%d`\n\n"+
				"👉 Please add your ID to `.env` or `~/.config/antigravity/bot.env`:\n"+
				"`ALLOWED_USER_IDS=%d`\n",
			userID, userID,
		)
		_, _ = h.client.SendMessage(ctx, chatID, unauthMsg, nil)
		return
	}

	// 2. Check if user is awaiting PIN input
	if h.authMgr.IsAwaitingPin(userID) && !strings.HasPrefix(text, "/") {
		ok, reason := h.authMgr.Authenticate(userID, text)
		if ok {
			_, _ = h.client.SendMessage(ctx, chatID, "✅ "+reason, h.buildMainKeyboard())
		} else {
			_, _ = h.client.SendMessage(ctx, chatID, "❌ "+reason, nil)
		}
		return
	}

	// 3. Command dispatcher
	if strings.HasPrefix(text, "/") {
		parts := strings.Fields(text)
		cmd := strings.ToLower(parts[0])

		switch cmd {
		case "/start", "/menu", "/help":
			h.handleStart(ctx, userID, chatID)
			return
		case "/pin", "/auth", "/unlock":
			h.authMgr.SetAwaitingPin(userID, true)
			_, _ = h.client.SendMessage(ctx, chatID, "🔐 **Please send your 6-digit security PIN:**", nil)
			return
		case "/lock":
			h.authMgr.Lock(userID)
			_, _ = h.client.SendMessage(ctx, chatID, "🔒 **Antigravity Controller has been locked.**", nil)
			return
		}
	}

	// 4. Verify Authentication
	isAuth, isLocked, reason := h.authMgr.CheckAuth(userID)
	if !isAuth {
		h.authMgr.SetAwaitingPin(userID, true)
		lockWarning := "🔒 **CONTROLLER IS LOCKED**\n\n" + reason + "\n\n👉 *Send your security PIN to unlock:*"
		if isLocked {
			lockWarning = "⏳ **TEMPORARY LOCKOUT**\n\n" + reason
		}
		_, _ = h.client.SendMessage(ctx, chatID, lockWarning, nil)
		return
	}

	// 5. Authenticated commands
	if strings.HasPrefix(text, "/") {
		parts := strings.Fields(text)
		cmd := strings.ToLower(parts[0])

		switch cmd {
		case "/status":
			h.handleStatus(ctx, chatID)
		case "/proj", "/projects":
			h.handleProjects(ctx, chatID)
		case "/cd", "/workspace":
			arg := ""
			if len(parts) > 1 {
				arg = parts[1]
			}
			h.handleCd(ctx, userID, chatID, arg)
		case "/ls":
			subpath := ""
			if len(parts) > 1 {
				subpath = parts[1]
			}
			h.handleLs(ctx, userID, chatID, subpath)
		case "/view", "/cat":
			if len(parts) > 1 {
				h.handleCat(ctx, userID, chatID, parts[1])
			} else {
				_, _ = h.client.SendMessage(ctx, chatID, "Usage: `/cat <filename>`", nil)
			}
		case "/finance":
			h.handleFinanceDashboard(ctx, userID, chatID)
		case "/account":
			h.handleAccount(ctx, chatID)
		case "/switch":
			if len(parts) > 1 {
				h.handleSwitch(ctx, chatID, parts[1])
			} else {
				_, _ = h.client.SendMessage(ctx, chatID, "Usage: `/switch <account_name>`", nil)
			}
		case "/models":
			h.handleModels(ctx, chatID)
		case "/reset":
			delete(h.userSessions, userID)
			_, _ = h.client.SendMessage(ctx, chatID, "🔄 Conversation session reset cleanly.", nil)
		default:
			_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("Unknown command `%s`. Type /help for menu.", cmd), nil)
		}
		return
	}

	// 6. Natural Language / AI Coding Prompt to Antigravity
	h.handleAIPrompt(ctx, userID, chatID, text)
}

func (h *BotHandler) handleCallbackQuery(ctx context.Context, q *CallbackQuery) {
	_ = h.client.AnswerCallbackQuery(ctx, q.ID, "", false)
	userID := q.From.ID
	chatID := q.Message.Chat.ID
	data := q.Data

	switch data {
	case "btn_status":
		h.handleStatus(ctx, chatID)
	case "btn_projects":
		h.handleProjects(ctx, chatID)
	case "btn_accounts":
		h.handleAccount(ctx, chatID)
	case "btn_finance":
		h.handleFinanceDashboard(ctx, userID, chatID)
	case "btn_lock":
		h.authMgr.Lock(userID)
		_, _ = h.client.SendMessage(ctx, chatID, "🔒 **Antigravity Controller locked.**", nil)
	case "btn_unlock":
		h.authMgr.SetAwaitingPin(userID, true)
		_, _ = h.client.SendMessage(ctx, chatID, "🔐 **Send your security PIN:**", nil)
	}
}

func (h *BotHandler) handleStart(ctx context.Context, userID int64, chatID int64) {
	isAuth, _, _ := h.authMgr.CheckAuth(userID)
	statusBadge := "🔒 `LOCKED`"
	if isAuth {
		statusBadge = "⚡ `AUTHENTICATED (READY)`"
	}

	ws := h.wsMgr.GetUserWorkspace(userID)
	acc := h.accMgr.GetActiveAccount()

	msg := fmt.Sprintf(
		"🚀 **ANTIGRAVITY REMOTE CONTROLLER (GO ENGINE)**\n\n"+
			"• **Security Status:** %s\n"+
			"• **Active AI Engine:** `Google Antigravity (agy)`\n"+
			"• **Active Account:** `%s`\n"+
			"• **Current Workspace:** `%s`\n\n"+
			"💡 *Send any coding prompt, or select a command below:*",
		statusBadge, acc, filepath.Base(ws),
	)

	_, _ = h.client.SendMessage(ctx, chatID, msg, h.buildMainKeyboard())
}

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

func (h *BotHandler) handleProjects(ctx context.Context, chatID int64) {
	projs := h.wsMgr.ListProjects()
	if len(projs) == 0 {
		_, _ = h.client.SendMessage(ctx, chatID, "📁 No projects found in `agyproj` registry.", nil)
		return
	}

	var sb strings.Builder
	sb.WriteString("📁 **REGISTERED PROJECTS (`agyproj`):**\n\n")
	for i, p := range projs {
		activeMarker := "  "
		if p.IsActive {
			activeMarker = "⭐ "
		}
		dirty := "clean"
		if p.IsDirty {
			dirty = fmt.Sprintf("dirty ↑%d", p.DirtyCount)
		}
		sb.WriteString(fmt.Sprintf("%s%d. **%s** (`%s`)\n   Path: `%s` [%s]\n",
			activeMarker, i+1, p.Name, p.GitBranch, p.Path, dirty))
	}
	sb.WriteString("\n👉 *Switch project with:* `/cd <project_id_or_name>`")
	_, _ = h.client.SendMessage(ctx, chatID, sb.String(), nil)
}

func (h *BotHandler) handleCd(ctx context.Context, userID int64, chatID int64, arg string) {
	if arg == "" {
		cur := h.wsMgr.GetUserWorkspace(userID)
		_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("📂 Current workspace: `%s`", cur), nil)
		return
	}

	newWs, err := h.wsMgr.SetUserWorkspace(userID, arg)
	if err != nil {
		_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("❌ Error: %v", err), nil)
		return
	}
	_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("✅ Workspace switched to:\n`%s`", newWs), nil)
}

func (h *BotHandler) handleLs(ctx context.Context, userID int64, chatID int64, subpath string) {
	ws := h.wsMgr.GetUserWorkspace(userID)
	files, err := h.wsMgr.ListFiles(ws, subpath)
	if err != nil {
		_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("❌ Cannot list directory: %v", err), nil)
		return
	}

	var sb strings.Builder
	sb.WriteString(fmt.Sprintf("📂 **Files in `%s/%s`:**\n\n", filepath.Base(ws), subpath))
	for _, f := range files {
		icon := "📄"
		if f.IsDir {
			icon = "📁"
		}
		sb.WriteString(fmt.Sprintf("%s `%s`\n", icon, f.Name))
	}
	_, _ = h.client.SendMessage(ctx, chatID, sb.String(), nil)
}

func (h *BotHandler) handleCat(ctx context.Context, userID int64, chatID int64, fname string) {
	ws := h.wsMgr.GetUserWorkspace(userID)
	content, err := h.wsMgr.ReadFileContent(ws, fname, 4096)
	if err != nil {
		_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("❌ Error reading file: %v", err), nil)
		return
	}
	if len(content) > 3000 {
		content = content[:3000] + "\n...(truncated)"
	}
	_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("📄 **%s**:\n```\n%s\n```", fname, content), nil)
}

func (h *BotHandler) handleFinanceDashboard(ctx context.Context, userID int64, chatID int64) {
	// Deep project inspection for finance-dashboard
	targetDir := "/home/truongnhon/projects/finance-dashboard"
	_, _ = h.wsMgr.SetUserWorkspace(userID, targetDir)

	scripts, _ := h.wsMgr.ListFiles(targetDir, "scripts")
	var scriptNames []string
	for _, s := range scripts {
		scriptNames = append(scriptNames, s.Name)
	}

	msg := fmt.Sprintf(
		"📊 **FINANCE DASHBOARD DIRECT INTEGRATION**\n\n"+
			"• **Workspace Path:** `%s`\n"+
			"• **Available Scripts:** `%s`\n"+
			"• **Database Sync Tool:** `scripts/sync-data.sh` (Neon Cloud <-> Local Postgres)\n\n"+
			"👉 *AI Engine is now locked onto `finance-dashboard`.* You can ask:\n"+
			"- `Check sync-data.sh status`\n"+
			"- `Audit financial categorization rules`\n"+
			"- `Inspect Neon Postgres connection pooling`",
		targetDir, strings.Join(scriptNames, ", "),
	)
	_, _ = h.client.SendMessage(ctx, chatID, msg, nil)
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

func (h *BotHandler) handleModels(ctx context.Context, chatID int64) {
	models := h.runner.AvailableModels()
	msg := "🧠 **AVAILABLE ANTIGRAVITY AI MODELS:**\n\n"
	for _, m := range models {
		msg += fmt.Sprintf("• `%s`\n", m)
	}
	_, _ = h.client.SendMessage(ctx, chatID, msg, nil)
}

func (h *BotHandler) handleAIPrompt(ctx context.Context, userID int64, chatID int64, prompt string) {
	ws := h.wsMgr.GetUserWorkspace(userID)
	sess, ok := h.userSessions[userID]
	if !ok {
		sess = &UserSessionState{}
		h.userSessions[userID] = sess
	}

	h.client.SendChatAction(ctx, chatID, "typing")

	initialMsg, err := h.client.SendMessage(ctx, chatID, "🤖 **Antigravity working...**", nil)
	if err != nil {
		return
	}

	runCtx, cancel := context.WithTimeout(ctx, h.cfg.TaskTimeout)
	defer cancel()

	events, err := h.runner.ExecutePrompt(runCtx, runner.RunnerOptions{
		AgyPath:        h.cfg.AgyPath,
		WorkspaceDir:   ws,
		Prompt:         prompt,
		ConversationID: sess.ActiveConvID,
		Model:          sess.ActiveModel,
		Effort:         sess.ActiveEffort,
		Mode:           h.cfg.DefaultMode,
	})
	if err != nil {
		_ = h.client.EditMessageText(ctx, chatID, initialMsg.MessageID, fmt.Sprintf("❌ Error: %v", err), nil)
		return
	}

	var lastStatusText string
	var finalAnswer strings.Builder

	for ev := range events {
		switch ev.Type {
		case "init":
			if ev.ConversationID != "" {
				sess.ActiveConvID = ev.ConversationID
			}
		case "tool_start":
			if ev.Content != "" && ev.Content != lastStatusText {
				lastStatusText = ev.Content
				_ = h.client.EditMessageText(ctx, chatID, initialMsg.MessageID, ev.Content, nil)
			}
		case "content":
			finalAnswer.WriteString(ev.Content)
		case "error":
			_ = h.client.EditMessageText(ctx, chatID, initialMsg.MessageID, ev.Content, nil)
			return
		}
	}

	ans := strings.TrimSpace(finalAnswer.String())
	if ans == "" {
		ans = "✔ **Antigravity completed task successfully.**"
	}

	// Telegram maximum message length is 4096 chars
	if len(ans) > 4000 {
		ans = ans[:4000] + "\n...(truncated)"
	}

	_ = h.client.EditMessageText(ctx, chatID, initialMsg.MessageID, ans, nil)
}

func (h *BotHandler) buildMainKeyboard() *InlineKeyboardMarkup {
	return &InlineKeyboardMarkup{
		InlineKeyboard: [][]InlineKeyboardButton{
			{
				{Text: "📁 Projects", CallbackData: "btn_projects"},
				{Text: "📊 Finance DB", CallbackData: "btn_finance"},
			},
			{
				{Text: "👤 Accounts & Quota", CallbackData: "btn_accounts"},
				{Text: "🖥️ Host Status", CallbackData: "btn_status"},
			},
			{
				{Text: "🔒 Lock", CallbackData: "btn_lock"},
				{Text: "🔓 Unlock", CallbackData: "btn_unlock"},
			},
		},
	}
}
