package tailscaleops

import (
	"bytes"
	"os"
	"path/filepath"
	"strings"
	"testing"
)

func TestAuthToken_EnsureAndGet(t *testing.T) {
	tmpDir := t.TempDir()
	tokenFile := filepath.Join(tmpDir, "test_token.secret")
	SetTokenPathForTest(tokenFile)
	defer SetTokenPathForTest("")

	tok1, err := EnsureAuthToken()
	if err != nil {
		t.Fatalf("unexpected error generating token: %v", err)
	}
	if len(tok1) != 64 {
		t.Fatalf("expected 64-char hex token, got length %d: %s", len(tok1), tok1)
	}

	// Verify written file
	data, err := os.ReadFile(tokenFile)
	if err != nil {
		t.Fatalf("expected file %s to exist: %v", tokenFile, err)
	}
	if strings.TrimSpace(string(data)) != tok1 {
		t.Fatalf("token file content mismatch: expected %s, got %s", tok1, string(data))
	}

	// Calling EnsureAuthToken or GetAuthToken again must return identical token
	tok2, err := EnsureAuthToken()
	if err != nil {
		t.Fatalf("unexpected error re-reading token: %v", err)
	}
	if tok1 != tok2 {
		t.Fatalf("token changed on second call: %s vs %s", tok1, tok2)
	}

	tok3 := GetAuthToken()
	if tok3 != tok1 {
		t.Fatalf("GetAuthToken mismatch: %s vs %s", tok3, tok1)
	}
}

func TestGenerateQRCodeString(t *testing.T) {
	qr, err := GenerateQRCodeString("http://127.0.0.1:7890/?token=abc123xyz")
	if err != nil {
		t.Fatalf("failed to generate QR code: %v", err)
	}
	if len(qr) == 0 {
		t.Fatalf("expected non-empty QR code string")
	}
	if !strings.Contains(qr, "▄") && !strings.Contains(qr, "▀") && !strings.Contains(qr, "█") {
		t.Fatalf("expected unicode block characters in small QR code output")
	}
}

func TestGetPairingInfo(t *testing.T) {
	tmpDir := t.TempDir()
	tokenFile := filepath.Join(tmpDir, "test_token.secret")
	SetTokenPathForTest(tokenFile)
	defer SetTokenPathForTest("")

	info := GetPairingInfo(7890)
	if info.Port != 7890 {
		t.Errorf("expected port 7890, got %d", info.Port)
	}
	if info.Token == "" {
		t.Errorf("expected non-empty token")
	}
	if !strings.Contains(info.WebURL, ":7890/?token=") {
		t.Errorf("expected WebURL to contain port and token query param, got %s", info.WebURL)
	}
	if !strings.HasPrefix(info.SSHCommand, "ssh ") {
		t.Errorf("expected SSHCommand to start with 'ssh ', got %s", info.SSHCommand)
	}
	if len(info.QRCode) == 0 {
		t.Errorf("expected non-empty QRCode")
	}
}

func TestPrintPairingInfo(t *testing.T) {
	tmpDir := t.TempDir()
	tokenFile := filepath.Join(tmpDir, "test_token.secret")
	SetTokenPathForTest(tokenFile)
	defer SetTokenPathForTest("")

	var buf bytes.Buffer
	PrintPairingInfo(&buf, 7890)
	out := buf.String()

	if !strings.Contains(out, "AGYMOBILE") {
		t.Errorf("expected AGYMOBILE in output, got %s", out)
	}
	if !strings.Contains(out, "Tailscale IP:") {
		t.Errorf("expected Tailscale IP in output")
	}
	if !strings.Contains(out, "SSH Connect:") {
		t.Errorf("expected SSH Connect in output")
	}
	if !strings.Contains(out, "Web URL:") {
		t.Errorf("expected Web URL in output")
	}
	if !strings.Contains(out, "Auth Token:") {
		t.Errorf("expected Auth Token in output")
	}
	if !strings.Contains(out, "Scan with your phone camera") {
		t.Errorf("expected QR scan instructions in output")
	}
}
