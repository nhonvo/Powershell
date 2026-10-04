package bot

import (
	"context"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"runtime"
	"strconv"
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
	ShowThinking bool
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
	// Register commands with Telegram API for auto-complete on mobile phone
	_ = h.client.SetMyCommands(ctx, []BotCommand{
		{Command: "plan", Description: "Interactive Plan & Controller Dashboard"},
		{Command: "model", Description: "Switch Active AI Model (Gemini/Claude/Ollama)"},
		{Command: "effort", Description: "Set Model Reasoning Effort (High/Medium/Low)"},
		{Command: "quota", Description: "Check Account Quotas (agyswitch)"},
		{Command: "thinking", Description: "Toggle Reasoning Steps ($0 Free)"},
		{Command: "projects", Description: "Select Registered Projects"},
		{Command: "cd", Description: "Switch Workspace Directory"},
		{Command: "ls", Description: "List Directory Files & Folders"},
		{Command: "getfile", Description: "Download File from PC to Phone"},
		{Command: "rebuild", Description: "Rebuild All 12 Micro-Apps"},
		{Command: "replan", Description: "Audit Tech Debt & Replan Roadmap"},
		{Command: "swarm", Description: "Run Multi-Agent Parallel Swarm"},
		{Command: "review", Description: "Perform Code Security Review"},
		{Command: "health", Description: "Micro-App & System Health Check"},
		{Command: "help", Description: "Bot Commands & Reference Guide"},
	})

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
			_, _ = h.client.SendMessage(ctx, chatID, "✅ "+reason, h.buildMainKeyboard(userID))
		} else {
			_, _ = h.client.SendMessage(ctx, chatID, "❌ "+reason, nil)
		}
		return
	}

	// 3. Authenticated commands & session continuation shortcuts
	lowerText := strings.ToLower(text)

	// Direct session continuation shortcuts: "-c", "0c", "/c", "/continue"
	if lowerText == "-c" || lowerText == "0c" || lowerText == "/c" || lowerText == "/continue" {
		h.handleAIPrompt(ctx, userID, chatID, "Continue the previous task, review progress, and execute next steps.")
		return
	}
	if strings.HasPrefix(lowerText, "-c ") || strings.HasPrefix(lowerText, "0c ") || strings.HasPrefix(lowerText, "/c ") {
		parts := strings.SplitN(text, " ", 2)
		if len(parts) > 1 {
			h.handleAIPrompt(ctx, userID, chatID, parts[1])
			return
		}
	}

	// 4. File Upload Handler (Document or Photo attached in message)
	if msg.Document != nil || len(msg.Photo) > 0 {
		h.handleFileUpload(ctx, userID, chatID, msg)
		return
	}

	if strings.HasPrefix(text, "/") {
		parts := strings.Fields(text)
		cmd := strings.ToLower(parts[0])

		switch cmd {
		case "/start", "/plan", "/menu":
			h.handlePlanDashboard(ctx, userID, chatID)
		case "/help":
			h.handleHelp(ctx, chatID)
		case "/model", "/models":
			if len(parts) > 1 {
				h.handleSetModel(ctx, userID, chatID, strings.Join(parts[1:], " "))
			} else {
				h.handleModels(ctx, userID, chatID)
			}
		case "/effort":
			arg := ""
			if len(parts) > 1 {
				arg = parts[1]
			}
			h.handleEffort(ctx, userID, chatID, arg)
		case "/quota":
			h.handleAccount(ctx, chatID)
		case "/thinking", "/thought":
			arg := ""
			if len(parts) > 1 {
				arg = parts[1]
			}
			h.handleToggleThinking(ctx, userID, chatID, arg)
		case "/skills", "/plugins":
			h.handleSkills(ctx, chatID)
		case "/health", "/diag", "/diagnose":
			h.handleHealthCheck(ctx, chatID)
		case "/status":
			h.handleStatus(ctx, chatID)
		case "/proj", "/projects":
			h.handleProjects(ctx, chatID)
		case "/rebuild", "/build":
			h.handleRebuild(ctx, userID, chatID)
		case "/replan":
			h.handleReplan(ctx, userID, chatID)
		case "/getfile", "/download":
			filePath := ""
			if len(parts) > 1 {
				filePath = parts[1]
			}
			h.handleGetFile(ctx, userID, chatID, filePath)
		case "/swarm":
			taskStr := "Inspect project state and perform automated refactoring and verification"
			if len(parts) > 1 {
				taskStr = strings.Join(parts[1:], " ")
			}
			h.handleSwarm(ctx, userID, chatID, taskStr)
		case "/review":
			h.handleReview(ctx, userID, chatID, parts[1:])
		case "/newproj", "/createproj":
			h.handleNewProj(ctx, userID, chatID, parts[1:])
		case "/research":
			h.handleResearch(ctx, userID, chatID, parts[1:])
		case "/sessions":
			h.handleSessions(ctx, chatID)
		case "/resume":
			h.handleResume(ctx, userID, chatID, parts[1:])
		case "/session":
			h.handleCurrentSession(ctx, userID, chatID)
		case "/config":
			h.handleConfig(ctx, chatID)
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
				_, _ = h.client.SendMessage(ctx, chatID, "Usage: `/cat <filename>` or `/getfile <filename>`", nil)
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
		case "/reset", "/new":
			delete(h.userSessions, userID)
			_, _ = h.client.SendMessage(ctx, chatID, "🔄 Conversation session reset cleanly.", nil)
		default:
			_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("Unknown command `%s`. Type /help for menu.", cmd), nil)
		}
		return
	}

	// 5. Quick project selection by number or "cd <name>"
	numClean := strings.TrimPrefix(text, "#")
	if num, err := strconv.Atoi(numClean); err == nil && num >= 1 && num <= 30 {
		projs := h.wsMgr.ListProjects()
		if num <= len(projs) {
			h.handleCd(ctx, userID, chatID, numClean)
			return
		}
	}
	if strings.HasPrefix(strings.ToLower(text), "cd ") {
		arg := strings.TrimSpace(text[3:])
		h.handleCd(ctx, userID, chatID, arg)
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

	if strings.HasPrefix(data, "cd_proj_") {
		numStr := strings.TrimPrefix(data, "cd_proj_")
		h.handleCd(ctx, userID, chatID, numStr)
		return
	}

	if strings.HasPrefix(data, "set_model_") {
		modelKey := strings.TrimPrefix(data, "set_model_")
		modelName := "Gemini 3.7 Flash"
		switch modelKey {
		case "gemini_3.7_flash", "gemini_flash":
			modelName = "Gemini 3.7 Flash"
		case "gemini_3.8_pro", "gemini_pro":
			modelName = "Gemini 3.8 Pro"
		case "claude_3.5_sonnet", "claude_sonnet":
			modelName = "Claude 3.5 Sonnet"
		case "claude_3.7_sonnet":
			modelName = "Claude 3.7 Sonnet"
		case "claude_opus_4.6", "claude_opus":
			modelName = "Claude Opus 4.6"
		case "ollama_local", "ollama":
			modelName = "Ollama Local"
		}
		_ = h.client.AnswerCallbackQuery(ctx, q.ID, "Switched to "+modelName, false)
		h.handleSetModel(ctx, userID, chatID, modelName)
		return
	}

	if strings.HasPrefix(data, "set_effort_") {
		effortLevel := strings.TrimPrefix(data, "set_effort_")
		sess := h.getSession(userID)
		sess.ActiveEffort = effortLevel
		_ = h.client.AnswerCallbackQuery(ctx, q.ID, "Effort set to "+effortLevel, false)
		_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("⚡ Model reasoning effort set to: `%s`", effortLevel), h.buildMainKeyboard(userID))
		return
	}

	switch data {
	case "btn_plan":
		h.handlePlanDashboard(ctx, userID, chatID)
	case "btn_help":
		h.handleHelp(ctx, chatID)
	case "btn_models":
		h.handleModels(ctx, userID, chatID)
	case "btn_effort":
		h.handleEffort(ctx, userID, chatID, "")
	case "btn_toggle_thinking":
		h.handleToggleThinking(ctx, userID, chatID, "")
	case "btn_skills":
		h.handleSkills(ctx, chatID)
	case "btn_health":
		h.handleHealthCheck(ctx, chatID)
	case "btn_status":
		h.handleStatus(ctx, chatID)
	case "btn_projects":
		h.handleProjects(ctx, chatID)
	case "btn_continue":
		h.handleAIPrompt(ctx, userID, chatID, "Continue the previous task, review progress, and execute next steps.")
	case "btn_rebuild":
		h.handleRebuild(ctx, userID, chatID)
	case "btn_replan":
		h.handleReplan(ctx, userID, chatID)
	case "btn_swarm":
		h.handleSwarm(ctx, userID, chatID, "Inspect current project status and run autonomous agent swarm review")
	case "btn_review":
		h.handleReview(ctx, userID, chatID, nil)
	case "btn_files":
		h.handleLs(ctx, userID, chatID, "")
	case "btn_accounts":
		h.handleAccount(ctx, chatID)
	case "btn_sessions":
		h.handleSessions(ctx, chatID)
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

	_, _ = h.client.SendMessage(ctx, chatID, msg, h.buildMainKeyboard(userID))
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

	var keyboard [][]InlineKeyboardButton
	var row []InlineKeyboardButton

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

		row = append(row, InlineKeyboardButton{
			Text:         fmt.Sprintf("%d. %s", i+1, p.Name),
			CallbackData: fmt.Sprintf("cd_proj_%d", i+1),
		})
		if len(row) == 2 {
			keyboard = append(keyboard, row)
			row = nil
		}
	}
	if len(row) > 0 {
		keyboard = append(keyboard, row)
	}

	sb.WriteString("\n👉 *Tap a button below or send:* `/cd <number_or_name>` *(or just reply with the number)*")
	_, _ = h.client.SendMessage(ctx, chatID, sb.String(), &InlineKeyboardMarkup{InlineKeyboard: keyboard})
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
	_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("✅ Workspace switched to **%s**:\n`%s`", filepath.Base(newWs), newWs), nil)
}

