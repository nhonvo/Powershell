package store_test

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
	"time"

	"agyswitch/internal/model"
	"agyswitch/internal/service/store"
	"agyswitch/internal/service/vault"
)

func TestStore_AccountDirectoryAndActiveMarker(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "store_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	v := vault.NewVault(tempDir)
	s := store.NewStore(tempDir, v)

	accDir := s.GetAccountDirectory("fptvttnhon2020")
	expected := filepath.Join(tempDir, ".gemini_fptvttnhon2020")
	if accDir != expected {
		t.Errorf("expected '%s', got '%s'", expected, accDir)
	}

	err = s.SetActiveAccount("fptvttnhon2020")
	if err != nil {
		t.Fatalf("failed to set active account: %v", err)
	}

	active := s.GetActiveAccount()
	if active != "fptvttnhon2020" {
		t.Errorf("expected active account 'fptvttnhon2020', got '%s'", active)
	}
}

func TestStore_CRUDAccountRegistry(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "store_crud_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	v := vault.NewVault(tempDir)
	s := store.NewStore(tempDir, v)

	// Add
	err = s.AddAccount("acc_alpha")
	if err != nil {
		t.Fatalf("failed to add account: %v", err)
	}

	active := s.GetActiveAccount()
	if active != "acc_alpha" {
		t.Errorf("expected active 'acc_alpha', got '%s'", active)
	}

	// Rename
	err = s.RenameAccount("acc_alpha", "acc_beta")
	if err != nil {
		t.Fatalf("failed to rename account: %v", err)
	}

	active = s.GetActiveAccount()
	if active != "acc_beta" {
		t.Errorf("expected active 'acc_beta', got '%s'", active)
	}

	// Delete
	err = s.DeleteAccount("acc_beta")
	if err != nil {
		t.Fatalf("failed to delete account: %v", err)
	}
}

func TestExtractGroupQuotas(t *testing.T) {
	// Test nil summary
	gPct, cPct := store.ExtractGroupQuotas(nil)
	if gPct != -1 || cPct != -1 {
		t.Errorf("expected -1, -1 for nil summary, got %f, %f", gPct, cPct)
	}

	// Test populated summary
	summary := &model.QuotaSummary{
		Groups: []model.QuotaGroup{
			{
				DisplayName: "Gemini 1.5 Pro & Flash",
				Buckets: []model.QuotaBucket{
					{
						BucketID:          "gemini_weekly",
						Window:            "weekly",
						RemainingFraction: 0.85,
					},
				},
			},
			{
				DisplayName: "Claude 3.5 Sonnet & GPT-4o 3P",
				Buckets: []model.QuotaBucket{
					{
						BucketID:          "claude_weekly",
						Window:            "weekly",
						RemainingFraction: 0.60,
					},
				},
			},
		},
	}

	gPct, cPct = store.ExtractGroupQuotas(summary)
	if gPct != 85.0 {
		t.Errorf("expected Gemini quota pct 85.0, got %f", gPct)
	}
	if cPct != 60.0 {
		t.Errorf("expected Claude quota pct 60.0, got %f", cPct)
	}
}

func TestExtractDetailedModelBuckets(t *testing.T) {
	// Test nil summary
	details := store.ExtractDetailedModelBuckets(nil)
	if details != nil {
		t.Errorf("expected nil for nil summary, got %v", details)
	}

	futureReset := time.Now().Add(25 * time.Hour).Format(time.RFC3339)
	summary := &model.QuotaSummary{
		Groups: []model.QuotaGroup{
			{
				DisplayName: "Gemini Models",
				Buckets: []model.QuotaBucket{
					{
						BucketID:          "gemini_weekly_bucket",
						DisplayName:       "Gemini 1.5 Pro",
						Window:            "", // Test empty window fallback to BucketID containing "weekly"
						ResetTime:         futureReset,
						RemainingFraction: 0.0, // Throttled
					},
					{
						BucketID:          "gemini_daily_bucket",
						DisplayName:       "Gemini Flash",
						Window:            "daily",
						ResetTime:         time.Now().Add(2 * time.Hour).Format(time.RFC3339),
						RemainingFraction: 0.50,
					},
				},
			},
		},
	}

	details = store.ExtractDetailedModelBuckets(summary)
	if len(details) != 2 {
		t.Fatalf("expected 2 bucket details, got %d", len(details))
	}

	// Bucket 1 checks
	b1 := details[0]
	if b1.BucketID != "gemini_weekly_bucket" {
		t.Errorf("expected BucketID gemini_weekly_bucket, got %s", b1.BucketID)
	}
	if b1.WindowType != "weekly" {
		t.Errorf("expected WindowType weekly, got %s", b1.WindowType)
	}
	if !b1.IsThrottled {
		t.Errorf("expected IsThrottled true for 0 remaining fraction")
	}
	if !strings.Contains(b1.ResetMessage, "Refreshes in 1d") {
		t.Errorf("expected reset message to contain 'Refreshes in 1d', got %s", b1.ResetMessage)
	}

	// Bucket 2 checks
	b2 := details[1]
	if b2.WindowType != "daily" {
		t.Errorf("expected WindowType daily, got %s", b2.WindowType)
	}
	if b2.IsThrottled {
		t.Errorf("expected IsThrottled false for 0.5 remaining fraction")
	}
	if b2.RemainingPct != 50.0 {
		t.Errorf("expected RemainingPct 50.0, got %f", b2.RemainingPct)
	}
}

