//go:build !windows

package sysinfo

import (
	"os"
	"syscall"
)

func readDiskMetrics(st *SystemStatus) {
	var stat syscall.Statfs_t
	wd, err := os.Getwd()
	if err != nil {
		wd = "/"
	}
	if err := syscall.Statfs(wd, &stat); err == nil {
		totalBytes := stat.Blocks * uint64(stat.Bsize)
		freeBytes := stat.Bfree * uint64(stat.Bsize)
		usedBytes := totalBytes - freeBytes

		st.DiskTotalGB = float64(totalBytes) / (1024 * 1024 * 1024)
		st.DiskUsedGB = float64(usedBytes) / (1024 * 1024 * 1024)
		if totalBytes > 0 {
			st.DiskUsedPct = (float64(usedBytes) / float64(totalBytes)) * 100
		}
	}
}
