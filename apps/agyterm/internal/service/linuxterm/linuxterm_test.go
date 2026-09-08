package linuxterm

import (
	"path/filepath"
	"strings"
	"testing"
)

func TestDefaultConfigPath(t *testing.T) {
	alacrittyPath := DefaultConfigPath(EmulatorAlacritty)
	if !strings.Contains(alacrittyPath, "alacritty") {
		t.Errorf("expected alacritty in path, got %s", alacrittyPath)
	}

	kittyPath := DefaultConfigPath(EmulatorKitty)
	if !strings.Contains(kittyPath, "kitty.conf") {
		t.Errorf("expected kitty.conf in path, got %s", kittyPath)
	}

	weztermPath := DefaultConfigPath(EmulatorWezTerm)
	if !strings.Contains(weztermPath, "wezterm.lua") {
		t.Errorf("expected wezterm.lua in path, got %s", weztermPath)
	}
}

func TestDetectTerminals(t *testing.T) {
	terms := DetectTerminals()
	if len(terms) != 3 {
		t.Fatalf("expected 3 emulators, got %d", len(terms))
	}

	foundIDs := make(map[EmulatorID]bool)
	for _, term := range terms {
		foundIDs[term.ID] = true
		if term.Name == "" {
			t.Errorf("expected non-empty name for %s", term.ID)
		}
	}

	if !foundIDs[EmulatorAlacritty] || !foundIDs[EmulatorKitty] || !foundIDs[EmulatorWezTerm] {
		t.Errorf("missing one or more expected emulators: %v", foundIDs)
	}
}

func TestAlacrittyConfig(t *testing.T) {
	tmpDir := t.TempDir()
	cfgPath := filepath.Join(tmpDir, "alacritty.toml")

	// 1. Create fresh config with font and opacity
	if err := SetTerminalFont(EmulatorAlacritty, cfgPath, "Hack Nerd Font", 13.5); err != nil {
		t.Fatalf("SetTerminalFont failed: %v", err)
	}
	if err := SetTerminalOpacity(EmulatorAlacritty, cfgPath, 0.92); err != nil {
		t.Fatalf("SetTerminalOpacity failed: %v", err)
	}

	// 2. Read back
	info, err := ReadTerminalConfig(EmulatorAlacritty, cfgPath)
	if err != nil {
		t.Fatalf("ReadTerminalConfig failed: %v", err)
	}
	if info.FontFace != "Hack Nerd Font" {
		t.Errorf("expected 'Hack Nerd Font', got '%s'", info.FontFace)
	}
	if info.FontSize != 13.5 {
		t.Errorf("expected 13.5, got %f", info.FontSize)
	}
	if info.Opacity != 0.92 {
		t.Errorf("expected 0.92, got %f", info.Opacity)
	}

	// 3. Update existing config
	if err := SetTerminalFont(EmulatorAlacritty, cfgPath, "JetBrainsMono Nerd Font", 14.0); err != nil {
		t.Fatalf("Update font failed: %v", err)
	}
	if err := SetTerminalOpacity(EmulatorAlacritty, cfgPath, 0.85); err != nil {
		t.Fatalf("Update opacity failed: %v", err)
	}

	info2, err := ReadTerminalConfig(EmulatorAlacritty, cfgPath)
	if err != nil {
		t.Fatalf("ReadTerminalConfig second pass failed: %v", err)
	}
	if info2.FontFace != "JetBrainsMono Nerd Font" {
		t.Errorf("expected 'JetBrainsMono Nerd Font', got '%s'", info2.FontFace)
	}
	if info2.FontSize != 14.0 {
		t.Errorf("expected 14.0, got %f", info2.FontSize)
	}
	if info2.Opacity != 0.85 {
		t.Errorf("expected 0.85, got %f", info2.Opacity)
	}
}