func TestStore_SyncActiveAccountCredentials(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "store_sync_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	v := vault.NewVault(tempDir)
	s := store.NewStore(tempDir, v)

	accName := "active_test_user"
	if err := s.SaveAccountRegistry([]string{accName}); err != nil {
		t.Fatalf("failed to save registry: %v", err)
	}

	// Set active_account.txt directly
	primaryDir := filepath.Join(tempDir, ".gemini")
	_ = os.MkdirAll(primaryDir, 0755)
	_ = os.WriteFile(filepath.Join(primaryDir, "active_account.txt"), []byte(accName), 0644)

	// Simulate login directly in primary dir (~/.gemini)
	primaryCliDir := filepath.Join(primaryDir, "antigravity-cli")
	_ = os.MkdirAll(primaryCliDir, 0755)
	fakeTokenJSON := `{"token":{"access_token":"ya29.testsyncactive12345"}}`
	_ = os.WriteFile(filepath.Join(primaryCliDir, "antigravity-oauth-token"), []byte(fakeTokenJSON), 0600)

	// Ensure ~/.gemini_<accName> has NO token yet
	accDir := s.GetAccountDirectory(accName)
	_ = os.MkdirAll(accDir, 0755)

	// Call ListAccountsFast - should detect token and sync to accDir
	accs := s.ListAccountsFast()
	if len(accs) != 1 {
		t.Fatalf("expected 1 account, got %d", len(accs))
	}

	if !accs[0].IsLoggedIn {
		t.Errorf("expected account to be logged in, got false")
	}

	// Verify token was synced to account directory
	accTokFile := filepath.Join(accDir, "antigravity-cli", "antigravity-oauth-token")
	if _, err := os.Stat(accTokFile); err != nil {
		t.Errorf("expected token to be synced to %s, but file does not exist", accTokFile)
	}
}

func TestStore_ResolveAccount(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "store_resolve_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	v := vault.NewVault(tempDir)
	s := store.NewStore(tempDir, v)

	accounts := []string{"fptvttnhon2020", "fptvttnhon2026", "nhontruongvo", "nhontruongvo3", "vothuongtruongnhon2002"}
	_ = s.SaveAccountRegistry(accounts)

	// 1. Exact match
	if res := s.ResolveAccount("fptvttnhon2026"); res != "fptvttnhon2026" {
		t.Errorf("expected 'fptvttnhon2026', got '%s'", res)
	}

	// 2. Case insensitive
	if res := s.ResolveAccount("FptVttNhon2026"); res != "fptvttnhon2026" {
		t.Errorf("expected 'fptvttnhon2026', got '%s'", res)
	}

	// 3. Substring token: "fp2026" -> "fptvttnhon2026"
	if res := s.ResolveAccount("fp2026"); res != "fptvttnhon2026" {
		t.Errorf("expected 'fptvttnhon2026' for 'fp2026', got '%s'", res)
	}

	// 4. Year token: "2026" -> "fptvttnhon2026"
	if res := s.ResolveAccount("2026"); res != "fptvttnhon2026" {
		t.Errorf("expected 'fptvttnhon2026' for '2026', got '%s'", res)
	}

	// 5. Unique token: "2002" -> "vothuongtruongnhon2002"
	if res := s.ResolveAccount("2002"); res != "vothuongtruongnhon2002" {
		t.Errorf("expected 'vothuongtruongnhon2002' for '2002', got '%s'", res)
	}
}

