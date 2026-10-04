package bot

import (
	"context"
	"fmt"
	"os/exec"
	"path/filepath"
	"runtime"
	"strings"
	"time"

	"agybot/internal/runner"
)

func (h *BotHandler) handleAIPrompt(ctx context.Context, userID int64, chatID int64, prompt string) {
	ws := h.wsMgr.GetUserWorkspace(userID)
	sess := h.getSession(userID)
	startTime := time.Now()

	h.client.SendChatAction(ctx, chatID, "typing")

	cancelMarkup := &InlineKeyboardMarkup{
		InlineKeyboard: [][]InlineKeyboardButton{
			{
				{Text: "🛑 Abort / Cancel Request", CallbackData: "cancel_prompt"},
			},
		},
	}

	initialMsg, err := h.client.SendMessage(ctx, chatID, fmt.Sprintf("🤖 **Antigravity AI Agent**\n📍 Workspace: `%s`\n⚡ Working on: _%s_...", filepath.Base(ws), prompt), cancelMarkup)
	if err != nil {
		return
	}

	runCtx, cancel := context.WithTimeout(ctx, h.cfg.TaskTimeout)
	defer cancel()

	h.registerCancel(userID, cancel)
	defer h.unregisterCancel(userID)

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
		case "thinking":
			if sess.ShowThinking && ev.Content != "" {
				thinkSnippet := ev.Content
				if len(thinkSnippet) > 300 {
					thinkSnippet = thinkSnippet[:300] + "..."
				}
				_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("💭 *Thinking:* _%s_", thinkSnippet), nil)
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

	// Update telemetry metrics hook
	elapsed := time.Since(startTime).Round(time.Millisecond)
	estTokens := len(ans) / 4
	if estTokens < 50 {
		estTokens = 50
	}
	estCost := float64(estTokens) * 0.000002

	sess.Metrics.TotalPrompts++
	sess.Metrics.TotalSteps += toolCount + 1
	sess.Metrics.EstTokens += estTokens
	sess.Metrics.EstCostUSD += estCost

	// Append metrics footer
	ansWithMetrics := fmt.Sprintf("%s\n\n⏱️ _Completed in %v · est: %d tokens ($%.5f USD)_", ans, elapsed, estTokens, estCost)

	// Update initial status indicator to mark completion
	_ = h.client.EditMessageText(ctx, chatID, initialMsg.MessageID, fmt.Sprintf("🤖 **Antigravity Response** (in `%s`):", filepath.Base(ws)), nil)

	// Send complete response (chunked if > 3900 chars)
	sendChunkedMessage(ctx, h.client, chatID, ansWithMetrics)
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
	_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("✅ **BUILD SUCCESSFUL!**\n```\n%s\n```", resStr), h.buildMainKeyboard(userID))
}

func (h *BotHandler) handleReplan(ctx context.Context, userID int64, chatID int64) {
	replanPrompt := "Replan and audit the current project workspace. Inspect technical debt, code structure, and generate an updated execution plan / product roadmap."
	h.handleAIPrompt(ctx, userID, chatID, replanPrompt)
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

func (h *BotHandler) handleMetrics(ctx context.Context, userID int64, chatID int64) {
	sess := h.getSession(userID)
	m := sess.Metrics
	msg := fmt.Sprintf(
		"📊 **SESSION TELEMETRY & COST HOOK**\n\n"+
			"• **Total Prompts:** `%d`\n"+
			"• **Total Tool Steps:** `%d`\n"+
			"• **Estimated Tokens:** `%d tokens`\n"+
			"• **Estimated Cost:** `$%.5f USD`\n"+
			"• **Cost Efficiency:** `100%% Local / Unlimited`\n",
		m.TotalPrompts, m.TotalSteps, m.EstTokens, m.EstCostUSD,
	)
	_, _ = h.client.SendMessage(ctx, chatID, msg, h.buildMainKeyboard(userID))
}
