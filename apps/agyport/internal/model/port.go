package model

import "fmt"

// PortInfo contains metadata for an active network socket and its owner process
type PortInfo struct {
	Port            int     `json:"port"`
	Protocol        string  `json:"protocol"` // "tcp", "udp"
	State           string  `json:"state"`    // "LISTEN", "UNCONN", etc.
	BindAddress     string  `json:"bind_address"`
	PID             int     `json:"pid"`
	ProcessName     string  `json:"process_name"`
	User            string  `json:"user"`
	CommandLine     string  `json:"command_line"`
	MemoryBytes     uint64  `json:"memory_bytes"`
	MemoryFormatted string  `json:"memory_formatted"`
	MemoryPercent   float64 `json:"memory_percent"`
	Framework       string  `json:"framework"`        // e.g. Next.js, Vite, FastAPI
	DevCategory     string  `json:"dev_category"`     // Frontend, Backend, DB, AI, System
	IsSystemPort    bool    `json:"is_system_port"`   // true if port < 1024 or critical (22, 53)
	CanKill         bool    `json:"can_kill"`
}

// MemorySummary contains real-time host RAM and Swap usage stats
type MemorySummary struct {
	TotalBytes         uint64  `json:"total_bytes"`
	UsedBytes          uint64  `json:"used_bytes"`
	FreeBytes          uint64  `json:"free_bytes"`
	AvailableBytes     uint64  `json:"available_bytes"`
	BuffersBytes       uint64  `json:"buffers_bytes"`
	CachedBytes        uint64  `json:"cached_bytes"`
	SwapTotalBytes     uint64  `json:"swap_total_bytes"`
	SwapUsedBytes      uint64  `json:"swap_used_bytes"`
	SwapFreeBytes      uint64  `json:"swap_free_bytes"`
	UsedPercent        float64 `json:"used_percent"`
	SwapUsedPercent    float64 `json:"swap_used_percent"`
	TotalFormatted     string  `json:"total_formatted"`
	UsedFormatted      string  `json:"used_formatted"`
	AvailableFormatted string  `json:"available_formatted"`
	CachedFormatted    string  `json:"cached_formatted"`
	SwapUsedFormatted  string  `json:"swap_used_formatted"`
}

// ProcessMemInfo provides details of a memory-consuming process
type ProcessMemInfo struct {
	PID            int      `json:"pid"`
	Name           string   `json:"name"`
	User           string   `json:"user"`
	Cmdline        string   `json:"cmdline"`
	RSSBytes       uint64   `json:"rss_bytes"`
	RSSFormatted   string   `json:"rss_formatted"`
	RSSPercent     float64  `json:"rss_percent"`
	Ports          []int    `json:"ports"`
	IsDevServer    bool     `json:"is_dev_server"`
	Framework      string   `json:"framework"`
	Recommendation string   `json:"recommendation"`
}

// RAMSuggestion represents an actionable optimization suggestion
type RAMSuggestion struct {
	ID                   string   `json:"id"`
	Title                string   `json:"title"`
	Description          string   `json:"description"`
	ReclaimableBytes     uint64   `json:"reclaimable_bytes"`
	ReclaimableFormatted string   `json:"reclaimable_formatted"`
	TargetPIDs           []int    `json:"target_pids"`
	TargetPorts          []int    `json:"target_ports"`
	ActionType           string   `json:"action_type"` // "kill_dev", "kill_pids", "drop_caches"
	SuggestedCommand     string   `json:"suggested_command"`
}

// KillResult summarizes the outcome of terminating a port or PID
type KillResult struct {
	Port        int    `json:"port,omitempty"`
	PID         int    `json:"pid"`
	ProcessName string `json:"process_name"`
	Success     bool   `json:"success"`
	Error       string `json:"error,omitempty"`
	Forced      bool   `json:"forced"`
}

// ReclaimResult summarizes the outcome of batch RAM reclamation
type ReclaimResult struct {
	KilledPIDs       []int    `json:"killed_pids"`
	KilledProcesses  []string `json:"killed_processes"`
	FreedBytes       uint64   `json:"freed_bytes"`
	FreedFormatted   string   `json:"freed_formatted"`
	FailedErrors     []string `json:"failed_errors,omitempty"`
}

// FormatBytes formats bytes into human-readable B, KB, MB, GB string
func FormatBytes(b uint64) string {
	const unit = 1024
	if b < unit {
		return fmt.Sprintf("%d B", b)
	}
	div, exp := uint64(unit), 0
	for n := b / unit; n >= unit; n /= unit {
		div *= unit
		exp++
	}
	return fmt.Sprintf("%.1f %cB", float64(b)/float64(div), "KMGTPE"[exp])
}
