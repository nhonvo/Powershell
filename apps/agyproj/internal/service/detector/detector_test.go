package detector_test

import (
	"os"
	"path/filepath"
	"testing"

	"agyproj/internal/service/detector"
)

func TestDetector_Analyze(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "detector_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	// Create dummy files simulating .NET and React
	_ = os.WriteFile(filepath.Join(tempDir, "MyApp.sln"), []byte(""), 0644)
	_ = os.WriteFile(filepath.Join(tempDir, "package.json"), []byte("{\"dependencies\": {\"react\": \"^18.0.0\"}}"), 0644)

	// Initialize dummy .git
	gitDir := filepath.Join(tempDir, ".git")
	_ = os.MkdirAll(gitDir, 0755)
	_ = os.WriteFile(filepath.Join(gitDir, "HEAD"), []byte("ref: refs/heads/feature/auth\n"), 0644)

	d := detector.NewDetector(tempDir)
	info := d.Analyze(tempDir)

	if info.GitBranch != "feature/auth" {
		t.Errorf("expected branch feature/auth, got %s", info.GitBranch)
	}

	if !info.IsGit {
		t.Errorf("expected isGit to be true")
	}

	if info.Stack != ".NET / C# · React" {
		t.Errorf("expected '.NET / C# · React', got '%s'", info.Stack)
	}
}
