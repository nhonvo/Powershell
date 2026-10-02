---
name: security-audit
description: Deep security and vulnerability audit for repositories, shell scripts, and Go micro-apps. Use when conducting a security assessment, hunting for hardcoded credentials, reviewing command execution sanitization, or checking IPC/PTY isolation.
---

# Security & Vulnerability Audit Protocol

This skill provides a focused security audit checklist and scanning workflow.

## Security Audit Checkpoints

### 1. Hardcoded Secrets & Credentials
- Scan for high-entropy tokens, API keys, passwords, and private certificates:
  - Regex patterns for Gemini API keys (`AIza[0-9A-Za-z-_]{35}`)
  - Telegram bot tokens (`[0-9]{9,10}:[a-zA-Z0-9_-]{35}`)
  - Tailscale auth keys (`tskey-auth-[a-zA-Z0-9]+`)
  - SSH private keys and PEM headers

### 2. Shell & Command Injection
- Inspect all invocations of `os/exec.Command`, `syscall.Exec`, and PowerShell `Invoke-Expression` / `Start-Process`:
  - Ensure arguments are passed as separate slice elements, never concatenated into raw shell strings (e.g. `bash -c "... $var ..."`).
  - Check for untrusted input passed to shell interpreters.

### 3. File Path Traversal
- Verify that user-supplied filenames, project names, or account names are sanitized using `filepath.Clean` and prevented from escaping base directories using `..` traversal.

### 4. Process Isolation & Resource Boundaries
- Check child processes spawned in pseudo-terminals:
  - Are process groups cleaned up on termination (`SIGTERM` / `SIGKILL`)?
  - Are environment variables scrubbed to prevent unintended inheritance of sensitive host credentials?

### 5. IPC & Local Daemon Security
- Verify local HTTP / WebSocket endpoints (e.g. `agyswitch` sidecar, `agybot` daemon):
  - Do they bind strictly to `127.0.0.1` / `localhost` instead of `0.0.0.0` unless explicitly intended?
  - Are authentication tokens / CSRF tokens checked on sensitive state mutation endpoints?
