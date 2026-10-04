package bot

import (
	"context"
	"fmt"
	"path/filepath"
	"strconv"
	"strings"
	"sync"
	"time"

	"agybot/internal/account"
	"agybot/internal/auth"
	"agybot/internal/config"
	"agybot/internal/runner"
	"agybot/internal/security"
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
	userCancels  map[int64]context.CancelFunc
	mu           sync.RWMutex
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
		userCancels:  make(map[int64]context.CancelFunc),
	}
}

func (h *BotHandler) registerCancel(userID int64, cancel context.CancelFunc) {
	h.mu.Lock()
	defer h.mu.Unlock()
	h.userCancels[userID] = cancel
}

func (h *BotHandler) unregisterCancel(userID int64) {
	h.mu.Lock()
	defer h.mu.Unlock()
	delete(h.userCancels, userID)
}

func (h *BotHandler) handleCancel(ctx context.Context, userID int64, chatID int64) {
	h.mu.Lock()
	cancelFunc, exists := h.userCancels[userID]
	if exists && cancelFunc != nil {
		cancelFunc()
		delete(h.userCancels, userID)
		h.mu.Unlock()
		_, _ = h.client.SendMessage(ctx, chatID, "🛑 **Task request cancelled by user.**", h.buildMainKeyboard(userID))
	} else {
		h.mu.Unlock()
		_, _ = h.client.SendMessage(ctx, chatID, "ℹ️ No active task or prompt is currently running.", h.buildMainKeyboard(userID))
	}
}

// StartPolling launches long-polling loop against Telegram Bot API
func (h *BotHandler) StartPolling(ctx context.Context) error {
	// Register 25 slash commands with Telegram API for auto-complete menu
	err := h.client.SetMyCommands(ctx, []BotCommand{
		{Command: "start", Description: "Start & Authenticate Remote Controller"},
		{Command: "plan", Description: "Interactive Plan & Controller Dashboard"},
		{Command: "status", Description: "Host CPU, RAM, Containers & Session Metrics"},
		{Command: "health", Description: "Micro-App & System Health Check"},
		{Command: "cancel", Description: "Abort Active AI Prompt or Task"},
		{Command: "model", Description: "Switch Active AI Model (Gemini/Claude/Ollama)"},
		{Command: "effort", Description: "Set Model Reasoning Effort (High/Medium/Low)"},
		{Command: "quota", Description: "Check Account Quotas (agyswitch)"},
		{Command: "thinking", Description: "Toggle Reasoning Steps ($0 Free)"},
		{Command: "metrics", Description: "View Token Usage & Cost Telemetry"},
		{Command: "skills", Description: "Inspect Loaded Skills & Plugins"},
		{Command: "proj", Description: "Registered Projects List (agyproj)"},
		{Command: "cd", Description: "Switch Workspace Directory"},
		{Command: "ls", Description: "List Directory Files & Folders"},
		{Command: "cat", Description: "View File Content"},
		{Command: "search", Description: "Deep Workspace Search & AI Summary"},
		{Command: "getfile", Description: "Download File from PC to Phone"},
		{Command: "newproj", Description: "Scaffold & Register New Project"},
		{Command: "clear", Description: "Clear Context & Start Fresh"},
		{Command: "rebuild", Description: "Rebuild All 12 Micro-Apps"},
		{Command: "replan", Description: "Audit Tech Debt & Replan Roadmap"},
		{Command: "swarm", Description: "Run Multi-Agent Parallel Swarm"},
		{Command: "review", Description: "Perform Code Security Review"},
		{Command: "config", Description: "Show Bot System Configuration"},
		{Command: "help", Description: "Bot Commands & Reference Guide"},
	})
	if err != nil {
		fmt.Printf("⚠️ SetMyCommands Error: %v\n", err)
	} else {
		fmt.Println("✅ Successfully registered 25 slash commands with Telegram Bot API!")
	}

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
		case "/cancel", "/stop", "/abort":
			h.handleCancel(ctx, userID, chatID)
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
		case "/metrics", "/token", "/tokens":
			h.handleMetrics(ctx, userID, chatID)
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
		case "/search", "/find":
			q := ""
			if len(parts) > 1 {
				q = strings.Join(parts[1:], " ")
			}
			h.handleSearch(ctx, userID, chatID, q)
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
		case "/clear":
			h.handleClear(ctx, userID, chatID)
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
			h.handleClear(ctx, userID, chatID)
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

	if lowerText == "🛑 cancel" || lowerText == "cancel" || lowerText == "stop" || lowerText == "abort" {
		h.handleCancel(ctx, userID, chatID)
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

	if data == "cancel_prompt" {
		h.handleCancel(ctx, userID, chatID)
		return
	}

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
	case "btn_metrics":
		h.handleMetrics(ctx, userID, chatID)
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
	case "btn_clear":
		h.handleClear(ctx, userID, chatID)
	case "btn_lock":
		h.authMgr.Lock(userID)
		_, _ = h.client.SendMessage(ctx, chatID, "🔒 **Antigravity Controller locked.**", nil)
	case "btn_unlock":
		h.authMgr.SetAwaitingPin(userID, true)
		_, _ = h.client.SendMessage(ctx, chatID, "🔐 **Send your security PIN:**", nil)
	}
}

func (h *BotHandler) handleFinanceDashboard(ctx context.Context, userID int64, chatID int64) {
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
	_, _ = h.client.SendMessage(ctx, chatID, msg, h.buildMainKeyboard(userID))
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
