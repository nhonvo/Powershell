//go:build windows
// +build windows

package portops

import (
	"bufio"
	"fmt"
	"os/exec"
	"strconv"
	"strings"
	"syscall"
	"unsafe"

	"agyport/internal/model"
)

var (
	modkernel32                  = syscall.NewLazyDLL("kernel32.dll")
	modpsapi                     = syscall.NewLazyDLL("psapi.dll")
	procGlobalMemoryStatusEx    = modkernel32.NewProc("GlobalMemoryStatusEx")
	procCreateToolhelp32Snapshot = modkernel32.NewProc("CreateToolhelp32Snapshot")
	procProcess32FirstW          = modkernel32.NewProc("Process32FirstW")
	procProcess32NextW           = modkernel32.NewProc("Process32NextW")
	procOpenProcess              = modkernel32.NewProc("OpenProcess")
	procCloseHandle              = modkernel32.NewProc("CloseHandle")
	procGetProcessMemoryInfo     = modpsapi.NewProc("GetProcessMemoryInfo")
)

type memoryStatusEx struct {
	cbSize                  uint32
	dwMemoryLoad            uint32
	ullTotalPhys            uint64
	ullAvailPhys            uint64
	ullTotalPageFile        uint64
	ullAvailPageFile        uint64
	ullTotalVirtual         uint64
	ullAvailVirtual         uint64
	ullAvailExtendedVirtual uint64
}

type processEntry32W struct {
	dwSize              uint32
	cntUsage            uint32
	th32ProcessID       uint32
	th32DefaultHeapID   uintptr
	th32ModuleID        uint32
	cntThreads          uint32
	th32ParentProcessID uint32
	pcPriClassBase      int32
	dwFlags             uint32
	szExeFile           [260]uint16
}

type processMemoryCounters struct {
	cb                         uint32
	pageFaultCount             uint32
	peakWorkingSetSize         uintptr
	workingSetSize             uintptr
	quotaPeakPagedPoolUsage    uintptr
	quotaPagedPoolUsage        uintptr
	quotaPeakNonPagedPoolUsage uintptr
	quotaNonPagedPoolUsage     uintptr
	pagefileUsage              uintptr
	peakPagefileUsage          uintptr
}

const (
	TH32CS_SNAPPROCESS = 0x00000002
	PROCESS_QUERY_INFO = 0x0400
	PROCESS_VM_READ    = 0x0010
)

// GetWindowsMemorySummary retrieves total, free, and used RAM on Windows via GlobalMemoryStatusEx
func GetWindowsMemorySummary() (*model.MemorySummary, error) {
	var status memoryStatusEx
	status.cbSize = uint32(unsafe.Sizeof(status))

	r1, _, err := procGlobalMemoryStatusEx.Call(uintptr(unsafe.Pointer(&status)))
	if r1 == 0 {
		return nil, fmt.Errorf("GlobalMemoryStatusEx failed: %w", err)
	}

	mem := &model.MemorySummary{
		TotalBytes:     status.ullTotalPhys,
		AvailableBytes: status.ullAvailPhys,
		FreeBytes:      status.ullAvailPhys,
		UsedBytes:      status.ullTotalPhys - status.ullAvailPhys,
	}

	if mem.TotalBytes > 0 {
		mem.UsedPercent = (float64(mem.UsedBytes) / float64(mem.TotalBytes)) * 100.0
	}

	mem.TotalFormatted = model.FormatBytes(mem.TotalBytes)
	mem.UsedFormatted = model.FormatBytes(mem.UsedBytes)
	mem.AvailableFormatted = model.FormatBytes(mem.AvailableBytes)
	mem.CachedFormatted = "0 B"
	mem.SwapUsedFormatted = "0 B"

	return mem, nil
}

// WinProcessDetails holds process metadata gathered via Windows APIs
type WinProcessDetails struct {
	PID         int
	Name        string
	MemoryBytes uint64
	CommandLine string
	User        string
}

// GetWindowsProcessesMap returns a map of PID -> WinProcessDetails using native Win32 APIs
func GetWindowsProcessesMap() map[int]*WinProcessDetails {
	res := make(map[int]*WinProcessDetails)

	snapshot, _, _ := procCreateToolhelp32Snapshot.Call(uintptr(TH32CS_SNAPPROCESS), 0)
	if snapshot == uintptr(syscall.InvalidHandle) {
		return res
	}
	defer procCloseHandle.Call(snapshot)

	var entry processEntry32W
	entry.dwSize = uint32(unsafe.Sizeof(entry))

	r1, _, _ := procProcess32FirstW.Call(snapshot, uintptr(unsafe.Pointer(&entry)))
	for r1 != 0 {
		pid := int(entry.th32ProcessID)
		name := syscall.UTF16ToString(entry.szExeFile[:])

		if pid > 0 {
			pDetails := &WinProcessDetails{
				PID:  pid,
				Name: name,
			}

			// Get process RAM usage (WorkingSetSize)
			hProc, _, _ := procOpenProcess.Call(uintptr(PROCESS_QUERY_INFO|PROCESS_VM_READ), 0, uintptr(pid))
			if hProc != 0 {
				var memCounters processMemoryCounters
				memCounters.cb = uint32(unsafe.Sizeof(memCounters))
				r2, _, _ := procGetProcessMemoryInfo.Call(hProc, uintptr(unsafe.Pointer(&memCounters)), uintptr(memCounters.cb))
				if r2 != 0 {
					pDetails.MemoryBytes = uint64(memCounters.workingSetSize)
				}
				procCloseHandle.Call(hProc)
			}

			res[pid] = pDetails
		}

		r1, _, _ = procProcess32NextW.Call(snapshot, uintptr(unsafe.Pointer(&entry)))
	}

	return res
}

// EnrichWindowsTargetPIDs fetches CommandLines for specific target PIDs in one fast batch call
func EnrichWindowsTargetPIDs(pMap map[int]*WinProcessDetails, targetPIDs []int) {
	if len(targetPIDs) == 0 {
		return
	}

	var pidStrs []string
	for _, pid := range targetPIDs {
		if pid > 0 {
			pidStrs = append(pidStrs, strconv.Itoa(pid))
		}
	}

	if len(pidStrs) == 0 {
		return
	}

	// Use wmic to get commandline for target PIDs
	// Format: wmic process where "ProcessId=100 or ProcessId=200" get ProcessId,CommandLine /format:csv
	filterParts := make([]string, len(pidStrs))
	for i, p := range pidStrs {
		filterParts[i] = fmt.Sprintf("ProcessId=%s", p)
	}
	wmicQuery := fmt.Sprintf("process where \"%s\" get ProcessId,CommandLine", strings.Join(filterParts, " or "))

	cmd := exec.Command("wmic", strings.Fields(wmicQuery)...)
	out, err := cmd.Output()
	if err == nil {
		scanner := bufio.NewScanner(strings.NewReader(string(out)))
		for scanner.Scan() {
			line := strings.TrimSpace(scanner.Text())
			if line == "" || strings.HasPrefix(line, "CommandLine") {
				continue
			}
			// wmic output lines usually end with PID after whitespace
			fields := strings.Fields(line)
			if len(fields) >= 2 {
				lastField := fields[len(fields)-1]
				if pid, err := strconv.Atoi(lastField); err == nil {
					cmdline := strings.TrimSpace(strings.TrimSuffix(line, lastField))
					if d, ok := pMap[pid]; ok {
						d.CommandLine = cmdline
					}
				}
			}
		}
	}
}
