package view

import (
	"strings"
	"testing"

	"agybot/internal/account"
	"agybot/internal/auth"
	"agybot/internal/config"
	"agybot/internal/workspace"
)

func TestDashboardView_RenderOverview(t *testing.T) {
	cfg := &config.Config{
		DefaultModel: "Gemini 3.7 Flash",
		DefaultMode:  "accept-edits",
	}
	authMgr := auth.NewAuthManager("", 3, 0, 0, nil)
	wsMgr := workspace.NewWorkspaceManager("/tmp")
	accMgr := account.NewAccountManager()

	v := NewDashboardView(cfg, authMgr, wsMgr, accMgr)
	rendered := v.RenderOverview()

	if !strings.Contains(rendered, "AGYBOT") {
		t.Errorf("Expected AGYBOT header in render")
	}
	if !strings.Contains(rendered, "Gemini 3.7 Flash") {
		t.Errorf("Expected Gemini 3.7 Flash in render")
	}
}

func TestDashboardView_RenderTabs(t *testing.T) {
	cfg := &config.Config{
		DefaultModel: "Gemini 3.7 Flash",
		DefaultMode:  "accept-edits",
	}
	authMgr := auth.NewAuthManager("", 3, 0, 0, nil)
	wsMgr := workspace.NewWorkspaceManager("/tmp")
	accMgr := account.NewAccountManager()

	v := NewDashboardView(cfg, authMgr, wsMgr, accMgr)

	for tab := 0; tab < 5; tab++ {
		v.ActiveTab = tab
		v.tabSwitched = true
		v.Render() // Should not crash
	}
}
