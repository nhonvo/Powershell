package ollamaops

import (
	"encoding/json"
	"fmt"
	"net/http"
	"net/http/httptest"
	"os"
	"path/filepath"
	"strings"
	"testing"
	"time"

	"agyollama/internal/model"
)

func TestClient_IsRunning(t *testing.T) {
	// Server online
	tsOnline := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path == "/api/version" {
			w.WriteHeader(http.StatusOK)
			_, _ = w.Write([]byte(`{"version":"0.3.12"}`))
			return
		}
		w.WriteHeader(http.StatusNotFound)
	}))
	defer tsOnline.Close()

	clientOnline := NewClientWithURL(tsOnline.URL)
	if !clientOnline.IsRunning() {
		t.Errorf("expected clientOnline.IsRunning() to be true")
	}

	// Server offline / invalid port
	clientOffline := NewClientWithURL("http://127.0.0.1:54321")
	if clientOffline.IsRunning() {
		t.Errorf("expected clientOffline.IsRunning() to be false")
	}
}

func TestClient_ListModels(t *testing.T) {
	ts := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path == "/api/tags" && r.Method == http.MethodGet {
			w.WriteHeader(http.StatusOK)
			_, _ = w.Write([]byte(`{
				"models": [
					{
						"name": "qwen2.5-coder:7b",
						"model": "qwen2.5-coder:7b",
						"modified_at": "2024-11-04T19:54:49.2443515Z",
						"size": 4683075200,
						"digest": "sha256:123456",
						"details": {
							"parent_model": "",
							"format": "gguf",
							"family": "qwen2",
							"families": ["qwen2"],
							"parameter_size": "7.6B",
							"quantization_level": "Q4_K_M"
						}
					},
					{
						"name": "llama3.1:8b",
						"model": "llama3.1:8b",
						"modified_at": "2024-10-15T12:00:00Z",
						"size": 4920700000,
						"digest": "sha256:abcdef",
						"details": {
							"family": "llama",
							"parameter_size": "8.0B",
							"quantization_level": "Q4_0"
						}
					}
				]
			}`))
			return
		}
		w.WriteHeader(http.StatusBadRequest)
	}))
	defer ts.Close()

	c := NewClientWithURL(ts.URL)
	models, err := c.ListModels()
	if err != nil {
		t.Fatalf("unexpected ListModels error: %v", err)
	}

	if len(models) != 2 {
		t.Fatalf("expected 2 models, got %d", len(models))
	}

	m0 := models[0]
	if m0.Name != "qwen2.5-coder:7b" {
		t.Errorf("expected name qwen2.5-coder:7b, got %s", m0.Name)
	}
	if m0.Family != "qwen2" {
		t.Errorf("expected family qwen2, got %s", m0.Family)
	}
	if m0.ParameterSize != "7.6B" {
		t.Errorf("expected parameter_size 7.6B, got %s", m0.ParameterSize)
	}
	if m0.QuantizationLevel != "Q4_K_M" {
		t.Errorf("expected quantization Q4_K_M, got %s", m0.QuantizationLevel)
	}
	if m0.SizeGB < 4.3 || m0.SizeGB > 4.4 {
		t.Errorf("unexpected SizeGB: %f", m0.SizeGB)
	}
}

func TestClient_ListModels_ErrorHandling(t *testing.T) {
	ts := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusInternalServerError)
		_, _ = w.Write([]byte("internal daemon error"))
	}))
	defer ts.Close()

	c := NewClientWithURL(ts.URL)
	_, err := c.ListModels()
	if err == nil {
		t.Errorf("expected error from ListModels on 500 status")
	}
}

func TestClient_ShowModel(t *testing.T) {
	ts := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path == "/api/show" && r.Method == http.MethodPost {
			var body map[string]string
			_ = json.NewDecoder(r.Body).Decode(&body)
			if body["name"] == "qwen2.5-coder:7b" {
				w.WriteHeader(http.StatusOK)
				_, _ = w.Write([]byte(`{
					"modelfile": "FROM qwen2.5-coder\nSYSTEM You are an expert AI coder.",
					"parameters": "stop \"<|im_end|>\"\ntemperature 0.7",
					"template": "{{ .System }}\n{{ .Prompt }}",
					"system": "You are an expert AI coder.",
					"details": {
						"family": "qwen2",
						"parameter_size": "7.6B"
					}
				}`))
				return
			}
		}
		w.WriteHeader(http.StatusNotFound)
	}))
	defer ts.Close()

	c := NewClientWithURL(ts.URL)
	detail, err := c.ShowModel("qwen2.5-coder:7b")
	if err != nil {
		t.Fatalf("unexpected ShowModel error: %v", err)
	}

	if !strings.Contains(detail.Modelfile, "FROM qwen2.5-coder") {
		t.Errorf("modelfile mismatch: %s", detail.Modelfile)
	}
	if !strings.Contains(detail.Parameters, "stop") {
		t.Errorf("parameters mismatch: %s", detail.Parameters)
	}
	if detail.System != "You are an expert AI coder." {
		t.Errorf("system mismatch: %s", detail.System)
	}
}

