package roadmap

import (
	"os"
	"strings"
	"testing"

	"agyreview/internal/model"
)

func TestRoadmap_Generate(t *testing.T) {
	cwd, _ := os.Getwd()
	target := &model.TargetRepo{
		Name: "test-app",
		Path: cwd,
	}

	score := model.BaremScore{
		TotalPoints: 75,
		LetterGrade: "C",
	}

	outPath, err := GenerateProductRoadmap(target, nil, score)
	if err != nil {
		t.Fatalf("unexpected error generating roadmap: %v", err)
	}
	defer os.Remove(outPath)

	data, err := os.ReadFile(outPath)
	if err != nil {
		t.Fatalf("failed to read roadmap deliverable: %v", err)
	}

	content := string(data)
	if !strings.Contains(content, "Horizon 1") || !strings.Contains(content, "Horizon 2") || !strings.Contains(content, "Horizon 3") {
		t.Errorf("expected 3 horizons in roadmap, got:\n%s", content)
	}
	if !strings.Contains(content, "75 / 100") {
		t.Errorf("expected baseline score 75/100, got:\n%s", content)
	}
}
