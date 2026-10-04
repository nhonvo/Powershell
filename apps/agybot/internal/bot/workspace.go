package bot

import (
	"context"
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"time"
)

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

	sb.WriteString("\n👉 *Tap a button below or send:* `/cd <number_or_name>` *(or reply with number)*")
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
	_, _ = h.client.SendMessage(ctx, chatID, fmt.Sprintf("✅ Workspace switched to **%s**:\n`%s`", filepath.Base(newWs), newWs), h.buildMainKeyboard(userID))
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

func (h *BotHandler) handleSearch(ctx context.Context, userID int64, chatID int64, query string) {
	query = strings.TrimSpace(query)
	if query == "" {
		msg := "🔎 **WORKSPACE FILE SEARCH**\n\n" +
			"Please send your search keyword or filename pattern.\n\n" +
			"👉 Examples:\n" +
			"• `/search auth` - Find auth handlers & files\n" +
			"• `/search main.go` - Find main entrypoints\n" +
			"• `/search config` - Search config variables"
		_, _ = h.client.SendMessage(ctx, chatID, msg, nil)
		return
	}

	ws := h.wsMgr.GetUserWorkspace(userID)
	var matches []string
	queryLower := strings.ToLower(query)

	_ = filepath.Walk(ws, func(path string, info os.FileInfo, err error) error {
		if err != nil {
			return nil
		}
		if info.IsDir() {
			base := filepath.Base(path)
			if base == ".git" || base == "node_modules" || base == "vendor" || base == ".gemini" || base == "dist" {
				return filepath.SkipDir
			}
			return nil
		}

		rel, errRel := filepath.Rel(ws, path)
		if errRel == nil && strings.Contains(strings.ToLower(rel), queryLower) {
			matches = append(matches, rel)
			if len(matches) >= 15 {
				return filepath.SkipAll
			}
		}
		return nil
	})

	var sb strings.Builder
	sb.WriteString(fmt.Sprintf("🔎 **SEARCH RESULTS FOR:** `%s` (in `%s`)\n\n", query, filepath.Base(ws)))

	if len(matches) == 0 {
		sb.WriteString("⚠️ No local matching files found by filename.\n\n")
	} else {
		sb.WriteString(fmt.Sprintf("📁 **Found %d matching file(s):**\n", len(matches)))
		for i, m := range matches {
			sb.WriteString(fmt.Sprintf("%d. `%s`\n", i+1, m))
		}
		sb.WriteString("\n")
	}

	sb.WriteString("👉 *Tap button below if you want AI to analyze and summarize code usage:*")

	aiBtn := &InlineKeyboardMarkup{
		InlineKeyboard: [][]InlineKeyboardButton{
			{
				{Text: "🤖 Summarize Code Usage with AI", CallbackData: "ai_search_" + query},
			},
		},
	}

	_, _ = h.client.SendMessage(ctx, chatID, sb.String(), aiBtn)
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
	_, _ = h.client.SendMessage(ctx, chatID, msg, h.buildMainKeyboard(userID))
}
