package dockerops

import (
	"testing"
)

func TestDockerOps_GetMemoryInfo(t *testing.T) {
	mem, err := GetMemoryInfo()
	if err != nil {
		t.Fatalf("GetMemoryInfo failed: %v", err)
	}
	if mem.TotalKB == 0 {
		t.Errorf("expected TotalKB > 0, got 0")
	}
	if mem.UsedPercent < 0 || mem.UsedPercent > 100 {
		t.Errorf("expected UsedPercent between 0 and 100, got %.2f", mem.UsedPercent)
	}
}

func TestDockerOps_ListContainers(t *testing.T) {
	// Should run without panic regardless of daemon status
	_, _ = ListContainers()
}
