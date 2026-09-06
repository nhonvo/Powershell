package vault_test

import (
	"os"
	"path/filepath"
	"testing"

	"agyswitch/internal/service/vault"
)

func TestVault_EncryptionDecryption(t *testing.T) {
	v := vault.NewVault("")

	original := "ya29.a0AXooCgVTEST_TOKEN_XYZ_12345"
	encrypted, err := v.Encrypt(original)
	if err != nil {
		t.Fatalf("failed to encrypt: %v", err)
	}

	if encrypted == original {
		t.Fatalf("encrypted token matches original plaintext")
	}

	decrypted, err := v.Decrypt(encrypted)
	if err != nil {
		t.Fatalf("failed to decrypt: %v", err)
	}

	if decrypted != original {
		t.Fatalf("expected decrypted '%s', got '%s'", original, decrypted)
	}
}

func TestVault_ReadTokenFromLocations(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "vault_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	v := vault.NewVault(tempDir)

	tokenPath := filepath.Join(tempDir, "antigravity-cli", "antigravity-oauth-token")
	_ = os.MkdirAll(filepath.Dir(tokenPath), 0755)

	tokenJSON := `{"token":{"access_token":"ya29.test_token_12345"}}`
	if err := os.WriteFile(tokenPath, []byte(tokenJSON), 0600); err != nil {
		t.Fatalf("failed to write token file: %v", err)
	}

	readToken := v.ReadTokenFromDir(tempDir)
	if readToken != "ya29.test_token_12345" {
		t.Fatalf("expected 'ya29.test_token_12345', got '%s'", readToken)
	}
}

func TestVault_GetShortSignature(t *testing.T) {
	v := vault.NewVault("")

	sig := v.GetShortSignature("ya29.a0AXooCg1234567890abcdef")
	if sig != "ya29..1234" {
		t.Fatalf("expected signature 'ya29..1234', got '%s'", sig)
	}
}
