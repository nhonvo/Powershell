#region GIT HELPER
# ==============================================================================
#  Shortcuts and wrappers for common Git operations.
# ==============================================================================

class GitHelper {
    static [void] Status([string[]]$PassThruArgs) {
        Write-Host "🔍 Git Status" -ForegroundColor Yellow
        if ($PassThruArgs) { git status @PassThruArgs } else { git status }
    }

    static [void] Undo() {
        Write-Host "⏪ Undoing last commit (keeping changes)..." -ForegroundColor Yellow
        git reset --soft HEAD~1
    }

    static [void] Unstage() {
        Write-Host "🔙 Unstaging changes..." -ForegroundColor Yellow
        git restore --staged .
    }

    static [void] StashSnapshot([string]$Message) {
        $msg = if ($Message) { $Message } else { "snapshot" }
        git stash push -m "$msg"
        git stash apply 0
        Write-Host "📸 Snapshot stashed: $msg" -ForegroundColor Green
    }

    static [void] Diff() {
        git diff
    }

    static [void] AddAll() {
        Write-Host "✅ Staging all changes..." -ForegroundColor Green
        git add .
    }

    static [void] Commit([string[]]$Message) {
        $commitMessage = $Message -join ' '
        Write-Host "📝 Committing with message: '$commitMessage'" -ForegroundColor Cyan
        git commit -m "$commitMessage"
    }

    static [void] Amend([string[]]$PassThruArgs) {
        Write-Host "✍️ Amending previous commit..." -ForegroundColor Cyan
        if ($PassThruArgs) { git commit --amend @PassThruArgs } else { git commit --amend }
    }

    static [void] Checkout([string]$branchName) {
        Write-Host "Check out branch: $branchName" -ForegroundColor Green
        git checkout $branchName
    }

    static [void] NewBranch([string]$branchName) {
        Write-Host "Check out and create a new branch: $branchName" -ForegroundColor Green
        git checkout -b $branchName
    }

    static [void] LogGraph() {
        git log --graph --oneline --decorate --all
    }

    static [void] LogPretty() {
        git log --pretty=format:"%C(yellow)%h%Creset -%C(red)%d%Creset %s %C(green)(%cr) %C(bold blue)<%an>%Creset" --abbrev-commit
    }

    static [void] Log() {
        git log
    }

    static [void] Pull([string[]]$PassThruArgs) {
        Write-Host "⏬ Pulling changes from remote..." -ForegroundColor Blue
        if ($PassThruArgs) { git pull @PassThruArgs } else { git pull }
    }

    static [void] Push([string[]]$PassThruArgs) {
        Write-Host "⏫ Pushing changes to remote..." -ForegroundColor Blue
        if ($PassThruArgs) { git push @PassThruArgs } else { git push }
    }

    static [void] PushForce([string[]]$PassThruArgs) {
        Write-Host "⏫ Force pushing changes..." -ForegroundColor Red
        if ($PassThruArgs) { git push --force @PassThruArgs } else { git push --force }
    }

    static [void] Fetch() {
        Write-Host "🔎 Fetching from all remotes..." -ForegroundColor Blue
        git fetch --all --prune
    }

    static [void] GetBranches([string[]]$PassThruArgs) {
        Write-Host "🌿 Branches:" -ForegroundColor Green
        if ($PassThruArgs) { git branch @PassThruArgs } else { git branch }
    }

    static [void] RemoveBranch([string]$branchName) {
        git branch -d $branchName
    }

    static [void] MergeSquash([string]$BranchName) {
        Write-Host "Squash merging branch: $BranchName" -ForegroundColor Yellow
        git merge --squash $BranchName
        Write-Host "Commit the squashed changes!" -ForegroundColor Cyan
    }

    static [void] ResetSoft() {
        git reset HEAD~
    }

    static [void] ResetHard() {
        git reset --hard
    }
}
#endregion
