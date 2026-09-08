package web

import (
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"path/filepath"
	"strings"
	"testing"

	"agymobile/internal/service/tailscaleops"
)

func setupTestToken(t *testing.T) string {
	tmpDir := t.TempDir()
	tokenFile := filepath.Join(tmpDir, "mobile_token.secret")
	tailscaleops.SetTokenPathForTest(tokenFile)
	t.Cleanup(func() {
		tailscaleops.SetTokenPathForTest("")
	})
	return tailscaleops.GetAuthToken()
}

func TestIsLoopback(t *testing.T) {
	cases := []struct {
		addr     string
		expected bool
	}{
		{"127.0.0.1:12345", true},
		{"[::1]:12345", true},
		{"localhost:12345", true},
		{"127.0.0.1", true},
		{"::1", true},
		{"100.86.177.57:12345", false},
		{"192.168.1.50:12345", false},
		{"10.0.0.1:80", false},
	}

	for _, c := range cases {
		req := httptest.NewRequest(http.MethodGet, "/", nil)
		req.RemoteAddr = c.addr
		got := isLoopback(req)
		if got != c.expected {
			t.Errorf("isLoopback(%q) = %v; expected %v", c.addr, got, c.expected)
		}
	}
}

func TestIsAuthorized(t *testing.T) {
	token := setupTestToken(t)

	// Loopback bypass
	reqLoopback := httptest.NewRequest(http.MethodGet, "/", nil)
	reqLoopback.RemoteAddr = "127.0.0.1:54321"
	if !isAuthorized(reqLoopback) {
		t.Errorf("expected loopback to be authorized without token")
	}

	// Remote without token
	reqRemoteNoAuth := httptest.NewRequest(http.MethodGet, "/", nil)
	reqRemoteNoAuth.RemoteAddr = "100.86.177.50:54321"
	if isAuthorized(reqRemoteNoAuth) {
		t.Errorf("expected remote without token to be rejected")
	}

	// Remote with invalid token
	reqRemoteBadToken := httptest.NewRequest(http.MethodGet, "/?token=wrong_token", nil)
	reqRemoteBadToken.RemoteAddr = "100.86.177.50:54321"
	if isAuthorized(reqRemoteBadToken) {
		t.Errorf("expected remote with bad query token to be rejected")
	}

	// Remote with valid query token
	reqRemoteQueryToken := httptest.NewRequest(http.MethodGet, "/?token="+token, nil)
	reqRemoteQueryToken.RemoteAddr = "100.86.177.50:54321"
	if !isAuthorized(reqRemoteQueryToken) {
		t.Errorf("expected remote with valid query token to be authorized")
	}

	// Remote with valid Bearer header
	reqRemoteBearer := httptest.NewRequest(http.MethodGet, "/", nil)
	reqRemoteBearer.RemoteAddr = "100.86.177.50:54321"
	reqRemoteBearer.Header.Set("Authorization", "Bearer "+token)
	if !isAuthorized(reqRemoteBearer) {
		t.Errorf("expected remote with valid Bearer token to be authorized")
	}

	// Remote with wrong Bearer header
	reqRemoteBadBearer := httptest.NewRequest(http.MethodGet, "/", nil)
	reqRemoteBadBearer.RemoteAddr = "100.86.177.50:54321"
	reqRemoteBadBearer.Header.Set("Authorization", "Bearer bad_secret")
	if isAuthorized(reqRemoteBadBearer) {
		t.Errorf("expected remote with bad Bearer token to be rejected")
	}
}

func TestHandleFlushRAM_AuthEnforcement(t *testing.T) {
	token := setupTestToken(t)

	// Remote request without token -> 401 Unauthorized
	reqUnauth := httptest.NewRequest(http.MethodPost, "/api/action/flush-ram", nil)
	reqUnauth.RemoteAddr = "100.86.177.50:54321"
	wUnauth := httptest.NewRecorder()
	handleFlushRAM(wUnauth, reqUnauth)
	if wUnauth.Code != http.StatusUnauthorized {
		t.Errorf("expected 401 Unauthorized for remote unauthenticated request, got %d", wUnauth.Code)
	}

	// Remote request with valid query token -> authorized (code is 200 or 500 depending on WSL/host permissions)
	reqAuthQuery := httptest.NewRequest(http.MethodPost, "/api/action/flush-ram?token="+token, nil)
	reqAuthQuery.RemoteAddr = "100.86.177.50:54321"
	wAuthQuery := httptest.NewRecorder()
	handleFlushRAM(wAuthQuery, reqAuthQuery)
	if wAuthQuery.Code == http.StatusUnauthorized {
		t.Errorf("expected request with valid token to pass auth, got 401")
	}

	// Remote request with valid Bearer header -> authorized
	reqAuthHeader := httptest.NewRequest(http.MethodPost, "/api/action/flush-ram", nil)
	reqAuthHeader.RemoteAddr = "100.86.177.50:54321"
	reqAuthHeader.Header.Set("Authorization", "Bearer "+token)
	wAuthHeader := httptest.NewRecorder()
	handleFlushRAM(wAuthHeader, reqAuthHeader)
	if wAuthHeader.Code == http.StatusUnauthorized {
		t.Errorf("expected request with valid Bearer header to pass auth, got 401")
	}

	// Loopback request without token -> authorized
	reqLoopback := httptest.NewRequest(http.MethodPost, "/api/action/flush-ram", nil)
	reqLoopback.RemoteAddr = "127.0.0.1:54321"
	wLoopback := httptest.NewRecorder()
	handleFlushRAM(wLoopback, reqLoopback)
	if wLoopback.Code == http.StatusUnauthorized {
		t.Errorf("expected loopback request without token to pass auth, got 401")
	}

	// Method Not Allowed
	reqGet := httptest.NewRequest(http.MethodGet, "/api/action/flush-ram?token="+token, nil)
	reqGet.RemoteAddr = "100.86.177.50:54321"
	wGet := httptest.NewRecorder()
	handleFlushRAM(wGet, reqGet)
	if wGet.Code != http.StatusMethodNotAllowed {
		t.Errorf("expected 405 Method Not Allowed for GET, got %d", wGet.Code)
	}
}

