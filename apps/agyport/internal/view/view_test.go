package view

import (
	"bytes"
	"strings"
	"testing"

	"agyport/internal/model"
)

func TestRenderProgressBar(t *testing.T) {
	tests := []struct {
		pct      float64
		width    int
		contains string
	}{
		{0.0, 10, "░░░░░░░░░░"},
		{50.0, 10, "█████░░░░░"},
		{100.0, 10, "██████████"},
	}

	for _, tt := range tests {
		bar := renderProgressBar(tt.pct, tt.width)
		if !strings.Contains(bar, tt.contains) {
			t.Errorf("renderProgressBar(%f, %d) = %q; expected to contain %q",
				tt.pct, tt.width, bar, tt.contains)
		}
	}
}

func TestFilteredPorts(t *testing.T) {
	app := NewApp()
	app.cachedPorts = []model.PortInfo{
		{Port: 3000, ProcessName: "node", Framework: "Next.js"},
		{Port: 5173, ProcessName: "node", Framework: "Vite"},
		{Port: 5432, ProcessName: "postgres", Framework: "PostgreSQL"},
		{Port: 6379, ProcessName: "redis-server", Framework: "Redis"},
	}

	// 1. No filter
	app.FilterQuery = ""
	all := app.filteredPorts()
	if len(all) != 4 {
		t.Errorf("expected 4 ports, got %d", len(all))
	}

	// 2. Filter by port number
	app.FilterQuery = "5173"
	p5173 := app.filteredPorts()
	if len(p5173) != 1 || p5173[0].Port != 5173 {
		t.Errorf("expected port 5173, got %v", p5173)
	}

	// 3. Filter by framework
	app.FilterQuery = "postgres"
	pg := app.filteredPorts()
	if len(pg) != 1 || pg[0].Port != 5432 {
		t.Errorf("expected postgres on 5432, got %v", pg)
	}
}

func TestPrintJSON(t *testing.T) {
	sample := model.PortInfo{Port: 8080, Protocol: "tcp"}
	var buf bytes.Buffer
	err := PrintJSON(&buf, sample)
	if err != nil {
		t.Fatalf("PrintJSON error: %v", err)
	}
	out := buf.String()
	if !strings.Contains(out, `"port": 8080`) {
		t.Errorf("PrintJSON output missing port: %s", out)
	}
}

func TestPrintStatus(t *testing.T) {
	app := NewApp()
	var buf bytes.Buffer
	app.PrintStatus(&buf)
	out := buf.String()
	if !strings.Contains(out, "AGYPORT") {
		t.Errorf("PrintStatus output missing header: %s", out)
	}
}

func TestTruncateStr(t *testing.T) {
	tests := []struct {
		in     string
		maxLen int
		exp    string
	}{
		{"hello", 10, "hello"},
		{"hello world", 5, "hell…"},
		{"abc", 3, "abc"},
		{"abc", 2, "a…"},
		{"", 5, ""},
		{"unicode-🚀-test", 10, "unicode-🚀…"},
	}

	for _, tt := range tests {
		res := truncateStr(tt.in, tt.maxLen)
		if res != tt.exp {
			t.Errorf("truncateStr(%q, %d) = %q, expected %q", tt.in, tt.maxLen, res, tt.exp)
		}
	}
}

func TestHorizontalRules(t *testing.T) {
	line := hr(80)
	if !strings.Contains(line, "\033[K\r\n") {
		t.Errorf("hr(80) missing erase and newline: %q", line)
	}
	if strings.Count(line, "─") != 79 {
		t.Errorf("hr(80) expected 79 dashes, got %d", strings.Count(line, "─"))
	}

	dotted := hrDotted(60)
	if !strings.Contains(dotted, "\033[K\r\n") {
		t.Errorf("hrDotted(60) missing erase and newline: %q", dotted)
	}
	if strings.Count(dotted, "┈") != 59 {
		t.Errorf("hrDotted(60) expected 59 dots, got %d", strings.Count(dotted, "┈"))
	}
}

