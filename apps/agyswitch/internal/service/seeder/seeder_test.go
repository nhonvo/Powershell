package seeder

import (
	"os"
	"path/filepath"
	"testing"

	"agyswitch/internal/service/store"
)

func TestSeeder(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "agyswitch_seeder_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	s := store.NewStore(tempDir, nil)
	sd := NewSeeder(tempDir, s)

	if err := sd.EnsureSeedTemplate(); err != nil {
		t.Fatalf("EnsureSeedTemplate failed: %v", err)
	}

	tmplRule := filepath.Join(tempDir, ".gemini_template", "config", "rules", "GEMINI.md")
	if _, err := os.Stat(tmplRule); os.IsNotExist(err) {
		t.Errorf("expected GEMINI.md template file to exist")
	}

	if err := sd.SeedAccount("test_acc"); err != nil {
		t.Fatalf("SeedAccount failed: %v", err)
	}

	accRule := filepath.Join(tempDir, ".gemini_test_acc", "config", "rules", "GEMINI.md")
	if _, err := os.Stat(accRule); os.IsNotExist(err) {
		t.Errorf("expected seeded account file to exist")
	}
}

func TestResetAccountEx(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "seeder_reset_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	st := store.NewStore(tempDir, nil)
	sd := NewSeeder(tempDir, st)

	accName := "acc_reset"
	accDir := st.GetAccountDirectory(accName)
	_ = os.MkdirAll(accDir, 0755)

	// 1. Test "auth" / "--auth" reset
	keyringToken := filepath.Join(accDir, "keyring_token.txt")
	_ = os.WriteFile(keyringToken, []byte("token123"), 0644)
	if err := sd.ResetAccountEx(accName, "--auth"); err != nil {
		t.Fatalf("ResetAccountEx --auth failed: %v", err)
	}
	if _, err := os.Stat(keyringToken); !os.IsNotExist(err) {
		t.Errorf("expected keyring_token.txt to be removed after auth reset")
	}

	// 2. Test "soft" / "--soft" reset
	logDir := filepath.Join(accDir, "antigravity-cli", "log")
	_ = os.MkdirAll(logDir, 0755)
	_ = os.WriteFile(filepath.Join(logDir, "test.log"), []byte("log data"), 0644)
	dbFile := filepath.Join(accDir, "agytui.dev.db")
	_ = os.WriteFile(dbFile, []byte("db data"), 0644)

	if err := sd.ResetAccountEx(accName, "soft"); err != nil {
		t.Fatalf("ResetAccountEx soft failed: %v", err)
	}
	if _, err := os.Stat(logDir); !os.IsNotExist(err) {
		t.Errorf("expected logDir to be removed after soft reset")
	}
	if _, err := os.Stat(dbFile); !os.IsNotExist(err) {
		t.Errorf("expected dbFile to be removed after soft reset")
	}

	// 3. Test "hard" / "--hard" reset (deletes account directory)
	_ = st.AddAccount(accName)
	if err := sd.ResetAccountEx(accName, "--hard"); err != nil {
		t.Fatalf("ResetAccountEx --hard failed: %v", err)
	}
	if _, err := os.Stat(accDir); !os.IsNotExist(err) {
		t.Errorf("expected accDir to be deleted after hard reset")
	}
}

