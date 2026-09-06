package sessions_test

import (
	"os"
	"path/filepath"
	"testing"

	"agyswitch/internal/service/sessions"
)

func TestSessions_DiscoverAndParse(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "sessions_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	m := sessions.NewManager(tempDir)

	// Create dummy conversation transcript
	convoID := "test-convo-12345"
	logPath := filepath.Join(tempDir, ".gemini", "antigravity-cli", "brain", convoID, ".system_generated", "logs", "transcript.jsonl")
	_ = os.MkdirAll(filepath.Dir(logPath), 0755)

	logData := "{\"step_index\":1,\"type\":\"USER_INPUT\",\"status\":\"DONE\"}\n{\"step_index\":2,\"type\":\"PLANNER_RESPONSE\",\"status\":\"DONE\"}\n"
	_ = os.WriteFile(logPath, []byte(logData), 0644)

	sessList, err := m.DiscoverSessions()
	if err != nil {
		t.Fatalf("failed to discover sessions: %v", err)
	}

	if len(sessList) != 1 {
		t.Fatalf("expected 1 session, got %d", len(sessList))
	}

	if sessList[0].ConversationID != convoID {
		t.Errorf("expected conversationID '%s', got '%s'", convoID, sessList[0].ConversationID)
	}

	steps, err := sessions.ParseTranscriptSteps(logPath)
	if err != nil {
		t.Fatalf("failed to parse transcript steps: %v", err)
	}

	if len(steps) != 2 {
		t.Fatalf("expected 2 steps, got %d", len(steps))
	}
}

func TestCalculateSessionCost(t *testing.T) {
	tests := []struct {
		steps    int
		expected float64
	}{
		{steps: 0, expected: 0.0},
		{steps: -5, expected: 0.0},
		{steps: 2000, expected: float64(2000*400) / 1000000.0 * 1.25}, // 1.0
		{steps: 100, expected: float64(100*400) / 1000000.0 * 1.25},   // 0.05
	}

	for _, tt := range tests {
		got := sessions.CalculateSessionCost(tt.steps)
		if got != tt.expected {
			t.Errorf("CalculateSessionCost(%d) = %f; want %f", tt.steps, got, tt.expected)
		}
	}
}

