package config

import (
	"bufio"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strconv"
	"strings"
	"time"
)

type Config struct {
	TelegramBotToken    string
	AllowedUserIDs      map[int64]bool
	AuthPinHash         string
	AuthMaxAttempts     int
	AuthLockoutDuration time.Duration
	AuthAutoLockTimeout time.Duration
	DefaultWorkspace    string
	DefaultModel        string
	DefaultEffort       string
	DefaultMode         string
	TaskTimeout         time.Duration
	AgyPath             string
	ServerPort          int
	ConfigLoadedFrom    string
}

// LoadConfig reads configuration from multiple potential locations in priority:
// 1. System environment variables
// 2. ~/.config/antigravity/bot.env
// 3. Local .env in working directory
// 4. /home/truongnhon/projects/BOT_TELEGRAM_SERVER-main/.env (legacy migration fallback)
func LoadConfig() *Config {
	cfg := &Config{
		AllowedUserIDs:      make(map[int64]bool),
		AuthMaxAttempts:     5,
		AuthLockoutDuration: 5 * time.Minute,
		AuthAutoLockTimeout: 30 * time.Minute,
		DefaultModel:        "Gemini 3.7 Flash",
		DefaultEffort:       "high",
		DefaultMode:         "accept-edits",
		TaskTimeout:         10 * time.Minute,
		ServerPort:          8090,
	}

	home, _ := os.UserHomeDir()
	candidateFiles := []string{
		filepath.Join(home, ".config", "antigravity", "bot.env"),
		".env",
		filepath.Join(home, "projects", "BOT_TELEGRAM_SERVER-main", ".env"),
	}

	for _, p := range candidateFiles {
		if fi, err := os.Stat(p); err == nil && !fi.IsDir() {
			loadEnvFile(p)
			if cfg.ConfigLoadedFrom == "" {
				cfg.ConfigLoadedFrom = p
			}
		}
	}

	cfg.TelegramBotToken = strings.TrimSpace(os.Getenv("TELEGRAM_BOT_TOKEN"))

	allowedRaw := strings.TrimSpace(os.Getenv("ALLOWED_USER_IDS"))
	if allowedRaw != "" {
		for _, part := range strings.FieldsFunc(allowedRaw, func(r rune) bool {
			return r == ',' || r == ';' || r == ' '
		}) {
			if id, err := strconv.ParseInt(strings.TrimSpace(part), 10, 64); err == nil && id > 0 {
				cfg.AllowedUserIDs[id] = true
			}
		}
	}

	cfg.AuthPinHash = strings.TrimSpace(os.Getenv("AUTH_PIN_HASH"))

	if maxAtt := os.Getenv("AUTH_MAX_ATTEMPTS"); maxAtt != "" {
		if v, err := strconv.Atoi(maxAtt); err == nil && v > 0 {
			cfg.AuthMaxAttempts = v
		}
	}

	if lockSec := os.Getenv("AUTH_LOCKOUT_SECONDS"); lockSec != "" {
		if v, err := strconv.Atoi(lockSec); err == nil && v > 0 {
			cfg.AuthLockoutDuration = time.Duration(v) * time.Second
		}
	}

	if autoMin := os.Getenv("AUTH_AUTO_LOCK_MINUTES"); autoMin != "" {
		if v, err := strconv.Atoi(autoMin); err == nil && v > 0 {
			cfg.AuthAutoLockTimeout = time.Duration(v) * time.Minute
		}
	}

	cfg.DefaultWorkspace = strings.TrimSpace(os.Getenv("DEFAULT_WORKSPACE"))
	if cfg.DefaultWorkspace == "" {
		// Fallback to finance-dashboard if available, otherwise cwd
		fdPath := filepath.Join(home, "projects", "finance-dashboard")
		if fi, err := os.Stat(fdPath); err == nil && fi.IsDir() {
			cfg.DefaultWorkspace = fdPath
		} else {
			cfg.DefaultWorkspace, _ = os.Getwd()
		}
	}

	if m := os.Getenv("DEFAULT_MODEL"); m != "" {
		cfg.DefaultModel = strings.TrimSpace(m)
	}
	if e := os.Getenv("DEFAULT_EFFORT"); e != "" {
		cfg.DefaultEffort = strings.TrimSpace(e)
	}
	if md := os.Getenv("DEFAULT_MODE"); md != "" {
		cfg.DefaultMode = strings.TrimSpace(md)
	}

	if to := os.Getenv("TASK_TIMEOUT"); to != "" {
		if v, err := strconv.Atoi(to); err == nil && v > 0 {
			cfg.TaskTimeout = time.Duration(v) * time.Second
		}
	}

	// Locate agy executable
	cfg.AgyPath = findAgyExecutable()

	return cfg
}

