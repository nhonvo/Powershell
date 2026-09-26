//go:build windows

package vault

import (
	"runtime"
	"strings"
	"syscall"
	"unicode/utf16"
	"unsafe"
)

type winCREDENTIAL struct {
	Flags              uint32
	Type               uint32
	TargetName         *uint16
	Comment            *uint16
	LastWritten        uint64
	CredentialBlobSize uint32
	CredentialBlob     uintptr
	Persist            uint32
	AttributeCount     uint32
	Attributes         uintptr
	TargetAlias        *uint16
	UserName           *uint16
}

// ReadWindowsCredential reads stored credentials from Windows Credential Manager using CredReadW API.
func ReadWindowsCredential(target string) string {
	if runtime.GOOS != "windows" {
		return ""
	}
	advapi32 := syscall.NewLazyDLL("advapi32.dll")
	procCredRead := advapi32.NewProc("CredReadW")
	procCredFree := advapi32.NewProc("CredFree")

	targetPtr, err := syscall.UTF16PtrFromString(target)
	if err != nil {
		return ""
	}

	var credPtr uintptr
	r1, _, _ := procCredRead.Call(
		uintptr(unsafe.Pointer(targetPtr)),
		uintptr(1), // CRED_TYPE_GENERIC
		uintptr(0),
		uintptr(unsafe.Pointer(&credPtr)),
	)

	if r1 == 0 || credPtr == 0 {
		return ""
	}

	defer procCredFree.Call(credPtr)

	cred := (*winCREDENTIAL)(unsafe.Pointer(credPtr))
	if cred.CredentialBlob == 0 || cred.CredentialBlobSize == 0 {
		return ""
	}

	blob := unsafe.Slice((*byte)(unsafe.Pointer(cred.CredentialBlob)), cred.CredentialBlobSize)
	raw := string(blob)
	raw = strings.TrimPrefix(raw, "\ufeff")
	raw = strings.TrimPrefix(raw, "\xef\xbb\xbf")
	raw = strings.TrimSpace(raw)

	if tok := ExtractCleanAccessToken(raw); tok != "" {
		if strings.HasPrefix(raw, "{") && strings.HasSuffix(raw, "}") {
			return raw
		}
		return tok
	}

	// Try UTF-16LE decoding fallback
	if len(blob)%2 == 0 && len(blob) >= 2 {
		u16s := make([]uint16, len(blob)/2)
		for i := 0; i < len(u16s); i++ {
			u16s[i] = uint16(blob[i*2]) | uint16(blob[i*2+1])<<8
		}
		str := string(utf16.Decode(u16s))
		str = strings.TrimPrefix(str, "\ufeff")
		str = strings.TrimSpace(str)
		if tok := ExtractCleanAccessToken(str); tok != "" {
			if strings.HasPrefix(str, "{") && strings.HasSuffix(str, "}") {
				return str
			}
			return tok
		}
	}

	return ""
}

// WriteWindowsCredential writes or updates a generic credential in Windows Credential Manager using CredWriteW API.
func WriteWindowsCredential(target string, secret string) bool {
	if runtime.GOOS != "windows" || target == "" || secret == "" {
		return false
	}
	advapi32 := syscall.NewLazyDLL("advapi32.dll")
	procCredWrite := advapi32.NewProc("CredWriteW")

	targetPtr, err := syscall.UTF16PtrFromString(target)
	if err != nil {
		return false
	}

	blob := []byte(secret)
	var blobPtr uintptr
	if len(blob) > 0 {
		blobPtr = uintptr(unsafe.Pointer(&blob[0]))
	}

	cred := winCREDENTIAL{
		Type:               1, // CRED_TYPE_GENERIC
		TargetName:         targetPtr,
		CredentialBlobSize: uint32(len(blob)),
		CredentialBlob:     blobPtr,
		Persist:            2, // CRED_PERSIST_LOCAL_MACHINE
	}

	r1, _, _ := procCredWrite.Call(
		uintptr(unsafe.Pointer(&cred)),
		uintptr(0),
	)

	return r1 != 0
}

// DeleteWindowsCredential removes a generic credential from Windows Credential Manager using CredDeleteW API.
func DeleteWindowsCredential(target string) bool {
	if runtime.GOOS != "windows" || target == "" {
		return false
	}
	advapi32 := syscall.NewLazyDLL("advapi32.dll")
	procCredDelete := advapi32.NewProc("CredDeleteW")

	targetPtr, err := syscall.UTF16PtrFromString(target)
	if err != nil {
		return false
	}

	r1, _, _ := procCredDelete.Call(
		uintptr(unsafe.Pointer(targetPtr)),
		uintptr(1), // CRED_TYPE_GENERIC
		uintptr(0),
	)

	return r1 != 0
}
