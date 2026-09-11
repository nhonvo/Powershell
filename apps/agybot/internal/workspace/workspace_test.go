package workspace

import (
	"os"
	"path/filepath"
	"testing"
)

func TestWorkspaceManager_ListAndSet(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "agybot_test_ws")
	if err != nil {
		t.Fatalf("Failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	subDir := filepath.Join(tempDir, "subdir")
	_ = os.Mkdir(subDir, 0755)
	testFile := filepath.Join(tempDir, "hello.txt")
	_ = os.WriteFile(testFile, []byte("hello antigravity"), 0644)

	mgr := NewWorkspaceManager(tempDir)
	ws := mgr.GetUserWorkspace(1001)
	if ws == "" {
		t.Errorf("Expected non-empty default workspace")
	}

	// Test SetUserWorkspace
	newWs, err := mgr.SetUserWorkspace(1001, subDir)
	if err != nil || newWs != subDir {
		t.Errorf("Expected workspace set to subDir, got %s (err: %v)", newWs, err)
	}

	// Test ListFiles
	files, err := mgr.ListFiles(tempDir, "")
	if err != nil || len(files) < 2 {
		t.Errorf("Expected at least 2 entries in files, got %d (err: %v)", len(files), err)
	}

	// Test ReadFileContent
	content, err := mgr.ReadFileContent(tempDir, "hello.txt", 1024)
	if err != nil || content != "hello antigravity" {
		t.Errorf("Expected content 'hello antigravity', got '%s'", content)
	}
}
