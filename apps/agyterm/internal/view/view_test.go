package view

import (
	"bytes"
	"strings"
	"testing"

	"agyterm/internal/model"
)

func TestApp_PrintStatus(t *testing.T) {
	app := NewApp()
	var buf bytes.Buffer
	app.PrintStatus(&buf)
	out := buf.String()
	if !strings.Contains(out, "AGYTERM") {
		t.Errorf("expected AGYTERM in output, got %s", out)
	}
}

func TestApp_Render(t *testing.T) {
	app := NewApp()
	app.ActiveTab = 0
	app.Render() // should not crash on themes tab

	app.searchMode = true
	app.searchQuery = "cat"
	app.Render() // should not crash in search mode

	app.searchQuery = "nonexistent_theme_xyz"
	app.Render() // should handle 0 matches gracefully

	app.ActiveTab = 1
	app.Render() // should not crash on fonts tab

	app.ActiveTab = 2
	app.Render() // should not crash on palette tab

	app.ActiveTab = 3
	app.Render() // should not crash on tools tab
}

func TestApp_FilteredThemes(t *testing.T) {
	app := NewApp()
	app.cachedThemes = []model.ThemeInfo{
		{Name: "catppuccin", Preview: "cat preview"},
		{Name: "dracula", Preview: "drac preview"},
		{Name: "dracula-blood", Preview: "blood preview"},
		{Name: "pure", Preview: "pure preview"},
	}

	// Empty query returns all
	app.searchQuery = ""
	all := app.getFilteredThemes()
	if len(all) != 4 {
		t.Fatalf("expected 4 themes, got %d", len(all))
	}

	// Case-insensitive query "DRAC"
	app.searchQuery = "DRAC"
	drac := app.getFilteredThemes()
	if len(drac) != 2 {
		t.Fatalf("expected 2 dracula themes, got %d", len(drac))
	}

	// Whitespace trimmed " pure "
	app.searchQuery = " pure "
	pure := app.getFilteredThemes()
	if len(pure) != 1 || pure[0].Name != "pure" {
		t.Fatalf("expected 1 pure theme, got %v", pure)
	}

	// No match
	app.searchQuery = "nonexistent"
	none := app.getFilteredThemes()
	if len(none) != 0 {
		t.Fatalf("expected 0 themes, got %d", len(none))
	}
}

