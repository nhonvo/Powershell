package model

import "time"

// DaemonStatus represents the local Ollama daemon's health and system metrics.
type DaemonStatus struct {
	IsOnline         bool   `json:"is_online"`
	Host             string `json:"host"`
	Port             int    `json:"port"`
	Version          string `json:"version"`
	ActiveModel      string `json:"active_model"`
	VRAMAllocated    string `json:"vram_allocated"`
	TotalHostRAM     uint64 `json:"total_host_ram"`
	AvailableHostRAM uint64 `json:"available_host_ram"`
}

// ModelInfo represents a locally installed Ollama model.
type ModelInfo struct {
	Name              string    `json:"name"`
	SizeBytes         int64     `json:"size_bytes"`
	SizeGB            float64   `json:"size_gb"`
	ModifiedAt        time.Time `json:"modified_at"`
	Family            string    `json:"family"`
	ParameterSize     string    `json:"parameter_size"`
	QuantizationLevel string    `json:"quantization_level"`
}

// ModelDetail provides full inspection information for a model.
type ModelDetail struct {
	Modelfile  string                 `json:"modelfile"`
	Parameters string                 `json:"parameters"`
	Template   string                 `json:"template"`
	System     string                 `json:"system"`
	Details    map[string]interface{} `json:"details"`
}

// BenchmarkResult stores the performance measurement of a model prompt evaluation.
type BenchmarkResult struct {
	ModelName     string  `json:"model_name"`
	SizeGB        float64 `json:"size_gb"`
	LatencySec    float64 `json:"latency_sec"`
	TokensPerSec  float64 `json:"tokens_per_sec"`
	PromptEvalSec float64 `json:"prompt_eval_sec"`
	Success       bool    `json:"success"`
	ErrorMsg      string  `json:"error_msg,omitempty"`
}

// PullProgress captures streaming download and layer verification progress.
type PullProgress struct {
	Status         string  `json:"status"`
	Digest         string  `json:"digest,omitempty"`
	TotalBytes     int64   `json:"total_bytes,omitempty"`
	CompletedBytes int64   `json:"completed_bytes,omitempty"`
	Percent        float64 `json:"percent,omitempty"`
}
