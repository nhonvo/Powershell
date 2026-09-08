package linuxterm

import (
	"bufio"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"regexp"
	"strconv"
	"strings"
)

// EmulatorID identifies a supported Linux terminal emulator
type EmulatorID string

const (
	EmulatorAlacritty EmulatorID = "alacritty"
	EmulatorKitty     EmulatorID = "kitty"
	EmulatorWezTerm   EmulatorID = "wezterm"
)

// EmulatorInfo holds detection and configuration details for a Linux terminal
type EmulatorInfo struct {
	ID          EmulatorID `json:"id"`
	Name        string     `json:"name"`
	Installed   bool       `json:"installed"`
	BinaryPath  string     `json:"binary_path"`
	ConfigPath  string     `json:"config_path"`
	ConfigFound bool       `json:"config_found"`
	FontFace    string     `json:"font_face"`
	FontSize    float64    `json:"font_size"`
	Opacity     float64    `json:"opacity"` // 0.0 to 1.0
}

// DefaultConfigPath returns the canonical config file path for an emulator
func DefaultConfigPath(id EmulatorID) string {
	home, err := os.UserHomeDir()
	if err != nil {
		return ""
	}

	switch id {
	case EmulatorAlacritty:
		tomlPath := filepath.Join(home, ".config", "alacritty", "alacritty.toml")
		if _, err := os.Stat(tomlPath); err == nil {
			return tomlPath
		}
		ymlPath := filepath.Join(home, ".config", "alacritty", "alacritty.yml")
		if _, err := os.Stat(ymlPath); err == nil {
			return ymlPath
		}
		return tomlPath
	case EmulatorKitty:
		return filepath.Join(home, ".config", "kitty", "kitty.conf")
	case EmulatorWezTerm:
		luaPath := filepath.Join(home, ".config", "wezterm", "wezterm.lua")
		if _, err := os.Stat(luaPath); err == nil {
			return luaPath
		}
		homeLua := filepath.Join(home, ".wezterm.lua")
		if _, err := os.Stat(homeLua); err == nil {
			return homeLua
		}
		return luaPath
	}
	return ""
}

// FindBinary searches PATH and common Linux bin directories for a binary
func FindBinary(name string) string {
	if p, err := exec.LookPath(name); err == nil {
		return p
	}
	home, _ := os.UserHomeDir()
	candidates := []string{
		filepath.Join(home, ".local", "bin", name),
		filepath.Join("/usr/bin", name),
		filepath.Join("/usr/local/bin", name),
		filepath.Join("/bin", name),
	}
	for _, c := range candidates {
		if fi, err := os.Stat(c); err == nil && !fi.IsDir() {
			return c
		}
	}
	return ""
}

// DetectTerminals inspects the host system for Alacritty, Kitty, and WezTerm
func DetectTerminals() []EmulatorInfo {
	emulators := []struct {
		id   EmulatorID
		name string
	}{
		{EmulatorAlacritty, "Alacritty"},
		{EmulatorKitty, "Kitty"},
		{EmulatorWezTerm, "WezTerm"},
	}

	var results []EmulatorInfo
	for _, em := range emulators {
		bin := FindBinary(string(em.id))
		cfgPath := DefaultConfigPath(em.id)
		cfgFound := false
		if fi, err := os.Stat(cfgPath); err == nil && !fi.IsDir() {
			cfgFound = true
		}

		info := EmulatorInfo{
			ID:          em.id,
			Name:        em.name,
			Installed:   bin != "",
			BinaryPath:  bin,
			ConfigPath:  cfgPath,
			ConfigFound: cfgFound,
		}

		if cfgFound {
			if parsed, err := ReadTerminalConfig(em.id, cfgPath); err == nil && parsed != nil {
				info.FontFace = parsed.FontFace
				info.FontSize = parsed.FontSize
				info.Opacity = parsed.Opacity
			}
		}

		results = append(results, info)
	}

	return results
}

