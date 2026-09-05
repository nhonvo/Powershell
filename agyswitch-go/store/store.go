package store

import (
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"net/http"
	"os"
	"path/filepath"
	"sort"
	"strings"
	"sync"
	"time"

	"agyswitch/vault"
)

type AccountInfo struct {
	AccountName    string        `json:"accountName"`
	Email          string        `json:"email"`
	IsActive       bool          `json:"isActive"`
	TokenSig       string        `json:"tokenSig"`
	IsLoggedIn     bool          `json:"isLoggedIn"`
	QuotaStatus    string        `json:"quotaStatus"`
	GeminiQuotaPct float64       `json:"geminiQuotaPct"`
	ClaudeQuotaPct float64       `json:"claudeQuotaPct"`
	QuotaSummary   *QuotaSummary `json:"quotaSummary,omitempty"`
}

type QuotaBucket struct {
	BucketID          string  `json:"bucketId"`
	DisplayName       string  `json:"displayName"`
	Window            string  `json:"window"`
	ResetTime         string  `json:"resetTime"`
	Description       string  `json:"description"`
	RemainingFraction float64 `json:"remainingFraction"`
}

type QuotaGroup struct {
	DisplayName string        `json:"displayName"`
	Description string        `json:"description"`
	Buckets     []QuotaBucket `json:"buckets"`
}

type QuotaSummary struct {
	Groups      []QuotaGroup `json:"groups"`
	Description string       `json:"description"`
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
	req.Header.Set("User-Agent", "antigravity/1.1.27")
	req.Header.Set("Content-Type", "application/json")

	resp, err := client.Do(req)
	if err != nil {
		return "✔ Quota OK"
	}
	defer resp.Body.Close()

	if resp.StatusCode == 200 {
		return "✔ Quota OK"
	} else if resp.StatusCode == 429 {
		return "✘ Rate Limit"
	} else if resp.StatusCode == 401 || resp.StatusCode == 403 {
		return "⚡ Auto-Refresh"
	}
	return "✔ Quota OK"
}

// ExtractGroupQuotas returns Gemini and Claude weekly quota percentages from summary.
func ExtractGroupQuotas(summary *QuotaSummary) (gPct float64, cPct float64) {
	if summary == nil {
		return -1, -1
	}
	gPct, cPct = -1, -1
	for _, g := range summary.Groups {
		name := strings.ToLower(g.DisplayName)
		for _, b := range g.Buckets {
			if b.Window == "weekly" || strings.Contains(b.BucketID, "weekly") {
				if strings.Contains(name, "gemini") {
					gPct = b.RemainingFraction * 100.0
				} else if strings.Contains(name, "claude") || strings.Contains(name, "gpt") || strings.Contains(name, "3p") {
					cPct = b.RemainingFraction * 100.0
				}
			}
		}
	}
	return gPct, cPct
}

// ListAccountNames dynamically discovers all registered accounts and .gemini_* directories.
func (s *Store) ListAccountNames() []string {
	knownMap := map[string]bool{
		"vothuongtruongnhon2002": true,
		"fptvttnhon2020":         true,
		"fptvttnhon2026":         true,
		"nhontruongvo":           true,
		"nhontruongvo3":          true,
	}

	entries, err := os.ReadDir(s.UserHome)
	if err == nil {
		for _, e := range entries {
			if e.IsDir() && strings.HasPrefix(e.Name(), ".gemini_") {
				accName := strings.TrimPrefix(e.Name(), ".gemini_")
				if accName != "" && accName != "status" {
					knownMap[accName] = true
				}
			}
		}
	}

	var names []string
	for k := range knownMap {
		names = append(names, k)
	}
	sort.Strings(names)
	return names
}

// AddAccount initializes a new account directory context and sets it active.
func (s *Store) AddAccount(name string) error {
	cleanName := strings.TrimSpace(name)
	if cleanName == "" {
		return errors.New("invalid account name")
	}

	accDir := s.GetAccountDirectory(cleanName)
	if err := os.MkdirAll(accDir, 0755); err != nil {
		return fmt.Errorf("failed to create account directory: %v", err)
	}

	return s.SetActiveAccount(cleanName)
}

// ListAccounts returns all registered accounts and their status.
func (s *Store) ListAccounts() []AccountInfo {
	known := s.ListAccountNames()
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

			var summary *QuotaSummary
			var gPct, cPct float64 = -1, -1

			if isLoggedIn {
				if q, err := s.GetAccountQuota(accName); err == nil {
					summary = q
					gPct, cPct = ExtractGroupQuotas(q)
				}
			}

			result[idx] = AccountInfo{
				AccountName:    accName,
				Email:          email,
				IsActive:       strings.EqualFold(accName, active),
				TokenSig:       sig,
				IsLoggedIn:     isLoggedIn,
				QuotaStatus:    quotaStatus,
				GeminiQuotaPct: gPct,
				ClaudeQuotaPct: cPct,
				QuotaSummary:   summary,
			}
		}(i, name)
	}

	wg.Wait()
	return result
}

