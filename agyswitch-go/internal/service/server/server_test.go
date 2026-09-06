package server_test

import (
	"bytes"
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"os"
	"strings"
	"testing"

	"agyswitch/internal/service/server"
	"agyswitch/internal/service/store"
	"agyswitch/internal/service/vault"
)

func TestServer_Handlers(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "server_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	v := vault.NewVault(tempDir)
	st := store.NewStore(tempDir, v)
	srv := server.NewServer(st, 0)

	if srv.Port != 8080 {
		t.Errorf("expected default port 8080, got %d", srv.Port)
	}

	handler := srv.Routes()

	// 1. Test GET /api/v1/status
	t.Run("GET /api/v1/status", func(t *testing.T) {
		req := httptest.NewRequest(http.MethodGet, "/api/v1/status", nil)
		rec := httptest.NewRecorder()

		handler.ServeHTTP(rec, req)

		if rec.Code != http.StatusOK {
			t.Errorf("expected status 200, got %d", rec.Code)
		}
		if contentType := rec.Header().Get("Content-Type"); !strings.Contains(contentType, "application/json") {
			t.Errorf("expected Content-Type application/json, got %s", contentType)
		}

		var resp map[string]interface{}
		if err := json.Unmarshal(rec.Body.Bytes(), &resp); err != nil {
			t.Fatalf("failed to parse response JSON: %v", err)
		}
		if _, ok := resp["activeAccount"]; !ok {
			t.Errorf("response missing 'activeAccount'")
		}
		if _, ok := resp["accounts"]; !ok {
			t.Errorf("response missing 'accounts'")
		}
	})

	// 2. Test /api/v1/switch - Method Not Allowed
	t.Run("GET /api/v1/switch Method Not Allowed", func(t *testing.T) {
		req := httptest.NewRequest(http.MethodGet, "/api/v1/switch", nil)
		rec := httptest.NewRecorder()

		handler.ServeHTTP(rec, req)

		if rec.Code != http.StatusMethodNotAllowed {
			t.Errorf("expected status 405, got %d", rec.Code)
		}
	})

	// 3. Test POST /api/v1/switch - Invalid Payload
	t.Run("POST /api/v1/switch Invalid Payload", func(t *testing.T) {
		req := httptest.NewRequest(http.MethodPost, "/api/v1/switch", strings.NewReader(`{"accountName": ""}`))
		rec := httptest.NewRecorder()

		handler.ServeHTTP(rec, req)

		if rec.Code != http.StatusBadRequest {
			t.Errorf("expected status 400 for empty accountName, got %d", rec.Code)
		}
	})

	// 4. Test POST /api/v1/switch - Valid Switch
	t.Run("POST /api/v1/switch Success", func(t *testing.T) {
		payload := []byte(`{"accountName": "target_acc"}`)
		req := httptest.NewRequest(http.MethodPost, "/api/v1/switch", bytes.NewBuffer(payload))
		rec := httptest.NewRecorder()

		handler.ServeHTTP(rec, req)

		if rec.Code != http.StatusOK {
			t.Fatalf("expected status 200, got %d", rec.Code)
		}

		var resp map[string]string
		if err := json.Unmarshal(rec.Body.Bytes(), &resp); err != nil {
			t.Fatalf("failed to parse switch response: %v", err)
		}
		if resp["status"] != "success" || resp["activeAccount"] != "target_acc" {
			t.Errorf("unexpected response: %+v", resp)
		}

		// Verify active account was updated in store
		if active := st.GetActiveAccount(); active != "target_acc" {
			t.Errorf("expected active account in store 'target_acc', got '%s'", active)
		}
	})

	// 5. Test GET / - Index HTML
	t.Run("GET / Index Page", func(t *testing.T) {
		req := httptest.NewRequest(http.MethodGet, "/", nil)
		rec := httptest.NewRecorder()

		handler.ServeHTTP(rec, req)

		if rec.Code != http.StatusOK {
			t.Errorf("expected status 200, got %d", rec.Code)
		}
		if !strings.Contains(rec.Body.String(), "AGYSWITCH Mobile Control") {
			t.Errorf("expected HTML body to contain title")
		}
	})
}
