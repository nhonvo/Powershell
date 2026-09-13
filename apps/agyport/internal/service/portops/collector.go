package portops

import (
	"bufio"
	"fmt"
	"os"
	"os/exec"
	"os/user"
	"path/filepath"
	"regexp"
	"runtime"
	"sort"
	"strconv"
	"strings"

	"agyport/internal/model"
)

var (
	ssUserRegex   = regexp.MustCompile(`pid=(\d+)`)
	ssNameRegex   = regexp.MustCompile(`"([^"]+)"`)
	pageSizeBytes = uint64(os.Getpagesize())
)

// ListPorts retrieves all active listening ports with enriched process and memory stats
func ListPorts() ([]model.PortInfo, error) {
	totalMemBytes := getTotalMemoryBytes()

	var ports []model.PortInfo
	seen := make(map[string]bool)

	// 1. Try lsof first (fast and rich for current user processes)
	if lsofPorts, err := parseLsof(); err == nil && len(lsofPorts) > 0 {
		for _, p := range lsofPorts {
			key := fmt.Sprintf("%s:%d", p.Protocol, p.Port)
			if !seen[key] {
				seen[key] = true
				enrichPortDetails(&p, totalMemBytes)
				ports = append(ports, p)
			}
		}
	}

	// 2. Try ss for system-wide sockets (catches services run by other users/daemons)
	if ssPorts, err := parseSs(); err == nil && len(ssPorts) > 0 {
		for _, p := range ssPorts {
			key := fmt.Sprintf("%s:%d", p.Protocol, p.Port)
			if !seen[key] {
				seen[key] = true
				enrichPortDetails(&p, totalMemBytes)
				ports = append(ports, p)
			}
		}
	}

	// 3. Fallback: If on Windows or cross-compiled
	if len(ports) == 0 && runtime.GOOS == "windows" {
		if winPorts, err := parseWindowsNetstat(); err == nil {
			for _, p := range winPorts {
				key := fmt.Sprintf("%s:%d", p.Protocol, p.Port)
				if !seen[key] {
					seen[key] = true
					enrichPortDetails(&p, totalMemBytes)
					ports = append(ports, p)
				}
			}
		}
	}

	// Sort ports ascending
	sort.Slice(ports, func(i, j int) bool {
		return ports[i].Port < ports[j].Port
	})

	return ports, nil
}

// FindPort searches for a specific port number
func FindPort(targetPort int) (*model.PortInfo, error) {
	ports, err := ListPorts()
	if err != nil {
		return nil, err
	}
	for _, p := range ports {
		if p.Port == targetPort {
			return &p, nil
		}
	}
	return nil, fmt.Errorf("port %d is not currently in use", targetPort)
}

// parseLsof executes and parses lsof -i -P -n
func parseLsof() ([]model.PortInfo, error) {
	cmd := exec.Command("lsof", "-i", "-P", "-n")
	out, err := cmd.Output()
	if err != nil {
		return nil, err
	}

	var results []model.PortInfo
	scanner := bufio.NewScanner(strings.NewReader(string(out)))
	if !scanner.Scan() {
		return results, nil
	}

	for scanner.Scan() {
		line := strings.TrimSpace(scanner.Text())
		if line == "" {
			continue
		}

		fields := strings.Fields(line)
		if len(fields) < 9 {
			continue
		}

		// Look for LISTEN or UDP sockets
		node := fields[7] // TCP or UDP
		nameField := fields[8]

		isTCPListen := strings.EqualFold(node, "TCP") && strings.Contains(line, "(LISTEN)")
		isUDP := strings.EqualFold(node, "UDP")

		if !isTCPListen && !isUDP {
			continue
		}

		pid, _ := strconv.Atoi(fields[1])
		comm := fields[0]
		userStr := fields[2]

		// Extract address and port from nameField (e.g., "*:3000", "127.0.0.1:8080", "[::]:5432")
		addr, port := parseAddrAndPort(nameField)
		if port <= 0 {
			continue
		}

		state := "LISTEN"
		proto := strings.ToLower(node)
		if isUDP {
			state = "UNCONN"
		}

		results = append(results, model.PortInfo{
			Port:         port,
			Protocol:     proto,
			State:        state,
			BindAddress:  addr,
			PID:          pid,
			ProcessName:  comm,
			User:         userStr,
			IsSystemPort: IsSystemPort(port),
			CanKill:      !IsSystemPort(port) && pid > 0,
		})
	}

	return results, nil
}

