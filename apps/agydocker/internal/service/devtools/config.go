package devtools

import (
	"encoding/json"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
	"time"

	"agydocker/internal/model"
)

type ConfigSettings struct {
	DevToolsPath string `json:"dev_tools_path"`
}

// GetDevToolsDir resolves the root directory of the centralized dev-tools stack.
func GetDevToolsDir() string {
	if envPath := os.Getenv("AGY_DEV_TOOLS_PATH"); envPath != "" {
		if info, err := os.Stat(envPath); err == nil && info.IsDir() {
			return envPath
		}
	}

	home, err := os.UserHomeDir()
	if err == nil {
		cfgPath := filepath.Join(home, ".config", "antigravity", "devtools.json")
		if data, err := os.ReadFile(cfgPath); err == nil {
			var settings ConfigSettings
			if json.Unmarshal(data, &settings) == nil && settings.DevToolsPath != "" {
				if info, err := os.Stat(settings.DevToolsPath); err == nil && info.IsDir() {
					return settings.DevToolsPath
				}
			}
		}

		defaultCandidate := filepath.Join(home, "projects", "dev-tools")
		if info, err := os.Stat(defaultCandidate); err == nil && info.IsDir() {
			return defaultCandidate
		}
	}

	// Canonical fallback
	fallback := "/home/truongnhon/projects/dev-tools"
	if info, err := os.Stat(fallback); err == nil && info.IsDir() {
		return fallback
	}

	return ""
}

// SetDevToolsDir saves a custom path into ~/.config/antigravity/devtools.json
func SetDevToolsDir(dir string) error {
	abs, err := filepath.Abs(dir)
	if err != nil {
		return err
	}
	if info, err := os.Stat(abs); err != nil || !info.IsDir() {
		return fmt.Errorf("directory does not exist: %s", abs)
	}

	home, err := os.UserHomeDir()
	if err != nil {
		return err
	}
	configDir := filepath.Join(home, ".config", "antigravity")
	if err := os.MkdirAll(configDir, 0755); err != nil {
		return err
	}

	settings := ConfigSettings{DevToolsPath: abs}
	data, err := json.MarshalIndent(settings, "", "  ")
	if err != nil {
		return err
	}
	return os.WriteFile(filepath.Join(configDir, "devtools.json"), data, 0644)
}

// GetStackStatus inspects live container and configuration status of the centralized dev-tools stack.
func GetStackStatus() (*model.DevToolsStackStatus, error) {
	dir := GetDevToolsDir()
	if dir == "" {
		return nil, fmt.Errorf("centralized dev-tools stack not found (expected at /home/truongnhon/projects/dev-tools or set AGY_DEV_TOOLS_PATH)")
	}

	composeFile := filepath.Join(dir, "docker-compose.yml")
	status := &model.DevToolsStackStatus{
		RootDir:     dir,
		ComposeFile: composeFile,
		PgAdminURL:  "http://localhost:5050",
		MongoURL:    "http://localhost:8082",
		LastChecked: time.Now(),
	}

	// Probe pgAdmin container
	cmd := exec.Command("docker", "ps", "--filter", "name=dev_tools_pgadmin", "--format", "{{.ID}}|{{.Status}}")
	if out, err := cmd.Output(); err == nil {
		text := strings.TrimSpace(string(out))
		if text != "" {
			parts := strings.Split(text, "|")
			status.PgAdminContainerID = parts[0]
			status.PgAdminRunning = strings.Contains(strings.ToLower(text), "up")
		}
	}

	// Probe Mongo Express container
	cmd2 := exec.Command("docker", "ps", "--filter", "name=dev_tools_mongo_express", "--format", "{{.ID}}|{{.Status}}")
	if out, err := cmd2.Output(); err == nil {
		text := strings.TrimSpace(string(out))
		if text != "" {
			parts := strings.Split(text, "|")
			status.MongoContainerID = parts[0]
			status.MongoRunning = strings.Contains(strings.ToLower(text), "up")
		}
	}

	// Read registered servers from servers.json
	servers, err := ReadServers(dir)
	if err == nil {
		status.RegisteredServers = servers
	}

	return status, nil
}

// StartStack runs 'docker compose up -d' in the dev-tools directory and reloads servers.
func StartStack() (string, error) {
	dir := GetDevToolsDir()
	if dir == "" {
		return "", fmt.Errorf("centralized dev-tools directory not found")
	}

	cmd := exec.Command("docker", "compose", "-f", filepath.Join(dir, "docker-compose.yml"), "up", "-d")
	cmd.Dir = dir
	out, err := cmd.CombinedOutput()
	if err != nil {
		return string(out), fmt.Errorf("failed to start dev-tools stack: %w (%s)", err, string(out))
	}

	// Allow container init before hot-reloading servers
	time.Sleep(1500 * time.Millisecond)
	_ = ReloadPgAdminServers()

	return string(out), nil
}

// StopStack runs 'docker compose down' in the dev-tools directory.
func StopStack() (string, error) {
	dir := GetDevToolsDir()
	if dir == "" {
		return "", fmt.Errorf("centralized dev-tools directory not found")
	}

	cmd := exec.Command("docker", "compose", "-f", filepath.Join(dir, "docker-compose.yml"), "down")
	cmd.Dir = dir
	out, err := cmd.CombinedOutput()
	if err != nil {
		return string(out), fmt.Errorf("failed to stop dev-tools stack: %w (%s)", err, string(out))
	}
	return string(out), nil
}

// RestartStack restarts the dev-tools stack and reloads server configurations.
func RestartStack() (string, error) {
	_, _ = StopStack()
	time.Sleep(500 * time.Millisecond)
	return StartStack()
}
