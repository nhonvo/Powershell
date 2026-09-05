package store

import (
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"net/http"
	"os"
	"path/filepath"
	"strings"
	"sync"
	"time"

	"agyswitch/vault"
)

type AccountInfo struct {
	AccountName string `json:"accountName"`
	Email       string `json:"email"`
	IsActive    bool   `json:"isActive"`
	TokenSig    string `json:"tokenSig"`
	IsLoggedIn  bool   `json:"isLoggedIn"`
	QuotaStatus string `json:"quotaStatus"`
}

type Store struct {
	UserHome string
	Vault    *vault.Vault
}

func NewStore(userHome string, v *vault.Vault) *Store {
	if userHome == "" {
		h, err := os.UserHomeDir()
		if err == nil {
			userHome = h
		}
	}
	if v == nil {
		v = vault.NewVault(userHome)
	}
	return &Store{
		UserHome: userHome,
		Vault:    v,
	}
}

// GetAccountDirectory resolves path to ~/.gemini_<accountName> (or ~/.gemini for default).
func (s *Store) GetAccountDirectory(accountName string) string {
	acc := strings.TrimSpace(accountName)
	if acc == "" || acc == "default" {
		return filepath.Join(s.UserHome, ".gemini")
	}
	return filepath.Join(s.UserHome, fmt.Sprintf(".gemini_%s", acc))
}

// GetActiveAccount reads ~/.gemini/active_account.txt.
func (s *Store) GetActiveAccount() string {
	activeFile := filepath.Join(s.UserHome, ".gemini", "active_account.txt")
	data, err := os.ReadFile(activeFile)
	if err != nil {
		return "vothuongtruongnhon2002"
	}
	acc := strings.TrimSpace(string(data))
	if acc == "" {
		return "vothuongtruongnhon2002"
	}
	return acc
}

// SetActiveAccount backs up current active context, mirrors target context, and syncs keyring.
func (s *Store) SetActiveAccount(accountName string) error {
	acc := strings.TrimSpace(accountName)
	if acc == "" {
		return errors.New("account name cannot be empty")
	}

	primaryDir := filepath.Join(s.UserHome, ".gemini")

	// Step 1: Pre-switch backup of current active account (~/.gemini -> ~/.gemini_<currentActive>)
	currentActive := s.GetActiveAccount()
	if currentActive != "" && !strings.EqualFold(currentActive, "default") && !strings.EqualFold(currentActive, acc) {
		currentActiveDir := s.GetAccountDirectory(currentActive)
		_ = os.MkdirAll(currentActiveDir, 0755)

		curToken := s.Vault.ReadTokenFromDir(primaryDir)
		if curToken != "" && curToken != "IDE_ACTIVE_SESSION" {
			_ = s.Vault.SaveTokenToContext(currentActiveDir, curToken)
		}
		_ = s.MirrorDirectory(primaryDir, currentActiveDir)
	}

	targetDir := s.GetAccountDirectory(acc)
	if err := os.MkdirAll(targetDir, 0755); err != nil {
		return err
	}
	if err := os.MkdirAll(primaryDir, 0755); err != nil {
		return err
	}

	// Always sanitize target context files with exact account email
	s.SanitizeAccountDirectory(acc)

	// Read target account token before mirroring
	targetToken := s.Vault.ReadTokenFromDir(targetDir)

	// Step 2: Mirror target directory to primary directory
	if !strings.EqualFold(targetDir, primaryDir) {
		if err := s.MirrorDirectory(targetDir, primaryDir); err != nil {
			return err
		}
	}

	// Always force-overwrite primary google_accounts.json & settings.json with target account email
	s.SanitizeAccountDirectoryIn(primaryDir, acc)

	// Record active account marker
	activeFile := filepath.Join(primaryDir, "active_account.txt")
	_ = os.WriteFile(activeFile, []byte(acc), 0644)

	// Step 3: Keyring isolation logic - Purge global keyring if target account has no explicit OAuth token
	if targetToken != "" && targetToken != "IDE_ACTIVE_SESSION" {
		_ = s.Vault.SaveTokenToContext(targetDir, targetToken)
		_ = s.Vault.SaveTokenToContext(primaryDir, targetToken)
	} else {
		s.Vault.PurgeGlobalKeyring()
	}

	return nil
}

// SanitizeAccountDirectory ensures google_accounts.json and settings.json exist cleanly.
func (s *Store) SanitizeAccountDirectory(accountName string) {
	accDir := s.GetAccountDirectory(accountName)
	s.SanitizeAccountDirectoryIn(accDir, accountName)
}

func (s *Store) SanitizeAccountDirectoryIn(targetDir, accountName string) {
	_ = os.MkdirAll(filepath.Join(targetDir, "antigravity-cli"), 0755)

	email := fmt.Sprintf("%s@gmail.com", accountName)
	if strings.Contains(accountName, "@") {
		email = accountName
	}

	gPath := filepath.Join(targetDir, "google_accounts.json")
	gObj := map[string]interface{}{
		"accounts":      []map[string]string{{"email": email}},
		"activeAccount": email,
	}
	if data, err := json.MarshalIndent(gObj, "", "  "); err == nil {
		_ = os.WriteFile(gPath, data, 0644)
	}

	sPath := filepath.Join(targetDir, "antigravity-cli", "settings.json")
	sObj := map[string]string{
		"accountName": accountName,
		"userEmail":   email,
	}
	if data, err := json.MarshalIndent(sObj, "", "  "); err == nil {
		_ = os.WriteFile(sPath, data, 0644)
	}
}

