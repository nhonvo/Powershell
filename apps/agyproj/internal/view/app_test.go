package view_test

import (
	"bytes"
	"os"
	"testing"

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
