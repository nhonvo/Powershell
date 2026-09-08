package dockerops

import (
	"bufio"
	"encoding/json"
	"fmt"
	"os"
	"os/exec"
	"strconv"
	"strings"

	"agydocker/internal/model"
)

// ListContainers retrieves all Docker containers
func ListContainers() ([]model.ContainerInfo, error) {
	cmd := exec.Command("docker", "ps", "-a", "--format", "{{json .}}")
	out, err := cmd.Output()
	if err != nil {
		return nil, fmt.Errorf("docker ps error: %w", err)
	}

	var list []model.ContainerInfo
	scanner := bufio.NewScanner(strings.NewReader(string(out)))
	for scanner.Scan() {
		line := strings.TrimSpace(scanner.Text())
		if line == "" {
			continue
		}

		type rawDockerJson struct {
			model.ContainerInfo
			Labels string `json:"Labels"`
		}
		var raw rawDockerJson
		if err := json.Unmarshal([]byte(line), &raw); err == nil {
			c := raw.ContainerInfo
			c.IsRunning = strings.EqualFold(c.State, "running")

			// 1. Extract compose project from Labels if present
			if raw.Labels != "" {
				for _, part := range strings.Split(raw.Labels, ",") {
					part = strings.TrimSpace(part)
					if strings.HasPrefix(part, "com.docker.compose.project=") {
						c.ComposeProject = strings.TrimPrefix(part, "com.docker.compose.project=")
						break
					}
				}
			}

			// 2. Fallback heuristic from container names if no compose project label
			if c.ComposeProject == "" {
				if strings.Contains(c.Names, "_") {
					parts := strings.Split(c.Names, "_")
					if len(parts) >= 2 && parts[0] != "" {
						c.ComposeProject = parts[0]
					}
				} else if strings.Contains(c.Names, "-") {
					parts := strings.Split(c.Names, "-")
					if len(parts) >= 2 && parts[0] != "" {
						c.ComposeProject = parts[0]
					}
				}
			}

			if c.ComposeProject == "" {
				c.ComposeProject = "Standalone"
			}

			list = append(list, c)
		}
	}
	return list, nil
}

// StartContainer starts a stopped container
func StartContainer(id string) error {
	cmd := exec.Command("docker", "start", id)
	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("start failed: %s (%w)", strings.TrimSpace(string(out)), err)
	}
	return nil
}

// StopContainer stops a running container
func StopContainer(id string) error {
	cmd := exec.Command("docker", "stop", id)
	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("stop failed: %s (%w)", strings.TrimSpace(string(out)), err)
	}
	return nil
}

// RestartContainer restarts a container
func RestartContainer(id string) error {
	cmd := exec.Command("docker", "restart", id)
	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("restart failed: %s (%w)", strings.TrimSpace(string(out)), err)
	}
	return nil
}

// GetContainerLogs returns recent logs
func GetContainerLogs(id string, lines int) (string, error) {
	if lines <= 0 {
		lines = 50
	}
	cmd := exec.Command("docker", "logs", "--tail", strconv.Itoa(lines), id)
	out, err := cmd.CombinedOutput()
	return string(out), err
}

// GetMemoryInfo parses /proc/meminfo directly from Linux kernel
func GetMemoryInfo() (*model.MemInfo, error) {
	data, err := os.ReadFile("/proc/meminfo")
	if err != nil {
		return nil, fmt.Errorf("failed to read /proc/meminfo: %w", err)
	}

	mem := &model.MemInfo{}
	scanner := bufio.NewScanner(strings.NewReader(string(data)))
	for scanner.Scan() {
		line := scanner.Text()
		parts := strings.Fields(line)
		if len(parts) < 2 {
			continue
		}
		key := strings.TrimSuffix(parts[0], ":")
		val, _ := strconv.ParseUint(parts[1], 10, 64)

		switch key {
		case "MemTotal":
			mem.TotalKB = val
		case "MemFree":
			mem.FreeKB = val
		case "MemAvailable":
			mem.AvailableKB = val
		case "Buffers":
			mem.BuffersKB = val
		case "Cached":
			mem.CachedKB = val
		case "SwapTotal":
			mem.SwapTotalKB = val
		case "SwapFree":
			mem.SwapFreeKB = val
		}
	}

	if mem.TotalKB > 0 {
		if mem.AvailableKB > 0 && mem.AvailableKB <= mem.TotalKB {
			mem.UsedKB = mem.TotalKB - mem.AvailableKB
		} else {
			mem.UsedKB = mem.TotalKB - mem.FreeKB
		}
		mem.UsedPercent = (float64(mem.UsedKB) / float64(mem.TotalKB)) * 100.0
	}

	if mem.SwapTotalKB > 0 {
		if mem.SwapFreeKB <= mem.SwapTotalKB {
			mem.SwapUsedKB = mem.SwapTotalKB - mem.SwapFreeKB
		}
		mem.SwapUsedPercent = (float64(mem.SwapUsedKB) / float64(mem.SwapTotalKB)) * 100.0
	}

	return mem, nil
}

// PruneSystem frees unused Docker cache and stopped containers
func PruneSystem() (string, error) {
	cmd := exec.Command("docker", "system", "prune", "-f")
	out, err := cmd.CombinedOutput()
	return strings.TrimSpace(string(out)), err
}

// ListVolumes lists Docker volumes
func ListVolumes() ([]model.VolumeInfo, error) {
	cmd := exec.Command("docker", "volume", "ls", "--format", "{{json .}}")
	out, err := cmd.Output()
	if err != nil {
		return nil, err
	}

	var list []model.VolumeInfo
	scanner := bufio.NewScanner(strings.NewReader(string(out)))
	for scanner.Scan() {
		line := strings.TrimSpace(scanner.Text())
		if line == "" {
			continue
		}
		var v model.VolumeInfo
		if err := json.Unmarshal([]byte(line), &v); err == nil {
			list = append(list, v)
		}
	}
	return list, nil
}
