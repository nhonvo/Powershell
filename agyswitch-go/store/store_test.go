package store_test

import (
	"os"
	"path/filepath"
	"testing"

	"agyswitch/store"
	"agyswitch/vault"
)

func TestStore_AccountDirectoryAndActiveMarker(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "store_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	v := vault.NewVault(tempDir)
	s := store.NewStore(tempDir, v)

	// Test GetAccountDirectory
	fptDir := s.GetAccountDirectory("fptvttnhon2020")
	expectedFpt := filepath.Join(tempDir, ".gemini_fptvttnhon2020")
	if fptDir != expectedFpt {
		t.Errorf("expected %s, got %s", expectedFpt, fptDir)
	}

	// Test SetActiveAccount
	err = s.SetActiveAccount("fptvttnhon2020")
	if err != nil {
		t.Fatalf("SetActiveAccount failed: %v", err)
	}

	active := s.GetActiveAccount()
	if active != "fptvttnhon2020" {
		t.Errorf("expected active fptvttnhon2020, got %s", active)
	}

	// Verify active_account.txt
	activeTxt := filepath.Join(tempDir, ".gemini", "active_account.txt")
	content, err := os.ReadFile(activeTxt)
	if err != nil || string(content) != "fptvttnhon2020" {
		t.Errorf("active_account.txt expected fptvttnhon2020, got %s", string(content))
	}
}

func TestStore_SanitizeAccountDirectory(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "store_sanitize_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	v := vault.NewVault(tempDir)
	s := store.NewStore(tempDir, v)

	s.SanitizeAccountDirectory("nhontruongvo")

	accDir := s.GetAccountDirectory("nhontruongvo")
	gPath := filepath.Join(accDir, "google_accounts.json")
	if _, err := os.Stat(gPath); os.IsNotExist(err) {
		t.Errorf("google_accounts.json not created: %s", gPath)
	}

	sPath := filepath.Join(accDir, "antigravity-cli", "settings.json")
	if _, err := os.Stat(sPath); os.IsNotExist(err) {
		t.Errorf("settings.json not created: %s", sPath)
	}
}

func TestStore_MirrorDirectory(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "store_mirror_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	v := vault.NewVault(tempDir)
	s := store.NewStore(tempDir, v)

	srcDir := filepath.Join(tempDir, "src")
	dstDir := filepath.Join(tempDir, "dst")

	_ = os.MkdirAll(srcDir, 0755)
	_ = os.WriteFile(filepath.Join(srcDir, "sample.txt"), []byte("hello mirror"), 0644)

	err = s.MirrorDirectory(srcDir, dstDir)
	if err != nil {
		t.Fatalf("MirrorDirectory failed: %v", err)
	}

	dstFile := filepath.Join(dstDir, "sample.txt")
	content, err := os.ReadFile(dstFile)
	if err != nil || string(content) != "hello mirror" {
		t.Errorf("expected hello mirror, got %s", string(content))
	}
}

func TestStore_ListAccounts(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "store_list_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	v := vault.NewVault(tempDir)
	s := store.NewStore(tempDir, v)

	_ = s.SetActiveAccount("nhontruongvo3")

	accs := s.ListAccounts()
	if len(accs) < 5 {
		t.Errorf("expected at least 5 accounts, got %d", len(accs))
	}

	foundActive := false
	for _, a := range accs {
		if a.AccountName == "nhontruongvo3" && a.IsActive {
			foundActive = true
			break
		}
	}
	if !foundActive {
		t.Errorf("nhontruongvo3 should be marked active in list")
	}
}
