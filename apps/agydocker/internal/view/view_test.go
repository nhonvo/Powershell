package view

import (
	"bytes"
	"errors"
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

	app.setPendingAction("c1", "killing")
	app.setPendingAction("c2", "removing")
	app.Render()

	app.setPendingAction("c1", "")
	app.setPendingAction("c2", "")
	if app.hasPendingActions() {
		t.Errorf("expected pending actions cleared")
	}
}

func TestApp_DockerDaemonWarning(t *testing.T) {
	app := NewApp()
	app.dockerErr = errors.New("connect: no such file or directory")
	// Render with error banner
	app.Render()

	var buf bytes.Buffer
	app.PrintStatus(&buf)
	if !strings.Contains(buf.String(), "Docker daemon is offline") {
		t.Errorf("expected daemon offline banner in PrintStatus, got: %s", buf.String())
	}
}

func TestApp_DownActionAndAsyncReload(t *testing.T) {
	app := NewApp()
	app.ActiveTab = 0
	app.cachedContainers = []model.ContainerInfo{
		{ID: "c1", Names: "dev_tools_pgadmin", ComposeProject: "dev-tools", State: "exited", IsRunning: false},
		{ID: "c2", Names: "dev_tools_mongo_express", ComposeProject: "dev-tools", State: "running", IsRunning: true},
	}

	// Test downing a compose stack
	app.setPendingAction("dev-tools", "downing")
	if !app.hasPendingActions() {
		t.Errorf("expected pending actions true for dev-tools downing")
	}
	if app.getPendingAction("dev-tools") != "downing" {
		t.Errorf("expected dev-tools downing, got %s", app.getPendingAction("dev-tools"))
	}

	app.spinnerIdx = 2
	app.Render()

	// Test downing a container
	app.setPendingAction("c1", "downing")
	app.Render()

	app.setPendingAction("dev-tools", "")
	app.setPendingAction("c1", "")

	// Test reloadAsync
	app.reloadAsync()
	// Calling reloadAsync while one is in progress should not crash
	app.reloadAsync()
}




