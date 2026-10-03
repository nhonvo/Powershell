package model

import "time"

// DevToolsServerEntry represents a single database server entry inside pgadmin/servers.json
type DevToolsServerEntry struct {
	ID            string `json:"-"`
	Name          string `json:"Name"`
	Group         string `json:"Group"`
	Host          string `json:"Host"`
	Port          int    `json:"Port"`
	MaintenanceDB string `json:"MaintenanceDB"`
	Username      string `json:"Username"`
	SSLMode       string `json:"SSLMode"`
	PassFile      string `json:"PassFile"`
}

// DevToolsServersJSON matches the pgAdmin servers.json root schema
type DevToolsServersJSON struct {
	Servers map[string]DevToolsServerEntry `json:"Servers"`
}

// DevToolsPassEntry represents a single line inside pgadmin/pgpassfile
// Format: hostname:port:database:username:password
type DevToolsPassEntry struct {
	Host     string
	Port     string
	Database string
	Username string
	Password string
}

// DevToolsStackStatus holds live operational telemetry for the centralized tools stack
type DevToolsStackStatus struct {
	RootDir            string
	ComposeFile        string
	PgAdminRunning     bool
	PgAdminURL         string
	PgAdminContainerID string
	PgAdminMemoryMB    float64
	MongoRunning       bool
	MongoURL           string
	MongoContainerID   string
	MongoMemoryMB      float64
	RegisteredServers  []DevToolsServerEntry
	LastChecked        time.Time
}

// DetectedDBTarget represents a database found by the auto-detector in a project
type DetectedDBTarget struct {
	ProjectName   string
	Engine        string // "postgres" or "mongo"
	ContainerName string
	Host          string
	Port          int
	DatabaseName  string
	Username      string
	Password      string
	NetworkName   string
	IsRunning     bool
}
