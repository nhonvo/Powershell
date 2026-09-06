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
	err = s.AddAccount("test_acc1")
	if err != nil {
		t.Fatalf("failed to add account: %v", err)
	}

	active := s.GetActiveAccount()
	if active != "test_acc1" {
		t.Errorf("expected active 'test_acc1', got '%s'", active)
	}

	// Rename
	err = s.RenameAccount("test_acc1", "test_acc2")
	if err != nil {
		t.Fatalf("failed to rename account: %v", err)
	}

	active = s.GetActiveAccount()
	if active != "test_acc2" {
		t.Errorf("expected active 'test_acc2', got '%s'", active)
	}

	// Delete
	err = s.DeleteAccount("test_acc2")
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

