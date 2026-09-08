package model

// ContainerInfo represents a Docker container
type ContainerInfo struct {
	ID             string `json:"ID"`
	Names          string `json:"Names"`
	Image          string `json:"Image"`
	State          string `json:"State"`
	Status         string `json:"Status"`
	Ports          string `json:"Ports"`
	ComposeProject string `json:"ComposeProject,omitempty"`
	IsRunning      bool   `json:"isRunning"`
}

// MemInfo represents WSL2 / Linux kernel memory statistics from /proc/meminfo
type MemInfo struct {
	TotalKB         uint64  `json:"total_kb"`
	FreeKB          uint64  `json:"free_kb"`
	AvailableKB     uint64  `json:"available_kb"`
	BuffersKB       uint64  `json:"buffers_kb"`
	CachedKB        uint64  `json:"cached_kb"`
	SwapTotalKB     uint64  `json:"swap_total_kb"`
	SwapFreeKB      uint64  `json:"swap_free_kb"`
	UsedKB          uint64  `json:"used_kb"`
	SwapUsedKB      uint64  `json:"swap_used_kb"`
	UsedPercent     float64 `json:"used_percent"`
	SwapUsedPercent float64 `json:"swap_used_percent"`
}

// VolumeInfo represents a Docker volume
type VolumeInfo struct {
	Driver string `json:"Driver"`
	Name   string `json:"Name"`
	Scope  string `json:"Scope"`
}
