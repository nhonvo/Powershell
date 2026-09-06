package rules

import (
	"encoding/json"
	"os"
	"path/filepath"

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
