package ramops

import (
	"fmt"
	"os"
	"os/user"
	"path/filepath"
	"runtime"
	"sort"
	"strconv"
	"strings"
	"sync"
	"time"

	"agyport/internal/model"
	"agyport/internal/service/portops"
)

var (
	pageSizeBytes = uint64(os.Getpagesize())

	ramCacheMutex  sync.Mutex
	cachedMem      *model.MemorySummary
	lastMemScan    time.Time
	cachedProcs    []model.ProcessMemInfo
	lastProcsScan  time.Time
	ramCacheTTL    = 2 * time.Second
)

// GetMemorySummary inspects host RAM usage (Windows API on Windows, /proc/meminfo on Linux)
func GetMemorySummary() (*model.MemorySummary, error) {
	ramCacheMutex.Lock()
	if time.Since(lastMemScan) < ramCacheTTL && cachedMem != nil {
		memCopy := *cachedMem
		ramCacheMutex.Unlock()
		return &memCopy, nil
	}
	ramCacheMutex.Unlock()

	if runtime.GOOS == "windows" {
		if winMem, err := portops.GetWindowsMemorySummary(); err == nil && winMem != nil {
			ramCacheMutex.Lock()
			cachedMem = winMem
			lastMemScan = time.Now()
			ramCacheMutex.Unlock()
			return winMem, nil
		}
	}

	data, err := os.ReadFile("/proc/meminfo")
	if err != nil {
		return nil, fmt.Errorf("could not read /proc/meminfo: %w", err)
	}

	mem := &model.MemorySummary{}
	lines := strings.Split(string(data), "\n")
	for _, l := range lines {
		fields := strings.Fields(l)
		if len(fields) < 2 {
			continue
		}
		valKB, _ := strconv.ParseUint(fields[1], 10, 64)
		valBytes := valKB * 1024

		switch fields[0] {
		case "MemTotal:":
			mem.TotalBytes = valBytes
		case "MemFree:":
			mem.FreeBytes = valBytes
		case "MemAvailable:":
			mem.AvailableBytes = valBytes
		case "Buffers:":
			mem.BuffersBytes = valBytes
		case "Cached:":
			mem.CachedBytes = valBytes
		case "SwapTotal:":
			mem.SwapTotalBytes = valBytes
		case "SwapFree:":
			mem.SwapFreeBytes = valBytes
		}
	}

	if mem.TotalBytes > 0 {
		if mem.AvailableBytes > 0 && mem.AvailableBytes <= mem.TotalBytes {
			mem.UsedBytes = mem.TotalBytes - mem.AvailableBytes
		} else {
			mem.UsedBytes = mem.TotalBytes - mem.FreeBytes
		}
		mem.UsedPercent = (float64(mem.UsedBytes) / float64(mem.TotalBytes)) * 100.0
	}

	if mem.SwapTotalBytes > 0 {
		if mem.SwapFreeBytes <= mem.SwapTotalBytes {
			mem.SwapUsedBytes = mem.SwapTotalBytes - mem.SwapFreeBytes
		}
		mem.SwapUsedPercent = (float64(mem.SwapUsedBytes) / float64(mem.SwapTotalBytes)) * 100.0
	}

	mem.TotalFormatted = model.FormatBytes(mem.TotalBytes)
	mem.UsedFormatted = model.FormatBytes(mem.UsedBytes)
	mem.AvailableFormatted = model.FormatBytes(mem.AvailableBytes)
	mem.CachedFormatted = model.FormatBytes(mem.CachedBytes + mem.BuffersBytes)
	mem.SwapUsedFormatted = model.FormatBytes(mem.SwapUsedBytes)

	ramCacheMutex.Lock()
	cachedMem = mem
	lastMemScan = time.Now()
	ramCacheMutex.Unlock()

	return mem, nil
}