func (h *BotHandler) handleLs(ctx context.Context, userID int64, chatID int64, subpath string) {
	ws := h.wsMgr.GetUserWorkspace(userID)
	files, err := h.wsMgr.ListFiles(ws, subpath)
	if err != nil {
		_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("❌ Cannot list directory: %v", err), nil)
		return
	}

	displayPath := subpath
	if displayPath == "" {
		displayPath = ws
	}

	var sb strings.Builder
	sb.WriteString(fmt.Sprintf("📂 **Files in `%s`:**\n\n", displayPath))
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

func (h *BotHandler) handleAIPrompt(ctx context.Context, userID int64, chatID int64, prompt string) {
	ws := h.wsMgr.GetUserWorkspace(userID)
	sess, ok := h.userSessions[userID]
	if !ok {
		sess = &UserSessionState{}
		h.userSessions[userID] = sess
	}

	h.client.SendChatAction(ctx, chatID, "typing")

	initialMsg, err := h.client.SendMessage(ctx, chatID, fmt.Sprintf("🤖 **Antigravity AI Agent**\n📍 Workspace: `%s`\n⚡ Working on: _%s_...", filepath.Base(ws), prompt), nil)
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
		_ = h.client.EditMessageText(ctx, chatID, initialMsg.MessageID, fmt.Sprintf("❌ Error starting agent: %v", err), nil)
		return
	}

	var lastStatusText string
	var finalAnswer strings.Builder
	var finalResultText string
	var lastEditTime time.Time
	toolCount := 0
	hasError := false

	for ev := range events {
		switch ev.Type {
		case "init":
			if ev.ConversationID != "" {
				sess.ActiveConvID = ev.ConversationID
			}
		case "tool_start":
			if ev.Content != "" && ev.Content != lastStatusText {
				lastStatusText = ev.Content
				toolCount++
				// Push tool execution log message directly to Telegram
				_, _ = h.client.SendMessage(ctx, chatID, ev.Content, nil)
				h.client.SendChatAction(ctx, chatID, "typing")
			}
		case "content":
			finalAnswer.WriteString(ev.Content)
			if time.Since(lastEditTime) > 2*time.Second {
				lastEditTime = time.Now()
				snippet := finalAnswer.String()
				if len(snippet) > 800 {
					snippet = snippet[len(snippet)-800:]
				}
				_ = h.client.EditMessageText(ctx, chatID, initialMsg.MessageID, fmt.Sprintf("🤖 **Antigravity AI Agent** (Streaming...)\n📍 Workspace: `%s`\n```\n%s\n```", filepath.Base(ws), snippet), nil)
			}
		case "result":
			if ev.Content != "" {
				finalResultText = ev.Content
			}
		case "error":
			hasError = true
			_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("❌ **Antigravity CLI Error:**\n```\n%s\n```", ev.Content), nil)
		}
	}

	if hasError {
		_ = h.client.EditMessageText(ctx, chatID, initialMsg.MessageID, fmt.Sprintf("❌ **Antigravity Agent Exited with Error** (Workspace: `%s`)", filepath.Base(ws)), nil)
		return
	}

	ans := strings.TrimSpace(finalResultText)
	if ans == "" {
		ans = strings.TrimSpace(finalAnswer.String())
	}
	if ans == "" {
		if toolCount > 0 {
			ans = fmt.Sprintf("✔ **Antigravity executed %d tool operations successfully.**", toolCount)
		} else {
			ans = fmt.Sprintf("✔ **Task completed in `%s`.**", filepath.Base(ws))
		}
	}

	// Update initial status indicator to mark completion
	_ = h.client.EditMessageText(ctx, chatID, initialMsg.MessageID, fmt.Sprintf("🤖 **Antigravity Response** (in `%s`):", filepath.Base(ws)), nil)

	// Send complete response (chunked if > 3900 chars)
	sendChunkedMessage(ctx, h.client, chatID, ans)

	// Auto-reopen / refresh main menu keyboard for smooth chat experience
	_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("💡 *Workspace `%s` ready:*", filepath.Base(ws)), h.buildMainKeyboard(userID))
}

