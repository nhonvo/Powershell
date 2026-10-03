package devtools

import (
	"bufio"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strings"

	"agydocker/internal/model"
)

// DetectProjectDB locates a database service within a project directory or from a container name.
func DetectProjectDB(pathOrContainer string) (*model.DetectedDBTarget, error) {
	target := strings.TrimSpace(pathOrContainer)
	if target == "" || target == "." {
		cwd, err := os.Getwd()
		if err == nil {
			target = cwd
		}
	}

	// 1. Check if target is a path to a directory
	if info, err := os.Stat(target); err == nil && info.IsDir() {
		return detectFromDirectory(target)
	}

	// 2. Check if target is a container name or ID
	return detectFromContainer(target)
}

func detectFromDirectory(dir string) (*model.DetectedDBTarget, error) {
	projName := filepath.Base(dir)
	composePath := filepath.Join(dir, "docker-compose.yml")
	if _, err := os.Stat(composePath); os.IsNotExist(err) {
		composePath = filepath.Join(dir, "compose.yaml")
	}

	envVars := make(map[string]string)
	envPath := filepath.Join(dir, ".env")
	if data, err := os.ReadFile(envPath); err == nil {
		scanner := bufio.NewScanner(strings.NewReader(string(data)))
		for scanner.Scan() {
			line := strings.TrimSpace(scanner.Text())
			if line == "" || strings.HasPrefix(line, "#") {
				continue
			}
			parts := strings.SplitN(line, "=", 2)
			if len(parts) == 2 {
				key := strings.TrimSpace(parts[0])
				val := strings.Trim(strings.TrimSpace(parts[1]), `"'`)
				envVars[key] = val
			}
		}
	}

	// Read compose file lines
	data, err := os.ReadFile(composePath)
	if err != nil {
		return nil, fmt.Errorf("no docker-compose.yml found in %s", dir)
	}

	content := string(data)
	dbTarget := &model.DetectedDBTarget{
		ProjectName:  projName,
		Port:         5432,
		Username:     "postgres",
		Password:     "postgres",
		DatabaseName: "postgres",
	}

	// Simple heuristic extraction
	lines := strings.Split(content, "\n")
	var currentService string
	isDBService := false

	for _, line := range lines {
		trimmed := strings.TrimSpace(line)
		if strings.HasSuffix(trimmed, ":") && strings.HasPrefix(line, "  ") && !strings.HasPrefix(line, "    ") {
			currentService = strings.TrimSuffix(trimmed, ":")
			isDBService = false
		}

		if strings.Contains(trimmed, "image:") {
			lowerImg := strings.ToLower(trimmed)
			if strings.Contains(lowerImg, "postgres") {
				dbTarget.Engine = "postgres"
				dbTarget.ContainerName = currentService
				dbTarget.Host = currentService
				isDBService = true
			} else if strings.Contains(lowerImg, "mongo") {
				dbTarget.Engine = "mongo"
				dbTarget.ContainerName = currentService
				dbTarget.Host = currentService
				dbTarget.Port = 27017
				isDBService = true
			}
		}

		if isDBService {
			if strings.Contains(trimmed, "container_name:") {
				parts := strings.Split(trimmed, ":")
				if len(parts) >= 2 {
					dbTarget.ContainerName = strings.TrimSpace(parts[1])
					dbTarget.Host = dbTarget.ContainerName
				}
			}
			if strings.Contains(trimmed, "POSTGRES_DB=") || strings.Contains(trimmed, "POSTGRES_DB:") {
				val := extractValue(trimmed, envVars)
				if val != "" {
					dbTarget.DatabaseName = val
				}
			}
			if strings.Contains(trimmed, "POSTGRES_USER=") || strings.Contains(trimmed, "POSTGRES_USER:") {
				val := extractValue(trimmed, envVars)
				if val != "" {
					dbTarget.Username = val
				}
			}
			if strings.Contains(trimmed, "POSTGRES_PASSWORD=") || strings.Contains(trimmed, "POSTGRES_PASSWORD:") {
				val := extractValue(trimmed, envVars)
				if val != "" {
					dbTarget.Password = val
				}
			}
		}
	}

	// Check if container is running live to get its exact network
	if dbTarget.ContainerName != "" {
		if live, err := detectFromContainer(dbTarget.ContainerName); err == nil && live != nil {
			if live.NetworkName != "" {
				dbTarget.NetworkName = live.NetworkName
			}
			if live.Password != "" && dbTarget.Password == "postgres" {
				dbTarget.Password = live.Password
			}
			dbTarget.IsRunning = live.IsRunning
		}
	}

	if dbTarget.NetworkName == "" {
		// Heuristic compose network default
		cleanName := strings.ToLower(strings.ReplaceAll(projName, " ", ""))
		cleanName = strings.ReplaceAll(cleanName, "_", "")
		cleanName = strings.ReplaceAll(cleanName, "-", "")
		dbTarget.NetworkName = cleanName + "_default"
	}

	return dbTarget, nil
}

