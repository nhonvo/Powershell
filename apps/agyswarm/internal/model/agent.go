package model

import (
	"sync"
	"time"
)

// AgentStatus represents the lifecycle state of a child agent terminal.
type AgentStatus string

const (
	StatusWorking   AgentStatus = "WORKING"    // Agent is actively running / executing
	StatusNeedInput AgentStatus = "NEED_INPUT" // Agent is blocked waiting for human prompt approval ([y/N], etc.)
	StatusDone      AgentStatus = "DONE"       // Process completed with exit code 0
	StatusError     AgentStatus = "ERROR"      // Process failed or encountered 429 quota exhaustion
	StatusPaused    AgentStatus = "PAUSED"     // Process suspended by operator
)

// AgentSession represents a single child terminal agent running inside a pseudo-terminal.
type AgentSession struct {
	ID           string      `json:"id"`
	Name         string      `json:"name"`
	AccountName  string      `json:"accountName"`
	WorkspaceDir string      `json:"workspaceDir"`
	Command      string      `json:"command"`
	Args         []string    `json:"args"`
	Status       AgentStatus `json:"status"`
	PID          int         `json:"pid"`
	CreatedAt    time.Time   `json:"createdAt"`
	ExitCode     int         `json:"exitCode"`
	PromptHint   string      `json:"promptHint,omitempty"`

	// Ring buffer of recent output lines
	LinesMu sync.RWMutex `json:"-"`
	Lines   []string     `json:"lines"`
}

// GetRecentLines returns up to maxLines from the agent's output buffer.
func (s *AgentSession) GetRecentLines(maxLines int) []string {
	s.LinesMu.RLock()
	defer s.LinesMu.RUnlock()

	if len(s.Lines) <= maxLines {
		out := make([]string, len(s.Lines))
		copy(out, s.Lines)
		return out
	}
	start := len(s.Lines) - maxLines
	out := make([]string, maxLines)
	copy(out, s.Lines[start:])
	return out
}

// AppendLine appends a line to the agent's output buffer, maintaining a maximum capacity.
func (s *AgentSession) AppendLine(line string, maxCapacity int) {
	s.LinesMu.Lock()
	defer s.LinesMu.Unlock()

	s.Lines = append(s.Lines, line)
	if len(s.Lines) > maxCapacity {
		s.Lines = s.Lines[len(s.Lines)-maxCapacity:]
	}
}

// SwarmTask represents an overarching multi-agent goal dispatched across multiple accounts.
type SwarmTask struct {
	ID              string      `json:"id"`
	Goal            string      `json:"goal"`
	TargetProject   string      `json:"targetProject"`
	DeliverableFile string      `json:"deliverableFile"`
	WorkerAgentIDs  []string    `json:"workerAgentIds"`
	Status          AgentStatus `json:"status"`
	CreatedAt       time.Time   `json:"createdAt"`
	CompletedAt     time.Time   `json:"completedAt,omitempty"`
}

// BlackboardArtifact represents a piece of knowledge, research fact, or code diff shared between agents.
type BlackboardArtifact struct {
	ID        string    `json:"id"`
	AgentID   string    `json:"agentId"`
	Topic     string    `json:"topic"`
	Markdown  string    `json:"markdown"`
	Timestamp time.Time `json:"timestamp"`
}
