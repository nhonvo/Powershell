package workspace

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"sync"
)

type ProjectItem struct {
	ID           string  `json:"id"`
	Name         string  `json:"name"`
	Path         string  `json:"path"`
	Stack        string  `json:"stack"`
	GitBranch    string  `json:"git_branch"`
	IsGit        bool    `json:"is_git"`
	IsDirty      bool    `json:"is_dirty"`
	DirtyCount   int     `json:"dirty_count"`
	IsPinned     bool    `json:"is_pinned"`
	IsActive     bool    `json:"is_active"`
	LastModified string  `json:"last_modified"`
	SessionCount int     `json:"session_count"`
	TotalCost    float64 `json:"total_cost"`
}

type ProjectsRegistry struct {
	ActiveProjectID string        `json:"active_project_id"`
	DefaultIDE      string        `json:"default_ide"`
	ScanRoots       []string      `json:"scan_roots"`
	Projects        []ProjectItem `json:"projects"`
}

type FileNode struct {
	Name     string `json:"name"`
	Path     string `json:"path"`
	IsDir    bool   `json:"is_dir"`
	Size     int64  `json:"size"`
	Modified string `json:"modified"`
}

type WorkspaceManager struct {
	mu              sync.RWMutex
	userWorkspaces  map[int64]string
	defaultWorkspace string
	registryPath    string
	settingsPath    string
}

func NewWorkspaceManager(defaultWorkspace string) *WorkspaceManager {
	home, _ := os.UserHomeDir()
	return &WorkspaceManager{
		userWorkspaces:   make(map[int64]string),
		defaultWorkspace: defaultWorkspace,
		registryPath:     filepath.Join(home, ".config", "antigravity", "projects.json"),
		settingsPath:     filepath.Join(home, ".gemini", "antigravity-cli", "settings.json"),
	}
}

// GetRegistry reads projects registered in agyproj
func (w *WorkspaceManager) GetRegistry() (*ProjectsRegistry, error) {
	data, err := os.ReadFile(w.registryPath)
	if err != nil {
		return nil, err
	}
	var reg ProjectsRegistry
	if err := json.Unmarshal(data, &reg); err != nil {
		return nil, err
	}
	return &reg, nil
}

// ListProjects returns all known projects from agyproj registry and settings.json
func (w *WorkspaceManager) ListProjects() []ProjectItem {
	var results []ProjectItem
	seen := make(map[string]bool)

	// 1. Read agyproj registry
	if reg, err := w.GetRegistry(); err == nil {
		for _, p := range reg.Projects {
			clean := filepath.Clean(p.Path)
			if !seen[clean] {
				seen[clean] = true
				if reg.ActiveProjectID == p.ID {
					p.IsActive = true
				}
				results = append(results, p)
			}
		}
	}

	// 2. Read trustedWorkspaces from antigravity settings
	if data, err := os.ReadFile(w.settingsPath); err == nil {
		var settings struct {
			TrustedWorkspaces []string `json:"trustedWorkspaces"`
		}
		if json.Unmarshal(data, &settings) == nil {
			for _, ws := range settings.TrustedWorkspaces {
				clean := filepath.Clean(ws)
				if !seen[clean] && isDir(clean) {
					seen[clean] = true
					results = append(results, ProjectItem{
						ID:   filepath.Base(clean),
						Name: filepath.Base(clean),
						Path: clean,
					})
				}
			}
		}
	}

	// 3. Include default workspace if missing
	if w.defaultWorkspace != "" {
		clean := filepath.Clean(w.defaultWorkspace)
		if !seen[clean] && isDir(clean) {
			seen[clean] = true
			results = append(results, ProjectItem{
				ID:       filepath.Base(clean),
				Name:     filepath.Base(clean),
				Path:     clean,
				IsActive: true,
			})
		}
	}

	return results
}

// GetUserWorkspace retrieves the active workspace path for a specific user ID
func (w *WorkspaceManager) GetUserWorkspace(userID int64) string {
	w.mu.RLock()
	defer w.mu.RUnlock()

	if ws, ok := w.userWorkspaces[userID]; ok && isDir(ws) {
		return ws
	}

	// Default fallback: Check active project from agyproj registry
	if reg, err := w.GetRegistry(); err == nil {
		for _, p := range reg.Projects {
			if p.ID == reg.ActiveProjectID && isDir(p.Path) {
				return p.Path
			}
		}
	}

	if w.defaultWorkspace != "" && isDir(w.defaultWorkspace) {
		return w.defaultWorkspace
	}

	cwd, _ := os.Getwd()
	return cwd
}

// SetUserWorkspace updates the active workspace for a user by path or project ID
func (w *WorkspaceManager) SetUserWorkspace(userID int64, pathOrID string) (string, error) {
	clean := strings.TrimSpace(pathOrID)
	if clean == "" {
		return "", fmt.Errorf("path or project id cannot be empty")
	}

	// Check if matching project ID in agyproj registry
	projs := w.ListProjects()
	for _, p := range projs {
		if strings.EqualFold(p.ID, clean) || strings.EqualFold(p.Name, clean) {
			if isDir(p.Path) {
				w.mu.Lock()
				w.userWorkspaces[userID] = p.Path
				w.mu.Unlock()
				return p.Path, nil
			}
		}
	}

	// Otherwise treat as directory path
	absPath, err := filepath.Abs(clean)
	if err != nil || !isDir(absPath) {
		return "", fmt.Errorf("directory does not exist: %s", clean)
	}

	w.mu.Lock()
	w.userWorkspaces[userID] = absPath
	w.mu.Unlock()
	return absPath, nil
}

// ListFiles lists directory contents of a workspace subfolder
func (w *WorkspaceManager) ListFiles(workspaceDir string, subpath string) ([]FileNode, error) {
	target := filepath.Join(workspaceDir, subpath)
	entries, err := os.ReadDir(target)
	if err != nil {
		return nil, err
	}

	var nodes []FileNode
	for _, entry := range entries {
		info, err := entry.Info()
		if err != nil {
			continue
		}
		nodes = append(nodes, FileNode{
			Name:     entry.Name(),
			Path:     filepath.Join(subpath, entry.Name()),
			IsDir:    entry.IsDir(),
			Size:     info.Size(),
			Modified: info.ModTime().Format("2006-01-02 15:04:05"),
		})
	}
	return nodes, nil
}

// ReadFileContent securely reads text files within the workspace limit
func (w *WorkspaceManager) ReadFileContent(workspaceDir string, relativePath string, maxBytes int64) (string, error) {
	if maxBytes <= 0 {
		maxBytes = 32 * 1024 // 32KB default
	}

	fullPath := filepath.Join(workspaceDir, relativePath)
	f, err := os.Open(fullPath)
	if err != nil {
		return "", err
	}
	defer f.Close()

	buf := make([]byte, maxBytes)
	n, err := f.Read(buf)
	if err != nil && n == 0 {
		return "", err
	}

	return string(buf[:n]), nil
}

func isDir(p string) bool {
	fi, err := os.Stat(p)
	return err == nil && fi.IsDir()
}