// ReadTerminalConfig parses the font face, font size, and opacity from a terminal config file
func ReadTerminalConfig(id EmulatorID, configPath string) (*EmulatorInfo, error) {
	data, err := os.ReadFile(configPath)
	if err != nil {
		return nil, err
	}

	info := &EmulatorInfo{
		ID:          id,
		ConfigPath:  configPath,
		ConfigFound: true,
	}

	content := string(data)

	switch id {
	case EmulatorAlacritty:
		// Font family
		reFamily := regexp.MustCompile(`(?m)^\s*family\s*=\s*["']([^"']+)["']`)
		if m := reFamily.FindStringSubmatch(content); len(m) > 1 {
			info.FontFace = m[1]
		}
		// Font size
		reSize := regexp.MustCompile(`(?m)^\s*size\s*=\s*([0-9.]+)`)
		if m := reSize.FindStringSubmatch(content); len(m) > 1 {
			if s, err := strconv.ParseFloat(m[1], 64); err == nil {
				info.FontSize = s
			}
		}
		// Window opacity
		reOp := regexp.MustCompile(`(?m)^\s*opacity\s*=\s*([0-9.]+)`)
		if m := reOp.FindStringSubmatch(content); len(m) > 1 {
			if op, err := strconv.ParseFloat(m[1], 64); err == nil {
				info.Opacity = op
			}
		}

	case EmulatorKitty:
		scanner := bufio.NewScanner(strings.NewReader(content))
		for scanner.Scan() {
			line := strings.TrimSpace(scanner.Text())
			if strings.HasPrefix(line, "#") {
				continue
			}
			fields := strings.Fields(line)
			if len(fields) >= 2 {
				switch fields[0] {
				case "font_family":
					info.FontFace = strings.Join(fields[1:], " ")
				case "font_size":
					if s, err := strconv.ParseFloat(fields[1], 64); err == nil {
						info.FontSize = s
					}
				case "background_opacity":
					if op, err := strconv.ParseFloat(fields[1], 64); err == nil {
						info.Opacity = op
					}
				}
			}
		}

	case EmulatorWezTerm:
		reFont := regexp.MustCompile(`(?m)(?:config\.)?font\s*=\s*wezterm\.font\s*\(?\s*["']([^"']+)["']`)
		if m := reFont.FindStringSubmatch(content); len(m) > 1 {
			info.FontFace = m[1]
		}
		reSize := regexp.MustCompile(`(?m)(?:config\.)?font_size\s*=\s*([0-9.]+)`)
		if m := reSize.FindStringSubmatch(content); len(m) > 1 {
			if s, err := strconv.ParseFloat(m[1], 64); err == nil {
				info.FontSize = s
			}
		}
		reOp := regexp.MustCompile(`(?m)(?:config\.)?window_background_opacity\s*=\s*([0-9.]+)`)
		if m := reOp.FindStringSubmatch(content); len(m) > 1 {
			if op, err := strconv.ParseFloat(m[1], 64); err == nil {
				info.Opacity = op
			}
		}
	}

	return info, nil
}

// SetTerminalFont updates font family and size in the specified config file
func SetTerminalFont(id EmulatorID, configPath string, fontFace string, fontSize float64) error {
	if configPath == "" {
		configPath = DefaultConfigPath(id)
	}

	dir := filepath.Dir(configPath)
	if err := os.MkdirAll(dir, 0755); err != nil {
		return fmt.Errorf("failed to create config directory %s: %w", dir, err)
	}

	var content string
	if data, err := os.ReadFile(configPath); err == nil {
		content = string(data)
	}

	switch id {
	case EmulatorAlacritty:
		content = updateAlacrittyFont(content, fontFace, fontSize)
	case EmulatorKitty:
		content = updateKittyFont(content, fontFace, fontSize)
	case EmulatorWezTerm:
		content = updateWezTermFont(content, fontFace, fontSize)
	default:
		return fmt.Errorf("unsupported emulator: %s", id)
	}

	return os.WriteFile(configPath, []byte(content), 0644)
}