// parseSs executes and parses ss -tulpn -H
func parseSs() ([]model.PortInfo, error) {
	cmd := exec.Command("ss", "-tulpn", "-H")
	out, err := cmd.Output()
	if err != nil {
		return nil, err
	}

	var results []model.PortInfo
	scanner := bufio.NewScanner(strings.NewReader(string(out)))
	for scanner.Scan() {
		line := strings.TrimSpace(scanner.Text())
		if line == "" {
			continue
		}

		fields := strings.Fields(line)
		if len(fields) < 5 {
			continue
		}

		netid := strings.ToLower(fields[0])
		state := fields[1]
		localAddr := fields[4]

		if !strings.HasPrefix(netid, "tcp") && !strings.HasPrefix(netid, "udp") {
			continue
		}

		proto := "tcp"
		if strings.HasPrefix(netid, "udp") {
			proto = "udp"
		}

		addr, port := parseAddrAndPort(localAddr)
		if port <= 0 {
			continue
		}

		pid := 0
		procName := ""
		if len(fields) >= 7 {
			procField := strings.Join(fields[6:], " ")
			if matches := ssUserRegex.FindStringSubmatch(procField); len(matches) > 1 {
				pid, _ = strconv.Atoi(matches[1])
			}
			if matches := ssNameRegex.FindStringSubmatch(procField); len(matches) > 1 {
				procName = matches[1]
			}
		}

		results = append(results, model.PortInfo{
			Port:         port,
			Protocol:     proto,
			State:        state,
			BindAddress:  addr,
			PID:          pid,
			ProcessName:  procName,
			IsSystemPort: IsSystemPort(port),
			CanKill:      !IsSystemPort(port) && pid > 0,
		})
	}

	return results, nil
}

// parseWindowsNetstat parses netstat -ano on Windows
func parseWindowsNetstat() ([]model.PortInfo, error) {
	cmd := exec.Command("netstat", "-ano")
	out, err := cmd.Output()
	if err != nil {
		return nil, err
	}

	var results []model.PortInfo
	scanner := bufio.NewScanner(strings.NewReader(string(out)))
	for scanner.Scan() {
		line := strings.TrimSpace(scanner.Text())
		fields := strings.Fields(line)
		if len(fields) < 4 {
			continue
		}

		proto := strings.ToLower(fields[0])
		if proto != "tcp" && proto != "udp" {
			continue
		}

		localAddr := fields[1]
		state := ""
		pid := 0

		if proto == "tcp" {
			if len(fields) < 5 {
				continue
			}
			state = fields[2]
			if !strings.EqualFold(state, "LISTENING") {
				continue
			}
			pid, _ = strconv.Atoi(fields[4])
		} else {
			state = "UNCONN"
			pid, _ = strconv.Atoi(fields[3])
		}

		addr, port := parseAddrAndPort(localAddr)
		if port <= 0 {
			continue
		}

		results = append(results, model.PortInfo{
			Port:         port,
			Protocol:     proto,
			State:        state,
			BindAddress:  addr,
			PID:          pid,
			IsSystemPort: IsSystemPort(port),
			CanKill:      !IsSystemPort(port) && pid > 0,
		})
	}

	return results, nil
}

// parseAddrAndPort splits an IP:Port string into components
func parseAddrAndPort(s string) (string, int) {
	s = strings.TrimSuffix(s, " (LISTEN)")
	lastColon := strings.LastIndex(s, ":")
	if lastColon == -1 || lastColon == len(s)-1 {
		return s, 0
	}

	addr := s[:lastColon]
	portStr := s[lastColon+1:]
	port, _ := strconv.Atoi(portStr)
	return addr, port
}

