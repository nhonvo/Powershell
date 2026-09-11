package sysinfo

import (
	"testing"
)

func TestSysInfo_GetStatus(t *testing.T) {
	st := GetSystemStatus()
	if st.Platform == "" {
		t.Errorf("Expected platform string")
	}
	if st.NumCPU <= 0 {
		t.Errorf("Expected CPU count > 0")
	}
	t.Logf("SysInfo Status: Host=%s, CPU=%d, RAM=%.2f/%.2fGB (%.1f%%), Disk=%.2f/%.2fGB",
		st.Hostname, st.NumCPU, st.MemUsedGB, st.MemTotalGB, st.MemUsedPct, st.DiskUsedGB, st.DiskTotalGB)
}