func detectFromContainer(containerName string) (*model.DetectedDBTarget, error) {
	cmd := exec.Command("docker", "inspect", containerName,
		"--format", "{{.Name}}|{{.Config.Image}}|{{range $k, $v := .NetworkSettings.Networks}}{{$k}}{{break}}{{end}}|{{range .Config.Env}}{{.}};{{end}}|{{.State.Running}}")
	out, err := cmd.CombinedOutput()
	if err != nil {
		return nil, fmt.Errorf("container '%s' not found: %s", containerName, strings.TrimSpace(string(out)))
	}

	parts := strings.Split(strings.TrimSpace(string(out)), "|")
	if len(parts) < 5 {
		return nil, fmt.Errorf("unexpected inspect format for %s", containerName)
	}

	rawName := strings.TrimPrefix(parts[0], "/")
	image := strings.ToLower(parts[1])
	network := parts[2]
	envs := strings.Split(parts[3], ";")
	isRunning := strings.EqualFold(parts[4], "true")

	target := &model.DetectedDBTarget{
		ProjectName:   rawName,
		ContainerName: rawName,
		Host:          rawName,
		NetworkName:   network,
		IsRunning:     isRunning,
		Port:          5432,
		Username:      "postgres",
		Password:      "postgres",
		DatabaseName:  "postgres",
	}

	if strings.Contains(image, "postgres") {
		target.Engine = "postgres"
	} else if strings.Contains(image, "mongo") {
		target.Engine = "mongo"
		target.Port = 27017
	} else {
		target.Engine = "generic"
	}

	for _, e := range envs {
		kv := strings.SplitN(e, "=", 2)
		if len(kv) == 2 {
			k := strings.TrimSpace(kv[0])
			v := strings.TrimSpace(kv[1])
			switch k {
			case "POSTGRES_DB":
				target.DatabaseName = v
			case "POSTGRES_USER":
				target.Username = v
			case "POSTGRES_PASSWORD":
				target.Password = v
			}
		}
	}

	return target, nil
}

func extractValue(line string, envVars map[string]string) string {
	parts := strings.SplitN(line, "=", 2)
	if len(parts) < 2 {
		parts = strings.SplitN(line, ":", 2)
	}
	if len(parts) < 2 {
		return ""
	}
	val := strings.TrimSpace(parts[1])
	val = strings.Trim(val, `"'`)
	if strings.HasPrefix(val, "${") && strings.HasSuffix(val, "}") {
		varName := strings.TrimSuffix(strings.TrimPrefix(val, "${"), "}")
		if v, ok := envVars[varName]; ok {
			return v
		}
	}
	return val
}

// SyncAllRunningDatabases searches all active containers and attaches database containers to pgAdmin.
func SyncAllRunningDatabases(devToolsDir string) (int, error) {
	cmd := exec.Command("docker", "ps", "--format", "{{.Names}}|{{.Image}}|{{.Networks}}")
	out, err := cmd.Output()
	if err != nil {
		return 0, fmt.Errorf("failed listing running containers: %w", err)
	}

	attachedCount := 0
	lines := strings.Split(strings.TrimSpace(string(out)), "\n")
	for _, l := range lines {
		if strings.TrimSpace(l) == "" {
			continue
		}
		parts := strings.Split(l, "|")
		if len(parts) < 3 {
			continue
		}
		cName := parts[0]
		image := strings.ToLower(parts[1])
		network := parts[2]

		if cName == "dev_tools_pgadmin" || cName == "dev_tools_mongo_express" {
			continue
		}

		if strings.Contains(image, "postgres") {
			target, err := detectFromContainer(cName)
			if err == nil && target != nil {
				// 1. Connect network
				_ = ConnectNetwork(network)
				_ = EnsureNetworkInCompose(devToolsDir, network)

				// 2. Register to pgAdmin
				entry := model.DevToolsServerEntry{
					Name:          fmt.Sprintf("%s (%s)", target.ContainerName, target.DatabaseName),
					Group:         "Auto-Synced Containers",
					Host:          target.ContainerName,
					Port:          target.Port,
					MaintenanceDB: target.DatabaseName,
					Username:      target.Username,
					SSLMode:       "prefer",
					PassFile:      "/pgpassfile",
				}
				if err := RegisterServer(devToolsDir, entry, target.Password); err == nil {
					attachedCount++
				}
			}
		}
	}

	if attachedCount > 0 {
		_ = ReloadPgAdminServers()
	}

	return attachedCount, nil
}
