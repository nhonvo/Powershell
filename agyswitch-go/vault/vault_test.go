package vault_test

import (
	"os"
	"path/filepath"
	"testing"

	"agyswitch/vault"
)

func TestVault_EncryptionDecryption(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "vault_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	v := vault.NewVault(tempDir)
	rawToken := "ya29.a0AXooCvg123456789SecretTokenContent"

	encrypted, err := v.Encrypt(rawToken)
	if err != nil {
		t.Fatalf("Encrypt failed: %v", err)
	}
	if encrypted == "" || encrypted == rawToken {
		t.Errorf("expected encrypted string, got: %s", encrypted)
	}

	decrypted, err := v.Decrypt(encrypted)
	if err != nil {
		t.Fatalf("Decrypt failed: %v", err)
	}
	if decrypted != rawToken {
		t.Errorf("expected %s, got %s", rawToken, decrypted)
	}
}

func TestVault_ReadTokenFromFourLocations(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "vault_read_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	v := vault.NewVault(tempDir)
	tokenVal := "ya29.sample_token_123"

	// 1. Test location 1: .keyring/7407b4dd...key
	loc1Dir := filepath.Join(tempDir, "loc1")
	_ = os.MkdirAll(filepath.Join(loc1Dir, ".keyring"), 0755)
	keyFile := filepath.Join(loc1Dir, ".keyring", vault.KeyHash)
	_ = os.WriteFile(keyFile, []byte("gemini:antigravity\n"+tokenVal+"\n"), 0600)

	got1 := v.ReadTokenFromDir(loc1Dir)
	if got1 != tokenVal {
		t.Errorf("loc1 expected %s, got %s", tokenVal, got1)
	}

	// 2. Test location 2: keyring_token.txt
	loc2Dir := filepath.Join(tempDir, "loc2")
	_ = os.MkdirAll(loc2Dir, 0755)
	encTok, _ := v.Encrypt(tokenVal)
	_ = os.WriteFile(filepath.Join(loc2Dir, "keyring_token.txt"), []byte(encTok), 0600)

	got2 := v.ReadTokenFromDir(loc2Dir)
	if got2 != tokenVal {
		t.Errorf("loc2 expected %s, got %s", tokenVal, got2)
	}

	// 3. Test location 3: antigravity-cli/antigravity-oauth-token
	loc3Dir := filepath.Join(tempDir, "loc3")
	_ = os.MkdirAll(filepath.Join(loc3Dir, "antigravity-cli"), 0755)
	_ = os.WriteFile(filepath.Join(loc3Dir, "antigravity-cli", "antigravity-oauth-token"), []byte(tokenVal), 0600)

	got3 := v.ReadTokenFromDir(loc3Dir)
	if got3 != tokenVal {
		t.Errorf("loc3 expected %s, got %s", tokenVal, got3)
	}

	// 4. Test location 4: antigravity-oauth-token
	loc4Dir := filepath.Join(tempDir, "loc4")
	_ = os.MkdirAll(loc4Dir, 0755)
	_ = os.WriteFile(filepath.Join(loc4Dir, "antigravity-oauth-token"), []byte(tokenVal), 0600)

	got4 := v.ReadTokenFromDir(loc4Dir)
	if got4 != tokenVal {
		t.Errorf("loc4 expected %s, got %s", tokenVal, got4)
	}
}

func TestVault_SaveTokenToContextAndPurge(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "vault_save_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	v := vault.NewVault(tempDir)
	contextDir := filepath.Join(tempDir, ".gemini_fptvttnhon2020")
	tokenVal := "ya29.saved_token_456"

	err = v.SaveTokenToContext(contextDir, tokenVal)
	if err != nil {
		t.Fatalf("SaveTokenToContext failed: %v", err)
	}

	// Verify token saved in context
	readBack := v.ReadTokenFromDir(contextDir)
	if readBack != tokenVal {
		t.Errorf("expected %s, got %s", tokenVal, readBack)
	}

	// Verify mirrored to global ~/.gemini/.keyring/
	globalKey := filepath.Join(tempDir, ".gemini", ".keyring", vault.KeyHash)
	if _, err := os.Stat(globalKey); os.IsNotExist(err) {
		t.Errorf("global key file was not created: %s", globalKey)
	}

	// Test PurgeGlobalKeyring
	v.PurgeGlobalKeyring()
	if _, err := os.Stat(globalKey); !os.IsNotExist(err) {
		t.Errorf("global key file should have been deleted after purge: %s", globalKey)
	}
}

func TestVault_GetShortSignature(t *testing.T) {
	v := vault.NewVault("/tmp")

	if sig := v.GetShortSignature(""); sig != "None" {
		t.Errorf("expected None for empty, got %s", sig)
	}
	if sig := v.GetShortSignature("ya29.a0AdMD6Eg4XpR7l4OvYeTAYw4wTf"); sig != "ya29..g4Xp" {
		t.Errorf("expected ya29..g4Xp, got %s", sig)
	}
}
