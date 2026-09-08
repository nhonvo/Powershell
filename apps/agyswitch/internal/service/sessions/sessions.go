package sessions

import (
	"bufio"
	"encoding/json"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"sort"
	"strings"
	"time"

	"agyswitch/internal/model"
)

type Manager struct {
	UserHome string
}

func NewManager(userHome string) *Manager {
	if userHome == "" {
		if h, err := os.UserHomeDir(); err == nil {
			userHome = h
		}
	}
	return &Manager{UserHome: userHome}
}

// DiscoverPrimarySessions returns top-level CLI sessions matching agy /resume dialog.
func (m *Manager) DiscoverPrimarySessions() ([]model.SessionInfo, error) {
	dbPath := filepath.Join(m.UserHome, ".gemini", "antigravity-cli", "conversation_summaries.db")
	if fi, err := os.Stat(dbPath); err == nil && !fi.IsDir() {
		if sessions, err := m.queryConversationSummaries(dbPath); err == nil && len(sessions) > 0 {
			return sessions, nil
		}
	}
	return m.scanBrainDirectory()
}

// DiscoverAllSessions returns both primary CLI sessions and all internal subagent tasks.
func (m *Manager) DiscoverAllSessions() ([]model.SessionInfo, error) {
	primary, _ := m.DiscoverPrimarySessions()
	primaryMap := make(map[string]bool)
	for _, p := range primary {
		primaryMap[p.ConversationID] = true
	}

	brainSessions, _ := m.scanBrainDirectory()
	var merged []model.SessionInfo
	merged = append(merged, primary...)

	for _, bs := range brainSessions {
		if !primaryMap[bs.ConversationID] {
			bs.IsSubagent = true
			if !strings.HasPrefix(bs.Title, "[Subagent]") {
				bs.Title = "[Subagent] " + bs.Title
			}
			merged = append(merged, bs)
		}
	}

	sort.Slice(merged, func(i, j int) bool {
		return merged[i].LastActive.After(merged[j].LastActive)
	})

	return merged, nil
}

// DiscoverSessions defaults to DiscoverPrimarySessions matching /resume dialog.
func (m *Manager) DiscoverSessions() ([]model.SessionInfo, error) {
	return m.DiscoverPrimarySessions()
}

type rawSummaryRecord struct {
	ConversationID string `json:"conversation_id"`
	Title          string `json:"title"`
	StepCount      int    `json:"step_count"`
	LastActiveUnix int64  `json:"last_active_unix"`
	WorkspaceDir   string `json:"workspace_dir"`
	IsSubagent     bool   `json:"is_subagent"`
	ParentID       string `json:"parent_id"`
}

