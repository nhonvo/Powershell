package tailscaleops

import (
	"encoding/json"
	"os/exec"
	"strings"

	"agymobile/internal/model"
)

type tailscaleStatusOutput struct {
	Self struct {
		DNSName      string   `json:"DNSName"`
		HostName     string   `json:"HostName"`
		TailscaleIPs []string `json:"TailscaleIPs"`
		Online       bool     `json:"Online"`
	} `json:"Self"`
	Peer map[string]struct {
		HostName string `json:"HostName"`
		OS       string `json:"OS"`
		Online   bool   `json:"Online"`
	} `json:"Peer"`
}

// GetTailscaleInfo discovers local Tailscale IPv4/IPv6 and connected mobile peers
func GetTailscaleInfo() model.TailscaleInfo {
	info := model.TailscaleInfo{
		HostName: "localhost",
		IPv4:     "127.0.0.1",
		IsOnline: false,
	}

	cmd := exec.Command("tailscale", "status", "--json")
	out, err := cmd.Output()
	if err != nil {
		// Fallback to simple `tailscale ip`
		if ipOut, ipErr := exec.Command("tailscale", "ip", "-4").Output(); ipErr == nil {
			ip := strings.TrimSpace(string(ipOut))
			if ip != "" {
				info.IPv4 = ip
				info.IsOnline = true
			}
		}
		return info
	}

	var status tailscaleStatusOutput
	if err := json.Unmarshal(out, &status); err == nil {
		info.HostName = status.Self.HostName
		info.MagicDNS = strings.TrimSuffix(status.Self.DNSName, ".")
		info.IsOnline = status.Self.Online

		for _, ip := range status.Self.TailscaleIPs {
			if strings.Contains(ip, ".") && info.IPv4 == "127.0.0.1" {
				info.IPv4 = ip
			} else if strings.Contains(ip, ":") && info.IPv6 == "" {
				info.IPv6 = ip
			}
		}

		// Find mobile peer (Android / iOS)
		for _, peer := range status.Peer {
			osLower := strings.ToLower(peer.OS)
			if osLower == "android" || osLower == "ios" {
				info.MobilePeer = peer.HostName + " (" + peer.OS + ")"
				break
			}
		}
	}

	return info
}