// GetTopMemoryProcesses returns the highest RAM-consuming processes
func GetTopMemoryProcesses(limit int) ([]model.ProcessMemInfo, error) {
	ramCacheMutex.Lock()
	if time.Since(lastProcsScan) < ramCacheTTL && cachedProcs != nil {
		procsCopy := make([]model.ProcessMemInfo, len(cachedProcs))
		copy(procsCopy, cachedProcs)
		ramCacheMutex.Unlock()
		if limit > 0 && len(procsCopy) > limit {
			return procsCopy[:limit], nil
		}
		return procsCopy, nil
	}
	ramCacheMutex.Unlock()

	memSummary, _ := GetMemorySummary()
	totalMem := uint64(1)
	if memSummary != nil && memSummary.TotalBytes > 0 {
		totalMem = memSummary.TotalBytes
	}

	// Map active listening ports to PIDs
	activePorts, _ := portops.ListPorts()
	pidToPorts := make(map[int][]int)
	for _, p := range activePorts {
		if p.PID > 0 {
			pidToPorts[p.PID] = append(pidToPorts[p.PID], p.Port)
		}
	}

	var procs []model.ProcessMemInfo

	if runtime.GOOS == "windows" {
		winMap := portops.GetWindowsProcessesMap()
		for pid, d := range winMap {
			if d.MemoryBytes < 1024*1024 {
				continue // Skip negligible processes under 1MB
			}
			isDev := isDevProcess(d.Name, d.CommandLine)
			ports := pidToPorts[pid]
			projName := portops.InferProjectName(d.CommandLine, d.Name)

			procs = append(procs, model.ProcessMemInfo{
				PID:          pid,
				Name:         d.Name,
				ProjectName:  projName,
				User:         d.User,
				Cmdline:      d.CommandLine,
				RSSBytes:     d.MemoryBytes,
				RSSFormatted: model.FormatBytes(d.MemoryBytes),
				RSSPercent:   (float64(d.MemoryBytes) / float64(totalMem)) * 100.0,
				Ports:        ports,
				IsDevServer:  isDev,
				Framework:    detectFrameworkFromCmd(d.Name, d.CommandLine),
			})
		}
	} else {
		entries, err := os.ReadDir("/proc")
		if err == nil {
			for _, e := range entries {
				if !e.IsDir() {
					continue
				}
				pid, err := strconv.Atoi(e.Name())
				if err != nil || pid <= 1 {
					continue
				}

				statmBytes, err := os.ReadFile(fmt.Sprintf("/proc/%d/statm", pid))
				if err != nil {
					continue
				}
				fields := strings.Fields(string(statmBytes))
				if len(fields) < 2 {
					continue
				}
				residentPages, _ := strconv.ParseUint(fields[1], 10, 64)
				rssBytes := residentPages * pageSizeBytes
				if rssBytes < 1024*1024 {
					continue
				}

				cmdline := ""
				if cmdBytes, err := os.ReadFile(fmt.Sprintf("/proc/%d/cmdline", pid)); err == nil {
					parts := strings.Split(string(cmdBytes), "\x00")
					var clean []string
					for _, p := range parts {
						if strings.TrimSpace(p) != "" {
							clean = append(clean, p)
						}
					}
					cmdline = strings.Join(clean, " ")
				}

				name := ""
				userName := ""
				if statusBytes, err := os.ReadFile(fmt.Sprintf("/proc/%d/status", pid)); err == nil {
					for _, l := range strings.Split(string(statusBytes), "\n") {
						if strings.HasPrefix(l, "Name:") {
							name = strings.TrimSpace(strings.TrimPrefix(l, "Name:"))
						} else if strings.HasPrefix(l, "Uid:") {
							f := strings.Fields(l)
							if len(f) >= 2 {
								if u, err := user.LookupId(f[1]); err == nil {
									userName = u.Username
								} else {
									userName = f[1]
								}
							}
						}
					}
				}

				if name == "" && cmdline != "" {
					name = filepath.Base(strings.Fields(cmdline)[0])
				}

				isDev := isDevProcess(name, cmdline)
				ports := pidToPorts[pid]
				projName := portops.InferProjectName(cmdline, name)

				procs = append(procs, model.ProcessMemInfo{
					PID:          pid,
					Name:         name,
					ProjectName:  projName,
					User:         userName,
					Cmdline:      cmdline,
					RSSBytes:     rssBytes,
					RSSFormatted: model.FormatBytes(rssBytes),
					RSSPercent:   (float64(rssBytes) / float64(totalMem)) * 100.0,
					Ports:        ports,
					IsDevServer:  isDev,
					Framework:    detectFrameworkFromCmd(name, cmdline),
				})
			}
		}
	}

	sort.Slice(procs, func(i, j int) bool {
		return procs[i].RSSBytes > procs[j].RSSBytes
	})

	ramCacheMutex.Lock()
	cachedProcs = make([]model.ProcessMemInfo, len(procs))
	copy(cachedProcs, procs)
	lastProcsScan = time.Now()
	ramCacheMutex.Unlock()

	if limit > 0 && len(procs) > limit {
		procs = procs[:limit]
	}

	return procs, nil
}

