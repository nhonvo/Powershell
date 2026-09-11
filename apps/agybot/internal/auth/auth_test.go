package auth

import (
	"testing"
	"time"
)

func TestAuth_HashAndVerifyPIN(t *testing.T) {
	pin := "888999"
	hashed := HashPIN(pin)
	if hashed == "" {
		t.Fatalf("Expected non-empty hashed pin")
	}

	if !VerifyPIN(pin, hashed) {
		t.Errorf("Expected PIN %s to verify against hash %s", pin, hashed)
	}

	if VerifyPIN("wrong_pin", hashed) {
		t.Errorf("Expected wrong PIN to fail verification")
	}
}

func TestAuth_ManagerFlow(t *testing.T) {
	pin := "556677"
	hashed := HashPIN(pin)
	whitelist := map[int64]bool{123: true}

	mgr := NewAuthManager(hashed, 3, 100*time.Millisecond, 200*time.Millisecond, whitelist)

	// 1. Unauthorized user
	isAuth, _, _ := mgr.CheckAuth(999)
	if isAuth {
		t.Errorf("Expected non-whitelisted user to fail check")
	}

	// 2. Initial state for user 123
	isAuth, isLocked, _ := mgr.CheckAuth(123)
	if isAuth || isLocked {
		t.Errorf("Expected user to be unauthenticated and unlocked initially")
	}

	// 3. Failed PIN attempt
	ok, _ := mgr.Authenticate(123, "000000")
	if ok {
		t.Errorf("Expected authentication to fail with incorrect PIN")
	}

	// 4. Successful PIN
	ok, _ = mgr.Authenticate(123, pin)
	if !ok {
		t.Errorf("Expected authentication to succeed with correct PIN")
	}
	isAuth, _, _ = mgr.CheckAuth(123)
	if !isAuth {
		t.Errorf("Expected session to be authenticated now")
	}

	// 5. Lock session
	mgr.Lock(123)
	isAuth, _, _ = mgr.CheckAuth(123)
	if isAuth {
		t.Errorf("Expected session to be locked after Lock()")
	}
}
