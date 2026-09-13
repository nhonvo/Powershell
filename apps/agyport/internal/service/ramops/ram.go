package ramops

import (
	"fmt"
	"os"
	"os/user"
	"path/filepath"
	"sort"
	"strconv"
	"strings"

	"agyport/internal/model"
	"agyport/internal/service/portops"
)

var pageSizeBytes = uint64(os.Getpagesize())

// GetMemorySummary inspects /proc/meminfo and calculates host RAM usage
func GetMemorySummary() (*model.MemorySummary, error) {
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

	return mem, nil
}

// GetTopMemoryProcesses returns the highest RAM-consuming processes
func GetTopMemoryProcesses(limit int) ([]model.ProcessMemInfo, error) {
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

	entries, err := os.ReadDir("/proc")
	if err != nil {
		return nil, err
	}

	var procs []model.ProcessMemInfo

	for _, e := range entries {
		if !e.IsDir() {
			continue
		}
		pid, err := strconv.Atoi(e.Name())
		if err != nil || pid <= 1 {
			continue // Skip non-numeric and init/kernel processes
		}

		// 1. Read statm
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
			continue // Skip negligible processes under 1MB
		}

		// 2. Read cmdline
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

		// 3. Read status for Name and Uid
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

		procs = append(procs, model.ProcessMemInfo{
			PID:          pid,
			Name:         name,
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

	// Sort by RSSBytes descending
	sort.Slice(procs, func(i, j int) bool {
		return procs[i].RSSBytes > procs[j].RSSBytes
	})

	if limit > 0 && len(procs) > limit {
		procs = procs[:limit]
	}

	return procs, nil
}

// GenerateRAMSuggestions analyzes system state and yields actionable suggestions
func GenerateRAMSuggestions(ports []model.PortInfo, procs []model.ProcessMemInfo, mem *model.MemorySummary) []model.RAMSuggestion {
	var suggestions []model.RAMSuggestion

	// 1. Check for Multiple Dev Servers running
	var devPIDs []int
	var devPorts []int
	var totalDevBytes uint64

	for _, p := range procs {
		if p.IsDevServer && p.PID > 1 {
			devPIDs = append(devPIDs, p.PID)
			totalDevBytes += p.RSSBytes
			devPorts = append(devPorts, p.Ports...)
		}
	}

	if len(devPIDs) > 0 && totalDevBytes > 250*1024*1024 {
		suggestions = append(suggestions, model.RAMSuggestion{
			ID:                   "reclaim_dev_servers",
			Title:                "Reclaim Active Developer Servers",
			Description:          fmt.Sprintf("%d dev processes (Node, Python, dotnet, etc.) are consuming %s RAM", len(devPIDs), model.FormatBytes(totalDevBytes)),
			ReclaimableBytes:     totalDevBytes,
			ReclaimableFormatted: model.FormatBytes(totalDevBytes),
			TargetPIDs:           devPIDs,
			TargetPorts:          devPorts,
			ActionType:           "kill_dev",
			SuggestedCommand:     "agyport reclaim",
		})
	}

	// 2. Check for Heavy Memory Hogs (> 500MB)
	for _, p := range procs {
		if p.RSSBytes >= 500*1024*1024 && p.PID > 1 {
			pDesc := fmt.Sprintf("Process '%s' (PID %d) is using %s (%.1f%% RAM)", p.Name, p.PID, p.RSSFormatted, p.RSSPercent)
			if len(p.Ports) > 0 {
				pDesc += fmt.Sprintf(" listening on port(s) %v", p.Ports)
			}
			suggestions = append(suggestions, model.RAMSuggestion{
				ID:                   fmt.Sprintf("kill_hog_%d", p.PID),
				Title:                fmt.Sprintf("High RAM Consumer: %s (PID %d)", p.Name, p.PID),
				Description:          pDesc,
				ReclaimableBytes:     p.RSSBytes,
				ReclaimableFormatted: p.RSSFormatted,
				TargetPIDs:           []int{p.PID},
				TargetPorts:          p.Ports,
				ActionType:           "kill_pids",
				SuggestedCommand:     fmt.Sprintf("agyport kill-pid %d", p.PID),
			})
		}
	}

	// 3. Check for Duplicate Web Ports (e.g. Next.js / Vite running on 3000 and 3001)
	portMap := make(map[string][]model.PortInfo)
	for _, p := range ports {
		if !p.IsSystemPort && p.Framework != "" && p.Framework != "System Service" && p.Framework != "Generic Service" && p.Framework != "Antigravity CLI" {
			portMap[p.Framework] = append(portMap[p.Framework], p)
		}
	}
	for framework, list := range portMap {
		uniquePIDs := make(map[int]bool)
		for _, item := range list {
			if item.PID > 0 {
				uniquePIDs[item.PID] = true
			}
		}

		if len(uniquePIDs) > 1 {
			var dupPorts []int
			var dupPIDs []int
			var dupBytes uint64
			for _, item := range list {
				dupPorts = append(dupPorts, item.Port)
				if item.PID > 0 {
					dupPIDs = append(dupPIDs, item.PID)
					dupBytes += item.MemoryBytes
				}
			}
			suggestions = append(suggestions, model.RAMSuggestion{
				ID:                   fmt.Sprintf("dup_server_%s", strings.ToLower(framework)),
				Title:                fmt.Sprintf("Duplicate %s Servers Detected", framework),
				Description:          fmt.Sprintf("%d distinct processes listening on ports %v, using %s RAM", len(uniquePIDs), dupPorts, model.FormatBytes(dupBytes)),
				ReclaimableBytes:     dupBytes,
				ReclaimableFormatted: model.FormatBytes(dupBytes),
				TargetPIDs:           dupPIDs,
				TargetPorts:          dupPorts,
				ActionType:           "kill_pids",
				SuggestedCommand:     fmt.Sprintf("agyport kill %d", dupPorts[len(dupPorts)-1]),
			})
		}
	}

	// 4. WSL2 Page Cache Bloat
	if mem != nil && mem.TotalBytes > 0 {
		cacheRatio := float64(mem.CachedBytes+mem.BuffersBytes) / float64(mem.TotalBytes)
		if cacheRatio > 0.35 && (mem.CachedBytes+mem.BuffersBytes) > 1024*1024*1024 {
			cacheReclaim := mem.CachedBytes + mem.BuffersBytes
			suggestions = append(suggestions, model.RAMSuggestion{
				ID:                   "drop_page_caches",
				Title:                "Drop WSL2 Buffer & Page Cache",
				Description:          fmt.Sprintf("WSL2 page/buffer cache is holding %s (%.1f%% of RAM)", model.FormatBytes(cacheReclaim), cacheRatio*100),
				ReclaimableBytes:     cacheReclaim,
				ReclaimableFormatted: model.FormatBytes(cacheReclaim),
				ActionType:           "drop_caches",
				SuggestedCommand:     "sudo sh -c 'echo 3 > /proc/sys/vm/drop_caches'",
			})
		}
	}

	return suggestions
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
