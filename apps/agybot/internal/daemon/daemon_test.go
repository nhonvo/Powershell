package daemon

import (
	"os"
	"path/filepath"
	"testing"
)

func TestDaemon_PIDPaths(t *testing.T) {
	pidPath := PIDFilePath()
	if !filepath.IsAbs(pidPath) {
		t.Errorf("Expected absolute PID path, got %s", pidPath)
	}

	logPath := LogFilePath()
	if !filepath.IsAbs(logPath) {
		t.Errorf("Expected absolute Log path, got %s", logPath)
	}

	running, _ := IsRunning()
	t.Logf("Initial daemon running state: %v", running)

	// Test TailLogs on empty/missing log
	_, _ = TailLogs(10)

	// Test clean stop when not running
	if !running {
		_, err := Stop()
		if err == nil {
			t.Errorf("Expected error stopping non-running daemon")
		}
	}
}

func TestDaemon_StalePIDCleanup(t *testing.T) {
	tempPidFile := PIDFilePath()
	_ = os.MkdirAll(filepath.Dir(tempPidFile), 0755)
	_ = os.WriteFile(tempPidFile, []byte("9999999\n"), 0644)

	running, _ := IsRunning()
	if running {
		t.Errorf("PID 9999999 should not be running")
	}

	if _, err := os.Stat(tempPidFile); !os.IsNotExist(err) {
		t.Errorf("Expected stale PID file to be removed by IsRunning")
	}
}
