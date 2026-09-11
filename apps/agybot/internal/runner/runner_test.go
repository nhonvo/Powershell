package runner

import (
	"context"
	"strings"
	"testing"
	"time"

	"agybot/internal/security"
)

func TestRunner_SafetyBlock(t *testing.T) {
	guard := security.NewSecurityGuard()
	r := NewAntigravityRunner(guard)

	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	ch, err := r.ExecutePrompt(ctx, RunnerOptions{
		Prompt: "format c: /fs:ntfs",
	})
	if err != nil {
		t.Fatalf("Unexpected error starting prompt: %v", err)
	}

	ev := <-ch
	if ev.Type != "error" || !strings.Contains(ev.Content, "Blocked by Safety Firewall") {
		t.Errorf("Expected safety firewall block, got %v: %s", ev.Type, ev.Content)
	}
}

func TestRunner_FormatToolNotification(t *testing.T) {
	r := NewAntigravityRunner(nil)

	res := r.FormatToolNotification("run_command", map[string]interface{}{
		"CommandLine": "git status",
	})
	if !strings.Contains(res, "Running command") || !strings.Contains(res, "git status") {
		t.Errorf("Unexpected tool notification: %s", res)
	}
}

func TestRunner_AvailableModels(t *testing.T) {
	r := NewAntigravityRunner(nil)
	models := r.AvailableModels()
	if len(models) == 0 {
		t.Fatalf("Expected available models")
	}
}