// GetRecommendedAccountInfo evaluates best account by highest combined quota.
func (s *Store) GetRecommendedAccountInfo(accs []AccountInfo) (string, string) {
	bestAcc := ""
	bestScore := -1.0

	for _, a := range accs {
		if !a.IsLoggedIn {
			continue
		}
		score := 0.0
		if a.GeminiQuotaPct >= 0 && a.ClaudeQuotaPct >= 0 {
			score = (a.GeminiQuotaPct + a.ClaudeQuotaPct) / 2.0
		} else if a.GeminiQuotaPct >= 0 {
			score = a.GeminiQuotaPct
		} else {
			score = 100.0
		}

		if score > bestScore {
			bestScore = score
			bestAcc = a.AccountName
		}
	}

	if bestAcc == "" {
		return "", "💡 \033[33mNo active authenticated account with quota available.\033[0m"
	}

	if bestScore >= 0 {
		return bestAcc, fmt.Sprintf("💡 \033[1;36mSmart Suggestion:\033[0m Account '\033[1;32m%s\033[0m' has the highest available quota (\033[1;33m%.1f%%\033[0m avg).", bestAcc, bestScore)
	}

	return bestAcc, fmt.Sprintf("💡 \033[1;36mSmart Suggestion:\033[0m Account '\033[1;32m%s\033[0m' is ready for requests.", bestAcc)
}

// FetchUserQuotaSummary queries Google Cloud Code API for live user quota details.
func FetchUserQuotaSummary(tok string) (*QuotaSummary, error) {
	tok = strings.TrimSpace(tok)
	if tok == "" {
		return nil, errors.New("unauthenticated")
	}

	client := &http.Client{Timeout: 4 * time.Second}
	req, err := http.NewRequest("POST", "https://cloudcode-pa.googleapis.com/v1internal:retrieveUserQuotaSummary", strings.NewReader("{}"))
	if err != nil {
		return nil, err
	}
	req.Header.Set("Authorization", "Bearer "+tok)
	req.Header.Set("User-Agent", "antigravity/1.1.27")
	req.Header.Set("Content-Type", "application/json")

	resp, err := client.Do(req)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != 200 {
		return nil, fmt.Errorf("quota API HTTP %d", resp.StatusCode)
	}

	var summary QuotaSummary
	if err := json.NewDecoder(resp.Body).Decode(&summary); err != nil {
		return nil, err
	}

	return &summary, nil
}

// GetAccountQuota refreshes token if needed and fetches live QuotaSummary for named account.
func (s *Store) GetAccountQuota(name string) (*QuotaSummary, error) {
	accDir := s.GetAccountDirectory(name)
	tok := s.Vault.EnsureValidAccessToken(accDir)
	if tok == "" {
		return nil, errors.New("account is logged out")
	}

	return FetchUserQuotaSummary(tok)
}

// RenderQuotaSummary builds terminal UI matching Antigravity Models & Quota layout.
func RenderQuotaSummary(email string, summary *QuotaSummary) string {
	if summary == nil || len(summary.Groups) == 0 {
		return fmt.Sprintf("\033[31mNo quota details available for %s\033[0m\n", email)
	}

	var sb strings.Builder
	sb.WriteString("\n└ \033[1;36mModels & Quota\033[0m\n\n")
	sb.WriteString(fmt.Sprintf("  Account: \033[1;32m%s\033[0m\n", email))

	for _, g := range summary.Groups {
		sb.WriteString(fmt.Sprintf("\n\033[1;33m%s\033[0m\n", strings.ToUpper(g.DisplayName)))
		if g.Description != "" {
			sb.WriteString(fmt.Sprintf("  %s\n", g.Description))
		}

		for _, b := range g.Buckets {
			pct := b.RemainingFraction * 100.0

			filledLen := int((pct / 100.0) * 40.0)
			if filledLen > 40 {
				filledLen = 40
			}
			if filledLen < 0 {
				filledLen = 0
			}
			emptyLen := 40 - filledLen

			bar := strings.Repeat("█", filledLen) + strings.Repeat("░", emptyLen)

			colorCode := "\033[32m" // Green
			if pct < 20.0 {
				colorCode = "\033[31m" // Red
			} else if pct < 50.0 {
				colorCode = "\033[33m" // Yellow
			}

			sb.WriteString(fmt.Sprintf("\n  %s\n", b.DisplayName))
			sb.WriteString(fmt.Sprintf("    [%s%s\033[0m] %.2f%%\n", colorCode, bar, pct))

			refreshMsg := "Quota available"
			if b.ResetTime != "" {
				if t, err := time.Parse(time.RFC3339, b.ResetTime); err == nil {
					diff := time.Until(t)
					if diff > 0 {
						hours := int(diff.Hours())
						mins := int(diff.Minutes()) % 60
						if hours >= 24 {
							days := hours / 24
							h := hours % 24
							refreshMsg = fmt.Sprintf("%.0f%% remaining · Refreshes in %dd %dh", pct, days, h)
						} else {
							refreshMsg = fmt.Sprintf("%.0f%% remaining · Refreshes in %dh %dm", pct, hours, mins)
						}
					}
				}
			}
			sb.WriteString(fmt.Sprintf("    %s\n", refreshMsg))
		}
	}

	if summary.Description != "" {
		sb.WriteString("\n  │" + summary.Description + "\n")
	}

	return sb.String()
}