// SetTerminalOpacity updates background/window opacity (0.0 - 1.0)
func SetTerminalOpacity(id EmulatorID, configPath string, opacity float64) error {
	if configPath == "" {
		configPath = DefaultConfigPath(id)
	}

	if opacity > 1.0 && opacity <= 100.0 {
		opacity = opacity / 100.0
	}
	if opacity < 0.0 {
		opacity = 0.0
	} else if opacity > 1.0 {
		opacity = 1.0
	}

	dir := filepath.Dir(configPath)
	if err := os.MkdirAll(dir, 0755); err != nil {
		return fmt.Errorf("failed to create config directory %s: %w", dir, err)
	}

	var content string
	if data, err := os.ReadFile(configPath); err == nil {
		content = string(data)
	}

	switch id {
	case EmulatorAlacritty:
		content = updateAlacrittyOpacity(content, opacity)
	case EmulatorKitty:
		content = updateKittyOpacity(content, opacity)
	case EmulatorWezTerm:
		content = updateWezTermOpacity(content, opacity)
	default:
		return fmt.Errorf("unsupported emulator: %s", id)
	}

	return os.WriteFile(configPath, []byte(content), 0644)
}

// Alacritty helper
func updateAlacrittyFont(content, fontFace string, fontSize float64) string {
	if strings.TrimSpace(content) == "" {
		var b strings.Builder
		b.WriteString("[font]\n")
		if fontSize > 0 {
			fmt.Fprintf(&b, "size = %.1f\n\n", fontSize)
		} else {
			b.WriteString("size = 12.0\n\n")
		}
		b.WriteString("[font.normal]\n")
		fmt.Fprintf(&b, "family = %q\n", fontFace)
		return b.String()
	}

	reFamily := regexp.MustCompile(`(?m)^\s*family\s*=\s*["'][^"']+["']`)
	if reFamily.MatchString(content) {
		content = reFamily.ReplaceAllString(content, fmt.Sprintf("family = %q", fontFace))
	} else if strings.Contains(content, "[font.normal]") {
		content = strings.Replace(content, "[font.normal]", fmt.Sprintf("[font.normal]\nfamily = %q", fontFace), 1)
	} else {
		content += fmt.Sprintf("\n[font.normal]\nfamily = %q\n", fontFace)
	}

	if fontSize > 0 {
		reSize := regexp.MustCompile(`(?m)^\s*size\s*=\s*[0-9.]+`)
		if reSize.MatchString(content) {
			content = reSize.ReplaceAllString(content, fmt.Sprintf("size = %.1f", fontSize))
		} else if strings.Contains(content, "[font]") {
			content = strings.Replace(content, "[font]", fmt.Sprintf("[font]\nsize = %.1f", fontSize), 1)
		} else {
			content += fmt.Sprintf("\n[font]\nsize = %.1f\n", fontSize)
		}
	}

	return content
}

func updateAlacrittyOpacity(content string, opacity float64) string {
	if strings.TrimSpace(content) == "" {
		return fmt.Sprintf("[window]\nopacity = %.2f\n", opacity)
	}

	reOp := regexp.MustCompile(`(?m)^\s*opacity\s*=\s*[0-9.]+`)
	if reOp.MatchString(content) {
		return reOp.ReplaceAllString(content, fmt.Sprintf("opacity = %.2f", opacity))
	}
	if strings.Contains(content, "[window]") {
		return strings.Replace(content, "[window]", fmt.Sprintf("[window]\nopacity = %.2f", opacity), 1)
	}
	return content + fmt.Sprintf("\n[window]\nopacity = %.2f\n", opacity)
}

// Kitty helper
func updateKittyFont(content, fontFace string, fontSize float64) string {
	lines := strings.Split(content, "\n")
	foundFamily := false
	foundSize := false

	var newLines []string
	for _, line := range lines {
		trimmed := strings.TrimSpace(line)
		if strings.HasPrefix(trimmed, "font_family") {
			newLines = append(newLines, fmt.Sprintf("font_family %s", fontFace))
			foundFamily = true
		} else if fontSize > 0 && strings.HasPrefix(trimmed, "font_size") {
			newLines = append(newLines, fmt.Sprintf("font_size %.1f", fontSize))
			foundSize = true
		} else {
			newLines = append(newLines, line)
		}
	}

	if !foundFamily {
		newLines = append(newLines, fmt.Sprintf("font_family %s", fontFace))
	}
	if fontSize > 0 && !foundSize {
		newLines = append(newLines, fmt.Sprintf("font_size %.1f", fontSize))
	}

	return strings.Join(newLines, "\n")
}

