package skills_test

import (
	"os"
	"path/filepath"
	"testing"

	"agyswitch/internal/service/skills"
)

func TestSkills_DiscoverAndSync(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "skills_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	m := skills.NewManager(tempDir)

	// Create dummy skill
	skillDir := filepath.Join(tempDir, ".gemini", "skills", "test-skill")
	_ = os.MkdirAll(skillDir, 0755)
	_ = os.WriteFile(filepath.Join(skillDir, "SKILL.md"), []byte("---\nname: test-skill\ndescription: Test Antigravity Skill\n---\n"), 0644)

	skillsList, err := m.DiscoverSkills("")
	if err != nil {
		t.Fatalf("failed to discover skills: %v", err)
	}

	if len(skillsList) != 1 {
		t.Fatalf("expected 1 skill, got %d", len(skillsList))
	}

	if skillsList[0].Name != "test-skill" {
		t.Errorf("expected skill name 'test-skill', got '%s'", skillsList[0].Name)
	}

	// Sync test
	accDir := filepath.Join(tempDir, ".gemini_acc1")
	synced, err := m.SyncSkills([]string{accDir})
	if err != nil {
		t.Fatalf("failed to sync skills: %v", err)
	}
	if synced != 1 {
		t.Errorf("expected 1 synced dir, got %d", synced)
	}

	if _, err := os.Stat(filepath.Join(accDir, "skills", "test-skill", "SKILL.md")); os.IsNotExist(err) {
		t.Errorf("synced skill file does not exist in account directory")
	}
}