// IsSystemPort returns true for ports that should have critical kill protection
func IsSystemPort(port int) bool {
	// 22: SSH, 53: DNS, 67/68: DHCP, 123: NTP, 323: Chrony NTP
	switch port {
	case 22, 53, 67, 68, 123, 323:
		return true
	}
	// Under 1024 are privileged system ports
	return port < 1024
}

// enrichPortDetails retrieves process cmdline, memory, framework, and user
func enrichPortDetails(p *model.PortInfo, totalMem uint64) {
	if p.PID <= 0 {
		inferServiceFromPort(p)
		return
	}

	// 1. Read /proc/{pid}/cmdline
	if cmdBytes, err := os.ReadFile(fmt.Sprintf("/proc/%d/cmdline", p.PID)); err == nil {
		parts := strings.Split(string(cmdBytes), "\x00")
		var cleanParts []string
		for _, part := range parts {
			if strings.TrimSpace(part) != "" {
				cleanParts = append(cleanParts, part)
			}
		}
		p.CommandLine = strings.Join(cleanParts, " ")
		if p.ProcessName == "" && len(cleanParts) > 0 {
			p.ProcessName = filepath.Base(cleanParts[0])
		}
	}

	// 2. Read /proc/{pid}/statm for memory (pages)
	if statmBytes, err := os.ReadFile(fmt.Sprintf("/proc/%d/statm", p.PID)); err == nil {
		fields := strings.Fields(string(statmBytes))
		if len(fields) >= 2 {
			if residentPages, err := strconv.ParseUint(fields[1], 10, 64); err == nil {
				p.MemoryBytes = residentPages * pageSizeBytes
				p.MemoryFormatted = model.FormatBytes(p.MemoryBytes)
				if totalMem > 0 {
					p.MemoryPercent = (float64(p.MemoryBytes) / float64(totalMem)) * 100.0
				}
			}
		}
	}

	// 3. Read User if not already set
	if p.User == "" {
		if statusBytes, err := os.ReadFile(fmt.Sprintf("/proc/%d/status", p.PID)); err == nil {
			lines := strings.Split(string(statusBytes), "\n")
			for _, l := range lines {
				if strings.HasPrefix(l, "Uid:") {
					fields := strings.Fields(l)
					if len(fields) >= 2 {
						if u, err := user.LookupId(fields[1]); err == nil {
							p.User = u.Username
						} else {
							p.User = fields[1]
						}
					}
					break
				}
			}
		}
	}

	// 4. Detect Framework and Dev Category
	inferFramework(p)
}

