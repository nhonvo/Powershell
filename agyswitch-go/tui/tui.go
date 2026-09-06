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
		renderUI(s, accs, s.GetActiveAccount(), selectedIndex, msg)
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
		case 'v', 'V': // View detailed Quota
			target := accs[selectedIndex].AccountName
			summary, err := s.GetAccountQuota(target)
			if err != nil {
				msg = fmt.Sprintf("\033[31mError fetching quota for '%s': %v\033[0m", target, err)
			} else {
				email := fmt.Sprintf("%s@gmail.com", target)
				msg = store.RenderQuotaSummary(email, summary)
			}
		case 'n', 'N': // Add / Register New Account
			_ = term.Restore(fd, oldState)
			fmt.Print("\r\n\033[36m[agyswitch]\033[0m Enter new account name (e.g. myaccount): ")
			var newAcc string
			fmt.Scanln(&newAcc)
			newAcc = strings.TrimSpace(newAcc)
			if newAcc != "" {
				if err := s.AddAccount(newAcc); err == nil {
					fmt.Printf("\033[36m[agyswitch]\033[0m Created account context '\033[32m%s\033[0m'. Launching 'agy login'...\r\n", newAcc)
					return l.LaunchAccount(newAcc, []string{"login"})
				}
			}
		case 'm', 'M': // Rename / Move Account
			target := accs[selectedIndex].AccountName
			_ = term.Restore(fd, oldState)
			fmt.Printf("\r\n\033[36m[agyswitch]\033[0m Enter new name for '\033[33m%s\033[0m': ", target)
			var newName string
			fmt.Scanln(&newName)
			newName = strings.TrimSpace(newName)
			if newName != "" && newName != target {
				if err := s.RenameAccount(target, newName); err != nil {
					msg = fmt.Sprintf("\033[31mError renaming account: %v\033[0m", err)
				} else {
					accs = s.ListAccounts()
					msg = fmt.Sprintf("\033[32mSuccessfully renamed '%s' -> '%s'.\033[0m", target, newName)
				}
			}
		case 'd', 'D': // Delete Account Context
			target := accs[selectedIndex].AccountName
			_ = term.Restore(fd, oldState)
			fmt.Printf("\r\n\033[31m[agyswitch]\033[0m Are you sure you want to delete '%s'? (y/N): ", target)
			var confirm string
			fmt.Scanln(&confirm)
			if strings.EqualFold(strings.TrimSpace(confirm), "y") {
				if err := s.DeleteAccount(target); err != nil {
					msg = fmt.Sprintf("\033[31mError deleting account: %v\033[0m", err)
				} else {
					accs = s.ListAccounts()
					if selectedIndex >= len(accs) && len(accs) > 0 {
						selectedIndex = len(accs) - 1
					}
					msg = fmt.Sprintf("\033[33mDeleted account '%s' cleanly.\033[0m", target)
				}
			}
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

func formatInlineQuotaBadge(a store.AccountInfo) string {
	if !a.IsLoggedIn {
		return ""
	}
	if a.GeminiQuotaPct >= 0 && a.ClaudeQuotaPct >= 0 {
		return fmt.Sprintf("\033[36mG: %.1f%%\033[0m · \033[35mC: %.1f%%\033[0m", a.GeminiQuotaPct, a.ClaudeQuotaPct)
	} else if a.GeminiQuotaPct >= 0 {
		return fmt.Sprintf("\033[36mG: %.1f%%\033[0m", a.GeminiQuotaPct)
	}
	return ""
}

func renderUI(s *store.Store, accs []store.AccountInfo, activeAcc string, selectedIndex int, statusMsg string) {
	fmt.Print("\033[H\033[2J") // Clear screen
	fmt.Print("\r\n🛸 \033[1;36mAGYSWITCH - Dedicated Antigravity Multi-Account Vault (Go TUI v1.4.0)\033[0m\r\n")
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
		inlineQuota := formatInlineQuotaBadge(a)

		if inlineQuota != "" {
			fmt.Printf("%s%s%s%d. \033[1m%-22s\033[0m (%-26s) %s  %s%s\r\n",
				cursor, highlightStart, activeMarker, i+1, a.AccountName, a.Email, badge, inlineQuota, highlightEnd)
		} else {
			fmt.Printf("%s%s%s%d. \033[1m%-22s\033[0m (%-26s) %s%s\r\n",
				cursor, highlightStart, activeMarker, i+1, a.AccountName, a.Email, badge, highlightEnd)
		}
	}

	fmt.Print("──────────────────────────────────────────────────────────────────────────────────\r\n")

	// Smart Suggestion Flow
	_, recBanner := s.GetRecommendedAccountInfo(accs)
	fmt.Printf(" %s\r\n", recBanner)

	// Live Quota Breakdown Pane for Selected Account
	if selectedIndex >= 0 && selectedIndex < len(accs) {
		sel := accs[selectedIndex]
		if sel.IsLoggedIn && sel.QuotaSummary != nil && len(sel.QuotaSummary.Groups) > 0 {
			fmt.Printf("\r\n 📊 \033[1;33mQuota Breakdown (%s):\033[0m\r\n", sel.AccountName)
			for _, g := range sel.QuotaSummary.Groups {
				for _, b := range g.Buckets {
					if b.Window == "weekly" || strings.Contains(b.BucketID, "weekly") {
						pct := b.RemainingFraction * 100.0
						filledLen := int((pct / 100.0) * 30.0)
						if filledLen > 30 {
							filledLen = 30
						}
						if filledLen < 0 {
							filledLen = 0
						}
						emptyLen := 30 - filledLen
						bar := strings.Repeat("█", filledLen) + strings.Repeat("░", emptyLen)

						colorCode := "\033[32m"
						if pct < 20.0 {
							colorCode = "\033[31m"
						} else if pct < 50.0 {
							colorCode = "\033[33m"
						}

						grpName := "Gemini"
						if strings.Contains(strings.ToLower(g.DisplayName), "claude") {
							grpName = "Claude/GPT"
						}

						fmt.Printf("    • %-11s: [%s%s\033[0m] \033[1m%.1f%%\033[0m\r\n", grpName, colorCode, bar, pct)
					}
				}
			}
		}
	}

	fmt.Print("──────────────────────────────────────────────────────────────────────────────────\r\n")
	if statusMsg != "" {
		fmt.Printf(" %s\r\n", statusMsg)
	}
	fmt.Print(" \033[1m[↑/↓ j/k]\033[0m Nav · \033[1m[1-9]\033[0m Jump · \033[1;32m[Enter]\033[0m Switch · \033[1;36m[N]\033[0m New · \033[1;33m[M]\033[0m Rename · \033[1;31m[D]\033[0m Delete · \033[1;36m[V]\033[0m Quota · \033[1;36m[L]\033[0m Launch · \033[1;35m[A]\033[0m Auto · \033[1;31m[Q/Esc]\033[0m Exit\r\n")
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