func TestHandleDockerStopAll_AuthEnforcement(t *testing.T) {
	token := setupTestToken(t)

	// Remote request without token -> 401 Unauthorized
	reqUnauth := httptest.NewRequest(http.MethodPost, "/api/action/docker-stop-all", nil)
	reqUnauth.RemoteAddr = "100.86.177.50:54321"
	wUnauth := httptest.NewRecorder()
	handleDockerStopAll(wUnauth, reqUnauth)
	if wUnauth.Code != http.StatusUnauthorized {
		t.Errorf("expected 401 Unauthorized, got %d", wUnauth.Code)
	}

	// Remote request with valid token -> 200 OK
	reqAuth := httptest.NewRequest(http.MethodPost, "/api/action/docker-stop-all?token="+token, nil)
	reqAuth.RemoteAddr = "100.86.177.50:54321"
	wAuth := httptest.NewRecorder()
	handleDockerStopAll(wAuth, reqAuth)
	if wAuth.Code != http.StatusOK {
		t.Errorf("expected 200 OK, got %d", wAuth.Code)
	}

	// Loopback request without token -> 200 OK
	reqLoopback := httptest.NewRequest(http.MethodPost, "/api/action/docker-stop-all", nil)
	reqLoopback.RemoteAddr = "127.0.0.1:54321"
	wLoopback := httptest.NewRecorder()
	handleDockerStopAll(wLoopback, reqLoopback)
	if wLoopback.Code != http.StatusOK {
		t.Errorf("expected 200 OK for loopback, got %d", wLoopback.Code)
	}

	// Method Not Allowed
	reqGet := httptest.NewRequest(http.MethodGet, "/api/action/docker-stop-all?token="+token, nil)
	reqGet.RemoteAddr = "100.86.177.50:54321"
	wGet := httptest.NewRecorder()
	handleDockerStopAll(wGet, reqGet)
	if wGet.Code != http.StatusMethodNotAllowed {
		t.Errorf("expected 405 Method Not Allowed for GET, got %d", wGet.Code)
	}
}

func TestHandleStats(t *testing.T) {
	req := httptest.NewRequest(http.MethodGet, "/api/stats", nil)
	w := httptest.NewRecorder()
	handleStats(w, req)

	if w.Code != http.StatusOK {
		t.Errorf("expected 200 OK, got %d", w.Code)
	}

	var data map[string]interface{}
	if err := json.Unmarshal(w.Body.Bytes(), &data); err != nil {
		t.Fatalf("expected valid JSON: %v", err)
	}
	if _, ok := data["host"]; !ok {
		t.Errorf("expected 'host' in stats JSON")
	}
	if _, ok := data["tailscale"]; !ok {
		t.Errorf("expected 'tailscale' in stats JSON")
	}
}

func TestHandleIndex(t *testing.T) {
	req := httptest.NewRequest(http.MethodGet, "/", nil)
	w := httptest.NewRecorder()
	handleIndex(w, req)

	if w.Code != http.StatusOK {
		t.Errorf("expected 200 OK, got %d", w.Code)
	}
	if !strings.Contains(w.Body.String(), "AGYMOBILE") {
		t.Errorf("expected AGYMOBILE in HTML body")
	}

	// Subpath should 404
	req404 := httptest.NewRequest(http.MethodGet, "/nonexistent", nil)
	w404 := httptest.NewRecorder()
	handleIndex(w404, req404)
	if w404.Code != http.StatusNotFound {
		t.Errorf("expected 404 for /nonexistent, got %d", w404.Code)
	}
}