// inferFramework inspects process name, commandline, and port to determine framework and category
func inferFramework(p *model.PortInfo) {
	cmdLower := strings.ToLower(p.CommandLine)
	nameLower := strings.ToLower(p.ProcessName)

	// Framework detection
	switch {
	case strings.Contains(cmdLower, "next") || strings.Contains(cmdLower, "next-server"):
		p.Framework = "Next.js"
		p.DevCategory = "Frontend"
	case strings.Contains(cmdLower, "vite"):
		p.Framework = "Vite"
		p.DevCategory = "Frontend"
	case strings.Contains(cmdLower, "react-scripts") || strings.Contains(cmdLower, "webpack"):
		p.Framework = "React/Webpack"
		p.DevCategory = "Frontend"
	case strings.Contains(cmdLower, "nuxt"):
		p.Framework = "Nuxt"
		p.DevCategory = "Frontend"
	case strings.Contains(cmdLower, "astro"):
		p.Framework = "Astro"
		p.DevCategory = "Frontend"
	case strings.Contains(cmdLower, "express") || strings.Contains(cmdLower, "nest"):
		p.Framework = "Nest/Express"
		p.DevCategory = "Backend"
	case nameLower == "node" || strings.Contains(cmdLower, "node "):
		p.Framework = "Node.js"
		p.DevCategory = "Web Server"
	case strings.Contains(cmdLower, "fastapi") || strings.Contains(cmdLower, "uvicorn"):
		p.Framework = "FastAPI"
		p.DevCategory = "Backend"
	case strings.Contains(cmdLower, "gunicorn") || strings.Contains(cmdLower, "flask"):
		p.Framework = "Flask"
		p.DevCategory = "Backend"
	case strings.Contains(cmdLower, "manage.py") || strings.Contains(cmdLower, "django"):
		p.Framework = "Django"
		p.DevCategory = "Backend"
	case strings.HasPrefix(nameLower, "python"):
		p.Framework = "Python App"
		p.DevCategory = "Backend"
	case nameLower == "dotnet" || strings.Contains(cmdLower, "dotnet"):
		p.Framework = "ASP.NET Core"
		p.DevCategory = "Backend"
	case nameLower == "postgres" || p.Port == 5432:
		p.Framework = "PostgreSQL"
		p.DevCategory = "Database"
	case nameLower == "redis-server" || p.Port == 6379:
		p.Framework = "Redis"
		p.DevCategory = "Database"
	case nameLower == "mysqld" || p.Port == 3306:
		p.Framework = "MySQL"
		p.DevCategory = "Database"
	case nameLower == "mongod" || p.Port == 27017:
		p.Framework = "MongoDB"
		p.DevCategory = "Database"
	case nameLower == "docker-proxy" || strings.Contains(cmdLower, "docker-proxy"):
		p.Framework = "Docker Container"
		p.DevCategory = "Container"
	case nameLower == "ollama" || p.Port == 11434:
		p.Framework = "Ollama AI"
		p.DevCategory = "AI/ML"
	case nameLower == "agy" || strings.Contains(cmdLower, "agy"):
		p.Framework = "Antigravity CLI"
		p.DevCategory = "DevTool"
	case p.IsSystemPort:
		p.Framework = "System Service"
		p.DevCategory = "System"
	default:
		if p.Port >= 3000 && p.Port <= 9999 {
			p.Framework = "Custom Dev Server"
			p.DevCategory = "Web Server"
		} else {
			p.Framework = "Generic Service"
			p.DevCategory = "Network"
		}
	}
}

// inferServiceFromPort provides fallbacks if PID could not be inspected
func inferServiceFromPort(p *model.PortInfo) {
	switch p.Port {
	case 22:
		p.ProcessName = "sshd"
		p.Framework = "OpenSSH"
		p.DevCategory = "System"
	case 53:
		p.ProcessName = "systemd-resolved"
		p.Framework = "DNS"
		p.DevCategory = "System"
	case 5432:
		p.ProcessName = "postgres"
		p.Framework = "PostgreSQL"
		p.DevCategory = "Database"
	case 6379:
		p.ProcessName = "redis-server"
		p.Framework = "Redis"
		p.DevCategory = "Database"
	case 3306:
		p.ProcessName = "mysqld"
		p.Framework = "MySQL"
		p.DevCategory = "Database"
	case 27017:
		p.ProcessName = "mongod"
		p.Framework = "MongoDB"
		p.DevCategory = "Database"
	case 11434:
		p.ProcessName = "ollama"
		p.Framework = "Ollama"
		p.DevCategory = "AI/ML"
	}
}

// getTotalMemoryBytes reads /proc/meminfo to get system total RAM
func getTotalMemoryBytes() uint64 {
	data, err := os.ReadFile("/proc/meminfo")
	if err != nil {
		return 0
	}
	lines := strings.Split(string(data), "\n")
	for _, l := range lines {
		if strings.HasPrefix(l, "MemTotal:") {
			fields := strings.Fields(l)
			if len(fields) >= 2 {
				valKB, _ := strconv.ParseUint(fields[1], 10, 64)
				return valKB * 1024
			}
		}
	}
	return 0
}
