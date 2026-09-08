package view_test

import (
	"bytes"
	"os"
	"testing"

	"agyswitch/internal/model"
	"agyswitch/internal/service/store"
	"agyswitch/internal/service/vault"
	"agyswitch/internal/view"
)

func TestApp_NonInteractivePrintStatus(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "view_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	v := vault.NewVault(tempDir)
	s := store.NewStore(tempDir, v)

	mockLauncher := func(acc string, dir string, args []string) error {
		return nil
	}

	app := view.NewApp(s, mockLauncher)

	var buf bytes.Buffer
	app.PrintStatus(&buf)

	output := buf.String()
	if output == "" {
		t.Errorf("expected non-empty output from PrintStatus")
	}
}

func TestApp_RenderTabs(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "view_render_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	v := vault.NewVault(tempDir)
	s := store.NewStore(tempDir, v)
	app := view.NewApp(s, func(string, string, []string) error { return nil })

	// Test Render across all 4 tabs in compact (60 cols) and wide (100 cols)
	for tab := 0; tab < 4; tab++ {
		app.ActiveTab = tab
		// Render with empty data
		app.Render(nil, nil)
	}

	// Test sessions render with populated data
	app.ActiveTab = 3
	app.SessionScope = 0
	app.Render(nil, []model.SessionInfo{
		{ConversationID: "c1", Title: "Primary CLI Task", ProjectName: "finance-dashboard", StepCount: 100, EstimatedCost: 0.05},
	})
	app.SessionScope = 1
	app.Render(nil, []model.SessionInfo{
		{ConversationID: "c1", Title: "Primary CLI Task", ProjectName: "finance-dashboard", StepCount: 100, EstimatedCost: 0.05},
		{ConversationID: "c2", Title: "[Subagent] Task 1", ProjectName: "finance-dashboard", StepCount: 50, EstimatedCost: 0.02, IsSubagent: true},
	})
}


