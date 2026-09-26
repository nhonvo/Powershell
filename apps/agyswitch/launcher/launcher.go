package launcher

import (
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"runtime"
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

// CleanArgs strips unsupported subcommands like 'auth' or 'login' and ensures --dangerously-skip-permissions is passed for non-login launches.
func (l *Launcher) CleanArgs(args []string) []string {
	var clean []string
	hasDanger := false
	isLogin := false

	for _, arg := range args {
		lower := strings.ToLower(arg)
		if lower == "login" || lower == "auth" {
			isLogin = true
			// Strip 'login' and 'auth' because agy CLI rejects them as positional prompt arguments
		} else if lower == "--dangerously-skip-permissions" {
			hasDanger = true
			clean = append(clean, arg)
		} else {
			clean = append(clean, arg)
		}
	}

	if !isLogin && !hasDanger {
		clean = append([]string{"--dangerously-skip-permissions"}, clean...)
	}

	return clean
}

// LaunchAccount Context sets GEMINI_HOME, clears IDE environment sync, runs agy, and executes post-run session token capture.
func (l *Launcher) LaunchAccount(accountName string, passArgs []string) error {
	return l.LaunchAccountInDir(accountName, "", passArgs)
}

// LaunchAccountInDir sets GEMINI_HOME, working directory, and launches agy.
func (l *Launcher) LaunchAccountInDir(accountName string, workingDir string, passArgs []string) error {
	accountName = l.Store.ResolveAccount(accountName)
	accDir := l.Store.GetAccountDirectory(accountName)
	if err := os.MkdirAll(accDir, 0755); err != nil {
		return err
	}

	_ = l.Store.SetActiveAccount(accountName)

	isLogin := false
	for _, a := range passArgs {
		if strings.EqualFold(a, "login") || strings.EqualFold(a, "auth") {
			isLogin = true
			break
		}
	}

	if isLogin {
		_ = l.Store.LogoutAccount(accountName)
		if runtime.GOOS == "windows" {
			vault.DeleteWindowsCredential("gemini:antigravity")
		}
		fmt.Fprintf(os.Stderr, "\033[36m[agyswitch]\033[0m Starting interactive Google OAuth sign-in for account '\033[32m%s\033[0m'...\n", accountName)
		fmt.Fprintf(os.Stderr, "\033[36m[agyswitch]\033[0m Context: \033[33m%s\033[0m (Follow prompts in terminal/browser)\n", accDir)
	} else {
		token := l.Vault.EnsureValidAccessToken(accDir)
		if token != "" && l.Store.MatchesAccountEmail(accDir, accountName) {
			if runtime.GOOS == "windows" {
				if winTok := l.Vault.ReadTokenFromDir(accDir); winTok != "" {
					vault.WriteWindowsCredential("gemini:antigravity", winTok)
				}
			}
			fmt.Fprintf(os.Stderr, "\033[36m[agyswitch]\033[0m Context for '\033[32m%s\033[0m': \033[32m%s\033[0m (✔ Logged In)\n", accountName, accDir)
		} else {
			if token != "" && !l.Store.MatchesAccountEmail(accDir, accountName) {
				l.Store.ClearCredentials(accDir)
			}
			l.Vault.PurgeGlobalKeyring()
			if runtime.GOOS == "windows" {
				vault.DeleteWindowsCredential("gemini:antigravity")
			}
			fmt.Fprintf(os.Stderr, "\033[36m[agyswitch]\033[0m Context for '\033[33m%s\033[0m': \033[33m%s\033[0m (✘ Logged Out - Login Required)\n", accountName, accDir)
		}
	}

	agyBin, err := l.FindAgyBin()
	if err != nil {
		return err
	}

	cleanArgs := l.CleanArgs(passArgs)

	cmd := exec.Command(agyBin, cleanArgs...)

	if workingDir != "" && workingDir != "Default Workspace" {
		if fi, err := os.Stat(workingDir); err == nil && fi.IsDir() {
			cmd.Dir = workingDir
			fmt.Fprintf(os.Stderr, "\033[36m[agyswitch]\033[0m Working Directory: \033[35m%s\033[0m\n", workingDir)
		}
	}

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
	geminiHomePath := accDir
	if strings.HasSuffix(strings.ToLower(agyBin), ".exe") && strings.HasPrefix(accDir, "/mnt/") {
		geminiHomePath = store.ToWindowsPath(accDir)
	} else if runtime.GOOS == "windows" && (strings.HasPrefix(geminiHomePath, "\\\\wsl") || strings.HasPrefix(geminiHomePath, "/")) {
		geminiHomePath = store.ToLinuxPath(geminiHomePath)
	}
	finalEnv = append(finalEnv, fmt.Sprintf("GEMINI_HOME=%s", geminiHomePath))
	cmd.Env = finalEnv

	cmd.Stdin = os.Stdin
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr

	runErr := cmd.Run()

	// Post-run hook: sync newly updated credentials back to account context & primaryDir
	primaryDir := filepath.Join(l.Store.UserHome, ".gemini")
	if runtime.GOOS == "windows" {
		if winTok := vault.ReadWindowsCredential("gemini:antigravity"); winTok != "" {
			_ = l.Vault.SaveTokenToContext(accDir, winTok)
			if strings.EqualFold(accountName, l.Store.GetActiveAccount()) {
				_ = l.Vault.SaveTokenToContext(primaryDir, winTok)
			}
		}
	}

	if strings.EqualFold(accountName, l.Store.GetActiveAccount()) {
		aTok := l.Vault.ReadTokenFromDir(accDir)
		if aTok != "" && l.Store.MatchesAccountEmail(accDir, accountName) {
			l.Store.SyncCredentials(accDir, primaryDir)
			_ = l.Vault.SyncKeyringCredentials(accDir)
			_ = l.Vault.SyncKeyringCredentials(primaryDir)
			fmt.Fprintf(os.Stderr, "\033[36m[agyswitch]\033[0m Persisted session token for account '\033[32m%s\033[0m'.\n", accountName)
		}
	}

	return runErr
}
