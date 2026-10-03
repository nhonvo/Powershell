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

	app.ActiveTab = 2
	app.Render() // should not crash on git

	app.ActiveTab = 3
	app.Render() // should not crash on swarm tab

	app.ActiveTab = 4
	app.ToolSubIndex = 0 // Docker
	app.Render()

	app.ToolSubIndex = 1 // Ports
	app.Render()

	app.ToolSubIndex = 2 // Ollama
	app.Render()

	app.ToolSubIndex = 6 // AWS cheat sheet
	app.Render()        // should not crash on tools tab

	tool := app.getActiveToolBinary()
	if tool != "aws" {
		t.Errorf("expected active tool 'aws', got %s", tool)
	}

	app.ToolSubIndex = 7 // Code Reviewer
	app.Render()
	tool = app.getActiveToolBinary()
	if tool != "agyreview" {
		t.Errorf("expected active tool 'agyreview', got %s", tool)
	}

	app.ActiveTab = 3
	if app.getActiveToolBinary() != "agyswarm" {
		t.Errorf("expected active tool 'agyswarm', got %s", app.getActiveToolBinary())
	}
}
