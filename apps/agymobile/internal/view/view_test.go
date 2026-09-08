package view

import (
	"bytes"
	"strings"
	"testing"
)

func TestApp_PrintStatus(t *testing.T) {
	app := NewApp()
	var buf bytes.Buffer
	app.PrintStatus(&buf)
	out := buf.String()
	if !strings.Contains(out, "AGYMOBILE") {
		t.Errorf("expected AGYMOBILE in output, got %s", out)
	}
}

func TestApp_Render(t *testing.T) {
	app := NewApp()
	app.ActiveTab = 0
	app.Render() // should not panic on main tab

	app.ActiveTab = 1
	app.Render() // should not panic on tailscale tab

	app.ActiveTab = 2
	app.Render() // should not panic on logs tab
}

func TestTruncate(t *testing.T) {
	if truncate("hello", 3) != "hel" {
		t.Errorf("expected hel, got %s", truncate("hello", 3))
	}
	if truncate("hello world", 8) != "hello..." {
		t.Errorf("expected hello..., got %s", truncate("hello world", 8))
	}
}