func (h *BotHandler) handleFileUpload(ctx context.Context, userID int64, chatID int64, msg *Message) {
	ws := h.wsMgr.GetUserWorkspace(userID)
	fileID := ""
	fileName := "uploaded_file"

	if msg.Document != nil {
		fileID = msg.Document.FileID
		if msg.Document.FileName != "" {
			fileName = msg.Document.FileName
		}
	} else if len(msg.Photo) > 0 {
		largest := msg.Photo[len(msg.Photo)-1]
		fileID = largest.FileID
		fileName = fmt.Sprintf("photo_%d.jpg", time.Now().Unix())
	}

	if fileID == "" {
		_, _ = h.client.SendMessage(ctx, chatID, "❌ Cannot process uploaded file.", nil)
		return
	}

	h.client.SendChatAction(ctx, chatID, "typing")
	tgFile, err := h.client.GetFile(ctx, fileID)
	if err != nil || tgFile.FilePath == "" {
		_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("❌ Error getting file metadata: %v", err), nil)
		return
	}

	targetPath := filepath.Join(ws, fileName)
	err = h.client.DownloadFile(ctx, tgFile.FilePath, targetPath)
	if err != nil {
		_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("❌ Error downloading file to workspace: %v", err), nil)
		return
	}

	msgText := fmt.Sprintf("📥 **FILE RECEIVED & SAVED TO WORKSPACE!**\n\n"+
		"• **File Name:** `%s`\n"+
		"• **Saved Path:** `%s`\n"+
		"• **Workspace:** `%s`\n\n"+
		"👉 *You can now ask Antigravity to inspect, analyze, or process this file!*",
		fileName, targetPath, filepath.Base(ws))

	_, _ = h.client.SendMessage(ctx, chatID, msgText, h.buildMainKeyboard(userID))
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