// GenerateRAMSuggestions returns empty slice (suggestions disabled)
func GenerateRAMSuggestions(ports []model.PortInfo, procs []model.ProcessMemInfo, mem *model.MemorySummary) []model.RAMSuggestion {
	return []model.RAMSuggestion{}
}

// ReclaimDevRAM terminates dev servers to immediately free memory
func ReclaimDevRAM(dryRun bool) (*model.ReclaimResult, error) {
	procs, err := GetTopMemoryProcesses(100)
	if err != nil {
		return nil, err
	}

	result := &model.ReclaimResult{}
	killedMap := make(map[int]bool)

	for _, p := range procs {
		if !p.IsDevServer || p.PID <= 1 {
			continue
		}
		if killedMap[p.PID] {
			continue
		}

		killedMap[p.PID] = true
		result.KilledPIDs = append(result.KilledPIDs, p.PID)
		result.KilledProcesses = append(result.KilledProcesses, fmt.Sprintf("%s (PID %d, %s)", p.Name, p.PID, p.RSSFormatted))
		result.FreedBytes += p.RSSBytes

		if !dryRun {
			if err := portops.KillPID(p.PID, false); err != nil {
				result.FailedErrors = append(result.FailedErrors, fmt.Sprintf("PID %d: %v", p.PID, err))
			}
		}
	}

	result.FreedFormatted = model.FormatBytes(result.FreedBytes)
	return result, nil
}

func isDevProcess(name, cmdline string) bool {
	n := strings.ToLower(name)
	c := strings.ToLower(cmdline)

	devBinaries := []string{
		"node", "npm", "npx", "bun", "deno", "vite", "next", "webpack",
		"python", "python3", "uvicorn", "gunicorn", "celery", "flask",
		"dotnet", "cargo-watch", "ng", "ruby", "rails", "puma",
	}

	for _, bin := range devBinaries {
		if n == bin || strings.HasPrefix(n, bin) {
			return true
		}
	}

	devKeywords := []string{
		"run dev", "start", "serve", "manage.py", "uvicorn", "vite", "next",
		"webpack-dev-server", "dotnet run", "flask run", "astro dev",
	}

	for _, kw := range devKeywords {
		if strings.Contains(c, kw) {
			return true
		}
	}

	return false
}

func detectFrameworkFromCmd(name, cmdline string) string {
	c := strings.ToLower(cmdline)
	n := strings.ToLower(name)

	switch {
	case strings.Contains(c, "next"):
		return "Next.js"
	case strings.Contains(c, "vite"):
		return "Vite"
	case strings.Contains(c, "react-scripts"):
		return "React"
	case strings.Contains(c, "fastapi") || strings.Contains(c, "uvicorn"):
		return "FastAPI"
	case strings.Contains(c, "manage.py") || strings.Contains(c, "django"):
		return "Django"
	case strings.Contains(c, "flask"):
		return "Flask"
	case n == "dotnet" || strings.Contains(c, "dotnet"):
		return "ASP.NET Core"
	case n == "node":
		return "Node.js"
	case n == "python" || n == "python3":
		return "Python"
	case n == "postgres":
		return "PostgreSQL"
	case n == "redis-server":
		return "Redis"
	case n == "ollama":
		return "Ollama AI"
	default:
		return n
	}
}
