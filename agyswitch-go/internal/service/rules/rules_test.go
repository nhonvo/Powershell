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

func TestCheckMCPServerStatus(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "rules_mcp_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	m := rules.NewManager(tempDir)

	// 1. Check with custom config file path
	mcpPath := filepath.Join(tempDir, "mcp_config.json")
	mcpContent := `{
		"mcpServers": {
			"git": {
				"command": "git-mcp"
			},
			"invalid_server": {
				"command": ""
			}
		}
	}`
	_ = os.WriteFile(mcpPath, []byte(mcpContent), 0644)

	statuses, err := m.CheckMCPServerStatus(mcpPath)
	if err != nil {
		t.Fatalf("CheckMCPServerStatus failed: %v", err)
	}

	if len(statuses) != 2 {
		t.Fatalf("expected 2 server statuses, got %d", len(statuses))
	}

	for _, st := range statuses {
		if st.ServerName == "git" {
			if !st.IsRunning || st.Command != "git-mcp" {
				t.Errorf("git server status mismatch: %+v", st)
			}
		} else if st.ServerName == "invalid_server" {
			if st.IsRunning || st.LastError != "No command specified" {
				t.Errorf("invalid_server status mismatch: %+v", st)
			}
		}
	}

	// 2. Check default fallback path ~/.gemini/config/mcp_config.json
	defaultPath := filepath.Join(tempDir, ".gemini", "config", "mcp_config.json")
	_ = os.MkdirAll(filepath.Dir(defaultPath), 0755)
	_ = os.WriteFile(defaultPath, []byte(`{"mcpServers":{"default_srv":{"command":"srv_cmd"}}}`), 0644)

	defaultStatuses, err := m.CheckMCPServerStatus("")
	if err != nil {
		t.Fatalf("CheckMCPServerStatus with empty path failed: %v", err)
	}

	if len(defaultStatuses) != 1 || defaultStatuses[0].ServerName != "default_srv" {
		t.Errorf("expected default_srv, got %+v", defaultStatuses)
	}

	// 3. Non-existent file path returns error
	_, err = m.CheckMCPServerStatus(filepath.Join(tempDir, "nonexistent.json"))
	if err == nil {
		t.Errorf("expected error for nonexistent file path")
	}
}

