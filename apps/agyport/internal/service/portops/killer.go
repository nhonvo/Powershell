package portops

import (
	"fmt"
	"os"
	"os/exec"
	"runtime"
	"strconv"
	"syscall"
	"time"

	"agyport/internal/model"
)

// KillPort terminates the process listening on the specified port
func KillPort(port int, force bool) (*model.KillResult, error) {
	if IsSystemPort(port) && !force {
		return &model.KillResult{
			Port:    port,
			Success: false,
			Error:   fmt.Sprintf("port %d is a protected system service; pass --force to terminate", port),
		}, fmt.Errorf("port %d is a protected system service; pass --force to terminate", port)
	}

	p, err := FindPort(port)
	if err != nil {
		return &model.KillResult{
			Port:    port,
			Success: false,
			Error:   err.Error(),
		}, err
	}

	if p.PID <= 0 {
		return &model.KillResult{
			Port:        port,
			ProcessName: p.ProcessName,
			Success:     false,
			Error:       "no associated PID found for port (process may belong to another user or kernel)",
		}, fmt.Errorf("no associated PID found for port %d", port)
	}

	res := &model.KillResult{
		Port:        port,
		PID:         p.PID,
		ProcessName: p.ProcessName,
		Forced:      force,
	}

	err = KillPID(p.PID, force)
	if err != nil {
		res.Success = false
		res.Error = err.Error()
		return res, err
	}

	res.Success = true
	return res, nil
}

// KillPID terminates a process by PID with graceful SIGTERM and fallback to SIGKILL
func KillPID(pid int, force bool) error {
	if pid <= 1 {
		return fmt.Errorf("refusing to kill system root process (PID %d)", pid)
	}

	if runtime.GOOS == "windows" {
		args := []string{"/PID", strconv.Itoa(pid), "/T"}
		if force {
			args = append(args, "/F")
		}
		cmd := exec.Command("taskkill", args...)
		out, err := cmd.CombinedOutput()
		if err != nil {
			return fmt.Errorf("taskkill error: %s (%w)", string(out), err)
		}
		return nil
	}

	proc, err := os.FindProcess(pid)
	if err != nil {
		return fmt.Errorf("could not find process PID %d: %w", pid, err)
	}

	if force {
		return proc.Signal(syscall.SIGKILL)
	}

	// 1. Try graceful SIGTERM
	_ = proc.Signal(syscall.SIGTERM)

	// Wait up to 600ms to see if process exited
	for i := 0; i < 6; i++ {
		time.Sleep(100 * time.Millisecond)
		// Signal 0 tests if process still exists
		if err := proc.Signal(syscall.Signal(0)); err != nil {
			return nil // Process is gone
		}
	}

	// 2. Fallback to SIGKILL
	return proc.Signal(syscall.SIGKILL)
}

// KillPorts terminates multiple ports
func KillPorts(ports []int, force bool) ([]model.KillResult, error) {
	var results []model.KillResult
	for _, port := range ports {
		res, _ := KillPort(port, force)
		results = append(results, *res)
	}
	return results, nil
}

// KillAllDevPorts terminates all running developer/application ports
func KillAllDevPorts(force bool) ([]model.KillResult, error) {
	ports, err := ListPorts()
	if err != nil {
		return nil, err
	}

	var results []model.KillResult
	killedPIDs := make(map[int]bool)

	for _, p := range ports {
		// Never kill system ports (SSH 22, DNS 53, etc.)
		if p.IsSystemPort || p.PID <= 1 {
			continue
		}

		// Only target dev/user ports
		if killedPIDs[p.PID] {
			continue
		}

		res, _ := KillPort(p.Port, force)
		if res.Success {
			killedPIDs[p.PID] = true
		}
		results = append(results, *res)
	}

	return results, nil
}
