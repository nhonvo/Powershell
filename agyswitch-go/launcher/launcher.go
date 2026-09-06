package launcher

import (
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strings"

	"agyswitch/internal/service/store"
	"agyswitch/internal/service/vault"
)

type Launcher struct {
	Store *store.Store
	Vault *vault.Vault
}

func NewLauncher(s *store.Store, v *vault.Vault) *Launcher {
	return &Launcher{
		Store: s,
		Vault: v,
	}
}

// FindAgyBin locates agy binary in system path or standard locations.
func (l *Launcher) FindAgyBin() (string, error) {
	if bin, err := exec.LookPath("agy"); err == nil && !strings.HasSuffix(bin, "agyswitch") {
		return bin, nil
	}
	if bin, err := exec.LookPath("antigravity"); err == nil {
		return bin, nil
	}

	userHome := l.Store.UserHome
	candidates := []string{
		filepath.Join(userHome, ".local", "bin", "agy"),
		filepath.Join(userHome, ".gemini", "antigravity-cli", "agy"),
		"/usr/local/bin/agy",
	}

	for _, c := range candidates {
		if fi, err := os.Stat(c); err == nil && !fi.IsDir() {
			return c, nil
		}
	}

	return "", fmt.Errorf("could not find 'agy' executable on PATH or standard installation paths")
}

// CleanArgs strips legacy unsupported subcommands like 'auth' or 'login'.
func (l *Launcher) CleanArgs(args []string) []string {
	var clean []string
	for _, arg := range args {
		if arg != "auth" && arg != "login" {
			clean = append(clean, arg)
		}
	}
	return clean
}

// LaunchAccount Context sets GEMINI_HOME, clears IDE environment sync, runs agy, and executes post-run session token capture.
func (l *Launcher) LaunchAccount(accountName string, passArgs []string) error {
	accDir := l.Store.GetAccountDirectory(accountName)
	if err := os.MkdirAll(accDir, 0755); err != nil {
		return err
	}

	_ = l.Store.SetActiveAccount(accountName)

	token := l.Vault.ReadTokenFromDir(accDir)
	if token != "" {
		_ = l.Vault.SaveTokenToContext(accDir, token)
		fmt.Fprintf(os.Stderr, "\033[36m[agyswitch]\033[0m Context for '\033[32m%s\033[0m': \033[32m%s\033[0m (✔ Logged In)\n", accountName, accDir)
	} else {
		l.Vault.PurgeGlobalKeyring()
		fmt.Fprintf(os.Stderr, "\033[36m[agyswitch]\033[0m Context for '\033[33m%s\033[0m': \033[33m%s\033[0m (✘ Logged Out - Login Required)\n", accountName, accDir)
	}

	agyBin, err := l.FindAgyBin()
	if err != nil {
		return err
	}

	cleanArgs := l.CleanArgs(passArgs)

	cmd := exec.Command(agyBin, cleanArgs...)

	// Environmental Isolation: Strip IDE sync variables to keep CLI strictly standalone
	var finalEnv []string
	for _, envStr := range os.Environ() {
		parts := strings.SplitN(envStr, "=", 2)
		if len(parts) == 2 {
			k := parts[0]
			if k != "GEMINI_CLI_IDE_AUTH_TOKEN" && k != "GEMINI_CLI_IDE_SERVER_PORT" && k != "ANTIGRAVITY_IDE_SERVER_PORT" {
				finalEnv = append(finalEnv, envStr)
			}
		}
	}
	finalEnv = append(finalEnv, fmt.Sprintf("GEMINI_HOME=%s", accDir))
	cmd.Env = finalEnv

	cmd.Stdin = os.Stdin
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr

	runErr := cmd.Run()

	// Post-run session token capture hook
	postToken := l.Vault.ReadTokenFromDir(accDir)
	if postToken == "" {
		globalKeyFile := filepath.Join(l.Store.UserHome, ".gemini", ".keyring", vault.KeyHash)
		if data, err := os.ReadFile(globalKeyFile); err == nil {
			lines := strings.Split(string(data), "\n")
			if len(lines) >= 2 && strings.TrimSpace(lines[1]) != "" {
				postToken = strings.TrimSpace(lines[1])
			}
		}
	}

	if postToken != "" {
		_ = l.Vault.SaveTokenToContext(accDir, postToken)
		fmt.Fprintf(os.Stderr, "\033[36m[agyswitch]\033[0m Persisted session token for account '\033[32m%s\033[0m'.\n", accountName)
	}

	return runErr
}
