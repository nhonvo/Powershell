package view

import (
	"bytes"
	"strings"
	"testing"
)

func TestCockpit_PrintStatus(t *testing.T) {
	app := NewCockpitApp()
	var buf bytes.Buffer
	app.PrintStatus(&buf)
	out := buf.String()
	if !strings.Contains(out, "AGYX") {
		t.Errorf("expected AGYX in output, got %s", out)
	}
}

func TestCockpit_Render(t *testing.T) {
	app := NewCockpitApp()
	app.ActiveTab = 1
	app.Render() // should not crash on proj

	app.ActiveTab = 4
	app.Render() // should not crash on ports tab

	app.ActiveTab = 5
	app.Render() // should not crash on ollama tab

	app.ActiveTab = 6
	app.ToolSubIndex = 2 // AWS cheat sheet
	app.Render()        // should not crash on tools tab

	tool := app.getActiveToolBinary()
	if tool != "aws" {
		t.Errorf("expected active tool 'aws', got %s", tool)
	}
}
