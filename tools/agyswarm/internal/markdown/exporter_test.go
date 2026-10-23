package markdown_test

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
	"time"

	"agyswarm/internal/markdown"
	"agyswarm/internal/model"
)

func TestExporter_ExportAgentSession(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "agyswarm_md_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	exporter := markdown.NewExporter(tempDir)

	sess := &model.AgentSession{
		ID:           "agent-1",
		Name:         "Lead-Researcher",
		AccountName:  "research_acc",
		WorkspaceDir: "/home/user/project",
		Command:      "echo",
		Args:         []string{"done"},
		Status:       model.StatusDone,
		PID:          12345,
		CreatedAt:    time.Now(),
		Lines:        []string{"\x1b[32mScanning web...\x1b[0m", "Found 5 citations."},
	}

	targetFile, err := exporter.ExportAgentSession(sess)
	if err != nil {
		t.Fatalf("failed to export session: %v", err)
	}

	content, err := os.ReadFile(targetFile)
	if err != nil {
		t.Fatalf("failed to read exported file: %v", err)
	}

	str := string(content)
	if !strings.Contains(str, "# Agent Session Dossier: Lead-Researcher") {
		t.Errorf("missing header in exported file")
	}
	if !strings.Contains(str, "Scanning web...") {
		t.Errorf("missing cleaned transcript in exported file")
	}
	// Check ANSI was stripped
	if strings.Contains(str, "\x1b[32m") {
		t.Errorf("ANSI codes were not stripped")
	}
}

func TestExporter_ExportSwarmTask(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "agyswarm_task_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	exporter := markdown.NewExporter(tempDir)

	task := &model.SwarmTask{
		ID:            "task-101",
		Goal:          "Analyze market trends",
		TargetProject: "finance-dashboard",
		Status:        model.StatusDone,
		CreatedAt:     time.Now(),
	}

	artifacts := []model.BlackboardArtifact{
		{
			ID:        "art-1",
			AgentID:   "agent-1",
			Topic:     "market_summary.md",
			Markdown:  "## Key Findings\nBullish sentiment.",
			Timestamp: time.Now(),
		},
	}

	sessions := []*model.AgentSession{
		{
			ID:          "agent-1",
			Name:        "Researcher",
			AccountName: "acc1",
			Status:      model.StatusDone,
			Lines:       []string{"Analysis complete."},
		},
	}

	targetFile, err := exporter.ExportSwarmTask(task, sessions, artifacts)
	if err != nil {
		t.Fatalf("failed to export swarm task: %v", err)
	}

	content, err := os.ReadFile(targetFile)
	if err != nil {
		t.Fatalf("failed to read exported file: %v", err)
	}

	str := string(content)
	if !strings.Contains(str, "Swarm Task Dossier: Analyze market trends") {
		t.Errorf("missing swarm task title")
	}
	if !strings.Contains(str, "market_summary.md") {
		t.Errorf("missing blackboard artifact")
	}
	if !filepath.IsAbs(targetFile) && !strings.HasPrefix(targetFile, tempDir) {
		t.Errorf("target file not in tempDir")
	}
}