func findAgyExecutable() string {
	if envAgy := os.Getenv("AGY_PATH"); envAgy != "" {
		if fi, err := os.Stat(envAgy); err == nil && !fi.IsDir() {
			return envAgy
		}
	}

	if p, err := exec.LookPath("agy"); err == nil {
		return p
	}
	if p, err := exec.LookPath("agy.exe"); err == nil {
		return p
	}

	home, _ := os.UserHomeDir()
	localAgy := filepath.Join(home, ".local", "bin", "agy")
	if fi, err := os.Stat(localAgy); err == nil && !fi.IsDir() {
		return localAgy
	}

	return "agy"
}

func loadEnvFile(path string) {
	f, err := os.Open(path)
	if err != nil {
		return
	}
	defer f.Close()

	scanner := bufio.NewScanner(f)
	for scanner.Scan() {
		line := strings.TrimSpace(scanner.Text())
		if line == "" || strings.HasPrefix(line, "#") {
			continue
		}
		parts := strings.SplitN(line, "=", 2)
		if len(parts) == 2 {
			k := strings.TrimSpace(parts[0])
			v := strings.TrimSpace(parts[1])
			v = strings.Trim(v, `"'`)
			if os.Getenv(k) == "" {
				_ = os.Setenv(k, v)
			}
		}
	}
}

// GetConfigFilePath returns the primary user config file in ~/.config/antigravity/bot.env
func GetConfigFilePath() string {
	home, _ := os.UserHomeDir()
	return filepath.Join(home, ".config", "antigravity", "bot.env")
}

// SaveConfigKey writes or replaces a key=value setting in ~/.config/antigravity/bot.env
func SaveConfigKey(key, value string) error {
	path := GetConfigFilePath()
	_ = os.MkdirAll(filepath.Dir(path), 0755)

	var lines []string
	keyUpper := strings.ToUpper(strings.TrimSpace(key))
	found := false

	if data, err := os.ReadFile(path); err == nil {
		scanner := bufio.NewScanner(strings.NewReader(string(data)))
		for scanner.Scan() {
			line := scanner.Text()
			trimmed := strings.TrimSpace(line)
			if strings.HasPrefix(trimmed, keyUpper+"=") {
				lines = append(lines, fmt.Sprintf("%s=%q", keyUpper, value))
				found = true
			} else {
				lines = append(lines, line)
			}
		}
	}

	if !found {
		lines = append(lines, fmt.Sprintf("%s=%q", keyUpper, value))
	}

	content := strings.Join(lines, "\n") + "\n"
	return os.WriteFile(path, []byte(content), 0600)
}

// AddWhitelistUser adds a user ID to the allowed list and persists to bot.env
func (c *Config) AddWhitelistUser(id int64) error {
	c.AllowedUserIDs[id] = true
	var ids []string
	for uid := range c.AllowedUserIDs {
		ids = append(ids, strconv.FormatInt(uid, 10))
	}
	return SaveConfigKey("ALLOWED_USER_IDS", strings.Join(ids, ","))
}

// RemoveWhitelistUser removes a user ID from the allowed list and persists to bot.env
func (c *Config) RemoveWhitelistUser(id int64) error {
	delete(c.AllowedUserIDs, id)
	var ids []string
	for uid := range c.AllowedUserIDs {
		ids = append(ids, strconv.FormatInt(uid, 10))
	}
	return SaveConfigKey("ALLOWED_USER_IDS", strings.Join(ids, ","))
}

