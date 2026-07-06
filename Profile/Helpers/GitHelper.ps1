#region GIT HELPER
# ==============================================================================
#  Shortcuts and wrappers for common Git operations.
# ==============================================================================

class GitHelper {
    static [void] Status([string[]]$PassThruArgs) {
        Write-Host "🔍 Git Status" -ForegroundColor Yellow
        if ($PassThruArgs) { git status @PassThruArgs | Out-Default } else { git status | Out-Default }
    }

    static [void] Undo() {
        Write-Host "⏪ Undoing last commit (keeping changes)..." -ForegroundColor Yellow
        git reset --soft HEAD~1 | Out-Default
    }

    static [void] Unstage() {
        Write-Host "🔙 Unstaging changes..." -ForegroundColor Yellow
        git restore --staged . | Out-Default
    }

    static [void] StashSnapshot([string]$Message) {
        $msg = if ($Message) { $Message } else { "snapshot" }
        git stash push -m "$msg" | Out-Default
        git stash apply 0 | Out-Default
        Write-Host "📸 Snapshot stashed: $msg" -ForegroundColor Green
    }

    static [void] Diff() {
        git diff | Out-Default
    }

    static [void] AddAll() {
        Write-Host "✅ Staging all changes..." -ForegroundColor Green
        git add . | Out-Default
    }

    static [void] Commit([string[]]$Message) {
        $commitMessage = $Message -join ' '
        Write-Host "📝 Committing with message: '$commitMessage'" -ForegroundColor Cyan
        git commit -m "$commitMessage" | Out-Default
    }

    static [void] Amend([string[]]$PassThruArgs) {
        Write-Host "✍️ Amending previous commit..." -ForegroundColor Cyan
        if ($PassThruArgs) { git commit --amend @PassThruArgs | Out-Default } else { git commit --amend | Out-Default }
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
        git log | Out-Default --graph --oneline --decorate --all | Out-Default
    }

    static [void] LogPretty() {
        git log | Out-Default --pretty=format:"%C(yellow)%h%Creset -%C(red)%d%Creset %s %C(green)(%cr) %C(bold blue)<%an>%Creset" --abbrev-commit | Out-Default
    }

    static [void] Log() {
        git log | Out-Default
    }

    static [void] Pull([string[]]$PassThruArgs) {
        Write-Host "⏬ Pulling changes from remote..." -ForegroundColor Blue
        if ($PassThruArgs) { git pull @PassThruArgs | Out-Default } else { git pull | Out-Default }
    }

    static [void] Push([string[]]$PassThruArgs) {
        Write-Host "⏫ Pushing changes to remote..." -ForegroundColor Blue
        if ($PassThruArgs) { git push @PassThruArgs | Out-Default } else { git push | Out-Default }
    }

    static [void] PushForce([string[]]$PassThruArgs) {
        Write-Host "⏫ Force pushing changes..." -ForegroundColor Red
        if ($PassThruArgs) { git push --force @PassThruArgs | Out-Default } else { git push --force | Out-Default }
    }

    static [void] Fetch() {
        Write-Host "🔎 Fetching from all remotes..." -ForegroundColor Blue
        git fetch --all --prune | Out-Default
    }

    static [void] GetBranches([string[]]$PassThruArgs) {
        Write-Host "🌿 Branches:" -ForegroundColor Green
        if ($PassThruArgs) { git branch @PassThruArgs | Out-Default } else { git branch | Out-Default }
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
}
#endregion



