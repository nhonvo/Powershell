package view

import (
	"bytes"
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"
	"time"

	"agyollama/internal/model"
	"agyollama/internal/service/ollamaops"
)

func TestApp_PrintStatus(t *testing.T) {
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

	client := ollamaops.NewClientWithURL(ts.URL)
	app := NewAppWithClient(client)

	var buf bytes.Buffer
	app.PrintStatus(&buf)
	out := buf.String()

	if !strings.Contains(out, "ONLINE") {
		t.Errorf("expected ONLINE in status output, got: %s", out)
	}
	if !strings.Contains(out, "qwen2.5-coder:7b") {
		t.Errorf("expected active model in status output, got: %s", out)
	}
}

func TestApp_PrintModels(t *testing.T) {
	ts := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path == "/api/tags" {
			w.WriteHeader(http.StatusOK)
			_, _ = w.Write([]byte(`{
				"models": [
					{
						"name": "qwen2.5-coder:7b",
						"size": 4683075200,
						"details": {
							"family": "qwen2",
							"parameter_size": "7.6B",
							"quantization_level": "Q4_K_M"
						}
					}
				]
			}`))
			return
		}
		w.WriteHeader(http.StatusOK)
	}))
	defer ts.Close()

	client := ollamaops.NewClientWithURL(ts.URL)
	app := NewAppWithClient(client)

	var buf bytes.Buffer
	app.PrintModels(&buf)
	out := buf.String()

	if !strings.Contains(out, "qwen2.5-coder:7b") {
		t.Errorf("expected model name in PrintModels, got: %s", out)
	}
	if !strings.Contains(out, "qwen2") {
		t.Errorf("expected family in PrintModels, got: %s", out)
	}
}

func TestApp_Render(t *testing.T) {
	app := NewApp()
	app.daemonStatus = &model.DaemonStatus{
		IsOnline:         true,
		Host:             "127.0.0.1",
		Port:             11434,
		Version:          "0.3.12",
		ActiveModel:      "qwen2.5-coder:7b",
		VRAMAllocated:    "4.8 GB allocated",
		TotalHostRAM:     16 * 1024 * 1024 * 1024,
		AvailableHostRAM: 8 * 1024 * 1024 * 1024,
	}

	app.models = []model.ModelInfo{
		{
			Name:              "qwen2.5-coder:7b",
			SizeBytes:         4683075200,
			SizeGB:            4.36,
			ModifiedAt:        time.Now(),
			Family:            "qwen2",
			ParameterSize:     "7.6B",
			QuantizationLevel: "Q4_K_M",
		},
		{
			Name:              "llama3.2:3b",
			SizeBytes:         2000000000,
			SizeGB:            1.86,
			ModifiedAt:        time.Now(),
			Family:            "llama",
			ParameterSize:     "3.2B",
			QuantizationLevel: "Q4_0",
		},
	}

	app.benchmarkList = []model.BenchmarkResult{
		{
			ModelName:     "qwen2.5-coder:7b",
			SizeGB:        4.36,
			LatencySec:    1.25,
			TokensPerSec:  42.5,
			PromptEvalSec: 0.15,
			Success:       true,
		},
	}

	app.logsContent = "2026-09-08 11:00:00 [info] Ollama server listening on 127.0.0.1:11434"

	// Tab 0: Status
	app.ActiveTab = 0
	app.Render()

	// Tab 1: Models
	app.ActiveTab = 1
	app.SelectedIndex = 0
	app.Render()

	// Tab 2: Benchmark
	app.ActiveTab = 2
	app.Render()

	// Tab 3: Logs
	app.ActiveTab = 3
	app.Render()
}

func TestApp_ModalsAndActions(t *testing.T) {
	app := NewApp()

	// Detail Modal
	app.viewDetailModal = &model.ModelDetail{
		Modelfile:  "FROM qwen2.5-coder\nSYSTEM Hello",
		Template:   "{{ .System }}\n{{ .Prompt }}",
		Parameters: "temperature 0.7",
		System:     "You are a helpful assistant.",
	}
	app.viewDetailName = "qwen2.5-coder:7b"
	app.Render()
	app.viewDetailModal = nil

	// Confirm Delete Modal
	app.confirmDelete = "old-model:latest"
	app.Render()
	app.confirmDelete = ""

	// Active Action & Spinner
	app.setActiveAction("pulling", "Pulling qwen2.5-coder:7b...")
	app.pullProgress = &model.PullProgress{
		Status:         "downloading",
		TotalBytes:     1000,
		CompletedBytes: 500,
		Percent:        50.0,
	}
	app.spinnerIdx = 2
	app.Render()
	app.setActiveAction("", "")
}

func TestApp_DaemonOfflineBanner(t *testing.T) {
	app := NewApp()
	app.daemonStatus = &model.DaemonStatus{
		IsOnline: false,
		Host:     "127.0.0.1",
		Port:     11434,
	}
	app.ActiveTab = 0
	app.Render()

	var buf bytes.Buffer
	app.PrintStatus(&buf)
	if !strings.Contains(buf.String(), "OFFLINE") {
		t.Errorf("expected OFFLINE in status output, got: %s", buf.String())
	}
}
