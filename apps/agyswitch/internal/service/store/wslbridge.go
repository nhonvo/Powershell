package store

import (
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"runtime"
	"strings"
)

// ToWindowsPath converts WSL Linux path (/mnt/c/Users/name) to Windows path (C:\Users\name).
func ToWindowsPath(linuxPath string) string {
	clean := strings.TrimSpace(linuxPath)
	if clean == "" {
		return ""
	}
	clean = filepath.ToSlash(clean)

	// Check for /mnt/<drive>/... pattern
	if strings.HasPrefix(clean, "/mnt/") && len(clean) >= 6 {
		drive := strings.ToUpper(string(clean[5]))
		rest := clean[6:]
		winPath := drive + ":" + strings.ReplaceAll(rest, "/", "\\")
		return winPath
	}

	return filepath.FromSlash(clean)
}

// ToLinuxPath converts Windows path (C:\Users\name) to WSL Linux path (/mnt/c/Users/name).
func ToLinuxPath(winPath string) string {
	clean := strings.TrimSpace(winPath)
	if clean == "" {
		return ""
	}
	clean = strings.ReplaceAll(clean, "\\", "/")

	// Check for C:/... drive pattern
	if len(clean) >= 2 && clean[1] == ':' {
		drive := strings.ToLower(string(clean[0]))
		rest := clean[2:]
		return "/mnt/" + drive + rest
	}

	return clean
}

// GetCounterpartHome resolves the corresponding home directory on the other OS if available.
func GetCounterpartHome(userHome string) string {
	userHome = strings.TrimSpace(userHome)
	if userHome == "" {
		if h, err := os.UserHomeDir(); err == nil {
			userHome = h
		}
	}

	if runtime.GOOS == "linux" {
		// Running in WSL/Linux -> check for Windows host home in /mnt/c/Users/
		usersDir := "/mnt/c/Users"
		entries, err := os.ReadDir(usersDir)
		if err == nil {
			userBase := filepath.Base(userHome)
			// Priority 1: Match same folder name
			for _, e := range entries {
				if e.IsDir() && strings.EqualFold(e.Name(), userBase) {
					return filepath.Join(usersDir, e.Name())
				}
			}
			// Priority 2: Return first non-system user folder
			for _, e := range entries {
				name := strings.ToLower(e.Name())
				if e.IsDir() && name != "public" && name != "default" && name != "all users" && name != "desktop.ini" {
					return filepath.Join(usersDir, e.Name())
				}
			}
		}
	} else if runtime.GOOS == "windows" {
		// Running on Windows -> check for WSL home in \\wsl.localhost\Ubuntu\home\... or \\wsl$\Ubuntu\home\...
		distros := []string{"Ubuntu", "Ubuntu-24.04", "Ubuntu-22.04", "Debian"}
		userBase := filepath.Base(userHome)

		for _, dist := range distros {
			wslHomeBase := fmt.Sprintf("\\\\wsl.localhost\\%s\\home", dist)
			if fi, err := os.Stat(wslHomeBase); err == nil && fi.IsDir() {
				entries, err := os.ReadDir(wslHomeBase)
				if err == nil {
					for _, e := range entries {
						if e.IsDir() && (strings.EqualFold(e.Name(), userBase) || !IsIgnoredAccount(e.Name())) {
							return filepath.Join(wslHomeBase, e.Name())
						}
					}
				}
			}
		}
	}

	return ""
}

// ExecPythonScript attempts to execute python script using python3, python, py, or py.exe.
func ExecPythonScript(script string, args ...string) error {
	_, err := ExecPythonOutput(script, args...)
	return err
}

// ExecPythonOutput attempts to execute python script and return stdout using python3, python, py, or py.exe.
func ExecPythonOutput(script string, args ...string) ([]byte, error) {
	candidates := []string{"python3", "python", "py", "py.exe"}
	if runtime.GOOS == "windows" {
		candidates = []string{"python.exe", "python3.exe", "py.exe", "python", "python3"}
	}

	var lastErr error
	for _, bin := range candidates {
		cmdArgs := append([]string{"-c", script}, args...)
		cmd := exec.Command(bin, cmdArgs...)
		out, err := cmd.Output()
		if err == nil {
			return out, nil
		}
		lastErr = fmt.Errorf("%s failed: %v", bin, err)
	}
	return nil, lastErr
}
