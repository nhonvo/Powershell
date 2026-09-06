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
