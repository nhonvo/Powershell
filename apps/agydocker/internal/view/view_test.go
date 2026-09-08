package view

import (
	"bytes"
	"strings"
	"testing"

	"agydocker/internal/model"
)

func TestApp_PrintStatus(t *testing.T) {
	app := NewApp()
	var buf bytes.Buffer
	app.PrintStatus(&buf)
	out := buf.String()
	if !strings.Contains(out, "AGYDOCKER") {
		t.Errorf("expected AGYDOCKER in output, got %s", out)
	}
}

func TestApp_Render(t *testing.T) {
	app := NewApp()
	app.ActiveTab = 0
	app.cachedContainers = []model.ContainerInfo{
		{ID: "c1", Names: "dev_tools_pgadmin", ComposeProject: "dev-tools", State: "exited", IsRunning: false},
		{ID: "c2", Names: "dev_tools_mongo_express", ComposeProject: "dev-tools", State: "running", IsRunning: true},
	}
	app.Render() // tab 0 grouped

	app.GroupViewMode = 1
	app.Render() // tab 0 flat

	app.ActiveTab = 1
	app.Render() // tab 1 RAM

	app.ActiveTab = 2
	app.Render() // tab 2 Volumes
}

func TestApp_PendingActions(t *testing.T) {
	app := NewApp()
	app.ActiveTab = 0
	app.cachedContainers = []model.ContainerInfo{
		{ID: "c1", Names: "dev_tools_pgadmin", ComposeProject: "dev-tools", State: "exited", IsRunning: false},
		{ID: "c2", Names: "dev_tools_mongo_express", ComposeProject: "dev-tools", State: "running", IsRunning: true},
	}

	if app.hasPendingActions() {
		t.Errorf("expected no pending actions initially")
	}

	app.setPendingAction("c1", "starting")
	app.setPendingAction("c2", "stopping")

	if !app.hasPendingActions() {
		t.Errorf("expected pending actions to be true")
	}

	if app.getPendingAction("c1") != "starting" {
		t.Errorf("expected c1 starting, got %s", app.getPendingAction("c1"))
	}
	if app.getPendingAction("c2") != "stopping" {
		t.Errorf("expected c2 stopping, got %s", app.getPendingAction("c2"))
	}

	app.spinnerIdx = 3
	app.Render() // should render with spinner frames without panic

	app.setPendingAction("c1", "")
	app.setPendingAction("c2", "")
	if app.hasPendingActions() {
		t.Errorf("expected pending actions cleared")
	}
}


