package daemon

import (
	"bufio"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strconv"
	"strings"
	"syscall"
	"time"
)

// PIDFilePath returns ~/.config/antigravity/agybot.pid
func PIDFilePath() string {
	home, _ := os.UserHomeDir()
	return filepath.Join(home, ".config", "antigravity", "agybot.pid")
}

// LogFilePath returns ~/.config/antigravity/agybot.log
func LogFilePath() string {
	home, _ := os.UserHomeDir()
	return filepath.Join(home, ".config", "antigravity", "agybot.log")
}

// IsRunning checks whether the daemon process is active and alive
func IsRunning() (bool, int) {
	pidPath := PIDFilePath()
	data, err := os.ReadFile(pidPath)
	if err != nil {
		return false, 0
	}

	pidStr := strings.TrimSpace(string(data))
	pid, err := strconv.Atoi(pidStr)
	if err != nil || pid <= 0 {
		return false, 0
	}

	// Probe process existence via signal 0
	process, err := os.FindProcess(pid)
	if err != nil {
		return false, 0
	}

	err = process.Signal(syscall.Signal(0))
	if err == nil {
		return true, pid
	}

	// Stale PID file
	_ = os.Remove(pidPath)
	return false, 0
}

// Start spawns the bot daemon as a detached background process
func Start(binPath string) (int, error) {
	if running, pid := IsRunning(); running {
		return pid, fmt.Errorf("daemon is already running with PID %d", pid)
	}

	if binPath == "" {
		if p, err := exec.LookPath("agybot"); err == nil {
			binPath = p
		} else {
			home, _ := os.UserHomeDir()
			binPath = filepath.Join(home, ".local", "bin", "agybot")
		}
	}

	logPath := LogFilePath()
	_ = os.MkdirAll(filepath.Dir(logPath), 0755)

	logFile, err := os.OpenFile(logPath, os.O_CREATE|os.O_WRONLY|os.O_APPEND, 0644)
	if err != nil {
		return 0, fmt.Errorf("failed to open log file %s: %w", logPath, err)
	}

	cmd := exec.Command(binPath, "daemon")
	cmd.Stdout = logFile
	cmd.Stderr = logFile
	cmd.Stdin = nil

	// Detach process from current terminal session
	setDaemonSysProcAttr(cmd)

	if err := cmd.Start(); err != nil {
		_ = logFile.Close()
		return 0, fmt.Errorf("failed to start daemon process: %w", err)
	}

	pid := cmd.Process.Pid
	_ = os.WriteFile(PIDFilePath(), []byte(strconv.Itoa(pid)+"\n"), 0644)
	_ = logFile.Close()

	// Wait 300ms to verify it didn't immediately crash
	time.Sleep(300 * time.Millisecond)
	if running, _ := IsRunning(); !running {
		return 0, fmt.Errorf("daemon started (PID %d) but exited prematurely. Check logs: %s", pid, logPath)
	}

	return pid, nil
}

// Stop gracefully terminates the daemon process
func Stop() (int, error) {
	running, pid := IsRunning()
	if !running {
		_ = os.Remove(PIDFilePath())
		return 0, fmt.Errorf("daemon is not running")
	}

	process, err := os.FindProcess(pid)
	if err != nil {
		_ = os.Remove(PIDFilePath())
		return 0, fmt.Errorf("cannot find process %d: %w", pid, err)
	}

	// 1. Send SIGTERM for graceful shutdown
	_ = process.Signal(syscall.SIGTERM)

	// Wait up to 3 seconds for exit
	for i := 0; i < 15; i++ {
		time.Sleep(200 * time.Millisecond)
		if err := process.Signal(syscall.Signal(0)); err != nil {
			_ = os.Remove(PIDFilePath())
			return pid, nil
		}
	}

	// 2. Force kill if still alive
	_ = process.Kill()
	_ = os.Remove(PIDFilePath())
	return pid, nil
}

// Restart stops and restarts the daemon
func Restart(binPath string) (int, error) {
	if running, _ := IsRunning(); running {
		_, _ = Stop()
		time.Sleep(500 * time.Millisecond)
	}
	return Start(binPath)
}

// TailLogs reads the last N lines from the daemon log file
func TailLogs(maxLines int) (string, error) {
	logPath := LogFilePath()
	f, err := os.Open(logPath)
	if err != nil {
		return "", fmt.Errorf("cannot open log file: %w", err)
	}
	defer f.Close()

	var lines []string
	scanner := bufio.NewScanner(f)
	for scanner.Scan() {
		lines = append(lines, scanner.Text())
	}

	if len(lines) > maxLines {
		lines = lines[len(lines)-maxLines:]
	}

	return strings.Join(lines, "\n"), nil
}