func (m *Manager) queryConversationSummaries(dbPath string) ([]model.SessionInfo, error) {
	script := `
import sqlite3, json, sys
from datetime import datetime

db_path = sys.argv[1]
conn = sqlite3.connect(db_path)
c = conn.cursor()
rows = c.execute("""
    SELECT conversation_id, title, preview, step_count, last_modified_time, workspace_uris, nesting_depth, parent_conversation_id 
    FROM conversation_summaries 
    WHERE nesting_depth = 0 AND (parent_conversation_id IS NULL OR parent_conversation_id = "")
    ORDER BY datetime(last_modified_time) DESC;
""").fetchall()

out = []
for r in rows:
    cid, title, prev, steps, mtime, uris, depth, parent = r
    t = title.strip() if title else ""
    if not t:
        t = prev.strip() if prev else "Untitled Conversation"
    ws = ""
    if uris:
        try:
            parsed = json.loads(uris)
            if isinstance(parsed, list) and len(parsed) > 0:
                ws = parsed[0].replace("file://", "")
        except:
            ws = uris
    ts = 0
    if mtime:
        try:
            ts = int(datetime.fromisoformat(mtime).timestamp())
        except:
            pass
    out.append({
        "conversation_id": cid,
        "title": t,
        "step_count": steps,
        "last_active_unix": ts,
        "workspace_dir": ws,
        "is_subagent": bool(depth > 0 or parent),
        "parent_id": parent or ""
    })
print(json.dumps(out))
`
	cmd := exec.Command("python3", "-c", script, dbPath)
	out, err := cmd.Output()
	if err != nil {
		return nil, err
	}

	var rawList []rawSummaryRecord
	if err := json.Unmarshal(out, &rawList); err != nil {
		return nil, err
	}

	brainDir := filepath.Join(m.UserHome, ".gemini", "antigravity-cli", "brain")
	var results []model.SessionInfo
	for _, r := range rawList {
		ws := CleanWorkspaceDir(r.WorkspaceDir)
		proj := ExtractProjectName(ws)
		lastActive := time.Now()
		if r.LastActiveUnix > 0 {
			lastActive = time.Unix(r.LastActiveUnix, 0)
		}
		logPath := filepath.Join(brainDir, r.ConversationID, ".system_generated", "logs", "transcript.jsonl")
		results = append(results, model.SessionInfo{
			ConversationID: r.ConversationID,
			Title:          r.Title,
			WorkspaceDir:   ws,
			ProjectName:    proj,
			LastActive:     lastActive,
			StepCount:      r.StepCount,
			EstimatedCost:  CalculateSessionCost(r.StepCount),
			LogPath:        logPath,
			IsSubagent:     r.IsSubagent,
			ParentID:       r.ParentID,
		})
	}

	return results, nil
}

// scanBrainDirectory parses brain/ logs directory for active conversation transcripts.
func (m *Manager) scanBrainDirectory() ([]model.SessionInfo, error) {
	brainDir := filepath.Join(m.UserHome, ".gemini", "antigravity-cli", "brain")
	entries, err := os.ReadDir(brainDir)
	if err != nil {
		return nil, err
	}

	var results []model.SessionInfo
	for _, e := range entries {
		if e.IsDir() && e.Name() != "scratch" {
			logPath := filepath.Join(brainDir, e.Name(), ".system_generated", "logs", "transcript.jsonl")
			info, err := os.Stat(logPath)
			if err == nil {
				stepCount := countLines(logPath)
				estimatedCost := CalculateSessionCost(stepCount)
				title, wsDir, projName := parseSessionMetadata(logPath)
				results = append(results, model.SessionInfo{
					ConversationID: e.Name(),
					Title:          title,
					WorkspaceDir:   wsDir,
					ProjectName:    projName,
					LastActive:     info.ModTime(),
					StepCount:      stepCount,
					EstimatedCost:  estimatedCost,
					LogPath:        logPath,
				})
			}
		}
	}

	// Sort chronologically with most recently active sessions first
	sort.Slice(results, func(i, j int) bool {
		return results[i].LastActive.After(results[j].LastActive)
	})

	return results, nil
}

// ExtractProjectName extracts a clean project name from a workspace directory path.
func ExtractProjectName(workspaceDir string) string {
	if workspaceDir == "" || workspaceDir == "Default Workspace" {
		return "Default Workspace"
	}
	clean := filepath.Clean(workspaceDir)
	parts := strings.Split(clean, string(filepath.Separator))
	for i, part := range parts {
		if strings.EqualFold(part, "projects") && i+1 < len(parts) {
			return parts[i+1]
		}
	}
	base := filepath.Base(clean)
	if base != "" && base != "/" && base != "." {
		return base
	}
	return "Default Workspace"
}

