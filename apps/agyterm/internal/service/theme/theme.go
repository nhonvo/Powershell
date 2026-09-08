package theme

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"sort"
	"strings"

	"agyterm/internal/model"
)

// ResolveThemesDir searches for the powershell-themes directory
func ResolveThemesDir() string {
	if env := os.Getenv("POSH_THEMES_PATH"); env != "" {
		if fi, err := os.Stat(env); err == nil && fi.IsDir() {
			return env
		}
	}

	home, _ := os.UserHomeDir()
	candidates := []string{
		filepath.Join(home, "projects", "powershell-profile", "shell", "assets", "powershell-themes"),
		filepath.Join(home, ".poshthemes"),
		filepath.Join(home, ".config", "oh-my-posh", "themes"),
	}

	for _, c := range candidates {
		if fi, err := os.Stat(c); err == nil && fi.IsDir() {
			return c
		}
	}

	return ""
}

// GetSelectedTheme gets the currently active Oh My Posh theme
func GetSelectedTheme() string {
	home, _ := os.UserHomeDir()
	file1 := filepath.Join(home, ".config", "selected_posh_theme.txt")
	if data, err := os.ReadFile(file1); err == nil {
		trimmed := strings.TrimSpace(string(data))
		if trimmed != "" {
			return trimmed
		}
	}

	file2 := filepath.Join(home, ".config", "antigravity", "selected_theme.txt")
	if data, err := os.ReadFile(file2); err == nil {
		trimmed := strings.TrimSpace(string(data))
		if trimmed != "" {
			return trimmed
		}
	}

	if env := os.Getenv("POSH_THEME"); env != "" {
		return env
	}
	if env := os.Getenv("THEME"); env != "" {
		return env
	}

	return "neko"
}

// ListThemes returns all available Oh My Posh themes in themesDir
func ListThemes(themesDir string, currentTheme string) ([]model.ThemeInfo, error) {
	if themesDir == "" {
		return nil, fmt.Errorf("themes directory not found")
	}

	entries, err := os.ReadDir(themesDir)
	if err != nil {
		return nil, err
	}

	var themes []model.ThemeInfo
	for _, entry := range entries {
		if entry.IsDir() || !strings.HasSuffix(entry.Name(), ".omp.json") {
			continue
		}

		name := strings.TrimSuffix(entry.Name(), ".omp.json")
		fullPath := filepath.Join(themesDir, entry.Name())
		isSel := strings.EqualFold(name, currentTheme)
		isMobile := strings.HasSuffix(name, "-mobile") || strings.HasSuffix(name, ".minimal")

		preview := BuildThemePreview(fullPath)

		themes = append(themes, model.ThemeInfo{
			Name:       name,
			Path:       fullPath,
			IsSelected: isSel,
			IsMobile:   isMobile,
			Preview:    preview,
		})
	}

	sort.Slice(themes, func(i, j int) bool {
		return strings.ToLower(themes[i].Name) < strings.ToLower(themes[j].Name)
	})

	return themes, nil
}

// SetTheme writes the chosen theme persistently
func SetTheme(themeName string, themesDir string) error {
	themeName = strings.TrimSpace(themeName)
	if themeName == "" {
		return fmt.Errorf("theme name cannot be empty")
	}

	themePath := filepath.Join(themesDir, themeName+".omp.json")
	if _, err := os.Stat(themePath); err != nil {
		return fmt.Errorf("theme file '%s.omp.json' not found in %s", themeName, themesDir)
	}

	home, _ := os.UserHomeDir()
	configDir := filepath.Join(home, ".config")
	_ = os.MkdirAll(configDir, 0755)

	// 1. Write Linux zsh/bash theme config
	file1 := filepath.Join(configDir, "selected_posh_theme.txt")
	if err := os.WriteFile(file1, []byte(themeName+"\n"), 0644); err != nil {
		return fmt.Errorf("failed to write %s: %w", file1, err)
	}

	// 2. Write cross-platform antigravity theme config if dir exists
	agyDir := filepath.Join(configDir, "antigravity")
	if _, err := os.Stat(agyDir); err == nil {
		file2 := filepath.Join(agyDir, "selected_theme.txt")
		_ = os.WriteFile(file2, []byte(themeName+"\n"), 0644)
	}

	return nil
}

// BuildThemePreview extracts segment color hints from omp.json
func BuildThemePreview(path string) string {
	data, err := os.ReadFile(path)
	if err != nil {
		return "\033[36m[ prompt]\033[0m"
	}

	var raw struct {
		Blocks []struct {
			Segments []struct {
				Type       string `json:"type"`
				Background string `json:"background"`
				Foreground string `json:"foreground"`
			} `json:"segments"`
		} `json:"blocks"`
	}

	if err := json.Unmarshal(data, &raw); err != nil {
		return "\033[36m[ prompt]\033[0m"
	}

	var b strings.Builder
	count := 0
	for _, blk := range raw.Blocks {
		for _, seg := range blk.Segments {
			if count >= 3 {
				break
			}
			label := seg.Type
			if label == "" {
				label = "dir"
			}
			if len(label) > 6 {
				label = label[:6]
			}

			// Map common colors
			colorCode := "\033[36m"
			bgLower := strings.ToLower(seg.Background)
			if strings.Contains(bgLower, "red") || strings.HasPrefix(bgLower, "#e") {
				colorCode = "\033[31m"
			} else if strings.Contains(bgLower, "green") || strings.HasPrefix(bgLower, "#5") || strings.HasPrefix(bgLower, "#a") {
				colorCode = "\033[32m"
			} else if strings.Contains(bgLower, "yellow") || strings.HasPrefix(bgLower, "#f") {
				colorCode = "\033[33m"
			} else if strings.Contains(bgLower, "blue") || strings.HasPrefix(bgLower, "#3") || strings.HasPrefix(bgLower, "#8") {
				colorCode = "\033[34m"
			} else if strings.Contains(bgLower, "magenta") || strings.Contains(bgLower, "purple") || strings.HasPrefix(bgLower, "#b") {
				colorCode = "\033[35m"
			}

			b.WriteString(fmt.Sprintf("%s[%s]\033[0m", colorCode, label))
			count++
		}
	}

	if b.Len() == 0 {
		return "\033[32m[path]\033[35m[git]\033[0m"
	}

	return b.String()
}
