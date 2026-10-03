package model

import "time"

type TaskPriority string

const (
	TaskP0 TaskPriority = "P0 - Blocker"
	TaskP1 TaskPriority = "P1 - High"
	TaskP2 TaskPriority = "P2 - Medium"
	TaskP3 TaskPriority = "P3 - Low"
)

type TShirtSize string

const (
	SizeS TShirtSize = "S (1-2h)"
	SizeM TShirtSize = "M (4-6h)"
	SizeL TShirtSize = "L (1-2d)"
)

type RemediationTask struct {
	TaskID         string       `json:"task_id"`
	FindingID      string       `json:"finding_id"`
	Priority       TaskPriority `json:"priority"`
	Size           TShirtSize   `json:"size"`
	Title          string       `json:"title"`
	TargetFile     string       `json:"target_file"`
	LineRange      string       `json:"line_range"`
	OffendingCode  string       `json:"offending_code"`
	ProposedFix    string       `json:"proposed_fix"`
	PatchDiff      string       `json:"patch_diff"`
	Status         string       `json:"status"` // "Pending", "Applied", "Verified", "Skipped"
	AppliedCommit  string       `json:"applied_commit,omitempty"`
}

type ExecutionPlan struct {
	TargetRepo     string            `json:"target_repo"`
	CreatedAt      time.Time         `json:"created_at"`
	Tasks          []RemediationTask `json:"tasks"`
	TotalTasks     int               `json:"total_tasks"`
	CompletedTasks int               `json:"completed_tasks"`
	TargetWorktree string            `json:"target_worktree,omitempty"`
}