func TestClient_DeleteModel(t *testing.T) {
	var deletedModel string
	ts := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path == "/api/delete" && r.Method == http.MethodDelete {
			var body map[string]string
			_ = json.NewDecoder(r.Body).Decode(&body)
			deletedModel = body["name"]
			w.WriteHeader(http.StatusOK)
			return
		}
		w.WriteHeader(http.StatusBadRequest)
	}))
	defer ts.Close()

	c := NewClientWithURL(ts.URL)
	err := c.DeleteModel("old-model:latest")
	if err != nil {
		t.Fatalf("unexpected DeleteModel error: %v", err)
	}
	if deletedModel != "old-model:latest" {
		t.Errorf("expected old-model:latest deleted, got %s", deletedModel)
	}
}

func TestClient_PullModel(t *testing.T) {
	ts := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path == "/api/pull" && r.Method == http.MethodPost {
			flusher, ok := w.(http.Flusher)
			if !ok {
				t.Fatal("expected http.ResponseWriter to be http.Flusher")
			}
			w.WriteHeader(http.StatusOK)

			chunks := []string{
				`{"status":"pulling manifest"}`,
				`{"status":"downloading 6a0740323318","digest":"sha256:6a0740323318","total":1000,"completed":250}`,
				`{"status":"downloading 6a0740323318","digest":"sha256:6a0740323318","total":1000,"completed":1000}`,
				`{"status":"verifying sha256 digest"}`,
				`{"status":"success"}`,
			}

			for _, c := range chunks {
				_, _ = w.Write([]byte(c + "\n"))
				flusher.Flush()
			}
			return
		}
		w.WriteHeader(http.StatusBadRequest)
	}))
	defer ts.Close()

	c := NewClientWithURL(ts.URL)
	var captured []model.PullProgress
	err := c.PullModel("qwen2.5-coder:7b", func(p model.PullProgress) {
		captured = append(captured, p)
	})

	if err != nil {
		t.Fatalf("unexpected PullModel error: %v", err)
	}
	if len(captured) != 5 {
		t.Fatalf("expected 5 progress events, got %d", len(captured))
	}
	if captured[1].Percent != 25.0 {
		t.Errorf("expected 25.0 percent, got %f", captured[1].Percent)
	}
	if captured[2].Percent != 100.0 {
		t.Errorf("expected 100.0 percent, got %f", captured[2].Percent)
	}
	if captured[4].Status != "success" {
		t.Errorf("expected status success, got %s", captured[4].Status)
	}
}

func TestClient_BenchmarkModel(t *testing.T) {
	ts := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path == "/api/tags" {
			w.WriteHeader(http.StatusOK)
			_, _ = w.Write([]byte(`{"models":[{"name":"qwen2.5-coder:7b","size":4683075200,"details":{"family":"qwen2"}}]}`))
			return
		}
		if r.URL.Path == "/api/generate" && r.Method == http.MethodPost {
			w.WriteHeader(http.StatusOK)
			_, _ = w.Write([]byte(`{
				"model": "qwen2.5-coder:7b",
				"response": "Mass curves space-time.",
				"done": true,
				"total_duration": 1250000000,
				"prompt_eval_duration": 250000000,
				"prompt_eval_count": 5,
				"eval_count": 50,
				"eval_duration": 1000000000
			}`))
			return
		}
		w.WriteHeader(http.StatusBadRequest)
	}))
	defer ts.Close()

	c := NewClientWithURL(ts.URL)
	bench, err := c.BenchmarkModel("qwen2.5-coder:7b")
	if err != nil {
		t.Fatalf("unexpected BenchmarkModel error: %v", err)
	}

	if !bench.Success {
		t.Fatalf("expected bench.Success to be true")
	}
	if bench.ModelName != "qwen2.5-coder:7b" {
		t.Errorf("expected model name qwen2.5-coder:7b, got %s", bench.ModelName)
	}
	if bench.LatencySec != 1.25 {
		t.Errorf("expected LatencySec 1.25, got %f", bench.LatencySec)
	}
	if bench.PromptEvalSec != 0.25 {
		t.Errorf("expected PromptEvalSec 0.25, got %f", bench.PromptEvalSec)
	}
	// 50 tokens / 1.0 sec = 50.0 tok/s
	if bench.TokensPerSec != 50.0 {
		t.Errorf("expected TokensPerSec 50.0, got %f", bench.TokensPerSec)
	}
}

