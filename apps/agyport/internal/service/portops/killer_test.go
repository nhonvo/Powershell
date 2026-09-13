package portops

import (
	"os/exec"
	"testing"
	"time"
)

func TestKillSystemPortProtection(t *testing.T) {
	// Attempting to kill system port 22 or 53 without force must fail safely
	_, err := KillPort(22, false)
	if err == nil {
		t.Errorf("expected KillPort(22, false) to fail with protected system port error, got nil")
	}

	_, err = KillPort(53, false)
	if err == nil {
		t.Errorf("expected KillPort(53, false) to fail with protected system port error, got nil")
	}
}

func TestKillPID1Protection(t *testing.T) {
	err := KillPID(1, false)
	if err == nil {
		t.Errorf("expected KillPID(1) to fail with root protection, got nil")
	}
}

func TestKillPIDRealProcess(t *testing.T) {
	// Spawn a dummy sleeper process
	cmd := exec.Command("sleep", "60")
	if err := cmd.Start(); err != nil {
		t.Skipf("skipping sleep test: %v", err)
	}

	pid := cmd.Process.Pid
	if pid <= 0 {
		t.Fatalf("invalid spawned PID: %d", pid)
	}

	// Terminate the process using KillPID
	err := KillPID(pid, false)
	if err != nil {
		t.Fatalf("KillPID(%d) failed: %v", pid, err)
	}

	// Verify the process is dead
	time.Sleep(200 * time.Millisecond)
	_ = cmd.Wait()
}
