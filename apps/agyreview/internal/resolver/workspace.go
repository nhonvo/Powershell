package resolver

import (
	"encoding/json"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
	"time"

	"agyreview/internal/model"
)

type ProjectsRegistry struct {
	Projects []struct {
		Name      string `json:"name"`
		Path      string `json:"path"`
		GitBranch string `json:"git_branch"`
	} `json:"projects"`
}

type CLIConfig struct {
	TrustedWorkspaces []string `json:"trustedWorkspaces"`
}

// DiscoverAllProjects gathers registered and local workspace directories for review.
func DiscoverAllProjects() []model.TargetRepo {
	var targets []model.TargetRepo
	seen := make(map[string]bool)

	home, _ := os.UserHomeDir()

	// 1. Read agyproj / agybot projects.json
	projJsonPath := filepath.Join(home, ".config", "antigravity", "projects.json")
	if data, err := os.ReadFile(projJsonPath); err == nil {
		var reg ProjectsRegistry
		if err := json.Unmarshal(data, &reg); err == nil {
			for _, p := range reg.Projects {
				clean := filepath.Clean(p.Path)
				if !seen[clean] {
					if fi, err := os.Stat(clean); err == nil && fi.IsDir() {
						seen[clean] = true
						branch := p.GitBranch
						if branch == "" {
							branch = getGitBranch(clean)
						}
						targets = append(targets, model.TargetRepo{
							Name:         p.Name,
							Path:         clean,
							Branch:       branch,
							HeadCommit:   getGitHead(clean),
							Scope:        model.ScopeWholeCodebase,
							DiscoveredAt: time.Now(),
						})
					}
				}
			}
		}
	}

	// 2. Read ~/.gemini/antigravity-cli/settings.json
	settingsPath := filepath.Join(home, ".gemini", "antigravity-cli", "settings.json")
	if data, err := os.ReadFile(settingsPath); err == nil {
		var cfg CLIConfig
		if err := json.Unmarshal(data, &cfg); err == nil {
			for _, ws := range cfg.TrustedWorkspaces {
				clean := filepath.Clean(ws)
				if !seen[clean] {
					if fi, err := os.Stat(clean); err == nil && fi.IsDir() {
						seen[clean] = true
						targets = append(targets, model.TargetRepo{
							Name:         filepath.Base(clean),
							Path:         clean,
							Branch:       getGitBranch(clean),
							HeadCommit:   getGitHead(clean),
							Scope:        model.ScopeWholeCodebase,
							DiscoveredAt: time.Now(),
						})
					}
				}
			}
		}
	}

	// 3. Scan common project root directories
	scanRoots := []string{
		filepath.Join(home, "Documents", "Powershell"),
		filepath.Join(home, "projects"),
		"D:\\projects",
		"D:\\learning-projects-code",
	}

	for _, root := range scanRoots {
		if fi, err := os.Stat(root); err == nil && fi.IsDir() {
			entries, err := os.ReadDir(root)
			if err == nil {
				for _, entry := range entries {
					if entry.IsDir() {
						pPath := filepath.Join(root, entry.Name())
						clean := filepath.Clean(pPath)
						if !seen[clean] {
							if isProjectDir(clean) {
								seen[clean] = true
								targets = append(targets, model.TargetRepo{
									Name:         entry.Name(),
									Path:         clean,
									Branch:       getGitBranch(clean),
									HeadCommit:   getGitHead(clean),
									Scope:        model.ScopeWholeCodebase,
									DiscoveredAt: time.Now(),
								})
							}
						}
					}
				}
			}
		}
	}

	return targets
}

func getGitBranch(dir string) string {
	cmd := exec.Command("git", "-C", dir, "rev-parse", "--abbrev-ref", "HEAD")
	out, err := cmd.Output()
	if err != nil {
		return "main"
	}
	return strings.TrimSpace(string(out))
}

func getGitHead(dir string) string {
	cmd := exec.Command("git", "-C", dir, "rev-parse", "--short", "HEAD")
	out, err := cmd.Output()
	if err != nil {
		return "head"
	}
	return strings.TrimSpace(string(out))
}

func isProjectDir(dir string) bool {
	gitDir := filepath.Join(dir, ".git")
	if _, err := os.Stat(gitDir); err == nil {
		return true
	}
	entries, err := os.ReadDir(dir)
	if err != nil {
		return false
	}
	for _, e := range entries {
		ext := strings.ToLower(filepath.Ext(e.Name()))
		if ext == ".go" || ext == ".cs" || ext == ".py" || ext == ".js" || ext == ".ts" || ext == ".ps1" || e.Name() == "package.json" || e.Name() == "Makefile" {
			return true
		}
	}
	return false
}
