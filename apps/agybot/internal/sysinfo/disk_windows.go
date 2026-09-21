//go:build windows

package sysinfo

func readDiskMetrics(st *SystemStatus) {
	// Disk metrics stub for Windows environments
	st.DiskTotalGB = 0
	st.DiskUsedGB = 0
	st.DiskUsedPct = 0
}
