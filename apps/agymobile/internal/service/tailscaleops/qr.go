package tailscaleops

import (
	"crypto/rand"
	"encoding/hex"
	"fmt"
	"io"
	"os"
	"path/filepath"
	"strings"

	"github.com/skip2/go-qrcode"
)

var tokenPathOverride string

// SetTokenPathForTest allows test isolation by overriding the token secret file path
func SetTokenPathForTest(p string) {
	tokenPathOverride = p
}

func getTokenPath() string {
	if tokenPathOverride != "" {
		return tokenPathOverride
	}
	if p := os.Getenv("AGY_MOBILE_TOKEN_PATH"); p != "" {
		return p
	}
	home, err := os.UserHomeDir()
	if err != nil {
		home = os.Getenv("HOME")
	}
	return filepath.Join(home, ".config", "antigravity", "mobile_token.secret")
}

// EnsureAuthToken ensures a persistent bearer token is loaded or generated at ~/.config/antigravity/mobile_token.secret
func EnsureAuthToken() (string, error) {
	tokenPath := getTokenPath()
	tokenDir := filepath.Dir(tokenPath)

	// 1. Read existing token if available
	if data, err := os.ReadFile(tokenPath); err == nil {
		token := strings.TrimSpace(string(data))
		if token != "" {
			return token, nil
		}
	}

	// 2. Generate a secure random 32-byte (64 hex characters) token
	if err := os.MkdirAll(tokenDir, 0700); err != nil {
		return "", fmt.Errorf("failed to create config dir: %w", err)
	}

	bytes := make([]byte, 32)
	if _, err := rand.Read(bytes); err != nil {
		return "", fmt.Errorf("failed to generate random token: %w", err)
	}

	token := hex.EncodeToString(bytes)
	if err := os.WriteFile(tokenPath, []byte(token+"\n"), 0600); err != nil {
		return "", fmt.Errorf("failed to write token file: %w", err)
	}

	return token, nil
}

// GetAuthToken retrieves or generates the mobile auth token
func GetAuthToken() string {
	token, _ := EnsureAuthToken()
	return token
}

// PairingInfo encapsulates connection parameters and pairing assets
type PairingInfo struct {
	TailscaleIP string `json:"tailscale_ip"`
	HostName    string `json:"hostname"`
	MagicDNS    string `json:"magic_dns"`
	Port        int    `json:"port"`
	Token       string `json:"token"`
	WebURL      string `json:"web_url"`
	SSHCommand  string `json:"ssh_command"`
	QRCode      string `json:"qr_code"`
}

// GenerateQRCodeString generates an ASCII/ANSI string representation of a QR code using unicode half-blocks
func GenerateQRCodeString(content string) (string, error) {
	qr, err := qrcode.New(content, qrcode.Medium)
	if err != nil {
		return "", err
	}
	return qr.ToSmallString(false), nil
}

// GetPairingInfo resolves pairing information for mobile development clients
func GetPairingInfo(port int) PairingInfo {
	if port <= 0 {
		port = 7890
	}
	ts := GetTailscaleInfo()
	token := GetAuthToken()

	user := os.Getenv("USER")
	if user == "" {
		user = "truongnhon"
	}

	ip := ts.IPv4
	if ip == "" || ip == "127.0.0.1" {
		ip = "127.0.0.1"
	}

	webURL := fmt.Sprintf("http://%s:%d/?token=%s", ip, port, token)
	sshCmd := fmt.Sprintf("ssh %s@%s", user, ip)

	qrStr, _ := GenerateQRCodeString(webURL)

	return PairingInfo{
		TailscaleIP: ts.IPv4,
		HostName:    ts.HostName,
		MagicDNS:    ts.MagicDNS,
		Port:        port,
		Token:       token,
		WebURL:      webURL,
		SSHCommand:  sshCmd,
		QRCode:      qrStr,
	}
}

// PrintPairingInfo writes a formatted pairing terminal helper with QR code to w
func PrintPairingInfo(w io.Writer, port int) {
	info := GetPairingInfo(port)

	fmt.Fprintln(w, "\r\n📱 \033[1;36mAGYMOBILE · Mobile Pairing & Remote Station\033[0m")
	fmt.Fprintln(w, "──────────────────────────────────────────────────────────────────────")
	fmt.Fprintf(w, " 🌐 \033[1mTailscale IP:\033[0m   %s\r\n", info.TailscaleIP)
	if info.MagicDNS != "" {
		fmt.Fprintf(w, " 📡 \033[1mMagicDNS:\033[0m       %s\r\n", info.MagicDNS)
	}
	fmt.Fprintf(w, " 🔑 \033[1mSSH Connect:\033[0m    \033[1;32m%s\033[0m\r\n", info.SSHCommand)
	fmt.Fprintf(w, " 🌐 \033[1mWeb URL:\033[0m        \033[1;33m%s\033[0m\r\n", info.WebURL)
	fmt.Fprintf(w, " 🛡️  \033[1mAuth Token:\033[0m     \033[37m%s\033[0m\r\n", info.Token)
	fmt.Fprintln(w, "──────────────────────────────────────────────────────────────────────")

	if info.QRCode != "" {
		fmt.Fprintf(w, "\r\n📲 \033[1mScan with your phone camera to pair Web Cockpit:\033[0m\r\n\r\n")
		fmt.Fprintln(w, info.QRCode)
	}

	fmt.Fprintln(w, "──────────────────────────────────────────────────────────────────────")
	fmt.Fprintln(w, " 💡 \033[37mTermux / ConnectBot / Termius: Run the SSH command above.\033[0m")
	fmt.Fprintln(w, " 💡 \033[37mSafari / Chrome PWA: Scan the QR code, then tap 'Add to Home Screen'.\033[0m")
}
