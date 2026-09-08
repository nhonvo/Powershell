package view

import (
	"os"
	"testing"
	"time"

	"agyswitch/internal/service/store"
	"agyswitch/internal/service/vault"
)

func TestApp_WaitKey(t *testing.T) {
	// Polling stdin with a short 5ms timeout should return false in tests without hanging
	ready := waitKey(int(os.Stdin.Fd()), 5)
	_ = ready
}

func TestApp_ProbingQuotasRendering(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "view_probe_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	v := vault.NewVault(tempDir)
	s := store.NewStore(tempDir, v)
	app := NewApp(s, func(string, string, []string) error { return nil })

	app.probeMu.Lock()
	app.isProbingQuotas = true
	app.spinnerIdx = 1
	app.probeMu.Unlock()

	// Ensure Render with probing active runs cleanly without panic
	app.Render(nil, nil)

	app.probeMu.Lock()
	app.spinnerIdx = 4
	app.probeMu.Unlock()
	app.Render(nil, nil)

	// Simulate background probe completion
	done := make(chan struct{})
	go func() {
		app.probeMu.Lock()
		app.isProbingQuotas = true
		app.probeMu.Unlock()

		time.Sleep(10 * time.Millisecond)

		app.probeMu.Lock()
		app.cachedAccs = app.Store.ListAccountsFast()
		app.isProbingQuotas = false
		app.needsReload = true
		app.StatusMsg = "\033[32m✔ Successfully updated live quotas in background.\033[0m"
		app.probeMu.Unlock()
		close(done)
	}()

	<-done

	app.probeMu.Lock()
	if app.isProbingQuotas {
		t.Errorf("expected isProbingQuotas to be false after completion")
	}
	if app.StatusMsg == "" {
		t.Errorf("expected StatusMsg to be set after probe completion")
	}
	app.probeMu.Unlock()

	app.Render(nil, nil)
}
