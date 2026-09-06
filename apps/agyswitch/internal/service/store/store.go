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

	"agyswitch/internal/model"
	"agyswitch/internal/service/vault"
)

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

	currentActive := s.GetActiveAccount()
	if currentActive != "" && !strings.EqualFold(currentActive, "default") && !strings.EqualFold(currentActive, acc) {
		currentActiveDir := s.GetAccountDirectory(currentActive)
		_ = os.MkdirAll(currentActiveDir, 0755)

		_ = MirrorDirectory(primaryDir, currentActiveDir)

		curToken := s.Vault.ReadTokenFromDir(primaryDir)
		if curToken != "" {
			_ = s.Vault.SaveTokenToContext(currentActiveDir, curToken)
		}
	}

	targetDir := s.GetAccountDirectory(acc)
	if _, err := os.Stat(targetDir); os.IsNotExist(err) {
		_ = os.MkdirAll(targetDir, 0755)
	}

	_ = MirrorDirectory(targetDir, primaryDir)

	targetToken := s.Vault.ReadTokenFromDir(targetDir)
	if targetToken != "" {
		_ = s.Vault.SaveTokenToContext(primaryDir, targetToken)
	}

	activeFile := filepath.Join(primaryDir, "active_account.txt")
	_ = os.WriteFile(activeFile, []byte(acc), 0644)

	_ = s.Vault.SyncKeyringCredentials(primaryDir)
	return nil
}