// CleanWorkspaceDir normalizes and resolves any candidate workspace path to its project root directory.
func CleanWorkspaceDir(raw string) string {
	if raw == "" || raw == "Default Workspace" {
		return "Default Workspace"
	}

	// 1. Trim surrounding quotes, escapes, backticks, backslashes, whitespace
	raw = strings.Trim(raw, " \"'`\\\r\n\t")
	raw = strings.TrimRight(raw, ".,;:`")

	clean := filepath.Clean(raw)

	// 2. Project root detection for /projects/<projectName>
	parts := strings.Split(clean, string(filepath.Separator))
	for i, part := range parts {
		if strings.EqualFold(part, "projects") && i+1 < len(parts) {
			rootParts := parts[:i+2]
			root := strings.Join(rootParts, string(filepath.Separator))
			if strings.HasPrefix(clean, string(filepath.Separator)) && !strings.HasPrefix(root, string(filepath.Separator)) {
				root = string(filepath.Separator) + root
			}
			return root
		}
	}

	// 3. Strip internal tool directories like .agents, .gemini, .git
	for _, marker := range []string{"/.agents", "/.gemini", "/.git", "/.vscode", "/.system_generated"} {
		if idx := strings.Index(clean, marker); idx != -1 {
			clean = clean[:idx]
		}
	}

	// 4. If it points to a file, take its parent directory
	if fi, err := os.Stat(clean); err == nil {
		if !fi.IsDir() {
			clean = filepath.Dir(clean)
		}
	} else if filepath.Ext(clean) != "" {
		clean = filepath.Dir(clean)
	}

	clean = filepath.Clean(clean)
	if clean == "" || clean == "." || clean == "/" {
		return "Default Workspace"
	}
	return clean
}

// GroupSessionsByProject groups sessions by their associated project workspace.
func GroupSessionsByProject(sessions []model.SessionInfo) []model.ProjectGroup {
	return GroupSessionsByProjectSorted(sessions, 0)
}

// GroupSessionsByProjectSorted groups sessions and sorts groups:
// sortMode: 0 = Recency (LastActive), 1 = TotalCost, 2 = TotalSteps
func GroupSessionsByProjectSorted(sessions []model.SessionInfo, sortMode int) []model.ProjectGroup {
	groupMap := make(map[string]*model.ProjectGroup)
	var order []string

	for _, s := range sessions {
		proj := s.ProjectName
		if proj == "" {
			proj = "Default Workspace"
		}
		g, exists := groupMap[proj]
		if !exists {
			g = &model.ProjectGroup{
				ProjectName:  proj,
				WorkspaceDir: s.WorkspaceDir,
				LastActive:   s.LastActive,
			}
			groupMap[proj] = g
			order = append(order, proj)
		}
		g.Sessions = append(g.Sessions, s)
		g.TotalSteps += s.StepCount
		g.TotalCost += s.EstimatedCost
		if s.LastActive.After(g.LastActive) {
			g.LastActive = s.LastActive
		}
	}

	var result []model.ProjectGroup
	for _, name := range order {
		result = append(result, *groupMap[name])
	}

	switch sortMode {
	case 1: // Total Cost
		sort.Slice(result, func(i, j int) bool {
			return result[i].TotalCost > result[j].TotalCost
		})
	case 2: // Total Steps
		sort.Slice(result, func(i, j int) bool {
			return result[i].TotalSteps > result[j].TotalSteps
		})
	default: // 0: Recency
		sort.Slice(result, func(i, j int) bool {
			return result[i].LastActive.After(result[j].LastActive)
		})
	}

	return result
}

// FlattenProjectGroups flattens project groups into a contiguous slice of sessions grouped by project.
func FlattenProjectGroups(groups []model.ProjectGroup) []model.SessionInfo {
	var result []model.SessionInfo
	for _, g := range groups {
		result = append(result, g.Sessions...)
	}
	return result
}

// DeleteSession purges a conversation directory from the brain directory.
func (m *Manager) DeleteSession(conversationID string) error {
	if conversationID == "" {
		return fmt.Errorf("empty conversation id")
	}
	brainDir := filepath.Join(m.UserHome, ".gemini", "antigravity-cli", "brain", conversationID)
	return os.RemoveAll(brainDir)
}

