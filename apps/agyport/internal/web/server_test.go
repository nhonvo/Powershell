package web

import (
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"
)

func TestHandleIndex(t *testing.T) {
	req := httptest.NewRequest("GET", "/", nil)
	w := httptest.NewRecorder()

	handleIndex(w, req)
	resp := w.Result()

	if resp.StatusCode != http.StatusOK {
		t.Fatalf("expected 200 OK, got %d", resp.StatusCode)
	}

	body := w.Body.String()
	if !strings.Contains(body, "AGYPORT") {
		t.Errorf("expected dashboard HTML to contain 'AGYPORT'")
	}
	if !strings.Contains(body, "reclaimDevRAM") {
		t.Errorf("expected dashboard HTML to contain reclaimDevRAM function")
	}
}

func TestHandleStats(t *testing.T) {
	req := httptest.NewRequest("GET", "/api/stats", nil)
	w := httptest.NewRecorder()

	handleStats(w, req)
	resp := w.Result()

	if resp.StatusCode != http.StatusOK {
		t.Fatalf("expected 200 OK, got %d", resp.StatusCode)
	}

	var stats StatsResponse
	err := json.Unmarshal(w.Body.Bytes(), &stats)
	if err != nil {
		t.Fatalf("failed to parse StatsResponse JSON: %v", err)
	}

	if stats.Timestamp == "" {
		t.Errorf("expected non-empty timestamp")
	}
}

func TestHandleKillValidation(t *testing.T) {
	// Missing port
	req := httptest.NewRequest("POST", "/api/kill", nil)
	w := httptest.NewRecorder()
	handleKill(w, req)
	if w.Result().StatusCode != http.StatusBadRequest {
		t.Errorf("expected 400 for missing port, got %d", w.Result().StatusCode)
	}

	// Invalid port
	req = httptest.NewRequest("POST", "/api/kill?port=abc", nil)
	w = httptest.NewRecorder()
	handleKill(w, req)
	if w.Result().StatusCode != http.StatusBadRequest {
		t.Errorf("expected 400 for invalid port, got %d", w.Result().StatusCode)
	}
}

func TestHandleReclaimDryRun(t *testing.T) {
	req := httptest.NewRequest("POST", "/api/reclaim?dry_run=true", nil)
	w := httptest.NewRecorder()

	handleReclaim(w, req)
	if w.Result().StatusCode != http.StatusOK {
		t.Fatalf("expected 200 OK, got %d", w.Result().StatusCode)
	}

	var res map[string]any
	err := json.Unmarshal(w.Body.Bytes(), &res)
	if err != nil {
		t.Fatalf("failed to decode reclaim json: %v", err)
	}
	if _, ok := res["freed_formatted"]; !ok {
		t.Errorf("expected freed_formatted in response")
	}
}
