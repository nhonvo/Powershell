package winterm

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"sort"
	"strings"

	"agyterm/internal/model"
)

// FindSettingsFile locates the Windows Terminal settings.json file from WSL
func FindSettingsFile() string {
	usersDir := "/mnt/c/Users"
	entries, err := os.ReadDir(usersDir)
	if err != nil {
		return ""
	}

	for _, entry := range entries {
		if !entry.IsDir() {
			continue
		}
		user := entry.Name()
		if user == "Default" || user == "Public" || user == "All Users" || strings.HasPrefix(user, "TEMP") {
			continue
		}

		// Check Packaged Store app
		pkgDir := filepath.Join(usersDir, user, "AppData", "Local", "Packages")
		if pkgEntries, err := os.ReadDir(pkgDir); err == nil {
			for _, p := range pkgEntries {
				if strings.HasPrefix(p.Name(), "Microsoft.WindowsTerminal_") {
					candidate := filepath.Join(pkgDir, p.Name(), "LocalState", "settings.json")
					if fi, err := os.Stat(candidate); err == nil && !fi.IsDir() {
						return candidate
					}
				}
			}
		}

		// Check Unpackaged / Preview / Scoop
		cand2 := filepath.Join(usersDir, user, "AppData", "Local", "Microsoft", "Windows Terminal", "settings.json")
		if fi, err := os.Stat(cand2); err == nil && !fi.IsDir() {
			return cand2
		}
	}

	return ""
}

// ReadProfiles parses all profiles from Windows Terminal settings.json
func ReadProfiles(settingsFile string) ([]model.WindowsTerminalProfile, error) {
	if settingsFile == "" {
		return nil, fmt.Errorf("settings file not specified")
	}

	data, err := os.ReadFile(settingsFile)
	if err != nil {
		return nil, err
	}

	var root map[string]interface{}
	if err := json.Unmarshal(data, &root); err != nil {
		return nil, fmt.Errorf("invalid json in %s: %w", settingsFile, err)
	}

	profilesRaw, ok := root["profiles"].(map[string]interface{})
	if !ok {
		return nil, fmt.Errorf("no 'profiles' section found in settings.json")
	}

	listRaw, ok := profilesRaw["list"].([]interface{})
	if !ok {
		return nil, fmt.Errorf("no 'profiles.list' array found in settings.json")
	}

	var profiles []model.WindowsTerminalProfile
	for _, item := range listRaw {
		m, ok := item.(map[string]interface{})
		if !ok {
			continue
		}

		p := model.WindowsTerminalProfile{
			Name: getString(m, "name"),
			Guid: getString(m, "guid"),
		}
		if h, ok := m["hidden"].(bool); ok {
			p.Hidden = h
		}
		if op, ok := m["opacity"].(float64); ok {
			p.Opacity = int(op)
		}
		p.ColorScheme = getString(m, "colorScheme")

		if fontMap, ok := m["font"].(map[string]interface{}); ok {
			p.FontFace = getString(fontMap, "face")
			if sz, ok := fontMap["size"].(float64); ok {
				p.FontSize = sz
			}
		}

		if p.Name != "" {
			profiles = append(profiles, p)
		}
	}

	return profiles, nil
}

// UpdateProfileFont sets the font face and optional font size for a profile
func UpdateProfileFont(settingsFile string, profileName string, newFontFace string, newFontSize float64) error {
	data, err := os.ReadFile(settingsFile)
	if err != nil {
		return err
	}

	// Backup
	_ = os.WriteFile(settingsFile+".bak", data, 0644)

	var root map[string]interface{}
	if err := json.Unmarshal(data, &root); err != nil {
		return err
	}

	profilesRaw, ok := root["profiles"].(map[string]interface{})
	if !ok {
		return fmt.Errorf("missing profiles section")
	}

	listRaw, ok := profilesRaw["list"].([]interface{})
	if !ok {
		return fmt.Errorf("missing profiles.list array")
	}

	updated := false
	for _, item := range listRaw {
		m, ok := item.(map[string]interface{})
		if !ok {
			continue
		}

		name := getString(m, "name")
		if strings.EqualFold(name, profileName) || profileName == "*" {
			fontMap, ok := m["font"].(map[string]interface{})
			if !ok || fontMap == nil {
				fontMap = make(map[string]interface{})
			}
			fontMap["face"] = newFontFace
			if newFontSize > 0 {
				fontMap["size"] = newFontSize
			}
			m["font"] = fontMap
			updated = true
		}
	}

	if !updated {
		return fmt.Errorf("profile '%s' not found", profileName)
	}

	newData, err := json.MarshalIndent(root, "", "    ")
	if err != nil {
		return err
	}

	return os.WriteFile(settingsFile, newData, 0644)
}

