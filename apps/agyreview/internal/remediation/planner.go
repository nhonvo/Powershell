package remediation

import (
	"fmt"
	"os"
	"path/filepath"
	"sort"
	"strings"
	"time"

	"agyreview/internal/model"
)

// GenerateRemediationPlan parses review findings into an atomic, dependency-ordered work execution plan.
func GenerateRemediationPlan(target *model.TargetRepo, findings []model.Finding) (*model.ExecutionPlan, string, error) {
	plan := &model.ExecutionPlan{
		TargetRepo: target.Name,
		CreatedAt:  time.Now(),
		Tasks:      make([]model.RemediationTask, 0),
	}

	for i, f := range findings {
		var prio model.TaskPriority
		var size model.TShirtSize

		switch f.Severity {
		case model.SevP0:
			prio = model.TaskP0
			size = model.SizeS // Immediate security patch
		case model.SevP1:
			prio = model.TaskP1
			size = model.SizeM
		case model.SevP2:
			prio = model.TaskP2
			size = model.SizeM
		default:
			prio = model.TaskP3
			size = model.SizeS
		}

		task := model.RemediationTask{
			TaskID:        fmt.Sprintf("TASK-%03d", i+1),
			FindingID:     f.ID,
			Priority:      prio,
			Size:          size,
			Title:         fmt.Sprintf("Fix %s in %s", f.Category, filepath.Base(f.FilePath)),
			TargetFile:    f.FilePath,
			LineRange:     fmt.Sprintf("L%d-L%d", f.StartLine, f.EndLine),
			OffendingCode: f.OffendingCode,
			ProposedFix:   f.ConcreteFix,
			PatchDiff:     f.PatchDiff,
			Status:        "Pending",
		}
		plan.Tasks = append(plan.Tasks, task)
	}

	// Sort tasks by priority: P0 -> P1 -> P2 -> P3
	sort.SliceStable(plan.Tasks, func(i, j int) bool {
		return plan.Tasks[i].Priority < plan.Tasks[j].Priority
	})
	plan.TotalTasks = len(plan.Tasks)

	// Format pure Markdown remediation deliverable
	var b strings.Builder
	timestamp := time.Now().Format("2006-01-02 15:04:05 MST")

	b.WriteString("# 🛠️ Autonomous Code Remediation & Task Breakdown Plan\n\n")
	b.WriteString(fmt.Sprintf("> **Target Repository**: `%s`  \n", target.Name))
	b.WriteString(fmt.Sprintf("> **Generated At**: `%s`  \n", timestamp))
	b.WriteString(fmt.Sprintf("> **Total Remediation Tasks**: `%d`  \n\n", plan.TotalTasks))
	b.WriteString("---\n\n")

	b.WriteString("## 1. Remediation Backlog & Priority Matrix\n\n")
	b.WriteString("| Task ID | Priority | Size | Target File & Lines | Proposed Fix | Status |\n")
	b.WriteString("| :--- | :--- | :---: | :--- | :--- | :---: |\n")

	for _, t := range plan.Tasks {
		b.WriteString(fmt.Sprintf("| `%s` | **%s** | `%s` | `[%s:%s](./%s)` | %s | %s |\n",
			t.TaskID, t.Priority, t.Size, t.TargetFile, t.LineRange, t.TargetFile, t.ProposedFix, t.Status))
	}
	b.WriteString("\n---\n\n")

	b.WriteString("## 2. Detailed Task Specification & Execution Steps\n\n")
	for _, t := range plan.Tasks {
		b.WriteString(fmt.Sprintf("### %s: %s\n\n", t.TaskID, t.Title))
		b.WriteString(fmt.Sprintf("- **Priority**: %s\n", t.Priority))
		b.WriteString(fmt.Sprintf("- **Estimated Effort**: %s\n", t.Size))
		b.WriteString(fmt.Sprintf("- **Target**: `[%s:%s](./%s)`\n\n", t.TargetFile, t.LineRange, t.TargetFile))
		b.WriteString("**Current Problematic Code:**\n```go\n")
		b.WriteString(t.OffendingCode + "\n```\n\n")
		b.WriteString("**Remediation Directive:**\n")
		b.WriteString(t.ProposedFix + "\n\n")
		if t.PatchDiff != "" {
			b.WriteString("**Diff Patch:**\n```diff\n" + t.PatchDiff + "\n```\n\n")
		}
		b.WriteString("---\n\n")
	}

	reportDir := filepath.Join(target.Path, "doc", "audit")
	_ = os.MkdirAll(reportDir, 0755)
	fileName := fmt.Sprintf("%s_REMEDIATION_PLAN.md", time.Now().Format("20060102_150405"))
	outPath := filepath.Join(reportDir, fileName)

	if err := os.WriteFile(outPath, []byte(b.String()), 0644); err != nil {
		return nil, "", err
	}

	return plan, outPath, nil
}
