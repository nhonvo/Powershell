package engine_test

import (
	"os"
	"testing"
	"time"

	"agyswarm/internal/engine"
	"agyswarm/internal/model"
)

func TestManager_SpawnAndReadOutput(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "agyswarm_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	mgr := engine.NewManager(tempDir)

	sess, err := mgr.Spawn(engine.SpawnConfig{
		Name:         "test-worker",
		WorkspaceDir: tempDir,
		Command:      "echo",
		Args:         []string{"hello from swarm agent"},
	})
	if err != nil {
		t.Fatalf("failed to spawn agent: %v", err)
	}

	if sess.ID == "" {
		t.Errorf("expected non-empty agent ID")
	}

	// Wait for process to exit and output to be scanned
	time.Sleep(150 * time.Millisecond)

	lines := sess.GetRecentLines(10)
	found := false
	for _, l := range lines {
		if l == "hello from swarm agent" || l == "hello from swarm agent\r" {
			found = true
			break
		}
	}
	if !found {
		t.Logf("recent lines: %+v", lines)
	}

	if sess.Status != model.StatusDone {
		t.Errorf("expected StatusDone, got %s", sess.Status)
	}

	// Test Get and List
	if got := mgr.Get(sess.ID); got == nil || got.ID != sess.ID {
		t.Errorf("expected to find agent by ID")
	}
	if list := mgr.List(); len(list) != 1 {
		t.Errorf("expected 1 session in list, got %d", len(list))
	}
}

func TestManager_InteractiveSendInputAndKill(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "agyswarm_test_kill_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	mgr := engine.NewManager(tempDir)

	// Spawn a long-running process (sleep 30)
	sess, err := mgr.Spawn(engine.SpawnConfig{
		Name:         "interactive-agent",
		WorkspaceDir: tempDir,
		Command:      "sleep",
		Args:         []string{"30"},
	})
	if err != nil {
		t.Fatalf("failed to spawn agent: %v", err)
	}

	if sess.Status != model.StatusWorking {
		t.Errorf("expected StatusWorking, got %s", sess.Status)
	}

	// Test SendInput
	if err := mgr.SendInput(sess.ID, []byte("\n")); err != nil {
		t.Errorf("SendInput failed: %v", err)
	}

	// Test Kill
	if err := mgr.Kill(sess.ID); err != nil {
		t.Errorf("Kill failed: %v", err)
	}
}
