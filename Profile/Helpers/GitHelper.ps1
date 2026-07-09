#region GIT HELPER
# ==============================================================================
#  Shortcuts and wrappers for common Git operations.
# ==============================================================================

class GitHelper {
    static [void] Status([string[]]$PassThruArgs) {
        Write-Host "[Git] Status" -ForegroundColor Yellow
        if ($PassThruArgs) { git status $PassThruArgs | Out-Default } else { git status | Out-Default }
    }

    static [void] Undo() {
        Write-Host "[Git] Undoing last commit (keeping changes)..." -ForegroundColor Yellow
        git reset --soft HEAD~1 | Out-Default
    }

    static [void] Unstage() {
        Write-Host "[Git] Unstaging changes..." -ForegroundColor Yellow
        git restore --staged . | Out-Default
    }

    static [void] StashSnapshot([string]$Message) {
        $msg = if ($Message) { $Message } else { "snapshot" }
        git stash push -m "$msg" | Out-Default
        git stash apply 0 | Out-Default
        Write-Host "[Git] Snapshot stashed: $msg" -ForegroundColor Green
    }

    static [void] Diff() {
        git diff | Out-Default
    }

    static [void] AddAll() {
        Write-Host "[Git] Staging all changes..." -ForegroundColor Green
        git add . | Out-Default
    }

    static [void] Commit([string[]]$Message) {
        $commitMessage = $Message -join ' '
        Write-Host "[Git] Committing with message: '$commitMessage'" -ForegroundColor Cyan
        git commit -m "$commitMessage" | Out-Default
    }

    static [void] Amend([string[]]$PassThruArgs) {
        Write-Host "[Git] Amending previous commit..." -ForegroundColor Cyan
        if ($PassThruArgs) { git commit --amend $PassThruArgs | Out-Default } else { git commit --amend | Out-Default }
    }

    static [void] Checkout([string]$branchName) {
        Write-Host "Check out branch: $branchName" -ForegroundColor Green
        git checkout $branchName | Out-Default
    }

    static [void] NewBranch([string]$branchName) {
        Write-Host "Check out and create a new branch: $branchName" -ForegroundColor Green
        git checkout -b $branchName | Out-Default
    }

    static [void] LogGraph() {
        git log --graph --oneline --decorate --all | Out-Default
    }

    static [void] LogPretty() {
        git log --pretty=format:"%C(yellow)%h%Creset -%C(red)%d%Creset %s %C(green)(%cr) %C(bold blue)<%an>%Creset" --abbrev-commit | Out-Default
    }

    static [void] Log() {
        git log | Out-Default
    }

    static [void] Pull([string[]]$PassThruArgs) {
        Write-Host "[Git] Pulling changes from remote..." -ForegroundColor Blue
        if ($PassThruArgs) { git pull $PassThruArgs | Out-Default } else { git pull | Out-Default }
    }

    static [void] Push([string[]]$PassThruArgs) {
        Write-Host "[Git] Pushing changes to remote..." -ForegroundColor Blue
        if ($PassThruArgs) { git push $PassThruArgs | Out-Default } else { git push | Out-Default }
    }

    static [void] PushForce([string[]]$PassThruArgs) {
        Write-Host "[Git] Force pushing changes..." -ForegroundColor Red
        if ($PassThruArgs) { git push --force $PassThruArgs | Out-Default } else { git push --force | Out-Default }
    }

    static [void] Fetch() {
        Write-Host "[Git] Fetching from all remotes..." -ForegroundColor Blue
        git fetch --all --prune | Out-Default
    }

    static [void] GetBranches([string[]]$PassThruArgs) {
        Write-Host "[Git] Branches:" -ForegroundColor Green
        if ($PassThruArgs) { git branch $PassThruArgs | Out-Default } else { git branch | Out-Default }
    }

    static [void] RemoveBranch([string]$branchName) {
        git branch -d $branchName | Out-Default
    }

    static [void] MergeSquash([string]$BranchName) {
        Write-Host "Squash merging branch: $BranchName" -ForegroundColor Yellow
        git merge --squash $BranchName | Out-Default
        Write-Host "Commit the squashed changes!" -ForegroundColor Cyan
    }

    static [void] ResetSoft() {
        git reset HEAD~ | Out-Default
    }

    static [void] ResetHard() {
        git reset --hard | Out-Default
    }

