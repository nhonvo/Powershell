package launcher_test

import (
	"os"
	"path/filepath"
	"testing"

	"agyswitch/internal/service/store"
	"agyswitch/internal/service/vault"
	"agyswitch/launcher"
)

func TestLauncher_CleanArgs(t *testing.T) {
	v := vault.NewVault("/tmp")
	s := store.NewStore("/tmp", v)
	l := launcher.NewLauncher(s, v)

	raw := []string{"auth", "--dangerously-skip-permissions", "-p", "hello"}
	cleaned := l.CleanArgs(raw)

	if len(cleaned) != 3 {
		t.Fatalf("expected 3 args, got %d", len(cleaned))
	}
	if cleaned[0] != "--dangerously-skip-permissions" || cleaned[1] != "-p" || cleaned[2] != "hello" {
		t.Errorf("unexpected cleaned args: %v", cleaned)
	}

	loginRaw := []string{"login"}
	loginCleaned := l.CleanArgs(loginRaw)
	if len(loginCleaned) != 0 {
		t.Errorf("expected login to be stripped, got: %v", loginCleaned)
	}
}

func TestLauncher_FindAgyBinCandidates(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "launcher_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	v := vault.NewVault(tempDir)
	s := store.NewStore(tempDir, v)
	l := launcher.NewLauncher(s, v)
	t.Setenv("PATH", "")

	// Create fake agy in ~/.local/bin/agy
	fakeAgyDir := filepath.Join(tempDir, ".local", "bin")
	_ = os.MkdirAll(fakeAgyDir, 0755)
	fakeAgyFile := filepath.Join(fakeAgyDir, "agy")
	_ = os.WriteFile(fakeAgyFile, []byte("#!/bin/sh\necho fake"), 0755)

	bin, err := l.FindAgyBin()
	if err != nil {
		t.Fatalf("FindAgyBin failed: %v", err)
	}
	if bin != fakeAgyFile {
		t.Errorf("expected %s, got %s", fakeAgyFile, bin)
	}
}
