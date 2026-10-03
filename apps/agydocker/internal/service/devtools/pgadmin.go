package devtools

import (
	"bufio"
	"encoding/json"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"sort"
	"strconv"
	"strings"
	"sync"

	"agydocker/internal/model"
)

var fileMu sync.Mutex

// ReadServers parses the pgadmin/servers.json file.
func ReadServers(devToolsDir string) ([]model.DevToolsServerEntry, error) {
	jsonPath := filepath.Join(devToolsDir, "pgadmin", "servers.json")
	data, err := os.ReadFile(jsonPath)
	if err != nil {
		return nil, fmt.Errorf("failed reading %s: %w", jsonPath, err)
	}

	var root model.DevToolsServersJSON
	if err := json.Unmarshal(data, &root); err != nil {
		return nil, fmt.Errorf("failed parsing servers.json: %w", err)
	}

	var result []model.DevToolsServerEntry
	for id, entry := range root.Servers {
		entry.ID = id
		result = append(result, entry)
	}

	// Sort numerically by ID
	sort.Slice(result, func(i, j int) bool {
		idI, errI := strconv.Atoi(result[i].ID)
		idJ, errJ := strconv.Atoi(result[j].ID)
		if errI == nil && errJ == nil {
			return idI < idJ
		}
		return result[i].ID < result[j].ID
	})

	return result, nil
}

// RegisterServer adds or updates a database server entry in servers.json and records password in pgpassfile.
func RegisterServer(devToolsDir string, entry model.DevToolsServerEntry, password string) error {
	fileMu.Lock()
	defer fileMu.Unlock()

	jsonPath := filepath.Join(devToolsDir, "pgadmin", "servers.json")
	passPath := filepath.Join(devToolsDir, "pgadmin", "pgpassfile")

	var root model.DevToolsServersJSON
	data, err := os.ReadFile(jsonPath)
	if err == nil {
		_ = json.Unmarshal(data, &root)
	}
	if root.Servers == nil {
		root.Servers = make(map[string]model.DevToolsServerEntry)
	}

	// Determine next ID or update existing
	targetID := ""
	maxID := 0
	for id, existing := range root.Servers {
		if val, err := strconv.Atoi(id); err == nil && val > maxID {
			maxID = val
		}
		if strings.EqualFold(existing.Name, entry.Name) || (existing.Host == entry.Host && existing.Port == entry.Port && existing.MaintenanceDB == entry.MaintenanceDB) {
			targetID = id
			break
		}
	}

	if targetID == "" {
		targetID = strconv.Itoa(maxID + 1)
	}

	if entry.PassFile == "" {
		entry.PassFile = "/pgpassfile"
	}
	if entry.SSLMode == "" {
		entry.SSLMode = "prefer"
	}
	if entry.Username == "" {
		entry.Username = "postgres"
	}

	root.Servers[targetID] = entry

	// Atomically write servers.json
	marshaled, err := json.MarshalIndent(root, "", "  ")
	if err != nil {
		return fmt.Errorf("failed marshaling servers.json: %w", err)
	}
	tmpJson := jsonPath + ".tmp"
	if err := os.WriteFile(tmpJson, marshaled, 0644); err != nil {
		return err
	}
	if err := os.Rename(tmpJson, jsonPath); err != nil {
		return err
	}

	// Update pgpassfile if password is provided
	if password != "" {
		if err := updatePassfile(passPath, entry.Host, entry.Port, entry.MaintenanceDB, entry.Username, password); err != nil {
			return fmt.Errorf("failed updating pgpassfile: %w", err)
		}
	}

	return nil
}

// updatePassfile appends or updates credentials with mode 0600
func updatePassfile(passPath string, host string, port int, dbname string, username string, password string) error {
	newEntry := fmt.Sprintf("%s:%d:*:%s:%s", host, port, username, password)
	if dbname != "" && dbname != "postgres" {
		newEntry = fmt.Sprintf("%s:%d:%s:%s:%s", host, port, dbname, username, password)
	}

	var lines []string
	found := false

	if data, err := os.ReadFile(passPath); err == nil {
		scanner := bufio.NewScanner(strings.NewReader(string(data)))
		for scanner.Scan() {
			line := strings.TrimSpace(scanner.Text())
			if line == "" || strings.HasPrefix(line, "#") {
				lines = append(lines, line)
				continue
			}
			parts := strings.Split(line, ":")
			if len(parts) >= 5 {
				if parts[0] == host && parts[1] == strconv.Itoa(port) && parts[3] == username {
					lines = append(lines, newEntry)
					found = true
					continue
				}
			}
			lines = append(lines, line)
		}
	}

	if !found {
		lines = append(lines, newEntry)
	}

	tmpPass := passPath + ".tmp"
	content := strings.Join(lines, "\n") + "\n"
	if err := os.WriteFile(tmpPass, []byte(content), 0600); err != nil {
		return err
	}
	if err := os.Chmod(tmpPass, 0600); err != nil {
		_ = err
	}
	return os.Rename(tmpPass, passPath)
}

// RemoveServer removes a server entry by name or ID.
func RemoveServer(devToolsDir string, nameOrID string) (bool, error) {
	fileMu.Lock()
	defer fileMu.Unlock()

	jsonPath := filepath.Join(devToolsDir, "pgadmin", "servers.json")
	data, err := os.ReadFile(jsonPath)
	if err != nil {
		return false, err
	}

	var root model.DevToolsServersJSON
	if err := json.Unmarshal(data, &root); err != nil {
		return false, err
	}

	targetKey := ""
	for id, s := range root.Servers {
		if id == nameOrID || strings.EqualFold(s.Name, nameOrID) {
			targetKey = id
			break
		}
	}

	if targetKey == "" {
		return false, nil
	}

	delete(root.Servers, targetKey)

	marshaled, err := json.MarshalIndent(root, "", "  ")
	if err != nil {
		return false, err
	}
	tmpJson := jsonPath + ".tmp"
	if err := os.WriteFile(tmpJson, marshaled, 0644); err != nil {
		return false, err
	}
	if err := os.Rename(tmpJson, jsonPath); err != nil {
		return false, err
	}

	return true, nil
}

// ReloadPgAdminServers executes setup.py load-servers inside dev_tools_pgadmin container.
func ReloadPgAdminServers() error {
	cmd := exec.Command("docker", "exec", "dev_tools_pgadmin",
		"/venv/bin/python3", "/pgadmin4/setup.py", "load-servers", "/pgadmin4/servers.json")
	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("failed reloading servers in pgAdmin container: %w (%s)", err, strings.TrimSpace(string(out)))
	}
	return nil
}
