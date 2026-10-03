package remediation

import (
	"os"
	"testing"

	"agyreview/internal/model"
)

func TestRemediation_Planner(t *testing.T) {
	cwd, _ := os.Getwd()
	target := &model.TargetRepo{
		Name: "test-repo",
		Path: cwd,
	}

	findings := []model.Finding{
		{
			ID:                 "SEC-1",
			Severity:           model.SevP0,
			Category:           "Security",
			FilePath:           "main.go",
			StartLine:          10,
			EndLine:            12,
			OffendingCode:      "apiKey = \"12345\"",
			FailureExplanation: "Hardcoded secret",
			ConcreteFix:        "Use os.Getenv",
		},
		{
			ID:                 "STYLE-1",
			Severity:           model.SevP3,
			Category:           "Style",
			FilePath:           "util.go",
			StartLine:          5,
			EndLine:            5,
			OffendingCode:      "var X int",
			FailureExplanation: "Exported variable without comment",
			ConcreteFix:        "Add doc comment",
		},
	}

	plan, outPath, err := GenerateRemediationPlan(target, findings)
	if err != nil {
		t.Fatalf("unexpected error generating remediation plan: %v", err)
	}
	defer os.Remove(outPath)

	if len(plan.Tasks) != 2 {
		t.Errorf("expected 2 remediation tasks, got %d", len(plan.Tasks))
	}

	// P0 must be first
	if plan.Tasks[0].Priority != model.TaskP0 {
		t.Errorf("expected first task to be P0, got %s", plan.Tasks[0].Priority)
	}
}
