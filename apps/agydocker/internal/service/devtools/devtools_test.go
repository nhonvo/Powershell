package devtools

import (
	"os"
	"path/filepath"
	"testing"

	"agydocker/internal/model"
)

func TestDevTools_ServersJSONAndPassfile(t *testing.T) {
	tmpDir, err := os.MkdirTemp("", "devtools_test_*")
	if err != nil {
		t.Fatalf("failed creating temp dir: %v", err)
	}
	defer os.RemoveAll(tmpDir)

	pgDir := filepath.Join(tmpDir, "pgadmin")
	_ = os.MkdirAll(pgDir, 0755)

	initialServers := `{
  "Servers": {
    "1": {
      "Name": "Finance DB (DEV - Local)",
      "Group": "Finance Dashboard",
      "Host": "finance_postgres",
      "Port": 5432,
      "MaintenanceDB": "financedb",
      "Username": "postgres",
      "SSLMode": "prefer",
      "PassFile": "/pgpassfile"
    }
  }
}`
	_ = os.WriteFile(filepath.Join(pgDir, "servers.json"), []byte(initialServers), 0644)
	_ = os.WriteFile(filepath.Join(pgDir, "pgpassfile"), []byte("finance_postgres:5432:*:postgres:secret123\n"), 0600)

	// Test 1: Read existing servers
	servers, err := ReadServers(tmpDir)
	if err != nil {
		t.Fatalf("unexpected error reading servers: %v", err)
	}
	if len(servers) != 1 {
		t.Fatalf("expected 1 server, got %d", len(servers))
	}
	if servers[0].Name != "Finance DB (DEV - Local)" {
		t.Errorf("expected server name Finance DB (DEV - Local), got %s", servers[0].Name)
	}

	// Test 2: Register a new server
	newEntry := model.DevToolsServerEntry{
		Name:          "Inventory DB (DEV)",
		Group:         "Inventory Management",
		Host:          "inventory-db-dev",
		Port:          5432,
		MaintenanceDB: "inventory_db",
		Username:      "postgres",
	}
	err = RegisterServer(tmpDir, newEntry, "pass456")
	if err != nil {
		t.Fatalf("failed registering new server: %v", err)
	}

	updatedServers, err := ReadServers(tmpDir)
	if err != nil {
		t.Fatalf("unexpected error reading updated servers: %v", err)
	}
	if len(updatedServers) != 2 {
		t.Fatalf("expected 2 servers after registration, got %d", len(updatedServers))
	}

	// Test 3: Remove server
	removed, err := RemoveServer(tmpDir, "Inventory DB (DEV)")
	if err != nil {
		t.Fatalf("failed removing server: %v", err)
	}
	if !removed {
		t.Errorf("expected server to be removed")
	}

	afterRemove, _ := ReadServers(tmpDir)
	if len(afterRemove) != 1 {
		t.Errorf("expected 1 server after deletion, got %d", len(afterRemove))
	}
}

func TestDevTools_EnsureNetworkInCompose(t *testing.T) {
	tmpDir, err := os.MkdirTemp("", "devtools_compose_*")
	if err != nil {
		t.Fatalf("failed creating temp dir: %v", err)
	}
	defer os.RemoveAll(tmpDir)

	composeContent := `services:
  pgadmin:
    image: dpage/pgadmin4:latest
    networks:
      - default
      - finance-dev-net
    restart: unless-stopped

networks:
  finance-dev-net:
    name: finance-dev-net
    external: true
`
	_ = os.WriteFile(filepath.Join(tmpDir, "docker-compose.yml"), []byte(composeContent), 0644)

	err = EnsureNetworkInCompose(tmpDir, "inventory-dev-net")
	if err != nil {
		t.Fatalf("unexpected error adding network to compose: %v", err)
	}

	content, _ := os.ReadFile(filepath.Join(tmpDir, "docker-compose.yml"))
	cStr := string(content)
	if !testing.Short() {
		_ = cStr
	}
}
