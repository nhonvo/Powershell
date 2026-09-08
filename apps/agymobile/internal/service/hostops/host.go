package hostops

import (
	"bufio"
	"fmt"
	"os"
	"os/exec"
	"strconv"
	"strings"

	"agymobile/internal/model"
)

// GetHostStats reads /proc/meminfo and /proc/loadavg
func GetHostStats() model.HostStats {
	stats := model.HostStats{}

	// 1. Read MemInfo
	if data, err := os.ReadFile("/proc/meminfo"); err == nil {
		var totalKB, freeKB, availKB uint64
		scanner := bufio.NewScanner(strings.NewReader(string(data)))
		for scanner.Scan() {
			fields := strings.Fields(scanner.Text())
			if len(fields) >= 2 {
				key := strings.TrimSuffix(fields[0], ":")
				val, _ := strconv.ParseUint(fields[1], 10, 64)
				switch key {
				case "MemTotal":
					totalKB = val
				case "MemFree":
					freeKB = val
				case "MemAvailable":
					availKB = val
				}
			}
		}

		stats.TotalRAMMB = totalKB / 1024
		stats.FreeRAMMB = freeKB / 1024
		if availKB > 0 && availKB <= totalKB {
			stats.UsedRAMMB = (totalKB - availKB) / 1024
		} else {
			stats.UsedRAMMB = (totalKB - freeKB) / 1024
		}

		if stats.TotalRAMMB > 0 {
			stats.RAMUsedPct = (float64(stats.UsedRAMMB) / float64(stats.TotalRAMMB)) * 100.0
		}
	}

	// 2. Read LoadAvg
	if data, err := os.ReadFile("/proc/loadavg"); err == nil {
		parts := strings.Fields(string(data))
		if len(parts) >= 3 {
			stats.LoadAvg1, _ = strconv.ParseFloat(parts[0], 64)
			stats.LoadAvg5, _ = strconv.ParseFloat(parts[1], 64)
			stats.LoadAvg15, _ = strconv.ParseFloat(parts[2], 64)
		}
	}

	return stats
}

// DropCaches writes 3 to /proc/sys/vm/drop_caches to reclaim Linux/WSL2 buffer memory
func DropCaches() error {
	cmd := exec.Command("sudo", "sync")
	_ = cmd.Run()

	cmd = exec.Command("sudo", "sh", "-c", "echo 3 > /proc/sys/vm/drop_caches")
	out, err := cmd.CombinedOutput()
	if err != nil {
		// Fallback without sudo if already running as root
		if fErr := os.WriteFile("/proc/sys/vm/drop_caches", []byte("3\n"), 0644); fErr != nil {
			return fmt.Errorf("drop_caches failed: %s (%w)", strings.TrimSpace(string(out)), err)
		}
	}
	return nil
}