func updateKittyOpacity(content string, opacity float64) string {
	lines := strings.Split(content, "\n")
	found := false

	var newLines []string
	for _, line := range lines {
		trimmed := strings.TrimSpace(line)
		if strings.HasPrefix(trimmed, "background_opacity") {
			newLines = append(newLines, fmt.Sprintf("background_opacity %.2f", opacity))
			found = true
		} else {
			newLines = append(newLines, line)
		}
	}

	if !found {
		newLines = append(newLines, fmt.Sprintf("background_opacity %.2f", opacity))
	}

	return strings.Join(newLines, "\n")
}

// WezTerm helper
func updateWezTermFont(content, fontFace string, fontSize float64) string {
	if strings.TrimSpace(content) == "" {
		sz := 12.0
		if fontSize > 0 {
			sz = fontSize
		}
		return fmt.Sprintf(`local wezterm = require 'wezterm'
local config = {}

if wezterm.config_builder then
  config = wezterm.config_builder()
end

config.font = wezterm.font %q
config.font_size = %.1f

return config
`, fontFace, sz)
	}

	reFont := regexp.MustCompile(`(?m)(?:config\.)?font\s*=\s*wezterm\.font\s*\(?\s*["'][^"']+["']\)?`)
	if reFont.MatchString(content) {
		content = reFont.ReplaceAllString(content, fmt.Sprintf("config.font = wezterm.font(%q)", fontFace))
	} else if strings.Contains(content, "return config") {
		content = strings.Replace(content, "return config", fmt.Sprintf("config.font = wezterm.font(%q)\nreturn config", fontFace), 1)
	} else {
		content += fmt.Sprintf("\nconfig.font = wezterm.font(%q)\n", fontFace)
	}

	if fontSize > 0 {
		reSize := regexp.MustCompile(`(?m)(?:config\.)?font_size\s*=\s*[0-9.]+`)
		if reSize.MatchString(content) {
			content = reSize.ReplaceAllString(content, fmt.Sprintf("config.font_size = %.1f", fontSize))
		} else if strings.Contains(content, "return config") {
			content = strings.Replace(content, "return config", fmt.Sprintf("config.font_size = %.1f\nreturn config", fontSize), 1)
		} else {
			content += fmt.Sprintf("\nconfig.font_size = %.1f\n", fontSize)
		}
	}

	return content
}

func updateWezTermOpacity(content string, opacity float64) string {
	if strings.TrimSpace(content) == "" {
		return fmt.Sprintf(`local wezterm = require 'wezterm'
local config = {}

if wezterm.config_builder then
  config = wezterm.config_builder()
end

config.window_background_opacity = %.2f

return config
`, opacity)
	}

	reOp := regexp.MustCompile(`(?m)(?:config\.)?window_background_opacity\s*=\s*[0-9.]+`)
	if reOp.MatchString(content) {
		return reOp.ReplaceAllString(content, fmt.Sprintf("config.window_background_opacity = %.2f", opacity))
	}
	if strings.Contains(content, "return config") {
		return strings.Replace(content, "return config", fmt.Sprintf("config.window_background_opacity = %.2f\nreturn config", opacity), 1)
	}
	return content + fmt.Sprintf("\nconfig.window_background_opacity = %.2f\n", opacity)
}

// CanonicalFontName maps common shorthand names to official Nerd Font GitHub release asset names
func CanonicalFontName(name string) string {
	lower := strings.ToLower(strings.TrimSpace(name))
	lower = strings.ReplaceAll(lower, "-", "")
	lower = strings.ReplaceAll(lower, "_", "")
	lower = strings.ReplaceAll(lower, " ", "")
	lower = strings.TrimSuffix(lower, "nerdfont")
	lower = strings.TrimSuffix(lower, "nf")

	switch lower {
	case "hack":
		return "Hack"
	case "firacode", "fira":
		return "FiraCode"
	case "jetbrainsmono", "jetbrains":
		return "JetBrainsMono"
	case "cascadiacode", "cascadia":
		return "CascadiaCode"
	case "meslo", "meslolgs":
		return "Meslo"
	case "ubuntumono", "ubuntu":
		return "UbuntuMono"
	case "sourcecodepro", "saucecodepro":
		return "SourceCodePro"
	case "dejavusansmono", "dejavu":
		return "DejaVuSansMono"
	case "inconsolata":
		return "Inconsolata"
	case "iosevka":
		return "Iosevka"
	case "victormono", "victor":
		return "VictorMono"
	default:
		// Capitalize first letter as fallback
		if len(name) > 0 {
			return strings.ToUpper(name[:1]) + name[1:]
		}
		return name
	}
}