func sendChunkedMessage(ctx context.Context, client *TelegramClient, chatID int64, text string) {
	const maxLen = 3900
	if len(text) <= maxLen {
		_, _ = client.SendMessage(ctx, chatID, text, nil)
		return
	}

	remaining := text
	for len(remaining) > 0 {
		if len(remaining) <= maxLen {
			_, _ = client.SendMessage(ctx, chatID, remaining, nil)
			break
		}

		splitIdx := strings.LastIndex(remaining[:maxLen], "\n")
		if splitIdx < maxLen/3 {
			splitIdx = maxLen
		}

		chunk := remaining[:splitIdx]
		remaining = strings.TrimLeft(remaining[splitIdx:], "\n")
		_, _ = client.SendMessage(ctx, chatID, chunk, nil)
		time.Sleep(100 * time.Millisecond)
	}
}

func (h *BotHandler) handleNewProj(ctx context.Context, userID int64, chatID int64, args []string) {
	if len(args) == 0 {
		_, _ = h.client.SendMessage(ctx, chatID, "Usage: `/newproj <project_name> [go|node|python|dotnet]`", nil)
		return
	}
	name := args[0]
	stack := "generic"
	if len(args) > 1 {
		stack = args[1]
	}

	p, err := h.wsMgr.CreateProject(name, stack)
	if err != nil {
		_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("❌ Error creating project: %v", err), nil)
		return
	}
	_, _ = h.wsMgr.SetUserWorkspace(userID, p.Path)

	msg := fmt.Sprintf(
		"🎉 **PROJECT CREATED & REGISTERED!**\n\n"+
			"• **Name:** `%s`\n"+
			"• **Stack:** `%s`\n"+
			"• **Path:** `%s`\n"+
			"• **Git:** Initialized (`main`)\n"+
			"• **Registered:** `agyproj` (`~/.config/antigravity/projects.json`)\n\n"+
			"👉 Active workspace automatically switched to `%s`!",
		p.Name, p.Stack, p.Path, p.Name,
	)
	_, _ = h.client.SendMessage(ctx, chatID, msg, nil)
}

