package tui_test

import (
	"os"
	"testing"

	"agyswitch/launcher"
	"agyswitch/store"
	"agyswitch/tui"
	"agyswitch/vault"
)

func TestTuiRun_NonInteractive_ExecutesWithoutError(t *testing.T) {
	tmpDir, err := os.MkdirTemp("", "agyswitch_tui_test_*")
	if err != nil {
		t.Fatalf("Failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tmpDir)

	v := vault.NewVault(tmpDir)
	s := store.NewStore(tmpDir, v)
	l := launcher.NewLauncher(s, v)

	err = tui.Run(s, v, l, tui.Options{ForceNonInteractive: true})
	if err != nil {
		t.Fatalf("Expected nil error in non-interactive mode, got %v", err)
	}
}

func TestTuiPrintStatus_ExecutesWithoutPanic(t *testing.T) {
	tmpDir, err := os.MkdirTemp("", "agyswitch_tui_status_test_*")
	if err != nil {
		t.Fatalf("Failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tmpDir)

	v := vault.NewVault(tmpDir)
	s := store.NewStore(tmpDir, v)

	// Should execute cleanly without error
	tui.PrintStatus(s)
}
