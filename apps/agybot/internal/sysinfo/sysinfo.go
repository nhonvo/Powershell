package sysinfo

import (
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"runtime"
	"strconv"
	"strings"
	"time"
)

type SystemStatus struct {
	Hostname        string  `json:"hostname"`
	Platform        string  `json:"platform"`
	NumCPU          int     `json:"num_cpu"`
	UptimeString    string  `json:"uptime_string"`
	MemTotalGB      float64 `json:"mem_total_gb"`
	MemUsedGB       float64 `json:"mem_used_gb"`
	MemUsedPct      float64 `json:"mem_used_pct"`
	DiskTotalGB     float64 `json:"disk_total_gb"`
	DiskUsedGB      float64 `json:"disk_used_gb"`
	DiskUsedPct     float64 `json:"disk_used_pct"`
	ContainersUp    int     `json:"containers_up"`
	ContainersTotal int     `json:"containers_total"`
	BrainSessions   int     `json:"brain_sessions"`
}

// GetSystemStatus gathers real-time host and container health metrics
func GetSystemStatus() SystemStatus {
	hostname, _ := os.Hostname()
	st := SystemStatus{
		Hostname: hostname,
		Platform: fmt.Sprintf("%s/%s", runtime.GOOS, runtime.GOARCH),
		NumCPU:   runtime.NumCPU(),
	}

	// 1. Uptime & Memory from sysinfo / /proc/meminfo
	readMemoryMetrics(&st)

	// 2. Disk metrics for current partition
	readDiskMetrics(&st)

	// 3. Docker container inspection
	readDockerMetrics(&st)

	// 4. Antigravity Brain session counter
	home, _ := os.UserHomeDir()
	brainDir := filepath.Join(home, ".gemini", "antigravity-cli", "brain")
	if entries, err := os.ReadDir(brainDir); err == nil {
		count := 0
		for _, e := range entries {
			if e.IsDir() {
				count++
			}
		}
		st.BrainSessions = count
	}

	return st
}

func readMemoryMetrics(st *SystemStatus) {
	if data, err := os.ReadFile("/proc/meminfo"); err == nil {
		var totalKB, freeKB, availableKB, buffersKB, cachedKB float64
		lines := strings.Split(string(data), "\n")
		for _, l := range lines {
			fields := strings.Fields(l)
			if len(fields) >= 2 {
				val, _ := strconv.ParseFloat(fields[1], 64)
				switch fields[0] {
				case "MemTotal:":
					totalKB = val
				case "MemFree:":
					freeKB = val
				case "MemAvailable:":
					availableKB = val
				case "Buffers:":
					buffersKB = val
				case "Cached:":
					cachedKB = val
				}
			}
		}

		if totalKB > 0 {
			st.MemTotalGB = totalKB / (1024 * 1024)
			var usedKB float64
			if availableKB > 0 {
				usedKB = totalKB - availableKB
			} else {
				usedKB = totalKB - freeKB - buffersKB - cachedKB
			}
			st.MemUsedGB = usedKB / (1024 * 1024)
			st.MemUsedPct = (usedKB / totalKB) * 100
		}
	}

	if data, err := os.ReadFile("/proc/uptime"); err == nil {
		fields := strings.Fields(string(data))
		if len(fields) > 0 {
			if sec, err := strconv.ParseFloat(fields[0], 64); err == nil {
				d := time.Duration(sec) * time.Second
				days := int(d.Hours() / 24)
				hours := int(d.Hours()) % 24
				mins := int(d.Minutes()) % 60
				if days > 0 {
					st.UptimeString = fmt.Sprintf("%dd %dh %dm", days, hours, mins)
				} else {
					st.UptimeString = fmt.Sprintf("%dh %dm", hours, mins)
				}
			}
		}
	}
}



func readDockerMetrics(st *SystemStatus) {
	if out, err := exec.Command("docker", "ps", "-q").Output(); err == nil {
		lines := strings.Split(strings.TrimSpace(string(out)), "\n")
		if len(lines) == 1 && lines[0] == "" {
			st.ContainersUp = 0
		} else {
			st.ContainersUp = len(lines)
		}
	}
	if out, err := exec.Command("docker", "ps", "-a", "-q").Output(); err == nil {
		lines := strings.Split(strings.TrimSpace(string(out)), "\n")
		if len(lines) == 1 && lines[0] == "" {
			st.ContainersTotal = 0
		} else {
			st.ContainersTotal = len(lines)
		}
	}
}