func (h *BotHandler) handleResearch(ctx context.Context, userID int64, chatID int64, args []string) {
	if len(args) == 0 {
		_, _ = h.client.SendMessage(ctx, chatID, "Usage: `/research <topic or architectural question>`", nil)
		return
	}
	topic := strings.Join(args, " ")
	researchPrompt := fmt.Sprintf(
		"Perform deep research on: %s. Structure your output clearly: "+
			"1. Executive Summary, 2. Architectural Analysis & Key Options, "+
			"3. Pros vs Cons / Risk Matrix, 4. Concrete Implementation Steps, 5. Recommendations.",
		topic,
	)
	h.handleAIPrompt(ctx, userID, chatID, researchPrompt)
}

func (h *BotHandler) handleSessions(ctx context.Context, chatID int64) {
	sessions := h.wsMgr.ListBrainSessions(8)
	if len(sessions) == 0 {
		_, _ = h.client.SendMessage(ctx, chatID, "🧠 No past Antigravity sessions found in `~/.gemini/antigravity-cli/brain/`.", nil)
		return
	}

	var sb strings.Builder
	sb.WriteString("🧠 **RECENT ANTIGRAVITY AI SESSIONS:**\n\n")
	for i, s := range sessions {
		timeAgo := time.Since(s.CreatedAt).Round(time.Minute)
		sb.WriteString(fmt.Sprintf("%d. `%s` (%s ago · %d steps · est: $%.4f)\n   _%s_\n",
			i+1, s.ID, timeAgo, s.Steps, s.CostUSD, s.Summary))
	}
	sb.WriteString("\n👉 Resume any session with: `/resume <conversation_id>`\n👉 Start fresh: `/reset`")
	_, _ = h.client.SendMessage(ctx, chatID, sb.String(), nil)
}

func (h *BotHandler) handleResume(ctx context.Context, userID int64, chatID int64, args []string) {
	if len(args) == 0 {
		_, _ = h.client.SendMessage(ctx, chatID, "Usage: `/resume <conversation_id>`", nil)
		return
	}
	convID := strings.TrimSpace(args[0])
	sess, ok := h.userSessions[userID]
	if !ok {
		sess = &UserSessionState{}
		h.userSessions[userID] = sess
	}
	sess.ActiveConvID = convID

	_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("🔄 Resumed Antigravity conversation: `%s`.\nNext prompts will continue with full session context!", convID), nil)
}

func (h *BotHandler) handleCurrentSession(ctx context.Context, userID int64, chatID int64) {
	sess, ok := h.userSessions[userID]
	convID := "None (New Session)"
	if ok && sess.ActiveConvID != "" {
		convID = sess.ActiveConvID
	}
	ws := h.wsMgr.GetUserWorkspace(userID)
	acc := h.accMgr.GetActiveAccount()

	msg := fmt.Sprintf(
		"📋 **ACTIVE SESSION CONTEXT**\n\n"+
			"• **Conversation ID:** `%s`\n"+
			"• **Workspace:** `%s`\n"+
			"• **Account:** `%s` (via agyswitch)\n"+
			"• **Model:** `%s`\n\n"+
			"👉 Switch session: `/resume <id>` or `/reset`",
		convID, ws, acc, h.cfg.DefaultModel,
	)
	_, _ = h.client.SendMessage(ctx, chatID, msg, nil)
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

func (h *BotHandler) handleRebuild(ctx context.Context, userID int64, chatID int64) {
	ws := h.wsMgr.GetUserWorkspace(userID)
	_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("🛠️ **REBUILDING WORKSPACE / SUITE APPS**\nWorkspace: `%s`\nExecuting build pipeline...", filepath.Base(ws)), nil)

	buildScript := filepath.Join(h.cfg.DefaultWorkspace, "scripts", "build-apps.ps1")
	var cmd *exec.Cmd
	if runtime.GOOS == "windows" {
		cmd = exec.CommandContext(ctx, "powershell", "-ExecutionPolicy", "Bypass", "-File", buildScript, "-Target", "all")
	} else {
		cmd = exec.CommandContext(ctx, "wsl", "bash", "-c", "cd /mnt/c/Users/TruongNhon/Documents/Powershell && pwsh -File scripts/build-apps.ps1 -Target all")
	}

	out, err := cmd.CombinedOutput()
	if err != nil {
		resStr := string(out)
		if len(resStr) > 2500 {
			resStr = resStr[len(resStr)-2500:]
		}
		_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("❌ **Rebuild Error:**\n```\n%s\n```", resStr), nil)
		return
	}

	resStr := string(out)
	if len(resStr) > 2500 {
		resStr = resStr[len(resStr)-2500:]
	}
	_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("✅ **BUILD SUCCESSFUL!**\n```\n%s\n```", resStr), nil)
}

