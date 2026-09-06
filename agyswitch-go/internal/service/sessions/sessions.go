package sessions

import (
	"bufio"
	"encoding/json"
	"os"
	"path/filepath"
	"strings"

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

// DiscoverSessions parses brain/ logs directory for active conversation transcripts.
func (m *Manager) DiscoverSessions() ([]model.SessionInfo, error) {
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
				title, wsDir := parseSessionMetadata(logPath)
				results = append(results, model.SessionInfo{
					ConversationID: e.Name(),
					Title:          title,
					WorkspaceDir:   wsDir,
					LastActive:     info.ModTime(),
					StepCount:      stepCount,
					EstimatedCost:  estimatedCost,
					LogPath:        logPath,
				})
			}
		}
	}
	return results, nil
}

func parseSessionMetadata(logPath string) (title string, workspaceDir string) {
	f, err := os.Open(logPath)
	if err != nil {
		return "Untitled Session", "Default Workspace"
	}
	defer f.Close()

	scanner := bufio.NewScanner(f)
	buf := make([]byte, 0, 64*1024)
	scanner.Buffer(buf, 1024*1024)

	for scanner.Scan() {
		line := scanner.Bytes()

		if workspaceDir == "" {
			if idx := strings.Index(string(line), `"Cwd"`); idx != -1 {
				sub := string(line)[idx:]
				parts := strings.SplitN(sub, `"`, 5)
				if len(parts) >= 4 {
					workspaceDir = strings.Trim(parts[3], `\"`)
				}
			}
		}

		if title == "" {
			if strings.Contains(string(line), `"USER_INPUT"`) {
				var rec struct {
					Content string `json:"content"`
				}
				if json.Unmarshal(line, &rec) == nil && rec.Content != "" {
					clean := rec.Content
					clean = strings.ReplaceAll(clean, "<USER_REQUEST>", "")
					clean = strings.ReplaceAll(clean, "</USER_REQUEST>", "")
					clean = strings.TrimSpace(clean)
					lines := strings.Split(clean, "\n")
					for _, l := range lines {
						l = strings.TrimSpace(l)
						if l != "" && !strings.HasPrefix(l, "<") && !strings.HasPrefix(l, "The current") {
							title = l
							break
						}
					}
				}
			}
		}

		if title != "" && workspaceDir != "" {
			break
		}
	}

	if title == "" {
		title = "Untitled Agent Task"
	}
	if workspaceDir == "" {
		workspaceDir = "Default Workspace"
	}

	if len(title) > 60 {
		title = title[:57] + "..."
	}

	return title, workspaceDir
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
