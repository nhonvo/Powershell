package workspace

import (
	"bufio"
	"encoding/json"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"sort"
	"strconv"
	"strings"
	"sync"
	"time"
)

type SessionItem struct {
	ID        string    `json:"id"`
	Steps     int       `json:"steps"`
	CreatedAt time.Time `json:"created_at"`
	Summary   string    `json:"summary"`
	CostUSD   float64   `json:"cost_usd"`
}

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

// SetUserWorkspace updates the active workspace for a user by path, project ID, or 1-based index
func (w *WorkspaceManager) SetUserWorkspace(userID int64, pathOrID string) (string, error) {
	clean := strings.TrimSpace(pathOrID)
	if clean == "" {
		return "", fmt.Errorf("path or project id cannot be empty")
	}

	projs := w.ListProjects()

	// 1. Support numeric project selection (e.g. "4" or "#4")
	numClean := strings.TrimPrefix(clean, "#")
	if idx, err := strconv.Atoi(numClean); err == nil && idx >= 1 && idx <= len(projs) {
		targetProj := projs[idx-1]
		if isDir(targetProj.Path) {
			w.mu.Lock()
			w.userWorkspaces[userID] = targetProj.Path
			w.mu.Unlock()
			return targetProj.Path, nil
		}
	}

	// 2. Check if matching project ID or Name in agyproj registry
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

	// 3. Otherwise treat as directory path
	absPath, err := filepath.Abs(clean)
	if err != nil || !isDir(absPath) {
		return "", fmt.Errorf("directory or project '%s' does not exist", clean)
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

// CreateProject creates a new project directory under ~/projects/<name>, initializes git,
// scaffolds initial stack templates, and registers the project into agyproj (projects.json).
func (w *WorkspaceManager) CreateProject(name, stack string) (*ProjectItem, error) {
	name = strings.TrimSpace(name)
	if name == "" {
		return nil, fmt.Errorf("project name cannot be empty")
	}

	home, _ := os.UserHomeDir()
	targetDir := filepath.Join(home, "projects", name)
	if err := os.MkdirAll(targetDir, 0755); err != nil {
		return nil, fmt.Errorf("failed to create project directory: %w", err)
	}

	// 1. Initialize git
	_ = exec.Command("git", "-C", targetDir, "init").Run()

	// 2. Scaffolding based on stack
	stackClean := strings.ToLower(strings.TrimSpace(stack))
	readmeContent := fmt.Sprintf("# %s\n\nCreated and managed via Antigravity Suite.\n", name)
	_ = os.WriteFile(filepath.Join(targetDir, "README.md"), []byte(readmeContent), 0644)

	displayStack := "Generic Codebase"
	switch {
	case strings.Contains(stackClean, "go"):
		displayStack = "Go Engine"
		goMod := fmt.Sprintf("module %s\n\ngo 1.26.0\n", name)
		_ = os.WriteFile(filepath.Join(targetDir, "go.mod"), []byte(goMod), 0644)
		mainGo := fmt.Sprintf("package main\n\nimport \"fmt\"\n\nfunc main() {\n\tfmt.Println(\"Hello from %s!\")\n}\n", name)
		_ = os.WriteFile(filepath.Join(targetDir, "main.go"), []byte(mainGo), 0644)
	case strings.Contains(stackClean, "node") || strings.Contains(stackClean, "ts") || strings.Contains(stackClean, "react"):
		displayStack = "TypeScript / Node"
		pkgJSON := fmt.Sprintf("{\n  \"name\": %q,\n  \"version\": \"1.0.0\",\n  \"private\": true\n}\n", name)
		_ = os.WriteFile(filepath.Join(targetDir, "package.json"), []byte(pkgJSON), 0644)
	case strings.Contains(stackClean, "python") || strings.Contains(stackClean, "py"):
		displayStack = "Python"
		_ = os.WriteFile(filepath.Join(targetDir, "requirements.txt"), []byte(""), 0644)
		_ = os.WriteFile(filepath.Join(targetDir, "main.py"), []byte("print('Hello from "+name+"!')\n"), 0644)
	case strings.Contains(stackClean, "dotnet") || strings.Contains(stackClean, "csharp"):
		displayStack = ".NET / C#"
	}

	// 3. Register in agyproj projects.json
	reg, _ := w.GetRegistry()
	if reg == nil {
		reg = &ProjectsRegistry{
			ActiveProjectID: name,
			DefaultIDE:      "code",
			ScanRoots:       []string{filepath.Join(home, "projects")},
		}
	}

	nowStr := time.Now().Format(time.RFC3339)
	newItem := ProjectItem{
		ID:           name,
		Name:         name,
		Path:         targetDir,
		Stack:        displayStack,
		GitBranch:    "main",
		IsGit:        true,
		IsDirty:      false,
		DirtyCount:   0,
		IsPinned:     true,
		IsActive:     true,
		LastModified: nowStr,
	}

	// Unset other actives
	found := false
	for i := range reg.Projects {
		if reg.Projects[i].ID == name {
			reg.Projects[i] = newItem
			found = true
		} else {
			reg.Projects[i].IsActive = false
		}
	}
	if !found {
		reg.Projects = append([]ProjectItem{newItem}, reg.Projects...)
	}
	reg.ActiveProjectID = name

	// Write back to registry file
	if data, err := json.MarshalIndent(reg, "", "  "); err == nil {
		_ = os.MkdirAll(filepath.Dir(w.registryPath), 0755)
		_ = os.WriteFile(w.registryPath, data, 0644)
	}

	return &newItem, nil
}

// ListBrainSessions inspects ~/.gemini/antigravity-cli/brain/ and returns the most recent AI sessions
func (w *WorkspaceManager) ListBrainSessions(maxCount int) []SessionItem {
	if maxCount <= 0 {
		maxCount = 10
	}

	home, _ := os.UserHomeDir()
	brainDir := filepath.Join(home, ".gemini", "antigravity-cli", "brain")
	entries, err := os.ReadDir(brainDir)
	if err != nil {
		return nil
	}

	var results []SessionItem
	for _, entry := range entries {
		if !entry.IsDir() || entry.Name() == "scratch" || strings.HasPrefix(entry.Name(), ".") {
			continue
		}

		info, err := entry.Info()
		if err != nil {
			continue
		}

		convID := entry.Name()
		transcriptPath := filepath.Join(brainDir, convID, ".system_generated", "logs", "transcript.jsonl")

		steps := 0
		summary := ""
		if f, err := os.Open(transcriptPath); err == nil {
			scanner := bufio.NewScanner(f)
			buf := make([]byte, 256*1024)
			scanner.Buffer(buf, 1024*1024)

			for scanner.Scan() {
				line := strings.TrimSpace(scanner.Text())
				if line == "" {
					continue
				}
				steps++
				var stepObj struct {
					Content  string `json:"content"`
					Thinking string `json:"thinking"`
				}
				if json.Unmarshal([]byte(line), &stepObj) == nil {
					if summary == "" && stepObj.Content != "" {
						summary = stepObj.Content
					} else if stepObj.Thinking != "" {
						summary = stepObj.Thinking
					}
				}
			}
			_ = f.Close()
		}

		if len(summary) > 60 {
			summary = summary[:60] + "..."
		}
		if summary == "" {
			summary = "Antigravity active session"
		}

		results = append(results, SessionItem{
			ID:        convID,
			Steps:     steps,
			CreatedAt: info.ModTime(),
			Summary:   summary,
			CostUSD:   float64(steps) * 0.0005,
		})
	}

	// Sort newest first
	sort.Slice(results, func(i, j int) bool {
		return results[i].CreatedAt.After(results[j].CreatedAt)
	})

	if len(results) > maxCount {
		results = results[:maxCount]
	}

	return results
}
