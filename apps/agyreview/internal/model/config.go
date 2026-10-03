package model

import (
	"encoding/json"
	"os"
	"path/filepath"
)

type WatchItem struct {
	Path        string      `json:"path"`
	TriggerMode TriggerMode `json:"trigger_mode"` // "auto" or "suggest"
	LastChecked string      `json:"last_checked,omitempty"`
	LastCommit  string      `json:"last_commit,omitempty"`
	LastBranch  string      `json:"last_branch,omitempty"`
}

type AppConfig struct {
	WatchedRepos     []WatchItem `json:"watched_repos"`
	PollIntervalSec  int         `json:"poll_interval_sec"`
	AutoApproveP0P1  bool        `json:"auto_approve_p0_p1"`
	ThreeLoops       bool        `json:"three_loops"`
	SwarmConcurrency int         `json:"swarm_concurrency"`
	ReportDir        string      `json:"report_dir"`
	DefaultTrigger   TriggerMode `json:"default_trigger"`
}

func DefaultConfig() *AppConfig {
	return &AppConfig{
		WatchedRepos:     make([]WatchItem, 0),
		PollIntervalSec:  30,
		AutoApproveP0P1:  false,
		ThreeLoops:       true,
		SwarmConcurrency: 4,
		ReportDir:        "doc/audit",
		DefaultTrigger:   TriggerSuggest,
	}
}

func ConfigFilePath() (string, error) {
	home, err := os.UserHomeDir()
	if err != nil {
		return "", err
	}
	dir := filepath.Join(home, ".config", "antigravity")
	if err := os.MkdirAll(dir, 0755); err != nil {
		return "", err
	}
	return filepath.Join(dir, "agyreview.json"), nil
}

func LoadConfig() (*AppConfig, error) {
	path, err := ConfigFilePath()
	if err != nil {
		return DefaultConfig(), err
	}
	data, err := os.ReadFile(path)
	if os.IsNotExist(err) {
		cfg := DefaultConfig()
		_ = SaveConfig(cfg)
		return cfg, nil
	} else if err != nil {
		return DefaultConfig(), err
	}
	var cfg AppConfig
	if err := json.Unmarshal(data, &cfg); err != nil {
		return DefaultConfig(), nil
	}
	if cfg.PollIntervalSec <= 0 {
		cfg.PollIntervalSec = 30
	}
	if cfg.SwarmConcurrency <= 0 {
		cfg.SwarmConcurrency = 4
	}
	if cfg.ReportDir == "" {
		cfg.ReportDir = "doc/audit"
	}
	if cfg.DefaultTrigger == "" {
		cfg.DefaultTrigger = TriggerSuggest
	}
	return &cfg, nil
}

func SaveConfig(cfg *AppConfig) error {
	path, err := ConfigFilePath()
	if err != nil {
		return err
	}
	data, err := json.MarshalIndent(cfg, "", "  ")
	if err != nil {
		return err
	}
	return os.WriteFile(path, data, 0644)
}
