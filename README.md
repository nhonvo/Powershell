# Enhanced PowerShell Profile 🚀

A modular, supercharged PowerShell configuration designed for .NET developers and AI enthusiasts.

## 📂 Structure

The profile is split into modular scripts within the `Profile/` directory for better maintainability:

- **00-Core.ps1**: Theme (`oh-my-posh`), `PSReadLine` settings, and module imports.
- **10-Aliases.ps1**: **CENTRALIZED** location for all aliases (WinGet, Git, .NET, AI, etc.).
- **20-Navigation.ps1**: Navigation logic (`Enter-Project`, `Set-LocationParent`).
- **30-System.ps1**: Utilities (`Reload-Profile`, `Invoke-VSCode`, `Invoke-OpenExplorer`).
- **50-DotNet.ps1**: Functions for the .NET CLI (`Invoke-DotNetRun`, `Update-Database`).
- **51-Git.ps1**: Git workflow functions (`Get-GitStatus`, `Invoke-GitAddAll`).
- **52-Docker.ps1**: Docker & Docker Compose functions.
- **53-AWS.ps1**: LocalStack SQS functions.
- **60-AI.ps1**: **NEW** Configuration for AI tools (Ollama, Claude, Copilot, Gemini).
- **99-Help.ps1**: Run `commands` to see a summary of available tools.

## 📦 Winget Shortcuts (New)

- **Search**: `wsearch <query>`
- **Install**: `winstall <package>`
- **List**: `wlist`
- **Update**: `wupdate`

## 🤖 AI Capabilities

This profile integrates local and cloud AI tools into your terminal flow.

### 🧠 Ollama (Local AI)

- **Check Status**: `ox` (Lists running models).
- **Run**: `ollama run <model>` (Standard CLI).
- **Config**: Default host is `localhost:11434`.

### 🐱 GitHub Copilot CLI

- **Suggest**: `?? "create a regex for email"` (Wraps `gh copilot suggest`).
- **Explain**: `what? "git reset --soft HEAD~1"` (Wraps `gh copilot explain`).
- _Requires `gh` CLI and `copilot` extension._

### 🤖 Claude

- **Run**: `claude` (Aliased to your local binary).
- **Config**: Can be configured to point to Ollama (mimicking Anthropic API) in `60-AI.ps1`.

### 💎 Gemini

- **Config**: Use `Set-GeminiKey` to set your API key for the session.

## 🚀 Setup & Usage

1. **Prerequisites**:
   - PowerShell 7+
   - [Oh My Posh](https://ohmyposh.dev/)
   - [Terminal Icons](https://github.com/devblackops/Terminal-Icons)
   - Git, .NET SDK, Docker (optional)

2. **Installation**:
   Clone this repo to your Documents folder:

   ```powershell
   cd $home\Documents
   git clone <repo-url> Powershell
   ```

3. **Reload**:
   Type `go` to reload the profile after changes.

4. **Explore**:
   Type `commands` to see the custom help menu.

## 🛠 customization

- **Theme**: Edit `00-Core.ps1` to change the Oh My Posh theme.
- **Projects**: Edit `20-Navigation.ps1` to add your own paths to the `proj` menu.
