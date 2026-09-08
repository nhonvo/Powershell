package view_test

import (
	"bytes"
	"os"
	"testing"

	"agyproj/internal/model"
	"agyproj/internal/service/launcher"
	"agyproj/internal/service/registry"
	"agyproj/internal/view"
)

func TestApp_Render(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "agyproj_view_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	reg := registry.NewManager(tempDir)
	lnch := launcher.NewLauncher()
	app := view.NewApp(reg, lnch)

	for tab := 0; tab < 3; tab++ {
		app.ActiveTab = tab
		app.Render(nil, nil)
	}

	var buf bytes.Buffer
	app.PrintList(&buf)
	if buf.Len() == 0 {
		t.Errorf("expected non-empty output from PrintList")
	}
}

func TestApp_SearchFilter(t *testing.T) {
	app := view.NewApp(nil, nil)
	app.SetRegisteredCache([]model.ProjectInfo{
		{
			ID:        "finance-app",
			Name:      "finance-app",
			Path:      "/home/user/projects/finance-app",
			Stack:     "C# / .NET",
			GitBranch: "main",
		},
		{
			ID:        "powershell-profile",
			Name:      "powershell-profile",
			Path:      "/home/user/projects/powershell-profile",
			Stack:     "Go / Bash",
			GitBranch: "feature/search",
		},
		{
			ID:        "react-frontend",
			Name:      "react-frontend",
			Path:      "/home/user/projects/react-frontend",
			Stack:     "TypeScript / React",
			GitBranch: "dev",
		},
	})

	// 1. Filter by Name
	app.SetSearchQuery("finance")
	matches := app.GetFilteredWorkspaces()
	if len(matches) != 1 || matches[0].Name != "finance-app" {
		t.Errorf("expected 1 match for 'finance', got %d", len(matches))
	}

	// 2. Filter by Stack
	app.SetSearchQuery("Go")
	matches = app.GetFilteredWorkspaces()
	if len(matches) != 1 || matches[0].Name != "powershell-profile" {
		t.Errorf("expected 1 match for 'Go', got %d", len(matches))
	}

	// 3. Filter by GitBranch
	app.SetSearchQuery("search")
	matches = app.GetFilteredWorkspaces()
	if len(matches) != 1 || matches[0].Name != "powershell-profile" {
		t.Errorf("expected 1 match for branch 'search', got %d", len(matches))
	}

	// 4. Empty query returns all
	app.SetSearchQuery("")
	matches = app.GetFilteredWorkspaces()
	if len(matches) != 3 {
		t.Errorf("expected 3 matches for empty query, got %d", len(matches))
	}
}

