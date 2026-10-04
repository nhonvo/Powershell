package bot

import (
	"context"
	"fmt"
	"strings"
	"time"
)

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
	sb.WriteString("\n👉 Resume any session with: `/resume <conversation_id>`\n👉 Start fresh: `/clear`")
	_, _ = h.client.SendMessage(ctx, chatID, sb.String(), nil)
}

func (h *BotHandler) handleResume(ctx context.Context, userID int64, chatID int64, args []string) {
	if len(args) == 0 {
		_, _ = h.client.SendMessage(ctx, chatID, "Usage: `/resume <conversation_id>`", nil)
		return
	}
	convID := strings.TrimSpace(args[0])
	sess := h.getSession(userID)
	sess.ActiveConvID = convID

	_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("🔄 Resumed Antigravity conversation: `%s`.\nNext prompts will continue with full session context!", convID), h.buildMainKeyboard(userID))
}

func (h *BotHandler) handleCurrentSession(ctx context.Context, userID int64, chatID int64) {
	sess := h.getSession(userID)
	convID := "None (New Session)"
	if sess.ActiveConvID != "" {
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
			"👉 Switch session: `/resume <id>` or `/clear`",
		convID, ws, acc, h.cfg.DefaultModel,
	)
	_, _ = h.client.SendMessage(ctx, chatID, msg, nil)
}

func (h *BotHandler) handleClear(ctx context.Context, userID int64, chatID int64) {
	sess := h.getSession(userID)
	sess.ActiveConvID = ""
	_, _ = h.client.SendMessage(ctx, chatID, "🔄 **Conversation Context Cleared!** Ready for a fresh prompt session.", h.buildMainKeyboard(userID))
}