// MirrorDirectory performs 1-to-1 file copy from src to dst.
func (s *Store) MirrorDirectory(srcDir, dstDir string) error {
	if _, err := os.Stat(srcDir); os.IsNotExist(err) {
		return nil
	}
	_ = os.MkdirAll(dstDir, 0755)

	return filepath.Walk(srcDir, func(path string, info os.FileInfo, err error) error {
		if err != nil {
			return nil
		}
		relPath, err := filepath.Rel(srcDir, path)
		if err != nil || relPath == "." {
			return nil
		}

		// Skip temporary or database lock files
		if strings.HasSuffix(relPath, ".tmp") || strings.HasSuffix(relPath, ".lock") {
			return nil
		}

		dstPath := filepath.Join(dstDir, relPath)
		if info.IsDir() {
			return os.MkdirAll(dstPath, info.Mode())
		}

		return copyFile(path, dstPath)
	})
}

func copyFile(src, dst string) error {
	in, err := os.Open(src)
	if err != nil {
		return err
	}
	defer in.Close()

	_ = os.MkdirAll(filepath.Dir(dst), 0755)
	out, err := os.Create(dst)
	if err != nil {
		return err
	}
	defer out.Close()

	_, err = io.Copy(out, in)
	return err
}

// ResetAccount clears all OAuth token files and keyring credentials for account.
func (s *Store) ResetAccount(accountName string) error {
	acc := strings.TrimSpace(accountName)
	if acc == "" {
		return errors.New("account name cannot be empty")
	}

	accDir := s.GetAccountDirectory(acc)
	_ = os.Remove(filepath.Join(accDir, "keyring_token.txt"))
	_ = os.Remove(filepath.Join(accDir, "antigravity-cli", "antigravity-oauth-token"))
	_ = os.Remove(filepath.Join(accDir, "antigravity-oauth-token"))
	_ = os.RemoveAll(filepath.Join(accDir, ".keyring"))

	// If resetting the currently active account, clear primary ~/.gemini files as well
	active := s.GetActiveAccount()
	if strings.EqualFold(acc, active) {
		s.Vault.PurgeGlobalKeyring()
	}

	return nil
}

// SelectBestQuotaAccount scans accounts and returns active account if logged in, or first logged-in account.
func (s *Store) SelectBestQuotaAccount() string {
	accs := s.ListAccounts()
	active := s.GetActiveAccount()

	// Check active account
	for _, a := range accs {
		if strings.EqualFold(a.AccountName, active) && a.IsLoggedIn {
			return a.AccountName
		}
	}

	// Pick first available logged-in account
	for _, a := range accs {
		if a.IsLoggedIn {
			return a.AccountName
		}
	}

	return active
}

// ProbeQuotaStatus checks live API response code for active OAuth token.
func ProbeQuotaStatus(tok string) string {
	tok = strings.TrimSpace(tok)
	if tok == "" {
		return "✘ Logged Out"
	}

	client := &http.Client{Timeout: 1 * time.Second}
	req, err := http.NewRequest("POST", "https://daily-cloudcode-pa.googleapis.com/v1internal:fetchAvailableModels", strings.NewReader("{}"))
	if err != nil {
		return "✔ Quota OK"
	}
	req.Header.Set("Authorization", "Bearer "+tok)
	req.Header.Set("Content-Type", "application/json")

	resp, err := client.Do(req)
	if err != nil {
		return "✔ Quota OK"
	}
	defer resp.Body.Close()

	if resp.StatusCode == 200 {
		return "✔ Quota OK (200 OK)"
	} else if resp.StatusCode == 429 {
		return "✘ Quota Limit (429 Rate Limit)"
	} else if resp.StatusCode == 401 || resp.StatusCode == 403 {
		return "✘ Token Expired (401 Auth Required)"
	}
	return fmt.Sprintf("✔ Quota OK (%d)", resp.StatusCode)
}

// ListAccounts returns all registered accounts and their status.
func (s *Store) ListAccounts() []AccountInfo {
	known := []string{"vothuongtruongnhon2002", "fptvttnhon2020", "fptvttnhon2026", "nhontruongvo", "nhontruongvo3"}
	active := s.GetActiveAccount()

	result := make([]AccountInfo, len(known))
	var wg sync.WaitGroup

	for i, name := range known {
		wg.Add(1)
		go func(idx int, accName string) {
			defer wg.Done()
			accDir := s.GetAccountDirectory(accName)
			tok := s.Vault.ReadTokenFromDir(accDir)

			email := fmt.Sprintf("%s@gmail.com", accName)
			sig := s.Vault.GetShortSignature(tok)
			isLoggedIn := tok != ""
			quotaStatus := ProbeQuotaStatus(tok)

			result[idx] = AccountInfo{
				AccountName: accName,
				Email:       email,
				IsActive:    strings.EqualFold(accName, active),
				TokenSig:    sig,
				IsLoggedIn:  isLoggedIn,
				QuotaStatus: quotaStatus,
			}
		}(i, name)
	}

	wg.Wait()
	return result
}


