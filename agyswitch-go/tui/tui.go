package tui

import (
	"fmt"
	"os"
	"strings"

	"agyswitch/launcher"
	"agyswitch/store"
	"agyswitch/vault"

	"golang.org/x/term"
)

// Options controls non-interactive or forced behavior for testing.
type Options struct {
	ForceNonInteractive bool
}

// Run launches the interactive TUI or falls back to status printing if non-TTY.
func Run(s *store.Store, v *vault.Vault, l *launcher.Launcher, opts ...Options) error {
	nonInteractive := false
	for _, o := range opts {
		if o.ForceNonInteractive {
			nonInteractive = true
		}
	}

	fd := int(os.Stdin.Fd())
	if nonInteractive || !term.IsTerminal(fd) {
		PrintStatus(s)
		return nil
	}

	oldState, err := term.MakeRaw(fd)
	if err != nil {
		PrintStatus(s)
		return nil
	}
	defer func() {
		_ = term.Restore(fd, oldState)
	}()

	accs := s.ListAccounts()
	if len(accs) == 0 {
		fmt.Print("\r\nNo registered accounts found.\r\n")
		return nil
	}

	selectedIndex := 0
	activeAccName := s.GetActiveAccount()
	for i, a := range accs {
		if strings.EqualFold(a.AccountName, activeAccName) {
			selectedIndex = i
			break
		}
	}

	msg := ""

	for {
		renderUI(accs, s.GetActiveAccount(), selectedIndex, msg)
		msg = ""

		var buf [3]byte
		n, err := os.Stdin.Read(buf[:])
		if err != nil || n == 0 {
			break
		}

		b := buf[0]

		// Check for Arrow keys (\x1b[A, \x1b[B)
		if b == 0x1b {
			if n >= 3 && buf[1] == '[' {
				switch buf[2] {
				case 'A': // Up
					if selectedIndex > 0 {
						selectedIndex--
					}
					continue
				case 'B': // Down
					if selectedIndex < len(accs)-1 {
						selectedIndex++
					}
					continue
				}
			}
			// ESC pressed
			break
		}

		switch b {
		case 'k', 'K':
			if selectedIndex > 0 {
				selectedIndex--
			}
		case 'j', 'J':
			if selectedIndex < len(accs)-1 {
				selectedIndex++
			}
		case '1', '2', '3', '4', '5', '6', '7', '8', '9':
			idx := int(b - '1')
			if idx >= 0 && idx < len(accs) {
				selectedIndex = idx
			}
		case '\r', '\n': // Enter -> Switch context
			target := accs[selectedIndex].AccountName
			if err := s.SetActiveAccount(target); err != nil {
				msg = fmt.Sprintf("\033[31mError: %v\033[0m", err)
			} else {
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[36m[agyswitch]\033[0m Context switched to '\033[32m%s\033[0m'.\r\n", target)
				return nil
			}
		case 'l', 'L': // Launch agy CLI with target account
			target := accs[selectedIndex].AccountName
			if err := s.SetActiveAccount(target); err != nil {
				msg = fmt.Sprintf("\033[31mError: %v\033[0m", err)
			} else {
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[36m[agyswitch]\033[0m Launching agy CLI for '\033[32m%s\033[0m'...\r\n", target)
				return l.LaunchAccount(target, nil)
			}
		case 'a', 'A': // Auto Quota Select & Launch
			bestAcc := s.SelectBestQuotaAccount()
			_ = term.Restore(fd, oldState)
			fmt.Printf("\r\n\033[36m[agyswitch]\033[0m Quota selector auto-selected account '\033[32m%s\033[0m'...\r\n", bestAcc)
			return l.LaunchAccount(bestAcc, nil)
		case 'x', 'X': // Reset Account Credentials
			target := accs[selectedIndex].AccountName
			if err := s.ResetAccount(target); err != nil {
				msg = fmt.Sprintf("\033[31mError resetting account: %v\033[0m", err)
			} else {
				accs = s.ListAccounts()
				msg = fmt.Sprintf("\033[33mCredentials and session token for '%s' reset cleanly.\033[0m", target)
			}
		case 'r', 'R': // Refresh
			accs = s.ListAccounts()
			msg = "\033[32mRefreshed token & quota status.\033[0m"
		case 'q', 'Q', 0x03: // Quit or Ctrl+C
			_ = term.Restore(fd, oldState)
			fmt.Print("\r\n")
			return nil
		}
	}

	return nil
}

func formatStatusBadge(a store.AccountInfo) string {
	if !a.IsLoggedIn {
		return "\033[31m[✘ Logged Out]\033[0m"
	}
	switch a.QuotaStatus {
	case "✔ Quota OK":
		return fmt.Sprintf("\033[32m[✔ Quota OK · Key: %s]\033[0m", a.TokenSig)
	case "⚡ Auto-Refresh":
		return fmt.Sprintf("\033[33m[⚡ Ready · Key: %s]\033[0m", a.TokenSig)
	case "✘ Rate Limit":
		return fmt.Sprintf("\033[35m[✘ Rate Limit · Key: %s]\033[0m", a.TokenSig)
	default:
		return fmt.Sprintf("\033[32m[✔ Quota OK · Key: %s]\033[0m", a.TokenSig)
	}
}

func renderUI(accs []store.AccountInfo, activeAcc string, selectedIndex int, statusMsg string) {
	fmt.Print("\033[H\033[2J") // Clear screen
	fmt.Print("\r\n🛸 \033[1;36mAGYSWITCH - Dedicated Antigravity Multi-Account Vault (Go TUI v1.3.0)\033[0m\r\n")
	fmt.Print("──────────────────────────────────────────────────────────────────────────────────\r\n")
	fmt.Printf(" Active Context: \033[1;32m%s\033[0m\r\n\r\n", activeAcc)

	for i, a := range accs {
		cursor := "  "
		highlightStart := ""
		highlightEnd := ""
		if i == selectedIndex {
			cursor = "\033[1;36m> \033[0m"
			highlightStart = "\033[1;37;44m"
			highlightEnd = "\033[0m"
		}

		activeMarker := "  "
		if a.IsActive {
			activeMarker = "\033[1;32m●\033[0m "
		}

		badge := formatStatusBadge(a)

		fmt.Printf("%s%s%s%d. \033[1m%-22s\033[0m (%-26s) %s%s\r\n",
			cursor, highlightStart, activeMarker, i+1, a.AccountName, a.Email, badge, highlightEnd)
	}

	fmt.Print("──────────────────────────────────────────────────────────────────────────────────\r\n")
	if statusMsg != "" {
		fmt.Printf(" %s\r\n", statusMsg)
	}
	fmt.Print(" \033[1m[↑/↓ j/k]\033[0m Nav · \033[1m[1-5]\033[0m Quick Jump · \033[1;32m[Enter]\033[0m Switch · \033[1;36m[L]\033[0m Launch · \033[1;35m[A]\033[0m Auto Quota · \033[1;33m[X]\033[0m Reset · \033[1;33m[R]\033[0m Refresh · \033[1;31m[Q/Esc]\033[0m Exit\r\n")
}

// PrintStatus prints the non-interactive status table.
func PrintStatus(s *store.Store) {
	fmt.Println("\n🛸 \033[1;36mAGYSWITCH - Dedicated Antigravity Multi-Account Vault (Go Engine v1.3.0)\033[0m")
	fmt.Println("──────────────────────────────────────────────────────────────────────────────────")
	accs := s.ListAccounts()
	active := s.GetActiveAccount()

	fmt.Printf(" Active Account: \033[1;32m%s\033[0m\n\n", active)

	for i, a := range accs {
		activeMarker := "  "
		if a.IsActive {
			activeMarker = "● "
		}

		badge := formatStatusBadge(a)

		fmt.Printf(" %s%d. \033[1m%-22s\033[0m (%-26s) %s\n",
			activeMarker, i+1, a.AccountName, a.Email, badge)
	}
	fmt.Printf("──────────────────────────────────────────────────────────────────────────────────\n\n")
}

