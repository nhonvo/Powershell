package view

import (
	"bytes"
	"strings"
	"testing"

	"agyreview/internal/model"
)

func TestCockpit_Status(t *testing.T) {
	cfg := model.DefaultConfig()
	app := NewCockpitApp(cfg)
	var buf bytes.Buffer
	app.PrintStatus(&buf)

	out := buf.String()
	if !strings.Contains(out, "AGYREVIEW") {
		t.Errorf("expected AGYREVIEW in output, got: %s", out)
	}
}

func TestCockpit_RenderTabs(t *testing.T) {
	cfg := model.DefaultConfig()
	app := NewCockpitApp(cfg)

	app.ActiveTab = 0
	app.Render()

	app.ActiveTab = 1
	app.Render()

	app.ActiveTab = 2
	app.Render()
}