func TestKittyConfig(t *testing.T) {
	tmpDir := t.TempDir()
	cfgPath := filepath.Join(tmpDir, "kitty.conf")

	// 1. Create fresh config
	if err := SetTerminalFont(EmulatorKitty, cfgPath, "FiraCode Nerd Font", 12.0); err != nil {
		t.Fatalf("SetTerminalFont failed: %v", err)
	}
	if err := SetTerminalOpacity(EmulatorKitty, cfgPath, 0.90); err != nil {
		t.Fatalf("SetTerminalOpacity failed: %v", err)
	}

	// 2. Read back
	info, err := ReadTerminalConfig(EmulatorKitty, cfgPath)
	if err != nil {
		t.Fatalf("ReadTerminalConfig failed: %v", err)
	}
	if info.FontFace != "FiraCode Nerd Font" {
		t.Errorf("expected 'FiraCode Nerd Font', got '%s'", info.FontFace)
	}
	if info.FontSize != 12.0 {
		t.Errorf("expected 12.0, got %f", info.FontSize)
	}
	if info.Opacity != 0.90 {
		t.Errorf("expected 0.90, got %f", info.Opacity)
	}

	// 3. Update existing config
	if err := SetTerminalFont(EmulatorKitty, cfgPath, "MesloLGS NF", 11.5); err != nil {
		t.Fatalf("Update font failed: %v", err)
	}
	if err := SetTerminalOpacity(EmulatorKitty, cfgPath, 0.95); err != nil {
		t.Fatalf("Update opacity failed: %v", err)
	}

	info2, err := ReadTerminalConfig(EmulatorKitty, cfgPath)
	if err != nil {
		t.Fatalf("ReadTerminalConfig second pass failed: %v", err)
	}
	if info2.FontFace != "MesloLGS NF" {
		t.Errorf("expected 'MesloLGS NF', got '%s'", info2.FontFace)
	}
	if info2.FontSize != 11.5 {
		t.Errorf("expected 11.5, got %f", info2.FontSize)
	}
	if info2.Opacity != 0.95 {
		t.Errorf("expected 0.95, got %f", info2.Opacity)
	}
}

func TestWezTermConfig(t *testing.T) {
	tmpDir := t.TempDir()
	cfgPath := filepath.Join(tmpDir, "wezterm.lua")

	// 1. Create fresh config
	if err := SetTerminalFont(EmulatorWezTerm, cfgPath, "Cascadia Code", 13.0); err != nil {
		t.Fatalf("SetTerminalFont failed: %v", err)
	}
	if err := SetTerminalOpacity(EmulatorWezTerm, cfgPath, 0.88); err != nil {
		t.Fatalf("SetTerminalOpacity failed: %v", err)
	}

	// 2. Read back
	info, err := ReadTerminalConfig(EmulatorWezTerm, cfgPath)
	if err != nil {
		t.Fatalf("ReadTerminalConfig failed: %v", err)
	}
	if info.FontFace != "Cascadia Code" {
		t.Errorf("expected 'Cascadia Code', got '%s'", info.FontFace)
	}
	if info.FontSize != 13.0 {
		t.Errorf("expected 13.0, got %f", info.FontSize)
	}
	if info.Opacity != 0.88 {
		t.Errorf("expected 0.88, got %f", info.Opacity)
	}

	// 3. Update existing config
	if err := SetTerminalFont(EmulatorWezTerm, cfgPath, "Hack Nerd Font", 14.5); err != nil {
		t.Fatalf("Update font failed: %v", err)
	}
	if err := SetTerminalOpacity(EmulatorWezTerm, cfgPath, 0.99); err != nil {
		t.Fatalf("Update opacity failed: %v", err)
	}

	info2, err := ReadTerminalConfig(EmulatorWezTerm, cfgPath)
	if err != nil {
		t.Fatalf("ReadTerminalConfig second pass failed: %v", err)
	}
	if info2.FontFace != "Hack Nerd Font" {
		t.Errorf("expected 'Hack Nerd Font', got '%s'", info2.FontFace)
	}
	if info2.FontSize != 14.5 {
		t.Errorf("expected 14.5, got %f", info2.FontSize)
	}
	if info2.Opacity != 0.99 {
		t.Errorf("expected 0.99, got %f", info2.Opacity)
	}
}

func TestCanonicalFontName(t *testing.T) {
	cases := map[string]string{
		"hack":               "Hack",
		"fira-code":          "FiraCode",
		"firacode":           "FiraCode",
		"jetbrains-mono":     "JetBrainsMono",
		"JetBrainsMono":      "JetBrainsMono",
		"cascadia-code":      "CascadiaCode",
		"MesloLGS":           "Meslo",
		"ubuntu-mono":        "UbuntuMono",
		"sauce-code-pro":     "SourceCodePro",
		"CustomFont":         "CustomFont",
	}

	for input, expected := range cases {
		out := CanonicalFontName(input)
		if out != expected {
			t.Errorf("CanonicalFontName(%q): expected %q, got %q", input, expected, out)
		}
	}
}

func TestInstallNerdFont_Empty(t *testing.T) {
	_, err := InstallNerdFont("")
	if err == nil {
		t.Errorf("expected error on empty font name")
	}
}
