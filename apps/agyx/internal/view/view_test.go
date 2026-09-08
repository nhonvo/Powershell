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
	app.Render() // should not crash
}
