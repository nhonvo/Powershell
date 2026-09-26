package seeder

import (
	"os"
	"path/filepath"
	"strings"

	"agyswitch/internal/service/store"
)

type Seeder struct {
	UserHome string
	Store    *store.Store
}

func NewSeeder(userHome string, s *store.Store) *Seeder {
	if userHome == "" {
		if h, err := os.UserHomeDir(); err == nil {
			userHome = h
		}
	}
	if s == nil {
		s = store.NewStore(userHome, nil)
	}
	return &Seeder{UserHome: userHome, Store: s}
}

// EnsureSeedTemplate provisions ~/.gemini_template directory structure if missing across primary and counterpart homes.
func (s *Seeder) EnsureSeedTemplate() error {
	templateDir := filepath.Join(s.UserHome, ".gemini_template")
	if err := os.MkdirAll(filepath.Join(templateDir, "config", "rules"), 0755); err != nil {
		return err
	}
	_ = os.MkdirAll(filepath.Join(templateDir, "skills"), 0755)

	geminiRule := filepath.Join(templateDir, "config", "rules", "GEMINI.md")
	if _, err := os.Stat(geminiRule); os.IsNotExist(err) {
		content := "# Antigravity Core Behavioral Rules\n- Be concise, modular, and precise.\n- Maintain architectural integrity and avoid superficial patches.\n"
		_ = os.WriteFile(geminiRule, []byte(content), 0644)
	}

	if cpHome := store.GetCounterpartHome(s.UserHome); cpHome != "" {
		cpTemplateDir := filepath.Join(cpHome, ".gemini_template")
		_ = os.MkdirAll(filepath.Join(cpTemplateDir, "config", "rules"), 0755)
		_ = os.MkdirAll(filepath.Join(cpTemplateDir, "skills"), 0755)
		_ = store.MirrorDirectory(templateDir, cpTemplateDir)
	}

	return nil
}

// SeedAccount copies ~/.gemini_template canonical defaults into target account directory on primary and counterpart homes.
func (s *Seeder) SeedAccount(accountName string) error {
	_ = s.EnsureSeedTemplate()
	templateDir := filepath.Join(s.UserHome, ".gemini_template")
	accDir := s.Store.GetAccountDirectory(accountName)

	err := store.MirrorDirectory(templateDir, accDir)

	if cpHome := store.GetCounterpartHome(s.UserHome); cpHome != "" {
		cpAccDir := s.Store.GetAccountDirectoryForHome(cpHome, accountName)
		_ = store.MirrorDirectory(templateDir, cpAccDir)
	}

	return err
}

// ResetAccountEx executes multi-tier account resets (--auth, --soft, --hard).
func (s *Seeder) ResetAccountEx(accountName string, mode string) error {
	accDir := s.Store.GetAccountDirectory(accountName)
	cleanMode := strings.TrimPrefix(strings.ToLower(strings.TrimSpace(mode)), "--")

	switch cleanMode {
	case "auth":
		return s.Store.LogoutAccount(accountName)
	case "soft":
		_ = os.RemoveAll(filepath.Join(accDir, "antigravity-cli", "log"))
		_ = os.RemoveAll(filepath.Join(accDir, "antigravity-cli", "cache"))
		_ = os.Remove(filepath.Join(accDir, "agytui.dev.db"))
		_ = os.Remove(filepath.Join(accDir, "agytui.dev.db-wal"))
		_ = os.Remove(filepath.Join(accDir, "agytui.dev.db-shm"))
	case "hard":
		_ = s.Store.DeleteAccount(accountName)
	default:
		return s.Store.ResetAccount(accountName)
	}

	return nil
}
