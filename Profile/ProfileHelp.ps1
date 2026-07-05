#region PROFILE HELP
# ==============================================================================
#  Exposes interactive help documentation for all custom commands.
# ==============================================================================

class CommandDoc {
    [string]$Alias
    [string]$FullName
    [string]$Desc
    [string]$Command
}

class ProfileHelp {
    static [System.Collections.Generic.Dictionary[string, CommandDoc[]]] GetCommands() {
        $dict = [System.Collections.Generic.Dictionary[string, CommandDoc[]]]::new()

        $dict["Navigation & System"] = @(
            [CommandDoc]@{ Alias = "..";      FullName = "..";      Desc = "Navigate up one directory level"; Command = "Set-Location .." }
            [CommandDoc]@{ Alias = "...";     FullName = "...";     Desc = "Navigate up two directory levels"; Command = "Set-Location ..\.." }
            [CommandDoc]@{ Alias = "prj";     FullName = "prj";     Desc = "Interactive project navigator/selector"; Command = "[ProfileNavigator]::EnterProject()" }
            [CommandDoc]@{ Alias = "usage";   FullName = "usage";   Desc = "Display summary of disk usage on drives"; Command = "[SystemHelper]::GetDiskSpace()" }
            [CommandDoc]@{ Alias = "kill";    FullName = "kill";    Desc = "Friendly process termination grid selector"; Command = "[SystemHelper]::StopProcessFriendly()" }
            [CommandDoc]@{ Alias = "ssh-info"; FullName = "ssh-info"; Desc = "Display Tailscale connection details & active port 22 SSH sessions"; Command = "[SshHelper]::GetConnectionInfo()" }
            [CommandDoc]@{ Alias = "ssh-addkey"; FullName = "ssh-addkey"; Desc = "Authorize a public SSH key for passwordless login"; Command = "[SshHelper]::AddAuthorizedKey()" }
        )

        $dict["Git"] = @(
            [CommandDoc]@{ Alias = "gs";   FullName = "gs";   Desc = "Shows git status"; Command = "[GitHelper]::Status()" }
            [CommandDoc]@{ Alias = "gd";   FullName = "gd";   Desc = "Shows git diff"; Command = "[GitHelper]::Diff()" }
            [CommandDoc]@{ Alias = "glo";  FullName = "glo";  Desc = "Shows git log graph"; Command = "[GitHelper]::LogGraph()" }
            [CommandDoc]@{ Alias = "ga";   FullName = "ga";   Desc = "Stages all files (git add .)"; Command = "[GitHelper]::AddAll()" }
            [CommandDoc]@{ Alias = "gcmt"; FullName = "gcmt"; Desc = "Commit changes with message"; Command = "[GitHelper]::Commit()" }
            [CommandDoc]@{ Alias = "gundo"; FullName = "gundo"; Desc = "Undo the last commit (keep changes)"; Command = "[GitHelper]::Undo()" }
        )

        $dict[".NET"] = @(
            [CommandDoc]@{ Alias = "dr";   FullName = "dr";   Desc = "Runs the current .NET project"; Command = "[DotNetHelper]::Run()" }
            [CommandDoc]@{ Alias = "db";   FullName = "db";   Desc = "Builds the current .NET project"; Command = "[DotNetHelper]::Build()" }
            [CommandDoc]@{ Alias = "dt";   FullName = "dt";   Desc = "Runs unit tests"; Command = "[DotNetHelper]::Test()" }
            [CommandDoc]@{ Alias = "dw";   FullName = "dw";   Desc = "Watch project for changes"; Command = "[DotNetHelper]::Watch()" }
            [CommandDoc]@{ Alias = "dclean"; FullName = "dclean"; Desc = "Remove all bin/ and obj/ folders recursively"; Command = "[DotNetHelper]::CleanBinObj()" }
        )

        $dict["Docker"] = @(
            [CommandDoc]@{ Alias = "dkcl";   FullName = "dkcl";   Desc = "Lists all running Docker containers"; Command = "[DockerHelper]::GetContainers($false)" }
            [CommandDoc]@{ Alias = "dkcpu";  FullName = "dkcpu";  Desc = "Runs docker-compose up"; Command = "[DockerHelper]::ComposeUp()" }
            [CommandDoc]@{ Alias = "dkcpd";  FullName = "dkcpd";  Desc = "Runs docker-compose down"; Command = "[DockerHelper]::ComposeDown()" }
        )

        $dict["AI Tools"] = @(
            [CommandDoc]@{ Alias = "claude";   FullName = "claude";   Desc = "Launch Anthropic Claude Code via local Ollama"; Command = "[AiHelper]::InvokeClaude()" }
            [CommandDoc]@{ Alias = "codex";    FullName = "codex";    Desc = "Launch Codex CLI via local Ollama"; Command = "[AiHelper]::InvokeCodex()" }
            [CommandDoc]@{ Alias = "model";    FullName = "model";    Desc = "Configure default local Ollama model"; Command = "[AiHelper]::SetOllamaModel()" }
            [CommandDoc]@{ Alias = "agy-acc";  FullName = "agy-acc";  Desc = "Dynamic Multi-Account Manager interactively"; Command = "[AgyAccountManager]::SelectAccountInteractive()" }
        )

        return $dict
    }

    static [void] Show([string]$CategoryFilter) {
        $cmds = [ProfileHelp]::GetCommands()

        if ($CategoryFilter) {
            $matchedKey = $cmds.Keys | Where-Object { $_ -like "*$CategoryFilter*" } | Select-Object -First 1
            if ($matchedKey) {
                Write-Host "  $matchedKey" -ForegroundColor Cyan
                $items = $cmds[$matchedKey]
                $pad = ($items | ForEach-Object { $_.Alias.Length } | Measure-Object -Maximum).Maximum + 2
                foreach ($item in $items) {
                    Write-Host ("  {0,-$pad}" -f $item.Alias) -NoNewline -ForegroundColor White
                    Write-Host $item.Desc -ForegroundColor DarkGray
                }
            } else {
                Write-Warning "No category matching '$CategoryFilter' found."
            }
            return
        }

        # Dynamic TUI category menu using global TerminalMenu
        $categories = [string[]]($cmds.Keys | Sort-Object)

        while ($true) {
            $menuItems = @()
            foreach ($cat in $categories) {
                $menuItems += "$cat ($($cmds[$cat].Count) commands)"
            }

            $selectedCat = ([type]"TerminalMenu")::Show("Select Help Category", $menuItems, 0)
            if ($selectedCat -lt 0) { return }

            $catName = $categories[$selectedCat]
            $catCmds = $cmds[$catName]

            while ($true) {
                $cmdLabels = @()
                foreach ($cmd in $catCmds) {
                    $cmdLabels += "{0,-10} - {1}" -f $cmd.Alias, $cmd.Desc
                }

                $selectedCmd = ([type]"TerminalMenu")::Show("Category: $catName", $cmdLabels, 0)
                if ($selectedCmd -lt 0) { break } # Go back to categories

                $cmdObj = $catCmds[$selectedCmd]
                Clear-Host
                Write-Host ""
                Write-Host "  Running: $($cmdObj.Alias) ($($cmdObj.Command))" -ForegroundColor Green
                Write-Host ""

                try {
                    Invoke-Expression $cmdObj.Alias
                } catch {
                    Write-Error "Execution failed: $_"
                }

                Write-Host ""
                Write-Host "  [Press any key to return to help menu]" -ForegroundColor DarkGray
                $null = [Console]::ReadKey($true)
            }
        }
    }
}
#endregion
