package account

import (
	"encoding/json"
	"fmt"
	"io"
	"os"
	"path/filepath"
	"strings"
	"sync"
)

type AccountInfo struct {
	AccountName    string  `json:"account_name"`
	Email          string  `json:"email"`
	IsActive       bool    `json:"is_active"`
	TokenSig       string  `json:"token_sig"`
	IsLoggedIn     bool    `json:"is_logged_in"`
	QuotaStatus    string  `json:"quota_status"`
	GeminiQuotaPct float64 `json:"gemini_quota_pct"`
	ClaudeQuotaPct float64 `json:"claude_quota_pct"`
	HomeDir        string  `json:"home_dir"`
}

type AccountManager struct {
	mu       sync.RWMutex
	userHome string
}

func NewAccountManager() *AccountManager {
	home, _ := os.UserHomeDir()
	return &AccountManager{userHome: home}
}

// GetActiveAccount reads ~/.gemini/active_account.txt
func (m *AccountManager) GetActiveAccount() string {
	activeFile := filepath.Join(m.userHome, ".gemini", "active_account.txt")
	if data, err := os.ReadFile(activeFile); err == nil {
		clean := strings.TrimSpace(string(data))
		if clean != "" {
			return clean
		}
	}
	return "fptvttnhon2020"
}

// ListAccounts scans ~/.gemini_* account directories and quota caches
func (m *AccountManager) ListAccounts() []AccountInfo {
	m.mu.RLock()
	defer m.mu.RUnlock()

	activeName := m.GetActiveAccount()
	var results []AccountInfo

	entries, err := os.ReadDir(m.userHome)
	if err != nil {
		return results
	}

	for _, entry := range entries {
		if !entry.IsDir() || !strings.HasPrefix(entry.Name(), ".gemini_") {
			continue
		}
		accName := strings.TrimPrefix(entry.Name(), ".gemini_")
		if accName == "status" || accName == "" {
			continue
		}

		accDir := filepath.Join(m.userHome, entry.Name())
		info := AccountInfo{
			AccountName:    accName,
			Email:          fmt.Sprintf("%s@gmail.com", accName),
			IsActive:       strings.EqualFold(accName, activeName),
			QuotaStatus:    "✔ Quota OK",
			GeminiQuotaPct: 100,
			ClaudeQuotaPct: 100,
			HomeDir:        accDir,
		}

		// Read cached quota if present
		quotaFile := filepath.Join(accDir, "quota_cache.json")
		if data, err := os.ReadFile(quotaFile); err == nil {
			var cached struct {
				Email          string  `json:"email"`
				TokenSig       string  `json:"tokenSig"`
				IsLoggedIn     bool    `json:"isLoggedIn"`
				QuotaStatus    string  `json:"quotaStatus"`
				GeminiQuotaPct float64 `json:"geminiQuotaPct"`
				ClaudeQuotaPct float64 `json:"claudeQuotaPct"`
			}
			if json.Unmarshal(data, &cached) == nil {
				if cached.Email != "" {
					info.Email = cached.Email
				}
				info.TokenSig = cached.TokenSig
				info.IsLoggedIn = cached.IsLoggedIn
				if cached.QuotaStatus != "" {
					info.QuotaStatus = cached.QuotaStatus
				}
				info.GeminiQuotaPct = cached.GeminiQuotaPct
				info.ClaudeQuotaPct = cached.ClaudeQuotaPct
			}
		}

		// Check token presence
		tokFile := filepath.Join(accDir, "antigravity-oauth-token")
		if _, err := os.Stat(tokFile); err == nil {
			info.IsLoggedIn = true
		}

		results = append(results, info)
	}

	return results
}

// SwitchAccount sets active account in ~/.gemini/active_account.txt and synchronizes credentials
func (m *AccountManager) SwitchAccount(accountName string) error {
	m.mu.Lock()
	defer m.mu.Unlock()

	targetName := strings.TrimSpace(accountName)
	if targetName == "" {
		return fmt.Errorf("account name cannot be empty")
	}

	targetDir := filepath.Join(m.userHome, fmt.Sprintf(".gemini_%s", targetName))
	if fi, err := os.Stat(targetDir); err != nil || !fi.IsDir() {
		return fmt.Errorf("account context directory not found: %s", targetDir)
	}

	// 1. Write ~/.gemini/active_account.txt
	activeFile := filepath.Join(m.userHome, ".gemini", "active_account.txt")
	if err := os.WriteFile(activeFile, []byte(targetName+"\n"), 0644); err != nil {
		return fmt.Errorf("failed to update active account file: %w", err)
	}

	// 2. Synchronize token files to primary ~/.gemini/
	primaryGemini := filepath.Join(m.userHome, ".gemini")
	_ = os.MkdirAll(primaryGemini, 0755)

	filesToSync := []string{
		"antigravity-oauth-token",
		"google-oauth-token",
		"credentials.json",
	}

	for _, fname := range filesToSync {
		src := filepath.Join(targetDir, fname)
		dst := filepath.Join(primaryGemini, fname)
		if _, err := os.Stat(src); err == nil {
			_ = copyFile(src, dst)
		}
	}

	return nil
}

func copyFile(src, dst string) error {
	in, err := os.Open(src)
	if err != nil {
		return err
	}
	defer in.Close()

	out, err := os.Create(dst)
	if err != nil {
		return err
	}
	defer out.Close()

	_, err = io.Copy(out, in)
	return err
}