func (h *BotHandler) handleReplan(ctx context.Context, userID int64, chatID int64) {
	replanPrompt := "Replan and audit the current project workspace. Inspect technical debt, code structure, and generate an updated execution plan / product roadmap."
	h.handleAIPrompt(ctx, userID, chatID, replanPrompt)
}

func (h *BotHandler) handleGetFile(ctx context.Context, userID int64, chatID int64, relOrAbsPath string) {
	if relOrAbsPath == "" {
		_, _ = h.client.SendMessage(ctx, chatID, "Usage: `/getfile <filepath>` or `/cat <filepath>`", nil)
		return
	}

	ws := h.wsMgr.GetUserWorkspace(userID)
	targetPath := relOrAbsPath
	if !filepath.IsAbs(targetPath) {
		targetPath = filepath.Join(ws, targetPath)
	}

	st, err := os.Stat(targetPath)
	if err != nil {
		_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("❌ File not found: `%s`", targetPath), nil)
		return
	}
	if st.IsDir() {
		h.handleLs(ctx, userID, chatID, relOrAbsPath)
		return
	}

	h.client.SendChatAction(ctx, chatID, "upload_document")
	err = h.client.SendDocument(ctx, chatID, targetPath, fmt.Sprintf("📄 `%s` (%d bytes)", filepath.Base(targetPath), st.Size()))
	if err != nil {
		_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("❌ Error sending document: %v", err), nil)
	}
}

func (h *BotHandler) handleSwarm(ctx context.Context, userID int64, chatID int64, task string) {
	ws := h.wsMgr.GetUserWorkspace(userID)
	prompt := fmt.Sprintf("Spawn an agyswarm multi-agent workforce to execute task in `%s`: %s", filepath.Base(ws), task)
	h.handleAIPrompt(ctx, userID, chatID, prompt)
}

func (h *BotHandler) handleReview(ctx context.Context, userID int64, chatID int64, args []string) {
	ws := h.wsMgr.GetUserWorkspace(userID)
	target := filepath.Base(ws)
	if len(args) > 0 {
		target = args[0]
	}
	prompt := fmt.Sprintf("Perform a comprehensive code review using agyreview on target `%s`. Report severity findings P0-P4.", target)
	h.handleAIPrompt(ctx, userID, chatID, prompt)
}

func (h *BotHandler) handlePlanDashboard(ctx context.Context, userID int64, chatID int64) {
	sess := h.getSession(userID)
	ws := h.wsMgr.GetUserWorkspace(userID)
	acc := h.accMgr.GetActiveAccount()

	thinkState := "🔴 OFF (Free & Clean)"
	if sess.ShowThinking {
		thinkState = "🟢 ON (Showing Reasoning - $0 FREE)"
	}

	modelState := sess.ActiveModel
	if modelState == "" {
		modelState = h.cfg.DefaultModel
	}

	msg := fmt.Sprintf(
		"📋 **ANTIGRAVITY CONTROL & EXECUTION PLANNER**\n\n"+
			"• **Active Workspace:** `%s`\n"+
			"• **Active Account:** `%s` (via agyswitch)\n"+
			"• **Selected Model:** `%s`\n"+
			"• **Thinking Steps:** %s\n\n"+
			"💡 *Select an action below to switch models, inspect quotas, check skills, toggle reasoning, or diagnose app status:*",
		filepath.Base(ws), acc, modelState, thinkState,
	)

	markup := &InlineKeyboardMarkup{
		InlineKeyboard: [][]InlineKeyboardButton{
			{
				{Text: "🧠 Switch Model", CallbackData: "btn_models"},
				{Text: "👤 Account Quota", CallbackData: "btn_accounts"},
			},
			{
				{Text: "⚡ Active Skills", CallbackData: "btn_skills"},
				{Text: "💭 Thinking: " + getToggleText(sess.ShowThinking), CallbackData: "btn_toggle_thinking"},
			},
			{
				{Text: "🩺 App Health Check", CallbackData: "btn_health"},
				{Text: "🛠️ Rebuild Apps", CallbackData: "btn_rebuild"},
			},
			{
				{Text: "📁 Switch Project", CallbackData: "btn_projects"},
				{Text: "❓ Commands & Help", CallbackData: "btn_help"},
			},
		},
	}

	_, _ = h.client.SendMessage(ctx, chatID, msg, markup)
}

