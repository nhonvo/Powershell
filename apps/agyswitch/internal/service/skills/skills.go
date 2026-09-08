package skills

import (
	"fmt"
	"io"
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

// DiscoverSkills scans global skills (~/.gemini/skills) and workspace skills (.agents/skills).
func (m *Manager) DiscoverSkills(workspaceDir string) ([]model.SkillInfo, error) {
	var results []model.SkillInfo
	seen := make(map[string]bool)

	// 1. Global skills directory (~/.gemini/skills)
	globalDir := filepath.Join(m.UserHome, ".gemini", "skills")
	if entries, err := os.ReadDir(globalDir); err == nil {
		for _, e := range entries {
			if e.IsDir() {
				skillPath := filepath.Join(globalDir, e.Name())
				desc := parseSkillDescription(skillPath)
				results = append(results, model.SkillInfo{
					Name:        e.Name(),
					Description: desc,
					Path:        skillPath,
					IsGlobal:    true,
				})
				seen[e.Name()] = true
			}
		}
	}

	// 2. Workspace skills (.agents/skills)
	if workspaceDir != "" {
		wsSkillsDir := filepath.Join(workspaceDir, ".agents", "skills")
		if entries, err := os.ReadDir(wsSkillsDir); err == nil {
			for _, e := range entries {
				if e.IsDir() && !seen[e.Name()] {
					skillPath := filepath.Join(wsSkillsDir, e.Name())
					desc := parseSkillDescription(skillPath)
					results = append(results, model.SkillInfo{
						Name:        e.Name(),
						Description: desc,
						Path:        skillPath,
						IsGlobal:    false,
					})
				}
			}
		}
	}

	return results, nil
}

func parseSkillDescription(skillDir string) string {
	skillMD := filepath.Join(skillDir, "SKILL.md")
	data, err := os.ReadFile(skillMD)
	if err != nil {
		return "Custom Antigravity skill cheatsheet module."
	}
	lines := strings.Split(string(data), "\n")
	for i, l := range lines {
		l = strings.TrimSpace(l)
		if strings.HasPrefix(strings.ToLower(l), "description:") {
			val := strings.TrimSpace(strings.TrimPrefix(l, "description:"))
			val = strings.Trim(val, " \"'")
			if val == ">" || val == ">-" || val == "|" || val == "|-" || val == "" {
				for j := i + 1; j < len(lines); j++ {
					next := strings.TrimSpace(lines[j])
					if next != "" && !strings.HasPrefix(next, "---") && !strings.Contains(next, ":") {
						return next
					}
				}
			}
			if val != "" {
				return val
			}
		}
	}
	return "Antigravity skill module."
}

// SyncSkills copies all global skills into all target account contexts ~/.gemini_*.
func (m *Manager) SyncSkills(accountDirs []string) (int, error) {
	globalDir := filepath.Join(m.UserHome, ".gemini", "skills")
	if _, err := os.Stat(globalDir); os.IsNotExist(err) {
		return 0, nil
	}

	syncedCount := 0
	for _, accDir := range accountDirs {
		targetSkillsDir := filepath.Join(accDir, "skills")
		_ = os.MkdirAll(targetSkillsDir, 0755)
		if err := mirrorFolder(globalDir, targetSkillsDir); err == nil {
			syncedCount++
		}
	}
	return syncedCount, nil
}

func mirrorFolder(src, dst string) error {
	return filepath.Walk(src, func(path string, info os.FileInfo, err error) error {
		if err != nil {
			return nil
		}
		rel, err := filepath.Rel(src, path)
		if err != nil || rel == "." {
			return nil
		}
		target := filepath.Join(dst, rel)
		if info.IsDir() {
			return os.MkdirAll(target, info.Mode())
		}
		return copyFileContents(path, target)
	})
}

func copyFileContents(src, dst string) error {
	in, err := os.Open(src)
	if err != nil {
		return err
	}
	defer in.Close()

	_ = os.MkdirAll(filepath.Dir(dst), 0755)
	out, err := os.Create(dst)
	if err != nil {
		return err
	}
	defer out.Close()

	_, err = io.Copy(out, in)
	return err
}

func (m *Manager) InstallSkillFromPath(srcPath string) error {
	info, err := os.Stat(srcPath)
	if err != nil || !info.IsDir() {
		return fmt.Errorf("invalid skill directory path: %s", srcPath)
	}

	skillName := filepath.Base(srcPath)
	targetDir := filepath.Join(m.UserHome, ".gemini", "skills", skillName)
	return mirrorFolder(srcPath, targetDir)
}
