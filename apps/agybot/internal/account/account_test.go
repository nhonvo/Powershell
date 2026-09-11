package account

import (
	"testing"
)

func TestAccountManager_GetActiveAndList(t *testing.T) {
	mgr := NewAccountManager()

	active := mgr.GetActiveAccount()
	if active == "" {
		t.Errorf("Expected non-empty active account name")
	}

	accounts := mgr.ListAccounts()
	if len(accounts) == 0 {
		t.Logf("No accounts found in home directory or running in sandbox")
	} else {
		foundActive := false
		for _, a := range accounts {
			if a.AccountName == active {
				foundActive = true
			}
		}
		t.Logf("Found %d accounts, active is %s (found: %v)", len(accounts), active, foundActive)
	}
}