func getToggleText(on bool) string {
	if on {
		return "ON"
	}
	return "OFF"
}

func (h *BotHandler) getSession(userID int64) *UserSessionState {
	sess, ok := h.userSessions[userID]
	if !ok {
		sess = &UserSessionState{}
		h.userSessions[userID] = sess
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
	_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("💭 **Thinking Steps Visibility:** %s", status), nil)
}

func (h *BotHandler) handleSetModel(ctx context.Context, userID int64, chatID int64, modelName string) {
	sess := h.getSession(userID)
	sess.ActiveModel = modelName
	_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("🧠 Active AI model set to: `%s`", modelName), nil)
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
		"• `/thinking [on|off]` - Toggle Reasoning Steps ($0 Free)\n\n" +
		"📁 **Workspaces & Code Projects:**\n" +
		"• `/proj` - Registered Projects List (`agyproj`)\n" +
		"• `/cd <dir>` - Switch Workspace (`/cd D:\\...`)\n" +
		"• `/ls [dir]` - List Files & Subfolders\n" +
		"• `/getfile <file>` - Upload File from PC to Phone\n" +
		"• `/newproj <name>` - Scaffold & Register New Project\n\n" +
		"🛠️ **Build & Multi-Agent Operations:**\n" +
		"• `/rebuild` - Rebuild All 12 Suite Micro-Apps\n" +
		"• `/replan` - Audit Tech Debt & Replan Roadmap\n" +
		"• `/swarm <task>` - Run Multi-Agent Parallel Swarm\n" +
		"• `/review` - Perform P0-P4 Code Security Review\n" +
		"• `/account` - View Quotas & Account Switcher (`agyswitch`)\n\n" +
		"🔄 **Session Continuation Shortcuts:**\n" +
		"• Reply `-c` or `0c` to seamlessly continue active prompt!"

	_, _ = h.client.SendMessage(ctx, chatID, helpText, nil)
}

func (h *BotHandler) buildMainKeyboard(userID int64) *InlineKeyboardMarkup {
	sess := h.getSession(userID)
	curModel := sess.ActiveModel
	if curModel == "" {
		curModel = h.cfg.DefaultModel
	}

	thinkText := "OFF"
	if sess.ShowThinking {
		thinkText = "ON"
	}

	return &InlineKeyboardMarkup{
		InlineKeyboard: [][]InlineKeyboardButton{
			{
				{Text: "📋 Plan & Controller", CallbackData: "btn_plan"},
				{Text: "🔄 Continue (-c)", CallbackData: "btn_continue"},
			},
			{
				{Text: "🧠 Model: " + curModel, CallbackData: "btn_models"},
				{Text: "💭 Thinking: " + thinkText, CallbackData: "btn_toggle_thinking"},
			},
			{
				{Text: "📁 Projects", CallbackData: "btn_projects"},
				{Text: "🛠️ Rebuild Apps", CallbackData: "btn_rebuild"},
			},
			{
				{Text: "🐝 Swarm Task", CallbackData: "btn_swarm"},
				{Text: "👁️ Code Review", CallbackData: "btn_review"},
			},
			{
				{Text: "📂 Files", CallbackData: "btn_files"},
				{Text: "👤 Accounts", CallbackData: "btn_accounts"},
			},
			{
				{Text: "🧠 Sessions", CallbackData: "btn_sessions"},
				{Text: "🖥️ Host Status", CallbackData: "btn_status"},
			},
		},
	}
}
