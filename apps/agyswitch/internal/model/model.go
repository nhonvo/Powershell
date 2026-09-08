package model

import "time"

// AccountInfo represents account state, credentials, and live quota metrics.
type AccountInfo struct {
	AccountName    string        `json:"accountName"`
	Email          string        `json:"email"`
	IsActive       bool          `json:"isActive"`
	TokenSig       string        `json:"tokenSig"`
	IsLoggedIn     bool          `json:"isLoggedIn"`
	QuotaStatus    string        `json:"quotaStatus"`
	GeminiQuotaPct float64       `json:"geminiQuotaPct"`
	ClaudeQuotaPct float64       `json:"claudeQuotaPct"`
	QuotaSummary   *QuotaSummary `json:"quotaSummary,omitempty"`
}

// QuotaBucket represents a single model quota limit window.
type QuotaBucket struct {
	BucketID          string  `json:"bucketId"`
	DisplayName       string  `json:"displayName"`
	Window            string  `json:"window"`
	ResetTime         string  `json:"resetTime"`
	Description       string  `json:"description"`
	RemainingFraction float64 `json:"remainingFraction"`
}

// QuotaGroup represents a collection of models sharing quota limits.
type QuotaGroup struct {
	DisplayName string        `json:"displayName"`
	Description string        `json:"description"`
	Buckets     []QuotaBucket `json:"buckets"`
}

// QuotaSummary represents live user quota API payload.
type QuotaSummary struct {
	Groups      []QuotaGroup `json:"groups"`
	Description string       `json:"description"`
}

// SkillInfo represents an Antigravity skill cheatsheet module.
type SkillInfo struct {
	Name        string `json:"name"`
	Description string `json:"description"`
	Path        string `json:"path"`
	IsGlobal    bool   `json:"isGlobal"`
}

// RuleInfo represents an Antigravity customization rule file.
type RuleInfo struct {
	Name     string `json:"name"`
	Path     string `json:"path"`
	IsGlobal bool   `json:"isGlobal"`
}

// SessionInfo represents a conversation session log transcript summary.
type SessionInfo struct {
	ConversationID string    `json:"conversationId"`
	Title          string    `json:"title"`
	WorkspaceDir   string    `json:"workspaceDir"`
	ProjectName    string    `json:"projectName"`
	LastActive     time.Time `json:"lastActive"`
	StepCount      int       `json:"stepCount"`
	EstimatedCost  float64   `json:"estimatedCost"`
	LogPath        string    `json:"logPath"`
	IsSubagent     bool      `json:"isSubagent"`
	ParentID       string    `json:"parentId"`
}

// ProjectGroup groups sessions belonging to a specific workspace or project directory.
type ProjectGroup struct {
	ProjectName  string        `json:"projectName"`
	WorkspaceDir string        `json:"workspaceDir"`
	TotalCost    float64       `json:"totalCost"`
	TotalSteps   int           `json:"totalSteps"`
	LastActive   time.Time     `json:"lastActive"`
	Sessions     []SessionInfo `json:"sessions"`
}


// ModelBucketDetail extends QuotaBucket with precise reset calculations.
type ModelBucketDetail struct {
	BucketID         string        `json:"bucketId"`
	ModelDisplayName string        `json:"modelDisplayName"`
	QuotaGroup       string        `json:"quotaGroup"`
	WindowType       string        `json:"windowType"`
	RemainingPct     float64       `json:"remainingPct"`
	ResetTime        time.Time     `json:"resetTime"`
	TimeUntilReset   time.Duration `json:"timeUntilReset"`
	ResetMessage     string        `json:"resetMessage"`
	IsThrottled      bool          `json:"isThrottled"`
}

// MCPServerStatus represents health ping status of a Model Context Protocol server.
type MCPServerStatus struct {
	ServerName string `json:"serverName"`
	Command    string `json:"command"`
	IsRunning  bool   `json:"isRunning"`
	LatencyMs  int64  `json:"latencyMs"`
	LastError  string `json:"lastError,omitempty"`
}
