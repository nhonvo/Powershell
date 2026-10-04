package ramops

import (
	"testing"

	"agyport/internal/model"
)

func TestGetMemorySummary(t *testing.T) {
	mem, err := GetMemorySummary()
	if err != nil {
		t.Fatalf("GetMemorySummary failed: %v", err)
	}

	if mem.TotalBytes == 0 {
		t.Errorf("expected TotalBytes > 0, got 0")
	}
	if mem.UsedPercent <= 0 || mem.UsedPercent > 100 {
		t.Errorf("unexpected UsedPercent: %f", mem.UsedPercent)
	}
	if mem.TotalFormatted == "" {
		t.Errorf("expected non-empty TotalFormatted")
	}
}

func TestIsDevProcess(t *testing.T) {
	tests := []struct {
		name    string
		cmdline string
		isDev   bool
	}{
		{"node", "node /app/server.js", true},
		{"vite", "vite dev", true},
		{"python3", "python3 -m uvicorn api:app", true},
		{"dotnet", "dotnet run", true},
		{"bash", "/bin/bash", false},
		{"sshd", "/usr/sbin/sshd -D", false},
		{"systemd", "/sbin/init", false},
	}

	for _, tt := range tests {
		got := isDevProcess(tt.name, tt.cmdline)
		if got != tt.isDev {
			t.Errorf("isDevProcess(%q, %q) = %v, want %v", tt.name, tt.cmdline, got, tt.isDev)
		}
	}
}

func TestGenerateRAMSuggestions(t *testing.T) {
	// Simulate dev processes consuming heavy RAM
	procs := []model.ProcessMemInfo{
		{
			PID:          2001,
			Name:         "node",
			RSSBytes:     300 * 1024 * 1024,
			RSSFormatted: "300.0 MB",
			IsDevServer:  true,
			Ports:        []int{3000},
			Framework:    "Next.js",
		},
		{
			PID:          2002,
			Name:         "node",
			RSSBytes:     400 * 1024 * 1024,
			RSSFormatted: "400.0 MB",
			IsDevServer:  true,
			Ports:        []int{3001},
			Framework:    "Next.js",
		},
		{
			PID:          3001,
			Name:         "heavy-runner",
			RSSBytes:     600 * 1024 * 1024,
			RSSFormatted: "600.0 MB",
			IsDevServer:  false,
		},
	}

	ports := []model.PortInfo{
		{
			Port:        3000,
			PID:         2001,
			Framework:   "Next.js",
			MemoryBytes: 300 * 1024 * 1024,
		},
		{
			Port:        3001,
			PID:         2002,
			Framework:   "Next.js",
			MemoryBytes: 400 * 1024 * 1024,
		},
	}

	mem := &model.MemorySummary{
		TotalBytes: 8 * 1024 * 1024 * 1024,
		UsedBytes:  4 * 1024 * 1024 * 1024,
	}

	suggestions := GenerateRAMSuggestions(ports, procs, mem)

	if len(suggestions) != 0 {
		t.Fatalf("expected 0 suggestions (disabled per user directive), got %d", len(suggestions))
	}
}

func TestReclaimDevRAMDryRun(t *testing.T) {
	result, err := ReclaimDevRAM(true)
	if err != nil {
		t.Fatalf("ReclaimDevRAM dry-run failed: %v", err)
	}
	if result == nil {
		t.Fatalf("expected non-nil result")
	}
	t.Logf("Dry-run found %d dev processes", len(result.KilledProcesses))
}
