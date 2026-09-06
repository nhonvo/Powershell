package store_test

import (
	"os"
	"path/filepath"
	"testing"

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
