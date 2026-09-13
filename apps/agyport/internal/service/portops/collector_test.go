package portops

import (
	"testing"

	"agyport/internal/model"
)

func TestIsSystemPort(t *testing.T) {
	// Protected system ports
	systemPorts := []int{22, 53, 67, 68, 80, 443, 123, 323, 1000}
	for _, port := range systemPorts {
		if !IsSystemPort(port) {
			t.Errorf("expected port %d to be recognized as a system port", port)
		}
	}

	// Developer & User ports
	devPorts := []int{3000, 3001, 4000, 5000, 5173, 8000, 8080, 9000, 44383}
	for _, port := range devPorts {
		if IsSystemPort(port) {
			t.Errorf("expected port %d to be recognized as a dev/user port, got system port", port)
		}
	}
}

func TestParseAddrAndPort(t *testing.T) {
	tests := []struct {
		input        string
		expectedAddr string
		expectedPort int
	}{
		{"127.0.0.1:3000", "127.0.0.1", 3000},
		{"0.0.0.0:8080", "0.0.0.0", 8080},
		{"*:5173", "*", 5173},
		{"[::]:5432", "[::]", 5432},
		{"[::1]:6379", "[::1]", 6379},
		{"127.0.0.1:44383 (LISTEN)", "127.0.0.1", 44383},
		{"invalid", "invalid", 0},
	}

	for _, tt := range tests {
		addr, port := parseAddrAndPort(tt.input)
		if addr != tt.expectedAddr || port != tt.expectedPort {
			t.Errorf("parseAddrAndPort(%q) = (%q, %d); want (%q, %d)",
				tt.input, addr, port, tt.expectedAddr, tt.expectedPort)
		}
	}
}

func TestInferFramework(t *testing.T) {
	tests := []struct {
		name        string
		cmdline     string
		port        int
		expFw       string
		expCategory string
	}{
		{
			name:        "node",
			cmdline:     "node /home/user/app/node_modules/next/dist/bin/next start",
			port:        3000,
			expFw:       "Next.js",
			expCategory: "Frontend",
		},
		{
			name:        "vite",
			cmdline:     "node /home/user/app/node_modules/vite/bin/vite.js",
			port:        5173,
			expFw:       "Vite",
			expCategory: "Frontend",
		},
		{
			name:        "python3",
			cmdline:     "python3 -m uvicorn main:app --port 8000",
			port:        8000,
			expFw:       "FastAPI",
			expCategory: "Backend",
		},
		{
			name:        "postgres",
			cmdline:     "/usr/lib/postgresql/16/bin/postgres",
			port:        5432,
			expFw:       "PostgreSQL",
			expCategory: "Database",
		},
		{
			name:        "redis-server",
			cmdline:     "/usr/bin/redis-server 127.0.0.1:6379",
			port:        6379,
			expFw:       "Redis",
			expCategory: "Database",
		},
		{
			name:        "ollama",
			cmdline:     "ollama serve",
			port:        11434,
			expFw:       "Ollama AI",
			expCategory: "AI/ML",
		},
		{
			name:        "dotnet",
			cmdline:     "dotnet run --project MyApp.csproj",
			port:        5000,
			expFw:       "ASP.NET Core",
			expCategory: "Backend",
		},
	}

	for _, tt := range tests {
		p := &model.PortInfo{
			ProcessName: tt.name,
			CommandLine: tt.cmdline,
			Port:        tt.port,
		}
		inferFramework(p)
		if p.Framework != tt.expFw {
			t.Errorf("inferFramework(%s): got Framework %q, want %q", tt.name, p.Framework, tt.expFw)
		}
		if p.DevCategory != tt.expCategory {
			t.Errorf("inferFramework(%s): got DevCategory %q, want %q", tt.name, p.DevCategory, tt.expCategory)
		}
	}
}

func TestListPorts(t *testing.T) {
	ports, err := ListPorts()
	if err != nil {
		t.Fatalf("ListPorts error: %v", err)
	}
	// At least something is running (like WSL2 ssh or resolved)
	t.Logf("Found %d active listening ports", len(ports))
}