func TestStore_LogoutAccount(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "store_logout_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	v := vault.NewVault(tempDir)
	s := store.NewStore(tempDir, v)

	accName := "test_logout_user"
	_ = s.AddAccount(accName)
	_ = s.SetActiveAccount(accName)

	// Create fake tokens in accDir and primaryDir
	accDir := s.GetAccountDirectory(accName)
	_ = os.MkdirAll(filepath.Join(accDir, "antigravity-cli"), 0755)
	_ = os.WriteFile(filepath.Join(accDir, "antigravity-cli", "antigravity-oauth-token"), []byte("ya29.testtoken"), 0600)

	primaryDir := filepath.Join(tempDir, ".gemini")
	_ = os.MkdirAll(filepath.Join(primaryDir, "antigravity-cli"), 0755)
	_ = os.WriteFile(filepath.Join(primaryDir, "antigravity-cli", "antigravity-oauth-token"), []byte("ya29.testtoken"), 0600)

	// Perform logout
	if err := s.LogoutAccount(accName); err != nil {
		t.Fatalf("LogoutAccount failed: %v", err)
	}

	// Verify tokens purged in accDir
	if s.Vault.ReadTokenFromDir(accDir) != "" {
		t.Errorf("expected accDir token to be empty after logout")
	}

	// Verify tokens purged in primaryDir
	if s.Vault.ReadTokenFromDir(primaryDir) != "" {
		t.Errorf("expected primaryDir token to be empty after active logout")
	}
}

func TestStore_DeleteAccountRobust(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "store_del_robust_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	v := vault.NewVault(tempDir)
	s := store.NewStore(tempDir, v)

	_ = s.SaveAccountRegistry([]string{"user_alpha", "user_beta"})
	_ = s.SetActiveAccount("user_beta")

	betaDir := s.GetAccountDirectory("user_beta")
	_ = os.MkdirAll(betaDir, 0755)
	if _, err := os.Stat(betaDir); os.IsNotExist(err) {
		t.Fatalf("expected user_beta directory to exist")
	}

	// Delete active account user_beta
	if err := s.DeleteAccount("user_beta"); err != nil {
		t.Fatalf("DeleteAccount failed: %v", err)
	}

	// 1. Directory must be completely removed from disk
	if _, err := os.Stat(betaDir); !os.IsNotExist(err) {
		t.Errorf("expected betaDir to be removed from disk, but it still exists")
	}

	// 2. Account must be removed from registry
	names := s.LoadAccountRegistry()
	for _, n := range names {
		if n == "user_beta" {
			t.Errorf("expected user_beta to be removed from registry")
		}
	}

	// 3. Active account must have safely transitioned to user_alpha
	active := s.GetActiveAccount()
	if active != "user_alpha" {
		t.Errorf("expected active account to transition to 'user_alpha', got '%s'", active)
	}
}

func TestStore_MatchesAccountEmail_CrossPollinationPrevention(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "store_email_match_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	v := vault.NewVault(tempDir)
	s := store.NewStore(tempDir, v)

	// Directory has token belonging to user_beta@gmail.com
	betaJWT := "eyJhbGciOiJSUzI1NiJ9.eyJlbWFpbCI6InVzZXJfYmV0YUBnbWFpbC5jb20ifQ.fakesig"
	tokJSON := `{"token":{"access_token":"ya29.beta"},"id_token":"` + betaJWT + `"}`

	accDir := filepath.Join(tempDir, ".gemini_user_alpha")
	cliDir := filepath.Join(accDir, "antigravity-cli")
	_ = os.MkdirAll(cliDir, 0755)
	_ = os.WriteFile(filepath.Join(cliDir, "antigravity-oauth-token"), []byte(tokJSON), 0600)

	// When checking user_alpha, MatchesAccountEmail must return false because token actually belongs to user_beta
	if s.MatchesAccountEmail(accDir, "user_alpha") {
		t.Errorf("expected MatchesAccountEmail to return false for mismatched token email")
	}

	// When checking user_beta, it should match
	if !s.MatchesAccountEmail(accDir, "user_beta") {
		t.Errorf("expected MatchesAccountEmail to return true for matching token email")
	}
}


