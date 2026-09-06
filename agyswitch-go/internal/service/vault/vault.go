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
	"net/http"
	"net/url"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
	"time"

	"golang.org/x/crypto/pbkdf2"
)

const KeyHash = "7407b4ddbbd1bfbf2dce30edc9115b02dd294ffb233a1e05d28b98df241bc386.key"
const SaltString = "AgySwitch_Secure_Entropy_v2"

const (
	GoogleClientID     = "agy-placeholder-client-id.apps.googleusercontent.com"
	GoogleClientSecret = "AGY_PLACEHOLDER_CLIENT_SECRET_REDACTED"
)

// Vault handles encryption, token discovery across all locations, and keyring sync.
type Vault struct {
	userHome string
}

// NewVault initializes a Vault instance with specified user home directory.
func NewVault(userHome string) *Vault {
	if userHome == "" {
		h, err := os.UserHomeDir()
		if err == nil {
			userHome = h
		}
	}
	return &Vault{userHome: userHome}
}

func (v *Vault) deriveKey() []byte {
	return pbkdf2.Key([]byte(SaltString), []byte("agyswitch_salt_entropy"), 100000, 32, sha256.New)
}

// Encrypt encrypts plaintext data using AES-256-GCM.
func (v *Vault) Encrypt(plaintext string) (string, error) {
	if plaintext == "" {
		return "", errors.New("empty plaintext")
	}

	key := v.deriveKey()
	block, err := aes.NewCipher(key)
	if err != nil {
		return "", err
	}

	gcm, err := cipher.NewGCM(block)
	if err != nil {
		return "", err
	}

	nonce := make([]byte, gcm.NonceSize())
	if _, err := io.ReadFull(rand.Reader, nonce); err != nil {
		return "", err
	}

	ciphertext := gcm.Seal(nonce, nonce, []byte(plaintext), nil)
	return base64.StdEncoding.EncodeToString(ciphertext), nil
}

// Decrypt decrypts AES-256-GCM ciphertext data.
func (v *Vault) Decrypt(encodedCiphertext string) (string, error) {
	if encodedCiphertext == "" {
		return "", errors.New("empty ciphertext")
	}

	data, err := base64.StdEncoding.DecodeString(encodedCiphertext)
	if err != nil {
		return "", err
	}

	key := v.deriveKey()
	block, err := aes.NewCipher(key)
	if err != nil {
		return "", err
	}

	gcm, err := cipher.NewGCM(block)
	if err != nil {
		return "", err
	}

	nonceSize := gcm.NonceSize()
	if len(data) < nonceSize {
		return "", errors.New("ciphertext too short")
	}

	nonce, ciphertext := data[:nonceSize], data[nonceSize:]
	plaintext, err := gcm.Open(nil, nonce, ciphertext, nil)
	if err != nil {
		return "", err
	}

	return string(plaintext), nil
}

// ExtractCleanAccessToken parses raw token string or JSON to extract access token ya29...
func ExtractCleanAccessToken(raw string) string {
	raw = strings.TrimSpace(raw)
	if raw == "" {
		return ""
	}

	type OAuthFile struct {
		AccessToken string `json:"access_token"`
		Token       struct {
			AccessToken string `json:"access_token"`
		} `json:"token"`
	}

	var parsed OAuthFile
	if err := json.Unmarshal([]byte(raw), &parsed); err == nil {
		if parsed.Token.AccessToken != "" {
			return parsed.Token.AccessToken
		}
		if parsed.AccessToken != "" {
			return parsed.AccessToken
		}
	}

	if idx := strings.Index(raw, "ya29."); idx != -1 {
		end := strings.IndexAny(raw[idx:], " \"'\n\r\t}")
		if end != -1 {
			return raw[idx : idx+end]
		}
		return raw[idx:]
	}

	return raw
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
			return ExtractCleanAccessToken(strings.TrimSpace(lines[1]))
		}
	}

	// 2. Check keyring_token.txt
	kTXT := filepath.Join(dir, "keyring_token.txt")
	if data, err := os.ReadFile(kTXT); err == nil {
		raw := strings.TrimSpace(string(data))
		if raw != "" {
			dec, err := v.Decrypt(raw)
			if err == nil && dec != "" {
				return ExtractCleanAccessToken(dec)
			}
			return ExtractCleanAccessToken(raw)
		}
	}

	// 3. Check antigravity-cli/antigravity-oauth-token
	aTok1 := filepath.Join(dir, "antigravity-cli", "antigravity-oauth-token")
	if data, err := os.ReadFile(aTok1); err == nil {
		tok := ExtractCleanAccessToken(string(data))
		if tok != "" {
			return tok
		}
	}

	// 4. Check antigravity-oauth-token
	aTok2 := filepath.Join(dir, "antigravity-oauth-token")
	if data, err := os.ReadFile(aTok2); err == nil {
		tok := ExtractCleanAccessToken(string(data))
		if tok != "" {
			return tok
		}
	}

	return ""
}

