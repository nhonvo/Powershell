# External Dependencies & Tools Reference

This document centralizes all external command-line interface (CLI) tools, libraries, modules, and package dependencies utilized by the PowerShell Control Center and profile environment.

## Core System Utilities

| Dependency | Purpose | Target Commands / Functions | Installation & Configuration |
|---|---|---|---|
| **Oh My Posh** | Prompt theme engine and status visualization | `Set-DesktopThemeMode`, `Set-MobileThemeMode`, `Select-ShellTheme` | Installed in user AppData Local. Configuration loads themes from `asset/powershell-themes/config.json`. |
| **Neovim / Vim** | Terminal-based integrated development environment (IDE) | `Invoke-TerminalIde` (alias `ide`) | Invokes `nvim` or `vim` in the active project directory. |
| **Tailscale** | Secure mesh VPN system info and connection details | `Get-SshConnectionInfo` (alias `ssh-info`) | Resolves Tailscale IPs and open tunnels. |
| **git** | Version control workflows and conventional commits wizards | `Get-GitStatus` (alias `gs`), `Invoke-GitCommitWizard` (alias `gcmt`) | CLI executable in system PATH. |

## Development SDKs & Containerization

| Dependency | Purpose | Target Commands / Functions | Installation & Configuration |
|---|---|---|---|
| **.NET SDK** | Compilation, testing, formatting, and database migrations | `Invoke-DotNetBuild` (alias `dbld`), `Invoke-DotNetTest` (alias `dtst`) | Manages solutions, Console projects, and EF Core Migrations. |
| **Docker & Compose** | Container stack orchestration and logs streaming | `Invoke-ComposeUp` (alias `dcup`), `Invoke-ComposeDown` (alias `dcdown`), `Invoke-DockerDashboard` (alias `dkcl`) | Manages Docker containers, volumes, and image pruning. |
| **AWS CLI / LocalStack** | Local cloud services debugging (S3, SQS, Lambda) | `Get-S3Buckets`, `Get-LocalSQSQueues`, `Get-LambdaFunctions` | Interfaces with local S3 and SQS emulator environments. |

## AI Agents & Reasoning Models

| Dependency | Purpose | Target Commands / Functions | Installation & Configuration |
|---|---|---|---|
| **Ollama** | Local reasoning LLM host and server runner | `Ensure-OllamaServer`, `Initialize-OllamaServer` | Runs background service at `http://localhost:11434`. |
| **Claude Code** | Anthropic local command assistant | `Invoke-Claude-By-Ollama` (alias `claude`) | Executed via Ollama agent prompts. |
| **Codex CLI** | Local AI code completion terminal utility | `Invoke-Codex-By-Ollama` (alias `codex`) | Executed via Ollama agent prompts. |
| **OpenClaw** | Local ChatGPT-compatible client console | `Invoke-ChatGPT` (alias `openclaw`) | Routes prompts to active account. |
| **Hermes** | Reasoning/thinking console LLM | `Invoke-Hermes-By-Ollama` (alias `hermes`) | Executed via Ollama agent prompts. |

## PowerShell Modules

| Module | Purpose | Target Commands / Functions | Installation & Configuration |
|---|---|---|---|
| **Microsoft.PowerShell.ConsoleGuiTools** | Out-ConsoleGridView console grid widgets | `Get-CustomCommands` (alias `cc` fallback) | Installed dynamically if available in interactive sessions. |
| **PSReadLine** | Advanced syntax highlighting and key handlers | `Register-PoshStreamingOnIdle`, `Set-PSReadLineKeyHandler` | Installed natively in PowerShell Core. |