// InstallNerdFont installs or gives instructions to install a Nerd Font into ~/.local/share/fonts
func InstallNerdFont(fontName string) (string, error) {
	if strings.TrimSpace(fontName) == "" {
		return "", fmt.Errorf("font name cannot be empty")
	}

	canonical := CanonicalFontName(fontName)
	home, err := os.UserHomeDir()
	if err != nil {
		return "", fmt.Errorf("could not resolve user home directory: %w", err)
	}

	targetDir := filepath.Join(home, ".local", "share", "fonts")
	if err := os.MkdirAll(targetDir, 0755); err != nil {
		return "", fmt.Errorf("failed to create font directory %s: %w", targetDir, err)
	}

	url := fmt.Sprintf("https://github.com/ryanoasis/nerd-fonts/releases/latest/download/%s.tar.xz", canonical)
	tmpFile := filepath.Join(os.TempDir(), fmt.Sprintf("%s.tar.xz", canonical))

	// 1. Download font archive via curl (with fallback to wget)
	dlSuccess := false
	var dlErr error

	if _, err := exec.LookPath("curl"); err == nil {
		cmd := exec.Command("curl", "-sSfL", "--connect-timeout", "10", "--max-time", "120", "-o", tmpFile, url)
		if out, err := cmd.CombinedOutput(); err == nil {
			dlSuccess = true
		} else {
			dlErr = fmt.Errorf("curl failed: %s (%w)", strings.TrimSpace(string(out)), err)
		}
	} else if _, err := exec.LookPath("wget"); err == nil {
		cmd := exec.Command("wget", "-q", "--timeout=10", "-O", tmpFile, url)
		if out, err := cmd.CombinedOutput(); err == nil {
			dlSuccess = true
		} else {
			dlErr = fmt.Errorf("wget failed: %s (%w)", strings.TrimSpace(string(out)), err)
		}
	} else {
		dlErr = fmt.Errorf("neither 'curl' nor 'wget' found in PATH")
	}

	// If download succeeded, unpack it and run fc-cache
	if dlSuccess {
		defer os.Remove(tmpFile)

		// Unpack using tar
		tarCmd := exec.Command("tar", "-xJf", tmpFile, "-C", targetDir)
		if out, err := tarCmd.CombinedOutput(); err != nil {
			// Try tar -xf in case xz is handled automatically
			tarCmd2 := exec.Command("tar", "-xf", tmpFile, "-C", targetDir)
			if out2, err2 := tarCmd2.CombinedOutput(); err2 != nil {
				return "", fmt.Errorf("failed to extract %s: %s / %s", tmpFile, string(out), string(out2))
			}
		}

		// Run fc-cache -f
		fcMsg := ""
		if fcPath, err := exec.LookPath("fc-cache"); err == nil {
			fcCmd := exec.Command(fcPath, "-f")
			if fcOut, fcErr := fcCmd.CombinedOutput(); fcErr == nil {
				fcMsg = "✔ Font cache refreshed with 'fc-cache -f'\n"
			} else {
				fcMsg = fmt.Sprintf("⚠️ fc-cache notice: %s\n", strings.TrimSpace(string(fcOut)))
			}
		}

		msg := fmt.Sprintf("✔ Successfully installed '%s Nerd Font' into %s\n%s", canonical, targetDir, fcMsg)
		return msg, nil
	}

	// 2. Download failed or network unavailable -> Provide clear instructions and run fc-cache
	instructions := fmt.Sprintf(`⚠️ Automated download could not complete: %v

📥 Manual Installation Instructions for %s Nerd Font:
1. Download release package:
   curl -fLo /tmp/%s.tar.xz %s
2. Extract into Linux fonts directory:
   mkdir -p ~/.local/share/fonts
   tar -xf /tmp/%s.tar.xz -C ~/.local/share/fonts/
3. Rebuild font cache:
   fc-cache -f
`, dlErr, canonical, canonical, url, canonical)

	// Even on download failure, if font directory exists, run fc-cache if requested
	if fcPath, err := exec.LookPath("fc-cache"); err == nil {
		_ = exec.Command(fcPath, "-f").Run()
	}

	return instructions, nil
}
