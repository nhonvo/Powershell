package engine

import (
	"testing"

	"agyswarm/internal/model"
)

func TestDetectStatus(t *testing.T) {
	tests := []struct {
		name           string
		lastLines      []string
		alive          bool
		exitCode       int
		expectedStatus model.AgentStatus
	}{
		{
			name:           "Process finished successfully",
			lastLines:      []string{"all tasks done"},
			alive:          false,
			exitCode:       0,
			expectedStatus: model.StatusDone,
		},
		{
			name:           "Process crashed",
			lastLines:      []string{"exit"},
			alive:          false,
			exitCode:       1,
			expectedStatus: model.StatusError,
		},
		{
			name:           "Agent waiting for approval",
			lastLines:      []string{"Applying git patch to main.go", "Approve execution? [y/N]"},
			alive:          true,
			exitCode:       0,
			expectedStatus: model.StatusNeedInput,
		},
		{
			name:           "Agent hit 429 quota error",
			lastLines:      []string{"Calling Gemini API...", "Error: 429 Rate Limit exceeded"},
			alive:          true,
			exitCode:       0,
			expectedStatus: model.StatusError,
		},
		{
			name:           "Agent actively working",
			lastLines:      []string{"[Step 12] Compiling Go modules", "Downloading dependencies..."},
			alive:          true,
			exitCode:       0,
			expectedStatus: model.StatusWorking,
		},
	}

	for _, tt := range tests {
		status, _ := DetectStatus(tt.lastLines, tt.alive, tt.exitCode)
		if status != tt.expectedStatus {
			t.Errorf("[%s] expected %s, got %s", tt.name, tt.expectedStatus, status)
		}
	}
}
