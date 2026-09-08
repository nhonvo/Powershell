package view

import (
	"bytes"
	"strings"
	"testing"
)

func TestApp_PrintStatus(t *testing.T) {
	app := NewApp(t.TempDir())
	var buf bytes.Buffer
	app.PrintStatus(&buf)
	out := buf.String()
	if !strings.Contains(out, "AGYGIT") {
		t.Errorf("expected AGYGIT in output, got %s", out)
	}
}

func TestApp_Render(t *testing.T) {
	app := NewApp(t.TempDir())
	app.ActiveTab = 0
	app.Render() // should not crash in raw render
}
