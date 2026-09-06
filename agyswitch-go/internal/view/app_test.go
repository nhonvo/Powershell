package view_test

import (
	"bytes"
	"os"
	"testing"

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

	mockLauncher := func(acc string, args []string) error {
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
