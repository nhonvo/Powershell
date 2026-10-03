package devtools

import (
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
)

// ConnectNetwork attaches a Docker network to the dev_tools_pgadmin container dynamically.
func ConnectNetwork(networkName string) error {
	if networkName == "" || networkName == "default" || networkName == "bridge" {
		return nil
	}

	// Check if already connected
	inspectCmd := exec.Command("docker", "inspect", "dev_tools_pgadmin", "--format", "{{range $k, $v := .NetworkSettings.Networks}}{{$k}} {{end}}")
	if out, err := inspectCmd.Output(); err == nil {
		currentNetworks := strings.Fields(string(out))
		for _, net := range currentNetworks {
			if net == networkName {
				return nil // Already connected!
			}
		}
	}

	// Connect dynamically without restarting container
	cmd := exec.Command("docker", "network", "connect", networkName, "dev_tools_pgadmin")
	out, err := cmd.CombinedOutput()
	if err != nil {
		outStr := strings.TrimSpace(string(out))
		if strings.Contains(outStr, "already exists in network") {
			return nil
		}
		return fmt.Errorf("failed connecting dev_tools_pgadmin to network %s: %s (%w)", networkName, outStr, err)
	}
	return nil
}

// EnsureNetworkInCompose updates the dev-tools docker-compose.yml to persist the external network across reboots.
func EnsureNetworkInCompose(devToolsDir string, networkName string) error {
	if networkName == "" || networkName == "default" || networkName == "bridge" {
		return nil
	}

	composePath := filepath.Join(devToolsDir, "docker-compose.yml")
	data, err := os.ReadFile(composePath)
	if err != nil {
		return err
	}
	content := string(data)

	// If network already mentioned, nothing to do
	if strings.Contains(content, networkName) {
		return nil
	}

	// Simple YAML append under services.pgadmin.networks and networks:
	// Find lines and insert cleanly
	lines := strings.Split(content, "\n")
	var newLines []string
	inPgAdminNetworks := false
	insertedServiceNet := false
	inRootNetworks := false

	for i, line := range lines {
		trimmed := strings.TrimSpace(line)

		if strings.HasPrefix(trimmed, "networks:") && !inPgAdminNetworks && !insertedServiceNet {
			// In service networks section
			inPgAdminNetworks = true
		} else if inPgAdminNetworks && (strings.HasPrefix(line, "    restart:") || strings.HasPrefix(line, "    volumes:") || strings.HasPrefix(line, "  mongo_express:")) {
			// End of service networks, insert our network
			newLines = append(newLines, fmt.Sprintf("      - %s", networkName))
			inPgAdminNetworks = false
			insertedServiceNet = true
		}

		if strings.HasPrefix(trimmed, "networks:") && !strings.HasPrefix(line, "  ") && !strings.HasPrefix(line, "    ") {
			inRootNetworks = true
		}

		newLines = append(newLines, line)

		if inRootNetworks && i == len(lines)-1 {
			// Append network definition at the very bottom
			newLines = append(newLines, fmt.Sprintf("  %s:\n    name: %s\n    external: true", networkName, networkName))
		}
	}

	// If root networks wasn't found at bottom
	if !inRootNetworks {
		newLines = append(newLines, "\nnetworks:\n"+fmt.Sprintf("  %s:\n    name: %s\n    external: true\n", networkName, networkName))
	}

	return os.WriteFile(composePath, []byte(strings.Join(newLines, "\n")), 0644)
}
