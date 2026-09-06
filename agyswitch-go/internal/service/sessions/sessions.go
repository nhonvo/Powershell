package sessions

import (
	"bufio"
	"encoding/json"
	"os"
	"path/filepath"

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
				results = append(results, model.SessionInfo{
					ConversationID: e.Name(),
					LastActive:     info.ModTime(),
					StepCount:      stepCount,
					LogPath:        logPath,
				})
			}
		}
	}
	return results, nil
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