    static [void] BranchCheckoutTui() {
        if (-not (Test-Path ".git")) {
            Write-Error "Not a git repository."
            return
        }
        $branches = @()
        try {
            $raw = git branch --format="%(refname:short)" 2>$null
            if ($null -ne $raw) {
                foreach ($b in $raw) {
                    if (-not [string]::IsNullOrWhiteSpace($b)) {
                        $branches += $b.Trim()
                    }
                }
            }
        } catch {}

        if ($Global:AiMode) {
            foreach ($b in $branches) {
                Write-Host $b
            }
            return
        }

        if ($branches.Count -eq 0) {
            Write-Host "No branches found." -ForegroundColor Yellow
            return
        }

        $selected = ([type]"TerminalMenu")::Show("Select Git Branch to Checkout", $branches, 0)
        if ($selected -ge 0) {
            $bName = $branches[$selected]
            Write-Host "Checking out branch: $bName" -ForegroundColor Green
            git checkout $bName
        } else {
            Write-Host "Cancelled." -ForegroundColor DarkGray
        }
    }

    static [string] GenerateAiCommitMessage() {
        $diff = git diff --cached
        if (-not $diff) { return "" }
        
        $prompt = "Generate a concise, one-line conventional commit description (excluding type/scope prefix) based on the following git diff. Output ONLY the description:`n`n$diff"
        
        $body = @{
            model = [AiHelper]::OllamaDefaultModel
            prompt = $prompt
            stream = $false
        } | ConvertTo-Json
        
        try {
            $res = Invoke-RestMethod -Method Post -Uri "http://127.0.0.1:11434/api/generate" -Body $body -ContentType "application/json" -TimeoutSec 5
            if ($res.response) {
                $desc = $res.response.Trim()
                $desc = $desc -replace '^(feat|fix|docs|style|refactor|test|chore)(\(.*?\))?:\s*', ''
                return $desc
            }
        } catch {
            Write-Warning "Failed to contact Ollama for AI commit suggestion: $_"
        }
        return ""
    }

    static [void] Gcmt([string]$DirectMessage) {
        $staged = git diff --cached --name-only
        if (-not $staged) {
            Write-Warning "No staged changes found. Run git add first."
            return
        }

        if ($Global:AiMode -and -not $DirectMessage) {
            Write-Host "Usage: gcmt <message>"
            Write-Host "Supported Commit Types: feat, fix, docs, style, refactor, test, chore"
            return
        }

        if ($DirectMessage) {
            Write-Host "Committing with message: '$DirectMessage'" -ForegroundColor Cyan
            git commit -m "$DirectMessage"
            return
        }

        $types = @(
            "feat     (New feature)",
            "fix      (Bug fix)",
            "docs     (Documentation changes)",
            "style    (Formatting, missing semi colons, etc)",
            "refactor (Code restructuring without behavior changes)",
            "test     (Adding missing tests)",
            "chore    (Maintenance/dependencies)"
        )
        $sel = ([type]"TerminalMenu")::Show("Select Commit Type", $types, 0)
        if ($sel -lt 0) { return }

        $type = switch ($sel) {
            0 { "feat" }
            1 { "fix" }
            2 { "docs" }
            3 { "style" }
            4 { "refactor" }
            5 { "test" }
            6 { "chore" }
        }

        $scope = Read-Host "Enter commit scope (optional, press Enter to skip)"
        $scope = $scope.Trim()

        $desc = Read-Host "Enter commit description (or type 'ai' to auto-generate)"
        if ($desc -eq "ai") {
            Write-Host "[Ollama] Querying Ollama for commit suggestion..." -ForegroundColor Yellow
            $desc = [GitHelper]::GenerateAiCommitMessage()
            if (-not $desc) {
                $desc = Read-Host "Ollama failed to generate message. Please enter description manually"
            } else {
                Write-Host "[Ollama] Generated: $desc" -ForegroundColor Green
            }
        }

        if ([string]::IsNullOrWhiteSpace($desc)) {
            Write-Warning "Commit description cannot be empty."
            return
        }

        $finalMsg = if ($scope) { "${type}($scope): $desc" } else { "${type}: $desc" }
        Write-Host ""
        Write-Host "Generated commit message: '$finalMsg'" -ForegroundColor Cyan
        $confirm = Read-Host "Confirm commit? (Y/N)"
        if ($confirm -match "^[Yy]") {
            git commit -m "$finalMsg"
        } else {
            Write-Host "Commit cancelled." -ForegroundColor DarkGray
        }
    }
}
#endregion
