package registry_test

import (
	"os"
	"path/filepath"
	"testing"

	"agyproj/internal/service/registry"
)

func TestRegistry_CRUD(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "registry_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	proj1 := filepath.Join(tempDir, "projects", "my-project-1")
	proj2 := filepath.Join(tempDir, "projects", "my-project-2")
	_ = os.MkdirAll(proj1, 0755)
	_ = os.MkdirAll(proj2, 0755)

	m := registry.NewManager(tempDir)

	// 1. Register project 1
	info, err := m.Register(proj1, true)
	if err != nil {
		t.Fatalf("failed to register proj1: %v", err)
	}
	if !info.IsPinned {
		t.Errorf("expected info.IsPinned to be true")
	}

	// 2. Register project 2
	_, err = m.Register(proj2, false)
	if err != nil {
		t.Fatalf("failed to register proj2: %v", err)
	}

	list := m.ListRegistered()
	if len(list) != 2 {
		t.Fatalf("expected 2 registered projects, got %d", len(list))
	}

	// 3. Toggle Pin on proj2
	newStatus, err := m.TogglePin("my-project-2")
	if err != nil || !newStatus {
		t.Fatalf("failed to toggle pin: status=%v, err=%v", newStatus, err)
	}

	// 4. Set Active
	err = m.SetActive("my-project-2")
	if err != nil {
		t.Fatalf("failed to set active: %v", err)
	}

	// 5. Unregister
	err = m.Unregister("my-project-1")
	if err != nil {
		t.Fatalf("failed to unregister: %v", err)
	}

	after := m.ListRegistered()
	if len(after) != 1 {
		t.Fatalf("expected 1 remaining project, got %d", len(after))
	}
}

func TestRegistry_ScanDirectory(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "registry_scan_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	root := filepath.Join(tempDir, "projects")
	_ = os.MkdirAll(filepath.Join(root, "app-alpha"), 0755)
	_ = os.MkdirAll(filepath.Join(root, "app-beta"), 0755)

	m := registry.NewManager(tempDir)
	discovered, err := m.ScanDirectory(root)
	if err != nil {
		t.Fatalf("failed to scan: %v", err)
	}
	if len(discovered) != 2 {
		t.Fatalf("expected 2 discovered projects, got %d", len(discovered))
	}

	// Test RegisterAll
	count, err := m.RegisterAll(root)
	if err != nil || count != 2 {
		t.Fatalf("expected 2 registered, got %d (err: %v)", count, err)
	}

	list := m.ListRegistered()
	if len(list) != 2 {
		t.Fatalf("expected 2 list registered, got %d", len(list))
	}
}
