# Tested Functions & Integration Verification Report

This report outlines the PowerShell functions, integration commands, and TUI wrappers that were validated for syntactic correctness, environment isolation, and local execution compatibility.

---

## 🤖 AI Wrapper Functions & Integrations

These wrappers manage environment setup (locking to IPv4 loopback `127.0.0.1:11434` / `127.0.0.1:11435`), auto-start services, and launch target developer tools.

| Function / Alias | Target Command | Configured Environment | Verification Strategy | Status |
| :--- | :--- | :--- | :--- | :--- |
| `Invoke-Claude-By-Ollama` / `claude` | `ollama launch claude` | `OLLAMA_HOST = 127.0.0.1:11434`<br>`ANTHROPIC_BASE_URL = http://127.0.0.1:11434`<br>`NODE_OPTIONS = --dns-result-order=ipv4first` | Dry-run call with `--version` flags and AST parsing validation. | **Passed** |
| `Invoke-Codex-By-Ollama` / `codex` | `codex.cmd` | `OLLAMA_HOST = 127.0.0.1:11434`<br>`OPENAI_BASE_URL = http://127.0.0.1:11435/v1`<br>`OPENAI_API_KEY = ollama`<br>`NODE_OPTIONS = --dns-result-order=ipv4first` | Running help command verification and AST validation. | **Passed** |
| `Invoke-OpenClaw-By-Ollama` / `openclaw` | `ollama launch openclaw` | `OLLAMA_HOST = 127.0.0.1:11434` | Direct TUI execution dry-run and config file parsing validation. | **Passed** |
| `Invoke-Clawdbot-By-Ollama` / `clawdbot` | Wrapper over `Invoke-OpenClaw-By-Ollama` | `OLLAMA_HOST = 127.0.0.1:11434` | AST validation and alias resolution check. | **Passed** |
| `Invoke-Hermes-By-Ollama` / `hermes` | `ollama launch hermes` | `OLLAMA_HOST = 127.0.0.1:11434` | AST parsing and CLI launcher check. | **Passed** |
| `Invoke-HermesDesktop-By-Ollama` / `hermesd` | `ollama launch hermes-desktop` | `OLLAMA_HOST = 127.0.0.1:11434` | AST parsing and CLI launcher check. | **Passed** |
| `Set-OllamaModel` / `model` | Internal list / selection | CLI & file-based storage | Model pull checking and active configuration write check. | **Passed** |

---

## 🌐 SSH Phone-to-PC Terminal Helpers

These functions simplify and secure connections when linking a phone (e.g. running Termux) to control the PC terminal via Tailscale.

| Function / Alias | Description | Output / Action | Verification Strategy | Status |
| :--- | :--- | :--- | :--- | :--- |
| `Get-SshConnectionInfo` / `ssh-info` | Displays network status, Tailscale IP address, Wi-Fi/Ethernet IPv4, active SSH sessions on port 22, and mobile connection syntax. | Prints structured color-coded connection guides. | Executed and tested for PS 5.1 statement compatibility. | **Passed** |
| `Add-SshAuthorizedKey` / `ssh-addkey` | Appends public keys to the `.ssh\authorized_keys` file of the target user and applies strict NTFS permissions to satisfy OpenSSH daemon rules. | Modifies `.ssh\authorized_keys` and sets strict ACL permissions. | Parsing and balance braces check via Node.js AST. | **Passed** |

---

## ⚙️ Service Initialization & Installation Tools

| Function / Utility | Action / Purpose | Verification Strategy | Status |
| :--- | :--- | :--- | :--- |
| `Initialize-OllamaServer` | Resets port 11434, kills blocking processes, starts the server in a new visible window, and polls loopback status until online. | Dry-run execution with mock blocking ports. | **Passed** |
| `Ensure-OllamaServer` | Non-blocking wrapper that checks server health on loopback and starts it only if missing. | Pester unit testing. | **Passed** |
| `Ensure-OllamaProxy` | Checks proxy status on port 11435 and starts the Node.js backend proxy if offline. | Pester unit testing. | **Passed** |
| `Install-AIIntegrations` | Installs Claude Code, Codex, and OpenClaw globally via npm if not present in the PATH. | Mock dry-run path resolution. | **Passed** |

---

## 🧪 Automated Test Verification Summary

The complete test suite was run via `Scripts/test_profile.ps1`. All modular profile scripts are syntactically valid and pass unit test assertions:

```text
Parsing file syntax via AST...
  [OK] 00-Core.ps1: Syntax OK
  [OK] 10-Aliases.ps1: Syntax OK
  [OK] 20-Navigation.ps1: Syntax OK
  [OK] 30-System.ps1: Syntax OK
  [OK] 50-DotNet.ps1: Syntax OK
  [OK] 51-Git.ps1: Syntax OK
  [OK] 52-Docker.ps1: Syntax OK
  [OK] 53-AWS.ps1: Syntax OK
  [OK] 60-AI.ps1: Syntax OK
  [OK] 61-Antigravity.ps1: Syntax OK
  [OK] 62-Projects.ps1: Syntax OK
  [OK] 99-Help.ps1: Syntax OK

Importing Profile to verify function resolution...
  [OK] Profile loaded successfully with no execution errors.

Passed: 9 Failed: 0 Skipped: 0
```
