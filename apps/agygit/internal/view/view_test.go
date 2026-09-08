package view

import (
	"bytes"
	"strings"
	"testing"

	"agygit/internal/model"
)

func TestApp_PrintStatus(t *testing.T) {
	app := NewApp(t.TempDir())
	var buf bytes.Buffer
	app.PrintStatus(&buf)
	out := buf.String()
	if !strings.Contains(out, "AGYGIT") {
		t.Errorf("expected AGYGIT in output, got %s", out)
	}
}

func TestApp_Render(t *testing.T) {
	app := NewApp(t.TempDir())
	app.ActiveTab = 0
	app.Render() // should not crash in raw render
}

func TestView_FileStatusBadge(t *testing.T) {
	tests := []struct {
		name     string
		file     model.ChangedFile
		expected string
	}{
		{
			name: "Conflict file",
			file: model.ChangedFile{
				IsConflict:     true,
				IndexStatus:    'U',
				WorkTreeStatus: 'U',
				Path:           "conflict.go",
			},
			expected: "⚠️ CONFLICT",
		},
		{
			name: "Untracked file",
			file: model.ChangedFile{
				IsUntracked:    true,
				IndexStatus:    '?',
				WorkTreeStatus: '?',
				Path:           "new.go",
			},
			expected: "[?Untrack]",
		},
		{
			name: "Deleted in Index",
			file: model.ChangedFile{
				IndexStatus:    'D',
				WorkTreeStatus: ' ',
				Path:           "deleted.go",
			},
			expected: "[-Deleted]",
		},
		{
			name: "Deleted in WorkTree",
			file: model.ChangedFile{
				IndexStatus:    ' ',
				WorkTreeStatus: 'D',
				Path:           "deleted_wt.go",
			},
			expected: "[-Deleted]",
		},
		{
			name: "Partially staged",
			file: model.ChangedFile{
				IndexStatus:    'M',
				WorkTreeStatus: 'M',
				Path:           "partial.go",
			},
			expected: "[±Partial]",
		},
		{
			name: "Staged file",
			file: model.ChangedFile{
				IsStaged:       true,
				IndexStatus:    'M',
				WorkTreeStatus: ' ',
				Path:           "staged.go",
			},
			expected: "[+Staged]",
		},
		{
			name: "Modified working tree file",
			file: model.ChangedFile{
				IndexStatus:    ' ',
				WorkTreeStatus: 'M',
				Path:           "modified.go",
			},
			expected: "[~Modified]",
		},
	}

	for _, tc := range tests {
		t.Run(tc.name, func(t *testing.T) {
			badge := fileStatusBadge(tc.file)
			if !strings.Contains(badge, tc.expected) {
				t.Errorf("expected badge to contain %q, got %q", tc.expected, badge)
			}
		})
	}
}

func TestView_RenderStatusTab_WithChangedFiles(t *testing.T) {
	app := NewApp(t.TempDir())
	app.ActiveRepoPath = "/mock/repo"
	app.ActiveTab = 0
	app.cachedStatus = &model.RepoStatus{
		Name:          "repo",
		Path:          "/mock/repo",
		CurrentBranch: "main",
		Ahead:         1,
		Behind:        0,
		StagedFiles:   1,
		DirtyFiles:    2,
	}
	app.cachedFiles = []model.ChangedFile{
		{
			IsStaged:       true,
			IndexStatus:    'M',
			WorkTreeStatus: ' ',
			Path:           "staged.txt",
		},
		{
			IndexStatus:    ' ',
			WorkTreeStatus: 'M',
			Path:           "modified.txt",
		},
		{
			IsConflict:     true,
			IndexStatus:    'U',
			WorkTreeStatus: 'U',
			Path:           "conflict.txt",
		},
	}
	app.SelectedIndex = 1

	var b strings.Builder
	app.renderStatusTab(&b, 100, 30)
	out := b.String()

	if !strings.Contains(out, "staged.txt") {
		t.Errorf("expected output to contain staged.txt, got: %s", out)
	}
	if !strings.Contains(out, "modified.txt") {
		t.Errorf("expected output to contain modified.txt, got: %s", out)
	}
	if !strings.Contains(out, "conflict.txt") {
		t.Errorf("expected output to contain conflict.txt, got: %s", out)
	}
	if !strings.Contains(out, "▶") {
		t.Errorf("expected output to contain selection cursor ▶, got: %s", out)
	}
	if !strings.Contains(out, "[+Staged]") {
		t.Errorf("expected output to contain [+Staged], got: %s", out)
	}
	if !strings.Contains(out, "[~Modified]") {
		t.Errorf("expected output to contain [~Modified], got: %s", out)
	}
	if !strings.Contains(out, "⚠️ CONFLICT") {
		t.Errorf("expected output to contain ⚠️ CONFLICT, got: %s", out)
	}
}
