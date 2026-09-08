package registry

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"sort"
	"strings"

	"agyproj/internal/model"
	"agyproj/internal/service/detector"
)

type Manager struct {
	UserHome   string
	ConfigFile string
	Detector   *detector.Detector
}

func NewManager(userHome string) *Manager {
	if userHome == "" {
		if h, err := os.UserHomeDir(); err == nil {
			userHome = h
		}
	}
	cfgDir := filepath.Join(userHome, ".config", "antigravity")
	_ = os.MkdirAll(cfgDir, 0755)
	return &Manager{
		UserHome:   userHome,
		ConfigFile: filepath.Join(cfgDir, "projects.json"),
		Detector:   detector.NewDetector(userHome),
	}
}

// Load reads config from disk or returns default configuration.
func (m *Manager) Load() (*model.RegistryConfig, error) {
	data, err := os.ReadFile(m.ConfigFile)
	if err != nil {
		defaultRoots := []string{filepath.Join(m.UserHome, "projects")}
		cfg := &model.RegistryConfig{
			DefaultIDE: "code",
			ScanRoots:  defaultRoots,
			Projects:   nil,
		}
		return cfg, nil
	}

	var cfg model.RegistryConfig
	if err := json.Unmarshal(data, &cfg); err != nil {
		return nil, err
	}
	return &cfg, nil
}

// Save persists the registry config to disk.
func (m *Manager) Save(cfg *model.RegistryConfig) error {
	_ = os.MkdirAll(filepath.Dir(m.ConfigFile), 0755)
	data, err := json.MarshalIndent(cfg, "", "  ")
	if err != nil {
		return err
	}
	return os.WriteFile(m.ConfigFile, data, 0644)
}

// Register adds or updates a project in the registry.
func (m *Manager) Register(path string, isPinned bool) (*model.ProjectInfo, error) {
	absPath, err := filepath.Abs(path)
	if err != nil {
		absPath = filepath.Clean(path)
	}

	fi, err := os.Stat(absPath)
	if err != nil || !fi.IsDir() {
		return nil, fmt.Errorf("path '%s' is not a valid directory", path)
	}

	cfg, err := m.Load()
	if err != nil {
		return nil, err
	}

	info := m.Detector.Analyze(absPath)
	info.IsPinned = isPinned

	found := false
	for i, p := range cfg.Projects {
		if strings.EqualFold(p.Path, absPath) || strings.EqualFold(p.ID, info.ID) {
			info.IsPinned = p.IsPinned || isPinned
			info.IsActive = p.IsActive
			cfg.Projects[i] = info
			found = true
			break
		}
	}

	if !found {
		if len(cfg.Projects) == 0 {
			info.IsActive = true
			cfg.ActiveProjectID = info.ID
		}
		cfg.Projects = append(cfg.Projects, info)
	}

	if err := m.Save(cfg); err != nil {
		return nil, err
	}
	return &info, nil
}

// Unregister removes a project from the registry.
func (m *Manager) Unregister(projectID string) error {
	cfg, err := m.Load()
	if err != nil {
		return err
	}

	var updated []model.ProjectInfo
	for _, p := range cfg.Projects {
		if !strings.EqualFold(p.ID, projectID) && !strings.EqualFold(p.Name, projectID) {
			updated = append(updated, p)
		}
	}

	cfg.Projects = updated
	if strings.EqualFold(cfg.ActiveProjectID, projectID) {
		if len(cfg.Projects) > 0 {
			cfg.ActiveProjectID = cfg.Projects[0].ID
			cfg.Projects[0].IsActive = true
		} else {
			cfg.ActiveProjectID = ""
		}
	}

	return m.Save(cfg)
}

// TogglePin toggles the pinned status of a project.
func (m *Manager) TogglePin(projectID string) (bool, error) {
	cfg, err := m.Load()
	if err != nil {
		return false, err
	}

	newStatus := false
	for i, p := range cfg.Projects {
		if strings.EqualFold(p.ID, projectID) || strings.EqualFold(p.Name, projectID) {
			cfg.Projects[i].IsPinned = !cfg.Projects[i].IsPinned
			newStatus = cfg.Projects[i].IsPinned
			break
		}
	}

	return newStatus, m.Save(cfg)
}

