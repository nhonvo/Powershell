package vault

import (
	"crypto/aes"
	"crypto/cipher"
	"crypto/rand"
	"crypto/sha256"
	"encoding/base64"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"os"
	"os/exec"
	"os/user"
	"path/filepath"
	"strings"

	"golang.org/x/crypto/pbkdf2"
)

const KeyHash = "7407b4ddbbd1bfbf2dce30edc9115b02dd294ffb233a1e05d28b98df241bc386.key"
const SaltString = "AgySwitch_Secure_Entropy_v2"

// Vault handles encryption, token discovery across all locations, and keyring sync.
type Vault struct {
	userHome string
}

func NewVault(userHome string) *Vault {
	if userHome == "" {
		h, err := os.UserHomeDir()
		if err == nil {
			userHome = h
		}
	}
	return &Vault{userHome: userHome}
}

// DeriveEncryptionKey generates a 32-byte AES key from system identity & machine salt.
func (v *Vault) DeriveEncryptionKey() []byte {
	u, err := user.Current()
	username := "user"
	if err == nil && u.Username != "" {
		username = u.Username
	}
	hostname, err := os.Hostname()
	if err != nil || hostname == "" {
		hostname = "localhost"
	}
	secret := fmt.Sprintf("%s@%s:%s", username, hostname, v.userHome)
	return pbkdf2.Key([]byte(secret), []byte(SaltString), 10000, 32, sha256.New)
}

// Encrypt protects a plaintext token string using AES-256-GCM.
func (v *Vault) Encrypt(plainText string) (string, error) {
	if strings.TrimSpace(plainText) == "" {
		return "", errors.New("cannot encrypt empty string")
	}

	key := v.DeriveEncryptionKey()
	block, err := aes.NewCipher(key)
	if err != nil {
		return "", err
	}

	aesGCM, err := cipher.NewGCM(block)
	if err != nil {
		return "", err
	}

	nonce := make([]byte, aesGCM.NonceSize())
	if _, err := io.ReadFull(rand.Reader, nonce); err != nil {
		return "", err
	}

	ciphertext := aesGCM.Seal(nonce, nonce, []byte(plainText), nil)
	return base64.StdEncoding.EncodeToString(ciphertext), nil
}

// Decrypt restores a token string from cipher text or returns plain text if unencrypted.
func (v *Vault) Decrypt(cipherText string) (string, error) {
	trimmed := strings.TrimSpace(cipherText)
	if trimmed == "" {
		return "", errors.New("empty cipher text")
	}

	// Plaintext tokens (ya29..., AIza..., JSON)
	if strings.HasPrefix(trimmed, "ya29.") || strings.HasPrefix(trimmed, "AIza") || strings.HasPrefix(trimmed, "{") {
		return trimmed, nil
	}

	data, err := base64.StdEncoding.DecodeString(trimmed)
	if err != nil {
		return trimmed, nil
	}

	key := v.DeriveEncryptionKey()
	block, err := aes.NewCipher(key)
	if err != nil {
		return trimmed, nil
	}

	aesGCM, err := cipher.NewGCM(block)
	if err != nil {
		return trimmed, nil
	}

	nonceSize := aesGCM.NonceSize()
	if len(data) < nonceSize {
		return trimmed, nil
	}

	nonce, ciphertext := data[:nonceSize], data[nonceSize:]
	plaintext, err := aesGCM.Open(nil, nonce, ciphertext, nil)
	if err != nil {
		return trimmed, nil
	}

	return string(plaintext), nil
}

// ReadTokenFromDir checks standalone CLI token locations inside directory context.
func (v *Vault) ReadTokenFromDir(dir string) string {
	if dir == "" {
		return ""
	}

	// 1. Check .keyring/7407b4dd...key
	kFile := filepath.Join(dir, ".keyring", KeyHash)
	if data, err := os.ReadFile(kFile); err == nil {
		lines := strings.Split(string(data), "\n")
		if len(lines) >= 2 && strings.TrimSpace(lines[1]) != "" {
			return strings.TrimSpace(lines[1])
		}
	}

	// 2. Check keyring_token.txt
	kTXT := filepath.Join(dir, "keyring_token.txt")
	if data, err := os.ReadFile(kTXT); err == nil {
		raw := strings.TrimSpace(string(data))
		if raw != "" {
			dec, err := v.Decrypt(raw)
			if err == nil && dec != "" {
				return dec
			}
			return raw
		}
	}

	// 3. Check antigravity-cli/antigravity-oauth-token
	aTok1 := filepath.Join(dir, "antigravity-cli", "antigravity-oauth-token")
	if data, err := os.ReadFile(aTok1); err == nil {
		if tok := strings.TrimSpace(string(data)); tok != "" {
			return tok
		}
	}

	// 4. Check antigravity-oauth-token
	aTok2 := filepath.Join(dir, "antigravity-oauth-token")
	if data, err := os.ReadFile(aTok2); err == nil {
		if tok := strings.TrimSpace(string(data)); tok != "" {
			return tok
		}
	}

	return ""
}

