package vault

import (
	"bytes"
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

var (
	GoogleClientID     = getOAuthConfig("AGY_GOOGLE_CLIENT_ID", []byte{
		107, 106, 109, 107, 106, 106, 108, 106, 108, 106, 111, 99, 107, 119, 46, 55,
		50, 41, 41, 51, 52, 104, 50, 104, 107, 54, 57, 40, 63, 104, 105, 111,
		44, 46, 53, 54, 53, 48, 50, 110, 61, 110, 106, 105, 63, 42, 116, 59,
		42, 42, 41, 116, 61, 53, 53, 61, 54, 63, 47, 41, 63, 40, 57, 53,
		52, 46, 63, 52, 46, 116, 57, 53, 55,
	})
	GoogleClientSecret = getOAuthConfig("AGY_GOOGLE_CLIENT_SECRET", []byte{
		29, 21, 25, 9, 10, 2, 119, 17, 111, 98, 28, 13, 8, 110, 98, 108,
		22, 62, 22, 16, 107, 55, 22, 24, 98, 41, 2, 25, 110, 32, 108, 43,
		30, 27, 60,
	})
)

func getOAuthConfig(envKey string, enc []byte) string {
	if val := os.Getenv(envKey); val != "" {
		return val
	}
	res := make([]byte, len(enc))
	for i, b := range enc {
		res[i] = b ^ 0x5A
	}
	return string(res)
}

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
	raw = strings.TrimPrefix(raw, "\ufeff")
	raw = strings.TrimPrefix(raw, "\xef\xbb\xbf")
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
		if parsed.Token.AccessToken != "" && strings.HasPrefix(parsed.Token.AccessToken, "ya29.") {
			return parsed.Token.AccessToken
		}
		if parsed.AccessToken != "" && strings.HasPrefix(parsed.AccessToken, "ya29.") {
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

	return ""
}

// ReadTokenFromDir checks standalone CLI token locations inside directory context.
func (v *Vault) ReadTokenFromDir(dir string) string {
	if dir == "" {
		return ""
	}

	// 1. Check antigravity-cli/antigravity-oauth-token (Official Antigravity token file)
	aTok1 := filepath.Join(dir, "antigravity-cli", "antigravity-oauth-token")
	if data, err := os.ReadFile(aTok1); err == nil {
		tok := ExtractCleanAccessToken(string(data))
		if tok != "" {
			return tok
		}
	}

	// 2. Check antigravity-oauth-token
	aTok2 := filepath.Join(dir, "antigravity-oauth-token")
	if data, err := os.ReadFile(aTok2); err == nil {
		tok := ExtractCleanAccessToken(string(data))
		if tok != "" {
			return tok
		}
	}

	// 3. Check keyring_token.txt
	kTXT := filepath.Join(dir, "keyring_token.txt")
	if data, err := os.ReadFile(kTXT); err == nil {
		raw := strings.TrimSpace(string(data))
		if raw != "" {
			dec, err := v.Decrypt(raw)
			if err == nil && dec != "" {
				if tok := ExtractCleanAccessToken(dec); tok != "" {
					return tok
				}
			}
			if tok := ExtractCleanAccessToken(raw); tok != "" {
				return tok
			}
		}
	}

	// 4. Check .keyring/7407b4dd...key
	kFile := filepath.Join(dir, ".keyring", KeyHash)
	if data, err := os.ReadFile(kFile); err == nil {
		lines := strings.Split(string(data), "\n")
		if len(lines) >= 2 && strings.TrimSpace(lines[1]) != "" {
			if tok := ExtractCleanAccessToken(strings.TrimSpace(lines[1])); tok != "" {
				return tok
			}
		}
	}

	return ""
}

// GetShortSignature extracts access token string from raw text or JSON object and returns clean, unique signature.
func (v *Vault) GetShortSignature(token string) string {
	cleanTok := ExtractCleanAccessToken(token)
	if cleanTok == "" {
		return "None"
	}

	if strings.HasPrefix(cleanTok, "ya29.") {
		s := strings.TrimPrefix(cleanTok, "ya29.")
		if len(s) >= 12 {
			return fmt.Sprintf("ya29..%s", s[8:12])
		} else if len(s) >= 4 {
			return fmt.Sprintf("ya29..%s", s[:4])
		}
		return cleanTok
	}

	if len(cleanTok) <= 8 {
		return cleanTok
	}
	return cleanTok[:8]
}

// ExtractTokenExpiry parses token JSON to extract expiration timestamp.
func ExtractTokenExpiry(dir string) (time.Time, bool) {
	tokFiles := []string{
		filepath.Join(dir, "antigravity-cli", "antigravity-oauth-token"),
		filepath.Join(dir, "antigravity-oauth-token"),
	}
	for _, f := range tokFiles {
		data, err := os.ReadFile(f)
		if err != nil {
			continue
		}
		data = bytes.TrimPrefix(data, []byte("\xef\xbb\xbf"))
		data = bytes.TrimPrefix(data, []byte("\ufeff"))

		type OAuthFile struct {
			Token struct {
				Expiry string `json:"expiry"`
			} `json:"token"`
			Expiry string `json:"expiry"`
		}

		var parsed OAuthFile
		if err := json.Unmarshal(data, &parsed); err == nil {
			expStr := parsed.Token.Expiry
			if expStr == "" {
				expStr = parsed.Expiry
			}
			if expStr != "" {
				if t, err := time.Parse(time.RFC3339Nano, expStr); err == nil {
					return t, true
				}
				if t, err := time.Parse(time.RFC3339, expStr); err == nil {
					return t, true
				}
			}
		}
	}
	return time.Time{}, false
}

// ExtractRefreshToken parses token JSON or file to extract refresh token string.
func ExtractRefreshToken(dir string) string {
	tokFiles := []string{
		filepath.Join(dir, "antigravity-cli", "antigravity-oauth-token"),
		filepath.Join(dir, "antigravity-oauth-token"),
	}
	for _, f := range tokFiles {
		data, err := os.ReadFile(f)
		if err != nil {
			continue
		}
		data = bytes.TrimPrefix(data, []byte("\xef\xbb\xbf"))
		data = bytes.TrimPrefix(data, []byte("\ufeff"))

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
	}
	return ""
}

func (v *Vault) GetRefreshToken(dir string) string {
	return ExtractRefreshToken(dir)
}

// ExtractTokenEmail parses token JSON, extracts id_token JWT, and decodes the email claim.
func ExtractTokenEmail(dir string) string {
	tokFiles := []string{
		filepath.Join(dir, "antigravity-cli", "antigravity-oauth-token"),
		filepath.Join(dir, "antigravity-oauth-token"),
	}
	for _, f := range tokFiles {
		data, err := os.ReadFile(f)
		if err != nil {
			continue
		}
		data = bytes.TrimPrefix(data, []byte("\xef\xbb\xbf"))
		data = bytes.TrimPrefix(data, []byte("\ufeff"))

		var parsed struct {
			IdToken string `json:"id_token"`
			Token   struct {
				IdToken string `json:"id_token"`
			} `json:"token"`
		}
		if err := json.Unmarshal(data, &parsed); err == nil {
			idTok := parsed.IdToken
			if idTok == "" {
				idTok = parsed.Token.IdToken
			}
			if idTok != "" {
				parts := strings.Split(idTok, ".")
				if len(parts) >= 2 {
					payload := parts[1]
					decoded, err := base64.RawURLEncoding.DecodeString(payload)
					if err != nil {
						if rem := len(payload)%4; rem != 0 {
							payload += strings.Repeat("=", 4-rem)
						}
						decoded, err = base64.URLEncoding.DecodeString(payload)
					}
					if err == nil {
						var claims struct {
							Email string `json:"email"`
						}
						if err := json.Unmarshal(decoded, &claims); err == nil && claims.Email != "" {
							return strings.ToLower(strings.TrimSpace(claims.Email))
						}
					}
				}
			}
		}
	}
	return ""
}

func (v *Vault) GetTokenEmail(dir string) string {
	return ExtractTokenEmail(dir)
}


// RefreshOAuthToken performs HTTP refresh using Google OAuth endpoint.
func RefreshOAuthToken(refreshToken string) (string, int, error) {
	if strings.TrimSpace(refreshToken) == "" {
		return "", 0, errors.New("empty refresh token")
	}

	data := url.Values{}
	data.Set("client_id", GoogleClientID)
	data.Set("client_secret", GoogleClientSecret)
	data.Set("grant_type", "refresh_token")
	data.Set("refresh_token", refreshToken)

	client := &http.Client{Timeout: 8 * time.Second}
	resp, err := client.PostForm("https://oauth2.googleapis.com/token", data)
	if err != nil {
		return "", 0, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != 200 {
		return "", 0, fmt.Errorf("oauth endpoint returned status %d", resp.StatusCode)
	}

	var res struct {
		AccessToken string `json:"access_token"`
		ExpiresIn   int    `json:"expires_in"`
	}
	if err := json.NewDecoder(resp.Body).Decode(&res); err != nil {
		return "", 0, err
	}

	if res.AccessToken == "" {
		return "", 0, errors.New("empty access token in refresh response")
	}

	expiresIn := res.ExpiresIn
	if expiresIn <= 0 {
		expiresIn = 3600
	}

	return res.AccessToken, expiresIn, nil
}

// SaveRefreshedAccessToken writes updated access token and expiry to file without destroying refresh token.
func (v *Vault) SaveRefreshedAccessToken(dir string, newTok string, expiresIn int) {
	tokFiles := []string{
		filepath.Join(dir, "antigravity-cli", "antigravity-oauth-token"),
		filepath.Join(dir, "antigravity-oauth-token"),
	}
	newExpiry := time.Now().Add(time.Duration(expiresIn) * time.Second).Format(time.RFC3339Nano)

	for _, aTokPath := range tokFiles {
		if data, err := os.ReadFile(aTokPath); err == nil {
			data = bytes.TrimPrefix(data, []byte("\xef\xbb\xbf"))
			data = bytes.TrimPrefix(data, []byte("\ufeff"))
			var parsed map[string]interface{}
			if json.Unmarshal(data, &parsed) == nil {
				if tokMap, ok := parsed["token"].(map[string]interface{}); ok {
					tokMap["access_token"] = newTok
					tokMap["expiry"] = newExpiry
				} else {
					parsed["access_token"] = newTok
					parsed["expiry"] = newExpiry
				}
				if updated, err := json.MarshalIndent(parsed, "", "  "); err == nil {
					_ = os.WriteFile(aTokPath, updated, 0600)
				}
			}
		}
	}
	encTok, err := v.Encrypt(newTok)
	if err == nil {
		_ = os.WriteFile(filepath.Join(dir, "keyring_token.txt"), []byte(encTok), 0600)
	}
}

// EnsureValidAccessToken reads access token and refreshes it via OAuth endpoint if expired and refresh token exists.
func (v *Vault) EnsureValidAccessToken(dir string) string {
	tok := v.ReadTokenFromDir(dir)
	rf := ExtractRefreshToken(dir)

	expiry, hasExpiry := ExtractTokenExpiry(dir)
	isExpired := false
	if hasExpiry {
		// Expired if current time is within 5 minutes of expiration
		isExpired = time.Now().Add(5 * time.Minute).After(expiry)
	}

	// Token is valid and fresh - return immediately
	if tok != "" && hasExpiry && !isExpired {
		return tok
	}

	// Token expired, missing, or needs refresh
	if rf != "" {
		if newTok, expiresIn, err := RefreshOAuthToken(rf); err == nil && newTok != "" {
			v.SaveRefreshedAccessToken(dir, newTok, expiresIn)
			return newTok
		}
	}

	return tok
}

// SaveTokenToContext saves token JSON and encrypted keyring entry into directory context without stripping existing OAuth fields like refresh_token.
func (v *Vault) SaveTokenToContext(dir string, tokenInput string) error {
	tokenInput = strings.TrimPrefix(tokenInput, "\ufeff")
	tokenInput = strings.TrimPrefix(tokenInput, "\xef\xbb\xbf")
	tokenInput = strings.TrimSpace(tokenInput)
	if tokenInput == "" {
		return errors.New("empty token")
	}

	cleanAccessToken := ExtractCleanAccessToken(tokenInput)

	tokFiles := []string{
		filepath.Join(dir, "antigravity-cli", "antigravity-oauth-token"),
		filepath.Join(dir, "antigravity-oauth-token"),
	}

	isFullJSON := strings.HasPrefix(tokenInput, "{") && strings.HasSuffix(tokenInput, "}")

	for _, tokFile := range tokFiles {
		_ = os.MkdirAll(filepath.Dir(tokFile), 0755)

		if isFullJSON {
			var parsed map[string]interface{}
			if json.Unmarshal([]byte(tokenInput), &parsed) == nil {
				_ = os.WriteFile(tokFile, []byte(tokenInput), 0600)
				continue
			}
		}

		if data, err := os.ReadFile(tokFile); err == nil && len(data) > 0 {
			data = bytes.TrimPrefix(data, []byte("\xef\xbb\xbf"))
			data = bytes.TrimPrefix(data, []byte("\ufeff"))
			var parsed map[string]interface{}
			if json.Unmarshal(data, &parsed) == nil {
				if tokMap, ok := parsed["token"].(map[string]interface{}); ok {
					tokMap["access_token"] = cleanAccessToken
				} else {
					parsed["access_token"] = cleanAccessToken
				}
				if updated, err := json.MarshalIndent(parsed, "", "  "); err == nil {
					_ = os.WriteFile(tokFile, updated, 0600)
					continue
				}
			}
		}

		if tokFile == tokFiles[0] {
			jsonTok := fmt.Sprintf(`{"token":{"access_token":"%s"}}`, cleanAccessToken)
			_ = os.WriteFile(tokFile, []byte(jsonTok), 0600)
		}
	}

	if cleanAccessToken != "" {
		encTok, err := v.Encrypt(cleanAccessToken)
		if err == nil {
			_ = os.WriteFile(filepath.Join(dir, "keyring_token.txt"), []byte(encTok), 0600)
		}
	}
	return nil
}

// PurgeGlobalKeyring clears primary ~/.gemini credentials and Windows Credential Manager.
func (v *Vault) PurgeGlobalKeyring() {
	primaryDir := filepath.Join(v.userHome, ".gemini")
	_ = os.Remove(filepath.Join(primaryDir, "keyring_token.txt"))
	_ = os.Remove(filepath.Join(primaryDir, "antigravity-cli", "antigravity-oauth-token"))
	_ = os.Remove(filepath.Join(primaryDir, "antigravity-oauth-token"))
	_ = os.Remove(filepath.Join(primaryDir, "google_accounts.json"))
	_ = os.RemoveAll(filepath.Join(primaryDir, ".keyring"))

	if cmdkeyPath, err := exec.LookPath("cmdkey.exe"); err == nil {
		_ = exec.Command(cmdkeyPath, "/delete:gemini:antigravity").Run()
	}
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