// GetShortSignature extracts access token string from raw text or JSON object and returns clean, unique signature.
func (v *Vault) GetShortSignature(token string) string {
	tok := strings.TrimSpace(token)
	if tok == "" || tok == "None" {
		return "None"
	}

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

	if strings.HasPrefix(tok, "ya29.") {
		cleanTok := strings.TrimPrefix(tok, "ya29.")
		if len(cleanTok) >= 12 {
			return fmt.Sprintf("ya29..%s", cleanTok[8:12])
		}
		return tok
	}

	return tok[:12]
}

// ExtractRefreshToken parses token JSON or file to extract refresh token string.
func ExtractRefreshToken(dir string) string {
	aTok1 := filepath.Join(dir, "antigravity-cli", "antigravity-oauth-token")
	data, err := os.ReadFile(aTok1)
	if err != nil {
		aTok2 := filepath.Join(dir, "antigravity-oauth-token")
		data, err = os.ReadFile(aTok2)
	}
	if err != nil {
		return ""
	}

	type OAuthFile struct {
		Token struct {
			RefreshToken string `json:"refresh_token"`
		} `json:"token"`
		RefreshToken string `json:"refresh_token"`
	}

	var parsed OAuthFile
	if err := json.Unmarshal(data, &parsed); err == nil {
		if parsed.Token.RefreshToken != "" {
			return parsed.Token.RefreshToken
		}
		if parsed.RefreshToken != "" {
			return parsed.RefreshToken
		}
	}
	return ""
}

// RefreshOAuthToken performs HTTP refresh using Google OAuth endpoint.
func RefreshOAuthToken(refreshToken string) (string, error) {
	if strings.TrimSpace(refreshToken) == "" {
		return "", errors.New("empty refresh token")
	}

	data := url.Values{}
	data.Set("client_id", GoogleClientID)
	data.Set("client_secret", GoogleClientSecret)
	data.Set("grant_type", "refresh_token")
	data.Set("refresh_token", refreshToken)

	client := &http.Client{Timeout: 5 * time.Second}
	resp, err := client.PostForm("https://oauth2.googleapis.com/token", data)
	if err != nil {
		return "", err
	}
	defer resp.Body.Close()

	if resp.StatusCode != 200 {
		return "", fmt.Errorf("oauth endpoint returned status %d", resp.StatusCode)
	}

	var res struct {
		AccessToken string `json:"access_token"`
		ExpiresIn   int    `json:"expires_in"`
	}
	if err := json.NewDecoder(resp.Body).Decode(&res); err != nil {
		return "", err
	}

	if res.AccessToken == "" {
		return "", errors.New("empty access token in refresh response")
	}

	return res.AccessToken, nil
}

// EnsureValidAccessToken reads access token and refreshes it via OAuth endpoint if refresh token exists.
func (v *Vault) EnsureValidAccessToken(dir string) string {
	tok := v.ReadTokenFromDir(dir)
	rf := ExtractRefreshToken(dir)

	if rf != "" {
		if newTok, err := RefreshOAuthToken(rf); err == nil && newTok != "" {
			aTokPath := filepath.Join(dir, "antigravity-cli", "antigravity-oauth-token")
			if data, err := os.ReadFile(aTokPath); err == nil {
				var parsed map[string]interface{}
				if json.Unmarshal(data, &parsed) == nil {
					if tokMap, ok := parsed["token"].(map[string]interface{}); ok {
						tokMap["access_token"] = newTok
						if updated, err := json.MarshalIndent(parsed, "", "  "); err == nil {
							_ = os.WriteFile(aTokPath, updated, 0600)
						}
					}
				}
			}
			return newTok
		}
	}

	return tok
}

// SyncKeyringCredentials copies credentials from dir context into Windows Credential Manager if cmdkey.exe is available.
func (v *Vault) SyncKeyringCredentials(dir string) error {
	tok := v.ReadTokenFromDir(dir)
	if tok == "" {
		return errors.New("no token available in directory context")
	}

	cmdkeyPath, err := exec.LookPath("cmdkey.exe")
	if err != nil {
		return nil
	}

	user := "antigravity"
	cmd := exec.Command(cmdkeyPath, "/generic:gemini:antigravity", "/user:"+user, "/pass:"+tok)
	return cmd.Run()
}