func MirrorDirectory(src, dst string) error {
	_ = os.MkdirAll(dst, 0755)
	return filepath.Walk(src, func(path string, info os.FileInfo, err error) error {
		if err != nil {
			return nil
		}
		relPath, err := filepath.Rel(src, path)
		if err != nil || relPath == "." {
			return nil
		}
		if strings.HasPrefix(relPath, "brain") || strings.HasPrefix(relPath, "antigravity-cli/log") {
			if info.IsDir() {
				return filepath.SkipDir
			}
			return nil
		}
		dstPath := filepath.Join(dst, relPath)
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

	active := s.GetActiveAccount()
	if strings.EqualFold(active, acc) {
		primaryDir := filepath.Join(s.UserHome, ".gemini")
		_ = os.Remove(filepath.Join(primaryDir, "keyring_token.txt"))
		_ = os.Remove(filepath.Join(primaryDir, "antigravity-cli", "antigravity-oauth-token"))
		_ = os.Remove(filepath.Join(primaryDir, "antigravity-oauth-token"))
		_ = os.RemoveAll(filepath.Join(primaryDir, ".keyring"))
	}

	return nil
}

func IsIgnoredAccount(name string) bool {
	lower := strings.ToLower(strings.TrimSpace(name))
	if lower == "" || lower == "status" || lower == "template" || lower == "default" {
		return true
	}
	prefixes := []string{"demo", "test", "temp", "tmp", "backup", "copy", "--", "."}
	for _, p := range prefixes {
		if strings.HasPrefix(lower, p) {
			return true
		}
	}
	return false
}

func (s *Store) GetRegistryPath() string {
	return filepath.Join(s.UserHome, ".gemini", "agyswitch_accounts.json")
}

func (s *Store) LoadAccountRegistry() []string {
	regPath := s.GetRegistryPath()
	var names []string
	hasRegistryFile := false

	if data, err := os.ReadFile(regPath); err == nil {
		if json.Unmarshal(data, &names) == nil && len(names) > 0 {
			hasRegistryFile = true
		}
	}

	knownMap := make(map[string]bool)
	if hasRegistryFile {
		for _, n := range names {
			n = strings.TrimSpace(n)
			if n != "" && !IsIgnoredAccount(n) {
				knownMap[n] = true
			}
		}
	} else {
		defaults := []string{"vothuongtruongnhon2002", "fptvttnhon2020", "fptvttnhon2026", "nhontruongvo", "nhontruongvo3"}
		for _, d := range defaults {
			if !IsIgnoredAccount(d) {
				knownMap[d] = true
			}
		}

		entries, err := os.ReadDir(s.UserHome)
		if err == nil {
			for _, e := range entries {
				if e.IsDir() && strings.HasPrefix(e.Name(), ".gemini_") {
					accName := strings.TrimPrefix(e.Name(), ".gemini_")
					if !IsIgnoredAccount(accName) {
						knownMap[accName] = true
					}
				}
			}
		}
	}

	var result []string
	for k := range knownMap {
		result = append(result, k)
	}
	sort.Strings(result)
	return result
}

func (s *Store) SaveAccountRegistry(names []string) error {
	regPath := s.GetRegistryPath()
	_ = os.MkdirAll(filepath.Dir(regPath), 0755)

	var clean []string
	seen := make(map[string]bool)
	for _, n := range names {
		n = strings.TrimSpace(n)
		if n != "" && !seen[n] {
			clean = append(clean, n)
			seen[n] = true
		}
	}
	sort.Strings(clean)

	data, err := json.MarshalIndent(clean, "", "  ")
	if err != nil {
		return err
	}

	return os.WriteFile(regPath, data, 0644)
}

func (s *Store) ListAccountNames() []string {
	return s.LoadAccountRegistry()
}

func (s *Store) AddAccount(name string) error {
	cleanName := strings.TrimSpace(name)
	if cleanName == "" {
		return errors.New("invalid account name")
	}

	accDir := s.GetAccountDirectory(cleanName)
	if err := os.MkdirAll(accDir, 0755); err != nil {
		return fmt.Errorf("failed to create account directory: %v", err)
	}

	names := s.LoadAccountRegistry()
	names = append(names, cleanName)
	_ = s.SaveAccountRegistry(names)

	return s.SetActiveAccount(cleanName)
}

func (s *Store) RenameAccount(oldName, newName string) error {
	oldName = strings.TrimSpace(oldName)
	newName = strings.TrimSpace(newName)

	if oldName == "" || newName == "" {
		return errors.New("old and new account names must not be empty")
	}

	oldDir := s.GetAccountDirectory(oldName)
	newDir := s.GetAccountDirectory(newName)

	if _, err := os.Stat(oldDir); err == nil {
		_ = os.Rename(oldDir, newDir)
	} else {
		_ = os.MkdirAll(newDir, 0755)
	}

	names := s.LoadAccountRegistry()
	var updated []string
	for _, n := range names {
		if strings.EqualFold(n, oldName) {
			updated = append(updated, newName)
		} else {
			updated = append(updated, n)
		}
	}
	_ = s.SaveAccountRegistry(updated)

	active := s.GetActiveAccount()
	if strings.EqualFold(active, oldName) {
		activeFile := filepath.Join(s.UserHome, ".gemini", "active_account.txt")
		_ = os.WriteFile(activeFile, []byte(newName), 0644)
	}

	return nil
}

func (s *Store) DeleteAccount(name string) error {
	name = strings.TrimSpace(name)
	if name == "" {
		return errors.New("account name cannot be empty")
	}

	_ = s.ResetAccount(name)

	names := s.LoadAccountRegistry()
	var updated []string
	for _, n := range names {
		if !strings.EqualFold(n, name) {
			updated = append(updated, n)
		}
	}
	_ = s.SaveAccountRegistry(updated)

	active := s.GetActiveAccount()
	if strings.EqualFold(active, name) {
		if len(updated) > 0 {
			_ = s.SetActiveAccount(updated[0])
		} else {
			activeFile := filepath.Join(s.UserHome, ".gemini", "active_account.txt")
			_ = os.Remove(activeFile)
		}
	}

	accDir := s.GetAccountDirectory(name)
	_ = os.RemoveAll(accDir)

	return nil
}

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

func ExtractGroupQuotas(summary *model.QuotaSummary) (gPct float64, cPct float64) {
	if summary == nil {
		return -1, -1
	}
	gPct, cPct = -1, -1
	for _, g := range summary.Groups {
		name := strings.ToLower(g.DisplayName)
		for _, b := range g.Buckets {
			pct := b.RemainingFraction * 100.0
			if strings.Contains(name, "gemini") {
				if gPct < 0 || pct < gPct {
					gPct = pct
				}
			} else if strings.Contains(name, "claude") || strings.Contains(name, "gpt") || strings.Contains(name, "3p") {
				if cPct < 0 || pct < cPct {
					cPct = pct
				}
			}
		}
	}
	return gPct, cPct
}

func (s *Store) PurgeQuotaCache(accName string) {
	_ = os.Remove(s.GetQuotaCachePath(accName))
}

func (s *Store) PurgeAllQuotaCaches() {
	for _, name := range s.ListAccountNames() {
		s.PurgeQuotaCache(name)
	}
}

func (s *Store) ListAccounts() []model.AccountInfo {
	known := s.ListAccountNames()
	active := s.GetActiveAccount()

	result := make([]model.AccountInfo, len(known))
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

			var summary *model.QuotaSummary
			var gPct, cPct float64 = -1, -1

			if isLoggedIn {
				if q, err := s.GetAccountQuota(accName); err == nil {
					summary = q
					gPct, cPct = ExtractGroupQuotas(q)
				} else {
					if strings.Contains(err.Error(), "401") || strings.Contains(err.Error(), "403") {
						quotaStatus = "🔑 Login Required"
					}
				}
			}

			result[idx] = model.AccountInfo{
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
			s.SaveQuotaCache(accName, result[idx])
		}(i, name)
	}

	wg.Wait()
	return result
}

func (s *Store) GetQuotaCachePath(accName string) string {
	return filepath.Join(s.GetAccountDirectory(accName), "quota_cache.json")
}

func (s *Store) SaveQuotaCache(accName string, info model.AccountInfo) {
	cachePath := s.GetQuotaCachePath(accName)
	if data, err := json.Marshal(info); err == nil {
		_ = os.WriteFile(cachePath, data, 0644)
	}
}

func (s *Store) LoadQuotaCache(accName string) (*model.AccountInfo, bool) {
	cachePath := s.GetQuotaCachePath(accName)
	data, err := os.ReadFile(cachePath)
	if err != nil {
		return nil, false
	}
	var info model.AccountInfo
	if err := json.Unmarshal(data, &info); err == nil {
		return &info, true
	}
	return nil, false
}

func (s *Store) ListAccountsFast() []model.AccountInfo {
	known := s.ListAccountNames()
	active := s.GetActiveAccount()

	result := make([]model.AccountInfo, len(known))
	for i, name := range known {
		accDir := s.GetAccountDirectory(name)
		tok := s.Vault.ReadTokenFromDir(accDir)
		email := fmt.Sprintf("%s@gmail.com", name)
		sig := s.Vault.GetShortSignature(tok)
		isLoggedIn := tok != ""

		var summary *model.QuotaSummary
		var gPct, cPct float64 = -1, -1
		quotaStatus := "✔ Quota OK"
		if !isLoggedIn {
			quotaStatus = "✘ Logged Out"
		}

		if cached, ok := s.LoadQuotaCache(name); ok && cached != nil {
			summary = cached.QuotaSummary
			gPct = cached.GeminiQuotaPct
			cPct = cached.ClaudeQuotaPct
			if cached.QuotaStatus != "" {
				quotaStatus = cached.QuotaStatus
			}
		}

		result[i] = model.AccountInfo{
			AccountName:    name,
			Email:          email,
			IsActive:       strings.EqualFold(name, active),
			TokenSig:       sig,
			IsLoggedIn:     isLoggedIn,
			QuotaStatus:    quotaStatus,
			GeminiQuotaPct: gPct,
			ClaudeQuotaPct: cPct,
			QuotaSummary:   summary,
		}
	}
	return result
}

func (s *Store) GetRecommendedAccountInfo(accs []model.AccountInfo) (string, string) {
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

func (s *Store) SelectBestQuotaAccount() string {
	accs := s.ListAccounts()
	bestAcc, _ := s.GetRecommendedAccountInfo(accs)
	if bestAcc == "" {
		return s.GetActiveAccount()
	}
	return bestAcc
}

func FetchUserQuotaSummary(tok string) (*model.QuotaSummary, error) {
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

	var summary model.QuotaSummary
	if err := json.NewDecoder(resp.Body).Decode(&summary); err != nil {
		return nil, err
	}

	return &summary, nil
}

func (s *Store) GetAccountQuota(name string) (*model.QuotaSummary, error) {
	accDir := s.GetAccountDirectory(name)
	tok := s.Vault.EnsureValidAccessToken(accDir)
	if tok == "" {
		return nil, errors.New("account is logged out")
	}

	return FetchUserQuotaSummary(tok)
}

func ExtractDetailedModelBuckets(summary *model.QuotaSummary) []model.ModelBucketDetail {
	if summary == nil {
		return nil
	}
	var details []model.ModelBucketDetail
	now := time.Now()

	for _, g := range summary.Groups {
		grpName := g.DisplayName
		for _, b := range g.Buckets {
			pct := b.RemainingFraction * 100.0
			win := b.Window
			if win == "" {
				if strings.Contains(b.BucketID, "weekly") {
					win = "weekly"
				} else if strings.Contains(b.BucketID, "daily") {
					win = "daily"
				} else {
					win = "standard"
				}
			}

			resetMsg := "Quota Available"
			var resetTime time.Time
			var until time.Duration

			if b.ResetTime != "" {
				if t, err := time.Parse(time.RFC3339, b.ResetTime); err == nil {
					resetTime = t
					if t.After(now) {
						until = t.Sub(now)
						hours := int(until.Hours())
						mins := int(until.Minutes()) % 60
						if hours >= 24 {
							days := hours / 24
							h := hours % 24
							resetMsg = fmt.Sprintf("Refreshes in %dd %dh", days, h)
						} else if hours > 0 {
							resetMsg = fmt.Sprintf("Refreshes in %dh %dm", hours, mins)
						} else {
							secs := int(until.Seconds()) % 60
							resetMsg = fmt.Sprintf("Refreshes in %dm %ds", mins, secs)
						}
					}
				}
			}

			details = append(details, model.ModelBucketDetail{
				BucketID:         b.BucketID,
				ModelDisplayName: b.DisplayName,
				QuotaGroup:       grpName,
				WindowType:       win,
				RemainingPct:     pct,
				ResetTime:        resetTime,
				TimeUntilReset:   until,
				ResetMessage:     resetMsg,
				IsThrottled:      pct <= 0.0,
			})
		}
	}
	return details
}

func RenderQuotaSummary(email string, summary *model.QuotaSummary) string {
	if summary == nil || len(summary.Groups) == 0 {
		return fmt.Sprintf("\033[31mNo quota details available for %s\033[0m\r\n", email)
	}

	var sb strings.Builder
	sb.WriteString("\r\n└ \033[1;36mModels & Quota\033[0m\r\n\r\n")
	sb.WriteString(fmt.Sprintf("  Account: \033[1;32m%s\033[0m\r\n", email))

	for _, g := range summary.Groups {
		sb.WriteString(fmt.Sprintf("\r\n\033[1;33m%s\033[0m\r\n", strings.ToUpper(g.DisplayName)))
		if g.Description != "" {
			sb.WriteString(fmt.Sprintf("  %s\r\n", g.Description))
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

			colorCode := "\033[32m"
			if pct < 20.0 {
				colorCode = "\033[31m"
			} else if pct < 50.0 {
				colorCode = "\033[33m"
			}

			sb.WriteString(fmt.Sprintf("\r\n  %s\r\n", b.DisplayName))
			sb.WriteString(fmt.Sprintf("    [%s%s\033[0m] %.2f%%\r\n", colorCode, bar, pct))

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
			sb.WriteString(fmt.Sprintf("    %s\r\n", refreshMsg))
		}
	}

	if summary.Description != "" {
		sb.WriteString("\r\n  │" + summary.Description + "\r\n")
	}

	return sb.String()
}
