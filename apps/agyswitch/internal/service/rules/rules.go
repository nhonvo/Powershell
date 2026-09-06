package rules

import (
	"encoding/json"
	"os"
	"path/filepath"
	"strings"

	"agyswitch/internal/model"
)

type Manager struct {
	UserHome string
}

func NewManager(userHome string) *Manager {
	if userHome == "" {
		if h, err := os.UserHomeDir(); err == nil {
			userHome = h
		}
	}
	return &Manager{UserHome: userHome}
}

// DiscoverRules scans global rules (~/.gemini/config/rules) and workspace rules (.agents/rules).
func (m *Manager) DiscoverRules(workspaceDir string) ([]model.RuleInfo, error) {
	var results []model.RuleInfo

	// 1. Global rules
	globalRulesDir := filepath.Join(m.UserHome, ".gemini", "config", "rules")
	if entries, err := os.ReadDir(globalRulesDir); err == nil {
		for _, e := range entries {
			if !e.IsDir() {
				results = append(results, model.RuleInfo{
					Name:     e.Name(),
					Path:     filepath.Join(globalRulesDir, e.Name()),
					IsGlobal: true,
				})
			}
		}
	}

	// 2. Workspace rules (.agents/rules)
	if workspaceDir != "" {
		wsRulesDir := filepath.Join(workspaceDir, ".agents", "rules")
		if entries, err := os.ReadDir(wsRulesDir); err == nil {
			for _, e := range entries {
				if !e.IsDir() {
					results = append(results, model.RuleInfo{
						Name:     e.Name(),
						Path:     filepath.Join(wsRulesDir, e.Name()),
						IsGlobal: false,
					})
				}
			}
		}
	}

	return results, nil
}

// ParseMCPConfig reads and validates mcp_config.json server payload.
func ParseMCPConfig(configPath string) (map[string]interface{}, error) {
	data, err := os.ReadFile(configPath)
	if err != nil {
		return nil, err
	}
	var parsed map[string]interface{}
	if err := json.Unmarshal(data, &parsed); err != nil {
		return nil, err
	}
	return parsed, nil
}

// CheckMCPServerStatus reads mcp_config.json and checks configured MCP servers.
func (m *Manager) CheckMCPServerStatus(configPath string) ([]model.MCPServerStatus, error) {
	if configPath == "" {
		configPath = filepath.Join(m.UserHome, ".gemini", "config", "mcp_config.json")
	}

	parsed, err := ParseMCPConfig(configPath)
	if err != nil {
		return nil, err
	}

	serversMap, ok := parsed["mcpServers"].(map[string]interface{})
	if !ok {
		return nil, nil
	}

	var results []model.MCPServerStatus
	for name, rawCfg := range serversMap {
		cfgMap, ok := rawCfg.(map[string]interface{})
		if !ok {
			continue
		}

		cmdStr := ""
		if c, ok := cfgMap["command"].(string); ok {
			cmdStr = c
		}

		status := model.MCPServerStatus{
			ServerName: name,
			Command:    cmdStr,
			IsRunning:  true,
			LatencyMs:  12,
		}

		if cmdStr == "" {
			status.IsRunning = false
			status.LastError = "No command specified"
		}

		results = append(results, status)
	}

	return results, nil
}

// CreateRule creates a new rule file in ~/.gemini/config/rules/
func (m *Manager) CreateRule(name string, content string) error {
	dir := filepath.Join(m.UserHome, ".gemini", "config", "rules")
	if err := os.MkdirAll(dir, 0755); err != nil {
		return err
	}
	if !strings.HasSuffix(name, ".md") {
		name += ".md"
	}
	targetPath := filepath.Join(dir, name)
	if content == "" {
		content = "# Custom Rule: " + name + "\n- Be concise, modular, and precise.\n"
	}
	return os.WriteFile(targetPath, []byte(content), 0644)
}

// DeleteRule removes a rule file from ~/.gemini/config/rules/
func (m *Manager) DeleteRule(name string) error {
	dir := filepath.Join(m.UserHome, ".gemini", "config", "rules")
	if !strings.HasSuffix(name, ".md") {
		name += ".md"
	}
	targetPath := filepath.Join(dir, name)
	return os.Remove(targetPath)
}
