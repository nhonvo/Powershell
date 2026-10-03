package model

import "time"

type ReviewScope string

const (
	ScopeWholeCodebase ReviewScope = "whole-codebase"
	ScopeSecurity      ReviewScope = "security-audit"
	ScopePRDiff        ReviewScope = "pr-diff"
	ScopeBranchDiff    ReviewScope = "branch-diff"
)

type TriggerMode string

const (
	TriggerManual  TriggerMode = "manual"
	TriggerAuto    TriggerMode = "auto"
	TriggerSuggest TriggerMode = "suggest"
)

type TargetRepo struct {
	Name         string      `json:"name"`
	Path         string      `json:"path"`
	OriginURL    string      `json:"origin_url,omitempty"`
	Branch       string      `json:"branch"`
	HeadCommit   string      `json:"head_commit"`
	CommitMsg    string      `json:"commit_msg,omitempty"`
	Author       string      `json:"author,omitempty"`
	IsRemote     bool        `json:"is_remote"`
	IsTemporary  bool        `json:"is_temporary"`
	PRNumber     int         `json:"pr_number,omitempty"`
	Scope        ReviewScope `json:"scope"`
	DiscoveredAt time.Time   `json:"discovered_at"`
}

type PRInfo struct {
	RepoOwner    string   `json:"repo_owner"`
	RepoName     string   `json:"repo_name"`
	Number       int      `json:"number"`
	Title        string   `json:"title"`
	BaseRef      string   `json:"base_ref"`
	HeadRef      string   `json:"head_ref"`
	DiffURL      string   `json:"diff_url"`
	HTMLURL      string   `json:"html_url"`
	Author       string   `json:"author"`
	ChangedFiles []string `json:"changed_files"`
}