func parseSessionMetadata(logPath string) (title string, workspaceDir string, projectName string) {
	f, err := os.Open(logPath)
	if err != nil {
		return "Untitled Session", "Default Workspace", "Default Workspace"
	}
	defer f.Close()

	scanner := bufio.NewScanner(f)
	buf := make([]byte, 0, 64*1024)
	scanner.Buffer(buf, 1024*1024)

	lineCount := 0
	for scanner.Scan() {
		lineCount++
		lineStr := scanner.Text()

		// 1. Detect Cwd in tool calls args
		if workspaceDir == "" {
			if idx := strings.Index(lineStr, `"Cwd"`); idx != -1 {
				rest := lineStr[idx+5:]
				if cIdx := strings.Index(rest, ":"); cIdx != -1 {
					val := strings.TrimSpace(rest[cIdx+1:])
					val = strings.TrimLeft(val, " \"\\")
					if end := strings.IndexAny(val, "\"\\,\r\n}"); end > 0 {
						cand := val[:end]
						if strings.HasPrefix(cand, "/") || strings.Contains(cand, ":\\") {
							workspaceDir = CleanWorkspaceDir(cand)
						}
					}
				}
			}
		}

		// 2. Secondary detection: Look for /projects/ path in tool calls or user_information
		if workspaceDir == "" {
			if pIdx := strings.Index(lineStr, "/projects/"); pIdx != -1 {
				start := strings.LastIndexAny(lineStr[:pIdx], " \"'\\=\t\n`")
				if start == -1 {
					start = 0
				} else {
					start++
				}
				cand := lineStr[start:]
				if end := strings.IndexAny(cand, " \"'\\,\r\n}>`"); end > 0 {
					cand = cand[:end]
				}
				cand = strings.Trim(cand, " \"'\\`")
				if strings.HasPrefix(cand, "/") {
					workspaceDir = CleanWorkspaceDir(cand)
				}
			}
		}

		// 3. Detect user prompt / title from USER_INPUT
		if title == "" {
			if strings.Contains(lineStr, `"USER_INPUT"`) {
				var rec struct {
					Content string `json:"content"`
				}
				if json.Unmarshal([]byte(lineStr), &rec) == nil && rec.Content != "" {
					clean := rec.Content
					clean = strings.ReplaceAll(clean, "<USER_REQUEST>", "")
					clean = strings.ReplaceAll(clean, "</USER_REQUEST>", "")
					clean = strings.TrimSpace(clean)
					lines := strings.Split(clean, "\n")
					for _, l := range lines {
						l = strings.TrimSpace(l)
						if l != "" && !strings.HasPrefix(l, "<") && !strings.HasPrefix(l, "The current") && !strings.HasPrefix(l, "-") {
							title = l
							break
						}
					}
				}
			}
		}

		if lineCount > 35 && title != "" {
			break
		}
	}

	if title == "" {
		title = "Untitled Agent Task"
	}
	if workspaceDir == "" {
		workspaceDir = "Default Workspace"
	} else {
		workspaceDir = CleanWorkspaceDir(workspaceDir)
	}

	if len(title) > 60 {
		title = title[:57] + "..."
	}

	projectName = ExtractProjectName(workspaceDir)
	return title, workspaceDir, projectName
}

func CalculateSessionCost(stepCount int) float64 {
	if stepCount <= 0 {
		return 0.0
	}
	return float64(stepCount*400) / 1000000.0 * 1.25
}

func countLines(path string) int {
	f, err := os.Open(path)
	if err != nil {
		return 0
	}
	defer f.Close()

	scanner := bufio.NewScanner(f)
	count := 0
	for scanner.Scan() {
		count++
	}
	return count
}

type StepRecord struct {
	StepIndex int    `json:"step_index"`
	Type      string `json:"type"`
	Status    string `json:"status"`
}

// ParseTranscriptSteps reads transcript.jsonl lines into structured step history.
func ParseTranscriptSteps(logPath string) ([]StepRecord, error) {
	f, err := os.Open(logPath)
	if err != nil {
		return nil, err
	}
	defer f.Close()

	var steps []StepRecord
	scanner := bufio.NewScanner(f)
	for scanner.Scan() {
		var rec StepRecord
		if json.Unmarshal(scanner.Bytes(), &rec) == nil {
			steps = append(steps, rec)
		}
	}
	return steps, nil
}
