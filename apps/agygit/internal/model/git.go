package model

import "time"

// RepoStatus holds Git fleet metrics for a repository
type RepoStatus struct {
	Name           string    `json:"name"`
	Path           string    `json:"path"`
	CurrentBranch  string    `json:"current_branch"`
	Ahead          int       `json:"ahead"`
	Behind         int       `json:"behind"`
	StagedFiles    int       `json:"staged_files"`
	DirtyFiles     int       `json:"dirty_files"` // modified but unstaged
	UntrackedFiles int       `json:"untracked_files"`
	StashCount     int       `json:"stash_count"`
	WorktreeCount  int       `json:"worktree_count"`
	LastCommit     string    `json:"last_commit"`
	LastCommitTime time.Time `json:"last_commit_time"`
	IsClean        bool      `json:"is_clean"`
}

// WorktreeInfo holds metadata about a Git worktree
type WorktreeInfo struct {
	RepoName   string `json:"repo_name"`
	RepoPath   string `json:"repo_path"`
	Path       string `json:"path"`
	Branch     string `json:"branch"`
	CommitHash string `json:"commit_hash"`
	IsMain     bool   `json:"is_main"`
	IsLocked   bool   `json:"is_locked"`
}

// CommitInfo holds brief commit summary
type CommitInfo struct {
	Hash         string `json:"hash"`
	Author       string `json:"author"`
	RelativeTime string `json:"relative_time"`
	Message      string `json:"message"`
}
