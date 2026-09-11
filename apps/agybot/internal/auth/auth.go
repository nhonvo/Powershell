package auth

import (
	"crypto/rand"
	"crypto/subtle"
	"encoding/hex"
	"fmt"
	"strings"
	"sync"
	"time"

	"golang.org/x/crypto/scrypt"
)

type Permission string

const (
	PermReadStatus   Permission = "READ_STATUS"
	PermRunAIAgent   Permission = "RUN_AI_AGENT"
	PermExecuteShell Permission = "EXECUTE_SHELL"
	PermFullAdmin    Permission = "FULL_ADMIN"
)

type UserSession struct {
	UserID          int64
	Authenticated   bool
	AuthenticatedAt time.Time
	LastActivityAt  time.Time
	FailedAttempts  int
	LockedUntil     time.Time
	AwaitingPin     bool
	Permissions     map[Permission]bool
}

type AuthManager struct {
	mu              sync.RWMutex
	sessions        map[int64]*UserSession
	pinHash         string
	maxAttempts     int
	lockoutDuration time.Duration
	autoLockTimeout time.Duration
	allowedUserIDs  map[int64]bool
}

func NewAuthManager(pinHash string, maxAttempts int, lockout time.Duration, autoLock time.Duration, whitelist map[int64]bool) *AuthManager {
	if maxAttempts <= 0 {
		maxAttempts = 5
	}
	if lockout <= 0 {
		lockout = 5 * time.Minute
	}
	if autoLock <= 0 {
		autoLock = 30 * time.Minute
	}
	if pinHash == "" {
		// Default safe dev PIN "123456"
		pinHash = HashPIN("123456")
	}

	return &AuthManager{
		sessions:        make(map[int64]*UserSession),
		pinHash:         pinHash,
		maxAttempts:     maxAttempts,
		lockoutDuration: lockout,
		autoLockTimeout: autoLock,
		allowedUserIDs:  whitelist,
	}
}

// HashPIN produces a scrypt salt_hex:hash_hex string matching standard scrypt (N=16384, r=8, p=1, keyLen=32)
func HashPIN(pin string) string {
	salt := make([]byte, 16)
	_, _ = rand.Read(salt)

	dk, err := scrypt.Key([]byte(pin), salt, 16384, 8, 1, 32)
	if err != nil {
		return ""
	}

	return hex.EncodeToString(salt) + ":" + hex.EncodeToString(dk)
}

// VerifyPIN checks whether the given pin matches the salt_hex:hash_hex string
func VerifyPIN(pin string, storedHash string) bool {
	parts := strings.Split(storedHash, ":")
	if len(parts) != 2 {
		return false
	}

	salt, err := hex.DecodeString(parts[0])
	if err != nil {
		return false
	}
	expectedHash, err := hex.DecodeString(parts[1])
	if err != nil {
		return false
	}

	dk, err := scrypt.Key([]byte(pin), salt, 16384, 8, 1, len(expectedHash))
	if err != nil {
		return false
	}

	return subtle.ConstantTimeCompare(dk, expectedHash) == 1
}

// IsUserWhitelisted checks if user ID is present in allowed list (or if list is empty, permits all)
func (a *AuthManager) IsUserWhitelisted(userID int64) bool {
	if len(a.allowedUserIDs) == 0 {
		return true
	}
	return a.allowedUserIDs[userID]
}

func (a *AuthManager) getOrCreateSession(userID int64) *UserSession {
	if s, ok := a.sessions[userID]; ok {
		return s
	}
	s := &UserSession{
		UserID:         userID,
		LastActivityAt: time.Now(),
		Permissions: map[Permission]bool{
			PermFullAdmin: true,
		},
	}
	a.sessions[userID] = s
	return s
}

// CheckAuth inspects the session status for a given user.
// Returns (isAuthenticated, isLockedOut, reason)
func (a *AuthManager) CheckAuth(userID int64) (bool, bool, string) {
	a.mu.Lock()
	defer a.mu.Unlock()

	if !a.IsUserWhitelisted(userID) {
		return false, false, "User not in authorized whitelist"
	}

	s := a.getOrCreateSession(userID)
	now := time.Now()

	// 1. Check temporary brute-force lockout
	if !s.LockedUntil.IsZero() && now.Before(s.LockedUntil) {
		remaining := time.Until(s.LockedUntil).Round(time.Second)
		return false, true, fmt.Sprintf("Too many failed attempts. Locked for %v", remaining)
	}

	// 2. Check inactivity auto-lock
	if s.Authenticated {
		if now.Sub(s.LastActivityAt) > a.autoLockTimeout {
			s.Authenticated = false
			return false, false, "Session expired due to inactivity (Auto-Locked). Please enter PIN."
		}
		s.LastActivityAt = now
		return true, false, ""
	}

	return false, false, "Authentication required. Please enter PIN."
}

// Authenticate validates user PIN and activates session
func (a *AuthManager) Authenticate(userID int64, pin string) (bool, string) {
	a.mu.Lock()
	defer a.mu.Unlock()

	if !a.IsUserWhitelisted(userID) {
		return false, "User ID not authorized"
	}

	s := a.getOrCreateSession(userID)
	now := time.Now()

	if !s.LockedUntil.IsZero() && now.Before(s.LockedUntil) {
		remaining := time.Until(s.LockedUntil).Round(time.Second)
		return false, fmt.Sprintf("Account locked. Try again in %v", remaining)
	}

	if VerifyPIN(pin, a.pinHash) {
		s.Authenticated = true
		s.AuthenticatedAt = now
		s.LastActivityAt = now
		s.FailedAttempts = 0
		s.LockedUntil = time.Time{}
		s.AwaitingPin = false
		return true, "Authentication successful! Welcome to Antigravity Controller."
	}

	s.FailedAttempts++
	if s.FailedAttempts >= a.maxAttempts {
		s.LockedUntil = now.Add(a.lockoutDuration)
		s.FailedAttempts = 0
		return false, fmt.Sprintf("Incorrect PIN! Max attempts exceeded. Locked for %v.", a.lockoutDuration)
	}

	remaining := a.maxAttempts - s.FailedAttempts
	return false, fmt.Sprintf("Incorrect PIN! %d attempts remaining before temporary lockout.", remaining)
}

// Lock forces session into unauthenticated state
func (a *AuthManager) Lock(userID int64) {
	a.mu.Lock()
	defer a.mu.Unlock()

	if s, ok := a.sessions[userID]; ok {
		s.Authenticated = false
		s.AwaitingPin = false
	}
}

// SetAwaitingPin marks state for interactive telegram text handler
func (a *AuthManager) SetAwaitingPin(userID int64, awaiting bool) {
	a.mu.Lock()
	defer a.mu.Unlock()
	s := a.getOrCreateSession(userID)
	s.AwaitingPin = awaiting
}

func (a *AuthManager) IsAwaitingPin(userID int64) bool {
	a.mu.RLock()
	defer a.mu.RUnlock()
	if s, ok := a.sessions[userID]; ok {
		return s.AwaitingPin
	}
	return false
}
