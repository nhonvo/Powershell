package model

import "time"

// ProjectInfo contains comprehensive metadata about a discovered or registered project.
type ProjectInfo struct {
	ID           string    `json:"id"`
	Name         string    `json:"name"`
	Path         string    `json:"path"`
	Stack        string    `json:"stack"`
	GitBranch    string    `json:"git_branch"`
	IsGit        bool      `json:"is_git"`
	IsDirty      bool      `json:"is_dirty"`
	DirtyCount   int       `json:"dirty_count"`
	IsPinned     bool      `json:"is_pinned"`
	IsActive     bool      `json:"is_active"`
	LastModified time.Time `json:"last_modified"`
	SessionCount int       `json:"session_count"`
	TotalCost    float64   `json:"total_cost"`
}

// RegistryConfig stores the persistent registry of pinned and registered projects.
type RegistryConfig struct {
	ActiveProjectID string        `json:"active_project_id"`
	DefaultIDE      string        `json:"default_ide"` // "code", "cursor", "nvim", "agy"
	ScanRoots       []string      `json:"scan_roots"`
	Projects        []ProjectInfo `json:"projects"`
}