// UpdateProfileOpacity sets opacity for a profile (0-100)
func UpdateProfileOpacity(settingsFile string, profileName string, opacity int) error {
	data, err := os.ReadFile(settingsFile)
	if err != nil {
		return err
	}

	_ = os.WriteFile(settingsFile+".bak", data, 0644)

	var root map[string]interface{}
	if err := json.Unmarshal(data, &root); err != nil {
		return err
	}

	profilesRaw, ok := root["profiles"].(map[string]interface{})
	if !ok {
		return fmt.Errorf("missing profiles section")
	}

	listRaw, ok := profilesRaw["list"].([]interface{})
	if !ok {
		return fmt.Errorf("missing profiles.list array")
	}

	for _, item := range listRaw {
		m, ok := item.(map[string]interface{})
		if !ok {
			continue
		}
		name := getString(m, "name")
		if strings.EqualFold(name, profileName) || profileName == "*" {
			m["opacity"] = opacity
		}
	}

	newData, err := json.MarshalIndent(root, "", "    ")
	if err != nil {
		return err
	}

	return os.WriteFile(settingsFile, newData, 0644)
}

// ListAvailableFonts discovers fonts installed on Windows and Linux
func ListAvailableFonts() ([]model.FontInfo, error) {
	seen := make(map[string]bool)
	var fonts []model.FontInfo

	// Recommended coding fonts list
	curated := []string{
		"Hack Nerd Font",
		"Cascadia Code",
		"Cascadia Mono",
		"FiraCode Nerd Font",
		"JetBrainsMono Nerd Font",
		"MesloLGS NF",
		"UbuntuMono Nerd Font",
		"Consolas",
		"DejaVu Sans Mono",
	}

	// 1. Scan Windows User Fonts
	usersDir := "/mnt/c/Users"
	if entries, err := os.ReadDir(usersDir); err == nil {
		for _, entry := range entries {
			if !entry.IsDir() {
				continue
			}
			userFontsDir := filepath.Join(usersDir, entry.Name(), "AppData", "Local", "Microsoft", "Windows", "Fonts")
			if fontFiles, err := os.ReadDir(userFontsDir); err == nil {
				for _, f := range fontFiles {
					name := f.Name()
					if strings.HasSuffix(name, ".ttf") || strings.HasSuffix(name, ".otf") {
						cleaned := cleanFontFilename(name)
						if cleaned != "" && !seen[cleaned] {
							seen[cleaned] = true
							fonts = append(fonts, model.FontInfo{
								Name:       cleaned,
								IsNerdFont: strings.Contains(strings.ToLower(cleaned), "nerd"),
								Source:     "windows-user",
							})
						}
					}
				}
			}
		}
	}

	// 2. Add curated fonts if not already seen
	for _, c := range curated {
		if !seen[c] {
			seen[c] = true
			fonts = append(fonts, model.FontInfo{
				Name:       c,
				IsNerdFont: strings.Contains(strings.ToLower(c), "nerd") || strings.Contains(strings.ToLower(c), "nf"),
				Source:     "curated",
			})
		}
	}

	sort.Slice(fonts, func(i, j int) bool {
		// Nerd fonts first, then alphabetical
		if fonts[i].IsNerdFont != fonts[j].IsNerdFont {
			return fonts[i].IsNerdFont
		}
		return fonts[i].Name < fonts[j].Name
	})

	return fonts, nil
}

func cleanFontFilename(f string) string {
	f = strings.TrimSuffix(f, ".ttf")
	f = strings.TrimSuffix(f, ".otf")
	// Normalize common names like HackNerdFont-Regular -> Hack Nerd Font
	if strings.HasPrefix(f, "HackNerdFont") {
		return "Hack Nerd Font"
	}
	if strings.HasPrefix(f, "CascadiaCode") {
		return "Cascadia Code"
	}
	if strings.HasPrefix(f, "UbuntuMono") {
		return "UbuntuMono"
	}
	return f
}

func getString(m map[string]interface{}, key string) string {
	if val, ok := m[key].(string); ok {
		return val
	}
	return ""
}
