//go:build !windows
// +build !windows

package portops

import "agyport/internal/model"

type WinProcessDetails struct {
	PID         int
	Name        string
	MemoryBytes uint64
	CommandLine string
	User        string
}

func GetWindowsMemorySummary() (*model.MemorySummary, error) {
	return nil, nil
}

func GetWindowsProcessesMap() map[int]*WinProcessDetails {
	return make(map[int]*WinProcessDetails)
}

func EnrichWindowsTargetPIDs(pMap map[int]*WinProcessDetails, targetPIDs []int) {}