// SetActive sets a project as active working context.
func (m *Manager) SetActive(projectID string) error {
	cfg, err := m.Load()
	if err != nil {
		return err
	}

	matched := false
	for i := range cfg.Projects {
		if strings.EqualFold(cfg.Projects[i].ID, projectID) || strings.EqualFold(cfg.Projects[i].Name, projectID) {
			cfg.Projects[i].IsActive = true
			cfg.ActiveProjectID = cfg.Projects[i].ID
			matched = true
		} else {
			cfg.Projects[i].IsActive = false
		}
	}

	if !matched {
		return fmt.Errorf("project '%s' not found in registry", projectID)
	}

	return m.Save(cfg)
}

// ListRegistered returns all registered projects refreshed with live git and session telemetry.
func (m *Manager) ListRegistered() []model.ProjectInfo {
	cfg, err := m.Load()
	if err != nil || len(cfg.Projects) == 0 {
		return nil
	}

	var list []model.ProjectInfo
	for _, p := range cfg.Projects {
		live := m.Detector.Analyze(p.Path)
		live.IsPinned = p.IsPinned
		live.IsActive = strings.EqualFold(live.ID, cfg.ActiveProjectID) || p.IsActive
		list = append(list, live)
	}

	// Sort pinned first, then by LastModified descending
	sort.Slice(list, func(i, j int) bool {
		if list[i].IsPinned != list[j].IsPinned {
			return list[i].IsPinned
		}
		return list[i].LastModified.After(list[j].LastModified)
	})

	return list
}

// ScanDirectory discovers immediate subdirectories under root and matches them against the registry.
func (m *Manager) ScanDirectory(root string) ([]model.ProjectInfo, error) {
	if root == "" {
		root = filepath.Join(m.UserHome, "projects")
	}

	entries, err := os.ReadDir(root)
	if err != nil {
		return nil, err
	}

	cfg, _ := m.Load()
	regMap := make(map[string]bool)
	pinMap := make(map[string]bool)
	if cfg != nil {
		for _, p := range cfg.Projects {
			regMap[strings.ToLower(p.Path)] = true
			pinMap[strings.ToLower(p.Path)] = p.IsPinned
		}
	}

	var results []model.ProjectInfo
	for _, e := range entries {
		if e.IsDir() && !strings.HasPrefix(e.Name(), ".") && e.Name() != "logs" && e.Name() != "archive" {
			subPath := filepath.Join(root, e.Name())
			info := m.Detector.Analyze(subPath)
			low := strings.ToLower(subPath)
			if regMap[low] {
				info.IsPinned = pinMap[low]
				info.IsActive = strings.EqualFold(info.ID, cfg.ActiveProjectID)
			}
			results = append(results, info)
		}
	}

	// Sort: Registered / Pinned first, then by name
	sort.Slice(results, func(i, j int) bool {
		lowI := strings.ToLower(results[i].Path)
		lowJ := strings.ToLower(results[j].Path)
		if regMap[lowI] != regMap[lowJ] {
			return regMap[lowI]
		}
		return results[i].Name < results[j].Name
	})

	return results, nil
}

// RegisterAll scans a root directory and registers all valid project folders.
func (m *Manager) RegisterAll(root string) (int, error) {
	discovered, err := m.ScanDirectory(root)
	if err != nil {
		return 0, err
	}

	count := 0
	for _, p := range discovered {
		if _, err := m.Register(p.Path, false); err == nil {
			count++
		}
	}
	return count, nil
}

// SetDefaultIDE updates the preferred default editor.
func (m *Manager) SetDefaultIDE(ide string) error {
	cfg, err := m.Load()
	if err != nil {
		return err
	}
	cfg.DefaultIDE = ide
	return m.Save(cfg)
}
