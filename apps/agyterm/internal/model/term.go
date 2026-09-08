package model

// ThemeInfo represents an Oh My Posh shell prompt theme
type ThemeInfo struct {
	Name       string `json:"name"`
	Path       string `json:"path"`
	IsSelected bool   `json:"is_selected"`
	IsMobile   bool   `json:"is_mobile"`
	Preview    string `json:"preview"`
}

// FontInfo represents an available system or nerd font
type FontInfo struct {
	Name       string `json:"name"`
	IsNerdFont bool   `json:"is_nerd_font"`
	Source     string `json:"source"` // "windows", "linux", "user"
	IsCurrent  bool   `json:"is_current"`
}

// WindowsTerminalProfile represents a profile in Windows Terminal settings.json
type WindowsTerminalProfile struct {
	Guid        string  `json:"guid"`
	Name        string  `json:"name"`
	FontFace    string  `json:"font_face"`
	FontSize    float64 `json:"font_size"`
	Opacity     int     `json:"opacity"`
	ColorScheme string  `json:"color_scheme"`
	Hidden      bool    `json:"hidden"`
}

// TerminalConfigState holds the combined live configuration
type TerminalConfigState struct {
	CurrentTheme        string                   `json:"current_theme"`
	ThemeSourceDir      string                   `json:"theme_source_dir"`
	WindowsSettingsFile string                   `json:"windows_settings_file"`
	Profiles            []WindowsTerminalProfile `json:"profiles"`
	AvailableFonts      []FontInfo               `json:"available_fonts"`
}

// ExternalToolInfo represents an external CLI tool or shell subsystem config
type ExternalToolInfo struct {
	ID         string   `json:"id"`
	Name       string   `json:"name"`
	Category   string   `json:"category"`
	Installed  bool     `json:"installed"`
	BinaryPath string   `json:"binary_path"`
	Version    string   `json:"version"`
	ConfigPath string   `json:"config_path"`
	Summary    string   `json:"summary"`
	Details    []string `json:"details"`
	Tips       string   `json:"tips"`
}

