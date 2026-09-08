package model

// HostStats contains live Linux & WSL2 system metrics
type HostStats struct {
	TotalRAMMB    uint64  `json:"total_ram_mb"`
	UsedRAMMB     uint64  `json:"used_ram_mb"`
	FreeRAMMB     uint64  `json:"free_ram_mb"`
	RAMUsedPct    float64 `json:"ram_used_pct"`
	LoadAvg1      float64 `json:"load_avg_1"`
	LoadAvg5      float64 `json:"load_avg_5"`
	LoadAvg15     float64 `json:"load_avg_15"`
	UptimeSeconds uint64  `json:"uptime_seconds"`
}

// TailscaleInfo contains Tailscale network status
type TailscaleInfo struct {
	IPv4       string `json:"ipv4"`
	IPv6       string `json:"ipv6"`
	HostName   string `json:"hostname"`
	MagicDNS   string `json:"magic_dns"`
	IsOnline   bool   `json:"is_online"`
	MobilePeer string `json:"mobile_peer"`
}

// MobileDashboard contains the full unified status for mobile clients
type MobileDashboard struct {
	Host             HostStats     `json:"host"`
	Tailscale        TailscaleInfo `json:"tailscale"`
	ActiveAIAccount  string        `json:"active_ai_account"`
	AITokenQuotaUSD  float64       `json:"ai_token_quota_usd"`
	AITokenCount     int64         `json:"ai_token_count"`
	RunningContainers int          `json:"running_containers"`
	TotalContainers  int           `json:"total_containers"`
	ActiveAgents     int           `json:"active_agents"`
	RecentAgentStep  string        `json:"recent_agent_step"`
}