// SaveTokenToContext writes token to all token locations in context dir, global keyring, and cmdkey.exe.
func (v *Vault) SaveTokenToContext(contextDir, token string) error {
	tok := strings.TrimSpace(token)
	if tok == "" {
		return errors.New("cannot save empty token")
	}

	if err := os.MkdirAll(filepath.Join(contextDir, ".keyring"), 0755); err != nil {
		return err
	}
	if err := os.MkdirAll(filepath.Join(contextDir, "antigravity-cli"), 0755); err != nil {
		return err
	}

	keyContent := fmt.Sprintf("gemini:antigravity\n%s\n", tok)
	if err := os.WriteFile(filepath.Join(contextDir, ".keyring", KeyHash), []byte(keyContent), 0600); err != nil {
		return err
	}

	encTok, err := v.Encrypt(tok)
	if err != nil {
		encTok = tok
	}
	_ = os.WriteFile(filepath.Join(contextDir, "keyring_token.txt"), []byte(encTok), 0600)
	_ = os.WriteFile(filepath.Join(contextDir, "antigravity-cli", "antigravity-oauth-token"), []byte(tok), 0600)
	_ = os.WriteFile(filepath.Join(contextDir, "antigravity-oauth-token"), []byte(tok), 0600)

	// Mirror to global ~/.gemini/.keyring/
	globalKeyringDir := filepath.Join(v.userHome, ".gemini", ".keyring")
	_ = os.MkdirAll(globalKeyringDir, 0755)
	_ = os.WriteFile(filepath.Join(globalKeyringDir, KeyHash), []byte(keyContent), 0600)

	// Re-inject token into OS Keyring via cmdkey.exe if available (Windows/WSL interop)
	if cmdkeyPath, err := exec.LookPath("cmdkey.exe"); err == nil {
		_ = exec.Command(cmdkeyPath, "/generic:gemini:antigravity", "/user:gemini", fmt.Sprintf("/pass:%s", tok)).Run()
	}

	return nil
}

// PurgeGlobalKeyring clears global keyring files and Windows cmdkey credentials when logged out.
func (v *Vault) PurgeGlobalKeyring() {
	globalKeyringFile := filepath.Join(v.userHome, ".gemini", ".keyring", KeyHash)
	_ = os.Remove(globalKeyringFile)
	_ = os.Remove(filepath.Join(v.userHome, ".gemini", "keyring_token.txt"))
	_ = os.Remove(filepath.Join(v.userHome, ".gemini", "antigravity-cli", "antigravity-oauth-token"))
	_ = os.Remove(filepath.Join(v.userHome, ".gemini", "antigravity-oauth-token"))

	if cmdkeyPath, err := exec.LookPath("cmdkey.exe"); err == nil {
		_ = exec.Command(cmdkeyPath, "/delete:gemini:antigravity").Run()
		_ = exec.Command(cmdkeyPath, "/delete:LegacyGeneric:target=gemini:antigravity").Run()
	}
}

// GetShortSignature extracts access token string from raw text or JSON object and returns clean, unique signature.
func (v *Vault) GetShortSignature(token string) string {
	tok := strings.TrimSpace(token)
	if tok == "" || tok == "None" {
		return "None"
	}

	// Parse JSON tokens like {"token":{"access_token":"ya29..."}, ...} or {"access_token":"ya29...", ...}
	if strings.HasPrefix(tok, "{") && strings.HasSuffix(tok, "}") {
		var jsonObj map[string]interface{}
		if err := json.Unmarshal([]byte(tok), &jsonObj); err == nil {
			if at, ok := jsonObj["access_token"].(string); ok && at != "" {
				tok = at
			} else if tkStr, ok := jsonObj["token"].(string); ok && tkStr != "" {
				tok = tkStr
			} else if tkMap, ok := jsonObj["token"].(map[string]interface{}); ok {
				if at, ok := tkMap["access_token"].(string); ok && at != "" {
					tok = at
				}
			}
		}
	}

	if len(tok) <= 12 {
		return tok
	}

	// For ya29 tokens, Google appends a fixed 4-digit suffix (e.g. 0211).
	// Extract the unique middle segment (e.g. ya29..Eg4X vs ya29..Eiv3).
	if strings.HasPrefix(tok, "ya29.") {
		cleanTok := strings.TrimPrefix(tok, "ya29.")
		if len(cleanTok) >= 12 {
			return fmt.Sprintf("ya29..%s", cleanTok[8:12])
		}
	}

	head := tok[:4]
	tail := tok[len(tok)-4:]
	return fmt.Sprintf("%s..%s", head, tail)
}
