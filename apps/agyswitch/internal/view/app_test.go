package view_test

import (
	"bytes"
	"fmt"
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

func TestApp_Sessions5ItemsLimitAndSearch(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "view_sess_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	v := vault.NewVault(tempDir)
	s := store.NewStore(tempDir, v)
	app := view.NewApp(s, func(string, string, []string) error { return nil })
	app.ActiveTab = 3

	// Create 8 dummy sessions in finance-dashboard
	var mockSessions []model.SessionInfo
	for i := 1; i <= 8; i++ {
		mockSessions = append(mockSessions, model.SessionInfo{
			ConversationID: fmt.Sprintf("conv-%d", i),
			Title:          fmt.Sprintf("Finance Task #%d", i),
			ProjectName:    "finance-dashboard",
			StepCount:      i * 10,
			EstimatedCost:  float64(i) * 0.01,
		})
	}

	// 1. Collapsed by default -> Should build 5 sessions + 1 expand toggle = 6 items
	items := app.BuildSessionViewItems(mockSessions)
	if len(items) != 6 {
		t.Fatalf("expected 6 items (5 sessions + 1 toggle), got %d", len(items))
	}
	if !items[5].IsExpandToggle || items[5].IsExpanded || items[5].HiddenCount != 3 {
		t.Errorf("expected expand toggle with 3 hidden sessions, got %+v", items[5])
	}

	// Render without crashing
	app.Render(nil, mockSessions)

	// 2. Expand project -> Should build 8 sessions + 1 collapse toggle = 9 items
	app.SetProjectExpanded("finance-dashboard", true)
	itemsExpanded := app.BuildSessionViewItems(mockSessions)
	if len(itemsExpanded) != 9 {
		t.Fatalf("expected 9 items (8 sessions + 1 collapse toggle), got %d", len(itemsExpanded))
	}
	if !itemsExpanded[8].IsExpandToggle || !itemsExpanded[8].IsExpanded {
		t.Errorf("expected collapse toggle, got %+v", itemsExpanded[8])
	}

	// 3. Search query
	app.SessionSearchQuery = "Task #3"
	filtered := app.FilterSessionsList(mockSessions, app.SessionSearchQuery)
	if len(filtered) != 1 || filtered[0].ConversationID != "conv-3" {
		t.Fatalf("expected 1 session matching 'Task #3', got %d", len(filtered))
	}

	// 4. Test rendering with active search and search typing mode
	app.SetSearching(true)
	app.Render(nil, filtered)

	app.SetSearching(false)
	app.Render(nil, filtered)
}