func TestClient_DefaultModelConfig(t *testing.T) {
	tempDir := t.TempDir()
	confPath := filepath.Join(tempDir, "ollama_default_model.txt")

	c := NewClient(WithConfigFile(confPath))

	// Initially defaults to DefaultModelName
	if c.GetDefaultModel() != DefaultModelName {
		t.Errorf("expected default model %s, got %s", DefaultModelName, c.GetDefaultModel())
	}

	// Set model
	err := c.SetDefaultModel("deepseek-coder:6.7b")
	if err != nil {
		t.Fatalf("SetDefaultModel failed: %v", err)
	}

	// Verify reading back
	if c.GetDefaultModel() != "deepseek-coder:6.7b" {
		t.Errorf("expected deepseek-coder:6.7b, got %s", c.GetDefaultModel())
	}

	// Verify file content
	content, _ := os.ReadFile(confPath)
	if strings.TrimSpace(string(content)) != "deepseek-coder:6.7b" {
		t.Errorf("file content mismatch: %s", string(content))
	}

	// Empty model name should error
	if err := c.SetDefaultModel(""); err == nil {
		t.Errorf("expected error setting empty model name")
	}
}

func TestClient_GetServerLogs(t *testing.T) {
	tempDir := t.TempDir()
	logPath := filepath.Join(tempDir, "server.log")

	var sampleLogs []string
	for i := 1; i <= 60; i++ {
		sampleLogs = append(sampleLogs, fmt.Sprintf("[%s] Log message line %d", time.Now().Format(time.RFC3339), i))
	}
	_ = os.WriteFile(logPath, []byte(strings.Join(sampleLogs, "\n")), 0644)

	c := NewClient(WithLogFile(logPath))
	logs, err := c.GetServerLogs(20)
	if err != nil {
		t.Fatalf("GetServerLogs failed: %v", err)
	}

	if !strings.Contains(logs, "Log message line 60") {
		t.Errorf("expected log line 60 in tail, got:\n%s", logs)
	}
	if strings.Contains(logs, "Log message line 10\n") {
		t.Errorf("expected older lines to be trimmed out")
	}

	// Test missing log file
	cMissing := NewClient(WithLogFile(filepath.Join(tempDir, "non_existent.log")))
	fallbackMsg, _ := cMissing.GetServerLogs(10)
	if !strings.Contains(fallbackMsg, "not found") {
		t.Errorf("expected missing file explanation in fallback message")
	}
}

func TestClient_GetDaemonStatus(t *testing.T) {
	ts := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		switch r.URL.Path {
		case "/api/version":
			w.WriteHeader(http.StatusOK)
			_, _ = w.Write([]byte(`{"version":"0.3.12"}`))
		case "/api/ps":
			w.WriteHeader(http.StatusOK)
			_, _ = w.Write([]byte(`{"models":[{"name":"qwen2.5-coder:7b","size_vram":5153960755}]}`))
		default:
			w.WriteHeader(http.StatusOK)
		}
	}))
	defer ts.Close()

	c := NewClientWithURL(ts.URL)
	status, err := c.GetDaemonStatus()
	if err != nil {
		t.Fatalf("unexpected GetDaemonStatus error: %v", err)
	}

	if !status.IsOnline {
		t.Errorf("expected status.IsOnline to be true")
	}
	if status.Version != "0.3.12" {
		t.Errorf("expected version 0.3.12, got %s", status.Version)
	}
	if status.ActiveModel != "qwen2.5-coder:7b" {
		t.Errorf("expected active model qwen2.5-coder:7b, got %s", status.ActiveModel)
	}
	if !strings.Contains(status.VRAMAllocated, "GB allocated") {
		t.Errorf("expected VRAM allocated string, got %s", status.VRAMAllocated)
	}
}
