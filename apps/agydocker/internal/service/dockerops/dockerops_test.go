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

func TestDockerOps_PruneVolumes(t *testing.T) {
	// Should run without panic regardless of daemon status
	_, _ = PruneVolumes()
}

func TestDockerOps_KillContainer(t *testing.T) {
	// Non-existent container test should safely return error or handle execution
	_ = KillContainer("test_nonexistent_container_xyz")
}

func TestDockerOps_RemoveContainer(t *testing.T) {
	// Non-existent container test should safely return error or handle execution
	_ = RemoveContainer("test_nonexistent_container_xyz")
}

func TestDockerOps_DownCompose(t *testing.T) {
	// Empty or Standalone project names should return error immediately
	if err := DownCompose(""); err == nil {
		t.Errorf("expected error for empty project")
	}
	if err := DownCompose("Standalone"); err == nil {
		t.Errorf("expected error for Standalone project")
	}
	// Non-existent compose stack should return error without panic
	_ = DownCompose("test_nonexistent_stack_xyz")
}

func TestDockerOps_DownContainer(t *testing.T) {
	// Empty container ID should return error
	if err := DownContainer(""); err == nil {
		t.Errorf("expected error for empty container id")
	}
	// Non-existent container test should safely return error or handle execution
	_ = DownContainer("test_nonexistent_container_xyz")
}


