package rules_test

import (
	"os"
	"path/filepath"
	"testing"

	"agyswitch/internal/service/rules"
)

func TestRules_DiscoverAndMCPParse(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "rules_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	m := rules.NewManager(tempDir)

	// Create global rule
	rulePath := filepath.Join(tempDir, ".gemini", "config", "rules", "code_style.md")
	_ = os.MkdirAll(filepath.Dir(rulePath), 0755)
	_ = os.WriteFile(rulePath, []byte("# Code Style Rules"), 0644)

	rulesList, err := m.DiscoverRules("")
	if err != nil {
		t.Fatalf("failed to discover rules: %v", err)
	}

	if len(rulesList) != 1 {
		t.Fatalf("expected 1 rule, got %d", len(rulesList))
	}

	if rulesList[0].Name != "code_style.md" {
		t.Errorf("expected rule 'code_style.md', got '%s'", rulesList[0].Name)
	}

	// MCP Test
	mcpPath := filepath.Join(tempDir, "mcp_config.json")
	_ = os.WriteFile(mcpPath, []byte(`{"mcpServers":{"git":{"command":"git-mcp"}}}`), 0644)

	parsed, err := rules.ParseMCPConfig(mcpPath)
	if err != nil {
		t.Fatalf("failed to parse mcp config: %v", err)
	}
	if parsed == nil || parsed["mcpServers"] == nil {
		t.Errorf("expected valid mcpServers map")
	}
}
