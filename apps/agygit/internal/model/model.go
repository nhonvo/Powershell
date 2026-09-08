package model

// ChangedFile represents a single file change parsed from git status porcelain
type ChangedFile struct {
	IndexStatus    byte   `json:"index_status"`     // 'M', 'A', 'D', 'R', 'C', '?'
	WorkTreeStatus byte   `json:"work_tree_status"`  // 'M', 'D', '?', 'U'
	Path           string `json:"path"`
	OriginalPath   string `json:"original_path,omitempty"` // For renames (R)
	IsStaged       bool   `json:"is_staged"`
	IsUntracked    bool   `json:"is_untracked"`
	IsConflict     bool   `json:"is_conflict"`
}
